// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Configuration;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Osu.Configuration
{
    public class OsuRulesetConfigManager : RulesetConfigManager<OsuRulesetSetting>
    {
        public OsuRulesetConfigManager(
            SettingsStore? settings,
            RulesetInfo ruleset,
            int? variant = null
        )
            : base(settings, ruleset, variant) { }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();
            SetDefault(OsuRulesetSetting.SnakingInSliders, true);
            SetDefault(OsuRulesetSetting.SnakingOutSliders, true);
            SetDefault(OsuRulesetSetting.ShowCursorTrail, true);
            SetDefault(OsuRulesetSetting.ShowCursorRipples, false);
            SetDefault(OsuRulesetSetting.PlayfieldBorderStyle, PlayfieldBorderStyle.None);

            SetDefault(OsuRulesetSetting.ReplayClickMarkersEnabled, false);
            SetDefault(OsuRulesetSetting.ReplayFrameMarkersEnabled, false);
            SetDefault(OsuRulesetSetting.ReplayCursorPathEnabled, false);
            SetDefault(OsuRulesetSetting.ReplayCursorHideEnabled, false);
            SetDefault(OsuRulesetSetting.ReplayAnalysisDisplayLength, 800);
        }
    }

    public enum OsuRulesetSetting
    {
        SnakingInSliders,
        SnakingOutSliders,
        ShowCursorTrail,
        ShowCursorRipples,
        PlayfieldBorderStyle,

        // Replay
        ReplayClickMarkersEnabled,
        ReplayFrameMarkersEnabled,
        ReplayCursorPathEnabled,
        ReplayCursorHideEnabled,
        ReplayAnalysisDisplayLength,
    }
}
