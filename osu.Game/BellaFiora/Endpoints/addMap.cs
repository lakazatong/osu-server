#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using osu.Framework.Extensions;
using osu.Game.Beatmaps;
using osu.Game.BellaFiora.Utils;
using osu.Game.Online.Multiplayer;
using osu.Game.Rulesets;

namespace osu.Game.BellaFiora.Endpoints
{
    public class addMapEndpoint : Endpoint<Server>
    {
        public override string Method { get; set; } = "GET";
        public override string Description { get; set; } =
            "Adds a beatmap set to the game.\n"
            + "You can use either `beatmapId` or `beatmapSetId` in the query string.\n"
            + "If you use `beatmapId`, it will fetch the beatmap set ID from osu.ppy.sh.";

        public addMapEndpoint(Server server)
            : base(server) { }

        public override Func<HttpListenerRequest, bool> Handler =>
            request =>
            {
                var QueryString = request.QueryString;
                string? beatmapId = QueryString["beatmapId"];
                string? beatmapSetId = QueryString["beatmapSetId"];
                if (int.TryParse(beatmapSetId, out int id))
                {
                    addBeatmapSet(id).FireAndForget();
                    return true;
                }

                if (int.TryParse(beatmapId, out int tmp))
                {
                    beatmapSetIdFromBeatmapId(tmp)
                        .ContinueWith(task =>
                        {
                            int id = task.GetResultSafely();
                            if (id == 0)
                                Server.RespondJSON(
                                    new Dictionary<string, string>
                                    {
                                        { "error", "Beatmap not found" },
                                    }
                                );
                            else
                                addBeatmapSet(id).FireAndForget();
                        });
                    return true;
                }

                return false;
            };

        private async Task<int> beatmapSetIdFromBeatmapId(int beatmapId)
        {
            using (HttpClient client = new HttpClient())
            {
                string osuFile;
                try
                {
                    osuFile = await client
                        .GetStringAsync($"https://osu.ppy.sh/osu/{beatmapId}")
                        .ConfigureAwait(false);
                }
                catch
                {
                    return 0;
                }
                string? beatmapSetIdLine =
                    osuFile
                        .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(line => line.StartsWith("BeatmapSetID")) ?? "";
                if (beatmapSetIdLine.Length == 0)
                    return 0;
                string[] parts = beatmapSetIdLine.Split(':');
                string rightMostValue = parts[^1].Trim();
                if (int.TryParse(rightMostValue, out int beatmapSetId))
                    return beatmapSetId;
            }
            return 0;
        }

        private void createSilentWav(string filename, int durationSeconds)
        {
            int sampleRate = 44100; // CD-quality sample rate
            int numChannels = 1; // Mono audio
            int bitsPerSample = 16; // 16-bit audio

            int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
            int totalDataBytes = byteRate * durationSeconds;
            int totalFileSize = 44 + totalDataBytes;

            using (FileStream fs = new FileStream(filename, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8))
            {
                // RIFF header
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(totalFileSize - 8); // Chunk size
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                // fmt subchunk
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Subchunk size (16 for PCM)
                writer.Write((short)1); // Audio format (1 = PCM)
                writer.Write((short)numChannels); // Number of channels
                writer.Write(sampleRate); // Sample rate
                writer.Write(byteRate); // Byte rate
                writer.Write((short)(numChannels * (bitsPerSample / 8))); // Block align
                writer.Write((short)bitsPerSample); // Bits per sample

                // data subchunk
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(totalDataBytes); // Data size

                // Write silence
                for (int i = 0; i < totalDataBytes / 2; i++)
                {
                    writer.Write((short)0); // Silent sample (16-bit PCM)
                }
            }
        }

        private async Task<bool> addBeatmapSet(int beatmapSetId)
        {
            string url = $"https://osu.ppy.sh/beatmapsets/{beatmapSetId}";
            JObject root = null!;
            string[] filenames = null!;
            BeatmapSetInfo beatmapSetInfo = null!;
            int maxTotalLength = 0;
            using (HttpClient client = new HttpClient())
            {
                string response;
                try
                {
                    response = await client.GetStringAsync(url).ConfigureAwait(false);
                }
                catch
                {
                    return false;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response);

                var scriptNode = htmlDoc.DocumentNode.SelectSingleNode(
                    "//script[@id='json-beatmapset']"
                );
                if (scriptNode != null)
                {
                    string jsonContent = scriptNode.InnerText;
                    root = JObject.Parse(jsonContent);
                }
                else
                {
                    Server.RespondJSON(
                        new Dictionary<string, string>
                        {
                            { "error", "Script with id 'json-beatmapset' not found" },
                        }
                    );
                    return false;
                }
                Console.WriteLine(root.ToString());
                JToken? beatmaps = root["beatmaps"];
                if (beatmaps == null)
                {
                    Server.RespondJSON(
                        new Dictionary<string, string> { { "error", "Beatmaps not found" } }
                    );
                    return false;
                }
                beatmapSetInfo = new() { OnlineID = beatmapSetId };
                filenames = new string[beatmaps.Count()];
                BeatmapInfo[] beatmapInfos = new BeatmapInfo[beatmaps.Count()];
                await Task.WhenAll(
                        beatmaps.Select(
                            async (JToken beatmap, int i) =>
                            {
                                string beatmapID = beatmap["id"]?.Value<string?>() ?? "";
                                string mode = beatmap["mode"]?.Value<string?>() ?? "";
                                string filename =
                                    $"{root["artist"]} - {root["title"]} ({root["creator"]}) [{beatmap["version"]}].osu";
                                maxTotalLength = Math.Max(
                                    maxTotalLength,
                                    beatmap["total_length"]?.Value<int?>() ?? 0
                                );
                                filenames[i] = filename;
                                beatmapSetInfo.Beatmaps.Add(
                                    new BeatmapInfo(
                                        new RulesetInfo { ShortName = mode, Available = true }
                                    )
                                    {
                                        OnlineID = int.Parse(beatmapID),
                                        BeatmapSet = beatmapSetInfo,
                                    }
                                );
                                string osuFile = await client
                                    .GetStringAsync($"https://osu.ppy.sh/osu/{beatmap["id"]}")
                                    .ConfigureAwait(false);
                                File.WriteAllText(filename, osuFile);
                            }
                        )
                    )
                    .ConfigureAwait(false);
            }
            string dummyAudioFilename = $"{root["title"]}.mp3";
            createSilentWav(dummyAudioFilename, maxTotalLength);
            string oszFilename = $"{beatmapSetId} {root["artist"]} - {root["title"]}.osz";
            using (var zipStream = new FileStream(oszFilename, FileMode.Create))
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    foreach (string file in filenames)
                    {
                        archive.CreateEntryFromFile(file, System.IO.Path.GetFileName(file));
                    }
                    archive.CreateEntryFromFile(dummyAudioFilename, dummyAudioFilename);
                }
            }
            await Server.BeatmapManager.Import(oszFilename).ConfigureAwait(false);
            await Server.SongSelect.BeatmapSetAdded(beatmapSetInfo).ConfigureAwait(false);
            Server.RespondJSON(
                new Dictionary<string, string> { { "message", $"BeatmapSet {beatmapSetId} added" } }
            );
            foreach (string file in filenames)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception) { }
            }
            try
            {
                File.Delete(dummyAudioFilename);
            }
            catch (Exception) { }
            return true;
        }
    }
}
