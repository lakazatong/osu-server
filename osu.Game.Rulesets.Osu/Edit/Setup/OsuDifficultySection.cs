// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Screens.Edit.Setup;

namespace osu.Game.Rulesets.Osu.Edit.Setup
{
    public partial class OsuDifficultySection : SetupSection
    {
        private FormSliderBar<float> circleSizeSlider { get; set; } = null!;
        private FormSliderBar<float> healthDrainSlider { get; set; } = null!;
        private FormSliderBar<float> approachRateSlider { get; set; } = null!;
        private FormSliderBar<float> overallDifficultySlider { get; set; } = null!;
        private FormSliderBar<double> baseVelocitySlider { get; set; } = null!;
        private FormSliderBar<double> tickRateSlider { get; set; } = null!;
        private FormSliderBar<float> stackLeniency { get; set; } = null!;

        public override LocalisableString Title => EditorSetupStrings.DifficultyHeader;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                circleSizeSlider = new FormSliderBar<float>
                {
                    Caption = BeatmapsetsStrings.ShowStatsCs,
                    HintText = EditorSetupStrings.CircleSizeDescription,
                    Current = new BindableFloat(Beatmap.Difficulty.CircleSize)
                    {
                        Default = BeatmapDifficulty.DEFAULT_DIFFICULTY,
                        MinValue = 0,
                        MaxValue = 10,
                        Precision = 0.1f,
                    },
                    TransferValueOnCommit = true,
                    TabbableContentContainer = this,
                },
                healthDrainSlider = new FormSliderBar<float>
                {
                    Caption = BeatmapsetsStrings.ShowStatsDrain,
                    HintText = EditorSetupStrings.DrainRateDescription,
                    Current = new BindableFloat(Beatmap.Difficulty.DrainRate)
                    {
                        Default = BeatmapDifficulty.DEFAULT_DIFFICULTY,
                        MinValue = 0,
                        MaxValue = 10,
                        Precision = 0.1f,
                    },
                    TransferValueOnCommit = true,
                    TabbableContentContainer = this,
                },
                approachRateSlider = new FormSliderBar<float>
                {
                    Caption = BeatmapsetsStrings.ShowStatsAr,
                    HintText = EditorSetupStrings.ApproachRateDescription,
                    Current = new BindableFloat(Beatmap.Difficulty.ApproachRate)
                    {
                        Default = BeatmapDifficulty.DEFAULT_DIFFICULTY,
                        MinValue = 0,
                        MaxValue = 10,
                        Precision = 0.1f,
                    },
                    TransferValueOnCommit = true,
                    TabbableContentContainer = this,
                },
                overallDifficultySlider = new FormSliderBar<float>
                {
                    Caption = BeatmapsetsStrings.ShowStatsAccuracy,
                    HintText = EditorSetupStrings.OverallDifficultyDescription,
                    Current = new BindableFloat(Beatmap.Difficulty.OverallDifficulty)
                    {
                        Default = BeatmapDifficulty.DEFAULT_DIFFICULTY,
                        MinValue = 0,
                        MaxValue = 10,
                        Precision = 0.1f,
                    },
                    TransferValueOnCommit = true,
                    TabbableContentContainer = this,
                },
                baseVelocitySlider = new FormSliderBar<double>
                {
                    Caption = EditorSetupStrings.BaseVelocity,
                    HintText = EditorSetupStrings.BaseVelocityDescription,
                    KeyboardStep = 0.1f,
                    Current = new BindableDouble(Beatmap.Difficulty.SliderMultiplier)
                    {
                        Default = 1.4,
                        MinValue = 0.4,
                        MaxValue = 3.6,
                        Precision = 0.01f,
                    },
                    TransferValueOnCommit = true,
                    TabbableContentContainer = this,
                },
                tickRateSlider = new FormSliderBar<double>
                {
                    Caption = EditorSetupStrings.TickRate,
                    HintText = EditorSetupStrings.TickRateDescription,
                    KeyboardStep = 1,
                    Current = new BindableDouble(Beatmap.Difficulty.SliderTickRate)
                    {
                        Default = 1,
                        MinValue = 1,
                        MaxValue = 4,
                        Precision = 1,
                    },
                    TransferValueOnCommit = true,
                    TabbableContentContainer = this,
                },
                stackLeniency = new FormSliderBar<float>
                {
                    Caption = "Stack Leniency",
                    HintText =
                        "In play mode, osu! automatically stacks notes which occur at the same location. Increasing this value means it is more likely to snap notes of further time-distance.",
                    KeyboardStep = 0.1f,
                    Current = new BindableFloat(Beatmap.StackLeniency)
                    {
                        Default = 0.7f,
                        MinValue = 0,
                        MaxValue = 1,
                        Precision = 0.1f,
                    },
                    TransferValueOnCommit = true,
                    TabbableContentContainer = this,
                },
            };

            foreach (var item in Children.OfType<FormSliderBar<float>>())
                item.Current.ValueChanged += _ => updateValues();

            foreach (var item in Children.OfType<FormSliderBar<double>>())
                item.Current.ValueChanged += _ => updateValues();
        }

        private void updateValues()
        {
            // for now, update these on commit rather than making BeatmapMetadata bindables.
            // after switching database engines we can reconsider if switching to bindables is a good direction.
            Beatmap.Difficulty.CircleSize = circleSizeSlider.Current.Value;
            Beatmap.Difficulty.DrainRate = healthDrainSlider.Current.Value;
            Beatmap.Difficulty.ApproachRate = approachRateSlider.Current.Value;
            Beatmap.Difficulty.OverallDifficulty = overallDifficultySlider.Current.Value;
            Beatmap.Difficulty.SliderMultiplier = baseVelocitySlider.Current.Value;
            Beatmap.Difficulty.SliderTickRate = tickRateSlider.Current.Value;
            Beatmap.StackLeniency = stackLeniency.Current.Value;

            Beatmap.UpdateAllHitObjects();
            Beatmap.SaveState();
        }
    }
}
