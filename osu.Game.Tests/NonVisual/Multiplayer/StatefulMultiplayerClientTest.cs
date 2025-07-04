﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Humanizer;
using NUnit.Framework;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Testing;
using osu.Game.Extensions;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Multiplayer;
using osu.Game.Tests.Visual.Multiplayer;

namespace osu.Game.Tests.NonVisual.Multiplayer
{
    [HeadlessTest]
    public partial class StatefulMultiplayerClientTest : MultiplayerTestScene
    {
        public override void SetUpSteps()
        {
            base.SetUpSteps();
            AddStep("join room", () => JoinRoom(CreateDefaultRoom()));
            WaitForJoined();
        }

        [Test]
        public void TestUserAddedOnJoin()
        {
            var user = new APIUser { Id = 33 };

            AddRepeatStep("add user multiple times", () => MultiplayerClient.AddUser(user), 3);
            AddUntilStep("room has 2 users", () => MultiplayerClient.ClientRoom?.Users.Count == 2);
        }

        [Test]
        public void TestUserRemovedOnLeave()
        {
            var user = new APIUser { Id = 44 };

            AddStep("add user", () => MultiplayerClient.AddUser(user));
            AddUntilStep("room has 2 users", () => MultiplayerClient.ClientRoom?.Users.Count == 2);

            AddStep("remove user", () => MultiplayerClient.RemoveUser(user));
            AddUntilStep("room has 1 user", () => MultiplayerClient.ClientRoom?.Users.Count == 1);
        }

        [Test]
        public void TestPlayingUserTracking()
        {
            int id = 2000;

            AddRepeatStep(
                "add some users",
                () => MultiplayerClient.AddUser(new APIUser { Id = id++ }),
                5
            );
            checkPlayingUserCount(0);

            changeState(3, MultiplayerUserState.WaitingForLoad);
            checkPlayingUserCount(3);

            changeState(3, MultiplayerUserState.Playing);
            checkPlayingUserCount(3);

            changeState(3, MultiplayerUserState.Results);
            checkPlayingUserCount(0);

            changeState(6, MultiplayerUserState.WaitingForLoad);
            checkPlayingUserCount(6);

            AddStep(
                "another user left",
                () =>
                    MultiplayerClient.RemoveUser(
                        (MultiplayerClient.ServerRoom?.Users.Last().User).AsNonNull()
                    )
            );
            checkPlayingUserCount(5);

            AddStep("leave room", () => MultiplayerClient.LeaveRoom());
            checkPlayingUserCount(0);
        }

        [Test]
        public void TestPlayingUsersUpdatedOnJoin()
        {
            AddStep("leave room", () => MultiplayerClient.LeaveRoom());
            AddUntilStep("wait for room part", () => !RoomJoined);

            AddStep(
                "create room initially in gameplay",
                () =>
                {
                    MultiplayerClient.RoomSetupAction = room =>
                    {
                        room.State = MultiplayerRoomState.Playing;
                        room.Users.Add(
                            new MultiplayerRoomUser(PLAYER_1_ID)
                            {
                                User = new APIUser { Id = PLAYER_1_ID },
                                State = MultiplayerUserState.Playing,
                            }
                        );
                    };

                    MultiplayerClient
                        .JoinRoom(MultiplayerClient.ServerSideRooms.Single())
                        .ConfigureAwait(false);
                }
            );

            AddUntilStep("wait for room join", () => RoomJoined);
            checkPlayingUserCount(1);
        }

        [Test]
        public void TestJoinRoomWithManyUsers()
        {
            AddStep("leave room", () => MultiplayerClient.LeaveRoom());
            AddUntilStep("wait for room part", () => !RoomJoined);

            AddStep(
                "create room with many users",
                () =>
                {
                    MultiplayerClient.RoomSetupAction = room =>
                    {
                        room.Users.AddRange(
                            Enumerable
                                .Range(PLAYER_1_ID, 100)
                                .Select(id => new MultiplayerRoomUser(id))
                        );
                    };

                    MultiplayerClient
                        .JoinRoom(MultiplayerClient.ServerSideRooms.Single())
                        .ConfigureAwait(false);
                }
            );

            AddUntilStep("wait for room join", () => RoomJoined);
        }

        private void checkPlayingUserCount(int expectedCount) =>
            AddAssert(
                $"{"user".ToQuantity(expectedCount)} playing",
                () => MultiplayerClient.CurrentMatchPlayingUserIds.Count == expectedCount
            );

        private void changeState(int userCount, MultiplayerUserState state) =>
            AddStep(
                $"{"user".ToQuantity(userCount)} in {state}",
                () =>
                {
                    for (int i = 0; i < userCount; ++i)
                    {
                        int userId =
                            MultiplayerClient.ServerRoom?.Users[i].UserID
                            ?? throw new AssertionException("Room cannot be null!");
                        MultiplayerClient.ChangeUserState(userId, state);
                    }
                }
            );
    }
}
