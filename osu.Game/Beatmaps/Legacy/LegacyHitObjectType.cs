﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Beatmaps.Legacy
{
    [Flags]
    public enum LegacyHitObjectType
    {
        Circle = 1,
        Slider = 1 << 1,
        NewCombo = 1 << 2,
        Spinner = 1 << 3,
        ComboOffset = (1 << 4) | (1 << 5) | (1 << 6),
        Hold = 1 << 7,

        ObjectTypes = Circle | Slider | Spinner | Hold,
    }
}
