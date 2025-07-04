// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Rulesets.Osu.UI.Cursor;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.UI
{
    public partial class OsuResumeOverlay : ResumeOverlay
    {
        private Container cursorScaleContainer = null!;
        private OsuClickToResumeCursor clickToResumeCursor = null!;

        private OsuCursorContainer? localCursorContainer;

        public override CursorContainer? LocalCursor =>
            State.Value == Visibility.Visible ? localCursorContainer : null;

        protected override LocalisableString Message => "Click the orange cursor to resume";

        [Resolved]
        private DrawableRuleset? drawableRuleset { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            OsuResumeOverlayInputBlocker? inputBlocker = null;

            var drawableOsuRuleset = (DrawableOsuRuleset?)drawableRuleset;

            if (drawableOsuRuleset != null)
            {
                var osuPlayfield = drawableOsuRuleset.Playfield;
                osuPlayfield.AttachResumeOverlayInputBlocker(
                    inputBlocker = new OsuResumeOverlayInputBlocker()
                );
            }

            Add(
                cursorScaleContainer = new Container
                {
                    Child = clickToResumeCursor =
                        new OsuClickToResumeCursor
                        {
                            ResumeRequested = action =>
                            {
                                // since the user had to press a button to tap the resume cursor,
                                // block that press event from potentially reaching a hit circle that's behind the cursor.
                                // we cannot do this from OsuClickToResumeCursor directly since we're in a different input manager tree than the gameplay one,
                                // so we rely on a dedicated input blocking component that's implanted in there to do that for us.
                                // note this only matters when the user didn't pause while they were holding the same key that they are resuming with.
                                if (
                                    inputBlocker != null
                                    && !drawableOsuRuleset
                                        .AsNonNull()
                                        .KeyBindingInputManager.PressedActions.Contains(action)
                                )
                                    inputBlocker.BlockNextPress = true;

                                Resume();
                            },
                        },
                }
            );
        }

        protected override void PopIn()
        {
            // Can't display if the cursor is outside the window.
            if (
                GameplayCursor.LastFrameState == Visibility.Hidden
                || drawableRuleset?.Contains(GameplayCursor.ActiveCursor.ScreenSpaceDrawQuad.Centre)
                    == false
            )
            {
                Resume();
                return;
            }

            base.PopIn();

            GameplayCursor.ActiveCursor.Hide();
            cursorScaleContainer.Position = ToLocalSpace(
                GameplayCursor.ActiveCursor.ScreenSpaceDrawQuad.Centre
            );
            clickToResumeCursor.Appear();

            if (localCursorContainer == null)
                Add(localCursorContainer = new OsuCursorContainer());
        }

        protected override void PopOut()
        {
            base.PopOut();

            localCursorContainer?.Expire();
            localCursorContainer = null;
            GameplayCursor?.ActiveCursor.Show();
        }

        protected override bool OnHover(HoverEvent e) => true;

        public partial class OsuClickToResumeCursor : OsuCursor, IKeyBindingHandler<OsuAction>
        {
            public override bool HandlePositionalInput => true;

            public Action<OsuAction>? ResumeRequested;
            private Container scaleTransitionContainer = null!;

            public OsuClickToResumeCursor()
            {
                RelativePositionAxes = Axes.Both;
            }

            protected override Container CreateCursorContent() =>
                scaleTransitionContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Child = base.CreateCursorContent(),
                };

            protected override float CalculateCursorScale() =>
                // Force minimum cursor size so it's easily clickable
                Math.Max(1f, base.CalculateCursorScale());

            protected override bool OnHover(HoverEvent e)
            {
                updateColour();
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateColour();
                base.OnHoverLost(e);
            }

            public bool OnPressed(KeyBindingPressEvent<OsuAction> e)
            {
                switch (e.Action)
                {
                    case OsuAction.LeftButton:
                    case OsuAction.RightButton:
                        if (!IsHovered)
                            return false;

                        scaleTransitionContainer.ScaleTo(2, TRANSITION_TIME, Easing.OutQuint);
                        ResumeRequested?.Invoke(e.Action);
                        return true;
                }

                return false;
            }

            public void OnReleased(KeyBindingReleaseEvent<OsuAction> e) { }

            public void Appear() =>
                Schedule(() =>
                {
                    updateColour();

                    // importantly, we perform the scale transition on an underlying container rather than the whole cursor
                    // to prevent attempts of abuse by the scale change in the cursor's hitbox (see: https://github.com/ppy/osu/issues/26477).
                    scaleTransitionContainer.ScaleTo(4).Then().ScaleTo(1, 1000, Easing.OutQuint);
                });

            private void updateColour()
            {
                this.FadeColour(IsHovered ? Color4.White : Color4.Orange, 400, Easing.OutQuint);
            }
        }

        public partial class OsuResumeOverlayInputBlocker : Drawable, IKeyBindingHandler<OsuAction>
        {
            public bool BlockNextPress;

            public OsuResumeOverlayInputBlocker()
            {
                RelativeSizeAxes = Axes.Both;
                Depth = float.MinValue;
            }

            public bool OnPressed(KeyBindingPressEvent<OsuAction> e)
            {
                bool block = BlockNextPress;
                BlockNextPress = false;
                return block;
            }

            public void OnReleased(KeyBindingReleaseEvent<OsuAction> e) { }
        }
    }
}
