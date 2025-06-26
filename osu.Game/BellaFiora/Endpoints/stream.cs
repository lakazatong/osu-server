#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Framework.Extensions;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class streamEndpoint : Endpoint<Server>
    {
        public streamEndpoint(Server server)
            : base(server) { }

        public override Func<HttpListenerRequest, bool> Handler =>
            request =>
            {
                callback();
                return true;
            };

        private void callback()
        {
            Server.UpdateThread.Post(_ => { }, null);
        }
    }
}
