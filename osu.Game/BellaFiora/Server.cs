#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public GameHost GameHost { get; internal set; } = null!;
        public SettingsOverlay SettingsOverlay { get; internal set; } = null!;

        public static readonly string HOST =
            Environment.GetEnvironmentVariable("DOCKER_ENV") == "true" ? "+" : "localhost";
        public static readonly string PORT =
            Environment.GetEnvironmentVariable("OSU_SERVER_PORT") ?? "8080";

        public Server(SynchronizationContext syncContext)
            : base($"http://{HOST}:{PORT}/")
        {
            UpdateThread = syncContext;

            var endpointTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t =>
                    t.IsClass
                    && !t.IsAbstract
                    && typeof(Endpoint<Server>).IsAssignableFrom(t)
                    && t.Namespace == "osu.Game.BellaFiora.Endpoints"
                );

            foreach (var type in endpointTypes)
            {
                var constructor = type.GetConstructor([typeof(Server)]);
                if (constructor == null)
                    continue;

                var instance = (Endpoint<Server>)constructor.Invoke([this]);
                Add(instance);
            }
        }
    }
}
