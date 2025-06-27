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
        }
    }
}
