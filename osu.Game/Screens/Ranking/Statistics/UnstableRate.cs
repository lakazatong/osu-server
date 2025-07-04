﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Screens.Ranking.Statistics
{
    /// <summary>
    /// Displays the unstable rate statistic for a given play.
    /// </summary>
    public partial class UnstableRate : SimpleStatisticItem<double?>
    {
        /// <summary>
        /// Creates and computes an <see cref="UnstableRate"/> statistic.
        /// </summary>
        /// <param name="hitEvents">Sequence of <see cref="HitEvent"/>s to calculate the unstable rate based on.</param>
        public UnstableRate(IReadOnlyList<HitEvent> hitEvents)
            : base("Unstable Rate")
        {
            Value = hitEvents.CalculateUnstableRate()?.Result;
        }

        protected override string DisplayValue(double? value) =>
            value?.ToString(@"N2") ?? "(not available)";
    }
}
