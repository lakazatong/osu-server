﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Graphics.UserInterfaceV2
{
    public partial class LabelledSwitchButton : LabelledComponent<SwitchButton, bool>
    {
        public LabelledSwitchButton()
            : base(true) { }

        protected override SwitchButton CreateComponent() => new SwitchButton();
    }
}
