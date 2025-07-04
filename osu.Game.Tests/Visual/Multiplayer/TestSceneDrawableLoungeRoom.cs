// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay.Lounge;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Multiplayer
{
    public partial class TestSceneDrawableLoungeRoom : OsuManualInputManagerTestScene
    {
        private readonly Room room = new Room { Password = "*" };

        [Cached]
        protected readonly OverlayColourProvider ColourProvider = new OverlayColourProvider(
            OverlayColourScheme.Pink
        );

        private LoungeRoomPanel panel = null!;
        private SearchTextBox searchTextBox = null!;

        private readonly ManualResetEventSlim allowResponseCallback = new ManualResetEventSlim();

        [BackgroundDependencyLoader]
        private void load()
        {
            var mockLounge = new Mock<IOnlinePlayLounge>();
            mockLounge
                .Setup(l =>
                    l.Join(
                        It.IsAny<Room>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<Room>>(),
                        It.IsAny<Action<string, Exception?>>()
                    )
                )
                .Callback<Room, string, Action<Room>, Action<string, Exception?>>(
                    (_, _, _, d) =>
                    {
                        Task.Run(() =>
                        {
                            allowResponseCallback.Wait(10000);
                            allowResponseCallback.Reset();
                            Schedule(() =>
                                d?.Invoke("Incorrect password", new InvalidPasswordException())
                            );
                        });
                    }
                );

            Dependencies.CacheAs(mockLounge.Object);
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep(
                "create drawable",
                () =>
                {
                    Child = new PopoverContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            searchTextBox = new SearchTextBox
                            {
                                HoldFocus = true,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Margin = new MarginPadding(50),
                                Width = 500,
                                Depth = float.MaxValue,
                            },
                            panel = new LoungeRoomPanel(room)
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                SelectedRoom = new Bindable<Room?>(),
                            },
                        },
                    };
                }
            );
        }

        [Test]
        public void TestFocusViaKeyboardCommit()
        {
            LoungeRoomPanel.PasswordEntryPopover? popover = null;

            AddAssert("search textbox has focus", () => checkFocus(searchTextBox));
            AddStep(
                "click room twice",
                () =>
                {
                    InputManager.MoveMouseTo(panel);
                    InputManager.Click(MouseButton.Left);
                    InputManager.Click(MouseButton.Left);
                }
            );
            AddUntilStep(
                "wait for popover",
                () =>
                    (
                        popover = InputManager
                            .ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>()
                            .SingleOrDefault()
                    ) != null
            );

            AddAssert(
                "textbox has focus",
                () => checkFocus(popover.ChildrenOfType<OsuPasswordTextBox>().Single())
            );

            AddStep(
                "enter password",
                () => popover.ChildrenOfType<OsuPasswordTextBox>().Single().Text = "password"
            );
            AddStep("commit via enter", () => InputManager.Key(Key.Enter));

            AddAssert("popover has focus", () => checkFocus(popover!));

            AddStep("attempt another enter", () => InputManager.Key(Key.Enter));

            AddAssert("popover still has focus", () => checkFocus(popover!));

            AddStep("unblock response", () => allowResponseCallback.Set());

            AddUntilStep(
                "wait for textbox refocus",
                () => checkFocus(popover.ChildrenOfType<OsuPasswordTextBox>().Single())
            );

            AddStep("press escape", () => InputManager.Key(Key.Escape));
            AddStep("press escape", () => InputManager.Key(Key.Escape));

            AddUntilStep("search textbox has focus", () => checkFocus(searchTextBox));
        }

        [Test]
        public void TestFocusViaMouseCommit()
        {
            LoungeRoomPanel.PasswordEntryPopover? popover = null;

            AddAssert("search textbox has focus", () => checkFocus(searchTextBox));
            AddStep(
                "click room twice",
                () =>
                {
                    InputManager.MoveMouseTo(panel);
                    InputManager.Click(MouseButton.Left);
                    InputManager.Click(MouseButton.Left);
                }
            );
            AddUntilStep(
                "wait for popover",
                () =>
                    (
                        popover = InputManager
                            .ChildrenOfType<LoungeRoomPanel.PasswordEntryPopover>()
                            .SingleOrDefault()
                    ) != null
            );

            AddAssert(
                "textbox has focus",
                () => checkFocus(popover.ChildrenOfType<OsuPasswordTextBox>().Single())
            );

            AddStep(
                "enter password",
                () => popover.ChildrenOfType<OsuPasswordTextBox>().Single().Text = "password"
            );

            AddStep(
                "commit via click button",
                () =>
                {
                    var button = popover.ChildrenOfType<OsuButton>().Single();
                    InputManager.MoveMouseTo(button);
                    InputManager.Click(MouseButton.Left);
                }
            );

            AddAssert("popover has focus", () => checkFocus(popover!));

            AddStep("attempt another click", () => InputManager.Click(MouseButton.Left));

            AddAssert("popover still has focus", () => checkFocus(popover!));

            AddStep("unblock response", () => allowResponseCallback.Set());

            AddUntilStep(
                "wait for textbox refocus",
                () => checkFocus(popover.ChildrenOfType<OsuPasswordTextBox>().Single())
            );

            AddStep(
                "click away",
                () =>
                {
                    InputManager.MoveMouseTo(searchTextBox);
                    InputManager.Click(MouseButton.Left);
                }
            );

            AddUntilStep("search textbox has focus", () => checkFocus(searchTextBox));
        }

        private bool checkFocus(Drawable expected) => InputManager.FocusedDrawable == expected;
    }
}
