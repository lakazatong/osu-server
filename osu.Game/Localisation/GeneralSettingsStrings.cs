// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class GeneralSettingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.GeneralSettings";

        /// <summary>
        /// "Language"
        /// </summary>
        public static LocalisableString LanguageHeader =>
            new TranslatableString(getKey(@"language_header"), @"Language");

        /// <summary>
        /// "Language"
        /// </summary>
        public static LocalisableString LanguageDropdown =>
            new TranslatableString(getKey(@"language_dropdown"), @"Language");

        /// <summary>
        /// "Prefer metadata in original language"
        /// </summary>
        public static LocalisableString PreferOriginalMetadataLanguage =>
            new TranslatableString(
                getKey(@"prefer_original"),
                @"Prefer metadata in original language"
            );

        /// <summary>
        /// "Prefer 24-hour time display"
        /// </summary>
        public static LocalisableString Prefer24HourTimeDisplay =>
            new TranslatableString(
                getKey(@"prefer_24_hour_time_display"),
                @"Prefer 24-hour time display"
            );

        /// <summary>
        /// "Updates"
        /// </summary>
        public static LocalisableString UpdateHeader =>
            new TranslatableString(getKey(@"update_header"), @"Updates");

        /// <summary>
        /// "Release stream"
        /// </summary>
        public static LocalisableString ReleaseStream =>
            new TranslatableString(getKey(@"release_stream"), @"Release stream");

        /// <summary>
        /// "Check for updates"
        /// </summary>
        public static LocalisableString CheckUpdate =>
            new TranslatableString(getKey(@"check_update"), @"Check for updates");

        /// <summary>
        /// "Checking for updates"
        /// </summary>
        public static LocalisableString CheckingForUpdates =>
            new TranslatableString(getKey(@"checking_for_updates"), @"Checking for updates");

        /// <summary>
        /// "Open osu! folder"
        /// </summary>
        public static LocalisableString OpenOsuFolder =>
            new TranslatableString(getKey(@"open_osu_folder"), @"Open osu! folder");

        /// <summary>
        /// "Export logs"
        /// </summary>
        public static LocalisableString ExportLogs =>
            new TranslatableString(getKey(@"export_logs"), @"Export logs");

        /// <summary>
        /// "Change folder location..."
        /// </summary>
        public static LocalisableString ChangeFolderLocation =>
            new TranslatableString(getKey(@"change_folder_location"), @"Change folder location...");

        /// <summary>
        /// "Run setup wizard"
        /// </summary>
        public static LocalisableString RunSetupWizard =>
            new TranslatableString(getKey(@"run_setup_wizard"), @"Run setup wizard");

        /// <summary>
        /// "Learn more about lazer"
        /// </summary>
        public static LocalisableString LearnMoreAboutLazer =>
            new TranslatableString(getKey(@"learn_more_about_lazer"), @"Learn more about lazer");

        /// <summary>
        /// "Check out the feature comparison and FAQ"
        /// </summary>
        public static LocalisableString LearnMoreAboutLazerTooltip =>
            new TranslatableString(
                getKey(@"check_out_the_feature_comparison"),
                @"Check out the feature comparison and FAQ"
            );

        /// <summary>
        /// "Check with your package manager / provider for other release streams."
        /// </summary>
        public static LocalisableString ChangeReleaseStreamPackageManagerWarning =>
            new TranslatableString(
                getKey(@"change_release_stream_package_warning"),
                @"Check with your package manager / provider for other release streams."
            );

        /// <summary>
        /// "Are you sure you want to run a potentially unstable version of the game?"
        /// </summary>
        public static LocalisableString ChangeReleaseStreamConfirmation =>
            new TranslatableString(
                getKey(@"change_release_stream_confirmation"),
                @"Are you sure you want to run a potentially unstable version of the game?"
            );

        /// <summary>
        /// "If you run into issues starting the game, you can usually run the installer from the official site to recover."
        /// </summary>
        public static LocalisableString ChangeReleaseStreamConfirmationInfo =>
            new TranslatableString(
                getKey(@"change_release_stream_confirmation_info"),
                @"If you run into issues starting the game, you can usually run the installer from the official site to recover."
            );

        /// <summary>
        /// "You are running the latest release ({0})"
        /// </summary>
        public static LocalisableString RunningLatestRelease(string version) =>
            new TranslatableString(
                getKey(@"running_latest_release"),
                @"You are running the latest release ({0})",
                version
            );

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
