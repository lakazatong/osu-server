#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.BellaFiora.Utils
{
    public abstract class BaseServer
    {
        public readonly string Prefix;
        private HttpListener listener;
        private HttpListenerContext context = null!;
        private Dictionary<string, Dictionary<string, Func<HttpListenerRequest, bool>>> handlers =
            new Dictionary<string, Dictionary<string, Func<HttpListenerRequest, bool>>>
            {
                { "GET", new Dictionary<string, Func<HttpListenerRequest, bool>>() },
                { "POST", new Dictionary<string, Func<HttpListenerRequest, bool>>() },
                { "PUT", new Dictionary<string, Func<HttpListenerRequest, bool>>() },
            };

        public List<IEndpoint> Endpoints = new List<IEndpoint>();

        public BaseServer(string Prefix)
        {
            this.Prefix = Prefix;
            listener = new HttpListener();
            listener.Prefixes.Add(Prefix);
        }

        public void Start()
        {
            listener.Start();
            receive();
            Console.WriteLine("Server started");
        }

        private void receive()
        {
            listener.BeginGetContext(new AsyncCallback(handleRequest), listener);
        }

        public void Stop()
        {
            listener.Stop();
            listener.Close();
            Console.WriteLine("Server stopped");
        }

        protected void Add(IEndpoint endpoint)
        {
            handlers[endpoint.Method]["/" + endpoint.Path] = endpoint.Handler;
            Endpoints.Add(endpoint);
        }

        public string BuildHTML(params object[] args)
        {
            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<!DOCTYPE html><html><body>");

            if (args is not null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] is null)
                        throw new ArgumentNullException();

                    string tag = args[i].ToString() ?? string.Empty;

                    switch (tag.ToLowerInvariant())
                    {
                        case "h1":
                        case "h2":
                            htmlBuilder.AppendFormat($"<{tag}>{args[++i]}</{tag}>");
                            break;

                        case "p":
                            htmlBuilder.AppendFormat($"<p>{args[++i]}</p>");
                            break;

                        case "ul":
                            i++;
                            IEnumerable<object> items = (IEnumerable<object>)args[i];
                            var formatItem = (Func<object, string?>)args[i + 1];
                            htmlBuilder.Append("<ul>");
                            foreach (object item in items)
                                htmlBuilder.AppendFormat(
                                    $"<li>{formatItem(item) ?? string.Empty}</li>"
                                );
                            htmlBuilder.Append("</ul>");
                            i++;
                            break;

                        default:
                            throw new ArgumentException($"Unsupported tag: {tag}");
                    }
                }
            }

            htmlBuilder.Append("</body></html>");
            return htmlBuilder.ToString();
        }

        public void RespondHTML(params object[] args)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(BuildHTML(args));
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        public void RespondJSON(object obj)
        {
            string jsonResponse = JsonConvert.SerializeObject(obj);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        public void RespondImage(Image<Rgba32>? image)
        {
            if (image == null)
            {
                context.Response.StatusCode = 404;
                context.Response.OutputStream.Close();
                return;
            }

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            byte[] buffer = ms.ToArray();
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "image/png";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private bool tryHandleRequest(IAsyncResult result)
        {
            try
            {
                var context = listener.EndGetContext(result);
                var request = context.Request;
                this.context = context;

                if (request.Url == null)
                    return false;
                var handlers = this.handlers[request.HttpMethod];

                if (
                    handlers != null
                    && handlers.TryGetValue(request.Url.AbsolutePath, out var handler)
                    && handler != null
                )
                    return handler(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("tryHandleRequest: " + ex.Message);
            }

            return false;
        }

        private void handleRequest(IAsyncResult result)
        {
            if (!listener.IsListening)
                return;

            if (!tryHandleRequest(result))
                RespondHTML("h1", "Invalid request");

            receive();
        }
    }
}
