#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Game.BellaFiora.Utils;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Select.Carousel;

namespace osu.Game.BellaFiora.Endpoints
{
    public class startMapEndpoint : Endpoint<Server>
    {
        public override string Method { get; set; } = "GET";
        public override string Description { get; set; } =
            "Starts a map with the given beatmap ID and mods.\n"
            + "You can use `beatmapId` and `mods` in the query string.\n"
            + "Mods should be a string of capital mod acronyms separated by '+'.";

        public startMapEndpoint(Server server)
            : base(server) { }

        public override Func<HttpListenerRequest, bool> Handler =>
            request =>
            {
                var QueryString = request.QueryString;
                string? beatmapIdStr = QueryString["beatmapId"];
                string? modsStr = QueryString["mods"];

                if (int.TryParse(beatmapIdStr, out int beatmapId) && modsStr != null)
                {
                    callback(beatmapId, modsStr);
                    return true;
                }
                return false;
            };

        private void callback(int beatmapId, string modsStr)
        {
            Server.UpdateThread.Post(
                _ =>
                {
                    CarouselBeatmap? carouselBeatmap = Server.SongSelect.GetCarouselBeatmap(
                        beatmapId
                    );
                    if (carouselBeatmap == null)
                    {
                        Server.RespondHTML(
                            "h1",
                            "Received recordMap request",
                            "p",
                            $"Beatmap ID: {beatmapId}",
                            "p",
                            "Requested Mods:",
                            "ul",
                            modsStr.Split('+'),
                            (Func<object, string?>)(e => e.ToString()),
                            "p",
                            "Do not have this beatmap"
                        );
                        return;
                    }

                    Server.ModPanels.Values.ForEach(p => p.ForceDeselect());

                    var selectedModPanels = new List<ModPanel>();

                    string pattern = string.Join(
                        "|",
                        Server.ModPanels.Keys.Select(k => Regex.Escape(k))
                    );
                    Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

                    var matches = regex.Matches(modsStr);

                    foreach (Match match in matches)
                    {
                        if (Server.ModPanels.TryGetValue(match.Value, out var panel))
                        {
                            if (!(panel.Mod.Type is ModType.Automation))
                                selectedModPanels.Add(panel);
                        }
                    }

                    selectedModPanels.ForEach(p => p.ForceSelect());
                    Server.AutoPanel.ForceSelect();

                    bool started = Server.SongSelect.StartMap(beatmapId);

                    Server.RespondHTML(
                        "h1",
                        "Received recordMap request",
                        "p",
                        $"Started: {started}",
                        "p",
                        $"Beatmap ID: {beatmapId}",
                        "p",
                        "Requested Mods:",
                        "ul",
                        modsStr.Split('+'),
                        Formatters.UnitFormatter,
                        "p",
                        "Selected Mods:",
                        "ul",
                        selectedModPanels,
                        Formatters.FormatPanel,
                        "p",
                        "All Mods:",
                        "ul",
                        Server.ModPanels.Values,
                        Formatters.FormatPanel
                    );
                },
                null
            );
        }
    }
}
