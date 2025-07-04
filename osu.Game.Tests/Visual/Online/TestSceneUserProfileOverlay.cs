﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets.Taiko;
using osu.Game.Tests.Resources;
using osu.Game.Users;

namespace osu.Game.Tests.Visual.Online
{
    [TestFixture]
    public partial class TestSceneUserProfileOverlay : OsuTestScene
    {
        private DummyAPIAccess dummyAPI => (DummyAPIAccess)API;

        private UserProfileOverlay profile = null!;

        [SetUpSteps]
        public void SetUp()
        {
            AddStep(
                "create profile overlay",
                () =>
                {
                    profile = new UserProfileOverlay();

                    Child = new DependencyProvidingContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        CachedDependencies = new (Type, object)[]
                        {
                            (typeof(UserProfileOverlay), profile),
                        },
                        Child = profile,
                    };
                }
            );
        }

        [Test]
        public void TestBlank()
        {
            AddStep("show overlay", () => profile.Show());
        }

        [Test]
        public void TestActualUser()
        {
            AddStep(
                "set up request handling",
                () =>
                {
                    dummyAPI.HandleRequest = req =>
                    {
                        if (req is GetUserRequest getUserRequest)
                        {
                            getUserRequest.TriggerSuccess(TEST_USER);
                            return true;
                        }

                        if (req is GetUserBeatmapsRequest getUserBeatmapsRequest)
                        {
                            getUserBeatmapsRequest.TriggerSuccess(
                                new List<APIBeatmapSet>
                                {
                                    CreateAPIBeatmapSet(),
                                    CreateAPIBeatmapSet(),
                                }
                            );
                            return true;
                        }

                        return false;
                    };
                }
            );
            AddStep("show user", () => profile.ShowUser(new APIUser { Id = 1 }));
            AddToggleStep(
                "toggle visibility",
                visible => profile.State.Value = visible ? Visibility.Visible : Visibility.Hidden
            );
            AddStep("log out", () => dummyAPI.Logout());
            AddStep(
                "log back in",
                () =>
                {
                    dummyAPI.Login("username", "password");
                    dummyAPI.AuthenticateSecondFactor("abcdefgh");
                }
            );
        }

        [Test]
        public void TestLoading()
        {
            GetUserRequest pendingRequest = null!;

            AddStep(
                "set up request handling",
                () =>
                {
                    dummyAPI.HandleRequest = req =>
                    {
                        if (req is GetUserRequest getUserRequest)
                        {
                            pendingRequest = getUserRequest;
                            return true;
                        }

                        return false;
                    };
                }
            );
            AddStep("show user", () => profile.ShowUser(new APIUser { Id = 1 }));
            AddWaitStep("wait some", 3);
            AddStep("complete request", () => pendingRequest.TriggerSuccess(TEST_USER));
        }

        [Test]
        public void TestLogin()
        {
            GetUserRequest pendingRequest = null!;

            AddStep(
                "set up request handling",
                () =>
                {
                    dummyAPI.HandleRequest = req =>
                    {
                        if (
                            dummyAPI.State.Value == APIState.Online
                            && req is GetUserRequest getUserRequest
                        )
                        {
                            pendingRequest = getUserRequest;
                            return true;
                        }

                        return false;
                    };
                }
            );
            AddStep("logout", () => dummyAPI.Logout());
            AddStep("show user", () => profile.ShowUser(new APIUser { Id = 1 }));
            AddStep(
                "login",
                () =>
                {
                    dummyAPI.Login("username", "password");
                    dummyAPI.AuthenticateSecondFactor("abcdefgh");
                }
            );
            AddWaitStep("wait some", 3);
            AddStep("complete request", () => pendingRequest.TriggerSuccess(TEST_USER));
        }

        [Test]
        public void TestCustomColourScheme()
        {
            int hue = 0;

            AddSliderStep("hue", 0, 360, 222, h => hue = h);

            AddStep(
                "set up request handling",
                () =>
                {
                    dummyAPI.HandleRequest = req =>
                    {
                        if (req is GetUserRequest getUserRequest)
                        {
                            getUserRequest.TriggerSuccess(
                                new APIUser
                                {
                                    Username = $"Colorful #{hue}",
                                    Id = 1,
                                    CountryCode = CountryCode.JP,
                                    CoverUrl = TestResources.COVER_IMAGE_2,
                                    ProfileHue = hue,
                                    PlayMode = "osu",
                                }
                            );
                            return true;
                        }

                        return false;
                    };
                }
            );

            AddStep("show user", () => profile.ShowUser(new APIUser { Id = 1 }));
        }

        [Test]
        public void TestCustomColourSchemeWithReload()
        {
            int hue = 0;
            GetUserRequest pendingRequest = null!;

            AddSliderStep("hue", 0, 360, 222, h => hue = h);

            AddStep(
                "set up request handling",
                () =>
                {
                    dummyAPI.HandleRequest = req =>
                    {
                        if (req is GetUserRequest getUserRequest)
                        {
                            pendingRequest = getUserRequest;
                            return true;
                        }

                        return false;
                    };
                }
            );

            AddStep("show user", () => profile.ShowUser(new APIUser { Id = 1 }));

            AddWaitStep("wait some", 3);
            AddStep(
                "complete request",
                () =>
                    pendingRequest.TriggerSuccess(
                        new APIUser
                        {
                            Username = $"Colorful #{hue}",
                            Id = 1,
                            CountryCode = CountryCode.JP,
                            CoverUrl = TestResources.COVER_IMAGE_2,
                            ProfileHue = hue,
                            PlayMode = "osu",
                        }
                    )
            );

            int hue2 = 0;

            AddSliderStep("hue 2", 0, 360, 50, h => hue2 = h);
            AddStep("show user", () => profile.ShowUser(new APIUser { Id = 2 }));
            AddWaitStep("wait some", 3);

            AddStep(
                "complete request",
                () =>
                    pendingRequest.TriggerSuccess(
                        new APIUser
                        {
                            Username = $"Colorful #{hue2}",
                            Id = 2,
                            CountryCode = CountryCode.JP,
                            CoverUrl = TestResources.COVER_IMAGE_2,
                            ProfileHue = hue2,
                            PlayMode = "osu",
                        }
                    )
            );

            AddStep(
                "show user different ruleset",
                () => profile.ShowUser(new APIUser { Id = 2 }, new TaikoRuleset().RulesetInfo)
            );
            AddWaitStep("wait some", 3);

            AddStep(
                "complete request",
                () =>
                    pendingRequest.TriggerSuccess(
                        new APIUser
                        {
                            Username = $"Colorful #{hue2}",
                            Id = 2,
                            CountryCode = CountryCode.JP,
                            CoverUrl = TestResources.COVER_IMAGE_2,
                            ProfileHue = hue2,
                            PlayMode = "osu",
                        }
                    )
            );
        }

        public static readonly APIUser TEST_USER = new APIUser
        {
            Username = @"Somebody",
            Id = 1,
            CountryCode = CountryCode.JP,
            CoverUrl = TestResources.COVER_IMAGE_1,
            JoinDate = DateTimeOffset.Now.AddDays(-1),
            LastVisit = DateTimeOffset.Now,
            Groups = new[]
            {
                new APIUserGroup
                {
                    Colour = "#EB47D0",
                    ShortName = "DEV",
                    Name = "Developers",
                },
                new APIUserGroup
                {
                    Colour = "#A347EB",
                    ShortName = "BN",
                    Name = "Beatmap Nominators",
                    Playmodes = new[] { "mania" },
                },
                new APIUserGroup
                {
                    Colour = "#A347EB",
                    ShortName = "BN",
                    Name = "Beatmap Nominators",
                    Playmodes = new[] { "osu", "taiko" },
                },
                new APIUserGroup
                {
                    Colour = "#A347EB",
                    ShortName = "BN",
                    Name = "Beatmap Nominators",
                    Playmodes = new[] { "osu", "taiko", "fruits", "mania" },
                },
                new APIUserGroup
                {
                    Colour = "#A347EB",
                    ShortName = "BN",
                    Name = "Beatmap Nominators (Probationary)",
                    Playmodes = new[] { "osu", "taiko", "fruits", "mania" },
                    IsProbationary = true,
                },
            },
            ProfileOrder = new[]
            {
                @"me",
                @"recent_activity",
                @"beatmaps",
                @"historical",
                @"kudosu",
                @"top_ranks",
                @"medals",
            },
            RankHighest = new APIUser.UserRankHighest { Rank = 1, UpdatedAt = DateTimeOffset.Now },
            Statistics = new UserStatistics
            {
                IsRanked = true,
                GlobalRank = 2148,
                CountryRank = 1,
                PP = 4567.89m,
                Level = new UserStatistics.LevelInfo { Current = 727, Progress = 69 },
                RankHistory = new APIRankHistory
                {
                    Mode = @"osu",
                    Data = Enumerable.Range(2345, 45).Concat(Enumerable.Range(2109, 40)).ToArray(),
                },
            },
            TournamentBanners = new[]
            {
                new TournamentBanner
                {
                    Id = 15588,
                    TournamentId = 41,
                    ImageLowRes =
                        "https://assets.ppy.sh/tournament-banners/official/owc2023/profile/supporter_CN.jpg",
                    Image =
                        "https://assets.ppy.sh/tournament-banners/official/owc2023/profile/supporter_CN@2x.jpg",
                },
                new TournamentBanner
                {
                    Id = 15589,
                    TournamentId = 41,
                    ImageLowRes =
                        "https://assets.ppy.sh/tournament-banners/official/owc2023/profile/supporter_PH.jpg",
                    Image =
                        "https://assets.ppy.sh/tournament-banners/official/owc2023/profile/supporter_PH@2x.jpg",
                },
                new TournamentBanner
                {
                    Id = 15590,
                    TournamentId = 41,
                    ImageLowRes =
                        "https://assets.ppy.sh/tournament-banners/official/owc2023/profile/supporter_CL.jpg",
                    Image =
                        "https://assets.ppy.sh/tournament-banners/official/owc2023/profile/supporter_CL@2x.jpg",
                },
            },
            Badges = new[]
            {
                new Badge
                {
                    AwardedAt = DateTimeOffset.FromUnixTimeSeconds(1505741569),
                    Description = "Outstanding help by being a voluntary test subject.",
                    ImageUrl = "https://assets.ppy.sh/profile-badges/contributor-new@2x.png",
                    ImageUrlLowRes = "https://assets.ppy.sh/profile-badges/contributor-new.png",
                    Url = "https://osu.ppy.sh/wiki/en/People/Community_Contributors",
                },
                new Badge
                {
                    AwardedAt = DateTimeOffset.FromUnixTimeSeconds(1505741569),
                    Description = "Badge without a url.",
                    ImageUrl = "https://assets.ppy.sh/profile-badges/contributor@2x.png",
                    ImageUrlLowRes = "https://assets.ppy.sh/profile-badges/contributor.png",
                },
            },
            DailyChallengeStatistics = new APIUserDailyChallengeStatistics
            {
                DailyStreakCurrent = 231,
                WeeklyStreakCurrent = 18,
                DailyStreakBest = 370,
                WeeklyStreakBest = 51,
                Top10PercentPlacements = 345,
                Top50PercentPlacements = 427,
            },
            Title = "osu!volunteer",
            Colour = "ff0000",
            Achievements = Array.Empty<APIUserAchievement>(),
            PlayMode = "osu",
            Kudosu = new APIUser.KudosuCount { Available = 10, Total = 50 },
            SupportLevel = 2,
            Location = "Somewhere",
            Interests = "Rhythm games",
            Occupation = "Gamer",
            Twitter = "test_user",
            Discord = "test_user",
            Website = "https://google.com",
            Team = new APITeam
            {
                Id = 1,
                Name = "Collective Wangs",
                ShortName = "WANG",
                FlagUrl = "https://assets.ppy.sh/teams/flag/1/wanglogo.jpg",
            },
        };
    }
}
