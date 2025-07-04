﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Game.Skinning;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Objects.Types
{
    /// <summary>
    /// A HitObject that is part of a combo and has extended information about its position relative to other combo objects.
    /// </summary>
    public interface IHasComboInformation : IHasCombo
    {
        /// <summary>
        /// Bindable exposure of <see cref="IndexInCurrentCombo"/>.
        /// </summary>
        Bindable<int> IndexInCurrentComboBindable { get; }

        /// <summary>
        /// The index of this hitobject in the current combo.
        /// </summary>
        int IndexInCurrentCombo { get; set; }

        /// <summary>
        /// Bindable exposure of <see cref="ComboIndex"/>.
        /// </summary>
        Bindable<int> ComboIndexBindable { get; }

        /// <summary>
        /// The index of this combo in relation to the beatmap.
        ///
        /// In other words, this is incremented by 1 each time a <see cref="NewCombo"/> is reached.
        /// </summary>
        int ComboIndex { get; set; }

        /// <summary>
        /// Bindable exposure of <see cref="ComboIndexWithOffsets"/>.
        /// </summary>
        Bindable<int> ComboIndexWithOffsetsBindable { get; }

        /// <summary>
        /// The index of this combo in relation to the beatmap, with all aggregate <see cref="IHasCombo.ComboOffset"/>s applied.
        /// This should be used instead of <see cref="ComboIndex"/> only when retrieving combo colours from the beatmap's skin.
        /// </summary>
        int ComboIndexWithOffsets { get; set; }

        /// <summary>
        /// Whether the HitObject starts a new combo.
        /// </summary>
        new bool NewCombo { get; set; }

        /// <inheritdoc cref="IHasCombo.ComboOffset"/>
        new int ComboOffset { get; set; }

        /// <summary>
        /// Bindable exposure of <see cref="LastInCombo"/>.
        /// </summary>
        Bindable<bool> LastInComboBindable { get; }

        /// <summary>
        /// Whether this is the last object in the current combo.
        /// </summary>
        bool LastInCombo { get; set; }

        /// <summary>
        /// Retrieves the colour of the combo described by this <see cref="IHasComboInformation"/> object.
        /// </summary>
        /// <param name="skin">The skin to retrieve the combo colour from, if wanted.</param>
        Color4 GetComboColour(ISkin skin) => GetSkinComboColour(this, skin, ComboIndex);

        /// <summary>
        /// Retrieves the colour of the combo described by a given <see cref="IHasComboInformation"/> object from a given skin.
        /// </summary>
        /// <param name="combo">The combo information, should be <c>this</c>.</param>
        /// <param name="skin">The skin to retrieve the combo colour from.</param>
        /// <param name="comboIndex">The index to retrieve the combo colour with.</param>
        /// <returns></returns>
        protected static Color4 GetSkinComboColour(
            IHasComboInformation combo,
            ISkin skin,
            int comboIndex
        )
        {
            return skin.GetConfig<SkinComboColourLookup, Color4>(
                    new SkinComboColourLookup(comboIndex, combo)
                )?.Value ?? Color4.White;
        }

        /// <summary>
        /// Given the previous object in the beatmap, update relevant combo information.
        /// </summary>
        /// <param name="lastObj">The previous hitobject, or null if this is the first object in the beatmap.</param>
        void UpdateComboInformation(IHasComboInformation? lastObj)
        {
            int index = lastObj?.ComboIndex ?? 0;
            int indexWithOffsets = lastObj?.ComboIndexWithOffsets ?? 0;
            int inCurrentCombo = (lastObj?.IndexInCurrentCombo + 1) ?? 0;

            if (NewCombo || lastObj == null)
            {
                inCurrentCombo = 0;
                index++;
                indexWithOffsets += ComboOffset + 1;

                if (lastObj != null)
                    lastObj.LastInCombo = true;
            }

            ComboIndex = index;
            ComboIndexWithOffsets = indexWithOffsets;
            IndexInCurrentCombo = inCurrentCombo;
        }
    }
}
