#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class statusEndpoint : Endpoint<Server>
    {
        public statusEndpoint(Server server)
            : base(server) { }

        public override Func<HttpListenerRequest, bool> Handler =>
            request =>
            {
                callback();
                return true;
            };

        private void callback()
        {
            Server.UpdateThread.Post(
                _ =>
                {
                    Server.RespondJSON(
                        new Dictionary<string, bool>
                        {
                            { "SongSelect", Server.SongSelect != null },
                            { "SkinManager", Server.SkinManager != null },
                            { "DefaultSkins", Server.DefaultSkins != null },
                            { "CustomSkins", Server.CustomSkins != null },
                            { "ModPanels", Server.ModPanels != null },
                            { "NMmod", Server.NMmod != null },
                            { "AutoPanel", Server.AutoPanel != null },
                            { "HDPanel", Server.HDPanel != null },
                            { "HRPanel", Server.HRPanel != null },
                            { "DTPanel", Server.DTPanel != null },
                            { "Player", Server.ReplayPlayer != null },
                            { "HotkeyExitOverlay", Server.HotkeyExitOverlay != null },
                            { "OsuConfigManager", Server.OsuConfigManager != null },
                            { "BeatmapDifficultyCache", Server.BeatmapDifficultyCache != null },
                            { "FrameworkConfigManager", Server.FrameworkConfigManager != null },
                            { "BeatmapManager", Server.BeatmapManager != null },
                        }
                    );
                },
                null
            );
        }
    }
}
