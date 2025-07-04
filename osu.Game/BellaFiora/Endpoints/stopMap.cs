#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class stopMapEndpoint : Endpoint<Server>
    {
        public override string Method { get; set; } = "GET";
        public override string Description { get; set; } = "Stops the current map.\nNo parameters.";

        public stopMapEndpoint(Server server)
            : base(server) { }

        public override Func<HttpListenerRequest, bool> Handler =>
            request =>
            {
                Server.UpdateThread.Post(
                    _ =>
                    {
                        Server.HotkeyExitOverlay?.Action.Invoke();
                        Server.RespondHTML("h1", "Received stopMap request", "p", "Map stopped");
                    },
                    null
                );
                return true;
            };
    }
}
