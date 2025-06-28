#pragma warning disable IDE0073

using System.Collections.Generic;
using System.Threading;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.BellaFiora.Endpoints;
using osu.Game.BellaFiora.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;
using osu.Game.Skinning;

namespace osu.Game.BellaFiora
{
    public class Server : BaseServer
    {
        public readonly SynchronizationContext UpdateThread;
        public SongSelect SongSelect { get; internal set; } = null!;
        public SkinManager SkinManager { get; internal set; } = null!;
        public Skin[] DefaultSkins { get; internal set; } = null!;
        public List<Skin> CustomSkins { get; internal set; } = [];
        public Dictionary<string, ModPanel> ModPanels { get; internal set; } =
            new Dictionary<string, ModPanel>();
        public Mod NMmod { get; internal set; } = new ModNoMod();
        public ModPanel AutoPanel { get; internal set; } = null!;
        public ModPanel HDPanel { get; internal set; } = null!;
        public ModPanel HRPanel { get; internal set; } = null!;
        public ModPanel DTPanel { get; internal set; } = null!;
        public ReplayPlayer? ReplayPlayer { get; internal set; } = null;
        public HotkeyExitOverlay? HotkeyExitOverlay { get; internal set; } = null;
        public OsuConfigManager OsuConfigManager { get; internal set; } = null!;
        public BeatmapDifficultyCache BeatmapDifficultyCache { get; internal set; } = null!;
        public FrameworkConfigManager FrameworkConfigManager { get; internal set; } = null!;
        public BeatmapManager BeatmapManager { get; internal set; } = null!;
        public ScreenshotManager ScreenshotManager { get; internal set; } = null!;
        public GameHost Host { get; internal set; } = null!;
        public SettingsOverlay SettingsOverlay { get; internal set; } = null!;

        public Server(SynchronizationContext syncContext)
            : base()
        {
            UpdateThread = syncContext;

            // AddGET("/loadConfig", new loadConfigEndpoint(this).Handler);
            // AddGET("/saveConfig", new saveConfigEndpoint(this).Handler);
            // AddGET("/stopMap", new stopMapEndpoint(this).Handler);
            // AddGET("/pp", new ppEndpoint(this).Handler);
            // AddGET("/loadOsuFile", new loadOsuFileEndpoint(this).Handler);
            // AddGET("/star", new starEndpoint(this).Handler);
            AddGET("/status", new statusEndpoint(this).Handler);
            AddGET("/screenshot", new screenshotEndpoint(this).Handler);
            AddGET("/toggleSettings", new toggleSettingsEndpoint(this).Handler);
            AddGET("/startMap", new startMapEndpoint(this).Handler);
            AddGET("/stopMap", new stopMapEndpoint(this).Handler);
            AddGET("/setSkin", new setSkinEndpoint(this).Handler);
        }
    }
}
