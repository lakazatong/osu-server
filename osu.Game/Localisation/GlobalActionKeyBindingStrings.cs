// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class GlobalActionKeyBindingStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.GlobalActionKeyBinding";

        /// <summary>
        /// "Toggle chat overlay"
        /// </summary>
        public static LocalisableString ToggleChat =>
            new TranslatableString(getKey(@"toggle_chat"), @"Toggle chat overlay");

        /// <summary>
        /// "Toggle social overlay"
        /// </summary>
        public static LocalisableString ToggleSocial =>
            new TranslatableString(getKey(@"toggle_social"), @"Toggle social overlay");

        /// <summary>
        /// "Reset input settings"
        /// </summary>
        public static LocalisableString ResetInputSettings =>
            new TranslatableString(getKey(@"reset_input_settings"), @"Reset input settings");

        /// <summary>
        /// "Toggle toolbar"
        /// </summary>
        public static LocalisableString ToggleToolbar =>
            new TranslatableString(getKey(@"toggle_toolbar"), @"Toggle toolbar");

        /// <summary>
        /// "Toggle settings"
        /// </summary>
        public static LocalisableString ToggleSettings =>
            new TranslatableString(getKey(@"toggle_settings"), @"Toggle settings");

        /// <summary>
        /// "Toggle beatmap listing"
        /// </summary>
        public static LocalisableString ToggleBeatmapListing =>
            new TranslatableString(getKey(@"toggle_beatmap_listing"), @"Toggle beatmap listing");

        /// <summary>
        /// "Increase volume"
        /// </summary>
        public static LocalisableString IncreaseVolume =>
            new TranslatableString(getKey(@"increase_volume"), @"Increase volume");

        /// <summary>
        /// "Decrease volume"
        /// </summary>
        public static LocalisableString DecreaseVolume =>
            new TranslatableString(getKey(@"decrease_volume"), @"Decrease volume");

        /// <summary>
        /// "Toggle mute"
        /// </summary>
        public static LocalisableString ToggleMute =>
            new TranslatableString(getKey(@"toggle_mute"), @"Toggle mute");

        /// <summary>
        /// "Skip cutscene"
        /// </summary>
        public static LocalisableString SkipCutscene =>
            new TranslatableString(getKey(@"skip_cutscene"), @"Skip cutscene");

        /// <summary>
        /// "Quick retry (hold)"
        /// </summary>
        public static LocalisableString QuickRetry =>
            new TranslatableString(getKey(@"quick_retry"), @"Quick retry (hold)");

        /// <summary>
        /// "Take screenshot"
        /// </summary>
        public static LocalisableString TakeScreenshot =>
            new TranslatableString(getKey(@"take_screenshot"), @"Take screenshot");

        /// <summary>
        /// "Toggle gameplay mouse buttons"
        /// </summary>
        public static LocalisableString ToggleGameplayMouseButtons =>
            new TranslatableString(
                getKey(@"toggle_gameplay_mouse_buttons"),
                @"Toggle gameplay mouse buttons"
            );

        /// <summary>
        /// "Back"
        /// </summary>
        public static LocalisableString Back => new TranslatableString(getKey(@"back"), @"Back");

        /// <summary>
        /// "Increase scroll speed"
        /// </summary>
        public static LocalisableString IncreaseScrollSpeed =>
            new TranslatableString(getKey(@"increase_scroll_speed"), @"Increase scroll speed");

        /// <summary>
        /// "Decrease scroll speed"
        /// </summary>
        public static LocalisableString DecreaseScrollSpeed =>
            new TranslatableString(getKey(@"decrease_scroll_speed"), @"Decrease scroll speed");

        /// <summary>
        /// "Select"
        /// </summary>
        public static LocalisableString Select =>
            new TranslatableString(getKey(@"select"), @"Select");

        /// <summary>
        /// "Quick exit (hold)"
        /// </summary>
        public static LocalisableString QuickExit =>
            new TranslatableString(getKey(@"quick_exit"), @"Quick exit (hold)");

        /// <summary>
        /// "Next track"
        /// </summary>
        public static LocalisableString MusicNext =>
            new TranslatableString(getKey(@"music_next"), @"Next track");

        /// <summary>
        /// "Previous track"
        /// </summary>
        public static LocalisableString MusicPrev =>
            new TranslatableString(getKey(@"music_prev"), @"Previous track");

        /// <summary>
        /// "Play / pause"
        /// </summary>
        public static LocalisableString MusicPlay =>
            new TranslatableString(getKey(@"music_play"), @"Play / pause");

        /// <summary>
        /// "Toggle now playing overlay"
        /// </summary>
        public static LocalisableString ToggleNowPlaying =>
            new TranslatableString(getKey(@"toggle_now_playing"), @"Toggle now playing overlay");

        /// <summary>
        /// "Previous selection"
        /// </summary>
        public static LocalisableString SelectPrevious =>
            new TranslatableString(getKey(@"select_previous"), @"Previous selection");

        /// <summary>
        /// "Next selection"
        /// </summary>
        public static LocalisableString SelectNext =>
            new TranslatableString(getKey(@"select_next"), @"Next selection");

        /// <summary>
        /// "Activate previous set"
        /// </summary>
        public static LocalisableString ActivatePreviousSet =>
            new TranslatableString(getKey(@"activate_previous_set"), @"Activate previous set");

        /// <summary>
        /// "Activate next set"
        /// </summary>
        public static LocalisableString ActivateNextSet =>
            new TranslatableString(getKey(@"activate_next_set"), @"Activate next set");

        /// <summary>
        /// "Expand previous group"
        /// </summary>
        public static LocalisableString ExpandPreviousGroup =>
            new TranslatableString(getKey(@"expand_previous_group"), @"Expand previous group");

        /// <summary>
        /// "Expand next group"
        /// </summary>
        public static LocalisableString ExpandNextGroup =>
            new TranslatableString(getKey(@"expand_next_group"), @"Expand next group");

        /// <summary>
        /// "Toggle expansion of current group"
        /// </summary>
        public static LocalisableString ToggleCurrentGroup =>
            new TranslatableString(
                getKey(@"toggle_current_group"),
                @"Toggle expansion of current group"
            );

        /// <summary>
        /// "Home"
        /// </summary>
        public static LocalisableString Home => new TranslatableString(getKey(@"home"), @"Home");

        /// <summary>
        /// "Toggle notifications"
        /// </summary>
        public static LocalisableString ToggleNotifications =>
            new TranslatableString(getKey(@"toggle_notifications"), @"Toggle notifications");

        /// <summary>
        /// "Toggle profile"
        /// </summary>
        public static LocalisableString ToggleProfile =>
            new TranslatableString(getKey(@"toggle_profile"), @"Toggle profile");

        /// <summary>
        /// "Pause / resume gameplay"
        /// </summary>
        public static LocalisableString PauseGameplay =>
            new TranslatableString(getKey(@"pause_gameplay"), @"Pause / resume gameplay");

        /// <summary>
        /// "Setup mode"
        /// </summary>
        public static LocalisableString EditorSetupMode =>
            new TranslatableString(getKey(@"editor_setup_mode"), @"Setup mode");

        /// <summary>
        /// "Compose mode"
        /// </summary>
        public static LocalisableString EditorComposeMode =>
            new TranslatableString(getKey(@"editor_compose_mode"), @"Compose mode");

        /// <summary>
        /// "Design mode"
        /// </summary>
        public static LocalisableString EditorDesignMode =>
            new TranslatableString(getKey(@"editor_design_mode"), @"Design mode");

        /// <summary>
        /// "Timing mode"
        /// </summary>
        public static LocalisableString EditorTimingMode =>
            new TranslatableString(getKey(@"editor_timing_mode"), @"Timing mode");

        /// <summary>
        /// "Tap for BPM"
        /// </summary>
        public static LocalisableString EditorTapForBPM =>
            new TranslatableString(getKey(@"editor_tap_for_bpm"), @"Tap for BPM");

        /// <summary>
        /// "Clone selection"
        /// </summary>
        public static LocalisableString EditorCloneSelection =>
            new TranslatableString(getKey(@"editor_clone_selection"), @"Clone selection");

        /// <summary>
        /// "Cycle grid spacing"
        /// </summary>
        public static LocalisableString EditorCycleGridSpacing =>
            new TranslatableString(getKey(@"editor_cycle_grid_spacing"), @"Cycle grid spacing");

        /// <summary>
        /// "Cycle grid type"
        /// </summary>
        public static LocalisableString EditorCycleGridType =>
            new TranslatableString(getKey(@"editor_cycle_grid_type"), @"Cycle grid type");

        /// <summary>
        /// "Test gameplay"
        /// </summary>
        public static LocalisableString EditorTestGameplay =>
            new TranslatableString(getKey(@"editor_test_gameplay"), @"Test gameplay");

        /// <summary>
        /// "Hold for HUD"
        /// </summary>
        public static LocalisableString HoldForHUD =>
            new TranslatableString(getKey(@"hold_for_hud"), @"Hold for HUD");

        /// <summary>
        /// "Random skin"
        /// </summary>
        public static LocalisableString RandomSkin =>
            new TranslatableString(getKey(@"random_skin"), @"Random skin");

        /// <summary>
        /// "Pause / resume replay"
        /// </summary>
        public static LocalisableString TogglePauseReplay =>
            new TranslatableString(getKey(@"toggle_pause_replay"), @"Pause / resume replay");

        /// <summary>
        /// "Toggle in-game interface"
        /// </summary>
        public static LocalisableString ToggleInGameInterface =>
            new TranslatableString(
                getKey(@"toggle_in_game_interface"),
                @"Toggle in-game interface"
            );

        /// <summary>
        /// "Toggle in-game leaderboard"
        /// </summary>
        public static LocalisableString ToggleInGameLeaderboard =>
            new TranslatableString(
                getKey(@"toggle_in_game_leaderboard"),
                @"Toggle in-game leaderboard"
            );

        /// <summary>
        /// "Toggle mod select"
        /// </summary>
        public static LocalisableString ToggleModSelection =>
            new TranslatableString(getKey(@"toggle_mod_selection"), @"Toggle mod select");

        /// <summary>
        /// "Deselect all mods"
        /// </summary>
        public static LocalisableString DeselectAllMods =>
            new TranslatableString(getKey(@"deselect_all_mods"), @"Deselect all mods");

        /// <summary>
        /// "Random"
        /// </summary>
        public static LocalisableString SelectNextRandom =>
            new TranslatableString(getKey(@"select_next_random"), @"Random");

        /// <summary>
        /// "Rewind"
        /// </summary>
        public static LocalisableString SelectPreviousRandom =>
            new TranslatableString(getKey(@"select_previous_random"), @"Rewind");

        /// <summary>
        /// "Beatmap Options"
        /// </summary>
        public static LocalisableString ToggleBeatmapOptions =>
            new TranslatableString(getKey(@"toggle_beatmap_options"), @"Beatmap Options");

        /// <summary>
        /// "Verify mode"
        /// </summary>
        public static LocalisableString EditorVerifyMode =>
            new TranslatableString(getKey(@"editor_verify_mode"), @"Verify mode");

        /// <summary>
        /// "Nudge selection left"
        /// </summary>
        public static LocalisableString EditorNudgeLeft =>
            new TranslatableString(getKey(@"editor_nudge_left"), @"Nudge selection left");

        /// <summary>
        /// "Nudge selection right"
        /// </summary>
        public static LocalisableString EditorNudgeRight =>
            new TranslatableString(getKey(@"editor_nudge_right"), @"Nudge selection right");

        /// <summary>
        /// "Flip selection horizontally"
        /// </summary>
        public static LocalisableString EditorFlipHorizontally =>
            new TranslatableString(
                getKey(@"editor_flip_horizontally"),
                @"Flip selection horizontally"
            );

        /// <summary>
        /// "Flip selection vertically"
        /// </summary>
        public static LocalisableString EditorFlipVertically =>
            new TranslatableString(getKey(@"editor_flip_vertically"), @"Flip selection vertically");

        /// <summary>
        /// "Increase distance spacing"
        /// </summary>
        public static LocalisableString EditorIncreaseDistanceSpacing =>
            new TranslatableString(
                getKey(@"editor_increase_distance_spacing"),
                @"Increase distance spacing"
            );

        /// <summary>
        /// "Decrease distance spacing"
        /// </summary>
        public static LocalisableString EditorDecreaseDistanceSpacing =>
            new TranslatableString(
                getKey(@"editor_decrease_distance_spacing"),
                @"Decrease distance spacing"
            );

        /// <summary>
        /// "Cycle previous beat snap divisor"
        /// </summary>
        public static LocalisableString EditorCyclePreviousBeatSnapDivisor =>
            new TranslatableString(
                getKey(@"editor_cycle_previous_beat_snap_divisor"),
                @"Cycle previous beat snap divisor"
            );

        /// <summary>
        /// "Cycle next beat snap divisor"
        /// </summary>
        public static LocalisableString EditorCycleNextBeatSnapDivisor =>
            new TranslatableString(
                getKey(@"editor_cycle_next_snap_divisor"),
                @"Cycle next beat snap divisor"
            );

        /// <summary>
        /// "Toggle skin editor"
        /// </summary>
        public static LocalisableString ToggleSkinEditor =>
            new TranslatableString(getKey(@"toggle_skin_editor"), @"Toggle skin editor");

        /// <summary>
        /// "Toggle FPS counter"
        /// </summary>
        public static LocalisableString ToggleFPSCounter =>
            new TranslatableString(getKey(@"toggle_fps_counter"), @"Toggle FPS counter");

        /// <summary>
        /// "Previous volume meter"
        /// </summary>
        public static LocalisableString PreviousVolumeMeter =>
            new TranslatableString(getKey(@"previous_volume_meter"), @"Previous volume meter");

        /// <summary>
        /// "Next volume meter"
        /// </summary>
        public static LocalisableString NextVolumeMeter =>
            new TranslatableString(getKey(@"next_volume_meter"), @"Next volume meter");

        /// <summary>
        /// "Seek replay forward"
        /// </summary>
        public static LocalisableString SeekReplayForward =>
            new TranslatableString(getKey(@"seek_replay_forward"), @"Seek replay forward");

        /// <summary>
        /// "Seek replay backward"
        /// </summary>
        public static LocalisableString SeekReplayBackward =>
            new TranslatableString(getKey(@"seek_replay_backward"), @"Seek replay backward");

        /// <summary>
        /// "Seek replay forward one frame"
        /// </summary>
        public static LocalisableString StepReplayForward =>
            new TranslatableString(
                getKey(@"step_replay_forward"),
                @"Seek replay forward one frame"
            );

        /// <summary>
        /// "Step replay backward one frame"
        /// </summary>
        public static LocalisableString StepReplayBackward =>
            new TranslatableString(
                getKey(@"step_replay_backward"),
                @"Step replay backward one frame"
            );

        /// <summary>
        /// "Toggle chat focus"
        /// </summary>
        public static LocalisableString ToggleChatFocus =>
            new TranslatableString(getKey(@"toggle_chat_focus"), @"Toggle chat focus");

        /// <summary>
        /// "Toggle replay settings"
        /// </summary>
        public static LocalisableString ToggleReplaySettings =>
            new TranslatableString(getKey(@"toggle_replay_settings"), @"Toggle replay settings");

        /// <summary>
        /// "Save replay"
        /// </summary>
        public static LocalisableString SaveReplay =>
            new TranslatableString(getKey(@"save_replay"), @"Save replay");

        /// <summary>
        /// "Export replay"
        /// </summary>
        public static LocalisableString ExportReplay =>
            new TranslatableString(getKey(@"export_replay"), @"Export replay");

        /// <summary>
        /// "Increase offset"
        /// </summary>
        public static LocalisableString IncreaseOffset =>
            new TranslatableString(getKey(@"increase_offset"), @"Increase offset");

        /// <summary>
        /// "Decrease offset"
        /// </summary>
        public static LocalisableString DecreaseOffset =>
            new TranslatableString(getKey(@"decrease_offset"), @"Decrease offset");

        /// <summary>
        /// "Toggle rotate control"
        /// </summary>
        public static LocalisableString EditorToggleRotateControl =>
            new TranslatableString(
                getKey(@"editor_toggle_rotate_control"),
                @"Toggle rotate control"
            );

        /// <summary>
        /// "Toggle scale control"
        /// </summary>
        public static LocalisableString EditorToggleScaleControl =>
            new TranslatableString(getKey(@"editor_toggle_scale_control"), @"Toggle scale control");

        /// <summary>
        /// "Toggle autoplay"
        /// </summary>
        public static LocalisableString EditorTestPlayToggleAutoplay =>
            new TranslatableString(getKey(@"editor_test_play_toggle_autoplay"), @"Toggle autoplay");

        /// <summary>
        /// "Toggle quick pause"
        /// </summary>
        public static LocalisableString EditorTestPlayToggleQuickPause =>
            new TranslatableString(
                getKey(@"editor_test_play_toggle_quick_pause"),
                @"Toggle quick pause"
            );

        /// <summary>
        /// "Quick exit to initial time"
        /// </summary>
        public static LocalisableString EditorTestPlayQuickExitToInitialTime =>
            new TranslatableString(
                getKey(@"editor_test_play_quick_exit_to_initial_time"),
                @"Quick exit to initial time"
            );

        /// <summary>
        /// "Quick exit to current time"
        /// </summary>
        public static LocalisableString EditorTestPlayQuickExitToCurrentTime =>
            new TranslatableString(
                getKey(@"editor_test_play_quick_exit_to_current_time"),
                @"Quick exit to current time"
            );

        /// <summary>
        /// "Increase mod speed"
        /// </summary>
        public static LocalisableString IncreaseModSpeed =>
            new TranslatableString(getKey(@"increase_mod_speed"), @"Increase mod speed");

        /// <summary>
        /// "Decrease mod speed"
        /// </summary>
        public static LocalisableString DecreaseModSpeed =>
            new TranslatableString(getKey(@"decrease_mod_speed"), @"Decrease mod speed");

        /// <summary>
        /// "Seek to previous hit object"
        /// </summary>
        public static LocalisableString EditorSeekToPreviousHitObject =>
            new TranslatableString(
                getKey(@"editor_seek_to_previous_hit_object"),
                @"Seek to previous hit object"
            );

        /// <summary>
        /// "Seek to next hit object"
        /// </summary>
        public static LocalisableString EditorSeekToNextHitObject =>
            new TranslatableString(
                getKey(@"editor_seek_to_next_hit_object"),
                @"Seek to next hit object"
            );

        /// <summary>
        /// "Seek to previous sample point"
        /// </summary>
        public static LocalisableString EditorSeekToPreviousSamplePoint =>
            new TranslatableString(
                getKey(@"editor_seek_to_previous_sample_point"),
                @"Seek to previous sample point"
            );

        /// <summary>
        /// "Seek to next sample point"
        /// </summary>
        public static LocalisableString EditorSeekToNextSamplePoint =>
            new TranslatableString(
                getKey(@"editor_seek_to_next_sample_point"),
                @"Seek to next sample point"
            );

        /// <summary>
        /// "Add bookmark"
        /// </summary>
        public static LocalisableString EditorAddBookmark =>
            new TranslatableString(getKey(@"editor_add_bookmark"), @"Add bookmark");

        /// <summary>
        /// "Remove closest bookmark"
        /// </summary>
        public static LocalisableString EditorRemoveClosestBookmark =>
            new TranslatableString(
                getKey(@"editor_remove_closest_bookmark"),
                @"Remove closest bookmark"
            );

        /// <summary>
        /// "Seek to previous bookmark"
        /// </summary>
        public static LocalisableString EditorSeekToPreviousBookmark =>
            new TranslatableString(
                getKey(@"editor_seek_to_previous_bookmark"),
                @"Seek to previous bookmark"
            );

        /// <summary>
        /// "Seek to next bookmark"
        /// </summary>
        public static LocalisableString EditorSeekToNextBookmark =>
            new TranslatableString(
                getKey(@"editor_seek_to_next_bookmark"),
                @"Seek to next bookmark"
            );

        /// <summary>
        /// "Absolute scroll song list"
        /// </summary>
        public static LocalisableString AbsoluteScrollSongList =>
            new TranslatableString(
                getKey(@"absolute_scroll_song_list"),
                @"Absolute scroll song list"
            );

        /// <summary>
        /// "Toggle movement control"
        /// </summary>
        public static LocalisableString EditorToggleMoveControl =>
            new TranslatableString(
                getKey(@"editor_toggle_move_control"),
                @"Toggle movement control"
            );

        /// <summary>
        /// "Discard unsaved changes"
        /// </summary>
        public static LocalisableString EditorDiscardUnsavedChanges =>
            new TranslatableString(
                getKey(@"editor_discard_unsaved_changes"),
                @"Discard unsaved changes"
            );

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
