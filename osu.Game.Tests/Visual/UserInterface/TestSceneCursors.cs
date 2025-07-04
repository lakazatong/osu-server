﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tests.Visual.UserInterface
{
    [TestFixture]
    public partial class TestSceneCursors : OsuManualInputManagerTestScene
    {
        private readonly GlobalCursorDisplay globalCursorDisplay;
        private readonly CustomCursorBox[] cursorBoxes = new CustomCursorBox[6];

        public TestSceneCursors()
        {
            Child = globalCursorDisplay = new GlobalCursorDisplay
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    // Middle user
                    cursorBoxes[0] = new CustomCursorBox(Color4.Green)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.5f),
                    },
                    // Top-left user
                    cursorBoxes[1] = new CustomCursorBox(Color4.Blue)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.4f),
                    },
                    // Bottom-right user
                    cursorBoxes[2] = new CustomCursorBox(Color4.Red)
                    {
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.4f),
                    },
                    // Bottom-left local
                    cursorBoxes[3] = new CustomCursorBox(Color4.Magenta, false)
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.4f),
                    },
                    // Top-right local
                    cursorBoxes[4] = new CustomCursorBox(Color4.Cyan, false)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.4f),
                    },
                    // Left-local
                    cursorBoxes[5] = new CustomCursorBox(Color4.Yellow, false)
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.2f, 1),
                    },
                },
            };

            AddToggleStep(
                "Smooth transitions",
                b => cursorBoxes.ForEach(box => box.SmoothTransition = b)
            );
        }

        [SetUp]
        public void SetUp() => Schedule(moveOut);

        /// <summary>
        /// -- Green Box --
        /// Tests whether hovering in and out of a drawable that provides the user cursor (green)
        /// results in the correct visibility state for that cursor.
        /// </summary>
        [Test]
        public void TestUserCursor()
        {
            AddStep("Move to green area", () => InputManager.MoveMouseTo(cursorBoxes[0]));
            AddAssert("Check green cursor visible", () => checkVisible(cursorBoxes[0].Cursor));
            AddAssert("Check green cursor at mouse", () => checkAtMouse(cursorBoxes[0].Cursor));
            AddStep("Move out", moveOut);
            AddAssert("Check green cursor invisible", () => !checkVisible(cursorBoxes[0].Cursor));
            AddAssert(
                "Check global cursor visible",
                () => checkVisible(globalCursorDisplay.MenuCursor)
            );
        }

        /// <summary>
        /// -- Purple Box --
        /// Tests whether hovering in and out of a drawable that provides a local cursor (purple)
        /// results in the correct visibility and state for that cursor.
        /// </summary>
        [Test]
        public void TestLocalCursor()
        {
            AddStep("Move to purple area", () => InputManager.MoveMouseTo(cursorBoxes[3]));
            AddAssert("Check purple cursor visible", () => checkVisible(cursorBoxes[3].Cursor));
            AddAssert("Check purple cursor at mouse", () => checkAtMouse(cursorBoxes[3].Cursor));
            AddAssert(
                "Check global cursor visible",
                () => checkVisible(globalCursorDisplay.MenuCursor)
            );
            AddAssert(
                "Check global cursor at mouse",
                () => checkAtMouse(globalCursorDisplay.MenuCursor)
            );
            AddStep("Move out", moveOut);
            AddAssert("Check purple cursor visible", () => checkVisible(cursorBoxes[3].Cursor));
            AddAssert(
                "Check global cursor visible",
                () => checkVisible(globalCursorDisplay.MenuCursor)
            );
        }

        /// <summary>
        /// -- Blue-Green Box Boundary --
        /// Tests whether overriding a user cursor (green) with another user cursor (blue)
        /// results in the correct visibility and states for the cursors.
        /// </summary>
        [Test]
        public void TestUserCursorOverride()
        {
            AddStep(
                "Move to blue-green boundary",
                () =>
                    InputManager.MoveMouseTo(
                        cursorBoxes[1].ScreenSpaceDrawQuad.BottomRight - new Vector2(10)
                    )
            );
            AddAssert("Check blue cursor visible", () => checkVisible(cursorBoxes[1].Cursor));
            AddAssert("Check green cursor invisible", () => !checkVisible(cursorBoxes[0].Cursor));
            AddAssert("Check blue cursor at mouse", () => checkAtMouse(cursorBoxes[1].Cursor));
            AddStep("Move out", moveOut);
            AddAssert("Check blue cursor not visible", () => !checkVisible(cursorBoxes[1].Cursor));
            AddAssert("Check green cursor not visible", () => !checkVisible(cursorBoxes[0].Cursor));
        }

        /// <summary>
        /// -- Yellow-Purple Box Boundary --
        /// Tests whether multiple local cursors (purple + yellow) may be visible and at the mouse position at the same time.
        /// </summary>
        [Test]
        public void TestMultipleLocalCursors()
        {
            AddStep(
                "Move to yellow-purple boundary",
                () =>
                    InputManager.MoveMouseTo(
                        cursorBoxes[5].ScreenSpaceDrawQuad.BottomRight - new Vector2(10)
                    )
            );
            AddAssert("Check purple cursor visible", () => checkVisible(cursorBoxes[3].Cursor));
            AddAssert("Check purple cursor at mouse", () => checkAtMouse(cursorBoxes[3].Cursor));
            AddAssert("Check yellow cursor visible", () => checkVisible(cursorBoxes[5].Cursor));
            AddAssert("Check yellow cursor at mouse", () => checkAtMouse(cursorBoxes[5].Cursor));
            AddStep("Move out", moveOut);
            AddAssert("Check purple cursor visible", () => checkVisible(cursorBoxes[3].Cursor));
            AddAssert("Check yellow cursor visible", () => checkVisible(cursorBoxes[5].Cursor));
        }

        /// <summary>
        /// -- Yellow-Blue Box Boundary --
        /// Tests whether a local cursor (yellow) may be displayed along with a user cursor override (blue).
        /// </summary>
        [Test]
        public void TestUserOverrideWithLocal()
        {
            AddStep(
                "Move to yellow-blue boundary",
                () =>
                    InputManager.MoveMouseTo(
                        cursorBoxes[5].ScreenSpaceDrawQuad.TopRight - new Vector2(10, 0)
                    )
            );
            AddAssert("Check blue cursor visible", () => checkVisible(cursorBoxes[1].Cursor));
            AddAssert("Check blue cursor at mouse", () => checkAtMouse(cursorBoxes[1].Cursor));
            AddAssert("Check yellow cursor visible", () => checkVisible(cursorBoxes[5].Cursor));
            AddAssert("Check yellow cursor at mouse", () => checkAtMouse(cursorBoxes[5].Cursor));
            AddStep("Move out", moveOut);
            AddAssert("Check blue cursor invisible", () => !checkVisible(cursorBoxes[1].Cursor));
            AddAssert("Check yellow cursor visible", () => checkVisible(cursorBoxes[5].Cursor));
        }

        /// <summary>
        /// Ensures non-mouse input hides global cursor on a "local cursor" area (which doesn't hide global cursor).
        /// </summary>
        [Test]
        public void TestKeyboardLocalCursor([Values] bool clickToShow)
        {
            AddStep(
                "Enable cursor hiding",
                () => globalCursorDisplay.MenuCursor.HideCursorOnNonMouseInput = true
            );
            AddStep(
                "Move to purple area",
                () =>
                    InputManager.MoveMouseTo(
                        cursorBoxes[3].ScreenSpaceDrawQuad.Centre + new Vector2(10, 0)
                    )
            );
            AddAssert("Check purple cursor visible", () => checkVisible(cursorBoxes[3].Cursor));
            AddAssert(
                "Check global cursor alpha is 1",
                () => globalCursorDisplay.MenuCursor.Alpha == 1
            );

            AddStep("Press key", () => InputManager.Key(Key.A));
            AddAssert(
                "Check purple cursor still visible",
                () => checkVisible(cursorBoxes[3].Cursor)
            );
            AddUntilStep(
                "Check global cursor alpha is 0",
                () => globalCursorDisplay.MenuCursor.ActiveCursor.Alpha == 0
            );

            if (clickToShow)
                AddStep("Click mouse", () => InputManager.Click(MouseButton.Left));
            else
                AddStep(
                    "Move mouse",
                    () =>
                        InputManager.MoveMouseTo(
                            InputManager.CurrentState.Mouse.Position + Vector2.One
                        )
                );

            AddAssert(
                "Check purple cursor still visible",
                () => checkVisible(cursorBoxes[3].Cursor)
            );
            AddUntilStep(
                "Check global cursor alpha is 1",
                () => globalCursorDisplay.MenuCursor.ActiveCursor.Alpha == 1
            );
        }

        /// <summary>
        /// Ensures mouse input after non-mouse input doesn't show global cursor on a "user cursor" area (which hides global cursor).
        /// </summary>
        [Test]
        public void TestKeyboardUserCursor([Values] bool clickToShow)
        {
            AddStep(
                "Enable cursor hiding",
                () => globalCursorDisplay.MenuCursor.HideCursorOnNonMouseInput = true
            );
            AddStep("Move to green area", () => InputManager.MoveMouseTo(cursorBoxes[0]));
            AddAssert("Check green cursor visible", () => checkVisible(cursorBoxes[0].Cursor));
            AddAssert(
                "Check global cursor alpha is 0",
                () =>
                    !checkVisible(globalCursorDisplay.MenuCursor)
                    && globalCursorDisplay.MenuCursor.ActiveCursor.Alpha == 0
            );

            AddStep("Press key", () => InputManager.Key(Key.A));
            AddAssert(
                "Check green cursor still visible",
                () => checkVisible(cursorBoxes[0].Cursor)
            );
            AddAssert(
                "Check global cursor alpha is still 0",
                () =>
                    !checkVisible(globalCursorDisplay.MenuCursor)
                    && globalCursorDisplay.MenuCursor.ActiveCursor.Alpha == 0
            );

            if (clickToShow)
                AddStep("Click mouse", () => InputManager.Click(MouseButton.Left));
            else
                AddStep(
                    "Move mouse",
                    () =>
                        InputManager.MoveMouseTo(
                            InputManager.CurrentState.Mouse.Position + Vector2.One
                        )
                );

            AddAssert(
                "Check green cursor still visible",
                () => checkVisible(cursorBoxes[0].Cursor)
            );
            AddAssert(
                "Check global cursor alpha is still 0",
                () =>
                    !checkVisible(globalCursorDisplay.MenuCursor)
                    && globalCursorDisplay.MenuCursor.ActiveCursor.Alpha == 0
            );
        }

        /// <summary>
        /// Moves the cursor to a point not covered by any cursor containers.
        /// </summary>
        private void moveOut() =>
            InputManager.MoveMouseTo(
                new Vector2(
                    InputManager.ScreenSpaceDrawQuad.Centre.X,
                    InputManager.ScreenSpaceDrawQuad.TopLeft.Y
                )
            );

        /// <summary>
        /// Checks if a cursor is visible.
        /// </summary>
        /// <param name="cursorContainer">The cursor to check.</param>
        private bool checkVisible(CursorContainer cursorContainer) =>
            cursorContainer.State.Value == Visibility.Visible;

        /// <summary>
        /// Checks if a cursor is at the current inputmanager screen position.
        /// </summary>
        /// <param name="cursorContainer">The cursor to check.</param>
        private bool checkAtMouse(CursorContainer cursorContainer) =>
            Precision.AlmostEquals(
                InputManager.CurrentState.Mouse.Position,
                cursorContainer.ToScreenSpace(cursorContainer.ActiveCursor.DrawPosition)
            );

        private partial class CustomCursorBox : Container, IProvideCursor
        {
            public bool SmoothTransition;

            public CursorContainer Cursor { get; }
            public bool ProvidingUserCursor { get; }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
                base.ReceivePositionalInputAt(screenSpacePos)
                || (SmoothTransition && !ProvidingUserCursor);

            private readonly Box background;

            public CustomCursorBox(Color4 cursorColour, bool providesUserCursor = true)
            {
                ProvidingUserCursor = providesUserCursor;

                Colour = cursorColour;
                Masking = true;

                Children = new Drawable[]
                {
                    background = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0.1f },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = providesUserCursor ? "User cursor" : "Local cursor",
                    },
                    Cursor = new TestCursorContainer
                    {
                        State =
                        {
                            Value = providesUserCursor ? Visibility.Hidden : Visibility.Visible,
                        },
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeTo(0.4f, 250, Easing.OutQuint);
                return false;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeTo(0.1f, 250);
                base.OnHoverLost(e);
            }
        }

        private partial class TestCursorContainer : CursorContainer
        {
            protected override Drawable CreateCursor() => new TestCursor();

            private partial class TestCursor : CircularContainer
            {
                public TestCursor()
                {
                    Origin = Anchor.Centre;

                    Size = new Vector2(50);
                    Masking = true;

                    Blending = BlendingParameters.Additive;
                    Alpha = 0.5f;

                    Child = new Box { RelativeSizeAxes = Axes.Both };
                }
            }
        }
    }
}
