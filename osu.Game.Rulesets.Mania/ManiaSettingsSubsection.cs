﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mania.Configuration;
using osu.Game.Rulesets.Mania.UI;

namespace osu.Game.Rulesets.Mania
{
    public partial class ManiaSettingsSubsection : RulesetSettingsSubsection
    {
        protected override LocalisableString Header => "osu!mania";

        public ManiaSettingsSubsection(ManiaRuleset ruleset)
            : base(ruleset) { }

        [BackgroundDependencyLoader]
        private void load()
        {
            var config = (ManiaRulesetConfigManager)Config;

            Children = new Drawable[]
            {
                new SettingsEnumDropdown<ManiaScrollingDirection>
                {
                    LabelText = RulesetSettingsStrings.ScrollingDirection,
                    Current = config.GetBindable<ManiaScrollingDirection>(
                        ManiaRulesetSetting.ScrollDirection
                    ),
                },
                new SettingsSlider<double, ManiaScrollSlider>
                {
                    LabelText = RulesetSettingsStrings.ScrollSpeed,
                    Current = config.GetBindable<double>(ManiaRulesetSetting.ScrollSpeed),
                    KeyboardStep = 1,
                },
                new SettingsCheckbox
                {
                    Keywords = new[] { "color" },
                    LabelText = RulesetSettingsStrings.TimingBasedColouring,
                    Current = config.GetBindable<bool>(
                        ManiaRulesetSetting.TimingBasedNoteColouring
                    ),
                },
            };

            if (RuntimeInfo.IsMobile)
            {
                Add(
                    new SettingsEnumDropdown<ManiaMobileLayout>
                    {
                        LabelText = RulesetSettingsStrings.MobileLayout,
                        Current = config.GetBindable<ManiaMobileLayout>(
                            ManiaRulesetSetting.MobileLayout
                        ),
                    }
                );
            }
        }

        private partial class ManiaScrollSlider : RoundedSliderBar<double>
        {
            public override LocalisableString TooltipText =>
                RulesetSettingsStrings.ScrollSpeedTooltip(
                    (int)DrawableManiaRuleset.ComputeScrollTime(Current.Value),
                    Current.Value
                );
        }
    }
}
