// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Skinning;
using osu.Game.Storyboards;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// A component which performs and acts as a central cache for difficulty calculations of beatmap/ruleset/mod combinations.
    /// Currently not persisted between game sessions.
    /// </summary>
    public partial class BeatmapDifficultyCache
        : MemoryCachingComponent<BeatmapDifficultyCache.DifficultyCacheLookup, StarDifficulty?>
    {
        // Too many simultaneous updates can lead to stutters. One thread seems to work fine for song select display purposes.
        private readonly ThreadedTaskScheduler updateScheduler = new ThreadedTaskScheduler(
            1,
            nameof(BeatmapDifficultyCache)
        );

        /// <summary>
        /// All bindables that should be updated along with the current ruleset + mods.
        /// </summary>
        private readonly WeakList<BindableStarDifficulty> trackedBindables =
            new WeakList<BindableStarDifficulty>();

        /// <summary>
        /// Cancellation sources used by tracked bindables.
        /// </summary>
        private readonly List<CancellationTokenSource> linkedCancellationSources =
            new List<CancellationTokenSource>();

        /// <summary>
        /// Lock to be held when operating on <see cref="trackedBindables"/> or <see cref="linkedCancellationSources"/>.
        /// </summary>
        private readonly object bindableUpdateLock = new object();

        private CancellationTokenSource trackedUpdateCancellationSource =
            new CancellationTokenSource();

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        [Resolved]
        private Bindable<RulesetInfo> currentRuleset { get; set; } = null!;

        [Resolved]
        private Bindable<IReadOnlyList<Mod>> currentMods { get; set; } = null!;

        private ModSettingChangeTracker? modSettingChangeTracker;
        private ScheduledDelegate? debouncedModSettingsChange;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            currentRuleset.BindValueChanged(_ => Scheduler.AddOnce(updateTrackedBindables));

            currentMods.BindValueChanged(
                mods =>
                {
                    modSettingChangeTracker?.Dispose();

                    Scheduler.AddOnce(updateTrackedBindables);

                    modSettingChangeTracker = new ModSettingChangeTracker(mods.NewValue);
                    modSettingChangeTracker.SettingChanged += _ =>
                    {
                        debouncedModSettingsChange?.Cancel();
                        debouncedModSettingsChange = Scheduler.AddDelayed(
                            updateTrackedBindables,
                            100
                        );
                    };
                },
                true
            );
        }

        public void Invalidate(IBeatmapInfo beatmap)
        {
            base.Invalidate(lookup => lookup.BeatmapInfo.Equals(beatmap));
        }

        /// <summary>
        /// Retrieves a bindable containing the star difficulty of a <see cref="BeatmapInfo"/> that follows the currently-selected ruleset and mods.
        /// </summary>
        /// <param name="beatmapInfo">The <see cref="BeatmapInfo"/> to get the difficulty of.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> which stops updating the star difficulty for the given <see cref="BeatmapInfo"/>.</param>
        /// <param name="computationDelay">A delay in milliseconds before performing the </param>
        /// <returns>A bindable that is updated to contain the star difficulty when it becomes available. May be an approximation while in an initial calculating state.</returns>
        public IBindable<StarDifficulty> GetBindableDifficulty(
            IBeatmapInfo beatmapInfo,
            CancellationToken cancellationToken = default,
            int computationDelay = 0
        )
        {
            var bindable = new BindableStarDifficulty(beatmapInfo, cancellationToken)
            {
                // Start with an approximate known value instead of zero.
                Value = new StarDifficulty(beatmapInfo.StarRating, 0),
            };

            updateBindable(
                bindable,
                currentRuleset.Value,
                currentMods.Value,
                cancellationToken,
                computationDelay
            );

            lock (bindableUpdateLock)
                trackedBindables.Add(bindable);

            return bindable;
        }

        /// <summary>
        /// Retrieves the difficulty of a <see cref="IBeatmapInfo"/>.
        /// </summary>
        /// <param name="beatmapInfo">The <see cref="IBeatmapInfo"/> to get the difficulty of.</param>
        /// <param name="rulesetInfo">The <see cref="IRulesetInfo"/> to get the difficulty with.</param>
        /// <param name="mods">The <see cref="Mod"/>s to get the difficulty with.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> which stops computing the star difficulty.</param>
        /// <param name="computationDelay">In the case a cached lookup was not possible, a value in milliseconds of to wait until performing potentially intensive lookup.</param>
        /// <returns>
        /// The requested <see cref="StarDifficulty"/>, if non-<see langword="null"/>.
        /// A <see langword="null"/> return value indicates that the difficulty process failed or was interrupted early,
        /// and as such there is no usable star difficulty value to be returned.
        /// </returns>
        public virtual Task<StarDifficulty?> GetDifficultyAsync(
            IBeatmapInfo beatmapInfo,
            IRulesetInfo? rulesetInfo = null,
            IEnumerable<Mod>? mods = null,
            CancellationToken cancellationToken = default,
            int computationDelay = 0
        )
        {
            // In the case that the user hasn't given us a ruleset, use the beatmap's default ruleset.
            rulesetInfo ??= beatmapInfo.Ruleset;

            var localBeatmapInfo = beatmapInfo as BeatmapInfo;
            var localRulesetInfo = rulesetInfo as RulesetInfo;

            // Difficulty can only be computed if the beatmap and ruleset are locally available.
            if (localBeatmapInfo == null || localRulesetInfo == null)
            {
                // If not, fall back to the existing star difficulty (e.g. from an online source).
                return Task.FromResult<StarDifficulty?>(
                    new StarDifficulty(
                        beatmapInfo.StarRating,
                        (beatmapInfo as IBeatmapOnlineInfo)?.MaxCombo ?? 0
                    )
                );
            }

            return GetAsync(
                new DifficultyCacheLookup(localBeatmapInfo, localRulesetInfo, mods),
                cancellationToken,
                computationDelay
            );
        }

        protected override Task<StarDifficulty?> ComputeValueAsync(
            DifficultyCacheLookup lookup,
            CancellationToken cancellationToken = default
        )
        {
            return Task.Factory.StartNew(
                () =>
                {
                    if (CheckExists(lookup, out var existing))
                        return existing;

                    return computeDifficulty(lookup, cancellationToken);
                },
                cancellationToken,
                TaskCreationOptions.HideScheduler
                    | TaskCreationOptions.RunContinuationsAsynchronously,
                updateScheduler
            );
        }

        protected override bool CacheNullValues => false;

        public Task<List<TimedDifficultyAttributes>> GetTimedDifficultyAttributesAsync(
            IWorkingBeatmap beatmap,
            Ruleset ruleset,
            Mod[] mods,
            CancellationToken cancellationToken = default
        )
        {
            return Task.Factory.StartNew(
                () =>
                    ruleset
                        .CreateDifficultyCalculator(beatmap)
                        .CalculateTimed(mods, cancellationToken),
                cancellationToken,
                TaskCreationOptions.HideScheduler
                    | TaskCreationOptions.RunContinuationsAsynchronously,
                updateScheduler
            );
        }

        /// <summary>
        /// Updates all tracked <see cref="BindableStarDifficulty"/> using the current ruleset and mods.
        /// </summary>
        private void updateTrackedBindables()
        {
            lock (bindableUpdateLock)
            {
                cancelTrackedBindableUpdate();

                foreach (var b in trackedBindables)
                {
                    var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                        trackedUpdateCancellationSource.Token,
                        b.CancellationToken
                    );
                    linkedCancellationSources.Add(linkedSource);

                    updateBindable(b, currentRuleset.Value, currentMods.Value, linkedSource.Token);
                }
            }
        }

        /// <summary>
        /// Cancels the existing update of all tracked <see cref="BindableStarDifficulty"/> via <see cref="updateTrackedBindables"/>.
        /// </summary>
        private void cancelTrackedBindableUpdate()
        {
            lock (bindableUpdateLock)
            {
                trackedUpdateCancellationSource.Cancel();
                trackedUpdateCancellationSource = new CancellationTokenSource();

                foreach (var c in linkedCancellationSources)
                    c.Dispose();

                linkedCancellationSources.Clear();
            }
        }

        /// <summary>
        /// Updates the value of a <see cref="BindableStarDifficulty"/> with a given ruleset + mods.
        /// </summary>
        /// <param name="bindable">The <see cref="BindableStarDifficulty"/> to update.</param>
        /// <param name="rulesetInfo">The <see cref="IRulesetInfo"/> to update with.</param>
        /// <param name="mods">The <see cref="Mod"/>s to update with.</param>
        /// <param name="cancellationToken">A token that may be used to cancel this update.</param>
        /// <param name="computationDelay">In the case a cached lookup was not possible, a value in milliseconds of to wait until performing potentially intensive lookup.</param>
        private void updateBindable(
            BindableStarDifficulty bindable,
            IRulesetInfo? rulesetInfo,
            IEnumerable<Mod>? mods,
            CancellationToken cancellationToken = default,
            int computationDelay = 0
        )
        {
            // GetDifficultyAsync will fall back to existing data from IBeatmapInfo if not locally available
            // (contrary to GetAsync)
            GetDifficultyAsync(
                    bindable.BeatmapInfo,
                    rulesetInfo,
                    mods,
                    cancellationToken,
                    computationDelay
                )
                .ContinueWith(
                    task =>
                    {
                        // We're on a threadpool thread, but we should exit back to the update thread so consumers can safely handle value-changed events.
                        Schedule(() =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return;

                            StarDifficulty? starDifficulty = task.GetResultSafely();

                            if (starDifficulty != null)
                                bindable.Value = starDifficulty.Value;
                        });
                    },
                    cancellationToken
                );
        }

        /// <summary>
        /// Computes the difficulty defined by a <see cref="DifficultyCacheLookup"/> key, and stores it to the timed cache.
        /// </summary>
        /// <param name="key">The <see cref="DifficultyCacheLookup"/> that defines the computation parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="StarDifficulty"/>.</returns>
        private StarDifficulty? computeDifficulty(
            in DifficultyCacheLookup key,
            CancellationToken cancellationToken = default
        )
        {
            // In the case that the user hasn't given us a ruleset, use the beatmap's default ruleset.
            var beatmapInfo = key.BeatmapInfo;
            var rulesetInfo = key.Ruleset;

            try
            {
                var ruleset = rulesetInfo.CreateInstance();
                Debug.Assert(ruleset != null);

                PlayableCachedWorkingBeatmap workingBeatmap = new PlayableCachedWorkingBeatmap(
                    beatmapManager.GetWorkingBeatmap(key.BeatmapInfo)
                );
                IBeatmap playableBeatmap = workingBeatmap.GetPlayableBeatmap(
                    ruleset.RulesetInfo,
                    key.OrderedMods,
                    cancellationToken
                );

                var difficulty = ruleset
                    .CreateDifficultyCalculator(workingBeatmap)
                    .Calculate(key.OrderedMods, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                var performanceCalculator = ruleset.CreatePerformanceCalculator();
                if (performanceCalculator == null)
                    return new StarDifficulty(difficulty, new PerformanceAttributes());

                ScoreProcessor scoreProcessor = ruleset.CreateScoreProcessor();
                scoreProcessor.Mods.Value = key.OrderedMods;
                scoreProcessor.ApplyBeatmap(playableBeatmap);
                cancellationToken.ThrowIfCancellationRequested();

                ScoreInfo perfectScore = new ScoreInfo(key.BeatmapInfo, ruleset.RulesetInfo)
                {
                    Passed = true,
                    Accuracy = 1,
                    Mods = key.OrderedMods,
                    MaxCombo = scoreProcessor.MaximumCombo,
                    Combo = scoreProcessor.MaximumCombo,
                    TotalScore = scoreProcessor.MaximumTotalScore,
                    Statistics = scoreProcessor.MaximumStatistics,
                    MaximumStatistics = scoreProcessor.MaximumStatistics,
                };

                var performance = performanceCalculator.Calculate(perfectScore, difficulty);
                cancellationToken.ThrowIfCancellationRequested();

                return new StarDifficulty(difficulty, performance);
            }
            catch (OperationCanceledException)
            {
                // no need to log, cancellations are expected as part of normal operation.
                return null;
            }
            catch (BeatmapInvalidForRulesetException invalidForRuleset)
            {
                if (rulesetInfo.Equals(beatmapInfo.Ruleset))
                    Logger.Error(
                        invalidForRuleset,
                        $"Failed to convert {beatmapInfo.OnlineID} to the beatmap's default ruleset ({beatmapInfo.Ruleset})."
                    );

                return null;
            }
            catch (Exception unknownException)
            {
                Logger.Error(unknownException, "Failed to calculate beatmap difficulty");

                return null;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            modSettingChangeTracker?.Dispose();

            cancelTrackedBindableUpdate();
            updateScheduler.Dispose();
        }

        public readonly struct DifficultyCacheLookup : IEquatable<DifficultyCacheLookup>
        {
            public readonly BeatmapInfo BeatmapInfo;
            public readonly RulesetInfo Ruleset;
            public readonly Mod[] OrderedMods;

            public DifficultyCacheLookup(
                BeatmapInfo beatmapInfo,
                RulesetInfo? ruleset,
                IEnumerable<Mod>? mods
            )
            {
                BeatmapInfo = beatmapInfo;
                // In the case that the user hasn't given us a ruleset, use the beatmap's default ruleset.
                Ruleset = ruleset ?? BeatmapInfo.Ruleset;
                OrderedMods =
                    mods?.OrderBy(m => m.Acronym).Select(mod => mod.DeepClone()).ToArray()
                    ?? Array.Empty<Mod>();
            }

            public bool Equals(DifficultyCacheLookup other) =>
                BeatmapInfo.Equals(other.BeatmapInfo)
                && Ruleset.Equals(other.Ruleset)
                && OrderedMods.SequenceEqual(other.OrderedMods);

            public override int GetHashCode()
            {
                var hashCode = new HashCode();

                hashCode.Add(BeatmapInfo.ID);
                hashCode.Add(Ruleset.ShortName);

                foreach (var mod in OrderedMods)
                    hashCode.Add(mod);

                return hashCode.ToHashCode();
            }
        }

        private class BindableStarDifficulty : Bindable<StarDifficulty>
        {
            public readonly IBeatmapInfo BeatmapInfo;
            public readonly CancellationToken CancellationToken;

            public BindableStarDifficulty(
                IBeatmapInfo beatmapInfo,
                CancellationToken cancellationToken
            )
            {
                BeatmapInfo = beatmapInfo;
                CancellationToken = cancellationToken;
            }
        }

        /// <summary>
        /// A working beatmap that caches its playable representation.
        /// This is intended as single-use for when it is guaranteed that the playable beatmap can be reused.
        /// </summary>
        private class PlayableCachedWorkingBeatmap : IWorkingBeatmap
        {
            private readonly IWorkingBeatmap working;
            private IBeatmap? playable;

            public PlayableCachedWorkingBeatmap(IWorkingBeatmap working)
            {
                this.working = working;
            }

            public IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods) =>
                playable ??= working.GetPlayableBeatmap(ruleset, mods);

            public IBeatmap GetPlayableBeatmap(
                IRulesetInfo ruleset,
                IReadOnlyList<Mod> mods,
                CancellationToken cancellationToken
            ) => playable ??= working.GetPlayableBeatmap(ruleset, mods, cancellationToken);

            IBeatmapInfo IWorkingBeatmap.BeatmapInfo => working.BeatmapInfo;
            bool IWorkingBeatmap.BeatmapLoaded => working.BeatmapLoaded;
            bool IWorkingBeatmap.TrackLoaded => working.TrackLoaded;
            IBeatmap IWorkingBeatmap.Beatmap => working.Beatmap;

            Texture IWorkingBeatmap.GetBackground() => working.GetBackground();

            Texture IWorkingBeatmap.GetPanelBackground() => working.GetPanelBackground();

            Waveform IWorkingBeatmap.Waveform => working.Waveform;
            Storyboard IWorkingBeatmap.Storyboard => working.Storyboard;
            ISkin IWorkingBeatmap.Skin => working.Skin;
            Track IWorkingBeatmap.Track => working.Track;

            Track IWorkingBeatmap.LoadTrack() => working.LoadTrack();

            Stream IWorkingBeatmap.GetStream(string storagePath) => working.GetStream(storagePath);

            void IWorkingBeatmap.BeginAsyncLoad() => working.BeginAsyncLoad();

            void IWorkingBeatmap.CancelAsyncLoad() => working.CancelAsyncLoad();

            void IWorkingBeatmap.PrepareTrackForPreview(
                bool looping,
                double offsetFromPreviewPoint
            ) => working.PrepareTrackForPreview(looping, offsetFromPreviewPoint);
        }
    }
}
