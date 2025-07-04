﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Pippidon.Beatmaps;
using osu.Game.Rulesets.Pippidon.Mods;
using osu.Game.Rulesets.Pippidon.UI;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Pippidon
{
    public class PippidonRuleset : Ruleset
    {
        public override string Description => "gather the osu!coins";

        public override DrawableRuleset CreateDrawableRulesetWith(
            IBeatmap beatmap,
            IReadOnlyList<Mod> mods = null
        ) => new DrawablePippidonRuleset(this, beatmap, mods);

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) =>
            new PippidonBeatmapConverter(beatmap, this);

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) =>
            new PippidonDifficultyCalculator(RulesetInfo, beatmap);

        public override IEnumerable<Mod> GetModsFor(ModType type)
        {
            switch (type)
            {
                case ModType.Automation:
                    return new[] { new PippidonModAutoplay() };

                default:
                    return Array.Empty<Mod>();
            }
        }

        public override string ShortName => "pippidon";

        public override IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0) =>
            new[]
            {
                new KeyBinding(InputKey.W, PippidonAction.MoveUp),
                new KeyBinding(InputKey.S, PippidonAction.MoveDown),
            };

        public override Drawable CreateIcon() => new PippidonRulesetIcon(this);

        // Leave this line intact. It will bake the correct version into the ruleset on each build/release.
        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;
    }
}
