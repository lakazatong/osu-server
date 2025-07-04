﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Input;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Configuration;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Replays;
using osu.Game.Rulesets.Mania.Skinning;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Mania.UI
{
    public partial class DrawableManiaRuleset : DrawableScrollingRuleset<ManiaHitObject>
    {
        /// <summary>
        /// The minimum time range. This occurs at a <see cref="ManiaRulesetSetting.ScrollSpeed"/> of 40.
        /// </summary>
        public const double MIN_TIME_RANGE = 290;

        /// <summary>
        /// The maximum time range. This occurs with a <see cref="ManiaRulesetSetting.ScrollSpeed"/> of 1.
        /// </summary>
        public const double MAX_TIME_RANGE = 11485;

        public new ManiaPlayfield Playfield => (ManiaPlayfield)base.Playfield;

        public new ManiaBeatmap Beatmap => (ManiaBeatmap)base.Beatmap;

        public IEnumerable<BarLine> BarLines;

        public override bool RequiresPortraitOrientation =>
            Beatmap.Stages.Count == 1 && mobileLayout.Value == ManiaMobileLayout.Portrait;

        protected override bool RelativeScaleBeatLengths => true;

        protected new ManiaRulesetConfigManager Config => (ManiaRulesetConfigManager)base.Config;

        private readonly Bindable<ManiaScrollingDirection> configDirection =
            new Bindable<ManiaScrollingDirection>();
        private readonly BindableDouble configScrollSpeed = new BindableDouble();
        private readonly Bindable<ManiaMobileLayout> mobileLayout =
            new Bindable<ManiaMobileLayout>();

        public double TargetTimeRange { get; protected set; }

        private double currentTimeRange;

        // Stores the current speed adjustment active in gameplay.
        private readonly Track speedAdjustmentTrack = new TrackVirtual(0);

        private ISkinSource currentSkin = null!;

        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        public DrawableManiaRuleset(
            Ruleset ruleset,
            IBeatmap beatmap,
            IReadOnlyList<Mod>? mods = null
        )
            : base(ruleset, beatmap, mods)
        {
            BarLines = new BarLineGenerator<BarLine>(Beatmap).BarLines;

            TimeRange.MinValue = 1;
            TimeRange.MaxValue = MAX_TIME_RANGE;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource source)
        {
            currentSkin = source;
            currentSkin.SourceChanged += onSkinChange;
            skinChanged();

            foreach (var mod in Mods.OfType<IApplicableToTrack>())
                mod.ApplyToTrack(speedAdjustmentTrack);

            bool isForCurrentRuleset = Beatmap.BeatmapInfo.Ruleset.Equals(Ruleset.RulesetInfo);

            foreach (var p in ControlPoints)
            {
                // Mania doesn't care about global velocity
                p.Velocity = 1;
                p.BaseBeatLength *= Beatmap.Difficulty.SliderMultiplier;

                // For non-mania beatmap, speed changes should only happen through timing points
                if (!isForCurrentRuleset)
                    p.EffectPoint = new EffectControlPoint();
            }

            BarLines.ForEach(Playfield.Add);

            Config.BindWith(ManiaRulesetSetting.ScrollDirection, configDirection);
            configDirection.BindValueChanged(
                direction => Direction.Value = (ScrollingDirection)direction.NewValue,
                true
            );

            Config.BindWith(ManiaRulesetSetting.ScrollSpeed, configScrollSpeed);
            configScrollSpeed.BindValueChanged(speed =>
            {
                if (!AllowScrollSpeedAdjustment)
                    return;

                TargetTimeRange = ComputeScrollTime(speed.NewValue);
            });

            TimeRange.Value =
                TargetTimeRange =
                currentTimeRange =
                    ComputeScrollTime(configScrollSpeed.Value);

            Config.BindWith(ManiaRulesetSetting.MobileLayout, mobileLayout);
            mobileLayout.BindValueChanged(_ => updateMobileLayout(), true);
        }

        private ManiaTouchInputArea? touchInputArea;

        private void updateMobileLayout()
        {
            switch (mobileLayout.Value)
            {
                case ManiaMobileLayout.LandscapeWithOverlay:
                    KeyBindingInputManager.Add(touchInputArea = new ManiaTouchInputArea(this));
                    break;

                default:
                    if (touchInputArea != null)
                        KeyBindingInputManager.Remove(touchInputArea, true);

                    touchInputArea = null;
                    break;
            }
        }

        protected override void AdjustScrollSpeed(int amount) => configScrollSpeed.Value += amount;

        protected override void Update()
        {
            base.Update();
            updateTimeRange();
        }

        private ScheduledDelegate? pendingSkinChange;
        private float hitPosition;

        private void onSkinChange()
        {
            // schedule required to avoid calls after disposed.
            // note that this has the side-effect of components only performing a skin change when they are alive.
            pendingSkinChange?.Cancel();
            pendingSkinChange = Scheduler.Add(skinChanged);
        }

        private void skinChanged()
        {
            hitPosition =
                currentSkin
                    .GetConfig<ManiaSkinConfigurationLookup, float>(
                        new ManiaSkinConfigurationLookup(
                            LegacyManiaSkinConfigurationLookups.HitPosition
                        )
                    )
                    ?.Value ?? Stage.HIT_TARGET_POSITION;

            pendingSkinChange = null;
        }

        private void updateTimeRange()
        {
            const float length_to_default_hit_position =
                768 - LegacyManiaSkinConfiguration.DEFAULT_HIT_POSITION;
            float lengthToHitPosition = 768 - hitPosition;

            // This scaling factor preserves the scroll speed as the scroll length varies from changes to the hit position.
            float scale = lengthToHitPosition / length_to_default_hit_position;

            // we're intentionally using the game host's update clock here to decouple the time range tween from the gameplay clock (which can be arbitrarily paused, or even rewinding)
            currentTimeRange = Interpolation.DampContinuously(
                currentTimeRange,
                TargetTimeRange,
                50,
                gameHost.UpdateThread.Clock.ElapsedFrameTime
            );
            TimeRange.Value =
                currentTimeRange
                * speedAdjustmentTrack.AggregateTempo.Value
                * speedAdjustmentTrack.AggregateFrequency.Value
                * scale;
        }

        /// <summary>
        /// Computes a scroll time (in milliseconds) from a scroll speed in the range of 1-40.
        /// </summary>
        /// <param name="scrollSpeed">The scroll speed.</param>
        /// <returns>The scroll time.</returns>
        public static double ComputeScrollTime(double scrollSpeed) => MAX_TIME_RANGE / scrollSpeed;

        public override PlayfieldAdjustmentContainer CreatePlayfieldAdjustmentContainer() =>
            new ManiaPlayfieldAdjustmentContainer(this);

        protected override Playfield CreatePlayfield() => new ManiaPlayfield(Beatmap.Stages);

        public override int Variant =>
            (int)(Beatmap.Stages.Count == 1 ? PlayfieldType.Single : PlayfieldType.Dual)
            + Beatmap.TotalColumns;

        protected override PassThroughInputManager CreateInputManager() =>
            new ManiaInputManager(Ruleset.RulesetInfo, Variant);

        public override DrawableHitObject<ManiaHitObject>? CreateDrawableRepresentation(
            ManiaHitObject h
        ) => null;

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) =>
            new ManiaFramedReplayInputHandler(replay);

        protected override ReplayRecorder CreateReplayRecorder(Score score) =>
            new ManiaReplayRecorder(score);

        protected override ResumeOverlay CreateResumeOverlay() => new DelayedResumeOverlay();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (currentSkin.IsNotNull())
                currentSkin.SourceChanged -= onSkinChange;
        }
    }
}
