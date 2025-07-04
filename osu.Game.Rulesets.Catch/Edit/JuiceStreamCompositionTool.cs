// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Edit.Blueprints;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;

namespace osu.Game.Rulesets.Catch.Edit
{
    public class JuiceStreamCompositionTool : CompositionTool
    {
        public JuiceStreamCompositionTool()
            : base(nameof(JuiceStream)) { }

        public override Drawable CreateIcon() =>
            new BeatmapStatisticIcon(BeatmapStatisticsIconType.Sliders);

        public override HitObjectPlacementBlueprint CreatePlacementBlueprint() =>
            new JuiceStreamPlacementBlueprint();
    }
}
