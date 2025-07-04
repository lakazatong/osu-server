﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile.Sections.Historical;

namespace osu.Game.Tests.Visual.Online
{
    public partial class TestSceneUserHistoryGraph : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(
            OverlayColourScheme.Pink
        );

        public TestSceneUserHistoryGraph()
        {
            UserHistoryGraph graph;

            Add(
                graph = new UserHistoryGraph("Test")
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 200,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            );

            var values = new[]
            {
                new APIUserHistoryCount { Date = new DateTime(2000, 1, 1), Count = 10 },
                new APIUserHistoryCount { Date = new DateTime(2000, 2, 1), Count = 20 },
                new APIUserHistoryCount { Date = new DateTime(2000, 3, 1), Count = 100 },
                new APIUserHistoryCount { Date = new DateTime(2000, 4, 1), Count = 15 },
                new APIUserHistoryCount { Date = new DateTime(2000, 5, 1), Count = 30 },
            };

            var moreValues = new[]
            {
                new APIUserHistoryCount { Date = new DateTime(2010, 5, 1), Count = 1000 },
                new APIUserHistoryCount { Date = new DateTime(2010, 6, 1), Count = 20 },
                new APIUserHistoryCount { Date = new DateTime(2010, 7, 1), Count = 20000 },
                new APIUserHistoryCount { Date = new DateTime(2010, 8, 1), Count = 30 },
                new APIUserHistoryCount { Date = new DateTime(2010, 9, 1), Count = 50 },
                new APIUserHistoryCount { Date = new DateTime(2010, 10, 1), Count = 2000 },
                new APIUserHistoryCount { Date = new DateTime(2010, 11, 1), Count = 2100 },
            };

            AddStep("Set fake values", () => graph.Values = values);
            AddStep("Set more values", () => graph.Values = moreValues);
            AddStep("Set null values", () => graph.Values = null);
            AddStep("Set empty values", () => graph.Values = Array.Empty<APIUserHistoryCount>());
        }
    }
}
