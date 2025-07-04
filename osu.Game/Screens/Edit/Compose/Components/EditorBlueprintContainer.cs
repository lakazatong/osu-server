// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Screens.Edit.Compose.Components
{
    public abstract partial class EditorBlueprintContainer : BlueprintContainer<HitObject>
    {
        [Resolved]
        protected EditorClock EditorClock { get; private set; }

        [Resolved]
        protected EditorBeatmap Beatmap { get; private set; }

        protected readonly HitObjectComposer Composer;

        private HitObjectUsageEventBuffer usageEventBuffer;

        protected InputManager InputManager { get; private set; }

        protected EditorBlueprintContainer(HitObjectComposer composer)
        {
            Composer = composer;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            SelectedItems.BindTo(Beatmap.SelectedHitObjects);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            InputManager = GetContainingInputManager();

            Beatmap.HitObjectAdded += AddBlueprintFor;
            Beatmap.HitObjectRemoved += RemoveBlueprintFor;
            Beatmap.SelectedHitObjects.CollectionChanged += updateSelectionLifetime;

            if (Composer != null)
            {
                foreach (var obj in Composer.HitObjects)
                    AddBlueprintFor(obj.HitObject);

                usageEventBuffer = new HitObjectUsageEventBuffer(Composer.Playfield);
                usageEventBuffer.HitObjectUsageBegan += AddBlueprintFor;
                usageEventBuffer.HitObjectUsageFinished += RemoveBlueprintFor;
                usageEventBuffer.HitObjectUsageTransferred += TransferBlueprintFor;
            }
        }

        protected override void Update()
        {
            base.Update();
            usageEventBuffer?.Update();
        }

        protected override IEnumerable<SelectionBlueprint<HitObject>> SortForMovement(
            IReadOnlyList<SelectionBlueprint<HitObject>> blueprints
        ) => blueprints.OrderBy(b => b.Item.StartTime);

        protected void ApplySnapResultTime(SnapResult result, double referenceTime)
        {
            if (!result.Time.HasValue)
                return;

            // Apply the start time at the newly snapped-to position
            double offset = result.Time.Value - referenceTime;

            if (offset != 0)
                Beatmap.PerformOnSelection(obj => obj.StartTime += offset);
        }

        protected override void AddBlueprintFor(HitObject item)
        {
            if (item is IBarLine)
                return;

            base.AddBlueprintFor(item);
        }

        /// <summary>
        /// Invoked when a <see cref="HitObject"/> has been transferred to another <see cref="DrawableHitObject"/>.
        /// </summary>
        /// <param name="hitObject">The hit object which has been assigned to a new drawable.</param>
        /// <param name="drawableObject">The new drawable that is representing the hit object.</param>
        protected virtual void TransferBlueprintFor(
            HitObject hitObject,
            DrawableHitObject drawableObject
        ) { }

        protected override void DragOperationCompleted()
        {
            base.DragOperationCompleted();

            // handle positional change etc.
            foreach (var blueprint in SelectionBlueprints)
                Beatmap.Update(blueprint.Item);
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            if (!base.OnDoubleClick(e))
                return false;

            EditorClock?.SeekSmoothlyTo(ClickedBlueprint.Item.StartTime);
            return true;
        }

        protected override SelectionBlueprintContainer CreateSelectionBlueprintContainer() =>
            new HitObjectOrderedSelectionContainer { RelativeSizeAxes = Axes.Both };

        protected override SelectionHandler<HitObject> CreateSelectionHandler() =>
            new EditorSelectionHandler();

        protected override void SelectAll()
        {
            Composer.Playfield.KeepAllAlive();
            SelectedItems.AddRange(Beatmap.HitObjects.Except(SelectedItems).ToArray());
        }

        /// <summary>
        /// Ensures that newly-selected hitobjects are kept alive
        /// and drops that keep-alive from newly-deselected objects.
        /// </summary>
        private void updateSelectionLifetime(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (HitObject newSelection in e.NewItems)
                    Composer.Playfield.SetKeepAlive(newSelection, true);
            }

            if (e.OldItems != null)
            {
                foreach (HitObject oldSelection in e.OldItems)
                    Composer.Playfield.SetKeepAlive(oldSelection, false);
            }
        }

        protected override void OnBlueprintSelected(SelectionBlueprint<HitObject> blueprint)
        {
            base.OnBlueprintSelected(blueprint);

            Composer.Playfield.SetKeepAlive(blueprint.Item, true);
        }

        protected override void OnBlueprintDeselected(SelectionBlueprint<HitObject> blueprint)
        {
            base.OnBlueprintDeselected(blueprint);

            Composer.Playfield.SetKeepAlive(blueprint.Item, false);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (Beatmap != null)
            {
                Beatmap.HitObjectAdded -= AddBlueprintFor;
                Beatmap.HitObjectRemoved -= RemoveBlueprintFor;
                Beatmap.SelectedHitObjects.CollectionChanged -= updateSelectionLifetime;
            }

            usageEventBuffer?.Dispose();
        }
    }
}
