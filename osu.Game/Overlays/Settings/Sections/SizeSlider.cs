﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Numerics;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.Settings.Sections
{
    /// <summary>
    /// A slider intended to show a "size" multiplier number, where 1x is 1.0.
    /// </summary>
    public partial class SizeSlider<T> : RoundedSliderBar<T>
        where T : struct, INumber<T>, IMinMaxValue<T>, IFormattable
    {
        public override LocalisableString TooltipText =>
            Current.Value.ToString(@"0.##x", NumberFormatInfo.CurrentInfo);
    }
}
