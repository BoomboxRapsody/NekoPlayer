// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using YouTubePlayerEX.App.Extensions;

namespace YouTubePlayerEX.App.Localisation
{
    public static class YTPlayerEXStrings
    {
        private const string prefix = @"YouTubePlayerEX.App.Resources.Localisation.YTPlayerEX";

        /// <summary>
        /// "{0} • {1} views • {2}"
        /// </summary>
        public static LocalisableString VideoMetadataDesc(string username, string views, string daysAgo) => new TranslatableString(getKey(@"video_metadata_desc"), "{0} • {1} views • {2}", username, views, daysAgo);

        /// <summary>
        /// "Playback speed"
        /// </summary>
        public static LocalisableString PlaybackSpeedWithoutValue => new TranslatableString(getKey(@"playback_speed_without_value"), "Playback speed");

        /// <summary>
        /// "Quick Action"
        /// </summary>
        public static LocalisableString QuickAction => new TranslatableString(getKey(@"quick_action"), "Quick Action");

        /// <summary>
        /// "Export logs"
        /// </summary>
        public static LocalisableString ExportLogs => new TranslatableString(getKey(@"export_logs"), "Export logs");

        /// <summary>
        /// "Load from video ID"
        /// </summary>
        public static LocalisableString LoadFromVideoId => new TranslatableString(getKey(@"load_from_video_id"), "Load from video ID");

        /// <summary>
        /// "Load Video"
        /// </summary>
        public static LocalisableString LoadVideo => new TranslatableString(getKey(@"load_video"), "Load Video");

        /// <summary>
        /// "Video ID must not be empty!"
        /// </summary>
        public static LocalisableString NoVideoIdError => new TranslatableString(getKey(@"error_noVideoId"), "Video ID must not be empty!");

        /// <summary>
        /// "{0} • {1} subscribers • Click to view channel via external web browser."
        /// </summary>
        public static LocalisableString ProfileImageTooltip(string username, string subs) => new TranslatableString(getKey(@"profile_image_tooltip"), "{0} • {1} subscribers • Click to view channel via external web browser.", username, subs);

        /// <summary>
        /// "Settings"
        /// </summary>
        public static LocalisableString Settings => new TranslatableString(getKey(@"settings"), "Settings");

        /// <summary>
        /// "Screen resolution"
        /// </summary>
        public static LocalisableString ScreenResolution => new TranslatableString(getKey(@"screen_resolution"), "Screen resolution");

        /// <summary>
        /// "Disabled"
        /// </summary>
        public static LocalisableString CaptionDisabled => new TranslatableString(getKey(@"caption_disabled"), "Disabled");

        /// <summary>
        /// "General"
        /// </summary>
        public static LocalisableString General => new TranslatableString(getKey(@"general"), "General");

        /// <summary>
        /// "Graphics"
        /// </summary>
        public static LocalisableString Graphics => new TranslatableString(getKey(@"graphics"), "Graphics");

        /// <summary>
        /// "Language"
        /// </summary>
        public static LocalisableString Language => new TranslatableString(getKey(@"language"), "Language");

        /// <summary>
        /// "Display"
        /// </summary>
        public static LocalisableString Display => new TranslatableString(getKey(@"display"), "Display");

        /// <summary>
        /// "Screen mode"
        /// </summary>
        public static LocalisableString ScreenMode => new TranslatableString(getKey(@"screen_mode"), "Screen mode");

        /// <summary>
        /// "Closed caption language (only available)"
        /// </summary>
        public static LocalisableString CaptionLanguage => new TranslatableString(getKey(@"caption_language"), "Closed caption language (only available)");

        /// <summary>
        /// "Closed caption font"
        /// </summary>
        public static LocalisableString CaptionFont => new TranslatableString(getKey(@"caption_font"), "Closed caption font");

        /// <summary>
        /// "Press F11 to exit the full screen."
        /// </summary>
        public static LocalisableString FullscreenEntered => new TranslatableString(getKey(@"fullscreen_entered"), "Press F11 to exit the full screen.");

        /// <summary>
        /// "Audio"
        /// </summary>
        public static LocalisableString Audio => new TranslatableString(getKey(@"audio"), "Audio");

        /// <summary>
        /// "Video volume"
        /// </summary>
        public static LocalisableString VideoVolume => new TranslatableString(getKey(@"video_volume"), "Video volume");

        /// <summary>
        /// "SFX volume"
        /// </summary>
        public static LocalisableString SFXVolume => new TranslatableString(getKey(@"sfx_volume"), "SFX volume");

        /// <summary>
        /// "Selected caption: {0}"
        /// </summary>
        public static LocalisableString SelectedCaption(LocalisableString language) => new TranslatableString(getKey(@"selected_caption"), "Selected caption: {0}", language);

        /// <summary>
        /// "Selected caption: {0} (auto-generated)"
        /// </summary>
        public static LocalisableString SelectedCaptionAutoGen(LocalisableString language) => new TranslatableString(getKey(@"selected_caption_auto_gen"), "Selected caption: {0} (auto-generated)", language);

        /// <summary>
        /// "{0} (auto-generated)"
        /// </summary>
        public static LocalisableString CaptionAutoGen(LocalisableString language) => new TranslatableString(getKey(@"caption_auto_gen"), "{0} (auto-generated)", language);

        /// <summary>
        /// "Fill"
        /// </summary>
        public static LocalisableString Fill => new TranslatableString(getKey(@"fill"), "Fill");

        /// <summary>
        /// "Letterbox"
        /// </summary>
        public static LocalisableString Letterbox => new TranslatableString(getKey(@"letterbox"), "Letterbox");

        /// <summary>
        /// "Aspect ratio method"
        /// </summary>
        public static LocalisableString AspectRatioMethod => new TranslatableString(getKey(@"aspect_ratio_method"), "Aspect ratio method");

        /// <summary>
        /// "Estimated: {0} | Actual: {1}"
        /// </summary>
        public static LocalisableString DislikeCountTooltip(string estimated, string actual) => new TranslatableString(getKey(@"dislike_count_tooltip"), "Estimated: {0} | Actual: {1}", estimated, actual);

        /// <summary>
        /// "Video metadata translate source"
        /// </summary>
        public static LocalisableString VideoMetadataTranslateSource => new TranslatableString(getKey(@"video_metadata_translate_source"), "Video metadata translate source");

        /// <summary>
        /// "Google Translate"
        /// </summary>
        public static LocalisableString GoogleTranslate => new TranslatableString(getKey(@"google_translate"), "Google Translate");

        /// <summary>
        /// "Auto"
        /// </summary>
        public static LocalisableString Auto => new TranslatableString(getKey(@"auto"), "Auto");

        /// <summary>
        /// "Video"
        /// </summary>
        public static LocalisableString Video => new TranslatableString(getKey(@"video"), "Video");

        /// <summary>
        /// "Use hardware acceleration"
        /// </summary>
        public static LocalisableString UseHardwareAcceleration => new TranslatableString(getKey(@"use_hardware_acceleration"), @"Use hardware acceleration");

        /// <summary>
        /// "Minimise video player when switching to another app"
        /// </summary>
        public static LocalisableString MinimiseOnFocusLoss => new TranslatableString(getKey(@"minimise_on_focus_loss"), @"Minimise video player when switching to another app");

        /// <summary>
        /// "Prefer high quality"
        /// </summary>
        public static LocalisableString PreferHighQuality => new TranslatableString(getKey(@"prefer_high_quality"), "Prefer high quality");

        /// <summary>
        /// "Video quality"
        /// </summary>
        public static LocalisableString VideoQuality => new TranslatableString(getKey(@"video_quality"), "Video quality");

        /// <summary>
        /// "Master volume"
        /// </summary>
        public static LocalisableString MasterVolume => new TranslatableString(getKey(@"master_volume"), "Master volume");

        /// <summary>
        /// "Enabled"
        /// </summary>
        public static LocalisableString Enabled => new TranslatableString(getKey(@"enabled"), "Enabled");

        /// <summary>
        /// "Disabled"
        /// </summary>
        public static LocalisableString Disabled => new TranslatableString(getKey(@"disabled"), "Disabled");

        /// <summary>
        /// "UI scaling"
        /// </summary>
        public static LocalisableString UIScaling => new TranslatableString(getKey(@"ui_scaling"), @"UI scaling");

        /// <summary>
        /// "Audio tracks"
        /// </summary>
        public static LocalisableString AudioLanguage => new TranslatableString(getKey(@"audio_language"), @"Audio tracks");

        /// <summary>
        /// "Adjust pitch on speed change"
        /// </summary>
        public static LocalisableString AdjustPitchOnSpeedChange => new TranslatableString(getKey(@"adjust_pitch_on_speed_change"), @"Adjust pitch on speed change");

        /// <summary>
        /// "{0} views  {1}"
        /// </summary>
        public static LocalisableString VideoMetadataDescWithoutChannelName(string views, string daysAgo) => new TranslatableString(getKey(@"video_metadata_desc_without_channel_name"), "{0} views  {1}", views, daysAgo);

        /// <summary>
        /// "Comments ({0})"
        /// </summary>
        public static LocalisableString Comments(LocalisableString count) => new TranslatableString(getKey(@"comments"), "Comments ({0})", count);

        /// <summary>
        /// "Video dim level"
        /// </summary>
        public static LocalisableString VideoDimLevel => new TranslatableString(getKey(@"dim"), "Video dim level");

        /// <summary>
        /// "Translate to {0}"
        /// </summary>
        public static LocalisableString TranslateTo(LocalisableString targetLang) => new TranslatableString(getKey(@"translate_to"), "Translate to {0}", targetLang);

        /// <summary>
        /// "See original (Translated by Google)"
        /// </summary>
        public static LocalisableString TranslateViewOriginal => new TranslatableString(getKey(@"translate_view_original"), "See original (Translated by Google)");

        /// <summary>
        /// "{0} (Reply to {1})"
        /// </summary>
        public static LocalisableString CommentReply(string from, string to) => new TranslatableString(getKey(@"comment_reply"), "{0} (Reply to {1})", from, to);

        /// <summary>
        /// "You are running the latest release ({0})"
        /// </summary>
        public static LocalisableString RunningLatestRelease(string version) => new TranslatableString(getKey(@"running_latest_release"), @"You are running the latest release ({0})", version);

        /// <summary>
        /// "Downloading update... {0}%"
        /// </summary>
        public static LocalisableString DownloadingUpdate(string percentage) => new TranslatableString(getKey(@"updating"), @"Downloading update... {0}%", percentage);

        /// <summary>
        /// "To apply updates, please restart the app."
        /// </summary>
        public static LocalisableString RestartRequired => new TranslatableString(getKey(@"restart_required"), "To apply updates, please restart the app.");

        /// <summary>
        /// "Update failed!"
        /// </summary>
        public static LocalisableString UpdateFailed => new TranslatableString(getKey(@"update_failed"), "Update failed!");

        /// <summary>
        /// "Checking for update..."
        /// </summary>
        public static LocalisableString CheckingUpdate => new TranslatableString(getKey(@"checking_update"), "Checking for update...");

        /// <summary>
        /// "Check for updates"
        /// </summary>
        public static LocalisableString CheckUpdate => new TranslatableString(getKey(@"check_update"), "Check for updates");

        /// <summary>
        /// "Frame limiter"
        /// </summary>
        public static LocalisableString FrameLimiter => new TranslatableString(getKey(@"frame_limiter"), "Frame limiter");

        /// <summary>
        /// "Like count hidden by uploader"
        /// </summary>
        public static LocalisableString LikeCountHidden => new TranslatableString(getKey(@"like_count_hidden"), "Like count hidden by uploader");

        /// <summary>
        /// "Disabled by uploader"
        /// </summary>
        public static LocalisableString DisabledByUploader => new TranslatableString(getKey(@"disabled_by_uploader"), "Disabled by uploader");

        /// <summary>
        /// "Dislike count data is provided by the "
        /// </summary>
        public static LocalisableString DislikeCounterCredits_1 => new TranslatableString(getKey(@"dislike_counter_credits_1"), @"Dislike count data is provided by the ");

        /// <summary>
        /// "."
        /// </summary>
        public static LocalisableString DislikeCounterCredits_2 => new TranslatableString(getKey(@"dislike_counter_credits_2"), @".");

        /// <summary>
        /// "Default"
        /// </summary>
        public static LocalisableString Default => new TranslatableString(getKey(@"common_default"), "Default");

        /// <summary>
        /// "Show FPS"
        /// </summary>
        public static LocalisableString ShowFPS => new TranslatableString(getKey(@"show_fps"), "Show FPS");

        /// <summary>
        /// "Cannot play private videos."
        /// </summary>
        public static LocalisableString CannotPlayPrivateVideos => new TranslatableString(getKey(@"cannot_play_private_videos"), "Cannot play private videos.");

        /// <summary>
        /// "Play"
        /// </summary>
        public static LocalisableString Play => new TranslatableString(getKey(@"play"), "Play");

        /// <summary>
        /// "Pause"
        /// </summary>
        public static LocalisableString Pause => new TranslatableString(getKey(@"pause"), "Pause");

        /// <summary>
        /// "Playback speed: {0}"
        /// </summary>
        public static LocalisableString PlaybackSpeed(double value) => new TranslatableString(getKey(@"playback_speed"), "Playback speed: {0}", value.ToStandardFormattedString(5, true));

        /// <summary>
        /// "Comments"
        /// </summary>
        public static LocalisableString CommentsWithoutCount => new TranslatableString(getKey(@"comments_without_count"), "Comments");

        /// <summary>
        /// "Setting the video quality to 8K may cause performance degradation, GPU overload, and driver crashes on some devices."
        /// </summary>
        public static LocalisableString VideoQuality8KWarning => new TranslatableString(getKey(@"video_quality_8k_warning"), "Setting the video quality to 8K may cause performance degradation, GPU overload, and driver crashes on some devices.");

        /// <summary>
        /// "Renderer"
        /// </summary>
        public static LocalisableString Renderer => new TranslatableString(getKey(@"renderer"), @"Renderer");

        /// <summary>
        /// "Exported logs!"
        /// </summary>
        public static LocalisableString LogsExportFinished => new TranslatableString(getKey(@"logs_export_finished"), @"Exported logs!");

        /// <summary>
        /// "Revert to default"
        /// </summary>
        public static LocalisableString RevertToDefault => new TranslatableString(getKey(@"revert_to_default"), @"Revert to default");

        /// <summary>
        /// "Everything"
        /// </summary>
        public static LocalisableString ScaleEverything => new TranslatableString(getKey(@"scale_everything"), @"Everything");

        /// <summary>
        /// "Video"
        /// </summary>
        public static LocalisableString ScaleVideo => new TranslatableString(getKey(@"scale_video"), @"Video");

        /// <summary>
        /// "Off"
        /// </summary>
        public static LocalisableString ScalingOff => new TranslatableString(getKey(@"scaling_off"), @"Off");

        /// <summary>
        /// "Screen scaling"
        /// </summary>
        public static LocalisableString ScreenScaling => new TranslatableString(getKey(@"screen_scaling"), @"Screen scaling");

        /// <summary>
        /// "Horizontal position"
        /// </summary>
        public static LocalisableString HorizontalPosition => new TranslatableString(getKey(@"horizontal_position"), @"Horizontal position");

        /// <summary>
        /// "Vertical position"
        /// </summary>
        public static LocalisableString VerticalPosition => new TranslatableString(getKey(@"vertical_position"), @"Vertical position");

        /// <summary>
        /// "Horizontal scale"
        /// </summary>
        public static LocalisableString HorizontalScale => new TranslatableString(getKey(@"horizontal_scale"), @"Horizontal scale");

        /// <summary>
        /// "Vertical scale"
        /// </summary>
        public static LocalisableString VerticalScale => new TranslatableString(getKey(@"vertical_scale"), @"Vertical scale");

        /// <summary>
        /// "Thumbnail dim"
        /// </summary>
        public static LocalisableString ThumbnailDim => new TranslatableString(getKey(@"thumbnail_dim"), @"Thumbnail dim");

        /// <summary>
        /// "Username display mode"
        /// </summary>
        public static LocalisableString UsernameDisplayMode => new TranslatableString(getKey(@"username_display_mode"), @"Username display mode");

        /// <summary>
        /// "Shrink app to avoid cameras and notches"
        /// </summary>
        public static LocalisableString ShrinkGameToSafeArea => new TranslatableString(getKey(@"shrink_game_to_safe_area"), @"Shrink app to avoid cameras and notches");

        /// <summary>
        /// "JPG (web-friendly)"
        /// </summary>
        public static LocalisableString Jpg => new TranslatableString(getKey(@"jpg_web_friendly"), @"JPG (web-friendly)");

        /// <summary>
        /// "PNG (lossless)"
        /// </summary>
        public static LocalisableString Png => new TranslatableString(getKey(@"png_lossless"), @"PNG (lossless)");

        /// <summary>
        /// "Screenshot saved!"
        /// </summary>
        public static LocalisableString ScreenshotSaved => new TranslatableString(getKey(@"screenshot_saved"), @"Screenshot saved!");

        /// <summary>
        /// "Screenshot"
        /// </summary>
        public static LocalisableString Screenshot => new TranslatableString(getKey(@"screenshot"), @"Screenshot");

        /// <summary>
        /// "Screenshot format"
        /// </summary>
        public static LocalisableString ScreenshotFormat => new TranslatableString(getKey(@"screenshot_format"), @"Screenshot format");

        /// <summary>
        /// "Show user interface in screenshots"
        /// </summary>
        public static LocalisableString ShowCursorInScreenshots => new TranslatableString(getKey(@"show_cursor_in_screenshots"), @"Show user interface in screenshots");

        /// <summary>
        /// "Prefer display username (2005~2022)"
        /// </summary>
        public static LocalisableString UsernameDisplayMode_DisplayName => new TranslatableString(getKey(@"username_display_mode_nickname"), @"Prefer display username (2005~2022)");

        /// <summary>
        /// "Prefer display handle (2022~now)"
        /// </summary>
        public static LocalisableString UsernameDisplayMode_Handle => new TranslatableString(getKey(@"username_display_mode_handle"), @"Prefer display handle (2022~now)");

        /// <summary>
        /// "Comment added."
        /// </summary>
        public static LocalisableString CommentAdded => new TranslatableString(getKey(@"comment_added"), "Comment added.");

        /// <summary>
        /// "Write comment as {0}"
        /// </summary>
        public static LocalisableString CommentWith(string username) => new TranslatableString(getKey(@"comment_with"), "Write comment as {0}", username);

        /// <summary>
        /// "Signed in to {0}"
        /// </summary>
        public static LocalisableString SignedIn(string username) => new TranslatableString(getKey(@"signed_in"), "Signed in to {0}", username);

        /// <summary>
        /// "Not logged in"
        /// </summary>
        public static LocalisableString SignedOut => new TranslatableString(getKey(@"signed_out"), "Not logged in");

        /// <summary>
        /// "Google account"
        /// </summary>
        public static LocalisableString GoogleAccount => new TranslatableString(getKey(@"google_account"), "Google account");

        /// <summary>
        /// "Search"
        /// </summary>
        public static LocalisableString Search => new TranslatableString(getKey(@"search"), "Search");

        /// <summary>
        /// "search..."
        /// </summary>
        public static LocalisableString SearchPlaceholder => new TranslatableString(getKey(@"search_placeholder"), "search...");

        /// <summary>
        /// "View latest versions"
        /// </summary>
        public static LocalisableString ViewLatestVersions => new TranslatableString(getKey(@"view_latest_versions"), "View latest versions");

        /// <summary>
        /// "Report"
        /// </summary>
        public static LocalisableString Report => new TranslatableString(getKey(@"report"), "Report");

        /// <summary>
        /// "What's going on?"
        /// </summary>
        public static LocalisableString WhatsGoingOn => new TranslatableString(getKey(@"whats_going_on"), "What's going on?");

        /// <summary>
        /// "We'll check for all Community Guidelines, so don't worry about making the perfect choice."
        /// </summary>
        public static LocalisableString ReportDesc => new TranslatableString(getKey(@"report_desc"), "We'll check for all Community Guidelines, so don't worry about making the perfect choice.");

        /// <summary>
        /// "Reason"
        /// </summary>
        public static LocalisableString ReportReason => new TranslatableString(getKey(@"report_reason"), "Reason");

        /// <summary>
        /// "Detailed reason"
        /// </summary>
        public static LocalisableString ReportSubReason => new TranslatableString(getKey(@"report_sub_reason"), "Detailed reason");

        /// <summary>
        /// "Submit"
        /// </summary>
        public static LocalisableString Submit => new TranslatableString(getKey(@"submit"), "Submit");

        /// <summary>
        /// "Thank you for your submission."
        /// </summary>
        public static LocalisableString ReportSuccess => new TranslatableString(getKey(@"report_success"), "Thank you for your submission.");

        /// <summary>
        /// "Description"
        /// </summary>
        public static LocalisableString Description => new TranslatableString(getKey(@"description"), "Description");

        /// <summary>
        /// "Windowed"
        /// </summary>
        public static LocalisableString Windowed => new TranslatableString(getKey(@"windowed"), "Windowed");

        /// <summary>
        /// "Borderless"
        /// </summary>
        public static LocalisableString Borderless => new TranslatableString(getKey(@"borderless"), "Borderless");

        /// <summary>
        /// "Fullscreen"
        /// </summary>
        public static LocalisableString Fullscreen => new TranslatableString(getKey(@"fullscreen"), "Fullscreen");

        /// <summary>
        /// "VSync"
        /// </summary>
        public static LocalisableString VSync => new TranslatableString(getKey(@"vertical_sync"), "VSync");

        /// <summary>
        /// "2x refresh rate"
        /// </summary>
        public static LocalisableString RefreshRate2X => new TranslatableString(getKey(@"refresh_rate_2x"), "2x refresh rate");

        /// <summary>
        /// "4x refresh rate"
        /// </summary>
        public static LocalisableString RefreshRate4X => new TranslatableString(getKey(@"refresh_rate_4x"), "4x refresh rate");

        /// <summary>
        /// "8x refresh rate"
        /// </summary>
        public static LocalisableString RefreshRate8X => new TranslatableString(getKey(@"refresh_rate_8x"), "8x refresh rate");

        /// <summary>
        /// "Basically unlimited"
        /// </summary>
        public static LocalisableString Unlimited => new TranslatableString(getKey(@"unlimited"), "Basically unlimited");

        /// <summary>
        /// "Always use original audio"
        /// </summary>
        public static LocalisableString AlwaysUseOriginalAudio => new TranslatableString(getKey(@"always_use_original_audio"), "Always use original audio");

        /// <summary>
        /// "No description has been added to this video."
        /// </summary>
        public static LocalisableString NoDescription => new TranslatableString(getKey(@"no_description"), "No description has been added to this video.");

        /// <summary>
        /// "View changelog for {0}"
        /// </summary>
        public static LocalisableString ViewChangelog(string version) => new TranslatableString(getKey(@"view_version_desc"), "View changelog for {0}", version);

        /// <summary>
        /// "Automatic"
        /// </summary>
        public static LocalisableString RenderTypeAutomatic => new TranslatableString(getKey(@"render_type_automatic"), "Automatic");

        /// <summary>
        /// "Automatic ({0})"
        /// </summary>
        public static LocalisableString RenderTypeAutomaticIsUse(string rendererName) => new TranslatableString(getKey(@"render_type_automatic_is_use"), "Automatic ({0})", rendererName);

        /// <summary>
        /// "Always use system cursor"
        /// </summary>
        public static LocalisableString UseSystemCursor => new TranslatableString(getKey(@"use_system_cursor"), "Always use system cursor");

        /// <summary>
        /// "Use experimental audio mode"
        /// </summary>
        public static LocalisableString WasapiLabel => new TranslatableString(getKey(@"wasapi_label"), @"Use experimental audio mode");

        /// <summary>
        /// "This will attempt to initialise the audio engine in a lower latency mode."
        /// </summary>
        public static LocalisableString WasapiTooltip => new TranslatableString(getKey(@"wasapi_tooltip"), @"This will attempt to initialise the audio engine in a lower latency mode.");

        /// <summary>
        /// "Output device"
        /// </summary>
        public static LocalisableString OutputDevice => new TranslatableString(getKey(@"output_device"), @"Output device");

        /// <summary>
        /// "Volume"
        /// </summary>
        public static LocalisableString Volume => new TranslatableString(getKey(@"volume"), @"Volume");

        /// <summary>
        /// "Debug"
        /// </summary>
        public static LocalisableString Debug => new TranslatableString(getKey(@"debug"), @"Debug");

        /// <summary>
        /// "Show log overlay"
        /// </summary>
        public static LocalisableString ShowLogOverlay => new TranslatableString(getKey(@"show_log_overlay"), @"Show log overlay");

        /// <summary>
        /// "Bypass front-to-back render pass"
        /// </summary>
        public static LocalisableString BypassFTBRenderPass => new TranslatableString(getKey(@"bypass_ftb_render_pass"), @"Bypass front-to-back render pass");

        /// <summary>
        /// "GC mode"
        /// </summary>
        public static LocalisableString GC_Mode => new TranslatableString(getKey(@"gc_mode"), @"GC mode");

        /// <summary>
        /// "Clear all caches"
        /// </summary>
        public static LocalisableString ClearAllCaches => new TranslatableString(getKey(@"clear_all_caches"), @"Clear all caches");

        /// <summary>
        /// "Load from playlist ID"
        /// </summary>
        public static LocalisableString LoadFromPlaylistId => new TranslatableString(getKey(@"load_from_playlist_id"), "Load from playlist ID");

        /// <summary>
        /// "Load Playlist"
        /// </summary>
        public static LocalisableString LoadPlaylist => new TranslatableString(getKey(@"load_playlist"), "Load Playlist");

        /// <summary>
        /// "Playlists"
        /// </summary>
        public static LocalisableString Playlists => new TranslatableString(getKey(@"playlists"), "Playlists");

        /// <summary>
        /// "Previous video"
        /// </summary>
        public static LocalisableString PreviousVideo => new TranslatableString(getKey(@"previous_video"), "Previous video");

        /// <summary>
        /// "Next video"
        /// </summary>
        public static LocalisableString NextVideo => new TranslatableString(getKey(@"next_video"), "Next video");

        /// <summary>
        /// "Hardware acceleration via GPU is enabled. If you experience any issues, please keep hardware acceleration disabled for now."
        /// </summary>
        public static LocalisableString HardwareAccelerationEnabledNote => new TranslatableString(getKey(@"hw_acceleration_enabled_note"), "Hardware acceleration via GPU is enabled. If you experience any issues, please keep hardware acceleration disabled for now.");

        /// <summary>
        /// "Audio effects (Beta)"
        /// </summary>
        public static LocalisableString AudioEffects => new TranslatableString(getKey(@"audio_effects"), "Audio effects (Beta)");

        /// <summary>
        /// "Reverb"
        /// </summary>
        public static LocalisableString ReverbEffect => new TranslatableString(getKey(@"reverb"), "Reverb");

        /// <summary>
        /// "Wet mix"
        /// </summary>
        public static LocalisableString WetMix => new TranslatableString(getKey(@"reverb_wet_mix"), "Wet mix");

        /// <summary>
        /// "Stereo width"
        /// </summary>
        public static LocalisableString StereoWidth => new TranslatableString(getKey(@"reverb_stereo_width"), "Stereo width");

        /// <summary>
        /// "High frequency damping"
        /// </summary>
        public static LocalisableString HighFreqDamp => new TranslatableString(getKey(@"reverb_high_frequency_damping"), "High frequency damping");

        /// <summary>
        /// "Room size"
        /// </summary>
        public static LocalisableString RoomSize => new TranslatableString(getKey(@"reverb_room_size"), "Room size");

        /// <summary>
        /// "Rotate rate"
        /// </summary>
        public static LocalisableString RotateParameters_fRate => new TranslatableString(getKey(@"audio_effect_rotate_rate"), "Rotate rate");

        /// <summary>
        /// "3D rotate effect"
        /// </summary>
        public static LocalisableString RotateParameters_Enabled => new TranslatableString(getKey(@"audio_effect_rotate_enabled"), "3D rotate effect");

        /// <summary>
        /// "Dry mix"
        /// </summary>
        public static LocalisableString DryMix => new TranslatableString(getKey(@"echo_dry_mix"), "Dry mix");

        /// <summary>
        /// "Wet mix"
        /// </summary>
        public static LocalisableString EchoWetMix => new TranslatableString(getKey(@"echo_wet_mix"), "Wet mix"); //separated from reverb_wet_mix

        /// <summary>
        /// "Feedback"
        /// </summary>
        public static LocalisableString EchoFeedback => new TranslatableString(getKey(@"echo_feedback"), "Feedback");

        /// <summary>
        /// "Delay"
        /// </summary>
        public static LocalisableString EchoDelay => new TranslatableString(getKey(@"echo_delay"), "Delay");

        /// <summary>
        /// "Echo"
        /// </summary>
        public static LocalisableString EchoEffect => new TranslatableString(getKey(@"echo_effect"), "Echo");

        /// <summary>
        /// "Distortion"
        /// </summary>
        public static LocalisableString DistortionEffect => new TranslatableString(getKey(@"distortion_effect"), "Distortion");

        /// <summary>
        /// "Distortion volume"
        /// </summary>
        public static LocalisableString DistortionVolume => new TranslatableString(getKey(@"distortion_volume"), "Distortion volume");

        /// <summary>
        /// "Video bloom level"
        /// </summary>
        public static LocalisableString VideoBloomLevel => new TranslatableString(getKey(@"bloom"), "Video bloom level");

        /// <summary>
        /// "Chromatic aberration effect strength"
        /// </summary>
        public static LocalisableString ChromaticAberration => new TranslatableString(getKey(@"chromatic_aberration"), "Chromatic aberration effect strength");

        /// <summary>
        /// "Visual effects"
        /// </summary>
        public static LocalisableString VisualEffects => new TranslatableString(getKey(@"visual_effects"), "Visual effects");

        /// <summary>
        /// "Video grayscale level"
        /// </summary>
        public static LocalisableString VideoGrayscaleLevel => new TranslatableString(getKey(@"grayscale"), "Video grayscale level");

        /// <summary>
        /// "Video hue shift"
        /// </summary>
        public static LocalisableString VideoHueShift => new TranslatableString(getKey(@"hue_shift"), "Video hue shift");

        /// <summary>
        /// "Discord Rich Presence"
        /// </summary>
        public static LocalisableString DiscordRichPresence => new TranslatableString(getKey(@"discord_rich_presence"), @"Discord Rich Presence");

        /// <summary>
        /// "Hide video information"
        /// </summary>
        public static LocalisableString HideIdentifiableInformation => new TranslatableString(getKey(@"hide_identifiable_information"), @"Hide video information");

        /// <summary>
        /// "Full"
        /// </summary>
        public static LocalisableString DiscordPresenceFull => new TranslatableString(getKey(@"discord_presence_full"), @"Full");

        /// <summary>
        /// "Off"
        /// </summary>
        public static LocalisableString DiscordPresenceOff => new TranslatableString(getKey(@"discord_presence_off"), @"Off");

        /// <summary>
        /// "Cancel"
        /// </summary>
        public static LocalisableString Cancel => new TranslatableString(getKey(@"cancel"), @"Cancel");

        /// <summary>
        /// "Yes"
        /// </summary>
        public static LocalisableString Yes => new TranslatableString(getKey(@"yes"), @"Yes");

        /// <summary>
        /// "Are you sure you want to unsubscribe this channel?"
        /// </summary>
        public static LocalisableString UnsubscribeDesc => new TranslatableString(getKey(@"unsubscribe_desc"), @"Are you sure you want to unsubscribe this channel?");

        /// <summary>
        /// "Subscribe"
        /// </summary>
        public static LocalisableString Subscribe => new TranslatableString(getKey(@"subscribe"), @"Subscribe");

        /// <summary>
        /// "Unsubscribe"
        /// </summary>
        public static LocalisableString Unsubscribe => new TranslatableString(getKey(@"unsubscribe"), @"Unsubscribe");

        /// <summary>
        /// "Subscription removed."
        /// </summary>
        public static LocalisableString SubscriptionRemoved => new TranslatableString(getKey(@"subscription_removed"), @"Subscription removed.");

        /// <summary>
        /// "Subscription added."
        /// </summary>
        public static LocalisableString SubscriptionAdded => new TranslatableString(getKey(@"subscription_added"), @"Subscription added.");

        /// <summary>
        /// "Closed captions"
        /// </summary>
        public static LocalisableString ClosedCaptions => new TranslatableString(getKey(@"closed_captions"), @"Closed captions");

        /// <summary>
        /// "Summarize via ChatGPT"
        /// </summary>
        public static LocalisableString SummarizeViaGPT => new TranslatableString(getKey(@"summarize_via_gpt"), @"Summarize via ChatGPT");

        /// <summary>
        /// "{0} Summarize this video"
        /// </summary>
        public static LocalisableString GPTSummarizePrompt(LocalisableString videoUrl) => new TranslatableString(getKey(@"gpt_summarize_prompt"), @"{0} Summarize this video", videoUrl);

        /// <summary>
        /// "Public"
        /// </summary>
        public static LocalisableString Public => new TranslatableString(getKey(@"public"), @"Public");

        /// <summary>
        /// "Unlisted"
        /// </summary>
        public static LocalisableString Unlisted => new TranslatableString(getKey(@"unlisted"), @"Unlisted");

        /// <summary>
        /// "Private"
        /// </summary>
        public static LocalisableString Private => new TranslatableString(getKey(@"private"), @"Private");

        /// <summary>
        /// "Save"
        /// </summary>
        public static LocalisableString Save => new TranslatableString(getKey(@"save"), @"Save");

        /// <summary>
        /// "Save or remove"
        /// </summary>
        public static LocalisableString SaveOrRemove => new TranslatableString(getKey(@"save_or_remove"), @"Save or remove");

        /// <summary>
        /// "Save location"
        /// </summary>
        public static LocalisableString SaveLocation => new TranslatableString(getKey(@"save_location"), @"Save location");

        /// <summary>
        /// "Add new playlist"
        /// </summary>
        public static LocalisableString AddNewPlaylist => new TranslatableString(getKey(@"add_new_playlist"), @"Add new playlist");

        /// <summary>
        /// "Privacy status"
        /// </summary>
        public static LocalisableString PrivacyStatus => new TranslatableString(getKey(@"privacy_status"), @"Privacy status");

        /// <summary>
        /// "Enter title here"
        /// </summary>
        public static LocalisableString TitlePlaceholder => new TranslatableString(getKey(@"title_placeholder"), @"Enter title here");

        /// <summary>
        /// "Title"
        /// </summary>
        public static LocalisableString Title => new TranslatableString(getKey(@"title"), @"Title");

        /// <summary>
        /// "Create"
        /// </summary>
        public static LocalisableString Create => new TranslatableString(getKey(@"create"), @"Create");

        /// <summary>
        /// "Added video {0} to playlist {1}."
        /// </summary>
        public static LocalisableString VideoSavedToPlaylist(string videoName, string playlistName) => new TranslatableString(getKey(@"video_saved_to_playlist"), @"Added video {0} to playlist {1}.", videoName, playlistName);

        /// <summary>
        /// "Removed video {0} from playlist {1}."
        /// </summary>
        public static LocalisableString VideoRemovedFromPlaylist(string videoName, string playlistName) => new TranslatableString(getKey(@"video_removed_from_playlist"), @"Removed video {0} from playlist {1}.", videoName, playlistName);

        /// <summary>
        /// "Menu"
        /// </summary>
        public static LocalisableString Menu => new TranslatableString(getKey(@"menu"), @"Menu");

        /// <summary>
        /// "Exit"
        /// </summary>
        public static LocalisableString Exit => new TranslatableString(getKey(@"exit"), @"Exit");

        /// <summary>
        /// "Normalize Audio"
        /// </summary>
        public static LocalisableString AudioNormalization => new TranslatableString(getKey(@"audio_normalization"), @"Normalize Audio");

        /// <summary>
        /// "Karaoke"
        /// </summary>
        public static LocalisableString KaraokeEffect => new TranslatableString(getKey(@"karaoke_effect"), "Karaoke");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
