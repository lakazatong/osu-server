﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Cursor;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneZoomableScrollContainer : OsuManualInputManagerTestScene
    {
        private TestZoomableScrollContainer scrollContainer;
        private Drawable innerBox;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep(
                "Add new scroll container",
                () =>
                {
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.X,
                            Height = 250,
                            Width = 0.75f,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = OsuColour.Gray(30),
                                },
                                scrollContainer = new TestZoomableScrollContainer(1, 60, 1)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativeSizeAxes = Axes.Both,
                                },
                            },
                        },
                        new MenuCursorContainer(),
                    };

                    scrollContainer.Add(
                        innerBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourInfo.GradientHorizontal(
                                new Color4(0.8f, 0.6f, 0.4f, 1f),
                                new Color4(0.4f, 0.6f, 0.8f, 1f)
                            ),
                        }
                    );
                }
            );
            AddUntilStep(
                "Scroll container is loaded",
                () => scrollContainer.LoadState >= LoadState.Loaded
            );
        }

        [Test]
        public void TestInitialZoomOutOfRange()
        {
            AddStep(
                "Invalid ZoomableScrollContainer throws ArgumentException",
                () =>
                {
                    Assert.Throws<ArgumentException>(() =>
                    {
                        _ = new ZoomableScrollContainer(1, 60, 0);
                    });
                }
            );
        }

        [Test]
        public void TestWidthInitialization()
        {
            AddAssert(
                "Inner container width was initialized",
                () => innerBox.DrawWidth == scrollContainer.DrawWidth
            );
        }

        [Test]
        public void TestWidthUpdatesOnDrawSizeChanges()
        {
            AddStep("Shrink scroll container", () => scrollContainer.Width = 0.5f);
            AddAssert(
                "Scroll container width shrunk",
                () => scrollContainer.DrawWidth == scrollContainer.Parent!.DrawWidth / 2
            );
            AddAssert(
                "Inner container width matches scroll container",
                () => innerBox.DrawWidth == scrollContainer.DrawWidth
            );
        }

        [Test]
        public void TestWidthUpdatesOnSecondZoomSetup()
        {
            AddAssert(
                "Inner container width = 1x",
                () => innerBox.DrawWidth == scrollContainer.DrawWidth
            );
            AddStep("reload zoom", () => scrollContainer.SetupZoom(10, 10, 60));
            AddAssert(
                "Inner container width = 10x",
                () => innerBox.DrawWidth == scrollContainer.DrawWidth * 10
            );
        }

        [Test]
        public void TestZoom0()
        {
            reset();
            AddAssert(
                "Box at 0",
                () => Precision.AlmostEquals(boxQuad.TopLeft, scrollQuad.TopLeft)
            );
            AddAssert(
                "Box width = 1x",
                () => Precision.AlmostEquals(boxQuad.Size, scrollQuad.Size)
            );
        }

        [Test]
        public void TestZoom10()
        {
            reset();
            AddStep("Set zoom = 10", () => scrollContainer.Zoom = 10);
            AddAssert(
                "Box at 1/2",
                () => Precision.AlmostEquals(boxQuad.Centre, scrollQuad.Centre, 1)
            );
            AddAssert(
                "Box width = 10x",
                () => Precision.AlmostEquals(boxQuad.Size.X, 10 * scrollQuad.Size.X)
            );
        }

        [Test]
        public void TestMouseZoomInOnceOutOnce()
        {
            reset();

            // Scroll in at 0.25
            AddStep(
                "Move mouse to 0.25x",
                () =>
                    InputManager.MoveMouseTo(
                        new Vector2(
                            scrollQuad.TopLeft.X + 0.25f * scrollQuad.Size.X,
                            scrollQuad.Centre.Y
                        )
                    )
            );
            AddStep("Press alt down", () => InputManager.PressKey(Key.AltLeft));
            AddStep("Scroll by 3", () => InputManager.ScrollBy(new Vector2(0, 3)));
            AddAssert(
                "Box not at 0",
                () => !Precision.AlmostEquals(boxQuad.TopLeft, scrollQuad.TopLeft)
            );
            AddAssert(
                "Box 1/2 at 1/2",
                () =>
                    Precision.AlmostEquals(
                        boxQuad.TopLeft.X + 0.5f * boxQuad.Size.X,
                        scrollQuad.TopLeft.X + 0.5f * scrollQuad.Size.X
                    )
            );

            // Scroll out at 0.25
            AddStep("Scroll by -3", () => InputManager.ScrollBy(new Vector2(0, -3)));
            AddAssert(
                "Box at 0",
                () => Precision.AlmostEquals(boxQuad.TopLeft, scrollQuad.TopLeft)
            );
            AddAssert(
                "Box 1/2 at 1/2",
                () =>
                    Precision.AlmostEquals(
                        boxQuad.TopLeft.X + 0.5f * boxQuad.Size.X,
                        scrollQuad.TopLeft.X + 0.5f * scrollQuad.Size.X
                    )
            );
            AddStep("Release alt", () => InputManager.ReleaseKey(Key.AltLeft));
        }

        [Test]
        public void TestMouseZoomInThenScroll()
        {
            reset();

            // Scroll in at 0.25
            AddStep(
                "Move mouse to 0.25x",
                () =>
                    InputManager.MoveMouseTo(
                        new Vector2(
                            scrollQuad.TopLeft.X + 0.25f * scrollQuad.Size.X,
                            scrollQuad.Centre.Y
                        )
                    )
            );
            AddStep("Press alt down", () => InputManager.PressKey(Key.AltLeft));
            AddStep("Zoom by 3", () => InputManager.ScrollBy(new Vector2(0, 3)));
            AddStep("Release alt", () => InputManager.ReleaseKey(Key.AltLeft));

            AddStep("Scroll far left", () => InputManager.ScrollBy(new Vector2(0, 30)));
            AddUntilStep(
                "Scroll is at start",
                () => Precision.AlmostEquals(scrollQuad.TopLeft.X, boxQuad.TopLeft.X, 1)
            );

            AddStep("Scroll far right", () => InputManager.ScrollBy(new Vector2(0, -300)));
            AddUntilStep(
                "Scroll is at end",
                () => Precision.AlmostEquals(scrollQuad.TopRight.X, boxQuad.TopRight.X, 1)
            );
        }

        [Test]
        public void TestMouseZoomInTwiceOutTwice()
        {
            reset();

            AddStep("Press alt down", () => InputManager.PressKey(Key.AltLeft));

            // Scroll in at 0.25
            AddStep(
                "Move mouse to 0.25x",
                () =>
                    InputManager.MoveMouseTo(
                        new Vector2(
                            scrollQuad.TopLeft.X + 0.25f * scrollQuad.Size.X,
                            scrollQuad.Centre.Y
                        )
                    )
            );
            AddStep("Scroll by 1", () => InputManager.ScrollBy(new Vector2(0, 1)));

            // Scroll in at 0.6
            AddStep(
                "Move mouse to 0.75x",
                () =>
                    InputManager.MoveMouseTo(
                        new Vector2(
                            scrollQuad.TopLeft.X + 0.75f * scrollQuad.Size.X,
                            scrollQuad.Centre.Y
                        )
                    )
            );
            AddStep("Scroll by 1", () => InputManager.ScrollBy(new Vector2(0, 1)));
            AddAssert(
                "Box not at 0",
                () => !Precision.AlmostEquals(boxQuad.TopLeft, scrollQuad.TopLeft)
            );

            // Very hard to determine actual position, so approximate
            AddAssert(
                "Box at correct position (1)",
                () =>
                    Precision.DefinitelyBigger(
                        scrollQuad.TopLeft.X + 0.25f * scrollQuad.Size.X,
                        boxQuad.TopLeft.X + 0.25f * boxQuad.Size.X
                    )
            );
            AddAssert(
                "Box at correct position (2)",
                () =>
                    Precision.DefinitelyBigger(
                        scrollQuad.TopLeft.X + 0.6f * scrollQuad.Size.X,
                        boxQuad.TopLeft.X + 0.3f * boxQuad.Size.X
                    )
            );
            AddAssert(
                "Box at correct position (3)",
                () =>
                    Precision.DefinitelyBigger(
                        boxQuad.TopLeft.X + 0.6f * boxQuad.Size.X,
                        scrollQuad.TopLeft.X + 0.6f * scrollQuad.Size.X
                    )
            );

            // Scroll out at 0.6
            AddStep("Scroll by -1", () => InputManager.ScrollBy(new Vector2(0, -1)));

            // Scroll out at 0.25
            AddStep(
                "Move mouse to 0.25x",
                () =>
                    InputManager.MoveMouseTo(
                        new Vector2(
                            scrollQuad.TopLeft.X + 0.25f * scrollQuad.Size.X,
                            scrollQuad.Centre.Y
                        )
                    )
            );
            AddStep("Scroll by -1", () => InputManager.ScrollBy(new Vector2(0, -1)));
            AddAssert(
                "Box at 0",
                () => Precision.AlmostEquals(boxQuad.TopLeft, scrollQuad.TopLeft)
            );

            AddStep("Release alt", () => InputManager.ReleaseKey(Key.AltLeft));
        }

        private void reset()
        {
            AddStep(
                "Reset",
                () =>
                {
                    scrollContainer.Zoom = 0;
                    scrollContainer.ScrollTo(0, false);
                }
            );
        }

        private Quad scrollQuad => scrollContainer.ScreenSpaceDrawQuad;
        private Quad boxQuad => innerBox.ScreenSpaceDrawQuad;

        private partial class TestZoomableScrollContainer : ZoomableScrollContainer
        {
            public TestZoomableScrollContainer(int minimum, float maximum, float initial)
                : base(minimum, maximum, initial) { }

            public new void SetupZoom(float initial, float minimum, float maximum) =>
                base.SetupZoom(initial, minimum, maximum);
        }
    }
}
