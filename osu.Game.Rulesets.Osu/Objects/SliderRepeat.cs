﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Osu.Objects
{
    public class SliderRepeat : SliderEndCircle
    {
        public double PathProgress { get; set; }

        public SliderRepeat(Slider slider)
            : base(slider) { }
    }
}
