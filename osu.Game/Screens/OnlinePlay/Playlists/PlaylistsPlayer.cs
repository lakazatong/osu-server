// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Screens;
using osu.Game.Extensions;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.Select.Leaderboards;
using osu.Game.Users;

namespace osu.Game.Screens.OnlinePlay.Playlists
{
    public partial class PlaylistsPlayer : RoomSubmittingPlayer
    {
        public Action? Exited;

        [Cached(typeof(IGameplayLeaderboardProvider))]
        private readonly PlaylistsGameplayLeaderboardProvider leaderboardProvider;

        protected override UserActivity InitialActivity =>
            new UserActivity.InPlaylistGame(Beatmap.Value.BeatmapInfo, Ruleset.Value);

        public PlaylistsPlayer(Room room, PlaylistItem playlistItem)
            : base(room, playlistItem, new PlayerConfiguration { ShowLeaderboard = true })
        {
            leaderboardProvider = new PlaylistsGameplayLeaderboardProvider(room, playlistItem);
        }

        [BackgroundDependencyLoader]
        private void load(IBindable<RulesetInfo> ruleset)
        {
            // Sanity checks to ensure that PlaylistsPlayer matches the settings for the current PlaylistItem
            if (!Beatmap.Value.BeatmapInfo.MatchesOnlineID(PlaylistItem.Beatmap))
                throw new InvalidOperationException(
                    "Current Beatmap does not match PlaylistItem's Beatmap"
                );

            if (ruleset.Value.OnlineID != PlaylistItem.RulesetID)
                throw new InvalidOperationException(
                    "Current Ruleset does not match PlaylistItem's Ruleset"
                );

            var requiredLocalMods = PlaylistItem.RequiredMods.Select(m =>
                m.ToMod(GameplayState.Ruleset)
            );
            if (!requiredLocalMods.All(m => Mods.Value.Any(m.Equals)))
                throw new InvalidOperationException(
                    "Current Mods do not match PlaylistItem's RequiredMods"
                );

            LoadComponentAsync(leaderboardProvider, AddInternal);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            if (base.OnExiting(e))
                return true;

            Exited?.Invoke();

            return false;
        }

        protected override ResultsScreen CreateResults(ScoreInfo score)
        {
            Debug.Assert(Room.RoomID != null);
            return new PlaylistItemScoreResultsScreen(score, Room.RoomID.Value, PlaylistItem)
            {
                AllowRetry = true,
                IsLocalPlay = true,
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            Exited = null;
        }
    }
}
