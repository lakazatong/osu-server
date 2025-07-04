﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.Mods
{
    public class ManiaModMirror : ModMirror, IApplicableToBeatmap
    {
        public override LocalisableString Description => "Notes are flipped horizontally.";
        public override bool Ranked => UsesDefaultConfiguration;

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            int availableColumns = ((ManiaBeatmap)beatmap).TotalColumns;

            beatmap
                .HitObjects.OfType<ManiaHitObject>()
                .ForEach(h => h.Column = availableColumns - 1 - h.Column);
        }
    }
}
