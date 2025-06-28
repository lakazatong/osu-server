#pragma warning disable IDE0073

using System;
using System.Net;

namespace osu.Game.BellaFiora.Utils
{
    public abstract class Endpoint<T> : IEndpoint
        where T : BaseServer
    {
        protected virtual T Server { get; private set; }
        public abstract string Method { get; set; }
        public string Path { get; }
        public string FullPath { get; }
        public abstract string Description { get; set; }

        public Endpoint(T server)
        {
            Server = server;
            Path = $"{GetType().Name.Replace("Endpoint", "")}";
            FullPath = $"{Server.Prefix}{Path}";
        }

        public abstract Func<HttpListenerRequest, bool> Handler { get; }
    }
}
