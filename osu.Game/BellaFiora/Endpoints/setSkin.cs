#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class setSkinEndpoint : Endpoint<Server>
    {
        public override string Method { get; set; } = "GET";
        public override string Description { get; set; } =
            "Sets the current skin.\n"
            + "You can use `skin` in the query string to set the skin.\n"
            + "0-3 are reserved for default skins, 4-9 fallback to 0,\n"
            + "10+ are custom skins. An index of 10 correponds to the first custom skin.";

        public setSkinEndpoint(Server server)
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
