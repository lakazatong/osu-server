﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Scoring;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Leaderboards;
using osu.Game.Tests.Resources;
using osu.Game.Users;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.SongSelect
{
    public partial class TestSceneBeatmapLeaderboard : OsuManualInputManagerTestScene
    {
        private readonly FailableLeaderboard leaderboard;

        [Cached(typeof(IDialogOverlay))]
        private readonly DialogOverlay dialogOverlay;

        private ScoreManager scoreManager = null!;
        private RulesetStore rulesetStore = null!;
        private BeatmapManager beatmapManager = null!;
        private PlaySongSelect songSelect = null!;

        private LeaderboardManager leaderboardManager = null!;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(
            IReadOnlyDependencyContainer parent
        )
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

            dependencies.Cache(rulesetStore = new RealmRulesetStore(Realm));
            dependencies.Cache(
                beatmapManager = new BeatmapManager(
                    LocalStorage,
                    Realm,
                    null,
                    dependencies.Get<AudioManager>(),
                    Resources,
                    dependencies.Get<GameHost>(),
                    Beatmap.Default
                )
            );
            dependencies.Cache(
                scoreManager = new ScoreManager(
                    rulesetStore,
                    () => beatmapManager,
                    LocalStorage,
                    Realm,
                    API
                )
            );
            dependencies.CacheAs<Screens.Select.SongSelect>(songSelect = new PlaySongSelect());
            dependencies.Cache(leaderboardManager = new LeaderboardManager());

            Dependencies.Cache(Realm);

            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            LoadComponent(songSelect);
            LoadComponent(leaderboardManager);
        }

        public TestSceneBeatmapLeaderboard()
        {
            Add(
                new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        dialogOverlay = new DialogOverlay { Depth = -1 },
                        leaderboard = new FailableLeaderboard
                        {
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Size = new Vector2(550f, 450f),
                            Scope = BeatmapLeaderboardScope.Global,
                        },
                    },
                }
            );
        }

        [Test]
        public void TestLocalScoresDisplay()
        {
            BeatmapInfo beatmapInfo = null!;

            AddStep(@"Set scope", () => leaderboard.Scope = BeatmapLeaderboardScope.Local);

            AddStep(
                @"Set beatmap",
                () =>
                {
                    beatmapManager
                        .Import(TestResources.GetQuickTestBeatmapForImport())
                        .WaitSafely();
                    beatmapInfo = beatmapManager.GetAllUsableBeatmapSets().First().Beatmaps.First();

                    leaderboard.BeatmapInfo = beatmapInfo;
                }
            );

            clearScores();
            checkDisplayedCount(0);

            importMoreScores(() => beatmapInfo);
            checkDisplayedCount(10);

            importMoreScores(() => beatmapInfo);
            checkDisplayedCount(20);

            clearScores();
            checkDisplayedCount(0);
        }

        [Test]
        public void TestLocalScoresDisplayWorksWhenStartingOffline()
        {
            BeatmapInfo beatmapInfo = null!;

            AddStep("Log out", () => API.Logout());
            AddStep(@"Set scope", () => leaderboard.Scope = BeatmapLeaderboardScope.Local);

            AddStep(
                @"Set beatmap",
                () =>
                {
                    beatmapManager
                        .Import(TestResources.GetQuickTestBeatmapForImport())
                        .WaitSafely();
                    beatmapInfo = beatmapManager.GetAllUsableBeatmapSets().First().Beatmaps.First();

                    leaderboard.BeatmapInfo = beatmapInfo;
                }
            );

            clearScores();
            importMoreScores(() => beatmapInfo);
            checkDisplayedCount(10);
        }

        [Test]
        public void TestLocalScoresDisplayOnBeatmapEdit()
        {
            BeatmapInfo beatmapInfo = null!;
            string originalHash = string.Empty;

            AddStep(@"Set scope", () => leaderboard.Scope = BeatmapLeaderboardScope.Local);

            AddStep(
                @"Import beatmap",
                () =>
                {
                    beatmapManager
                        .Import(TestResources.GetQuickTestBeatmapForImport())
                        .WaitSafely();
                    beatmapInfo = beatmapManager.GetAllUsableBeatmapSets().First().Beatmaps.First();

                    leaderboard.BeatmapInfo = beatmapInfo;
                }
            );

            clearScores();
            checkDisplayedCount(0);

            AddStep(
                @"Perform initial save to guarantee stable hash",
                () =>
                {
                    IBeatmap beatmap = beatmapManager.GetWorkingBeatmap(beatmapInfo).Beatmap;
                    beatmapManager.Save(beatmapInfo, beatmap);

                    originalHash = beatmapInfo.Hash;
                }
            );

            importMoreScores(() => beatmapInfo);

            checkDisplayedCount(10);
            checkStoredCount(10);

            AddStep(
                @"Save with changes",
                () =>
                {
                    IBeatmap beatmap = beatmapManager.GetWorkingBeatmap(beatmapInfo).Beatmap;
                    beatmap.Difficulty.ApproachRate = 12;
                    beatmapManager.Save(beatmapInfo, beatmap);
                }
            );

            AddAssert("Hash changed", () => beatmapInfo.Hash, () => Is.Not.EqualTo(originalHash));
            checkDisplayedCount(0);
            checkStoredCount(10);

            importMoreScores(() => beatmapInfo);
            importMoreScores(() => beatmapInfo);
            checkDisplayedCount(20);
            checkStoredCount(30);

            AddStep(
                @"Revert changes",
                () =>
                {
                    IBeatmap beatmap = beatmapManager.GetWorkingBeatmap(beatmapInfo).Beatmap;
                    beatmap.Difficulty.ApproachRate = 8;
                    beatmapManager.Save(beatmapInfo, beatmap);
                }
            );

            AddAssert("Hash restored", () => beatmapInfo.Hash, () => Is.EqualTo(originalHash));
            checkDisplayedCount(10);
            checkStoredCount(30);

            clearScores();
            checkDisplayedCount(0);
            checkStoredCount(0);
        }

        [Test]
        public void TestGlobalScoresDisplay()
        {
            AddStep(@"Set scope", () => leaderboard.Scope = BeatmapLeaderboardScope.Global);
            AddStep(
                @"New Scores",
                () => leaderboard.SetScores(GenerateSampleScores(new BeatmapInfo()))
            );
            AddStep(
                @"New Scores with teams",
                () =>
                    leaderboard.SetScores(
                        GenerateSampleScores(new BeatmapInfo())
                            .Select(s =>
                            {
                                s.User.Team = new APITeam();
                                return s;
                            })
                    )
            );
        }

        [Test]
        public void TestPersonalBest()
        {
            AddStep(@"Show personal best", showPersonalBest);
            AddStep("null personal best position", showPersonalBestWithNullPosition);
        }

        [Test]
        public void TestPlaceholderStates()
        {
            AddStep("ensure no scores displayed", () => leaderboard.SetScores(null));

            AddStep(
                @"Network failure",
                () => leaderboard.SetErrorState(LeaderboardState.NetworkFailure)
            );
            AddStep(@"No team", () => leaderboard.SetErrorState(LeaderboardState.NoTeam));
            AddStep(
                @"No supporter",
                () => leaderboard.SetErrorState(LeaderboardState.NotSupporter)
            );
            AddStep(
                @"Not logged in",
                () => leaderboard.SetErrorState(LeaderboardState.NotLoggedIn)
            );
            AddStep(
                @"Ruleset unavailable",
                () => leaderboard.SetErrorState(LeaderboardState.RulesetUnavailable)
            );
            AddStep(
                @"Beatmap unavailable",
                () => leaderboard.SetErrorState(LeaderboardState.BeatmapUnavailable)
            );
            AddStep(
                @"None selected",
                () => leaderboard.SetErrorState(LeaderboardState.NoneSelected)
            );
        }

        [Test]
        public void TestUseTheseModsDoesNotCopySystemMods()
        {
            AddStep(
                @"set scores",
                () =>
                    leaderboard.SetScores(
                        leaderboard.Scores,
                        new ScoreInfo
                        {
                            Position = 999,
                            Rank = ScoreRank.XH,
                            Accuracy = 1,
                            MaxCombo = 244,
                            TotalScore = 1707827,
                            Ruleset = new OsuRuleset().RulesetInfo,
                            Mods = new Mod[] { new OsuModHidden(), new ModScoreV2() },
                            User = new APIUser
                            {
                                Id = 6602580,
                                Username = @"waaiiru",
                                CountryCode = CountryCode.ES,
                            },
                        }
                    )
            );
            AddUntilStep(
                "wait for scores",
                () => this.ChildrenOfType<LeaderboardScore>().Count(),
                () => Is.GreaterThan(0)
            );
            AddStep(
                "right click panel",
                () =>
                {
                    InputManager.MoveMouseTo(this.ChildrenOfType<LeaderboardScore>().Single());
                    InputManager.Click(MouseButton.Right);
                }
            );
            AddStep(
                "click use these mods",
                () =>
                {
                    InputManager.MoveMouseTo(this.ChildrenOfType<DrawableOsuMenuItem>().Single());
                    InputManager.Click(MouseButton.Left);
                }
            );
            AddAssert(
                "song select received HD",
                () => songSelect.Mods.Value.Any(m => m is OsuModHidden)
            );
            AddAssert(
                "song select did not receive SV2",
                () => !songSelect.Mods.Value.Any(m => m is ModScoreV2)
            );
        }

        private void showPersonalBestWithNullPosition()
        {
            leaderboard.SetScores(
                leaderboard.Scores,
                new ScoreInfo
                {
                    Rank = ScoreRank.XH,
                    Accuracy = 1,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    Ruleset = new OsuRuleset().RulesetInfo,
                    User = new APIUser
                    {
                        Id = 6602580,
                        Username = @"waaiiru",
                        CountryCode = CountryCode.ES,
                    },
                }
            );
        }

        private void showPersonalBest()
        {
            leaderboard.SetScores(
                leaderboard.Scores,
                new ScoreInfo
                {
                    Position = 999,
                    Rank = ScoreRank.XH,
                    Accuracy = 1,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Ruleset = new OsuRuleset().RulesetInfo,
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    User = new APIUser
                    {
                        Id = 6602580,
                        Username = @"waaiiru",
                        CountryCode = CountryCode.ES,
                    },
                }
            );
        }

        private void importMoreScores(Func<BeatmapInfo> beatmapInfo)
        {
            AddStep(
                @"Import new scores",
                () =>
                {
                    foreach (var score in GenerateSampleScores(beatmapInfo()))
                        scoreManager.Import(score);
                }
            );
        }

        private void clearScores()
        {
            AddStep("Clear all scores", () => scoreManager.Delete());
        }

        private void checkDisplayedCount(int expected) =>
            AddUntilStep(
                $"{expected} scores displayed",
                () => leaderboard.ChildrenOfType<LeaderboardScore>().Count(),
                () => Is.EqualTo(expected)
            );

        private void checkStoredCount(int expected) =>
            AddUntilStep(
                $"Total scores stored is {expected}",
                () => Realm.Run(r => r.All<ScoreInfo>().Count(s => !s.DeletePending)),
                () => Is.EqualTo(expected)
            );

        public static ScoreInfo[] GenerateSampleScores(BeatmapInfo beatmapInfo)
        {
            return new[]
            {
                new ScoreInfo
                {
                    Rank = ScoreRank.XH,
                    Accuracy = 1,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now,
                    Mods = new Mod[]
                    {
                        new OsuModHidden(),
                        new OsuModFlashlight
                        {
                            FollowDelay = { Value = 200 },
                            SizeMultiplier = { Value = 5 },
                        },
                        new OsuModDifficultyAdjust
                        {
                            CircleSize = { Value = 11 },
                            ApproachRate = { Value = 10 },
                            OverallDifficulty = { Value = 10 },
                            DrainRate = { Value = 10 },
                            ExtendedLimits = { Value = true },
                        },
                    },
                    Ruleset = new OsuRuleset().RulesetInfo,
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    User = new APIUser
                    {
                        Id = 6602580,
                        Username = @"waaiiru",
                        CountryCode = CountryCode.ES,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.X,
                    Accuracy = 1,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddSeconds(-30),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,
                    User = new APIUser
                    {
                        Id = 4608074,
                        Username = @"Skycries",
                        CountryCode = CountryCode.BR,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.SH,
                    Accuracy = 1,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddSeconds(-70),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,

                    User = new APIUser
                    {
                        Id = 1014222,
                        Username = @"eLy",
                        CountryCode = CountryCode.JP,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.S,
                    Accuracy = 1,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddMinutes(-40),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,

                    User = new APIUser
                    {
                        Id = 1541390,
                        Username = @"Toukai",
                        CountryCode = CountryCode.CA,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.A,
                    Accuracy = 1,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddHours(-2),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,

                    User = new APIUser
                    {
                        Id = 2243452,
                        Username = @"Satoruu",
                        CountryCode = CountryCode.VE,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.B,
                    Accuracy = 0.9826,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddHours(-25),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,

                    User = new APIUser
                    {
                        Id = 2705430,
                        Username = @"Mooha",
                        CountryCode = CountryCode.FR,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.C,
                    Accuracy = 0.9654,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddHours(-50),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,

                    User = new APIUser
                    {
                        Id = 7151382,
                        Username = @"Mayuri Hana",
                        CountryCode = CountryCode.TH,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.D,
                    Accuracy = 0.6025,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddHours(-72),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,

                    User = new APIUser
                    {
                        Id = 2051389,
                        Username = @"FunOrange",
                        CountryCode = CountryCode.CA,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.D,
                    Accuracy = 0.5140,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddMonths(-10),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,

                    User = new APIUser
                    {
                        Id = 6169483,
                        Username = @"-Hebel-",
                        CountryCode = CountryCode.MX,
                    },
                },
                new ScoreInfo
                {
                    Rank = ScoreRank.D,
                    Accuracy = 0.4222,
                    MaxCombo = 244,
                    TotalScore = 1707827,
                    Date = DateTime.Now.AddYears(-2),
                    Mods = new Mod[] { new OsuModHidden(), new OsuModHardRock() },
                    BeatmapInfo = beatmapInfo,
                    BeatmapHash = beatmapInfo.Hash,
                    Ruleset = new OsuRuleset().RulesetInfo,

                    User = new APIUser
                    {
                        Id = 6702666,
                        Username = @"prhtnsm",
                        CountryCode = CountryCode.DE,
                    },
                },
            };
        }

        private partial class FailableLeaderboard : BeatmapLeaderboard
        {
            public new void SetErrorState(LeaderboardState state) => base.SetErrorState(state);

            public new void SetScores(
                IEnumerable<ScoreInfo>? scores,
                ScoreInfo? userScore = null
            ) => base.SetScores(scores, userScore);
        }
    }
}
