﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Replays.Types;

namespace osu.Game.Rulesets.Taiko.Replays
{
    public class TaikoReplayFrame : ReplayFrame, IConvertibleReplayFrame
    {
        public List<TaikoAction> Actions = new List<TaikoAction>();

        public TaikoReplayFrame() { }

        public TaikoReplayFrame(double time, params TaikoAction[] actions)
            : base(time)
        {
            Actions.AddRange(actions);
        }

        public void FromLegacy(
            LegacyReplayFrame currentFrame,
            IBeatmap beatmap,
            ReplayFrame? lastFrame = null
        )
        {
            if (currentFrame.MouseRight1)
                Actions.Add(TaikoAction.LeftRim);
            if (currentFrame.MouseRight2)
                Actions.Add(TaikoAction.RightRim);
            if (currentFrame.MouseLeft1)
                Actions.Add(TaikoAction.LeftCentre);
            if (currentFrame.MouseLeft2)
                Actions.Add(TaikoAction.RightCentre);
        }

        public LegacyReplayFrame ToLegacy(IBeatmap beatmap)
        {
            ReplayButtonState state = ReplayButtonState.None;

            if (Actions.Contains(TaikoAction.LeftRim))
                state |= ReplayButtonState.Right1;
            if (Actions.Contains(TaikoAction.RightRim))
                state |= ReplayButtonState.Right2;
            if (Actions.Contains(TaikoAction.LeftCentre))
                state |= ReplayButtonState.Left1;
            if (Actions.Contains(TaikoAction.RightCentre))
                state |= ReplayButtonState.Left2;

            return new LegacyReplayFrame(Time, null, null, state);
        }

        public override bool IsEquivalentTo(ReplayFrame other) =>
            other is TaikoReplayFrame taikoFrame
            && Time == taikoFrame.Time
            && Actions.SequenceEqual(taikoFrame.Actions);
    }
}
