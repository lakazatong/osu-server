// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Skinning.Default;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Mania.Edit.Blueprints
{
    public abstract partial class ManiaPlacementBlueprint<T> : HitObjectPlacementBlueprint
        where T : ManiaHitObject
    {
        protected new T HitObject => (T)base.HitObject;

        [Resolved]
        private ManiaHitObjectComposer? composer { get; set; }

        private Column? column;

        public Column? Column
        {
            get => column;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                if (value == column)
                    return;

                column = value;
                HitObject.Column = column.Index;
            }
        }

        protected ManiaPlacementBlueprint(T hitObject)
            : base(hitObject)
        {
            RelativeSizeAxes = Axes.None;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            if (Column == null)
                return false;

            BeginPlacement(true);
            return true;
        }

        public override SnapResult UpdateTimeAndPosition(
            Vector2 screenSpacePosition,
            double fallbackTime
        )
        {
            var result =
                composer?.FindSnappedPositionAndTime(screenSpacePosition)
                ?? new SnapResult(screenSpacePosition, fallbackTime);

            base.UpdateTimeAndPosition(result.ScreenSpacePosition, result.Time ?? fallbackTime);

            if (result.Playfield is Column col)
            {
                // Apply an offset to better align with the visual grid.
                // This should only be applied during placement, as during selection / drag operations the movement is relative
                // to the initial point of interaction rather than the grid.
                switch (col.ScrollingInfo.Direction.Value)
                {
                    case ScrollingDirection.Down:
                        result.ScreenSpacePosition -= new Vector2(0, getNoteHeight(col) / 2);
                        break;

                    case ScrollingDirection.Up:
                        result.ScreenSpacePosition += new Vector2(0, getNoteHeight(col) / 2);
                        break;
                }

                if (PlacementActive == PlacementState.Waiting)
                    Column = col;
            }

            return result;
        }

        private float getNoteHeight(Column resultPlayfield) =>
            resultPlayfield.ToScreenSpace(new Vector2(DefaultNotePiece.NOTE_HEIGHT)).Y
            - resultPlayfield.ToScreenSpace(Vector2.Zero).Y;
    }
}
