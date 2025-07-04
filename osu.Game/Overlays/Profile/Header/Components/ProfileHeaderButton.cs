﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public abstract partial class ProfileHeaderButton : OsuHoverContainer
    {
        private readonly Box background;
        private readonly Container content;
        private readonly LoadingLayer loading;

        protected override Container<Drawable> Content => content;

        protected override IEnumerable<Drawable> EffectTargets => new[] { background };

        protected ProfileHeaderButton()
        {
            AutoSizeAxes = Axes.X;
            Height = 40;

            base.Content.Add(
                new CircularContainer
                {
                    Masking = true,
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        background = new Box { RelativeSizeAxes = Axes.Both },
                        content = new Container
                        {
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                            Padding = new MarginPadding { Horizontal = 10 },
                        },
                        loading = new LoadingLayer(true, false),
                    },
                }
            );
        }

        protected void ShowLoadingLayer()
        {
            loading.Show();
        }

        protected void HideLoadingLayer()
        {
            loading.Hide();
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            IdleColour = colourProvider.Background6;
            HoverColour = colourProvider.Background5;
        }
    }
}
