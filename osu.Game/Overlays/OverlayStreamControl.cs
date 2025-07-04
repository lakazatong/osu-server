﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;

namespace osu.Game.Overlays
{
    public abstract partial class OverlayStreamControl<T> : TabControl<T>
    {
        protected OverlayStreamControl()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        public void Populate(List<T> streams) => streams.ForEach(AddItem);

        protected override Dropdown<T> CreateDropdown() => null;

        protected override TabItem<T> CreateTabItem(T value) =>
            CreateStreamItem(value)
                .With(item =>
                {
                    item.SelectedItem.BindTo(Current);
                });

        [NotNull]
        protected abstract OverlayStreamItem<T> CreateStreamItem(T value);

        protected override TabFillFlowContainer CreateTabFlow() =>
            new TabFillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                AllowMultiline = true,
            };

        protected override bool OnHover(HoverEvent e)
        {
            foreach (var streamBadge in TabContainer.OfType<OverlayStreamItem<T>>())
                streamBadge.UserHoveringArea = true;

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            foreach (var streamBadge in TabContainer.OfType<OverlayStreamItem<T>>())
                streamBadge.UserHoveringArea = false;

            base.OnHoverLost(e);
        }
    }
}
