﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Game.Overlays.Dialog;
using osu.Game.Skinning;

namespace osu.Game.Screens.Select
{
    public partial class SkinDeleteDialog : DeletionDialog
    {
        private readonly Skin skin;

        public SkinDeleteDialog(Skin skin)
        {
            this.skin = skin;
            BodyText = skin.SkinInfo.Value.Name;
        }

        [BackgroundDependencyLoader]
        private void load(SkinManager manager)
        {
            DangerousAction = () =>
            {
                manager.Delete(skin.SkinInfo.Value);
                manager.CurrentSkinInfo.SetDefault();
            };
        }
    }
}
