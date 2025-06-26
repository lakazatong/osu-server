#pragma warning disable IDE0073

// using osu.Framework.Logging;
using System.Collections.Generic;
using System.Threading;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Overlays.Mods;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;
using osu.Game.Skinning;

namespace osu.Game.BellaFiora
{
    public class Triggers
    {
        private static Server server = null!;
        private static SkinManager skinManager = null!;
        private static Skin[] defaultSkins = null!;
        private static bool footerButtonModsLoadCompleteTrigged = false;
        private static OsuConfigManager osuConfigManager = null!;
        private static FrameworkConfigManager frameworkConfigManager = null!;
        private static Dictionary<string, ModPanel> modPanels = new Dictionary<string, ModPanel>();
        private static ModPanel autoPanel = null!;
        private static ModPanel hdPanel = null!;
        private static ModPanel hrPanel = null!;
        private static ModPanel dtPanel = null!;
        private static BeatmapDifficultyCache beatmapDifficultyCache = null!;
        private static BeatmapManager beatmapManager = null!;
        private static ScreenshotManager screenshotManager = null!;
        private static GameHost host = null!;

        public static void CarouselBeatmapsTrulyLoaded(SongSelect songSelect)
        {
            if (server == null && SynchronizationContext.Current != null)
            {
                server = new Server(SynchronizationContext.Current)
                {
                    SongSelect = songSelect,
                    SkinManager = skinManager,
                    DefaultSkins = defaultSkins,
                    OsuConfigManager = osuConfigManager,
                    FrameworkConfigManager = frameworkConfigManager,
                    ModPanels = modPanels,
                    AutoPanel = autoPanel,
                    HDPanel = hdPanel,
                    HRPanel = hrPanel,
                    DTPanel = dtPanel,
                    BeatmapDifficultyCache = beatmapDifficultyCache,
                    BeatmapManager = beatmapManager,
                    ScreenshotManager = screenshotManager,
                    Host = host,
                };
                server.Start();
            }
        }

        public static void ModPanelLoadComplete(ModPanel panel)
        {
            if (server == null)
            {
                modPanels.TryAdd(panel.Mod.Acronym, panel);
                if (autoPanel == null && panel.Mod.Acronym == "AT")
                    autoPanel = panel;
                if (hdPanel == null && panel.Mod.Acronym == "HD")
                    hdPanel = panel;
                if (hrPanel == null && panel.Mod.Acronym == "HR")
                    hrPanel = panel;
                if (dtPanel == null && panel.Mod.Acronym == "DT")
                    dtPanel = panel;
            }
            else
            {
                server.ModPanels.TryAdd(panel.Mod.Acronym, panel);
                if (server.AutoPanel == null && panel.Mod.Acronym == "AT")
                    server.AutoPanel = panel;
                if (server.HDPanel == null && panel.Mod.Acronym == "HD")
                    server.HDPanel = panel;
                if (server.HRPanel == null && panel.Mod.Acronym == "HR")
                    server.HRPanel = panel;
                if (server.DTPanel == null && panel.Mod.Acronym == "DT")
                    server.DTPanel = panel;
            }
        }

        public static void FooterButtonModsLoadComplete(FooterButtonMods button)
        {
            if (!footerButtonModsLoadCompleteTrigged)
            {
                // this will trigger the creation of all mod panels
                button.TriggerClick();
                footerButtonModsLoadCompleteTrigged = true;
            }
        }

        public static void PlayerLoaded(Player player, HotkeyExitOverlay? hotkeyExitOverlay)
        {
            if (server == null)
                return;
            if (player is ReplayPlayer replayPlayer)
                server.ReplayPlayer = replayPlayer;
            if (hotkeyExitOverlay != null)
                server.HotkeyExitOverlay = hotkeyExitOverlay;
        }

        public static void SkinManagerCreated(SkinManager sm, Skin[] ds)
        {
            if (skinManager == null)
            {
                skinManager = sm;
                defaultSkins = ds;
            }
        }

        public static void ScreenShotManagerCreated(ScreenshotManager sm)
        {
            screenshotManager = sm;
        }

        public static void OsuGameBaseCreated(
            BeatmapDifficultyCache bdc,
            BeatmapManager bm,
            OsuConfigManager lc,
            FrameworkConfigManager fcm,
            GameHost ht
        )
        {
            beatmapDifficultyCache = bdc;
            beatmapManager = bm;
            osuConfigManager = lc;
            frameworkConfigManager = fcm;
            host = ht;
        }
    }
}
