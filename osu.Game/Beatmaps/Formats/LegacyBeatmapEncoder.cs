// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Beatmaps.Formats
{
    public class LegacyBeatmapEncoder
    {
        public const int FIRST_LAZER_VERSION = 128;

        private readonly IBeatmap beatmap;

        private readonly ISkin? skin;

        private readonly int onlineRulesetID;

        /// <summary>
        /// Creates a new <see cref="LegacyBeatmapEncoder"/>.
        /// </summary>
        /// <param name="beatmap">The beatmap to encode.</param>
        /// <param name="skin">The beatmap's skin, used for encoding combo colours.</param>
        public LegacyBeatmapEncoder(IBeatmap beatmap, ISkin? skin)
        {
            this.beatmap = beatmap;
            this.skin = skin;

            onlineRulesetID = beatmap.BeatmapInfo.Ruleset.OnlineID;

            if (onlineRulesetID < 0 || onlineRulesetID > 3)
                throw new ArgumentException(
                    "Only beatmaps in the osu, taiko, catch, or mania rulesets can be encoded to the legacy beatmap format.",
                    nameof(beatmap)
                );
        }

        public void Encode(TextWriter writer)
        {
            writer.WriteLine($"osu file format v{FIRST_LAZER_VERSION}");

            writer.WriteLine();
            handleGeneral(writer);

            writer.WriteLine();
            handleEditor(writer);

            writer.WriteLine();
            handleMetadata(writer);

            writer.WriteLine();
            handleDifficulty(writer);

            writer.WriteLine();
            handleEvents(writer);

            writer.WriteLine();
            handleControlPoints(writer);

            writer.WriteLine();
            handleColours(writer);

            writer.WriteLine();
            handleHitObjects(writer);
        }

        private void handleGeneral(TextWriter writer)
        {
            writer.WriteLine("[General]");

            if (!string.IsNullOrEmpty(beatmap.Metadata.AudioFile))
                writer.WriteLine(
                    FormattableString.Invariant(
                        $"AudioFilename: {Path.GetFileName(beatmap.Metadata.AudioFile)}"
                    )
                );
            writer.WriteLine(FormattableString.Invariant($"AudioLeadIn: {beatmap.AudioLeadIn}"));
            writer.WriteLine(
                FormattableString.Invariant($"PreviewTime: {beatmap.Metadata.PreviewTime}")
            );
            writer.WriteLine(FormattableString.Invariant($"Countdown: {(int)beatmap.Countdown}"));
            writer.WriteLine(
                FormattableString.Invariant(
                    $"SampleSet: {toLegacySampleBank(((beatmap.ControlPointInfo as LegacyControlPointInfo)?.SamplePoints.FirstOrDefault() ?? SampleControlPoint.DEFAULT).SampleBank)}"
                )
            );
            writer.WriteLine(
                FormattableString.Invariant($"StackLeniency: {beatmap.StackLeniency}")
            );
            writer.WriteLine(FormattableString.Invariant($"Mode: {onlineRulesetID}"));
            writer.WriteLine(
                FormattableString.Invariant(
                    $"LetterboxInBreaks: {(beatmap.LetterboxInBreaks ? '1' : '0')}"
                )
            );
            // if (beatmap.BeatmapInfo.UseSkinSprites)
            //     writer.WriteLine(@"UseSkinSprites: 1");
            // if (b.AlwaysShowPlayfield)
            //     writer.WriteLine(@"AlwaysShowPlayfield: 1");
            // if (b.OverlayPosition != OverlayPosition.NoChange)
            //     writer.WriteLine(@"OverlayPosition: " + b.OverlayPosition);
            // if (!string.IsNullOrEmpty(b.SkinPreference))
            //     writer.WriteLine(@"SkinPreference:" + b.SkinPreference);
            if (beatmap.EpilepsyWarning)
                writer.WriteLine(@"EpilepsyWarning: 1");
            if (beatmap.CountdownOffset > 0)
                writer.WriteLine(
                    FormattableString.Invariant($@"CountdownOffset: {beatmap.CountdownOffset}")
                );
            if (onlineRulesetID == 3)
                writer.WriteLine(
                    FormattableString.Invariant(
                        $"SpecialStyle: {(beatmap.SpecialStyle ? '1' : '0')}"
                    )
                );
            writer.WriteLine(
                FormattableString.Invariant(
                    $"WidescreenStoryboard: {(beatmap.WidescreenStoryboard ? '1' : '0')}"
                )
            );
            if (beatmap.SamplesMatchPlaybackRate)
                writer.WriteLine(@"SamplesMatchPlaybackRate: 1");
        }

        private void handleEditor(TextWriter writer)
        {
            writer.WriteLine("[Editor]");

            if (beatmap.Bookmarks.Length > 0)
                writer.WriteLine(
                    FormattableString.Invariant($"Bookmarks: {string.Join(',', beatmap.Bookmarks)}")
                );
            writer.WriteLine(
                FormattableString.Invariant($"DistanceSpacing: {beatmap.DistanceSpacing}")
            );
            writer.WriteLine(
                FormattableString.Invariant($"BeatDivisor: {beatmap.BeatmapInfo.BeatDivisor}")
            );
            writer.WriteLine(FormattableString.Invariant($"GridSize: {beatmap.GridSize}"));
            writer.WriteLine(FormattableString.Invariant($"TimelineZoom: {beatmap.TimelineZoom}"));
        }

        private void handleMetadata(TextWriter writer)
        {
            writer.WriteLine("[Metadata]");

            writer.WriteLine(FormattableString.Invariant($"Title: {beatmap.Metadata.Title}"));
            if (!string.IsNullOrEmpty(beatmap.Metadata.TitleUnicode))
                writer.WriteLine(
                    FormattableString.Invariant($"TitleUnicode: {beatmap.Metadata.TitleUnicode}")
                );
            writer.WriteLine(FormattableString.Invariant($"Artist: {beatmap.Metadata.Artist}"));
            if (!string.IsNullOrEmpty(beatmap.Metadata.ArtistUnicode))
                writer.WriteLine(
                    FormattableString.Invariant($"ArtistUnicode: {beatmap.Metadata.ArtistUnicode}")
                );
            writer.WriteLine(
                FormattableString.Invariant($"Creator: {beatmap.Metadata.Author.Username}")
            );
            writer.WriteLine(
                FormattableString.Invariant($"Version: {beatmap.BeatmapInfo.DifficultyName}")
            );
            if (!string.IsNullOrEmpty(beatmap.Metadata.Source))
                writer.WriteLine(FormattableString.Invariant($"Source: {beatmap.Metadata.Source}"));
            if (!string.IsNullOrEmpty(beatmap.Metadata.Tags))
                writer.WriteLine(FormattableString.Invariant($"Tags: {beatmap.Metadata.Tags}"));
            if (beatmap.BeatmapInfo.OnlineID > 0)
                writer.WriteLine(
                    FormattableString.Invariant($"BeatmapID: {beatmap.BeatmapInfo.OnlineID}")
                );
            if (beatmap.BeatmapInfo.BeatmapSet?.OnlineID > 0)
                writer.WriteLine(
                    FormattableString.Invariant(
                        $"BeatmapSetID: {beatmap.BeatmapInfo.BeatmapSet.OnlineID}"
                    )
                );
        }

        private void handleDifficulty(TextWriter writer)
        {
            writer.WriteLine("[Difficulty]");

            writer.WriteLine(
                FormattableString.Invariant($"HPDrainRate: {beatmap.Difficulty.DrainRate}")
            );
            writer.WriteLine(
                FormattableString.Invariant($"CircleSize: {beatmap.Difficulty.CircleSize}")
            );
            writer.WriteLine(
                FormattableString.Invariant(
                    $"OverallDifficulty: {beatmap.Difficulty.OverallDifficulty}"
                )
            );
            writer.WriteLine(
                FormattableString.Invariant($"ApproachRate: {beatmap.Difficulty.ApproachRate}")
            );

            writer.WriteLine(
                FormattableString.Invariant(
                    $"SliderMultiplier: {beatmap.Difficulty.SliderMultiplier}"
                )
            );
            writer.WriteLine(
                FormattableString.Invariant($"SliderTickRate: {beatmap.Difficulty.SliderTickRate}")
            );
        }

        private void handleEvents(TextWriter writer)
        {
            writer.WriteLine("[Events]");

            if (!string.IsNullOrEmpty(beatmap.BeatmapInfo.Metadata.BackgroundFile))
                writer.WriteLine(
                    FormattableString.Invariant(
                        $"{(int)LegacyEventType.Background},0,\"{beatmap.BeatmapInfo.Metadata.BackgroundFile}\",0,0"
                    )
                );

            foreach (var b in beatmap.Breaks)
                writer.WriteLine(
                    FormattableString.Invariant(
                        $"{(int)LegacyEventType.Break},{b.StartTime},{b.EndTime}"
                    )
                );

            foreach (string l in beatmap.UnhandledEventLines)
                writer.WriteLine(l);
        }

        private void handleControlPoints(TextWriter writer)
        {
            var legacyControlPoints = new LegacyControlPointInfo();
            foreach (var point in beatmap.ControlPointInfo.AllControlPoints)
                legacyControlPoints.Add(point.Time, point.DeepClone());

            writer.WriteLine("[TimingPoints]");

            SampleControlPoint? lastRelevantSamplePoint = null;
            DifficultyControlPoint? lastRelevantDifficultyPoint = null;

            // In osu!taiko and osu!mania, a scroll speed is stored as "slider velocity" in legacy formats.
            // In that case, a scrolling speed change is a global effect and per-hit object difficulty control points are ignored.
            bool scrollSpeedEncodedAsSliderVelocity = onlineRulesetID == 1 || onlineRulesetID == 3;

            // iterate over hitobjects and pull out all required sample and difficulty changes
            extractDifficultyControlPoints(beatmap.HitObjects);
            extractSampleControlPoints(beatmap.HitObjects);

            if (scrollSpeedEncodedAsSliderVelocity)
            {
                foreach (var point in legacyControlPoints.EffectPoints)
                {
                    legacyControlPoints.Add(
                        point.Time,
                        new DifficultyControlPoint
                        {
                            SliderVelocityBindable =
                            {
                                MinValue = point.ScrollSpeedBindable.MinValue,
                                MaxValue = point.ScrollSpeedBindable.MaxValue,
                                Value = point.ScrollSpeedBindable.Value,
                            },
                        }
                    );
                }
            }

            LegacyControlPointProperties lastControlPointProperties =
                new LegacyControlPointProperties();

            foreach (var group in legacyControlPoints.Groups)
            {
                var groupTimingPoint = group
                    .ControlPoints.OfType<TimingControlPoint>()
                    .FirstOrDefault();
                var controlPointProperties = getLegacyControlPointProperties(
                    group,
                    groupTimingPoint != null
                );

                // If the group contains a timing control point, it needs to be output separately.
                if (groupTimingPoint != null)
                {
                    writer.Write(FormattableString.Invariant($"{groupTimingPoint.Time},"));
                    writer.Write(FormattableString.Invariant($"{groupTimingPoint.BeatLength},"));
                    outputControlPointAt(controlPointProperties, true);
                    lastControlPointProperties = controlPointProperties;
                    lastControlPointProperties.SliderVelocity = 1;
                }

                if (controlPointProperties.IsRedundant(lastControlPointProperties))
                    continue;

                // Output any remaining effects as secondary non-timing control point.
                writer.Write(FormattableString.Invariant($"{group.Time},"));
                writer.Write(
                    FormattableString.Invariant($"{-100 / controlPointProperties.SliderVelocity},")
                );
                outputControlPointAt(controlPointProperties, false);
                lastControlPointProperties = controlPointProperties;
            }

            LegacyControlPointProperties getLegacyControlPointProperties(
                ControlPointGroup group,
                bool updateSampleBank
            )
            {
                var timingPoint = legacyControlPoints.TimingPointAt(group.Time);
                var difficultyPoint = legacyControlPoints.DifficultyPointAt(group.Time);
                var samplePoint = legacyControlPoints.SamplePointAt(group.Time);
                var effectPoint = legacyControlPoints.EffectPointAt(group.Time);

                // Apply the control point to a hit sample to uncover legacy properties (e.g. suffix)
                HitSampleInfo tempHitSample = samplePoint.ApplyTo(
                    new ConvertHitObjectParser.LegacyHitSampleInfo(string.Empty)
                );
                int customSampleBank = toLegacyCustomSampleBank(tempHitSample);

                // Convert effect flags to the legacy format
                LegacyEffectFlags effectFlags = LegacyEffectFlags.None;
                if (effectPoint.KiaiMode)
                    effectFlags |= LegacyEffectFlags.Kiai;
                if (timingPoint.OmitFirstBarLine)
                    effectFlags |= LegacyEffectFlags.OmitFirstBarLine;

                return new LegacyControlPointProperties
                {
                    SliderVelocity = difficultyPoint.SliderVelocity,
                    TimingSignature = timingPoint.TimeSignature.Numerator,
                    SampleBank = updateSampleBank
                        ? (int)toLegacySampleBank(tempHitSample.Bank)
                        : lastControlPointProperties.SampleBank,
                    // Inherit the previous custom sample bank if the current custom sample bank is not set
                    CustomSampleBank =
                        customSampleBank >= 0
                            ? customSampleBank
                            : lastControlPointProperties.CustomSampleBank,
                    SampleVolume = tempHitSample.Volume,
                    EffectFlags = effectFlags,
                };
            }

            void outputControlPointAt(LegacyControlPointProperties controlPoint, bool isTimingPoint)
            {
                writer.Write(
                    FormattableString.Invariant(
                        $"{controlPoint.TimingSignature.ToString(CultureInfo.InvariantCulture)},"
                    )
                );
                writer.Write(
                    FormattableString.Invariant(
                        $"{controlPoint.SampleBank.ToString(CultureInfo.InvariantCulture)},"
                    )
                );
                writer.Write(
                    FormattableString.Invariant(
                        $"{controlPoint.CustomSampleBank.ToString(CultureInfo.InvariantCulture)},"
                    )
                );
                writer.Write(
                    FormattableString.Invariant(
                        $"{controlPoint.SampleVolume.ToString(CultureInfo.InvariantCulture)},"
                    )
                );
                writer.Write(FormattableString.Invariant($"{(isTimingPoint ? "1" : "0")},"));
                writer.Write(
                    FormattableString.Invariant(
                        $"{((int)controlPoint.EffectFlags).ToString(CultureInfo.InvariantCulture)}"
                    )
                );
                writer.WriteLine();
            }

            IEnumerable<DifficultyControlPoint> collectDifficultyControlPoints(
                IEnumerable<HitObject> hitObjects
            )
            {
                if (scrollSpeedEncodedAsSliderVelocity)
                    yield break;

                foreach (var hitObject in hitObjects)
                {
                    if (hitObject is IHasSliderVelocity hasSliderVelocity)
                        yield return new DifficultyControlPoint
                        {
                            Time = hitObject.StartTime,
                            SliderVelocity = hasSliderVelocity.SliderVelocityMultiplier,
                        };
                }
            }

            void extractDifficultyControlPoints(IEnumerable<HitObject> hitObjects)
            {
                foreach (
                    var hDifficultyPoint in collectDifficultyControlPoints(hitObjects)
                        .OrderBy(dp => dp.Time)
                )
                {
                    if (!hDifficultyPoint.IsRedundant(lastRelevantDifficultyPoint))
                    {
                        legacyControlPoints.Add(hDifficultyPoint.Time, hDifficultyPoint);
                        lastRelevantDifficultyPoint = hDifficultyPoint;
                    }
                }
            }

            IEnumerable<SampleControlPoint> collectSampleControlPoints(
                IEnumerable<HitObject> hitObjects
            )
            {
                foreach (var hitObject in hitObjects)
                {
                    if (hitObject is IHasRepeats hasNodeSamples)
                    {
                        double spanDuration = hasNodeSamples.Duration / hasNodeSamples.SpanCount();

                        for (int i = 0; i < hasNodeSamples.NodeSamples.Count; ++i)
                        {
                            double nodeTime = hitObject.StartTime + i * spanDuration;

                            if (hasNodeSamples.NodeSamples[i].Count > 0)
                                yield return createSampleControlPointFor(
                                    nodeTime,
                                    hasNodeSamples.NodeSamples[i]
                                );

                            if (
                                spanDuration > LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY + 1
                                && hitObject.Samples.Count > 0
                                && i < hasNodeSamples.NodeSamples.Count - 1
                            )
                                yield return createSampleControlPointFor(
                                    nodeTime + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY + 1,
                                    hitObject.Samples
                                );
                        }
                    }
                    else if (hitObject.Samples.Count > 0)
                    {
                        yield return createSampleControlPointFor(
                            hitObject.GetEndTime(),
                            hitObject.Samples
                        );
                    }

                    foreach (var nested in collectSampleControlPoints(hitObject.NestedHitObjects))
                        yield return nested;
                }

                SampleControlPoint createSampleControlPointFor(
                    double time,
                    IList<HitSampleInfo> samples
                )
                {
                    int volume = samples.Max(o => o.Volume);
                    string bank =
                        samples
                            .Where(s => s.Name == HitSampleInfo.HIT_NORMAL)
                            .Select(s => s.Bank)
                            .FirstOrDefault() ?? samples.Select(s => s.Bank).First();
                    int customIndex = samples.Any(o =>
                        o is ConvertHitObjectParser.LegacyHitSampleInfo
                    )
                        ? samples
                            .OfType<ConvertHitObjectParser.LegacyHitSampleInfo>()
                            .Max(o => o.CustomSampleBank)
                        : -1;

                    return new LegacyBeatmapDecoder.LegacySampleControlPoint
                    {
                        Time = time,
                        SampleVolume = volume,
                        SampleBank = bank,
                        CustomSampleBank = customIndex,
                    };
                }
            }

            void extractSampleControlPoints(IEnumerable<HitObject> hitObject)
            {
                foreach (
                    var hSamplePoint in collectSampleControlPoints(hitObject).OrderBy(sp => sp.Time)
                )
                {
                    if (!hSamplePoint.IsRedundant(lastRelevantSamplePoint))
                    {
                        legacyControlPoints.Add(hSamplePoint.Time, hSamplePoint);
                        lastRelevantSamplePoint = hSamplePoint;
                    }
                }
            }
        }

        private void handleColours(TextWriter writer)
        {
            var colours = skin
                ?.GetConfig<GlobalSkinColours, IReadOnlyList<Color4>>(
                    GlobalSkinColours.ComboColours
                )
                ?.Value;

            if (colours == null || colours.Count == 0)
                return;

            writer.WriteLine("[Colours]");

            for (
                int i = 0;
                i < Math.Min(colours.Count, LegacyBeatmapDecoder.MAX_COMBO_COLOUR_COUNT);
                i++
            )
            {
                var comboColour = colours[i];

                writer.Write(FormattableString.Invariant($"Combo{1 + i}: "));
                writer.Write(
                    FormattableString.Invariant($"{(byte)(comboColour.R * byte.MaxValue)},")
                );
                writer.Write(
                    FormattableString.Invariant($"{(byte)(comboColour.G * byte.MaxValue)},")
                );
                writer.Write(
                    FormattableString.Invariant($"{(byte)(comboColour.B * byte.MaxValue)},")
                );
                writer.Write(
                    FormattableString.Invariant($"{(byte)(comboColour.A * byte.MaxValue)}")
                );
                writer.WriteLine();
            }
        }

        private void handleHitObjects(TextWriter writer)
        {
            writer.WriteLine("[HitObjects]");

            if (beatmap.HitObjects.Count == 0)
                return;

            foreach (var h in beatmap.HitObjects)
                handleHitObject(writer, h);
        }

        private void handleHitObject(TextWriter writer, HitObject hitObject)
        {
            Vector2 position = new Vector2(256, 192);

            switch (onlineRulesetID)
            {
                case 0:
                case 2:
                    position = ((IHasPosition)hitObject).Position;
                    break;

                case 3:
                    int totalColumns = (int)Math.Max(1, beatmap.Difficulty.CircleSize);
                    position.X = (int)
                        Math.Ceiling(((IHasXPosition)hitObject).X * (512f / totalColumns));
                    break;
            }

            writer.Write(FormattableString.Invariant($"{position.X},"));
            writer.Write(FormattableString.Invariant($"{position.Y},"));
            writer.Write(FormattableString.Invariant($"{hitObject.StartTime},"));
            writer.Write(FormattableString.Invariant($"{(int)getObjectType(hitObject)},"));
            writer.Write(
                FormattableString.Invariant($"{(int)toLegacyHitSoundType(hitObject.Samples)},")
            );

            if (hitObject is IHasPath path)
            {
                addPathData(writer, path, position);
                writer.Write(getSampleBank(hitObject.Samples));
            }
            else
            {
                if (hitObject is IHasDuration)
                    addEndTimeData(writer, hitObject);

                writer.Write(getSampleBank(hitObject.Samples));
            }

            writer.WriteLine();
        }

        private LegacyHitObjectType getObjectType(HitObject hitObject)
        {
            LegacyHitObjectType type = 0;

            if (hitObject is IHasCombo combo)
            {
                type = (LegacyHitObjectType)(combo.ComboOffset << 4);

                if (combo.NewCombo)
                    type |= LegacyHitObjectType.NewCombo;
            }

            switch (hitObject)
            {
                case IHasPath:
                    type |= LegacyHitObjectType.Slider;
                    break;

                case IHasDuration:
                    if (onlineRulesetID == 3)
                        type |= LegacyHitObjectType.Hold;
                    else
                        type |= LegacyHitObjectType.Spinner;
                    break;

                default:
                    type |= LegacyHitObjectType.Circle;
                    break;
            }

            return type;
        }

        private void addPathData(TextWriter writer, IHasPath pathData, Vector2 position)
        {
            for (int i = 0; i < pathData.Path.ControlPoints.Count; i++)
            {
                PathControlPoint point = pathData.Path.ControlPoints[i];

                // Note that lazer's encoding format supports specifying multiple curve types for a slider path, which is not supported by stable.
                // Backwards compatibility with stable is handled by `LegacyBeatmapExporter` and `BezierConverter.ConvertToModernBezier()`.
                if (point.Type != null)
                {
                    switch (point.Type?.Type)
                    {
                        case SplineType.BSpline:
                            writer.Write(
                                point.Type.Value.Degree > 0 ? $"B{point.Type.Value.Degree}|" : "B|"
                            );
                            break;

                        case SplineType.Catmull:
                            writer.Write("C|");
                            break;

                        case SplineType.PerfectCurve:
                            writer.Write("P|");
                            break;

                        case SplineType.Linear:
                            writer.Write("L|");
                            break;
                    }
                }

                if (i != 0)
                {
                    writer.Write(
                        FormattableString.Invariant(
                            $"{position.X + point.Position.X}:{position.Y + point.Position.Y}"
                        )
                    );
                    writer.Write(i != pathData.Path.ControlPoints.Count - 1 ? "|" : ",");
                }
            }

            var curveData = pathData as IHasPathWithRepeats;

            writer.Write(FormattableString.Invariant($"{(curveData?.RepeatCount ?? 0) + 1},"));
            writer.Write(
                FormattableString.Invariant(
                    $"{pathData.Path.ExpectedDistance.Value ?? pathData.Path.Distance},"
                )
            );

            if (curveData != null)
            {
                for (int i = 0; i < curveData.SpanCount() + 1; i++)
                {
                    writer.Write(
                        FormattableString.Invariant(
                            $"{(i < curveData.NodeSamples.Count ? (int)toLegacyHitSoundType(curveData.NodeSamples[i]) : 0)}"
                        )
                    );
                    writer.Write(i != curveData.SpanCount() ? "|" : ",");
                }

                for (int i = 0; i < curveData.SpanCount() + 1; i++)
                {
                    writer.Write(
                        i < curveData.NodeSamples.Count
                            ? getSampleBank(curveData.NodeSamples[i], true)
                            : "0:0"
                    );
                    writer.Write(i != curveData.SpanCount() ? "|" : ",");
                }
            }
        }

        private void addEndTimeData(TextWriter writer, HitObject hitObject)
        {
            var endTimeData = (IHasDuration)hitObject;
            var type = getObjectType(hitObject);

            char suffix = ',';

            // Holds write the end time as if it's part of sample data.
            if (type == LegacyHitObjectType.Hold)
                suffix = ':';

            writer.Write(FormattableString.Invariant($"{endTimeData.EndTime}{suffix}"));
        }

        private string getSampleBank(IList<HitSampleInfo> samples, bool banksOnly = false)
        {
            LegacySampleBank normalBank = toLegacySampleBank(
                samples.SingleOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL)?.Bank
            );
            LegacySampleBank addBank = toLegacySampleBank(
                samples
                    .FirstOrDefault(s =>
                        !string.IsNullOrEmpty(s.Name)
                        && s.Name != HitSampleInfo.HIT_NORMAL
                        && !s.EditorAutoBank
                    )
                    ?.Bank
            );

            StringBuilder sb = new StringBuilder();

            sb.Append(FormattableString.Invariant($"{(int)normalBank}:"));
            sb.Append(FormattableString.Invariant($"{(int)addBank}"));

            if (!banksOnly)
            {
                int customSampleBank = toLegacyCustomSampleBank(
                    samples.FirstOrDefault(s => !string.IsNullOrEmpty(s.Name))
                );
                string sampleFilename =
                    samples.FirstOrDefault(s => string.IsNullOrEmpty(s.Name))?.LookupNames.First()
                    ?? string.Empty;
                int volume = samples.FirstOrDefault()?.Volume ?? 100;

                // We want to ignore custom sample banks and volume when not encoding to the mania game mode,
                // because they cause unexpected results in the editor and are already satisfied by the control points.
                if (onlineRulesetID != 3)
                {
                    customSampleBank = 0;
                    volume = 0;
                }

                sb.Append(':');
                sb.Append(FormattableString.Invariant($"{customSampleBank}:"));
                sb.Append(FormattableString.Invariant($"{volume}:"));
                sb.Append(FormattableString.Invariant($"{sampleFilename}"));
            }

            return sb.ToString();
        }

        private LegacyHitSoundType toLegacyHitSoundType(IList<HitSampleInfo> samples)
        {
            LegacyHitSoundType type = LegacyHitSoundType.None;

            foreach (var sample in samples)
            {
                switch (sample.Name)
                {
                    case HitSampleInfo.HIT_WHISTLE:
                        type |= LegacyHitSoundType.Whistle;
                        break;

                    case HitSampleInfo.HIT_FINISH:
                        type |= LegacyHitSoundType.Finish;
                        break;

                    case HitSampleInfo.HIT_CLAP:
                        type |= LegacyHitSoundType.Clap;
                        break;
                }
            }

            return type;
        }

        private LegacySampleBank toLegacySampleBank(string? sampleBank)
        {
            switch (sampleBank?.ToLowerInvariant())
            {
                case HitSampleInfo.BANK_NORMAL:
                    return LegacySampleBank.Normal;

                case HitSampleInfo.BANK_SOFT:
                    return LegacySampleBank.Soft;

                case HitSampleInfo.BANK_DRUM:
                    return LegacySampleBank.Drum;

                default:
                    return LegacySampleBank.None;
            }
        }

        private int toLegacyCustomSampleBank(HitSampleInfo? hitSampleInfo)
        {
            if (hitSampleInfo is ConvertHitObjectParser.LegacyHitSampleInfo legacy)
                return legacy.CustomSampleBank;

            return 0;
        }

        private struct LegacyControlPointProperties
        {
            internal double SliderVelocity { get; set; }
            internal int TimingSignature { get; init; }
            internal int SampleBank { get; init; }
            internal int CustomSampleBank { get; init; }
            internal int SampleVolume { get; init; }
            internal LegacyEffectFlags EffectFlags { get; init; }

            internal bool IsRedundant(LegacyControlPointProperties other) =>
                SliderVelocity == other.SliderVelocity
                && TimingSignature == other.TimingSignature
                && SampleBank == other.SampleBank
                && CustomSampleBank == other.CustomSampleBank
                && SampleVolume == other.SampleVolume
                && EffectFlags == other.EffectFlags;
        }
    }
}
