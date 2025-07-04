﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using osu.Framework.Localisation;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Scoring;
using osu.Game.Utils;

namespace osu.Game.Users
{
    public class UserStatistics
    {
        [JsonProperty]
        public APIUser User;

        [JsonProperty(@"level")]
        public LevelInfo Level;

        public struct LevelInfo
        {
            [JsonProperty(@"current")]
            public int Current;

            [JsonProperty(@"progress")]
            public int Progress;
        }

        [JsonProperty(@"is_ranked")]
        public bool IsRanked;

        [JsonProperty(@"global_rank")]
        public int? GlobalRank;

        [JsonProperty(@"country_rank")]
        public int? CountryRank;

        // populated via User model, as that's where the data currently lives.
        public APIRankHistory RankHistory;

        [JsonProperty(@"pp")]
        public decimal? PP;

        [JsonProperty(@"ranked_score")]
        public long RankedScore;

        [JsonProperty(@"hit_accuracy")]
        public double Accuracy;

        [JsonIgnore]
        public LocalisableString DisplayAccuracy => (Accuracy / 100).FormatAccuracy();

        [JsonProperty(@"play_count")]
        public int PlayCount;

        [JsonProperty(@"play_time")]
        public int? PlayTime;

        [JsonProperty(@"total_score")]
        public long TotalScore;

        [JsonProperty(@"total_hits")]
        public int TotalHits;

        [JsonProperty(@"maximum_combo")]
        public int MaxCombo;

        [JsonProperty(@"replays_watched_by_others")]
        public int ReplaysWatched;

        [JsonProperty(@"grade_counts")]
        public Grades GradesCount;

        [JsonProperty(@"variants")]
        [CanBeNull]
        public List<Variant> Variants;

        public struct Grades
        {
            [JsonProperty(@"ssh")]
            public int? SSPlus;

            [JsonProperty(@"ss")]
            public int SS;

            [JsonProperty(@"sh")]
            public int? SPlus;

            [JsonProperty(@"s")]
            public int S;

            [JsonProperty(@"a")]
            public int A;

            public int this[ScoreRank rank]
            {
                get
                {
                    switch (rank)
                    {
                        case ScoreRank.XH:
                            return SSPlus ?? 0;

                        case ScoreRank.X:
                            return SS;

                        case ScoreRank.SH:
                            return SPlus ?? 0;

                        case ScoreRank.S:
                            return S;

                        case ScoreRank.A:
                            return A;

                        default:
                            throw new ArgumentException($"API does not return {rank.ToString()}");
                    }
                }
            }
        }

        public enum RulesetVariant
        {
            [EnumMember(Value = "4k")]
            [LocalisableDescription(
                typeof(BeatmapsStrings),
                nameof(BeatmapsStrings.VariantMania4k)
            )]
            FourKey,

            [EnumMember(Value = "7k")]
            [LocalisableDescription(
                typeof(BeatmapsStrings),
                nameof(BeatmapsStrings.VariantMania7k)
            )]
            SevenKey,
        }

        public class Variant
        {
            [JsonProperty("country_rank")]
            public int? CountryRank;

            [JsonProperty("global_rank")]
            public int? GlobalRank;

            [JsonProperty("mode")]
            public string Mode;

            [JsonProperty("pp")]
            public decimal PP;

            [JsonProperty("variant")]
            [JsonConverter(typeof(StringEnumConverter))]
            public RulesetVariant VariantType;
        }
    }
}
