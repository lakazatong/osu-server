// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.MatchTypes.TeamVersus;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Taiko.Mods;
using osu.Game.Screens.OnlinePlay.Multiplayer.Participants;
using osu.Game.Tests.Resources;
using osu.Game.Users;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Multiplayer
{
    public partial class TestSceneMultiplayerParticipantsList : MultiplayerTestScene
    {
        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("join room", () => JoinRoom(CreateDefaultRoom()));
            WaitForJoined();
            createNewParticipantsList();
        }

        [Test]
        public void TestAddUser()
        {
            AddAssert(
                "one unique panel",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Select(p => p.Current.Value)
                        .Distinct()
                        .Count() == 1
            );

            AddStep(
                "add user",
                () =>
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 3,
                            Username = "Second",
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    )
            );

            AddAssert(
                "two unique panels",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Select(p => p.Current.Value)
                        .Distinct()
                        .Count() == 2
            );
        }

        [Test]
        public void TestAddUnresolvedUser()
        {
            AddAssert(
                "one unique panel",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Select(p => p.Current.Value)
                        .Distinct()
                        .Count() == 1
            );

            AddStep("add non-resolvable user", () => MultiplayerClient.TestAddUnresolvedUser());
            AddUntilStep(
                "null user added",
                () => MultiplayerClient.ClientRoom.AsNonNull().Users.Count(u => u.User == null) == 1
            );

            AddUntilStep(
                "two unique panels",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Select(p => p.Current.Value)
                        .Distinct()
                        .Count() == 2
            );

            AddStep(
                "kick null user",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Single(p => p.Current.Value.User == null)
                        .ChildrenOfType<ParticipantPanel.KickButton>()
                        .Single()
                        .TriggerClick()
            );

            AddUntilStep(
                "null user kicked",
                () => MultiplayerClient.ClientRoom.AsNonNull().Users.Count == 1
            );
        }

        [Test]
        public void TestRemoveUser()
        {
            APIUser? secondUser = null;

            AddStep(
                "add a user",
                () =>
                {
                    MultiplayerClient.AddUser(
                        secondUser = new APIUser
                        {
                            Id = 3,
                            Username = "Second",
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    );
                }
            );

            AddStep("remove host", () => MultiplayerClient.RemoveUser(API.LocalUser.Value));

            AddAssert(
                "single panel is for second user",
                () =>
                    this.ChildrenOfType<ParticipantPanel>().Single().Current.Value.UserID
                    == secondUser?.Id
            );
        }

        [Test]
        public void TestGameStateHasPriorityOverDownloadState()
        {
            AddStep(
                "set to downloading map",
                () =>
                    MultiplayerClient.ChangeBeatmapAvailability(BeatmapAvailability.Downloading(0))
            );
            checkProgressBarVisibility(true);

            AddStep(
                "make user ready",
                () => MultiplayerClient.ChangeState(MultiplayerUserState.Results)
            );
            checkProgressBarVisibility(false);
            AddUntilStep(
                "ready mark visible",
                () => this.ChildrenOfType<StateDisplay>().Single().IsPresent
            );

            AddStep(
                "make user ready",
                () => MultiplayerClient.ChangeState(MultiplayerUserState.Idle)
            );
            checkProgressBarVisibility(true);
        }

        [Test]
        public void TestCorrectInitialState()
        {
            AddStep(
                "set to downloading map",
                () =>
                    MultiplayerClient.ChangeBeatmapAvailability(BeatmapAvailability.Downloading(0))
            );
            createNewParticipantsList();
            checkProgressBarVisibility(true);
        }

        [Test]
        public void TestBeatmapDownloadingStates()
        {
            AddStep(
                "set to unknown",
                () => MultiplayerClient.ChangeBeatmapAvailability(BeatmapAvailability.Unknown())
            );
            AddStep(
                "set to no map",
                () =>
                    MultiplayerClient.ChangeBeatmapAvailability(BeatmapAvailability.NotDownloaded())
            );
            AddStep(
                "set to downloading map",
                () =>
                    MultiplayerClient.ChangeBeatmapAvailability(BeatmapAvailability.Downloading(0))
            );

            checkProgressBarVisibility(true);

            AddRepeatStep(
                "increment progress",
                () =>
                {
                    float progress =
                        this.ChildrenOfType<ParticipantPanel>()
                            .Single()
                            .Current.Value.BeatmapAvailability.DownloadProgress ?? 0;
                    MultiplayerClient.ChangeBeatmapAvailability(
                        BeatmapAvailability.Downloading(progress + RNG.NextSingle(0.1f))
                    );
                },
                25
            );

            AddAssert(
                "progress bar increased",
                () => this.ChildrenOfType<ProgressBar>().Single().Current.Value > 0
            );

            AddStep(
                "set to importing map",
                () => MultiplayerClient.ChangeBeatmapAvailability(BeatmapAvailability.Importing())
            );
            checkProgressBarVisibility(false);

            AddStep(
                "set to available",
                () =>
                    MultiplayerClient.ChangeBeatmapAvailability(
                        BeatmapAvailability.LocallyAvailable()
                    )
            );
        }

        [Test]
        public void TestToggleReadyState()
        {
            AddAssert(
                "ready mark invisible",
                () => !this.ChildrenOfType<StateDisplay>().Single().IsPresent
            );

            AddStep(
                "make user ready",
                () => MultiplayerClient.ChangeState(MultiplayerUserState.Ready)
            );
            AddUntilStep(
                "ready mark visible",
                () => this.ChildrenOfType<StateDisplay>().Single().IsPresent
            );

            AddStep(
                "make user idle",
                () => MultiplayerClient.ChangeState(MultiplayerUserState.Idle)
            );
            AddUntilStep(
                "ready mark invisible",
                () => !this.ChildrenOfType<StateDisplay>().Single().IsPresent
            );
        }

        [Test]
        public void TestToggleSpectateState()
        {
            AddStep(
                "make user spectating",
                () => MultiplayerClient.ChangeState(MultiplayerUserState.Spectating)
            );
            AddStep(
                "make user idle",
                () => MultiplayerClient.ChangeState(MultiplayerUserState.Idle)
            );
        }

        [Test]
        public void TestCrownChangesStateWhenHostTransferred()
        {
            AddStep(
                "add user",
                () =>
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 3,
                            Username = "Second",
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    )
            );

            AddUntilStep(
                "first user crown visible",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Single(p => p.Current.Value.UserID == 1001)
                        .ChildrenOfType<SpriteIcon>()
                        .First()
                        .Alpha == 1
            );
            AddUntilStep(
                "second user crown hidden",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Single(p => p.Current.Value.UserID == 3)
                        .ChildrenOfType<SpriteIcon>()
                        .First()
                        .Alpha == 0
            );

            AddStep("make second user host", () => MultiplayerClient.TransferHost(3));

            AddUntilStep(
                "first user crown visible",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Single(p => p.Current.Value.UserID == 1001)
                        .ChildrenOfType<SpriteIcon>()
                        .First()
                        .Alpha == 0
            );
            AddUntilStep(
                "second user crown hidden",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Single(p => p.Current.Value.UserID == 3)
                        .ChildrenOfType<SpriteIcon>()
                        .First()
                        .Alpha == 1
            );
        }

        [Test]
        public void TestHostGetsPinnedToTop()
        {
            AddStep(
                "add user",
                () =>
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 3,
                            Username = "Second",
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    )
            );

            AddStep("make second user host", () => MultiplayerClient.TransferHost(3));
            AddAssert(
                "second user above first",
                () =>
                {
                    var first = this.ChildrenOfType<ParticipantPanel>()
                        .Single(u => u.Current.Value.UserID == 1001);
                    var second = this.ChildrenOfType<ParticipantPanel>()
                        .Single(u => u.Current.Value.UserID == 3);
                    return second.ScreenSpaceDrawQuad.TopLeft.Y
                        < first.ScreenSpaceDrawQuad.TopLeft.Y;
                }
            );
        }

        [Test]
        public void TestKickButtonOnlyPresentWhenHost()
        {
            AddStep(
                "add user",
                () =>
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 3,
                            Username = "Second",
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    )
            );

            AddUntilStep(
                "kick buttons visible",
                () =>
                    this.ChildrenOfType<ParticipantPanel.KickButton>().Count(d => d.IsPresent) == 1
            );

            AddStep("make second user host", () => MultiplayerClient.TransferHost(3));

            AddUntilStep(
                "kick buttons not visible",
                () => !this.ChildrenOfType<ParticipantPanel.KickButton>().Any(d => d.IsPresent)
            );

            AddStep(
                "make local user host again",
                () => MultiplayerClient.TransferHost(API.LocalUser.Value.Id)
            );

            AddUntilStep(
                "kick buttons visible",
                () =>
                    this.ChildrenOfType<ParticipantPanel.KickButton>().Count(d => d.IsPresent) == 1
            );
        }

        [Test]
        public void TestKickButtonKicks()
        {
            AddStep(
                "add user",
                () =>
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 3,
                            Username = "Second",
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    )
            );

            AddStep(
                "kick second user",
                () =>
                    this.ChildrenOfType<ParticipantPanel.KickButton>()
                        .Single(d => d.IsPresent)
                        .TriggerClick()
            );

            AddUntilStep(
                "second user kicked",
                () => MultiplayerClient.ClientRoom?.Users.Single().UserID == API.LocalUser.Value.Id
            );
        }

        [Test]
        public void TestManyUsers()
        {
            const int users_count = 200;

            AddStep(
                "add many users",
                () =>
                {
                    for (int i = 0; i < users_count; i++)
                    {
                        MultiplayerClient.AddUser(
                            new APIUser
                            {
                                Id = i,
                                Username = $"User {i}",
                                RulesetsStatistics = new Dictionary<string, UserStatistics>
                                {
                                    {
                                        Ruleset.Value.ShortName,
                                        new UserStatistics { GlobalRank = RNG.Next(1, 100000) }
                                    },
                                },
                                CoverUrl = TestResources.COVER_IMAGE_3,
                            }
                        );

                        MultiplayerClient.ChangeUserState(
                            i,
                            (MultiplayerUserState)RNG.Next(0, (int)MultiplayerUserState.Results + 1)
                        );

                        if (RNG.NextBool())
                        {
                            var beatmapState = (DownloadState)
                                RNG.Next(0, (int)DownloadState.LocallyAvailable + 1);

                            switch (beatmapState)
                            {
                                case DownloadState.NotDownloaded:
                                    MultiplayerClient.ChangeUserBeatmapAvailability(
                                        i,
                                        BeatmapAvailability.NotDownloaded()
                                    );
                                    break;

                                case DownloadState.Downloading:
                                    MultiplayerClient.ChangeUserBeatmapAvailability(
                                        i,
                                        BeatmapAvailability.Downloading(RNG.NextSingle())
                                    );
                                    break;

                                case DownloadState.Importing:
                                    MultiplayerClient.ChangeUserBeatmapAvailability(
                                        i,
                                        BeatmapAvailability.Importing()
                                    );
                                    break;
                            }
                        }
                    }
                }
            );

            AddRepeatStep(
                "switch hosts",
                () => MultiplayerClient.TransferHost(RNG.Next(0, users_count)),
                10
            );
            AddStep("give host back", () => MultiplayerClient.TransferHost(API.LocalUser.Value.Id));

            AddRepeatStep(
                "perform many random state changes at once",
                () =>
                {
                    for (int i = 0; i < users_count; ++i)
                    {
                        MultiplayerClient.ChangeUserBeatmapAvailability(
                            i,
                            BeatmapAvailability.LocallyAvailable()
                        );
                        MultiplayerClient.ChangeUserState(
                            i,
                            RNG.NextBool() ? MultiplayerUserState.Idle : MultiplayerUserState.Ready
                        );
                    }
                },
                100
            );
        }

        [Test]
        public void TestUserWithMods()
        {
            AddStep(
                "add user",
                () =>
                {
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 0,
                            Username = "User 0",
                            RulesetsStatistics = new Dictionary<string, UserStatistics>
                            {
                                {
                                    Ruleset.Value.ShortName,
                                    new UserStatistics { GlobalRank = RNG.Next(1, 100000) }
                                },
                            },
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    );

                    MultiplayerClient.ChangeUserMods(
                        0,
                        new Mod[]
                        {
                            new OsuModHardRock(),
                            new OsuModDifficultyAdjust { ApproachRate = { Value = 1 } },
                        }
                    );
                }
            );

            for (var i = MultiplayerUserState.Idle; i < MultiplayerUserState.Results; i++)
            {
                var state = i;
                AddStep($"set state: {state}", () => MultiplayerClient.ChangeUserState(0, state));
            }

            AddStep(
                "set state: downloading",
                () =>
                    MultiplayerClient.ChangeUserBeatmapAvailability(
                        0,
                        BeatmapAvailability.Downloading(0)
                    )
            );

            AddStep(
                "set state: locally available",
                () =>
                    MultiplayerClient.ChangeUserBeatmapAvailability(
                        0,
                        BeatmapAvailability.LocallyAvailable()
                    )
            );
        }

        [Test]
        public void TestUserWithStyle()
        {
            AddStep(
                "add users",
                () =>
                {
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 0,
                            Username = "User 0",
                            RulesetsStatistics = new Dictionary<string, UserStatistics>
                            {
                                {
                                    Ruleset.Value.ShortName,
                                    new UserStatistics { GlobalRank = RNG.Next(1, 100000) }
                                },
                            },
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    );

                    MultiplayerClient.ChangeUserStyle(0, 259, 2);
                }
            );

            AddStep(
                "set beatmap locally available",
                () =>
                    MultiplayerClient.ChangeUserBeatmapAvailability(
                        0,
                        BeatmapAvailability.LocallyAvailable()
                    )
            );
            AddStep(
                "change user style to beatmap: 258, ruleset: 1",
                () => MultiplayerClient.ChangeUserStyle(0, 258, 1)
            );
            AddStep(
                "change user style to beatmap: null, ruleset: null",
                () => MultiplayerClient.ChangeUserStyle(0, null, null)
            );
        }

        [Test]
        public void TestModOverlap()
        {
            AddStep(
                "add dummy mods",
                () =>
                {
                    MultiplayerClient.ChangeUserMods(
                        new Mod[] { new OsuModNoFail(), new OsuModDoubleTime() }
                    );
                }
            );

            AddStep(
                "add user with mods",
                () =>
                {
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 0,
                            Username = "Baka",
                            RulesetsStatistics = new Dictionary<string, UserStatistics>
                            {
                                {
                                    Ruleset.Value.ShortName,
                                    new UserStatistics { GlobalRank = RNG.Next(1, 100000) }
                                },
                            },
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    );
                    MultiplayerClient.ChangeUserMods(
                        0,
                        new Mod[] { new OsuModHardRock(), new OsuModDoubleTime() }
                    );
                }
            );

            AddStep("set 0 ready", () => MultiplayerClient.ChangeState(MultiplayerUserState.Ready));

            AddStep(
                "set 1 spectate",
                () => MultiplayerClient.ChangeUserState(0, MultiplayerUserState.Spectating)
            );

            // Have to set back to idle due to status priority.
            AddStep(
                "set 0 no map, 1 ready",
                () =>
                {
                    MultiplayerClient.ChangeState(MultiplayerUserState.Idle);
                    MultiplayerClient.ChangeBeatmapAvailability(
                        BeatmapAvailability.NotDownloaded()
                    );
                    MultiplayerClient.ChangeUserState(0, MultiplayerUserState.Ready);
                }
            );

            AddStep(
                "set 0 downloading",
                () =>
                    MultiplayerClient.ChangeBeatmapAvailability(BeatmapAvailability.Downloading(0))
            );

            AddStep(
                "set 0 spectate",
                () => MultiplayerClient.ChangeUserState(0, MultiplayerUserState.Spectating)
            );

            AddStep(
                "make both default",
                () =>
                {
                    MultiplayerClient.ChangeBeatmapAvailability(
                        BeatmapAvailability.LocallyAvailable()
                    );
                    MultiplayerClient.ChangeUserState(0, MultiplayerUserState.Idle);
                    MultiplayerClient.ChangeState(MultiplayerUserState.Idle);
                }
            );
        }

        [Test]
        public void TestModsAndRuleset()
        {
            AddStep(
                "add another user",
                () =>
                {
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = 0,
                            Username = "User 0",
                            RulesetsStatistics = new Dictionary<string, UserStatistics>
                            {
                                {
                                    Ruleset.Value.ShortName,
                                    new UserStatistics { GlobalRank = RNG.Next(1, 100000) }
                                },
                            },
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    );

                    MultiplayerClient.ChangeUserBeatmapAvailability(
                        0,
                        BeatmapAvailability.LocallyAvailable()
                    );
                }
            );

            AddStep(
                "set user styles",
                () =>
                {
                    MultiplayerClient.ChangeUserStyle(API.LocalUser.Value.OnlineID, 259, 1);
                    MultiplayerClient.ChangeUserMods(
                        API.LocalUser.Value.OnlineID,
                        [
                            new APIMod(new TaikoModConstantSpeed()),
                            new APIMod(new TaikoModHidden()),
                            new APIMod(new TaikoModFlashlight()),
                            new APIMod(new TaikoModHardRock()),
                        ]
                    );

                    MultiplayerClient.ChangeUserStyle(0, 259, 2);
                    MultiplayerClient.ChangeUserMods(
                        0,
                        [
                            new APIMod(new CatchModFloatingFruits()),
                            new APIMod(new CatchModHidden()),
                            new APIMod(new CatchModMirror()),
                        ]
                    );
                }
            );
        }

        [Test]
        public void TestTeams()
        {
            AddStep(
                "enable teams",
                () => MultiplayerClient.ChangeSettings(matchType: MatchType.TeamVersus)
            );
            AddAssert(
                "one unique panel",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Select(p => p.Current.Value)
                        .Distinct()
                        .Count() == 1
            );

            int id = 3;
            AddRepeatStep(
                "add users",
                () =>
                    MultiplayerClient.AddUser(
                        new APIUser
                        {
                            Id = Interlocked.Increment(ref id),
                            Username = "Second",
                            CoverUrl = TestResources.COVER_IMAGE_3,
                        }
                    ),
                5
            );

            AddAssert(
                "two unique panels",
                () =>
                    this.ChildrenOfType<ParticipantPanel>()
                        .Select(p => p.Current.Value)
                        .Distinct()
                        .Count() == 6
            );

            AddAssert(
                "user 1001 on red team",
                () =>
                    (
                        MultiplayerClient.ClientRoom!.Users.Single(u => u.UserID == 1001).MatchState
                        as TeamVersusUserState
                    )?.TeamID,
                () => Is.EqualTo(0)
            );
            AddStep(
                "click first team indicator",
                () =>
                {
                    InputManager.MoveMouseTo(this.ChildrenOfType<TeamDisplay>().First());
                    InputManager.Click(MouseButton.Left);
                }
            );
            AddAssert(
                "user 1001 on blue team",
                () =>
                    (
                        MultiplayerClient.ClientRoom!.Users.Single(u => u.UserID == 1001).MatchState
                        as TeamVersusUserState
                    )?.TeamID,
                () => Is.EqualTo(1)
            );
        }

        private void createNewParticipantsList()
        {
            ParticipantsList? participantsList = null;

            AddStep(
                "create new list",
                () =>
                    Child = new OsuContextMenuContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = participantsList =
                            new ParticipantsList
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Y,
                                Size = new Vector2(380, 0.7f),
                            },
                    }
            );

            AddUntilStep("wait for list to load", () => participantsList?.IsLoaded == true);

            AddStep(
                "set beatmap available",
                () =>
                    MultiplayerClient.ChangeBeatmapAvailability(
                        BeatmapAvailability.LocallyAvailable()
                    )
            );
        }

        private void checkProgressBarVisibility(bool visible) =>
            AddUntilStep(
                $"progress bar {(visible ? "is" : "is not")}visible",
                () => this.ChildrenOfType<ProgressBar>().Single().IsPresent == visible
            );
    }
}
