﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Localisation;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Graphics.UserInterface
{
    public partial class OsuPasswordTextBox : OsuTextBox
    {
        protected override Drawable GetDrawableCharacter(char c) =>
            new FallingDownContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = new PasswordMaskChar(FontSize),
            };

        protected override bool AllowUniqueCharacterSamples => false;

        private readonly CapsWarning warning;

        [Resolved]
        private GameHost host { get; set; } = null!;

        public OsuPasswordTextBox()
        {
            InputProperties = new TextInputProperties(TextInputType.Password, false);

            Add(
                warning = new CapsWarning
                {
                    Size = new Vector2(20),
                    Origin = Anchor.CentreRight,
                    Anchor = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 10 },
                    Alpha = 0,
                }
            );
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.CapsLock)
                updateCapsWarning(host.CapsLockEnabled);
            return base.OnKeyDown(e);
        }

        protected override void OnFocus(FocusEvent e)
        {
            updateCapsWarning(host.CapsLockEnabled);
            base.OnFocus(e);
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            updateCapsWarning(false);
            base.OnFocusLost(e);
        }

        private void updateCapsWarning(bool visible) =>
            warning.FadeTo(visible ? 1 : 0, 250, Easing.OutQuint);

        public partial class PasswordMaskChar : Container
        {
            private readonly CircularContainer circle;

            public PasswordMaskChar(float size)
            {
                Size = new Vector2(size / 2, size);
                Children = new[]
                {
                    circle = new CircularContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        Alpha = 0,
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.8f, 0),
                        Children = new[]
                        {
                            new Box { Colour = Color4.White, RelativeSizeAxes = Axes.Both },
                        },
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                circle.FadeIn(500, Easing.OutQuint);
                circle.ResizeTo(new Vector2(0.8f), 500, Easing.OutQuint);
            }
        }

        private partial class CapsWarning : SpriteIcon, IHasTooltip
        {
            public LocalisableString TooltipText => CommonStrings.CapsLockIsActive;

            public CapsWarning()
            {
                Icon = FontAwesome.Solid.ExclamationTriangle;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colour)
            {
                Colour = colour.YellowLight;
            }
        }
    }
}
