// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Catch.Edit.Blueprints.Components;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Catch.Edit.Blueprints
{
    public partial class JuiceStreamSelectionBlueprint : CatchSelectionBlueprint<JuiceStream>
    {
        public override Quad SelectionQuad =>
            HitObjectContainer.ToScreenSpace(
                getBoundingBox().Offset(new Vector2(0, HitObjectContainer.DrawHeight))
            );

        public override MenuItem[] ContextMenuItems => getContextMenuItems().ToArray();

        private float minNestedX;
        private float maxNestedX;

        private readonly ScrollingPath scrollingPath;

        private readonly NestedOutlineContainer nestedOutlineContainer;

        private readonly Cached pathCache = new Cached();

        private readonly SelectionEditablePath editablePath;

        /// <summary>
        /// The <see cref="JuiceStreamPath.InvalidationID"/> of the <see cref="JuiceStreamPath"/> corresponding the current <see cref="SliderPath"/> of the hit object.
        /// When the path is edited, the change is detected and the <see cref="SliderPath"/> of the hit object is updated.
        /// </summary>
        private int lastEditablePathId = -1;

        /// <summary>
        /// The <see cref="SliderPath.Version"/> of the current <see cref="SliderPath"/> of the hit object.
        /// When the <see cref="SliderPath"/> of the hit object is changed by external means, the change is detected and the <see cref="JuiceStreamPath"/> is re-initialized.
        /// </summary>
        private int lastSliderPathVersion = -1;

        private Vector2 rightMouseDownPosition;

        [Resolved]
        private EditorBeatmap? editorBeatmap { get; set; }

        [Resolved]
        private IEditorChangeHandler? changeHandler { get; set; }

        [Resolved]
        private BindableBeatDivisor? beatDivisor { get; set; }

        public JuiceStreamSelectionBlueprint(JuiceStream hitObject)
            : base(hitObject)
        {
            InternalChildren = new Drawable[]
            {
                scrollingPath = new ScrollingPath(),
                nestedOutlineContainer = new NestedOutlineContainer(),
                editablePath = new SelectionEditablePath(hitObject, positionToTime),
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            HitObject.DefaultsApplied += onDefaultsApplied;
            computeObjectBounds();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsSelected)
                return;

            if (editablePath.PathId != lastEditablePathId)
                updateHitObjectFromPath();

            Vector2 startPosition = CatchHitObjectUtils.GetStartPosition(
                HitObjectContainer,
                HitObject
            );
            editablePath.Position =
                nestedOutlineContainer.Position =
                scrollingPath.Position =
                    startPosition;

            editablePath.UpdateFrom(HitObjectContainer, HitObject);

            if (pathCache.IsValid)
                return;

            scrollingPath.UpdatePathFrom(HitObjectContainer, HitObject);
            nestedOutlineContainer.UpdateNestedObjectsFrom(HitObjectContainer, HitObject);

            pathCache.Validate();
        }

        protected override void OnSelected()
        {
            initializeJuiceStreamPath();
            base.OnSelected();
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (!IsSelected)
                return base.OnMouseDown(e);

            switch (e.Button)
            {
                case MouseButton.Left when e.ControlPressed:
                    editablePath.AddVertex(
                        editablePath.ToRelativePosition(e.ScreenSpaceMouseDownPosition)
                    );
                    return true;

                case MouseButton.Right:
                    // Record the mouse position to be used in the "add vertex" action.
                    rightMouseDownPosition = editablePath.ToRelativePosition(
                        e.ScreenSpaceMouseDownPosition
                    );
                    break;
            }

            return base.OnMouseDown(e);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!IsSelected)
                return false;

            if (e.Key == Key.F && e.ControlPressed && e.ShiftPressed)
            {
                convertToStream();
                return true;
            }

            return false;
        }

        private void onDefaultsApplied(HitObject _)
        {
            computeObjectBounds();
            pathCache.Invalidate();

            if (lastSliderPathVersion != HitObject.Path.Version.Value)
                initializeJuiceStreamPath();
        }

        private void computeObjectBounds()
        {
            minNestedX =
                HitObject.NestedHitObjects.OfType<CatchHitObject>().Min(nested => nested.OriginalX)
                - HitObject.OriginalX;
            maxNestedX =
                HitObject.NestedHitObjects.OfType<CatchHitObject>().Max(nested => nested.OriginalX)
                - HitObject.OriginalX;
        }

        private RectangleF getBoundingBox()
        {
            float left = HitObject.OriginalX + minNestedX;
            float right = HitObject.OriginalX + maxNestedX;
            float top = HitObjectContainer.PositionAtTime(HitObject.EndTime);
            float bottom = HitObjectContainer.PositionAtTime(HitObject.StartTime);
            float objectRadius = CatchHitObject.OBJECT_RADIUS * HitObject.Scale;
            return new RectangleF(left, top, right - left, bottom - top).Inflate(objectRadius);
        }

        private double positionToTime(float relativeYPosition)
        {
            double time = HitObjectContainer.TimeAtPosition(relativeYPosition, HitObject.StartTime);
            return time - HitObject.StartTime;
        }

        private void initializeJuiceStreamPath()
        {
            editablePath.InitializeFromHitObject(HitObject);

            // Record the current ID to update the hit object only when a change is made to the path.
            lastEditablePathId = editablePath.PathId;
            lastSliderPathVersion = HitObject.Path.Version.Value;
        }

        private void updateHitObjectFromPath()
        {
            editablePath.UpdateHitObjectFromPath(HitObject);
            editorBeatmap?.Update(HitObject);

            lastEditablePathId = editablePath.PathId;
            lastSliderPathVersion = HitObject.Path.Version.Value;
        }

        // duplicated in `SliderSelectionBlueprint.convertToStream()`
        // consider extracting common helper when applying changes here
        private void convertToStream()
        {
            if (editorBeatmap == null || beatDivisor == null)
                return;

            var timingPoint = editorBeatmap.ControlPointInfo.TimingPointAt(HitObject.StartTime);
            double streamSpacing = timingPoint.BeatLength / beatDivisor.Value;

            changeHandler?.BeginChange();

            int i = 0;
            double time = HitObject.StartTime;

            while (!Precision.DefinitelyBigger(time, HitObject.GetEndTime(), 1))
            {
                // positionWithRepeats is a fractional number in the range of [0, HitObject.SpanCount()]
                // and indicates how many fractional spans of a slider have passed up to time.
                double positionWithRepeats =
                    (time - HitObject.StartTime) / HitObject.Duration * HitObject.SpanCount();
                double pathPosition = positionWithRepeats - (int)positionWithRepeats;
                // every second span is in the reverse direction - need to reverse the path position.
                if (positionWithRepeats % 2 >= 1)
                    pathPosition = 1 - pathPosition;

                float fruitXValue = HitObject.OriginalX + HitObject.Path.PositionAt(pathPosition).X;

                editorBeatmap.Add(
                    new Fruit
                    {
                        StartTime = time,
                        OriginalX = fruitXValue,
                        NewCombo = i == 0 && HitObject.NewCombo,
                        Samples = HitObject.Samples.Select(s => s.With()).ToList(),
                    }
                );

                i += 1;
                time = HitObject.StartTime + i * streamSpacing;
            }

            editorBeatmap.Remove(HitObject);

            changeHandler?.EndChange();
        }

        private IEnumerable<MenuItem> getContextMenuItems()
        {
            yield return new OsuMenuItem(
                "Add vertex",
                MenuItemType.Standard,
                () =>
                {
                    editablePath.AddVertex(rightMouseDownPosition);
                }
            )
            {
                Hotkey = new Hotkey(new KeyCombination(InputKey.Control, InputKey.MouseLeft)),
            };

            yield return new OsuMenuItem(
                "Convert to stream",
                MenuItemType.Destructive,
                convertToStream
            )
            {
                Hotkey = new Hotkey(
                    new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.F)
                ),
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            HitObject.DefaultsApplied -= onDefaultsApplied;
        }
    }
}
