// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mania.Mods
{
    public abstract class ManiaModWithPlayfieldCover
        : ModHidden,
            IApplicableToDrawableRuleset<ManiaHitObject>
    {
        public override Type[] IncompatibleMods => new[] { typeof(ModFlashlight<ManiaHitObject>) };

        /// <summary>
        /// The direction in which the cover should expand.
        /// </summary>
        protected abstract CoverExpandDirection ExpandDirection { get; }

        /// <summary>
        /// The relative area that should be completely covered. This does not include the fade.
        /// </summary>
        public abstract BindableNumber<float> Coverage { get; }

        public virtual void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            ManiaPlayfield maniaPlayfield = (ManiaPlayfield)drawableRuleset.Playfield;

            foreach (Column column in maniaPlayfield.Stages.SelectMany(stage => stage.Columns))
            {
                HitObjectContainer hoc = column.HitObjectContainer;
                Container hocParent = (Container)hoc.Parent!;

                hocParent.Remove(hoc, false);
                hocParent.Add(
                    CreateCover(hoc)
                        .With(c =>
                        {
                            c.RelativeSizeAxes = Axes.Both;
                            c.Direction = ExpandDirection;
                            c.Coverage.BindTo(Coverage);
                        })
                );
            }
        }

        protected virtual PlayfieldCoveringWrapper CreateCover(Drawable content) =>
            new PlayfieldCoveringWrapper(content);

        protected override void ApplyIncreasedVisibilityState(
            DrawableHitObject hitObject,
            ArmedState state
        ) { }

        protected override void ApplyNormalVisibilityState(
            DrawableHitObject hitObject,
            ArmedState state
        ) { }
    }
}
