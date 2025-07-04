﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterface;
using osuTK;

namespace osu.Game.Tests.Visual.Online
{
    [TestFixture]
    public partial class TestSceneGraph : OsuTestScene
    {
        public TestSceneGraph()
        {
            BarGraph graph;

            Child = graph = new BarGraph
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(0.5f),
            };

            AddStep(
                "values from 1-10",
                () => graph.Values = Enumerable.Range(1, 10).Select(i => (float)i)
            );
            AddStep(
                "small values",
                () =>
                    graph.Values = Enumerable
                        .Range(1, 10)
                        .Select(i => i * 0.01f)
                        .Concat(new[] { 100f })
            );
            AddStep(
                "values from 1-100",
                () => graph.Values = Enumerable.Range(1, 100).Select(i => (float)i)
            );
            AddStep(
                "reversed values from 1-10",
                () => graph.Values = Enumerable.Range(1, 10).Reverse().Select(i => (float)i)
            );
            AddStep("empty values", () => graph.Values = Array.Empty<float>());
            AddStep("Bottom to top", () => graph.Direction = BarDirection.BottomToTop);
            AddStep("Top to bottom", () => graph.Direction = BarDirection.TopToBottom);
            AddStep("Left to right", () => graph.Direction = BarDirection.LeftToRight);
            AddStep("Right to left", () => graph.Direction = BarDirection.RightToLeft);

            AddToggleStep(
                "Toggle movement",
                enabled =>
                {
                    if (enabled)
                        graph.MoveToY(-10, 1000).Then().MoveToY(10, 1000).Loop();
                    else
                        graph.ClearTransforms();
                }
            );
        }
    }
}
