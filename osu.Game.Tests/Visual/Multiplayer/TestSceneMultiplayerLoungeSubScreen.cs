// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.Rooms;
using osu.Game.Screens.OnlinePlay.Lounge;
using osu.Game.Screens.OnlinePlay.Multiplayer;
using osu.Game.Tests.Visual.OnlinePlay;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Multiplayer
{
    public partial class TestSceneMultiplayerLoungeSubScreen : MultiplayerTestScene
    {
        private MultiplayerLoungeSubScreen loungeScreen = null!;

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep(
                "push screen",
                () => LoadScreen(loungeScreen = new MultiplayerLoungeSubScreen())
            );
            AddUntilStep("wait for present", () => loungeScreen.IsCurrentScreen());
        }

        [Test]
        public void TestJoinRoomWithoutPassword()
        {
            createRooms(GenerateRooms(1, withPassword: false));
            AddStep("select room", () => InputManager.Key(Key.Down));
            AddStep("join room", () => InputManager.Key(Key.Enter));

            AddAssert("room joined", () => MultiplayerClient.RoomJoined);
        }

        [Test]
        public void TestPopoverHidesOnBackButton()
        {
            createRooms(GenerateRooms(1, withPassword: true));
            AddStep("select room", () => InputManager.Key(Key.Down));
            AddStep("attempt join room", () => InputManager.Key(Key.Enter));

            AddUntilStep(
                "password prompt appeared",
                () => InputManager.ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>().Any()
            );

            AddAssert(
                "textbox has focus",
                () => InputManager.FocusedDrawable is OsuPasswordTextBox
            );

            AddStep("hit escape", () => InputManager.Key(Key.Escape));
            AddAssert("textbox lost focus", () => InputManager.FocusedDrawable is SearchTextBox);

            AddStep("hit escape", () => InputManager.Key(Key.Escape));
            AddUntilStep(
                "password prompt hidden",
                () => !InputManager.ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>().Any()
            );

            AddAssert("room not joined", () => !MultiplayerClient.RoomJoined);
        }

        [Test]
        public void TestPopoverHidesOnLeavingScreen()
        {
            createRooms(GenerateRooms(1, withPassword: true));
            AddStep("select room", () => InputManager.Key(Key.Down));
            AddStep("attempt join room", () => InputManager.Key(Key.Enter));

            AddUntilStep(
                "password prompt appeared",
                () => InputManager.ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>().Any()
            );
            AddStep("exit screen", () => Stack.Exit());
            AddUntilStep(
                "password prompt hidden",
                () => !InputManager.ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>().Any()
            );

            AddAssert("room not joined", () => !MultiplayerClient.RoomJoined);
        }

        [Test]
        public void TestJoinRoomWithIncorrectPasswordViaButton()
        {
            LoungeRoomPanel.PasswordEntryPopover? passwordEntryPopover = null;

            createRooms(GenerateRooms(1, withPassword: true));
            AddStep("select room", () => InputManager.Key(Key.Down));
            AddStep("attempt join room", () => InputManager.Key(Key.Enter));
            AddUntilStep(
                "password prompt appeared",
                () =>
                    (
                        passwordEntryPopover = InputManager
                            .ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>()
                            .FirstOrDefault()
                    ) != null
            );
            AddStep(
                "enter password in text box",
                () => passwordEntryPopover.ChildrenOfType<TextBox>().First().Text = "wrong"
            );
            AddStep(
                "press join room button",
                () => passwordEntryPopover.ChildrenOfType<OsuButton>().First().TriggerClick()
            );

            AddAssert("still at lounge", () => loungeScreen.IsCurrentScreen());
            AddUntilStep(
                "password prompt still visible",
                () => passwordEntryPopover!.State.Value == Visibility.Visible
            );
            AddAssert(
                "textbox still focused",
                () => InputManager.FocusedDrawable is OsuPasswordTextBox
            );

            AddAssert("room not joined", () => !MultiplayerClient.RoomJoined);
        }

        [Test]
        public void TestJoinRoomWithIncorrectPasswordViaEnter()
        {
            LoungeRoomPanel.PasswordEntryPopover? passwordEntryPopover = null;

            createRooms(GenerateRooms(1, withPassword: true));
            AddStep("select room", () => InputManager.Key(Key.Down));
            AddStep("attempt join room", () => InputManager.Key(Key.Enter));
            AddUntilStep(
                "password prompt appeared",
                () =>
                    (
                        passwordEntryPopover = InputManager
                            .ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>()
                            .FirstOrDefault()
                    ) != null
            );
            AddStep(
                "enter password in text box",
                () => passwordEntryPopover.ChildrenOfType<TextBox>().First().Text = "wrong"
            );
            AddStep("press enter", () => InputManager.Key(Key.Enter));

            AddAssert("still at lounge", () => loungeScreen.IsCurrentScreen());
            AddUntilStep(
                "password prompt still visible",
                () => passwordEntryPopover!.State.Value == Visibility.Visible
            );
            AddAssert(
                "textbox still focused",
                () => InputManager.FocusedDrawable is OsuPasswordTextBox
            );

            AddAssert("room not joined", () => !MultiplayerClient.RoomJoined);
        }

        [Test]
        public void TestJoinRoomWithCorrectPassword()
        {
            LoungeRoomPanel.PasswordEntryPopover? passwordEntryPopover = null;

            createRooms(GenerateRooms(1, withPassword: true));
            AddStep("select room", () => InputManager.Key(Key.Down));
            AddStep("attempt join room", () => InputManager.Key(Key.Enter));
            AddUntilStep(
                "password prompt appeared",
                () =>
                    (
                        passwordEntryPopover = InputManager
                            .ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>()
                            .FirstOrDefault()
                    ) != null
            );
            AddStep(
                "enter password in text box",
                () => passwordEntryPopover.ChildrenOfType<TextBox>().First().Text = "password"
            );
            AddStep(
                "press join room button",
                () => passwordEntryPopover.ChildrenOfType<OsuButton>().First().TriggerClick()
            );

            AddUntilStep("room joined", () => MultiplayerClient.RoomJoined);
        }

        [Test]
        public void TestJoinRoomWithPasswordViaKeyboardOnly()
        {
            LoungeRoomPanel.PasswordEntryPopover? passwordEntryPopover = null;

            createRooms(GenerateRooms(1, withPassword: true));
            AddStep("select room", () => InputManager.Key(Key.Down));
            AddStep("attempt join room", () => InputManager.Key(Key.Enter));
            AddUntilStep(
                "password prompt appeared",
                () =>
                    (
                        passwordEntryPopover = InputManager
                            .ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>()
                            .FirstOrDefault()
                    ) != null
            );
            AddStep(
                "enter password in text box",
                () => passwordEntryPopover.ChildrenOfType<TextBox>().First().Text = "password"
            );
            AddStep("press enter", () => InputManager.Key(Key.Enter));

            AddAssert("room joined", () => MultiplayerClient.RoomJoined);
        }

        private void createRooms(params Room[] rooms)
        {
            AddStep(
                "create rooms",
                () =>
                {
                    foreach (var room in rooms)
                        API.Queue(new CreateRoomRequest(room));
                }
            );

            AddStep("refresh lounge", () => loungeScreen.RefreshRooms());
        }

        protected override OnlinePlayTestSceneDependencies CreateOnlinePlayDependencies() =>
            new MultiplayerTestSceneDependencies();
    }
}
