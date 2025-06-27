#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Framework.Graphics.Containers;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class toggleSettingsEndpoint : Endpoint<Server>
    {
        public toggleSettingsEndpoint(Server server)
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
                    Server.SettingsOverlay?.ToggleVisibility();
                    Server.RespondJSON(
                        new { state = Server.SettingsOverlay?.State.Value == Visibility.Visible }
                    );
                },
                null
            );
        }
    }
}
