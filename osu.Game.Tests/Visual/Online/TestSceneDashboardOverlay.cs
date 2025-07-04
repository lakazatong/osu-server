﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Online
{
    public partial class TestSceneDashboardOverlay : OsuTestScene
    {
        private readonly DashboardOverlay overlay;

        public TestSceneDashboardOverlay()
        {
            Add(overlay = new DashboardOverlay());
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            int supportLevel = 0;

            for (int i = 0; i < 1000; i++)
            {
                supportLevel++;

                if (supportLevel > 3)
                    supportLevel = 0;

                ((DummyAPIAccess)API).Friends.Add(
                    new APIRelation
                    {
                        TargetID = 2,
                        RelationType = RelationType.Friend,
                        Mutual = true,
                        TargetUser = new APIUser
                        {
                            Username = @"peppy",
                            Id = 2,
                            Colour = "99EB47",
                            CoverUrl = TestResources.COVER_IMAGE_3,
                            IsSupporter = supportLevel > 0,
                            SupportLevel = supportLevel,
                        },
                    }
                );
            }
        }

        [Test]
        public void TestShow()
        {
            AddStep("Show", overlay.Show);
        }

        [Test]
        public void TestHide()
        {
            AddStep("Hide", overlay.Hide);
        }
    }
}
