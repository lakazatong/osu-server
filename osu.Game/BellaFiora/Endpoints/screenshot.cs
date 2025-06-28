#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Framework.Extensions;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class screenshotEndpoint : Endpoint<Server>
    {
        public override string Method { get; set; } = "GET";
        public override string Description { get; set; } =
            "Returns a screenshot of the game.\nNo parameters.";

        public screenshotEndpoint(Server server)
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
                    Server
                        .ScreenshotManager.GetScreenshotAsync()
                        .ContinueWith(t => Server.RespondImage(t.GetResultSafely()));
                },
                null
            );
        }
    }
}
