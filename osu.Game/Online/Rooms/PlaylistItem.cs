// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;

namespace osu.Game.Online.Rooms
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PlaylistItem : IEquatable<PlaylistItem>
    {
        [JsonProperty("id")]
        public long ID { get; set; }

        [JsonProperty("owner_id")]
        public int OwnerID { get; set; }

        [JsonProperty("ruleset_id")]
        public int RulesetID { get; set; }

        /// <summary>
        /// Whether this <see cref="PlaylistItem"/> is still a valid selection for the <see cref="Room"/>.
        /// </summary>
        [JsonProperty("expired")]
        public bool Expired { get; set; }

        [JsonProperty("playlist_order")]
        public ushort? PlaylistOrder { get; set; }

        [JsonProperty("played_at")]
        public DateTimeOffset? PlayedAt { get; set; }

        /// <summary>
        /// Mods that participants are allowed to apply at their own discretion.
        /// </summary>
        /// <remarks>
        /// This will be empty when <see cref="Freestyle"/> is <c>true</c>, but participants may still select any mods from their choice of ruleset,
        /// provided the mod is <see cref="ModUtils.CheckCompatibleSet(IEnumerable{Mod})">compatible</see> with the rest of the user's selection.
        /// </remarks>
        [JsonProperty("allowed_mods")]
        public APIMod[] AllowedMods { get; set; } = Array.Empty<APIMod>();

        /// <summary>
        /// Mods that should be applied for every participant in the room.
        /// </summary>
        [JsonProperty("required_mods")]
        public APIMod[] RequiredMods { get; set; } = Array.Empty<APIMod>();

        /// <summary>
        /// Used for deserialising from the API.
        /// </summary>
        [JsonProperty("beatmap")]
        private APIBeatmap apiBeatmap
        {
            // This getter is required/used internally by JSON.NET during deserialisation to do default-value comparisons. It is never used during serialisation (see: ShouldSerializeapiBeatmap()).
            // It will always return a null value on deserialisation, which JSON.NET will handle gracefully.
            get => (APIBeatmap)Beatmap;
            set => Beatmap = value;
        }

        /// <summary>
        /// Used for serialising to the API.
        /// </summary>
        [JsonProperty("beatmap_id")]
        private int onlineBeatmapId
        {
            get => Beatmap.OnlineID;
            // This setter is only required for client-side serialise-then-deserialise operations.
            // Serialisation is supposed to emit only a `beatmap_id`, but a (non-null) `beatmap` is required on deserialise.
            set => Beatmap = new APIBeatmap { OnlineID = value };
        }

        /// <summary>
        /// Indicates whether participants in the room are able to pick their own choice of beatmap difficulty, ruleset, and mods.
        /// </summary>
        [JsonProperty("freestyle")]
        public bool Freestyle { get; set; }

        /// <summary>
        /// A beatmap representing this playlist item.
        /// In many cases, this will *not* contain any usable information apart from OnlineID.
        /// </summary>
        [JsonIgnore]
        public IBeatmapInfo Beatmap { get; private set; }

        [JsonIgnore]
        public IBindable<bool> Valid => valid;

        private readonly Bindable<bool> valid = new BindableBool(true);

        [JsonIgnore]
        public IBindable<bool> Completed => completed;

        private readonly Bindable<bool> completed = new BindableBool(false);

        [JsonConstructor]
        private PlaylistItem()
            : this(new APIBeatmap()) { }

        public PlaylistItem(IBeatmapInfo beatmap)
        {
            Beatmap = beatmap;
        }

        /// <summary>
        /// Creates a new <see cref="PlaylistItem"/> from a <see cref="MultiplayerPlaylistItem"/>.
        /// </summary>
        /// <remarks>
        /// This will create unique instances of the <see cref="RequiredMods"/> and <see cref="AllowedMods"/> arrays but NOT unique instances of the contained <see cref="APIMod"/>s.
        /// </remarks>
        public PlaylistItem(MultiplayerPlaylistItem item)
            : this(
                new APIBeatmap
                {
                    OnlineID = item.BeatmapID,
                    StarRating = item.StarRating,
                    Checksum = item.BeatmapChecksum,
                }
            )
        {
            ID = item.ID;
            OwnerID = item.OwnerID;
            RulesetID = item.RulesetID;
            Expired = item.Expired;
            PlaylistOrder = item.PlaylistOrder;
            PlayedAt = item.PlayedAt;
            RequiredMods = item.RequiredMods.ToArray();
            AllowedMods = item.AllowedMods.ToArray();
            Freestyle = item.Freestyle;
        }

        public void MarkInvalid() => valid.Value = false;

        public void MarkCompleted() => completed.Value = true;

        #region Newtonsoft.Json implicit ShouldSerialize() methods

        // The properties in this region are used implicitly by Newtonsoft.Json to not serialise certain fields in some cases.
        // They rely on being named exactly the same as the corresponding fields (casing included) and as such should NOT be renamed
        // unless the fields are also renamed.

        [UsedImplicitly]
        public bool ShouldSerializeID() => false;

        // ReSharper disable once IdentifierTypo
        [UsedImplicitly]
        public bool ShouldSerializeapiBeatmap() => false;

        #endregion

        public PlaylistItem With(
            Optional<long> id = default,
            Optional<IBeatmapInfo> beatmap = default,
            Optional<ushort?> playlistOrder = default,
            Optional<int> ruleset = default
        )
        {
            return new PlaylistItem(beatmap.GetOr(Beatmap))
            {
                ID = id.GetOr(ID),
                OwnerID = OwnerID,
                RulesetID = ruleset.GetOr(RulesetID),
                Expired = Expired,
                PlaylistOrder = playlistOrder.GetOr(PlaylistOrder),
                PlayedAt = PlayedAt,
                AllowedMods = AllowedMods,
                RequiredMods = RequiredMods,
                Freestyle = Freestyle,
                valid = { Value = Valid.Value },
            };
        }

        public bool Equals(PlaylistItem? other) =>
            ID == other?.ID
            && Beatmap.OnlineID == other.Beatmap.OnlineID
            && RulesetID == other.RulesetID
            && Expired == other.Expired
            && PlaylistOrder == other.PlaylistOrder
            && AllowedMods.SequenceEqual(other.AllowedMods)
            && RequiredMods.SequenceEqual(other.RequiredMods)
            && Freestyle == other.Freestyle;
    }
}
