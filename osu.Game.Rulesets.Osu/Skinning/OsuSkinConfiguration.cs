// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Osu.Skinning
{
    public enum OsuSkinConfiguration
    {
        SliderPathRadius,
        CursorCentre,
        CursorExpand,
        CursorRotate,
        CursorTrailRotate,
        HitCircleOverlayAboveNumber,

        // ReSharper disable once IdentifierTypo
        HitCircleOverlayAboveNumer, // Some old skins will have this typo
        SpinnerFrequencyModulate,
        SpinnerNoBlink,
    }
}
