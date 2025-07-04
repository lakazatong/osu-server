// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    public partial class TestSceneOsuModHidden : OsuModTestScene
    {
        [Test]
        public void TestDefaultBeatmapTest() =>
            CreateModTest(
                new ModTestData
                {
                    Mod = new TestOsuModHidden(),
                    Autoplay = true,
                    PassCondition = () =>
                        checkSomeHit() && objectWithIncreasedVisibilityHasIndex(0),
                }
            );

        [Test]
        public void FirstCircleAfterTwoSpinners() =>
            CreateModTest(
                new ModTestData
                {
                    Mod = new TestOsuModHidden(),
                    Autoplay = true,
                    CreateBeatmap = () =>
                        new Beatmap
                        {
                            HitObjects = new List<HitObject>
                            {
                                new Spinner { Position = new Vector2(256, 192), EndTime = 1000 },
                                new Spinner
                                {
                                    Position = new Vector2(256, 192),
                                    StartTime = 1200,
                                    EndTime = 2200,
                                },
                                new HitCircle
                                {
                                    Position = new Vector2(300, 192),
                                    StartTime = 3200,
                                },
                                new HitCircle
                                {
                                    Position = new Vector2(384, 192),
                                    StartTime = 4200,
                                },
                            },
                        },
                    PassCondition = () =>
                        checkSomeHit() && objectWithIncreasedVisibilityHasIndex(2),
                }
            );

        [Test]
        public void FirstSliderAfterTwoSpinners() =>
            CreateModTest(
                new ModTestData
                {
                    Mod = new TestOsuModHidden(),
                    Autoplay = true,
                    CreateBeatmap = () =>
                        new Beatmap
                        {
                            HitObjects = new List<HitObject>
                            {
                                new Spinner { Position = new Vector2(256, 192), EndTime = 1000 },
                                new Spinner
                                {
                                    Position = new Vector2(256, 192),
                                    StartTime = 1200,
                                    EndTime = 2200,
                                },
                                new Slider
                                {
                                    StartTime = 3200,
                                    Path = new SliderPath(
                                        PathType.LINEAR,
                                        new[] { Vector2.Zero, new Vector2(100, 0) }
                                    ),
                                },
                                new Slider
                                {
                                    StartTime = 5200,
                                    Path = new SliderPath(
                                        PathType.LINEAR,
                                        new[] { Vector2.Zero, new Vector2(100, 0) }
                                    ),
                                },
                            },
                        },
                    PassCondition = () =>
                        checkSomeHit() && objectWithIncreasedVisibilityHasIndex(2),
                }
            );

        [Test]
        public void TestWithSliderReuse() =>
            CreateModTest(
                new ModTestData
                {
                    Mod = new TestOsuModHidden(),
                    Autoplay = true,
                    CreateBeatmap = () =>
                        new Beatmap
                        {
                            HitObjects = new List<HitObject>
                            {
                                new Slider
                                {
                                    StartTime = 1000,
                                    Path = new SliderPath(
                                        PathType.LINEAR,
                                        new[] { Vector2.Zero, new Vector2(100, 0) }
                                    ),
                                },
                                new Slider
                                {
                                    StartTime = 4000,
                                    Path = new SliderPath(
                                        PathType.LINEAR,
                                        new[] { Vector2.Zero, new Vector2(100, 0) }
                                    ),
                                },
                            },
                        },
                    PassCondition = checkSomeHit,
                }
            );

        [Test]
        public void TestApproachCirclesOnly() =>
            CreateModTest(
                new ModTestData
                {
                    Mod = new OsuModHidden { OnlyFadeApproachCircles = { Value = true } },
                    Autoplay = true,
                    CreateBeatmap = () =>
                        new Beatmap
                        {
                            HitObjects = new List<HitObject>
                            {
                                new HitCircle
                                {
                                    StartTime = 1000,
                                    Position = new Vector2(206, 142),
                                },
                                new HitCircle
                                {
                                    StartTime = 2000,
                                    Position = new Vector2(306, 142),
                                },
                                new Slider
                                {
                                    StartTime = 3000,
                                    Position = new Vector2(156, 242),
                                    Path = new SliderPath(
                                        PathType.LINEAR,
                                        new[] { Vector2.Zero, new Vector2(200, 0) }
                                    ),
                                },
                                new Spinner
                                {
                                    Position = new Vector2(256, 192),
                                    StartTime = 7000,
                                    EndTime = 9000,
                                },
                            },
                        },
                    PassCondition = checkSomeHit,
                }
            );

        private bool checkSomeHit() => Player.ScoreProcessor.JudgedHits >= 4;

        private bool objectWithIncreasedVisibilityHasIndex(int index) =>
            Player.GameplayState.Mods.OfType<TestOsuModHidden>().Single().FirstObject
            == Player.GameplayState.Beatmap.HitObjects[index];

        private class TestOsuModHidden : OsuModHidden
        {
            public new HitObject? FirstObject => base.FirstObject;
        }
    }
}
