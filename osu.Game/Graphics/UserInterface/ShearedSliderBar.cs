// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Overlays;
using osuTK.Graphics;
using Vector2 = osuTK.Vector2;

namespace osu.Game.Graphics.UserInterface
{
    public partial class ShearedSliderBar<T> : OsuSliderBar<T>
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        protected readonly ShearedNub Nub;
        protected readonly Box LeftBox;
        protected readonly Box RightBox;
        private readonly Container nubContainer;

        private readonly HoverClickSounds hoverClickSounds;

        private readonly Container mainContent;

        protected virtual bool FocusIndicator => true;

        private Color4 accentColour;

        public Color4 AccentColour
        {
            get => accentColour;
            set
            {
                accentColour = value;

                // We want to slightly darken the colour for the box because the sheared slider has the boxes at the same height as the nub,
                // making the nub invisible when not hovered.
                LeftBox.Colour = value.Darken(0.1f);
            }
        }

        private Colour4 backgroundColour;

        public Color4 BackgroundColour
        {
            get => backgroundColour;
            set
            {
                backgroundColour = value;
                RightBox.Colour = value;
            }
        }

        public Color4 NubShadowColour
        {
            get => Nub.ShadowColour;
            set => Nub.ShadowColour = value;
        }

        public ShearedSliderBar()
        {
            Shear = OsuGame.SHEAR;
            Height = ShearedNub.HEIGHT;
            RangePadding = ShearedNub.EXPANDED_SIZE / 2;
            Children = new Drawable[]
            {
                mainContent = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Masking = true,
                    CornerRadius = 5,
                    Children = new Drawable[]
                    {
                        LeftBox = new Box
                        {
                            EdgeSmoothness = new Vector2(0, 0.5f),
                            RelativeSizeAxes = Axes.Y,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        RightBox = new Box
                        {
                            EdgeSmoothness = new Vector2(0, 0.5f),
                            RelativeSizeAxes = Axes.Y,
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                        },
                    },
                },
                nubContainer = new Container
                {
                    Shear = -OsuGame.SHEAR,
                    RelativeSizeAxes = Axes.Both,
                    Child = Nub =
                        new ShearedNub
                        {
                            Origin = Anchor.TopCentre,
                            RelativePositionAxes = Axes.X,
                            Current = { Value = true },
                            OnDoubleClicked = () =>
                            {
                                if (!Current.Disabled)
                                    Current.SetDefault();
                            },
                        },
                },
                hoverClickSounds = new HoverClickSounds(),
            };
        }

        [BackgroundDependencyLoader(true)]
        private void load(OverlayColourProvider? colourProvider, OsuColour colours)
        {
            AccentColour = colourProvider?.Highlight1 ?? colours.Pink;
            BackgroundColour = colourProvider?.Background5 ?? colours.PinkDarker.Darken(1);
        }

        protected override void Update()
        {
            base.Update();

            nubContainer.Padding = new MarginPadding { Horizontal = RangePadding };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Current.BindDisabledChanged(
                disabled =>
                {
                    Alpha = disabled ? 0.3f : 1;
                    hoverClickSounds.Enabled.Value = !disabled;
                },
                true
            );
        }

        protected override void OnFocus(FocusEvent e)
        {
            base.OnFocus(e);

            if (FocusIndicator)
            {
                mainContent.EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = AccentColour.Darken(1),
                    Hollow = true,
                    Radius = 2,
                };
            }
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            base.OnFocusLost(e);

            mainContent.EdgeEffect = default;
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateGlow();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateGlow();
            base.OnHoverLost(e);
        }

        protected override bool ShouldHandleAsRelativeDrag(MouseDownEvent e) =>
            Nub.ReceivePositionalInputAt(e.ScreenSpaceMouseDownPosition);

        protected override void OnDragEnd(DragEndEvent e)
        {
            updateGlow();
            base.OnDragEnd(e);
        }

        private void updateGlow()
        {
            Nub.Glowing = !Current.Disabled && (IsHovered || IsDragged);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            LeftBox.Size = new Vector2(
                Math.Clamp(
                    RangePadding
                        + Nub.DrawPosition.X
                        - Nub.DrawWidth / 2f
                        + ShearedNub.CORNER_RADIUS
                        - 0.5f,
                    0,
                    Math.Max(0, DrawWidth)
                ),
                1
            );
            RightBox.Size = new Vector2(
                Math.Clamp(
                    DrawWidth
                        - RangePadding
                        - Nub.DrawPosition.X
                        - Nub.DrawWidth / 2f
                        + ShearedNub.CORNER_RADIUS
                        - 0.5f,
                    0,
                    Math.Max(0, DrawWidth)
                ),
                1
            );
        }

        protected override void UpdateValue(float value)
        {
            Nub.MoveToX(value, 250, Easing.OutQuint);
        }
    }
}
