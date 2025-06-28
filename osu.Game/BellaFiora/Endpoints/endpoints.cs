#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class endpointsEndpoint : Endpoint<Server>
    {
        public override string Method { get; set; } = "GET";
        public override string Description { get; set; } = "Returns this.\nNo parameters.";

        public endpointsEndpoint(Server server)
            : base(server) { }

        public override Func<HttpListenerRequest, bool> Handler =>
            request =>
            {
                Server.UpdateThread.Post(
                    _ =>
                    {
                        var endpoints = new Dictionary<string, string>();
                        foreach (var endpoint in Server.Endpoints)
                            endpoints[endpoint.Path] = endpoint.Description;

                        Server.RespondJSON(endpoints);
                    },
                    null
                );
                return true;
            };
    }
}
