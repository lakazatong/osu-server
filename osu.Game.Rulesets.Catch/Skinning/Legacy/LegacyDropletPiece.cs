// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Catch.Skinning.Legacy
{
    public partial class LegacyDropletPiece : LegacyCatchHitObjectPiece
    {
        private static readonly Vector2 droplet_max_size = new Vector2(160);

        public LegacyDropletPiece()
        {
            Scale = new Vector2(0.8f);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Texture? texture = Skin.GetTexture("fruit-drop")?.WithMaximumSize(droplet_max_size);
            Texture? overlayTexture = Skin.GetTexture("fruit-drop-overlay")
                ?.WithMaximumSize(droplet_max_size);

            SetTexture(texture, overlayTexture);
        }
    }
}
