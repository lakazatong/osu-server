﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.IO.Serialization;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Skinning;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public abstract class BeatmapConversionTest<TConvertMapping, TConvertValue>
        where TConvertMapping : ConvertMapping<TConvertValue>, IEquatable<TConvertMapping>, new()
        where TConvertValue : IEquatable<TConvertValue>
    {
        private const string resource_namespace = "Testing.Beatmaps";
        private const string expected_conversion_suffix = "-expected-conversion";

        protected abstract string ResourceAssembly { get; }

        protected void Test(string name, params Type[] mods)
        {
            var ourResult = convert(
                name,
                mods.Select(m => (Mod)Activator.CreateInstance(m)).ToArray()
            );
            var expectedResult = read(name);

            foreach (var m in ourResult.Mappings)
                m.PostProcess();

            foreach (var m in expectedResult.Mappings)
                m.PostProcess();

            Assert.Multiple(() =>
            {
                int mappingCounter = 0;

                while (true)
                {
                    if (
                        mappingCounter >= ourResult.Mappings.Count
                        && mappingCounter >= expectedResult.Mappings.Count
                    )
                        break;

                    if (mappingCounter >= ourResult.Mappings.Count)
                        Assert.Fail(
                            $"A conversion did not generate any hitobjects, but should have, for hitobject at time: {expectedResult.Mappings[mappingCounter].StartTime}\n"
                        );
                    else if (mappingCounter >= expectedResult.Mappings.Count)
                        Assert.Fail(
                            $"A conversion generated hitobjects, but should not have, for hitobject at time: {ourResult.Mappings[mappingCounter].StartTime}\n"
                        );
                    else if (
                        !expectedResult
                            .Mappings[mappingCounter]
                            .Equals(ourResult.Mappings[mappingCounter])
                    )
                    {
                        var expectedMapping = expectedResult.Mappings[mappingCounter];
                        var ourMapping = ourResult.Mappings[mappingCounter];

                        Assert.Fail(
                            $"The conversion mapping differed for object at time {expectedMapping.StartTime}:\n"
                                + $"Expected {JsonConvert.SerializeObject(expectedMapping)}\n"
                                + $"Received: {JsonConvert.SerializeObject(ourMapping)}\n"
                        );
                    }
                    else
                    {
                        var ourMapping = ourResult.Mappings[mappingCounter];
                        var expectedMapping = expectedResult.Mappings[mappingCounter];

                        Assert.Multiple(() =>
                        {
                            int objectCounter = 0;

                            while (true)
                            {
                                if (
                                    objectCounter >= ourMapping.Objects.Count
                                    && objectCounter >= expectedMapping.Objects.Count
                                )
                                    break;

                                if (objectCounter >= ourMapping.Objects.Count)
                                {
                                    Assert.Fail(
                                        $"The conversion did not generate a hitobject, but should have, for hitobject at time: {expectedMapping.StartTime}:\n"
                                            + $"Expected: {JsonConvert.SerializeObject(expectedMapping.Objects[objectCounter])}\n"
                                    );
                                }
                                else if (objectCounter >= expectedMapping.Objects.Count)
                                {
                                    Assert.Fail(
                                        $"The conversion generated a hitobject, but should not have, for hitobject at time: {ourMapping.StartTime}:\n"
                                            + $"Received: {JsonConvert.SerializeObject(ourMapping.Objects[objectCounter])}\n"
                                    );
                                }
                                else if (
                                    !expectedMapping
                                        .Objects[objectCounter]
                                        .Equals(ourMapping.Objects[objectCounter])
                                )
                                {
                                    Assert.Fail(
                                        $"The conversion generated differing hitobjects for object at time: {expectedMapping.StartTime}:\n"
                                            + $"Expected: {JsonConvert.SerializeObject(expectedMapping.Objects[objectCounter])}\n"
                                            + $"Received: {JsonConvert.SerializeObject(ourMapping.Objects[objectCounter])}\n"
                                    );
                                }

                                objectCounter++;
                            }
                        });
                    }

                    mappingCounter++;
                }
            });
        }

        private ConvertResult convert(string name, Mod[] mods)
        {
            var conversionTask = Task.Factory.StartNew(
                () =>
                {
                    var beatmap = GetBeatmap(name);

                    string beforeConversion = beatmap.Serialize();

                    var converterResult = new Dictionary<HitObject, IEnumerable<HitObject>>();

                    var working = new ConversionWorkingBeatmap(beatmap)
                    {
                        ConversionGenerated = (o, r, c) =>
                        {
                            converterResult[o] = r;
                            OnConversionGenerated(o, r, c);
                        },
                    };

                    working.GetPlayableBeatmap(CreateRuleset().RulesetInfo, mods);

                    string afterConversion = beatmap.Serialize();

                    Assert.AreEqual(
                        beforeConversion,
                        afterConversion,
                        "Conversion altered original beatmap"
                    );

                    return new ConvertResult
                    {
                        Mappings = converterResult
                            .Select(r =>
                            {
                                var mapping = CreateConvertMapping(r.Key);
                                mapping.StartTime = r.Key.StartTime;
                                mapping.Objects.AddRange(r.Value.SelectMany(CreateConvertValue));
                                return mapping;
                            })
                            .ToList(),
                    };
                },
                TaskCreationOptions.LongRunning
            );

            if (!conversionTask.Wait(10000))
                Assert.Fail("Conversion timed out");

            return conversionTask.GetResultSafely();
        }

        protected virtual void OnConversionGenerated(
            HitObject original,
            IEnumerable<HitObject> result,
            IBeatmapConverter beatmapConverter
        ) { }

        private ConvertResult read(string name)
        {
            using (
                var resStream = openResource(
                    $"{resource_namespace}.{name}{expected_conversion_suffix}.json"
                )
            )
            using (var reader = new StreamReader(resStream))
            {
                string contents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<ConvertResult>(contents);
            }
        }

        public IBeatmap GetBeatmap(string name)
        {
            using (var resStream = openResource($"{resource_namespace}.{name}.osu"))
            using (var stream = new LineBufferedReader(resStream))
            {
                var decoder = Decoder.GetDecoder<Beatmap>(stream);
                ((LegacyBeatmapDecoder)decoder).ApplyOffsets = false;
                var beatmap = decoder.Decode(stream);

                var rulesetInstance = CreateRuleset();
                beatmap.BeatmapInfo.Ruleset =
                    beatmap.BeatmapInfo.Ruleset.OnlineID == rulesetInstance.RulesetInfo.OnlineID
                        ? rulesetInstance.RulesetInfo
                        : new RulesetInfo();

                return beatmap;
            }
        }

        private Stream openResource(string name)
        {
            string localPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                .AsNonNull();
            return Assembly
                .LoadFrom(Path.Combine(localPath, $"{ResourceAssembly}.dll"))
                .GetManifestResourceStream($@"{ResourceAssembly}.Resources.{name}");
        }

        /// <summary>
        /// Creates the conversion mapping for a <see cref="HitObject"/>. A conversion mapping stores important information about the conversion process.
        /// This is generated _after_ the <see cref="HitObject"/> has been converted.
        /// <para>
        /// This should be used to validate the integrity of the conversion process after a conversion has occurred.
        /// </para>
        /// </summary>
        protected virtual TConvertMapping CreateConvertMapping(HitObject source) =>
            new TConvertMapping();

        /// <summary>
        /// Creates the conversion value for a <see cref="HitObject"/>. A conversion value stores information about the converted <see cref="HitObject"/>.
        /// <para>
        /// This should be used to validate the integrity of the converted <see cref="HitObject"/>.
        /// </para>
        /// </summary>
        /// <param name="hitObject">The converted <see cref="HitObject"/>.</param>
        protected abstract IEnumerable<TConvertValue> CreateConvertValue(HitObject hitObject);

        /// <summary>
        /// Creates the <see cref="Ruleset"/> applicable to this <see cref="BeatmapConversionTest{TConvertMapping,TConvertValue}"/>.
        /// </summary>
        protected abstract Ruleset CreateRuleset();

        private class ConvertResult
        {
            [JsonProperty]
            public List<TConvertMapping> Mappings = new List<TConvertMapping>();
        }

        private class ConversionWorkingBeatmap : WorkingBeatmap
        {
            public Action<HitObject, IEnumerable<HitObject>, IBeatmapConverter> ConversionGenerated;

            private readonly IBeatmap beatmap;

            public ConversionWorkingBeatmap(IBeatmap beatmap)
                : base(beatmap.BeatmapInfo, null)
            {
                this.beatmap = beatmap;
            }

            protected override IBeatmap GetBeatmap() => beatmap;

            public override Texture GetBackground() => throw new NotImplementedException();

            protected override Track GetBeatmapTrack() => throw new NotImplementedException();

            protected internal override ISkin GetSkin() => throw new NotImplementedException();

            public override Stream GetStream(string storagePath) =>
                throw new NotImplementedException();

            protected override IBeatmapConverter CreateBeatmapConverter(
                IBeatmap beatmap,
                Ruleset ruleset
            )
            {
                var converter = base.CreateBeatmapConverter(beatmap, ruleset);
                converter.ObjectConverted += (orig, converted) =>
                    ConversionGenerated?.Invoke(orig, converted, converter);
                return converter;
            }
        }
    }

    public abstract class BeatmapConversionTest<TConvertValue>
        : BeatmapConversionTest<ConvertMapping<TConvertValue>, TConvertValue>
        where TConvertValue : IEquatable<TConvertValue> { }

    public class ConvertMapping<TConvertValue> : IEquatable<ConvertMapping<TConvertValue>>
        where TConvertValue : IEquatable<TConvertValue>
    {
        [JsonProperty]
        public double StartTime;

        [JsonIgnore]
        public List<TConvertValue> Objects = new List<TConvertValue>();

        [JsonProperty("Objects")]
        private List<TConvertValue> setObjects
        {
            set => Objects = value;
        }

        /// <summary>
        /// Invoked after this <see cref="ConvertMapping{TConvertValue}"/> is populated to post-process the contained data.
        /// </summary>
        public virtual void PostProcess() { }

        public virtual bool Equals(ConvertMapping<TConvertValue> other) =>
            StartTime == other?.StartTime;
    }
}
