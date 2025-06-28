#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class statusEndpoint : Endpoint<Server>
    {
        public override string Method { get; set; } = "GET";
        public override string Description { get; set; } =
            "Returns wether each property of the server is non-null.\nNo parameters.";

        public statusEndpoint(Server server)
            : base(server) { }

        public override Func<HttpListenerRequest, bool> Handler =>
            request =>
            {
                Server.UpdateThread.Post(
                    _ =>
                    {
                        var status = new Dictionary<string, bool>();
                        foreach (
                            var property in typeof(Server).GetProperties(
                                System.Reflection.BindingFlags.Public
                                    | System.Reflection.BindingFlags.Instance
                            )
                        )
                        {
                            object? value = property.GetValue(Server);
                            status[property.Name] = value != null;
                        }

                        Server.RespondJSON(status);
                    },
                    null
                );
                return true;
            };
    }
}
