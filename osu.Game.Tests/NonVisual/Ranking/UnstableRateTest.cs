// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Utils;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Ranking.Statistics;

namespace osu.Game.Tests.NonVisual.Ranking
{
    [TestFixture]
    public class UnstableRateTest
    {
        [Test]
        public void TestDistributedHits()
        {
            var events = Enumerable
                .Range(-5, 11)
                .Select(t => new HitEvent(t - 5, 1.0, HitResult.Great, new HitObject(), null, null))
                .ToList();

            var unstableRate = new UnstableRate(events);

            Assert.IsNotNull(unstableRate.Value);
            Assert.AreEqual(unstableRate.Value.Value, 10 * Math.Sqrt(10), Precision.DOUBLE_EPSILON);
        }

        [Test]
        public void TestDistributedHitsIncrementalRewind()
        {
            var events = Enumerable
                .Range(-5, 11)
                .Select(t => new HitEvent(t - 5, 1.0, HitResult.Great, new HitObject(), null, null))
                .ToList();

            // Add some red herrings
            events.Insert(
                4,
                new HitEvent(
                    200,
                    1.0,
                    HitResult.Meh,
                    new HitObject { HitWindows = HitWindows.Empty },
                    null,
                    null
                )
            );
            events.Insert(8, new HitEvent(-100, 1.0, HitResult.Miss, new HitObject(), null, null));

            HitEventExtensions.UnstableRateCalculationResult result = null;

            for (int i = 0; i < events.Count; i++)
            {
                result = events.GetRange(0, i + 1).CalculateUnstableRate(result);
            }

            result = events.GetRange(0, 2).CalculateUnstableRate(result);

            Assert.IsNotNull(result!.Result);
            Assert.AreEqual(5, result.Result, Precision.DOUBLE_EPSILON);
        }

        [Test]
        public void TestDistributedHitsIncremental()
        {
            var events = Enumerable
                .Range(-5, 11)
                .Select(t => new HitEvent(t - 5, 1.0, HitResult.Great, new HitObject(), null, null))
                .ToList();

            // Add some red herrings
            events.Insert(
                4,
                new HitEvent(
                    200,
                    1.0,
                    HitResult.Meh,
                    new HitObject { HitWindows = HitWindows.Empty },
                    null,
                    null
                )
            );
            events.Insert(8, new HitEvent(-100, 1.0, HitResult.Miss, new HitObject(), null, null));

            HitEventExtensions.UnstableRateCalculationResult result = null;

            for (int i = 0; i < events.Count; i++)
            {
                result = events.GetRange(0, i + 1).CalculateUnstableRate(result);
            }

            Assert.IsNotNull(result!.Result);
            Assert.AreEqual(10 * Math.Sqrt(10), result.Result, Precision.DOUBLE_EPSILON);
        }

        [Test]
        public void TestMissesAndEmptyWindows()
        {
            var events = new[]
            {
                new HitEvent(-100, 1.0, HitResult.Miss, new HitObject(), null, null),
                new HitEvent(0, 1.0, HitResult.Great, new HitObject(), null, null),
                new HitEvent(
                    200,
                    1.0,
                    HitResult.Meh,
                    new HitObject { HitWindows = HitWindows.Empty },
                    null,
                    null
                ),
            };

            var unstableRate = new UnstableRate(events);

            Assert.AreEqual(0, unstableRate.Value);
        }

        [Test]
        public void TestStaticRateChange()
        {
            var events = new[]
            {
                new HitEvent(-150, 1.5, HitResult.Great, new HitObject(), null, null),
                new HitEvent(-150, 1.5, HitResult.Great, new HitObject(), null, null),
                new HitEvent(150, 1.5, HitResult.Great, new HitObject(), null, null),
                new HitEvent(150, 1.5, HitResult.Great, new HitObject(), null, null),
            };

            var unstableRate = new UnstableRate(events);

            Assert.AreEqual(10 * 100, unstableRate.Value);
        }

        [Test]
        public void TestDynamicRateChange()
        {
            var events = new[]
            {
                new HitEvent(-50, 0.5, HitResult.Great, new HitObject(), null, null),
                new HitEvent(75, 0.75, HitResult.Great, new HitObject(), null, null),
                new HitEvent(-100, 1.0, HitResult.Great, new HitObject(), null, null),
                new HitEvent(125, 1.25, HitResult.Great, new HitObject(), null, null),
            };

            var unstableRate = new UnstableRate(events);

            Assert.AreEqual(10 * 100, unstableRate.Value);
        }
    }
}
