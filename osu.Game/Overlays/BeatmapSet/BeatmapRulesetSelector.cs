﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;

namespace osu.Game.Overlays.BeatmapSet
{
    public partial class BeatmapRulesetSelector : OverlayRulesetSelector
    {
        private readonly Bindable<APIBeatmapSet?> beatmapSet = new Bindable<APIBeatmapSet?>();

        public APIBeatmapSet? BeatmapSet
        {
            get => beatmapSet.Value;
            set
            {
                // propagate value to tab items first to enable only available rulesets.
                beatmapSet.Value = value;

                Current.Value = TabContainer.TabItems.FirstOrDefault(t => t.Enabled.Value)?.Value;
            }
        }

        protected override TabItem<RulesetInfo> CreateTabItem(RulesetInfo value) =>
            new BeatmapRulesetTabItem(value) { BeatmapSet = { BindTarget = beatmapSet } };
    }
}
