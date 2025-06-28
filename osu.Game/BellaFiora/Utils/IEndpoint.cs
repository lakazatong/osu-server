#pragma warning disable IDE0073

using System;
using System.Net;

namespace osu.Game.BellaFiora.Utils
{
    public interface IEndpoint
    {
        string Method { get; set; }
        string Path { get; }
        string FullPath { get; }
        string Description { get; set; }
        Func<HttpListenerRequest, bool> Handler { get; }
    }
}
