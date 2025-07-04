// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Localisation;
using osu.Game.BellaFiora;
using osu.Game.Graphics;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Overlays.Mods
{
    public partial class ModPanel : ModSelectPanel, IFilterable
    {
        public Mod Mod => modState.Mod;
        public override BindableBool Active => modState.Active;

        protected override float IdleSwitchWidth => 54;
        protected override float ExpandedSwitchWidth => 70;

        private readonly ModState modState;

        public ModPanel(ModState modState)
        {
            this.modState = modState;

            Title = Mod.Name;
            Description = Mod.Description;

            SwitchContainer.Child = new ModSwitchSmall(Mod)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Active = { BindTarget = Active },
                Shear = -OsuGame.SHEAR,
                Scale = new Vector2(HEIGHT / ModSwitchSmall.DEFAULT_SIZE),
            };
        }

        public ModPanel(Mod mod)
            : this(new ModState(mod)) { }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            AccentColour = colours.ForModType(Mod.Type);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            modState.ValidForSelection.BindValueChanged(_ => updateFilterState());
            modState.MatchingTextFilter.BindValueChanged(_ => updateFilterState(), true);
            modState.Preselected.BindValueChanged(
                b =>
                {
                    if (b.NewValue)
                    {
                        Content.EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Glow,
                            Colour = AccentColour,
                            Hollow = true,
                            Radius = 2,
                        };
                    }
                    else
                        Content.EdgeEffect = default;
                },
                true
            );

            Triggers.AssignToServer(server =>
            {
                server.ModPanels.TryAdd(Mod.Acronym, this);
                if (server.AutoPanel == null && Mod.Acronym == "AT")
                    server.AutoPanel = this;
                if (server.HDPanel == null && Mod.Acronym == "HD")
                    server.HDPanel = this;
                if (server.HRPanel == null && Mod.Acronym == "HR")
                    server.HRPanel = this;
                if (server.DTPanel == null && Mod.Acronym == "DT")
                    server.DTPanel = this;
            });
        }

        protected override void Select()
        {
            modState.PendingConfiguration = Mod.RequiresConfiguration;
            Active.Value = true;
        }

        protected override void Deselect()
        {
            modState.PendingConfiguration = false;
            Active.Value = false;
        }

        public void ForceSelect() => Select();

        public void ForceDeselect() => Deselect();

        #region Filtering support

        /// <seealso cref="ModState.Visible"/>
        public bool Visible => modState.Visible;

        public override IEnumerable<LocalisableString> FilterTerms =>
            new LocalisableString[] { Mod.Name, Mod.Name.Replace(" ", string.Empty), Mod.Acronym };

        public override bool MatchingFilter
        {
            get => modState.MatchingTextFilter.Value;
            set => modState.MatchingTextFilter.Value = value;
        }

        private void updateFilterState()
        {
            this.FadeTo(Visible ? 1 : 0);
        }

        #endregion
    }
}
