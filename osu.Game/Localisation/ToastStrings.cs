// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class ToastStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.Toast";

        /// <summary>
        /// "no key bound"
        /// </summary>
        public static LocalisableString NoKeyBound =>
            new TranslatableString(getKey(@"no_key_bound"), @"no key bound");

        /// <summary>
        /// "Music Playback"
        /// </summary>
        public static LocalisableString MusicPlayback =>
            new TranslatableString(getKey(@"music_playback"), @"Music Playback");

        /// <summary>
        /// "Pause track"
        /// </summary>
        public static LocalisableString PauseTrack =>
            new TranslatableString(getKey(@"pause_track"), @"Pause track");

        /// <summary>
        /// "Play track"
        /// </summary>
        public static LocalisableString PlayTrack =>
            new TranslatableString(getKey(@"play_track"), @"Play track");

        /// <summary>
        /// "Restart track"
        /// </summary>
        public static LocalisableString RestartTrack =>
            new TranslatableString(getKey(@"restart_track"), @"Restart track");

        /// <summary>
        /// "Beatmap saved"
        /// </summary>
        public static LocalisableString BeatmapSaved =>
            new TranslatableString(getKey(@"beatmap_saved"), @"Beatmap saved");

        /// <summary>
        /// "Skin saved"
        /// </summary>
        public static LocalisableString SkinSaved =>
            new TranslatableString(getKey(@"skin_saved"), @"Skin saved");

        /// <summary>
        /// "Copied to clipboard"
        /// </summary>
        public static LocalisableString CopiedToClipboard =>
            new TranslatableString(getKey(@"copied_to_clipboard"), @"Copied to clipboard");

        /// <summary>
        /// "Speed changed to {0:N2}x"
        /// </summary>
        public static LocalisableString SpeedChangedTo(double speed) =>
            new TranslatableString(getKey(@"speed_changed"), @"Speed changed to {0:N2}x", speed);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
