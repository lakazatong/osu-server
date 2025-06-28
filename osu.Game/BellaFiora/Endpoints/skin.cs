using System;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class skinEndpoint : Endpoint<Server>
    {
        public skinEndpoint(Server server)
            : base(server) { }

        public override Func<HttpListenerRequest, bool> Handler =>
            request =>
            {
                var QueryString = request.QueryString;
                string? skinStr = QueryString["skin"];

                if (int.TryParse(skinStr, out int skin))
                {
                    callback(skin);
                    return true;
                }
                return false;
            };

        private void callback(int skin)
        {
            Server.UpdateThread.Post(
                _ =>
                {
                    if (skin < 10)
                    {
                        // reserved to default skins
                        // 0: DefaultLegacySkin
                        // 1: TrianglesSkin
                        // 2: ArgonSkin
                        // 3: ArgonProSkin
                        // 4-9: fallback to 0
                        if (skin is < 0 or > 3)
                            skin = 0;
                        Server.SkinManager.CurrentSkinInfo.Value = Server
                            .DefaultSkins[skin]
                            .SkinInfo;
                    }
                    else
                    {
                        // custom skin ID
                        if (skin - 10 < Server.CustomSkins.Count)
                            Server.SkinManager.CurrentSkinInfo.Value = Server
                                .CustomSkins[skin - 10]
                                .SkinInfo;
                        else
                            Server.SkinManager.CurrentSkinInfo.Value = Server
                                .DefaultSkins[0]
                                .SkinInfo;
                    }

                    Server.RespondHTML("h1", "Received skin change request", "p", $"Skin: {skin}");
                },
                null
            );
        }
    }
}
