﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Input.Bindings;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osu.Game.Screens.Play.PlayerSettings;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.Select.Leaderboards;
using osu.Game.Users;

namespace osu.Game.Screens.Play
{
    [Cached]
    public partial class ReplayPlayer : Player, IKeyBindingHandler<GlobalAction>
    {
        public const double BASE_SEEK_AMOUNT = 1000;

        private readonly Func<IBeatmap, IReadOnlyList<Mod>, Score> createScore;

        private readonly bool replayIsFailedScore;

        private PlaybackSettings playbackSettings;

        [Cached(typeof(IGameplayLeaderboardProvider))]
        private readonly SoloGameplayLeaderboardProvider leaderboardProvider =
            new SoloGameplayLeaderboardProvider();

        protected override UserActivity InitialActivity =>
            new UserActivity.WatchingReplay(Score.ScoreInfo);

        private bool isAutoplayPlayback => GameplayState.Mods.OfType<ModAutoplay>().Any();

        // Disallow replays from failing. (see https://github.com/ppy/osu/issues/6108)
        protected override bool CheckModsAllowFailure()
        {
            if (!replayIsFailedScore && !isAutoplayPlayback)
                return false;

            return base.CheckModsAllowFailure();
        }

        public ReplayPlayer(Score score, PlayerConfiguration configuration = null)
            : this((_, _) => score, configuration)
        {
            replayIsFailedScore = score.ScoreInfo.Rank == ScoreRank.F;
        }

        public ReplayPlayer(
            Func<IBeatmap, IReadOnlyList<Mod>, Score> createScore,
            PlayerConfiguration configuration = null
        )
            : base(configuration)
        {
            this.createScore = createScore;
            Configuration.ShowLeaderboard = true;
        }

        /// <summary>
        /// Add a settings group to the HUD overlay. Intended to be used by rulesets to add replay-specific settings.
        /// </summary>
        /// <param name="settings">The settings group to be shown.</param>
        public void AddSettings(PlayerSettingsGroup settings) =>
            Schedule(() =>
            {
                settings.Expanded.Value = false;
                HUDOverlay.PlayerSettingsOverlay.Add(settings);
            });

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            if (!LoadedBeatmapSuccessfully)
                return;

            AddInternal(leaderboardProvider);

            playbackSettings = new PlaybackSettings
            {
                Depth = float.MaxValue,
                Expanded =
                {
                    BindTarget = config.GetBindable<bool>(
                        OsuSetting.ReplayPlaybackControlsExpanded
                    ),
                },
            };

            if (GameplayClockContainer is MasterGameplayClockContainer master)
                playbackSettings.UserPlaybackRate.BindTo(master.UserPlaybackRate);

            HUDOverlay.PlayerSettingsOverlay.AddAtStart(playbackSettings);
        }

        protected override void PrepareReplay()
        {
            DrawableRuleset?.SetReplayScore(Score);
        }

        protected override Score CreateScore(IBeatmap beatmap) => createScore(beatmap, Mods.Value);

        // Don't re-import replay scores as they're already present in the database.
        protected override Task ImportScore(Score score) => Task.CompletedTask;

        protected override ResultsScreen CreateResults(ScoreInfo score) =>
            new SoloResultsScreen(score)
            {
                // Only show the relevant button otherwise things look silly.
                AllowWatchingReplay = !isAutoplayPlayback,
                AllowRetry = isAutoplayPlayback,
            };

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.StepReplayBackward:
                    StepFrame(-1);
                    return true;

                case GlobalAction.StepReplayForward:
                    StepFrame(1);
                    return true;

                case GlobalAction.SeekReplayBackward:
                    SeekInDirection(-5 * (float)playbackSettings.UserPlaybackRate.Value);
                    return true;

                case GlobalAction.SeekReplayForward:
                    SeekInDirection(5 * (float)playbackSettings.UserPlaybackRate.Value);
                    return true;

                case GlobalAction.TogglePauseReplay:
                    if (GameplayClockContainer.IsPaused.Value)
                        GameplayClockContainer.Start();
                    else
                        GameplayClockContainer.Stop();
                    return true;
            }

            return false;
        }

        public void StepFrame(int direction)
        {
            GameplayClockContainer.Stop();

            var frames = GameplayState.Score.Replay.Frames;

            if (frames.Count == 0)
                return;

            GameplayClockContainer.Seek(
                direction < 0
                    ? (
                        frames.LastOrDefault(f => f.Time < GameplayClockContainer.CurrentTime)
                        ?? frames.First()
                    ).Time
                    : (
                        frames.FirstOrDefault(f => f.Time > GameplayClockContainer.CurrentTime)
                        ?? frames.Last()
                    ).Time
            );
        }

        public void SeekInDirection(float amount)
        {
            double target = Math.Clamp(
                GameplayClockContainer.CurrentTime + amount * BASE_SEEK_AMOUNT,
                0,
                GameplayState.Beatmap.GetLastObjectTime()
            );

            Seek(target);
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e) { }
    }
}
