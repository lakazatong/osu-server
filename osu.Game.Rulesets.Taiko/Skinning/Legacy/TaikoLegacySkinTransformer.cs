// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Game.Audio;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.UI;
using osu.Game.Screens.Play.HUD;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Taiko.Skinning.Legacy
{
    public class TaikoLegacySkinTransformer : LegacySkinTransformer
    {
        public override bool IsProvidingLegacyResources =>
            base.IsProvidingLegacyResources || hasHitCircle || hasBarLeft;

        private readonly Lazy<bool> hasExplosion;

        private bool hasHitCircle => GetTexture("taikohitcircle") != null;
        private bool hasBarLeft => GetTexture("taiko-bar-left") != null;

        public TaikoLegacySkinTransformer(ISkin skin)
            : base(skin)
        {
            hasExplosion = new Lazy<bool>(() =>
                GetTexture(getHitName(TaikoSkinComponents.TaikoExplosionGreat)) != null
            );
        }

        public override Drawable? GetDrawableComponent(ISkinComponentLookup lookup)
        {
            switch (lookup)
            {
                case GlobalSkinnableContainerLookup containerLookup:
                {
                    // Modifications for global components.
                    if (containerLookup.Ruleset == null)
                        return base.GetDrawableComponent(lookup);

                    // we don't have enough assets to display these components (this is especially the case on a "beatmap" skin).
                    if (!IsProvidingLegacyResources)
                        return null;

                    switch (containerLookup.Lookup)
                    {
                        case GlobalSkinnableContainers.MainHUDComponents:
                            return new DefaultSkinComponentsContainer(container =>
                            {
                                var combo = container
                                    .OfType<LegacyDefaultComboCounter>()
                                    .FirstOrDefault();
                                var spectatorList = container
                                    .OfType<SpectatorList>()
                                    .FirstOrDefault();
                                var leaderboard = container
                                    .OfType<DrawableGameplayLeaderboard>()
                                    .FirstOrDefault();

                                Vector2 pos = new Vector2();

                                if (combo != null)
                                {
                                    combo.Anchor = Anchor.BottomLeft;
                                    combo.Origin = Anchor.BottomLeft;
                                    combo.Scale = new Vector2(1.28f);

                                    pos += new Vector2(
                                        10,
                                        -(combo.DrawHeight * 1.56f + 20) * combo.Scale.X
                                    );
                                }

                                if (leaderboard != null)
                                {
                                    leaderboard.Anchor = Anchor.BottomLeft;
                                    leaderboard.Origin = Anchor.BottomLeft;
                                    leaderboard.Position = pos;
                                    leaderboard.Height = 170;
                                    pos += new Vector2(10 + leaderboard.Width, -leaderboard.Height);
                                }

                                if (spectatorList != null)
                                {
                                    spectatorList.Anchor = Anchor.BottomLeft;
                                    spectatorList.Origin = Anchor.TopLeft;
                                    spectatorList.Position = pos;
                                }
                            })
                            {
                                new LegacyDefaultComboCounter(),
                                new SpectatorList(),
                                new DrawableGameplayLeaderboard(),
                            };
                    }

                    return null;
                }

                case SkinComponentLookup<HitResult>:
                {
                    // if a taiko skin is providing explosion sprites, hide the judgements completely
                    if (hasExplosion.Value)
                        return Drawable.Empty().With(d => d.Expire());

                    break;
                }

                case TaikoSkinComponentLookup taikoComponent:
                {
                    switch (taikoComponent.Component)
                    {
                        case TaikoSkinComponents.DrumRollBody:
                            if (GetTexture("taiko-roll-middle") != null)
                                return new LegacyDrumRoll();

                            return null;

                        case TaikoSkinComponents.InputDrum:
                            if (hasBarLeft)
                                return new LegacyInputDrum();

                            return null;

                        case TaikoSkinComponents.DrumSamplePlayer:
                            return null;

                        case TaikoSkinComponents.CentreHit:
                        case TaikoSkinComponents.RimHit:
                            if (hasHitCircle)
                                return new LegacyHit(taikoComponent.Component);

                            return null;

                        case TaikoSkinComponents.DrumRollTick:
                            return this.GetAnimation("sliderscorepoint", false, false);

                        case TaikoSkinComponents.Swell:
                            if (GetTexture("spinner-circle") != null)
                                return new LegacySwell();

                            return null;

                        case TaikoSkinComponents.HitTarget:
                            if (GetTexture("taikobigcircle") != null)
                                return new TaikoLegacyHitTarget();

                            return null;

                        case TaikoSkinComponents.PlayfieldBackgroundRight:
                            if (GetTexture("taiko-bar-right") != null)
                                return new TaikoLegacyPlayfieldBackgroundRight();

                            return null;

                        case TaikoSkinComponents.PlayfieldBackgroundLeft:
                            // This is displayed inside LegacyInputDrum. It is required to be there for layout purposes (can be seen on legacy skins).
                            if (GetTexture("taiko-bar-right") != null)
                                return Drawable.Empty();

                            return null;

                        case TaikoSkinComponents.BarLine:
                            if (GetTexture("taiko-barline") != null)
                                return new LegacyBarLine();

                            return null;

                        case TaikoSkinComponents.TaikoExplosionMiss:
                            var missSprite = this.GetAnimation(
                                getHitName(taikoComponent.Component),
                                true,
                                false
                            );
                            if (missSprite != null)
                                return new LegacyHitExplosion(missSprite);

                            return null;

                        case TaikoSkinComponents.TaikoExplosionOk:
                        case TaikoSkinComponents.TaikoExplosionGreat:
                            string hitName = getHitName(taikoComponent.Component);
                            var hitSprite = this.GetAnimation(hitName, true, false);

                            if (hitSprite != null)
                            {
                                var strongHitSprite = this.GetAnimation($"{hitName}k", true, false);

                                return new LegacyHitExplosion(hitSprite, strongHitSprite);
                            }

                            return null;

                        case TaikoSkinComponents.TaikoExplosionKiai:
                            // suppress the default kiai explosion if the skin brings its own sprites.
                            // the drawable needs to expire as soon as possible to avoid accumulating empty drawables on the playfield.
                            if (hasExplosion.Value)
                                return Drawable.Empty().With(d => d.Expire());

                            return null;

                        case TaikoSkinComponents.Scroller:
                            if (GetTexture("taiko-slider") != null)
                                return new LegacyTaikoScroller();

                            return null;

                        case TaikoSkinComponents.Mascot:
                            return new DrawableTaikoMascot();

                        case TaikoSkinComponents.KiaiGlow:
                            if (GetTexture("taiko-glow") != null)
                                return new LegacyKiaiGlow();

                            return null;

                        default:
                            throw new UnsupportedSkinComponentException(lookup);
                    }
                }
            }

            return base.GetDrawableComponent(lookup);
        }

        private string getHitName(TaikoSkinComponents component)
        {
            switch (component)
            {
                case TaikoSkinComponents.TaikoExplosionMiss:
                    return "taiko-hit0";

                case TaikoSkinComponents.TaikoExplosionOk:
                    return "taiko-hit100";

                case TaikoSkinComponents.TaikoExplosionGreat:
                    return "taiko-hit300";
            }

            throw new ArgumentOutOfRangeException(
                nameof(component),
                $"Invalid component type: {component}"
            );
        }

        public override ISample? GetSample(ISampleInfo sampleInfo)
        {
            if (sampleInfo is HitSampleInfo hitSampleInfo)
                return base.GetSample(new LegacyTaikoSampleInfo(hitSampleInfo));

            return base.GetSample(sampleInfo);
        }

        private class LegacyTaikoSampleInfo : HitSampleInfo
        {
            public LegacyTaikoSampleInfo(HitSampleInfo sampleInfo)
                : base(sampleInfo.Name, sampleInfo.Bank, sampleInfo.Suffix, sampleInfo.Volume) { }

            public override IEnumerable<string> LookupNames
            {
                get
                {
                    foreach (string name in base.LookupNames)
                        yield return name.Insert(name.LastIndexOf('/') + 1, "taiko-");
                }
            }
        }
    }
}
