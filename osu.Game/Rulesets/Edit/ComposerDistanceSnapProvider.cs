// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Overlays.OSD;
using osu.Game.Overlays.Settings.Sections;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.TernaryButtons;

namespace osu.Game.Rulesets.Edit
{
    public abstract partial class ComposerDistanceSnapProvider
        : Component,
            IDistanceSnapProvider,
            IScrollBindingHandler<GlobalAction>
    {
        private const float adjust_step = 0.1f;

        public BindableDouble DistanceSpacingMultiplier { get; } =
            new BindableDouble(1.0)
            {
                MinValue = 0.1,
                MaxValue = 6.0,
                Precision = 0.01,
            };

        Bindable<double> IDistanceSnapProvider.DistanceSpacingMultiplier =>
            DistanceSpacingMultiplier;

        private ExpandableSlider<double, SizeSlider<double>> distanceSpacingSlider = null!;
        private ExpandableButton currentDistanceSpacingButton = null!;

        [Resolved]
        private Playfield playfield { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        protected EditorBeatmap EditorBeatmap { get; private set; } = null!;

        [Resolved]
        private IBeatSnapProvider beatSnapProvider { get; set; } = null!;

        [Resolved]
        private OnScreenDisplay? onScreenDisplay { get; set; }

        public readonly Bindable<TernaryState> DistanceSnapToggle = new Bindable<TernaryState>();

        private bool distanceSnapMomentary;
        private TernaryState? distanceSnapStateBeforeMomentaryToggle;

        private EditorToolboxGroup? toolboxGroup;

        public void AttachToToolbox(ExpandingToolboxContainer toolboxContainer)
        {
            if (toolboxGroup != null)
                throw new InvalidOperationException(
                    $"{nameof(AttachToToolbox)} may be called only once for a single {nameof(ComposerDistanceSnapProvider)} instance."
                );

            toolboxContainer.Add(
                toolboxGroup = new EditorToolboxGroup("snapping")
                {
                    Name = "snapping",
                    Alpha = DistanceSpacingMultiplier.Disabled ? 0 : 1,
                    Children = new Drawable[]
                    {
                        distanceSpacingSlider = new ExpandableSlider<double, SizeSlider<double>>
                        {
                            KeyboardStep = adjust_step,
                            // Manual binding in LoadComplete to handle one-way event flow.
                            Current = DistanceSpacingMultiplier.GetUnboundCopy(),
                        },
                        currentDistanceSpacingButton = new ExpandableButton
                        {
                            Action = () =>
                            {
                                (HitObject before, HitObject after)? objects =
                                    getObjectsOnEitherSideOfCurrentTime();

                                Debug.Assert(objects != null);

                                DistanceSpacingMultiplier.Value = ReadCurrentDistanceSnap(
                                    objects.Value.before,
                                    objects.Value.after
                                );
                                DistanceSnapToggle.Value = TernaryState.True;
                            },
                            RelativeSizeAxes = Axes.X,
                        },
                    },
                }
            );

            DistanceSpacingMultiplier.Value = EditorBeatmap.DistanceSpacing;
            DistanceSpacingMultiplier.BindValueChanged(
                multiplier =>
                {
                    distanceSpacingSlider.ContractedLabelText =
                        $"D. S. ({multiplier.NewValue:0.##x})";
                    distanceSpacingSlider.ExpandedLabelText =
                        $"Distance Spacing ({multiplier.NewValue:0.##x})";

                    if (multiplier.NewValue != multiplier.OldValue)
                        onScreenDisplay?.Display(
                            new DistanceSpacingToast(
                                multiplier.NewValue.ToLocalisableString(@"0.##x"),
                                multiplier
                            )
                        );

                    EditorBeatmap.DistanceSpacing = multiplier.NewValue;
                },
                true
            );

            DistanceSpacingMultiplier.BindDisabledChanged(
                disabled => distanceSpacingSlider.Alpha = disabled ? 0 : 1,
                true
            );

            // Manual binding to handle enabling distance spacing when the slider is interacted with.
            distanceSpacingSlider.Current.BindValueChanged(spacing =>
            {
                DistanceSpacingMultiplier.Value = spacing.NewValue;
                DistanceSnapToggle.Value = TernaryState.True;
            });
            DistanceSpacingMultiplier.BindValueChanged(spacing =>
                distanceSpacingSlider.Current.Value = spacing.NewValue
            );
        }

        private (HitObject before, HitObject after)? getObjectsOnEitherSideOfCurrentTime()
        {
            HitObject? lastBefore = null;

            foreach (var entry in playfield.HitObjectContainer.AliveEntries)
            {
                double objTime = entry.Value.HitObject.StartTime;

                if (objTime >= editorClock.CurrentTime)
                    continue;

                if (lastBefore == null || objTime > lastBefore.StartTime)
                    lastBefore = entry.Value.HitObject;
            }

            if (lastBefore == null)
                return null;

            HitObject? firstAfter = null;

            foreach (var entry in playfield.HitObjectContainer.AliveEntries)
            {
                double objTime = entry.Value.HitObject.StartTime;

                if (objTime < editorClock.CurrentTime)
                    continue;

                if (firstAfter == null || objTime < firstAfter.StartTime)
                    firstAfter = entry.Value.HitObject;
            }

            if (firstAfter == null)
                return null;

            if (lastBefore == firstAfter)
                return null;

            return (lastBefore, firstAfter);
        }

        public abstract double ReadCurrentDistanceSnap(HitObject before, HitObject after);

        protected override void Update()
        {
            base.Update();

            (HitObject before, HitObject after)? objects = getObjectsOnEitherSideOfCurrentTime();

            double currentSnap =
                objects == null
                    ? 0
                    : ReadCurrentDistanceSnap(objects.Value.before, objects.Value.after);

            if (currentSnap > DistanceSpacingMultiplier.MinValue)
            {
                currentDistanceSpacingButton.Enabled.Value =
                    currentDistanceSpacingButton.Expanded.Value
                    && !DistanceSpacingMultiplier.Disabled
                    && !Precision.AlmostEquals(
                        currentSnap,
                        DistanceSpacingMultiplier.Value,
                        DistanceSpacingMultiplier.Precision / 2
                    );
                currentDistanceSpacingButton.ContractedLabelText = $"current {currentSnap:N2}x";
                currentDistanceSpacingButton.ExpandedLabelText = $"Use current ({currentSnap:N2}x)";
            }
            else
            {
                currentDistanceSpacingButton.Enabled.Value = false;
                currentDistanceSpacingButton.ContractedLabelText = string.Empty;
                currentDistanceSpacingButton.ExpandedLabelText = "Use current (unavailable)";
            }
        }

        public IEnumerable<DrawableTernaryButton> CreateTernaryButtons() =>
            new[]
            {
                new DrawableTernaryButton
                {
                    Current = DistanceSnapToggle,
                    Description = "Distance Snap",
                    CreateIcon = () => new SpriteIcon { Icon = OsuIcon.EditorDistanceSnap },
                },
            };

        public void HandleToggleViaKey(KeyboardEvent key)
        {
            bool altPressed = key.AltPressed;

            if (altPressed && !distanceSnapMomentary)
            {
                distanceSnapStateBeforeMomentaryToggle = DistanceSnapToggle.Value;
                DistanceSnapToggle.Value =
                    DistanceSnapToggle.Value == TernaryState.False
                        ? TernaryState.True
                        : TernaryState.False;
                distanceSnapMomentary = true;
            }

            if (!altPressed && distanceSnapMomentary)
            {
                Debug.Assert(distanceSnapStateBeforeMomentaryToggle != null);
                DistanceSnapToggle.Value = distanceSnapStateBeforeMomentaryToggle.Value;
                distanceSnapStateBeforeMomentaryToggle = null;
                distanceSnapMomentary = false;
            }
        }

        public virtual bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.EditorIncreaseDistanceSpacing:
                case GlobalAction.EditorDecreaseDistanceSpacing:
                    return AdjustDistanceSpacing(e.Action, adjust_step);
            }

            return false;
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<GlobalAction> e) { }

        public bool OnScroll(KeyBindingScrollEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.EditorIncreaseDistanceSpacing:
                case GlobalAction.EditorDecreaseDistanceSpacing:
                    return AdjustDistanceSpacing(e.Action, e.ScrollAmount * adjust_step);
            }

            return false;
        }

        protected virtual bool AdjustDistanceSpacing(GlobalAction action, float amount)
        {
            if (DistanceSpacingMultiplier.Disabled)
                return false;

            if (action == GlobalAction.EditorIncreaseDistanceSpacing)
                DistanceSpacingMultiplier.Value += amount;
            else if (action == GlobalAction.EditorDecreaseDistanceSpacing)
                DistanceSpacingMultiplier.Value -= amount;

            DistanceSnapToggle.Value = TernaryState.True;
            return true;
        }

        #region IDistanceSnapProvider

        public virtual float GetBeatSnapDistance(IHasSliderVelocity? withVelocity = null)
        {
            return (float)(
                100
                * (withVelocity?.SliderVelocityMultiplier ?? 1)
                * EditorBeatmap.Difficulty.SliderMultiplier
                * 1
                / beatSnapProvider.BeatDivisor
            );
        }

        public virtual float DurationToDistance(
            double duration,
            double timingReference,
            IHasSliderVelocity? withVelocity = null
        )
        {
            double beatLength = beatSnapProvider.GetBeatLengthAtTime(timingReference);
            return (float)(duration / beatLength * GetBeatSnapDistance(withVelocity));
        }

        public virtual double DistanceToDuration(
            float distance,
            double timingReference,
            IHasSliderVelocity? withVelocity = null
        )
        {
            double beatLength = beatSnapProvider.GetBeatLengthAtTime(timingReference);
            return distance / GetBeatSnapDistance(withVelocity) * beatLength;
        }

        public virtual float FindSnappedDistance(
            float distance,
            double snapReferenceTime,
            IHasSliderVelocity? withVelocity = null
        )
        {
            double actualDuration =
                snapReferenceTime + DistanceToDuration(distance, snapReferenceTime, withVelocity);

            double snappedTime = beatSnapProvider.SnapTime(actualDuration, snapReferenceTime);

            double beatLength = beatSnapProvider.GetBeatLengthAtTime(snapReferenceTime);

            // we don't want to exceed the actual duration and snap to a point in the future.
            // as we are snapping to beat length via SnapTime (which will round-to-nearest), check for snapping in the forward direction and reverse it.
            if (snappedTime > actualDuration + 1)
                snappedTime -= beatLength;

            return DurationToDistance(
                snappedTime - snapReferenceTime,
                snapReferenceTime,
                withVelocity
            );
        }

        #endregion

        private partial class DistanceSpacingToast : Toast
        {
            private readonly ValueChangedEvent<double> change;

            public DistanceSpacingToast(LocalisableString value, ValueChangedEvent<double> change)
                : base(getAction(change).GetLocalisableDescription(), value, string.Empty)
            {
                this.change = change;
            }

            [BackgroundDependencyLoader]
            private void load(RealmKeyBindingStore keyBindingStore)
            {
                ShortcutText.Text = keyBindingStore
                    .GetBindingsStringFor(getAction(change))
                    .ToUpper();
            }

            private static GlobalAction getAction(ValueChangedEvent<double> change) =>
                change.NewValue - change.OldValue > 0
                    ? GlobalAction.EditorIncreaseDistanceSpacing
                    : GlobalAction.EditorDecreaseDistanceSpacing;
        }
    }
}
