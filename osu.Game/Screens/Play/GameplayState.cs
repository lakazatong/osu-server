// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Storyboards;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// The state of an active gameplay session, generally constructed and exposed by <see cref="Player"/>.
    /// </summary>
    public class GameplayState
    {
        /// <summary>
        /// The final post-convert post-mod-application beatmap.
        /// </summary>
        public readonly IBeatmap Beatmap;

        /// <summary>
        /// The ruleset used in gameplay.
        /// </summary>
        public readonly Ruleset Ruleset;

        /// <summary>
        /// The mods applied to the gameplay.
        /// </summary>
        public readonly IReadOnlyList<Mod> Mods;

        /// <summary>
        /// The gameplay score.
        /// </summary>
        public readonly Score Score;

        public readonly ScoreProcessor ScoreProcessor;
        public readonly HealthProcessor HealthProcessor;

        /// <summary>
        /// The storyboard associated with the beatmap.
        /// </summary>
        public readonly Storyboard Storyboard;

        /// <summary>
        /// Whether gameplay completed without the user failing.
        /// </summary>
        public bool HasPassed { get; set; }

        /// <summary>
        /// Whether the user failed during gameplay. This is only set when the gameplay session has completed due to the fail.
        /// </summary>
        public bool HasFailed { get; set; }

        /// <summary>
        /// Whether the user quit gameplay without having either passed or failed.
        /// </summary>
        public bool HasQuit { get; set; }

        public bool HasCompleted => HasPassed || HasFailed || HasQuit;

        /// <summary>
        /// A bindable tracking the last judgement result applied to any hit object.
        /// </summary>
        public IBindable<JudgementResult> LastJudgementResult => lastJudgementResult;

        private readonly Bindable<JudgementResult> lastJudgementResult =
            new Bindable<JudgementResult>();

        /// <summary>
        /// The local user's playing state (whether actively playing, paused, or not playing due to watching a replay or similar).
        /// </summary>
        public IBindable<LocalUserPlayingState> PlayingState { get; } =
            new Bindable<LocalUserPlayingState>();

        public GameplayState(
            IBeatmap beatmap,
            Ruleset ruleset,
            IReadOnlyList<Mod>? mods = null,
            Score? score = null,
            ScoreProcessor? scoreProcessor = null,
            HealthProcessor? healthProcessor = null,
            Storyboard? storyboard = null,
            IBindable<LocalUserPlayingState>? localUserPlayingState = null
        )
        {
            Beatmap = beatmap;
            Ruleset = ruleset;
            Score =
                score
                ?? new Score
                {
                    ScoreInfo =
                    {
                        BeatmapInfo = beatmap.BeatmapInfo,
                        Ruleset = ruleset.RulesetInfo,
                    },
                };
            Mods = mods ?? Array.Empty<Mod>();
            ScoreProcessor = scoreProcessor ?? ruleset.CreateScoreProcessor();
            HealthProcessor =
                healthProcessor ?? ruleset.CreateHealthProcessor(beatmap.HitObjects[0].StartTime);
            Storyboard = storyboard ?? new Storyboard();

            if (localUserPlayingState != null)
                PlayingState.BindTo(localUserPlayingState);
        }

        /// <summary>
        /// Applies the score change of a <see cref="JudgementResult"/> to this <see cref="GameplayState"/>.
        /// </summary>
        /// <param name="result">The <see cref="JudgementResult"/> to apply.</param>
        public void ApplyResult(JudgementResult result) => lastJudgementResult.Value = result;
    }
}
