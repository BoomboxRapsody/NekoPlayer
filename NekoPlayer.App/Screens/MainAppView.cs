// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DiscordRPC;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Humanizer;
using NekoPlayer.App.Audio;
using NekoPlayer.App.Config;
using NekoPlayer.App.Extensions;
using NekoPlayer.App.Graphics;
using NekoPlayer.App.Graphics.Characters;
using NekoPlayer.App.Graphics.Containers;
using NekoPlayer.App.Graphics.Shaders;
using NekoPlayer.App.Graphics.Spine;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Graphics.UserInterface;
using NekoPlayer.App.Graphics.UserInterfaceV2;
using NekoPlayer.App.Graphics.UserInterfaceV3;
using NekoPlayer.App.Graphics.Videos;
using NekoPlayer.App.Input;
using NekoPlayer.App.Input.Binding;
using NekoPlayer.App.Localisation;
using NekoPlayer.App.Online;
using NekoPlayer.App.Overlays;
using NekoPlayer.App.Overlays.Containers;
using NekoPlayer.App.Overlays.OSD;
using NekoPlayer.App.Overlays.Volume;
using NekoPlayer.App.Updater;
using NekoPlayer.App.Utils;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics.Video;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using PaletteNet;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;
using static Google.Apis.YouTube.v3.CommentThreadsResource.ListRequest;
using Container = osu.Framework.Graphics.Containers.Container;
using Language = NekoPlayer.App.Localisation.Language;
using OverlayContainer = NekoPlayer.App.Graphics.Containers.OverlayContainer;

namespace NekoPlayer.App.Screens
{
    public partial class MainAppView : NekoPlayerScreen, IKeyBindingHandler<GlobalAction>, INekoPlayerAppMessageHandler
    {
        private BufferedContainer videoContainer;
        private AdaptiveButton commentSendButton, searchButton, loadPlaylistBtn, downloadBtn;
        private RoundedButton acceptButton, updatePlaylistButton, loadBtn, viewChannelButton;
        private RoundedAltButton logoutButton, declineButton;
        private ControlBarIconButton prevVideoButton, nextVideoButton;
        private EnhancedFocusedTextBox videoIdBox, playlistIdBox, searchTextBox;
        private EnhancedFocusedTextBoxWithProfileImage commentTextBox;
        private NekoPlayerLoadingSpinner spinner;
        private ScheduledDelegate spinnerShow;
        private IdleTracker idleTracker;
        private Container uiContainer;
        private DrawSizePreservingFillContainer uiGradientContainer;
        private SettingsContainer settingsContainer;
        private OverlayContainer loadVideoContainer, videoDescriptionContainer, commentsContainer, searchContainer, reportAbuseOverlay, loadPlaylistContainer, unsubscribeDialog, addPlaylistOverlay, videoSaveLocationOverlay, myChannelDialog, editPlaylistOverlay, downloadReadyContainer, downloadOverlay, downloadCompletedOverlay;
        private SideOverlayContainer playlistOverlay, audioEffectsOverlay, menuOverlay, myPlaylistsOverlay, exitOptions;
        private IconButton menuOverlayShow;
        private MenuButtonItem loadBtnOverlayShow, settingsOverlayShowBtn, commentOpenButton, searchOpenButton, reportOpenButton, playlistOpenButton, audioEffectsOpenButton, saveVideoOpenButton, newPlaylistOpenButton, myPlaylistsOpenButton;
        private VideoMetadataDisplayWithoutProfile videoMetadataDisplay;
        private VideoMetadataDisplay videoMetadataDisplayDetails;
        private RoundedButtonContainer commentOpenButtonDetails, likeButton;



        private string[] broWhat = new[]
        {
            @"cuayo",
            @"cuayo1",
            @"cuayo2",
            @"cuayo3",
            @"ner",
            @"speaki_ner",
            @"speaki1",
        };

        private FormEnumDropdown<PrivacyStatus> playlistPrivacyStatusDropdown, editPlaylistPrivacyStatusDropdown;

        private YouTubeChannelMetadataDisplay youtubeChannelMetadataDisplay, youtubeChannelMetadataDisplay2;

        private SettingsItemV2 audioLanguageItem, audioLanguageItem2;

        private Sample overlayShowSample, overlayHideSample;
        private AdaptiveMaterialButton reportButton;
        private FormTextBox reportComment, playlistTitleBox, editPlaylistTitleBox;

        private FormDropdown<Playlist> myPlaylistsDropdown;

        private Container overlayFadeContainer;
        private Container commentsEmpty;
        private RoundedButtonContainer dislikeButton;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // Be sure to dispose the track, otherwise memory will be leaked!
            // This is automatic for DrawableTrack.
            overlayShowSample.Dispose();
            overlayHideSample.Dispose();
            currentVideoSource?.Expire();
        }

        [Resolved]
        private AudioManager audio { get; set; } = null!;

        private AdaptiveSpriteText videoLoadingProgress, videoInfoDetails, likeCount, dislikeCount, commentCount, commentsContainerTitle, currentTime, totalTime, volumeText;
        private AdaptiveSpriteText speedText;
        private LinkFlowContainer videoDescription;
        private FillFlowContainer commentContainer, searchResultContainer, playlistItemsView, myPlaylistItemsView;

        [Resolved]
        private GoogleOAuth2 googleOAuth2 { get; set; } = null!;

        private ReportDropdown reportReason, reportSubReason;

        private BindableNumber<double> videoProgress = new BindableNumber<double>()
        {
            MinValue = 0,
            MaxValue = 1,
        };

        private Bindable<double> windowedPositionX = null!;
        private Bindable<double> windowedPositionY = null!;
        private Bindable<WindowMode> windowMode = null!;
        private Bindable<List<InternalShader>> appliedEffects = new Bindable<List<InternalShader>>();

        private Bindable<ClosedCaptionLanguage> captionLanguage = null!;
        private bool isControlVisible = true;

        private Bindable<Config.AudioQuality> audioQuality;
        private Bindable<Config.VideoQuality> videoQuality;
        private Bindable<HardwareVideoDecoder> hardwareVideoDecoder;
        private Bindable<Localisation.Language> audioLanguage;
        private Bindable<bool> adjustPitch;
        private Bindable<string> localeBindable = new Bindable<string>();
        private ThumbnailContainerBackground thumbnailContainer;
        private NekoPlayerSeekBar<double> seekbar;
        private Bindable<LocalisableString> updateInfomationText;
        private Bindable<bool> updateButtonEnabled, fpsDisplay, captionEnabled, use_sdl3;
        private Bindable<AspectRatioMethod> aspectRatioMethod;
        private Bindable<DiscordRichPresenceMode> discordRichPresence;

        [Resolved]
        private AudioEffectsConfigManager audioEffectsConfig { get; set; } = null!;

        private AdaptiveTextFlowContainer debugInfo;

        private BufferedContainer videoScalingContainer;

        private Box likeButtonBackground, dislikeButtonBackground, likeButtonBackgroundSelected, dislikeButtonBackgroundSelected, speedBarBG, volumeBarBG, timeBG;

        private FillFlowContainer likeButtonForeground, dislikeButtonForeground;

        private Container userInterfaceContainer;

        private Bindable<bool> alwaysUseOriginalAudio;

        [Resolved]
        private AdaptiveColour colours { get; set; } = null!;

        private Bindable<SettingsNote.Data> videoQualityWarning = new Bindable<SettingsNote.Data>();
        private Bindable<SettingsNote.Data> oauth_note = new Bindable<SettingsNote.Data>();
        private Bindable<SettingsNote.Data> hwAccelNote = new Bindable<SettingsNote.Data>();

        private Bindable<OverlayColourScheme> colourSchemeBindable;
        private Bindable<ProfileImageShape> profileImageShape;
        private Bindable<CloseButtonAction> closeButtonAction;

        private Bindable<VideoMetadataDisplayAlignment> videoMetadataDisplayAlignment;

        private Bindable<UIFont> ui_font;
        private Bindable<CaptionFonts> caption_font;

        private Bindable<float> scalingBackgroundDim = null!;

        private Bindable<double> speedTextRolling;
        private Bindable<double> volumeTextRolling;

        private SpriteIcon volumeIcon, speedBarIcon;

        private PlaybackSpeedSliderBar speedBarSlider;
        private RoundedSliderBar<double> volumeBarSlider;

        private SpineSprite menuOverlayCharacter, audioEffectsOverlayCharacter, settingsOverlayCharacter;

        private LinkFlowContainer playlistAuthor;

        private Bindable<bool> signedIn;

        //private ParallaxContainer thumbnailContainerBase;

        [Resolved]
        private ShaderManager shaderManager { get; set; } = null!;

        private Bindable<double> videoVolume;

        private GhostIcon ghostIcon;

#nullable enable
        [Resolved(canBeNull: true)]
        private Online.DiscordRPC? discordRPC { get; set; }
#nullable disable

        //effects
        private Bindable<bool> reverbEnabled, rotateEnabled, echoEnabled, distortionEnabled, karaokeEnabled, chorusEnabled;
        private FillFlowContainer reverbSettings, rotateSettings, echoSettings, distortionSettings, chorusSettings;

        private Bindable<bool> repeat = new Bindable<bool>();
        private Bindable<bool> alwaysShowControl = new Bindable<bool>();

        protected T GetShaderByType<T>() where T : InternalShader, new()
            => shaderManager.LocalInternalShader<T>();

        private ControlBarIconButton repeatButton, pinButton, captionButton, videoSettingsButton, playlistButton, quickLikeButton, quickDislikeButton, quickCommentOpenButton;

        private AdaptiveSpriteText timeText;

        private Bindable<bool> trayIconVisible;

        private Bindable<CommentsSortCriteria> CommentsSort;
        private Bindable<SearchSortCriteria> SearchSort;

        protected Bindable<ReleaseStream> ReleaseStream;

        private Bindable<SFXType> overlaySFXType;
        private Bindable<bool> playOverlaySFX;

        private Bindable<float> captionBGOpacity;

        private CancellationTokenSource videoLoadProcess;

        private bool commentTextBoxContainerFocused, searchTextBoxContainerFocused;
        private Container commentTextBoxContainer, searchTextBoxContainer;

        [BackgroundDependencyLoader]
        private void load(ISampleStore sampleStore, FrameworkConfigManager config, NekoPlayerConfigManager appConfig, GameHost host, Storage storage, OverlayColourProvider overlayColourProvider, TextureStore textures, FrameworkDebugConfigManager debugConfig)
        {
            speedTextRolling = new Bindable<double>(1);
            volumeTextRolling = new Bindable<double>(1);
            appliedEffects.Value = new List<InternalShader>();
            window = host.Window;

            app.RegisterMessage(this);

            videoVolume = config.GetBindable<double>(FrameworkSetting.VolumeMusic);

            showVideoMetadataOnWindowTitle = appConfig.GetBindable<bool>(NekoPlayerSetting.ShowVideoMetadataOnWindowTitle);

            uiVisible = screenshotManager.CursorVisibility.GetBoundCopy();
            signedIn = googleOAuth2.SignedIn.GetBoundCopy();

            isAnyOverlayOpen = sessionStatics.GetBindable<bool>(Static.IsAnyOverlayOpen);
            videoPlaying = sessionStatics.GetBindable<bool>(Static.IsVideoPlaying);
            trayIconVisible = sessionStatics.GetBindable<bool>(Static.WindowIsTray);
            ReleaseStream = appConfig.GetBindable<ReleaseStream>(NekoPlayerSetting.ReleaseStream);

            playOverlaySFX = appConfig.GetBindable<bool>(NekoPlayerSetting.PlayOverlaySFX);
            overlaySFXType = appConfig.GetBindable<SFXType>(NekoPlayerSetting.OverlaySFXType);

            captionBGOpacity = appConfig.GetBindable<float>(NekoPlayerSetting.CaptionBGOpacity);

            usernameDisplayMode = appConfig.GetBindable<UsernameDisplayMode>(NekoPlayerSetting.UsernameDisplayMode);
            CommentsSort = appConfig.GetBindable<CommentsSortCriteria>(NekoPlayerSetting.CommentsSortCriteria);
            SearchSort = appConfig.GetBindable<SearchSortCriteria>(NekoPlayerSetting.SearchSortCriteria);

            reverbEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.ReverbEnabled);
            rotateEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.RotateEnabled);
            echoEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.EchoEnabled);
            distortionEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.DistortionEnabled);
            karaokeEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.KaraokeEnabled);
            chorusEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.ChorusEnabled);

            videoMetadataDisplayAlignment = appConfig.GetBindable<VideoMetadataDisplayAlignment>(NekoPlayerSetting.VideoMetadataDisplayAlignment);

            scalingMode = appConfig.GetBindable<ScalingMode>(NekoPlayerSetting.Scaling);
            scalingSizeX = appConfig.GetBindable<float>(NekoPlayerSetting.ScalingSizeX);
            scalingSizeY = appConfig.GetBindable<float>(NekoPlayerSetting.ScalingSizeY);
            scalingPositionX = appConfig.GetBindable<float>(NekoPlayerSetting.ScalingPositionX);
            scalingPositionY = appConfig.GetBindable<float>(NekoPlayerSetting.ScalingPositionY);
            scalingBackgroundDim = appConfig.GetBindable<float>(NekoPlayerSetting.ScalingBackgroundDim);
            alwaysUseOriginalAudio = appConfig.GetBindable<bool>(NekoPlayerSetting.AlwaysUseOriginalAudio);
            discordRichPresence = appConfig.GetBindable<DiscordRichPresenceMode>(NekoPlayerSetting.DiscordRichPresence);
            closeButtonAction = appConfig.GetBindable<CloseButtonAction>(NekoPlayerSetting.CloseButtonAction);
            colourSchemeBindable = appConfig.GetBindable<OverlayColourScheme>(NekoPlayerSetting.ColourScheme);
            profileImageShape = appConfig.GetBindable<ProfileImageShape>(NekoPlayerSetting.ProfileImageShape);

            captionEnabled = appConfig.GetBindable<bool>(NekoPlayerSetting.CaptionEnabled);

            localeBindable = config.GetBindable<string>(FrameworkSetting.Locale);
            fpsDisplay = appConfig.GetBindable<bool>(NekoPlayerSetting.ShowFpsDisplay);
            use_sdl3 = config.GetBindable<bool>(FrameworkSetting.UseExperimentalSDL3);
            adjustPitch = appConfig.GetBindable<bool>(NekoPlayerSetting.AdjustPitchOnSpeedChange);
            audioQuality = appConfig.GetBindable<Config.AudioQuality>(NekoPlayerSetting.AudioQuality);
            videoQuality = appConfig.GetBindable<Config.VideoQuality>(NekoPlayerSetting.VideoQuality);
            audioLanguage = appConfig.GetBindable<Localisation.Language>(NekoPlayerSetting.AudioLanguage);
            hardwareVideoDecoder = config.GetBindable<HardwareVideoDecoder>(FrameworkSetting.HardwareVideoDecoder);
            cursorInWindow = host.Window?.CursorInWindow.GetBoundCopy();
            windowMode = config.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            captionLanguage = appConfig.GetBindable<ClosedCaptionLanguage>(NekoPlayerSetting.ClosedCaptionLanguage);
            windowedPositionX = config.GetBindable<double>(FrameworkSetting.WindowedPositionX);
            windowedPositionY = config.GetBindable<double>(FrameworkSetting.WindowedPositionY);
            updateInfomationText = game.UpdateManagerVersionText.GetBoundCopy();
            updateButtonEnabled = game.UpdateButtonEnabled.GetBoundCopy();

            ui_font = appConfig.GetBindable<UIFont>(NekoPlayerSetting.UIFont);
            caption_font = appConfig.GetBindable<CaptionFonts>(NekoPlayerSetting.CaptionFont);

            aspectRatioMethod = appConfig.GetBindable<AspectRatioMethod>(NekoPlayerSetting.AspectRatioMethod);

            accentColor = overlayColourProvider1.Content2;
            bgColor = overlayColourProvider1.Background3;

            overlaySFXType.BindValueChanged(sfx =>
            {
                refreshSFX();
            }, true);

            use_sdl3.BindValueChanged(_ =>
            {
                if (game?.RestartAppWhenExited() == true)
                {
                    game.AttemptExit();
                }
            });

            colourSchemeBindable.BindValueChanged(_ =>
            {
                if (game?.RestartAppWhenExited() == true)
                {
                    game.AttemptExit();
                }
            });

            ui_font.BindValueChanged(_ =>
            {
                if (game?.RestartAppWhenExited() == true)
                {
                    game.AttemptExit();
                }
            });

            #region The UI Components
            InternalChildren = new Drawable[]
            {
                idleTracker = new AppIdleTracker(3000),
                videoScalingContainer = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new ScalingContainerNew(ScalingMode.Video)
                    {
                        Children = new Drawable[] {
                            new ParallaxContainer
                            {
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.Black,
                                    },
                                    thumbnailContainer = new ThumbnailContainerBackground
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.Black,
                                        Alpha = .5f,
                                    },
                                },
                            },
                        },
                    },
                },
                videoContainer = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                new GlobalScrollAdjustsVolume(),
                userInterfaceContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        spinner = new NekoPlayerLoadingSpinner(true, true)
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Margin = new MarginPadding(40),
                        },
                        videoLoadingProgress = new AdaptiveSpriteText
                        {
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Margin = new MarginPadding
                            {
                                Bottom = 110,
                            },
                        },
                        uiGradientContainer = new DrawSizePreservingFillContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(0.65f), Color4.Black.Opacity(0)),
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    RelativeSizeAxes = Axes.X,
                                    Height = 300,
                                },
                                new Box
                                {
                                    Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(0), Color4.Black.Opacity(0.65f)),
                                    Origin = Anchor.BottomLeft,
                                    Anchor = Anchor.BottomLeft,
                                    RelativeSizeAxes = Axes.X,
                                    Height = 300,
                                },
                            }
                        },
                        uiContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding(8),
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Right = 40,
                                    },
                                    Child = videoMetadataDisplay = new VideoMetadataDisplayWithoutProfile
                                    {
                                        Width = 520,
                                        Height = 70,
                                        Origin = Anchor.TopLeft,
                                        Anchor = Anchor.TopLeft,
                                        Position = new Vector2(-18, -18),
                                        Margin = new MarginPadding
                                        {
                                            Left = 8,
                                        },
                                        ClickEvent = _ => showOverlayContainer(videoDescriptionContainer),
                                    },
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 4,
                                    },
                                    Children = new Drawable[]
                                    {
                                        menuOverlayShow = new IconButton
                                        {
                                            Enabled = { Value = true },
                                            Origin = Anchor.TopRight,
                                            Anchor = Anchor.TopRight,
                                            Size = new Vector2(40, 40),
                                            Icon = FontAwesome.Solid.Bars,
                                            IconScale = new Vector2(1.2f),
                                            TooltipText = NekoPlayerStrings.Menu,
                                            BackgroundColour = overlayColourProvider.Background5,
                                        },
                                    }
                                },
                                new Container
                                {
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    RelativeSizeAxes = Axes.X,
                                    Height = 84,
                                    Masking = false,
                                    //CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS,
                                    /*
                                    EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                    {
                                        Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                        Colour = Color4.Black.Opacity(0.25f),
                                        Offset = new Vector2(0, 2),
                                        Radius = 16,
                                    },
                                    */
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = overlayColourProvider.Background5,
                                            Alpha = 0f,
                                        },
                                        new FillFlowContainer {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding(16),
                                            Spacing = new Vector2(0, 2),
                                            Children = new Drawable[] {
                                                seekbar = new RoundedSeekBar
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    PlaySamplesOnAdjust = false,
                                                    DisplayAsPercentage = true,
                                                    AlwaysPresent = true,
                                                    Current = { BindTarget = videoProgress },
                                                },
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Children = new Drawable[] {
                                                        currentTime = new AdaptiveSpriteText
                                                        {
                                                            Anchor = Anchor.TopLeft,
                                                            Origin = Anchor.TopLeft,
                                                            Text = "0:00",
                                                            Alpha = 0,
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                    },
                                                },
                                                new AdaptiveRoundedScrollContainer(Direction.Horizontal)
                                                {
                                                    ScrollbarVisible = false,
                                                    Masking = false,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Children = new Drawable[]
                                                    {
                                                        new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.Y,
                                                            AutoSizeAxes = Axes.X,
                                                            AlwaysPresent = true,
                                                            Spacing = new Vector2(8, 0),
                                                            Direction = FillDirection.Horizontal,
                                                            Children = new Drawable[]
                                                            {
                                                                new Container
                                                                {
                                                                    AutoSizeAxes = Axes.X,
                                                                    Height = 30,
                                                                    Children = new Drawable[]
                                                                    {
                                                                        new FillFlowContainer
                                                                        {
                                                                            AutoSizeAxes = Axes.Both,
                                                                            Spacing = new Vector2(3, 0),
                                                                            Direction = FillDirection.Horizontal,
                                                                            Children = new Drawable[]
                                                                            {
                                                                                prevVideoButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = false },
                                                                                    Icon = FontAwesome.Solid.FastBackward,
                                                                                    TooltipText = NekoPlayerStrings.PreviousVideo,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = async _ =>
                                                                                    {
                                                                                        if (playlists.Count > 0)
                                                                                        {
                                                                                            if (playlistItemIndex != 0)
                                                                                                playlistItemIndex--;

                                                                                            Schedule(async () =>
                                                                                            {
                                                                                                SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId);
                                                                                            });
                                                                                        }
                                                                                    }
                                                                                },
                                                                                playPause = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = true },
                                                                                    Icon = FontAwesome.Solid.Play,
                                                                                    TooltipText = NekoPlayerStrings.Play,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = _ =>
                                                                                    {
                                                                                        if (currentVideoSource != null)
                                                                                        {
                                                                                            if (currentVideoSource.IsPlaying())
                                                                                                currentVideoSource.Pause();
                                                                                            else
                                                                                                currentVideoSource.Play();
                                                                                        }
                                                                                    }
                                                                                },
                                                                                nextVideoButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = false },
                                                                                    Icon = FontAwesome.Solid.FastForward,
                                                                                    TooltipText = NekoPlayerStrings.NextVideo,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = async _ =>
                                                                                    {
                                                                                        if (playlists.Count > 0)
                                                                                        {
                                                                                            if (playlistItemIndex != playlists.Count - 1)
                                                                                                playlistItemIndex++;

                                                                                            Schedule(async () =>
                                                                                            {
                                                                                                SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId);
                                                                                            });
                                                                                        }
                                                                                    }
                                                                                },
                                                                                repeatButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = true },
                                                                                    Icon = FontAwesome.Solid.Sync,
                                                                                    TooltipText = NekoPlayerStrings.Repeat,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = _ =>
                                                                                    {
                                                                                        updateRepeatState();
                                                                                    }
                                                                                },
                                                                                captionButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = true },
                                                                                    Icon = FontAwesome.Solid.ClosedCaptioning,
                                                                                    TooltipText = NekoPlayerStrings.ClosedCaptions,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = _ =>
                                                                                    {
                                                                                        CycleCaptionLanguage();
                                                                                    }
                                                                                },
                                                                                videoSettingsButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = true },
                                                                                    Icon = FontAwesome.Solid.Cog,
                                                                                    TooltipText = NekoPlayerStrings.VideoSettings,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = _ =>
                                                                                    {
                                                                                        ShowSettingsOverlayAtName("Video Settings");
                                                                                    }
                                                                                },
                                                                                playlistButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = true },
                                                                                    Icon = FontAwesome.Solid.List,
                                                                                    TooltipText = NekoPlayerStrings.Playlists,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = _ =>
                                                                                    {
                                                                                        showOverlayContainer(playlistOverlay);
                                                                                    }
                                                                                },
                                                                                quickCommentOpenButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = false },
                                                                                    Icon = FontAwesome.Regular.CommentAlt,
                                                                                    TooltipText = NekoPlayerStrings.Comments("0"),
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = _ =>
                                                                                    {
                                                                                        showOverlayContainer(commentsContainer);
                                                                                    }
                                                                                },
                                                                                pinButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = true },
                                                                                    Icon = FontAwesome.Solid.MapPin,
                                                                                    TooltipText = NekoPlayerStrings.PlayerControlPin,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                    ClickAction = _ =>
                                                                                    {
                                                                                        updatePinState();
                                                                                    }
                                                                                },
                                                                                quickLikeButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = false },
                                                                                    Icon = FontAwesome.Solid.ThumbsUp,
                                                                                    TooltipText = NekoPlayerStrings.LikeButton,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                },
                                                                                quickDislikeButton = new ControlBarIconButton(false)
                                                                                {
                                                                                    Width = 50,
                                                                                    Enabled = { Value = false },
                                                                                    Icon = FontAwesome.Solid.ThumbsDown,
                                                                                    TooltipText = NekoPlayerStrings.DislikeButton,
                                                                                    IconColour = overlayColourProvider.Content2,
                                                                                    BackgroundColour = overlayColourProvider.Background3,
                                                                                    IconScale = new Vector2(0.85f),
                                                                                },
                                                                            }
                                                                        }
                                                                    }
                                                                },
                                                                new Container
                                                                {
                                                                    AutoSizeAxes = Axes.X,
                                                                    Height = 30,
                                                                    Masking = true,
                                                                    CornerRadius = 15,
                                                                    /*
                                                                    EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                                                    {
                                                                        Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                                                        Colour = Color4.Black.Opacity(0.25f),
                                                                        Offset = new Vector2(0, 2),
                                                                        Radius = 16,
                                                                    },
                                                                    */
                                                                    Children = new Drawable[]
                                                                    {
                                                                        speedBarBG = new Box
                                                                        {
                                                                            RelativeSizeAxes = Axes.Both,
                                                                            Colour = overlayColourProvider.Background3,
                                                                            Alpha = 1f,
                                                                        },
                                                                        new FillFlowContainer
                                                                        {
                                                                            AutoSizeAxes = Axes.Both,
                                                                            Spacing = new Vector2(8, 0),
                                                                            Direction = FillDirection.Horizontal,
                                                                            Padding = new MarginPadding
                                                                            {
                                                                                Horizontal = 8
                                                                            },
                                                                            Children = new Drawable[]
                                                                            {
                                                                                speedBarIcon = new SpriteIcon
                                                                                {
                                                                                    Icon = FontAwesome.Solid.TachometerAlt,
                                                                                    Width = 16,
                                                                                    Height = 16,
                                                                                    Margin = new MarginPadding
                                                                                    {
                                                                                        Top = 8,
                                                                                    },
                                                                                    Colour = overlayColourProvider.Content2,
                                                                                },
                                                                                speedBarSlider = new PlaybackSpeedSliderBar
                                                                                {
                                                                                    Width = 200,
                                                                                    Margin = new MarginPadding
                                                                                    {
                                                                                        Top = 6,
                                                                                    },
                                                                                    KeyboardStep = 0.05f,
                                                                                    PlaySamplesOnAdjust = true,
                                                                                    AlwaysPresent = true,
                                                                                    Current = { BindTarget = playbackSpeed },
                                                                                },
                                                                                speedText = new AdaptiveSpriteText
                                                                                {
                                                                                    Margin = new MarginPadding
                                                                                    {
                                                                                        Top = 7
                                                                                    },
                                                                                    AlwaysPresent = true,
                                                                                    Font = NekoPlayerApp.DefaultFont.With(weight: "Bold"),
                                                                                    Colour = overlayColourProvider.Content2,
                                                                                },
                                                                            }
                                                                        }
                                                                    }
                                                                },
                                                                new Container
                                                                {
                                                                    AutoSizeAxes = Axes.X,
                                                                    Height = 30,
                                                                    Masking = true,
                                                                    CornerRadius = 15,
                                                                    /*
                                                                    EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                                                    {
                                                                        Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                                                        Colour = Color4.Black.Opacity(0.25f),
                                                                        Offset = new Vector2(0, 2),
                                                                        Radius = 16,
                                                                    },
                                                                    */
                                                                    Children = new Drawable[]
                                                                    {
                                                                        volumeBarBG = new Box
                                                                        {
                                                                            RelativeSizeAxes = Axes.Both,
                                                                            Colour = overlayColourProvider.Background3,
                                                                            Alpha = 1f,
                                                                        },
                                                                        new FillFlowContainer
                                                                        {
                                                                            AutoSizeAxes = Axes.Both,
                                                                            Spacing = new Vector2(8, 0),
                                                                            Direction = FillDirection.Horizontal,
                                                                            Padding = new MarginPadding
                                                                            {
                                                                                Horizontal = 8
                                                                            },
                                                                            Children = new Drawable[]
                                                                            {
                                                                                volumeIcon = new SpriteIcon
                                                                                {
                                                                                    Icon = FontAwesome.Solid.VolumeUp,
                                                                                    Width = 16,
                                                                                    Height = 16,
                                                                                    Margin = new MarginPadding
                                                                                    {
                                                                                        Top = 8,
                                                                                    },
                                                                                    Colour = overlayColourProvider.Content2,
                                                                                },
                                                                                volumeBarSlider = new RoundedSliderBar<double>
                                                                                {
                                                                                    Width = 200,
                                                                                    Margin = new MarginPadding
                                                                                    {
                                                                                        Top = 6,
                                                                                    },
                                                                                    KeyboardStep = 0.05f,
                                                                                    PlaySamplesOnAdjust = false,
                                                                                    DisplayAsPercentage = true,
                                                                                    AlwaysPresent = true,
                                                                                    Current = videoVolume,
                                                                                },
                                                                                volumeText = new AdaptiveSpriteText
                                                                                {
                                                                                    Margin = new MarginPadding
                                                                                    {
                                                                                        Top = 7
                                                                                    },
                                                                                    AlwaysPresent = true,
                                                                                    Font = NekoPlayerApp.DefaultFont.With(weight: "Bold"),
                                                                                    Colour = overlayColourProvider.Content2,
                                                                                },
                                                                            }
                                                                        }
                                                                    }
                                                                },
                                                                new Container
                                                                {
                                                                    AutoSizeAxes = Axes.X,
                                                                    Height = 30,
                                                                    Masking = true,
                                                                    CornerRadius = 15,
                                                                    /*
                                                                    EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                                                    {
                                                                        Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                                                        Colour = Color4.Black.Opacity(0.25f),
                                                                        Offset = new Vector2(0, 2),
                                                                        Radius = 16,
                                                                    },
                                                                    */
                                                                    Children = new Drawable[]
                                                                    {
                                                                        timeBG = new Box
                                                                        {
                                                                            RelativeSizeAxes = Axes.Both,
                                                                            Colour = overlayColourProvider.Background3,
                                                                            Alpha = 1f,
                                                                        },
                                                                        new FillFlowContainer
                                                                        {
                                                                            AutoSizeAxes = Axes.Both,
                                                                            Spacing = new Vector2(8, 0),
                                                                            Direction = FillDirection.Horizontal,
                                                                            Padding = new MarginPadding
                                                                            {
                                                                                Horizontal = 8
                                                                            },
                                                                            Children = new Drawable[]
                                                                            {
                                                                                timeText = new AdaptiveSpriteText
                                                                                {
                                                                                    Margin = new MarginPadding
                                                                                    {
                                                                                        Top = 7
                                                                                    },
                                                                                    AlwaysPresent = true,
                                                                                    Font = NekoPlayerApp.DefaultFont.With(weight: "Bold"),
                                                                                    Colour = overlayColourProvider.Content2,
                                                                                    Text = "0:00 / 0:00"
                                                                                },
                                                                            }
                                                                        }
                                                                    }
                                                                },
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    },
                                }
                            }
                        },
                        overlayFadeContainer = new OverlayFadeContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ClickAction = _ => hideOverlays(),
                            Child = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black,
                            }
                        },
                        menuOverlayCharacter = new ErpinSkin3Sprite
                        {
                            Margin = new MarginPadding
                            {
                                Right = 650,
                            },
                            Y = 100,
                            Scale = new Vector2(0.4f),
                            Origin = Anchor.BottomRight,
                            Anchor = Anchor.BottomRight,
                        },
                        audioEffectsOverlayCharacter = new YomiSprite
                        {
                            Margin = new MarginPadding
                            {
                                Right = 650,
                            },
                            Y = 100,
                            Scale = new Vector2(0.4f),
                            Origin = Anchor.BottomRight,
                            Anchor = Anchor.BottomRight,
                        },
                        settingsOverlayCharacter = new ButterSprite
                        {
                            Margin = new MarginPadding
                            {
                                Right = 1100,
                            },
                            Y = 100,
                            Scale = new Vector2(0.4f),
                            Origin = Anchor.BottomRight,
                            Anchor = Anchor.BottomRight,
                        },
                        loadVideoContainer = new BottomOverlayContainer
                        {
                            Size = new Vector2(0.7f, 1f),
                            Height = 200,
                            RelativeSizeAxes = Axes.X,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.LoadFromVideoId,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                loadBtn = new RoundedButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomCentre,
                                    Anchor = Anchor.BottomCentre,
                                    Text = NekoPlayerStrings.LoadVideo,
                                    Size = new Vector2(450, 40),
                                    Margin = new MarginPadding(16),
                                },
                                videoIdBox = new EnhancedFocusedTextBox
                                {
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre,
                                    Text = "",
                                    FontSize = 30,
                                    Margin = new MarginPadding(8),
                                    Size = new Vector2(.9f, 60),
                                    RelativeSizeAxes = Axes.X,
                                    OnEnterKeyPressed = () =>
                                    {
                                        if (string.IsNullOrEmpty(videoIdBox.Text))
                                            return;

                                        Task.Run(async () =>
                                        {
                                            try
                                            {
                                                Schedule(async () =>
                                                {
                                                    ClearPlaylistItems();
                                                    Schedule(async () =>
                                                    {
                                                        YoutubeExplode.Playlists.PlaylistId? playlistId = YoutubeExplode.Playlists.PlaylistId.TryParse(videoIdBox.Text);
                                                        YoutubeExplode.Videos.VideoId? videoId = YoutubeExplode.Videos.VideoId.TryParse(videoIdBox.Text);

                                                        if (videoId != null && !string.IsNullOrEmpty(videoId.Value))
                                                        {
                                                            SetVideoSource(videoIdBox.Text);
                                                        } else
                                                        {
                                                            SetPlaylist(videoIdBox.Text).FireAndForget();
                                                        }
                                                    });
                                                });
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error(ex, ex.GetDescription());
                                            }
                                        });
                                    }
                                },
                            }
                        },
                        settingsContainer = new SettingsContainer
                        {
                           OAuthSignInAction = () =>
                           {
                                if (!googleOAuth2.SignedIn.Value)
                                {
                                    Task.Run(() => googleOAuth2.SignIn());
                                }
                                else
                                {
                                    hideOverlays();
                                    showOverlayContainer(myChannelDialog);
                                }
                           },
                           CheckUpdateAction = () =>
                           {
                                if (game.UpdateManager is NoActionUpdateManager)
                                {
                                    host.OpenUrlExternally(@"https://github.com/BoomboxRapsody/NekoPlayer/releases");
                                }
                                else
                                {
                                    if (game.RestartRequired.Value != true)
                                        checkForUpdates().FireAndForget();
                                    else
                                        game.RestartAction.Invoke();
                                }
                           },
                           CloseOverlayAction = () =>
                           {
                               hideOverlayContainer(settingsContainer);
                           }
                        },
                        videoDescriptionContainer = new BottomOverlayContainer
                        {
                            Size = new Vector2(0.7f),
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding()
                                    {
                                        Horizontal = 6,
                                    },
                                    Child = new AdaptiveScrollContainer
                                    {
                                        Padding = new MarginPadding()
                                        {
                                            Top = 108,
                                            Bottom = 6,
                                        },
                                        RelativeSizeAxes = Axes.Both,
                                        CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS,
                                        Masking = true,
                                        ScrollbarVisible = false,
                                        Children = new Drawable[]
                                        {
                                            new Container
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS,
                                                Masking = true,
                                                Child = new Box
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = overlayColourProvider.Background4,
                                                    Alpha = 0.7f,
                                                },
                                            },
                                            new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS,
                                                Spacing = new Vector2(0, 8),
                                                Padding = new MarginPadding(12),
                                                Masking = true,
                                                Children = new Drawable[]
                                                {
                                                    videoInfoDetails = new AdaptiveSpriteText
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        Font = NekoPlayerApp.DefaultFont.With(weight: "Bold"),
                                                        Colour = overlayColourProvider.Content2,
                                                        AlwaysPresent = true,
                                                    },
                                                    videoDescription = new LinkFlowContainer(f => f.Font = NekoPlayerApp.DefaultFont)
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                        AlwaysPresent = true,
                                                        Colour = overlayColourProvider.Content2,
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = 128,
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding(6),
                                    Spacing = new Vector2(0, 5),
                                    Direction = FillDirection.Vertical,
                                    Children = new Drawable[]
                                    {
                                        videoMetadataDisplayDetails = new VideoMetadataDisplay
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Height = 60,
                                            Origin = Anchor.TopLeft,
                                            Anchor = Anchor.TopLeft,
                                            AlwaysPresent = true,
                                        },
                                        new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(2, 0),
                                            Children = new Drawable[]
                                            {
                                                likeButton = new RoundedButtonContainer
                                                {
                                                    AutoSizeAxes = Axes.X,
                                                    Height = 32,
                                                    CornerRadius = new CornersInfo(16, 16, NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f),
                                                    Masking = true,
                                                    AlwaysPresent = true,
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            Children = new Drawable[] {
                                                                likeButtonBackground = new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = overlayColourProvider.Background3,
                                                                    Alpha = 1f,
                                                                },
                                                                likeButtonBackgroundSelected = new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = overlayColourProvider.Content2,
                                                                    Alpha = 0f,
                                                                },
                                                            },
                                                        },
                                                        likeButtonForeground = new FillFlowContainer
                                                        {
                                                            AutoSizeAxes = Axes.X,
                                                            RelativeSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Horizontal,
                                                            Spacing = new Vector2(4, 0),
                                                            Padding = new MarginPadding(8),
                                                            Colour = overlayColourProvider.Content2,
                                                            Children = new Drawable[]
                                                            {
                                                                new SpriteIcon
                                                                {
                                                                    Width = 15,
                                                                    Height = 15,
                                                                    Icon = FontAwesome.Solid.ThumbsUp,
                                                                },
                                                                likeCount = new AdaptiveSpriteText
                                                                {
                                                                    Text = "0",
                                                                },
                                                            }
                                                        }
                                                    }
                                                },
                                                dislikeButton = new RoundedButtonContainer
                                                {
                                                    AutoSizeAxes = Axes.X,
                                                    Height = 32,
                                                    CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f, 16, 16),
                                                    Masking = true,
                                                    AlwaysPresent = true,
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            Children = new Drawable[] {
                                                                dislikeButtonBackground = new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = overlayColourProvider.Background3,
                                                                    Alpha = 1f,
                                                                },
                                                                dislikeButtonBackgroundSelected = new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = overlayColourProvider.Content2,
                                                                    Alpha = 0f,
                                                                },
                                                            },
                                                        },
                                                        dislikeButtonForeground = new FillFlowContainer
                                                        {
                                                            AutoSizeAxes = Axes.X,
                                                            RelativeSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Horizontal,
                                                            Spacing = new Vector2(4, 0),
                                                            Padding = new MarginPadding(8),
                                                            Colour = overlayColourProvider.Content2,
                                                            Children = new Drawable[]
                                                            {
                                                                new SpriteIcon
                                                                {
                                                                    Width = 15,
                                                                    Height = 15,
                                                                    Icon = FontAwesome.Solid.ThumbsDown,
                                                                },
                                                                dislikeCount = new AdaptiveSpriteText
                                                                {
                                                                    Text = "0",
                                                                },
                                                            }
                                                        }
                                                    }
                                                },
                                                commentOpenButtonDetails = new RoundedButtonContainer
                                                {
                                                    AutoSizeAxes = Axes.X,
                                                    Height = 32,
                                                    CornerRadius = 16,
                                                    Masking = true,
                                                    AlwaysPresent = true,
                                                    ClickAction = f =>
                                                    {
                                                        if (commentsDisabled)
                                                            return;

                                                        hideOverlays();
                                                        showOverlayContainer(commentsContainer);
                                                    },
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS / 1.5f,
                                                            Child = new Box
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Colour = overlayColourProvider.Background3,
                                                                Alpha = 1f,
                                                            },
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            AutoSizeAxes = Axes.X,
                                                            RelativeSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Horizontal,
                                                            Spacing = new Vector2(4, 0),
                                                            Padding = new MarginPadding(8),
                                                            Children = new Drawable[]
                                                            {
                                                                new SpriteIcon
                                                                {
                                                                    Width = 15,
                                                                    Height = 15,
                                                                    Icon = FontAwesome.Regular.CommentAlt,
                                                                    Colour = overlayColourProvider.Content2,
                                                                },
                                                                commentCount = new AdaptiveSpriteText
                                                                {
                                                                    Text = "0",
                                                                    Colour = overlayColourProvider.Content2,
                                                                },
                                                            }
                                                        }
                                                    }
                                                },
                                            }
                                        },
                                    }
                                },
                            }
                        },
                        commentsContainer = new BottomOverlayContainer
                        {
                            Size = new Vector2(0.7f),
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Top = 56,
                                                Bottom = 56 + 8,
                                            },
                                            Children = new Drawable[]
                                            {
                                                commentContainer = new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Spacing = new Vector2(0, 4),
                                                    AlwaysPresent = true,
                                                }
                                            }
                                        },
                                        commentsEmpty = new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    Direction = FillDirection.Vertical,
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Width = 300,
                                                    AutoSizeAxes = Axes.Y,
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            Anchor = Anchor.TopCentre,
                                                            Origin = Anchor.TopCentre,
                                                            Margin = new MarginPadding(10),
                                                            Size = new Vector2(50),
                                                            Child = ghostIcon = new GhostIcon
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                            },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new AdaptiveSpriteText
                                                        {
                                                            Anchor = Anchor.TopCentre,
                                                            Origin = Anchor.TopCentre,
                                                            Font = NekoPlayerApp.DefaultFont.With(size: 32, weight: "Bold"),
                                                            Text = NekoPlayerStrings.NoComments,
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new LinkFlowContainer
                                                        {
                                                            Alpha = 1,
                                                            AlwaysPresent = true,
                                                            Anchor = Anchor.TopCentre,
                                                            Origin = Anchor.TopCentre,
                                                            Padding = new MarginPadding { Top = 8 },
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            TextAnchor = Anchor.Centre,
                                                            Text = NekoPlayerStrings.NoCommentsDesc,
                                                            Colour = overlayColourProvider.Foreground2,
                                                        }
                                                    }
                                                },
                                                new Sprite
                                                {
                                                    Size = new Vector2(120),
                                                    Texture = textures.Get(@"speaki"),
                                                    Anchor = Anchor.BottomLeft,
                                                    Origin = Anchor.BottomLeft,
                                                },
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = 56 + 20,
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5.Opacity(0), overlayColourProvider.Background5),
                                    Height = 56 + 20,
                                },
                                commentsContainerTitle = new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.Comments("0"),
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                new OverlaySortTabControl<CommentsSortCriteria>
                                {
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    Current = CommentsSort,
                                    Margin = new MarginPadding()
                                    {
                                        Top = 15,
                                        Right = 20,
                                    },
                                },
                                commentTextBoxContainer = new Container
                                {
                                    Margin = new MarginPadding
                                    {
                                        Bottom = 12,
                                    },
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 48,
                                    },
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    RelativeSizeAxes = Axes.X,
                                    Size = new Vector2(0.55f, 1f),
                                    Height = 45,
                                    Children = new Drawable[]
                                    {
                                        commentTextBox = new EnhancedFocusedTextBoxWithProfileImage
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = "",
                                            FontSize = 20,
                                            Height = 45,
                                            PlaceholderText = NekoPlayerStrings.LoginToComment,
                                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                            {
                                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                                Colour = Color4.Black.Opacity(0.25f),
                                                Offset = new Vector2(0, 8),
                                                Radius = 64,
                                            },
                                            OnEnterKeyPressed = () =>
                                            {
                                                if (videoData == null)
                                                    return;

                                                if (!googleOAuth2.SignedIn.Value)
                                                    return;

                                                if (string.IsNullOrEmpty(commentTextBox.Text))
                                                    return;

                                                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.General, NekoPlayerStrings.CommentAdded, FontAwesome.Regular.Comment);
                                                api.SendComment(videoId, commentTextBox.Text);

                                                Scheduler.AddDelayed(() => updateComments(videoId), 2000);

                                                Schedule(() => onScreenDisplay.Display(toast));

                                                commentTextBox.Text = string.Empty;
                                            }
                                        },
                                        commentSendButton = new IconButton
                                        {
                                            Origin = Anchor.CentreRight,
                                            Anchor = Anchor.CentreRight,
                                            Margin = new MarginPadding
                                            {
                                                Right = 6,
                                            },
                                            Icon = FontAwesome.Solid.PaperPlane,
                                            Width = 35,
                                            Height = 35,
                                            AlwaysPresent = true,
                                            Enabled = { Value = true },
                                            BackgroundColour = overlayColourProvider.Background3,
                                        },
                                    },
                                },
                            }
                        },
                        searchContainer = new BottomOverlayContainer
                        {
                            Size = new Vector2(0.7f),
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Top = (56 * 2),
                                                Bottom = 6,
                                            },
                                            Children = new Drawable[]
                                            {
                                                searchResultContainer = new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Spacing = new Vector2(0, 4),
                                                    AlwaysPresent = true,
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = 56 + 20,
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5.Opacity(0), overlayColourProvider.Background5),
                                    Height = 56 + 20,
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.Search,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                new OverlaySortTabControl<SearchSortCriteria>
                                {
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    Current = SearchSort,
                                    Margin = new MarginPadding()
                                    {
                                        Top = 15,
                                        Right = 20,
                                    },
                                },
                                searchTextBoxContainer = new Container
                                {
                                    Margin = new MarginPadding
                                    {
                                        Bottom = 12,
                                    },
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 48,
                                    },
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    RelativeSizeAxes = Axes.X,
                                    Size = new Vector2(0.55f, 1f),
                                    Height = 45,
                                    Children = new Drawable[]
                                    {
                                        searchTextBox = new EnhancedFocusedTextBox
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = "",
                                            PlaceholderText = NekoPlayerStrings.SearchPlaceholder,
                                            FontSize = 20,
                                            Height = 45,
                                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                            {
                                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                                Colour = Color4.Black.Opacity(0.25f),
                                                Offset = new Vector2(0, 8),
                                                Radius = 64,
                                            },
                                            OnEnterKeyPressed = () =>
                                            {
                                                if (string.IsNullOrEmpty(searchTextBox.Text))
                                                    return;

                                                Schedule(() => Search());
                                            }
                                        },
                                        searchButton = new IconButton
                                        {
                                            Origin = Anchor.CentreRight,
                                            Anchor = Anchor.CentreRight,
                                            Margin = new MarginPadding
                                            {
                                                Right = 6,
                                            },
                                            Icon = FontAwesome.Solid.Search,
                                            Width = 35,
                                            Height = 35,
                                            AlwaysPresent = true,
                                            Enabled = { Value = true },
                                        },
                                    },
                                },
                            }
                        },
                        reportAbuseOverlay = new BottomOverlayContainer
                        {
                            Size = new Vector2(0.7f, 1f),
                            Height = 300,
                            RelativeSizeAxes = Axes.X,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[]
                                    {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Bottom = 72,
                                                Top = 56,
                                            },
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                    Children = new Drawable[]
                                                    {
                                                        new TruncatingSpriteText
                                                        {
                                                            Text = NekoPlayerStrings.WhatsGoingOn,
                                                            Font = NekoPlayerApp.DefaultFont.With(size: 27, weight: "Bold"),
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new AdaptiveTextFlowContainer(f => f.Font = NekoPlayerApp.DefaultFont.With(size: 17, weight: "Regular"))
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Text = NekoPlayerStrings.ReportDesc,
                                                            Colour = overlayColourProvider.Background1,
                                                        },
                                                        reportReason = new ReportDropdown
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Caption = NekoPlayerStrings.ReportReason,
                                                        },
                                                        reportSubReason = new ReportDropdown
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Caption = NekoPlayerStrings.ReportSubReason,
                                                        },
                                                        reportComment = new FormTextBox
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = NekoPlayerStrings.Description,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                        reportButton = new SettingsButtonV2
                                        {
                                            Height = 40,
                                            Padding = new MarginPadding
                                            {
                                                Horizontal = 16,
                                            },
                                            Margin = new MarginPadding
                                            {
                                                Bottom = 16,
                                            },
                                            Text = NekoPlayerStrings.Submit,
                                            BackgroundColour = colours.Yellow,
                                            Origin = Anchor.BottomCentre,
                                            Anchor = Anchor.BottomCentre,
                                        },
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = (56 + 20),
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.Report,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                            }
                        },
                        playlistOverlay = new SideOverlayContainer
                        {
                            Name = "Playlist Overlay",
                            Size = new Vector2(1f, 1f),
                            Width = 400,
                            RelativeSizeAxes = Axes.Y,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, NekoPlayerApp.UI_CORNER_RADIUS, 0, 0),
                            Masking = true,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Bottom = 16,
                                                Top = 56,
                                            },
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                    Children = new Drawable[]
                                                    {
                                                        playlistName = new AdaptiveTextFlowContainer(f =>
                                                        {
                                                            f.Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "Bold");
                                                            f.Colour = overlayColourProvider.Content2;
                                                        })
                                                        {
                                                            TextAnchor = Anchor.Centre,
                                                            Origin = Anchor.TopCentre,
                                                            Anchor = Anchor.TopCentre,
                                                            Text = NekoPlayerStrings.PlaylistNotLoaded,
                                                             RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                        },
                                                        playlistAuthor = new LinkFlowContainer(f =>
                                                        {
                                                            f.Font = NekoPlayerApp.DefaultFont.With(size: 16, weight: "SemiBold");
                                                            f.Colour = overlayColourProvider.Background1;
                                                        })
                                                        {
                                                            TextAnchor = Anchor.Centre,
                                                            Origin = Anchor.TopCentre,
                                                            Anchor = Anchor.TopCentre,
                                                            Text = NekoPlayerStrings.PlaylistNotLoadedDesc,
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                        },
                                                        playlistItemsView = new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Vertical,
                                                            Spacing = new Vector2(4),
                                                            Children = Array.Empty<Drawable>()
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = (56 + 20),
                                },
                                new IconButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    Size = new Vector2(35, 35),
                                    IconScale = new Vector2(0.8f),
                                    Margin = new MarginPadding(14),
                                    Icon = FontAwesome.Solid.Times,
                                    BackgroundColour = overlayColourProvider.Background4,
                                    Action = () =>
                                    {
                                        hideOverlayContainer(playlistOverlay);
                                    }
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    Text = NekoPlayerStrings.Playlists,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                            }
                        },
                        myPlaylistsOverlay = new SideOverlayContainer
                        {
                            Size = new Vector2(1f, 1f),
                            Width = 400,
                            RelativeSizeAxes = Axes.Y,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, NekoPlayerApp.UI_CORNER_RADIUS, 0, 0),
                            Masking = true,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Bottom = 16,
                                                Top = 56,
                                            },
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                    Children = new Drawable[]
                                                    {
                                                        myPlaylistItemsView = new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Vertical,
                                                            Spacing = new Vector2(4),
                                                            Children = Array.Empty<Drawable>()
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = (56 + 20),
                                },
                                new IconButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    Margin = new MarginPadding(14),
                                    Size = new Vector2(35, 35),
                                    IconScale = new Vector2(0.8f),
                                    Icon = FontAwesome.Solid.Times,
                                    BackgroundColour = overlayColourProvider.Background4,
                                    Action = () =>
                                    {
                                        hideOverlayContainer(myPlaylistsOverlay);
                                    }
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    Text = NekoPlayerStrings.MyPlaylists,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                            }
                        },
                        loadPlaylistContainer = new BottomOverlayContainer
                        {
                            Size = new Vector2(0.7f, 1f),
                            Height = 200,
                            RelativeSizeAxes = Axes.X,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.LoadFromPlaylistId,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                loadPlaylistBtn = new AdaptiveButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomRight,
                                    Anchor = Anchor.BottomRight,
                                    Text = NekoPlayerStrings.LoadPlaylist,
                                    Size = new Vector2(200, 60),
                                    Margin = new MarginPadding(8),
                                },
                                playlistIdBox = new EnhancedFocusedTextBox
                                {
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre,
                                    Text = "",
                                    FontSize = 30,
                                    Size = new Vector2(0.9f, 60),
                                    RelativeSizeAxes = Axes.X,
                                    Margin = new MarginPadding(8),
                                    OnEnterKeyPressed = () =>
                                    {
                                        if (string.IsNullOrEmpty(playlistIdBox.Text))
                                            return;

                                        SetPlaylist(playlistIdBox.Text).FireAndForget();
                                    }
                                },
                            }
                        },
                        audioEffectsOverlay = new SideOverlayContainer
                        {
                            Name = "Audio Effects Overlay",
                            Size = new Vector2(1f, 1f),
                            Width = 400,
                            RelativeSizeAxes = Axes.Y,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, NekoPlayerApp.UI_CORNER_RADIUS, 0, 0),
                            Masking = true,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Bottom = 16,
                                                Top = 56,
                                            },
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                    Children = new Drawable[]
                                                    {
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = NekoPlayerStrings.ReverbEffect,
                                                            Current = reverbEnabled,
                                                            Hotkey = new Hotkey(GlobalAction.ToggleReverbEffect),
                                                        }),
                                                        reverbSettings = new FillFlowContainer
                                                        {
                                                            Direction = FillDirection.Vertical,
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Masking = true,
                                                            Spacing = new Vector2(0, 4),
                                                            Children = new Drawable[]
                                                            {
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.WetMix,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ReverbWetMix),
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.StereoWidth,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ReverbStereoWidth),
                                                                    DisplayAsPercentage = true,
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.HighFreqDamp,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ReverbDamp),
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.RoomSize,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ReverbRoomSize),
                                                                    DisplayAsPercentage = true,
                                                                }),
                                                            }
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = NekoPlayerStrings.RotateParameters_Enabled,
                                                            Current = rotateEnabled,
                                                            Hotkey = new Hotkey(GlobalAction.ToggleRotateEffect),
                                                        }),
                                                        rotateSettings = new FillFlowContainer
                                                        {
                                                            Direction = FillDirection.Vertical,
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Masking = true,
                                                            Spacing = new Vector2(0, 4),
                                                            Children = new Drawable[]
                                                            {
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.RotateParameters_fRate,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.RotateRate),
                                                                    DisplayAsPercentage = true,
                                                                }),
                                                            }
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = NekoPlayerStrings.EchoEffect,
                                                            Current = echoEnabled,
                                                            Hotkey = new Hotkey(GlobalAction.ToggleEchoEffect),
                                                        }),
                                                        echoSettings = new FillFlowContainer
                                                        {
                                                            Direction = FillDirection.Vertical,
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Masking = true,
                                                            Spacing = new Vector2(0, 4),
                                                            Children = new Drawable[]
                                                            {
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.DryMix,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.EchoDryMix),
                                                                    LabelFormat = f => $"{f - 2}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.EchoWetMix,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.EchoWetMix),
                                                                    LabelFormat = f => $"{f - 2}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.EchoFeedback,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.EchoFeedback),
                                                                    LabelFormat = f => $"{f - 1}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.EchoDelay,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.EchoDelay),
                                                                }),
                                                            }
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = NekoPlayerStrings.DistortionEffect,
                                                            Current = distortionEnabled,
                                                            Hotkey = new Hotkey(GlobalAction.ToggleDistortionEffect),
                                                        }),
                                                        distortionSettings = new FillFlowContainer
                                                        {
                                                            Direction = FillDirection.Vertical,
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Masking = true,
                                                            Spacing = new Vector2(0, 4),
                                                            Children = new Drawable[]
                                                            {
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.DistortionVolume,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.DistortionVolume),
                                                                    DisplayAsPercentage = true,
                                                                }),
                                                            }
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = NekoPlayerStrings.KaraokeMode,
                                                            Current = karaokeEnabled,
                                                            Hotkey = new Hotkey(GlobalAction.ToggleKaraokeEffect),
                                                        }),
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = NekoPlayerStrings.ChorusEffect,
                                                            Current = chorusEnabled,
                                                            Hotkey = new Hotkey(GlobalAction.ToggleChorusEffect),
                                                        }),
                                                        chorusSettings = new FillFlowContainer
                                                        {
                                                            Direction = FillDirection.Vertical,
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Masking = true,
                                                            Spacing = new Vector2(0, 4),
                                                            Children = new Drawable[]
                                                            {
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.DryMix,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ChorusDryMix),
                                                                    LabelFormat = f => $"{f - 2}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.EchoWetMix,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ChorusWetMix),
                                                                    LabelFormat = f => $"{f - 2}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.EchoFeedback,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ChorusFeedback),
                                                                    LabelFormat = f => $"{f - 1}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.ChorusMinSweep,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ChorusMinSweep),
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.ChorusMaxSweep,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ChorusMaxSweep),
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = NekoPlayerStrings.ChorusRate,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ChorusRate),
                                                                }),
                                                            }
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = (56 + 20),
                                },
                                new IconButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    Margin = new MarginPadding(14),
                                    Size = new Vector2(35, 35),
                                    IconScale = new Vector2(0.8f),
                                    Icon = FontAwesome.Solid.Times,
                                    BackgroundColour = overlayColourProvider.Background4,
                                    Action = () =>
                                    {
                                        hideOverlayContainer(audioEffectsOverlay);
                                    }
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    Text = NekoPlayerStrings.AudioEffects,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                            }
                        },
                        unsubscribeDialog = new BottomOverlayContainer
                        {
                            Width = 450,
                            Height = 200,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                youtubeChannelMetadataDisplay = new YouTubeChannelMetadataDisplay
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Margin = new MarginPadding(8),
                                    Size = new Vector2(0.965f, 1f),
                                    Height = 60,
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    AlwaysPresent = true,
                                },
                                new AdaptiveTextFlowContainer(f => f.Font = NekoPlayerApp.DefaultFont.With(size: 20))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre,
                                    TextAnchor = Anchor.Centre,
                                    AlwaysPresent = true,
                                    Text = NekoPlayerStrings.UnsubscribeDesc,
                                    Colour = overlayColourProvider.Content2,
                                },
                                declineButton = new RoundedAltButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomLeft,
                                    Anchor = Anchor.BottomLeft,
                                    Text = NekoPlayerStrings.Cancel,
                                    Size = new Vector2(200, 40),
                                    Margin = new MarginPadding(8),
                                },
                                acceptButton = new RoundedButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomRight,
                                    Anchor = Anchor.BottomRight,
                                    Text = NekoPlayerStrings.Yes,
                                    Size = new Vector2(200, 40),
                                    BackgroundColour = colours.RedDark,
                                    Margin = new MarginPadding(8),
                                },
                            }
                        },
                        videoSaveLocationOverlay = new BottomOverlayContainer
                        {
                            Size = new Vector2(1f, 1f),
                            Width = 450,
                            Height = 210,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Top = 56,
                                            },
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                    Children = new Drawable[]
                                                    {
                                                        myPlaylistsDropdown = new PlaylistDropdown
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = NekoPlayerStrings.Playlists,
                                                        },
                                                        new RoundedAltButton
                                                        {
                                                            Enabled = { Value = true },
                                                            Text = NekoPlayerStrings.AddNewPlaylist,
                                                            RelativeSizeAxes = Axes.X,
                                                            Size = new Vector2(1, 40),
                                                            Action = () =>
                                                            {
                                                                hideOverlays();
                                                                showOverlayContainer(addPlaylistOverlay);
                                                            }
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Horizontal,
                                                            Children = new Drawable[]
                                                            {
                                                                new RoundedAltButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = NekoPlayerStrings.Cancel,
                                                                    Size = new Vector2(200, 40),
                                                                    Margin = new MarginPadding(4),
                                                                    Action = () =>
                                                                    {
                                                                        hideOverlayContainer(videoSaveLocationOverlay);
                                                                    }
                                                                },
                                                                new RoundedButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = NekoPlayerStrings.SaveOrRemove,
                                                                    Size = new Vector2(200, 40),
                                                                    Margin = new MarginPadding(4),
                                                                    Action = async () =>
                                                                    {
                                                                        if (videoId != null)
                                                                        {
                                                                            bool trickcalChibiGo = await api.IsVideoExistsOnPlaylist(myPlaylistsDropdown.Current.Value.Id, videoData.Id);
                                                                            hideOverlays();

                                                                            if (!trickcalChibiGo)
                                                                                saveVideoToPlaylist(videoData.Id);
                                                                            else
                                                                                removeVideoFromPlaylist(videoData.Id);
                                                                        }
                                                                    }
                                                                },
                                                            },
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = (56 + 20),
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.SaveLocation,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                            }
                        },
                        editPlaylistOverlay = new BottomOverlayContainer
                        {
                            Size = new Vector2(1f, 1f),
                            Width = 450,
                            Height = 220,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Bottom = 16,
                                                Top = 56,
                                            },
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                    Children = new Drawable[]
                                                    {
                                                        editPlaylistTitleBox = new FormTextBox
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = NekoPlayerStrings.Title,
                                                            PlaceholderText = NekoPlayerStrings.TitlePlaceholder,
                                                        },
                                                        editPlaylistPrivacyStatusDropdown = new FormEnumDropdown<PrivacyStatus>
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = NekoPlayerStrings.PrivacyStatus,
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Horizontal,
                                                            Children = new Drawable[]
                                                            {
                                                                new RoundedAltButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = NekoPlayerStrings.Cancel,
                                                                    Size = new Vector2(200, 40),
                                                                    Margin = new MarginPadding(4),
                                                                    Action = () =>
                                                                    {
                                                                        hideOverlayContainer(editPlaylistOverlay);
                                                                    }
                                                                },
                                                                updatePlaylistButton = new RoundedButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = NekoPlayerStrings.Apply,
                                                                    Size = new Vector2(200, 40),
                                                                    Margin = new MarginPadding(4),
                                                                },
                                                            },
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = (56 + 20),
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.EditPlaylist,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                            }
                        },
                        addPlaylistOverlay = new BottomOverlayContainer
                        {
                            Size = new Vector2(1f, 1f),
                            Width = 450,
                            Height = 220,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Top = 56,
                                            },
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                    Children = new Drawable[]
                                                    {
                                                        playlistTitleBox = new FormTextBox
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = NekoPlayerStrings.Title,
                                                            PlaceholderText = NekoPlayerStrings.TitlePlaceholder,
                                                        },
                                                        playlistPrivacyStatusDropdown = new FormEnumDropdown<PrivacyStatus>
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = NekoPlayerStrings.PrivacyStatus,
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Horizontal,
                                                            Children = new Drawable[]
                                                            {
                                                                new RoundedAltButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = NekoPlayerStrings.Cancel,
                                                                    Size = new Vector2(200, 40),
                                                                    Margin = new MarginPadding(4),
                                                                    Action = () =>
                                                                    {
                                                                        hideOverlayContainer(addPlaylistOverlay);
                                                                    }
                                                                },
                                                                new RoundedButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = NekoPlayerStrings.Create,
                                                                    Size = new Vector2(200, 40),
                                                                    Margin = new MarginPadding(4),
                                                                    Action = async () =>
                                                                    {
                                                                        hideOverlays();
                                                                        await api.AddPlaylist(playlistTitleBox.Current.Value, playlistPrivacyStatusDropdown.Current.Value);
                                                                    }
                                                                },
                                                            },
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = (56 + 20),
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.AddNewPlaylist,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                            }
                        },
                        menuOverlay = new SideOverlayContainer
                        {
                            Name = "Menu Overlay",
                            Size = new Vector2(1f, 1f),
                            Width = 400,
                            RelativeSizeAxes = Axes.Y,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, NekoPlayerApp.UI_CORNER_RADIUS, 0, 0),
                            Masking = true,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Padding = new MarginPadding
                                            {
                                                Bottom = 16,
                                                Top = 56,
                                            },
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(2),
                                                    Children = new Drawable[]
                                                    {
                                                        loadBtnOverlayShow = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Regular.FolderOpen,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.LoadVideo,
                                                            Hotkey = new Hotkey(GlobalAction.OpenLoadVideo),
                                                            RoundCorner = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 8, NekoPlayerApp.UI_CORNER_RADIUS, 8),
                                                        },
                                                        settingsOverlayShowBtn = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.Cog,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.Settings,
                                                            Hotkey = new Hotkey(GlobalAction.OpenSettings),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                        },
                                                        commentOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = false },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Regular.CommentAlt,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.CommentsWithoutCount,
                                                            Hotkey = new Hotkey(GlobalAction.OpenComments),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                        },
                                                        searchOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.Search,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.Search,
                                                            Hotkey = new Hotkey(GlobalAction.OpenSearch),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                        },
                                                        reportOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = false },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.Flag,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.Report,
                                                            Hotkey = new Hotkey(GlobalAction.ReportAbuse),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                        },
                                                        playlistOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.List,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.Playlists,
                                                            Hotkey = new Hotkey(GlobalAction.OpenPlaylist),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                        },
                                                        myPlaylistsOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = false },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.List,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.MyPlaylists,
                                                            Hotkey = new Hotkey(GlobalAction.OpenMyPlaylists),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                            Action = () =>
                                                            {
                                                                hideOverlays();
                                                                showOverlayContainer(myPlaylistsOverlay);
                                                            }
                                                        },
                                                        audioEffectsOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.VolumeUp,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.AudioEffects,
                                                            Hotkey = new Hotkey(GlobalAction.OpenAudioEffects),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                        },
                                                        saveVideoOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = false },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Regular.Bookmark,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.Save,
                                                            Hotkey = new Hotkey(GlobalAction.SaveVideoToPlaylist),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                        },
                                                        newPlaylistOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = false },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.Bookmark,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.AddNewPlaylist,
                                                            Hotkey = new Hotkey(GlobalAction.AddPlaylistKey),
                                                            RoundCorner = new CornersInfo(8, 8, 8, 8),
                                                            Action = () =>
                                                            {
                                                                hideOverlays();
                                                                showOverlayContainer(addPlaylistOverlay);
                                                            },
                                                        },
                                                        new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.SignOutAlt,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.Exit,
                                                            Hotkey = new Hotkey(GlobalAction.QuitApp),
                                                            RoundCorner = new CornersInfo(8, NekoPlayerApp.UI_CORNER_RADIUS, 8, NekoPlayerApp.UI_CORNER_RADIUS),
                                                            Action = () =>
                                                            {
                                                                overlayHideSample.Volume.Value = 0;
                                                                hideOverlays();
                                                                game.AttemptExit();
                                                            },
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                new Box
                                {
                                    Name = "masking of overlay",
                                    RelativeSizeAxes = Axes.X,
                                    Colour = ColourInfo.GradientVertical(overlayColourProvider.Background5, overlayColourProvider.Background5.Opacity(0)),
                                    Height = (56 + 20),
                                },
                                new IconButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    Size = new Vector2(35, 35),
                                    IconScale = new Vector2(0.8f),
                                    Margin = new MarginPadding(14),
                                    Icon = FontAwesome.Solid.Times,
                                    BackgroundColour = overlayColourProvider.Background4,
                                    Action = () =>
                                    {
                                        hideOverlayContainer(menuOverlay);
                                    }
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    Text = NekoPlayerStrings.Menu,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                            }
                        },
                        exitOptions = new SideOverlayContainer
                        {
                            Size = new Vector2(1f, 1f),
                            Width = 400,
                            RelativeSizeAxes = Axes.Y,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, NekoPlayerApp.UI_CORNER_RADIUS, 0, 0),
                            Masking = true,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    Text = NekoPlayerStrings.ExitOptions,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                        Bottom = 16,
                                        Top = 56,
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                    Children = new Drawable[]
                                                    {
                                                        new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Solid.SignOutAlt,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = NekoPlayerStrings.Exit,
                                                            Action = () =>
                                                            {
                                                                hideOverlays();
                                                                game.AttemptExit();
                                                            },
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        myChannelDialog = new BottomOverlayContainer
                        {
                            Width = 450,
                            Height = 185,
                            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS, 0, NekoPlayerApp.UI_CORNER_RADIUS, 0),
                            Masking = true,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Text = NekoPlayerStrings.GoogleAccount,
                                    Margin = new MarginPadding(16),
                                    Font = NekoPlayerApp.DefaultFont.With(size: 30, weight: "ExtraBold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                youtubeChannelMetadataDisplay2 = new YouTubeChannelMetadataDisplay
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Margin = new MarginPadding(8),
                                    Size = new Vector2(0.95f, 1f),
                                    Height = 60,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre,
                                    AlwaysPresent = true,
                                },
                                logoutButton = new RoundedAltButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomLeft,
                                    Anchor = Anchor.BottomLeft,
                                    Text = NekoPlayerStrings.Logout,
                                    Size = new Vector2(200, 40),
                                    Margin = new MarginPadding(16),
                                    Action = () =>
                                    {
                                        if (googleOAuth2.SignedIn.Value)
                                        {
                                            hideOverlays();
                                            Task.Run(() => googleOAuth2.SignOut());
                                        }
                                    },
                                },
                                viewChannelButton = new RoundedButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomRight,
                                    Anchor = Anchor.BottomRight,
                                    Text = NekoPlayerStrings.ViewChannel,
                                    Size = new Vector2(200, 40),
                                    Margin = new MarginPadding(16),
                                },
                            }
                        },
                    }
                }
            };
            #endregion

            thumbnailContainer.BlurTo(Vector2.Divide(new Vector2(10, 10), 1));

            RegisterOverlayContainer(loadVideoContainer);
            overlayFadeContainer.Hide();
            RegisterOverlayContainer(settingsContainer);
            RegisterOverlayContainer(videoDescriptionContainer);
            RegisterOverlayContainer(commentsContainer);
            RegisterOverlayContainer(searchContainer);
            RegisterOverlayContainer(reportAbuseOverlay);
            RegisterOverlayContainer(playlistOverlay);
            RegisterOverlayContainer(loadPlaylistContainer);
            RegisterOverlayContainer(audioEffectsOverlay);
            RegisterOverlayContainer(unsubscribeDialog);
            RegisterOverlayContainer(videoSaveLocationOverlay);
            RegisterOverlayContainer(addPlaylistOverlay);
            RegisterOverlayContainer(menuOverlay);
            RegisterOverlayContainer(myChannelDialog);
            RegisterOverlayContainer(myPlaylistsOverlay);
            RegisterOverlayContainer(exitOptions);
            RegisterOverlayContainer(editPlaylistOverlay);

            ReleaseStream.BindValueChanged(async _ => await checkForUpdates());

            captionEnabled.Disabled = true;

            menuOverlayShow.ClickAction = _ =>
            {
                showOverlayContainer(menuOverlay);
            };

            videoMetadataDisplayAlignment.BindValueChanged(v =>
            {
                SetVideoMetadataDisplayAlignment(v.NewValue);
            }, true);

            signedIn.BindValueChanged(loginBool =>
            {
                if (loginBool.NewValue)
                {
                    GetReportReasons();

                    localeBindable.BindValueChanged(locale =>
                    {
                        Task.Run(async () =>
                        {
                            GetReportReasons();
                        });
                    });

                    if (videoId != null)
                        Task.Run(async () => updateVideoMetadata(videoId));

                    #region playlists

                    Schedule(() => myPlaylistsOpenButton.Enabled.Value = true);

                    fetchMyPlaylists();
                    #endregion

                    Schedule(() => commentSendButton.Enabled.Value = true);
                    Schedule(() => newPlaylistOpenButton.Enabled.Value = true);
                    Channel wth = api.GetMineChannel();

                    //login.Text = NekoPlayerStrings.SignedIn(api.GetLocalizedChannelTitle(wth, true));
                    settingsContainer.UpdateLoginStateText(NekoPlayerStrings.SignedIn(api.GetLocalizedChannelTitle(wth, true)));

                    youtubeChannelMetadataDisplay2.UpdateUser(wth);

                    viewChannelButton.Action = () =>
                    {
                        if (googleOAuth2.SignedIn.Value)
                        {
                            hideOverlays();

                            if (wth != null)
                                app.Host.OpenUrlExternally($"https://www.youtube.com/channel/{wth.Id}");
                        }
                    };

                    if (api.TryToGetMineChannel() != null)
                    {
                        commentTextBox.PlaceholderText = NekoPlayerStrings.CommentWith;
                        commentTextBox.RefreshChannelProfile(api.GetMineChannel());
                    }

                    Schedule(() => settingsContainer.UpdateLoginState());
                }
                else
                {
                    if (videoId != null)
                        Task.Run(async () => updateVideoMetadata(videoId));

                    Schedule(() => commentSendButton.Enabled.Value = false);
                    settingsContainer.UpdateLoginStateText(NekoPlayerStrings.SignedOut);
                    Schedule(() => saveVideoOpenButton.Enabled.Value = false);
                    Schedule(() => reportOpenButton.Enabled.Value = false);
                    Schedule(() => newPlaylistOpenButton.Enabled.Value = false);
                    Schedule(() => myPlaylistsOpenButton.Enabled.Value = false);
                    Schedule(() => quickLikeButton.Enabled.Value = false);
                    Schedule(() => quickDislikeButton.Enabled.Value = false);

                    foreach (var item in myPlaylistItemsView.Children)
                    {
                        Schedule(() => item.Expire());
                    }

                    commentTextBox.PlaceholderText = NekoPlayerStrings.LoginToComment;
                    Schedule(() => settingsContainer.UpdateLoginState());
                }
            }, true);
            /*
            if (googleOAuth2.SignedIn.Value)
            {
                login.Text = "Signed in";
            }
            else
            {
                login.Text = "Not logged in";
            }
            */

            reportReason.Current.BindValueChanged(value =>
            {
                try
                {
                    if (value.NewValue.ContainsSecondaryReasons == true)
                    {
                        reportSubReason.Show();
                        reportSubReason.Items = value.NewValue.SecondaryReasons;
                        reportSubReason.Current.Value = value.NewValue.SecondaryReasons[0];
                    }
                    else
                    {
                        reportSubReason.Hide();
                    }
                }
                catch
                {
                    reportSubReason.Hide();
                }
            });

            commentsDisabled = true;

            prevVideoButton.SetCornerRadius(new CornersInfo(15, 15, NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f));
            nextVideoButton.SetCornerRadius(new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f, 15, 15));

            repeatButton.SetCornerRadius(new CornersInfo(15, 15, NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f));
            pinButton.SetCornerRadius(new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f, 15, 15));
            playPause.SetEnabledValue2(true);
            captionButton.SetEnabledValue2(true);
            videoSettingsButton.SetEnabledValue2(true);
            playlistButton.SetEnabledValue2(true);
            quickCommentOpenButton.SetEnabledValue2(true);

            quickLikeButton.SetCornerRadius(new CornersInfo(15, 15, NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f));
            quickDislikeButton.SetCornerRadius(new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f, 15, 15));

            searchButton.BackgroundColour = commentSendButton.BackgroundColour = overlayColourProvider.Background3;

            oauth_note.Value = new SettingsNote.Data(NekoPlayerStrings.OAuthNote, SettingsNote.Type.Informational);

            playlistName.Text = NekoPlayerStrings.PlaylistNotLoaded;
            playlistAuthor.Text = NekoPlayerStrings.PlaylistNotLoadedDesc;

            settingsContainer.VideoQualitySettings.Current.BindValueChanged(quality =>
            {
                if (currentVideoSource != null && isVideoLoading == false)
                {
                    Task.Run(async () =>
                    {
                        Schedule(async () =>
                        {
                            SetVideoSource(videoId, true, LoadType.VideoOnly);
                        });
                    });
                }
            });

            audioQuality.BindValueChanged(quality =>
            {
                if (currentVideoSource != null && isVideoLoading == false)
                {
                    Task.Run(async () =>
                    {
                        Schedule(async () =>
                        {
                            SetVideoSource(videoId, true, LoadType.AudioOnly);
                        });
                    });
                }
            });

            videoVolume.BindValueChanged(volume =>
            {
                this.TransformBindableTo(volumeTextRolling, volume.NewValue, 400, Easing.OutQuint);
                if (volume.NewValue > 0.5)
                {
                    volumeIcon.Icon = FontAwesome.Solid.VolumeUp;
                }
                else if (volume.NewValue >= 0.01)
                {
                    volumeIcon.Icon = FontAwesome.Solid.VolumeDown;
                }
                else
                {
                    volumeIcon.Icon = FontAwesome.Solid.VolumeMute;
                }
            }, true);

            captionEnabled.BindValueChanged(enabled =>
            {
                captionButton.SetEnabledValue2(!enabled.NewValue);
                captionButton.IconObject.FadeColour(enabled.NewValue ? bgColor : accentColor, 250, Easing.OutQuint);
                captionButton.Icon = enabled.NewValue ? FontAwesome.Solid.ClosedCaptioning : FontAwesome.Regular.ClosedCaptioning;
            }, true);

            alwaysUseOriginalAudio.BindValueChanged(enabled =>
            {
                if (currentVideoSource != null && isVideoLoading == false)
                {
                    Task.Run(async () =>
                    {
                        Schedule(async () =>
                        {
                            SetVideoSource(videoId, true, LoadType.AudioOnly);
                        });
                    });
                }
            }, true);

            adjustPitch.BindValueChanged(value =>
            {
                currentVideoSource?.UpdatePreservePitch(value.NewValue);
            });

            audioLanguage.BindValueChanged(_ =>
            {
                if (currentVideoSource != null && isVideoLoading == false)
                {
                    Task.Run(async () =>
                    {
                        Schedule(async () =>
                        {
                            SetVideoSource(videoId, true, LoadType.AudioOnly);
                        });
                    });
                }
            });

            settingsContainer.CaptionLangDropdown.Current.BindValueChanged(lang =>
            {
                if (currentVideoSource != null)
                {
                    if (captionEnabled.Value)
                    {
                        Task.Run(async () =>
                        {
                            var trackManifest = await game.YouTubeClient.Videos.ClosedCaptions.GetManifestAsync(videoUrl);

                            var trackInfo = trackManifest.Tracks.Where(track => track.Language.Name == lang.NewValue.Name).First();

                            ClosedCaptionTrack captionTrack = null;

                            if (trackInfo != null)
                            {
                                Schedule(() =>
                                {
                                    ToastBase toast = new ToastWithIcon(NekoPlayerStrings.CaptionLanguage, lang.NewValue.Name, FontAwesome.Solid.ClosedCaptioning);

                                    onScreenDisplay.Display(toast);
                                });

                                captionTrack = await game.YouTubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);
                            }

                            currentVideoSource.UpdateCaptionTrack(captionTrack);
                        });
                    }
                    else
                    {
                        currentVideoSource.UpdateCaptionTrack(null);
                    }
                }
            });

            captionEnabled.BindValueChanged(enabled =>
            {
                if (currentVideoSource != null)
                {
                    if (captionEnabled.Value)
                    {
                        Task.Run(async () =>
                        {
                            var trackManifest = await game.YouTubeClient.Videos.ClosedCaptions.GetManifestAsync(videoUrl);

                            string preferedLang = string.Empty;

                            if (settingsContainer.CaptionLangDropdown.Current.Value != null)
                            {
                                preferedLang = settingsContainer.CaptionLangDropdown.Current.Value.Hl.ToString();
                            }
                            else
                            {
                                preferedLang = CultureInfo.CurrentCulture.Name;
                            }

                            settingsContainer.CaptionLangDropdown.Current.Value = settingsContainer.CaptionLangDropdown.Items.Where(lang => lang.Hl.Contains(preferedLang)).First();

                            var trackInfo = trackManifest.Tracks.Where(track => track.Language.Code.Contains(preferedLang)).First();

                            ClosedCaptionTrack captionTrack = null;

                            if (enabled.NewValue)
                            {
                                Schedule(() =>
                                {
                                    try
                                    {
                                        ToastBase toast = new ToastWithIcon(NekoPlayerStrings.CaptionLanguage, settingsContainer.CaptionLangDropdown.Current.Value.Name, FontAwesome.Solid.ClosedCaptioning);
                                        onScreenDisplay.Display(toast);
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Error(e, e.GetDescription());
                                    }
                                });

                                captionTrack = await game.YouTubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);
                            }

                            currentVideoSource.UpdateCaptionTrack(captionTrack);
                        });
                    }
                    else
                    {
                        currentVideoSource.UpdateCaptionTrack(null);
                    }
                }
            });

            idleTracker.IsIdle.BindValueChanged(idle =>
            {
                if (idle.NewValue == true)
                {
                    if (videoPlaying.Value)
                    {
                        hideControls();
                    }
                }
                else
                {
                    showControls();
                }
            }, true);

            playbackSpeed.BindValueChanged(speed =>
            {
                this.TransformBindableTo(speedTextRolling, speed.NewValue, 400, Easing.OutQuint);
            }, true);

            speedTextRolling.BindValueChanged(speed =>
            {
                double intValue = speed.NewValue;
                speedText.Text = $@"{intValue:0.##}x";
            }, true);

            volumeTextRolling.BindValueChanged(volume =>
            {
                int intValue = (int)Math.Round(volume.NewValue * 100);
                volumeText.Text = $"{intValue}%";
            }, true);

            reverbEnabled.BindValueChanged(_ =>
            {
                reverbSettings.ClearTransforms();
                reverbSettings.AutoSizeDuration = 400;
                reverbSettings.AutoSizeEasing = Easing.OutQuint;

                updateAudioEffectsVisibility();
            });

            rotateEnabled.BindValueChanged(_ =>
            {
                rotateSettings.ClearTransforms();
                rotateSettings.AutoSizeDuration = 400;
                rotateSettings.AutoSizeEasing = Easing.OutQuint;

                updateAudioEffectsVisibility();
            });

            echoEnabled.BindValueChanged(_ =>
            {
                echoSettings.ClearTransforms();
                echoSettings.AutoSizeDuration = 400;
                echoSettings.AutoSizeEasing = Easing.OutQuint;

                updateAudioEffectsVisibility();
            });

            distortionEnabled.BindValueChanged(_ =>
            {
                distortionSettings.ClearTransforms();
                distortionSettings.AutoSizeDuration = 400;
                distortionSettings.AutoSizeEasing = Easing.OutQuint;

                updateAudioEffectsVisibility();
            });

            chorusEnabled.BindValueChanged(_ =>
            {
                chorusSettings.ClearTransforms();
                chorusSettings.AutoSizeDuration = 400;
                chorusSettings.AutoSizeEasing = Easing.OutQuint;

                updateAudioEffectsVisibility();
            });
            updateAudioEffectsVisibility();

            videoProgress.BindValueChanged(seek =>
            {
                if (seekbar.IsDragged)
                {
                    currentVideoSource?.SeekTo(seek.NewValue * 1000);
                }
            });

            uiVisible.BindValueChanged(visible =>
            {
                Schedule(() =>
                {
                    if (visible.NewValue)
                    {
                        userInterfaceContainer.Show();
                    }
                    else
                    {
                        userInterfaceContainer.Hide();
                    }
                });
            }, true);

            void updateAudioEffectsVisibility()
            {
                try
                {
                    //reverb
                    if (reverbEnabled.Value == false)
                        reverbSettings.ResizeHeightTo(0, 400, Easing.OutQuint);

                    reverbSettings.AutoSizeAxes = reverbEnabled.Value != false ? Axes.Y : Axes.None;

                    //rotate
                    if (rotateEnabled.Value == false)
                        rotateSettings.ResizeHeightTo(0, 400, Easing.OutQuint);

                    rotateSettings.AutoSizeAxes = rotateEnabled.Value != false ? Axes.Y : Axes.None;

                    //echo
                    if (echoEnabled.Value == false)
                        echoSettings.ResizeHeightTo(0, 400, Easing.OutQuint);

                    echoSettings.AutoSizeAxes = echoEnabled.Value != false ? Axes.Y : Axes.None;

                    //distortion
                    if (distortionEnabled.Value == false)
                        distortionSettings.ResizeHeightTo(0, 400, Easing.OutQuint);

                    distortionSettings.AutoSizeAxes = distortionEnabled.Value != false ? Axes.Y : Axes.None;

                    //chorus
                    if (chorusEnabled.Value == false)
                        chorusSettings.ResizeHeightTo(0, 400, Easing.OutQuint);

                    chorusSettings.AutoSizeAxes = chorusEnabled.Value != false ? Axes.Y : Axes.None;
                }
                catch
                {
                }
            }
        }

        private void fetchMyPlaylists()
        {
            foreach (var item in myPlaylistItemsView.Children)
            {
                Schedule(() => item.Expire());
            }

            if (!googleOAuth2.SignedIn.Value)
                return;

            Task.Run(async () =>
            {
                IList<Playlist> playlists = await api.GetMyPlaylistItemsAsync();

                Schedule(() =>
                {
                    myPlaylistsDropdown.Items = playlists;
                    myPlaylistsDropdown.Current.Value = playlists[0];
                });
            });

            Task.Run(async () =>
            {
                IList<Playlist> playlists = await api.GetMyPlaylistItemsAsync();

                foreach (Playlist playlist in playlists)
                {
                    MyPlaylistView playlistItemView = new MyPlaylistView()
                    {
                        RelativeSizeAxes = Axes.X,
                        Enabled = { Value = true },
                        ClickAction = async v =>
                        {
                            Schedule(async () =>
                            {
                                SetPlaylist(playlist.Id).FireAndForget();
                            });
                        },
                        OptionsClickEvent = async data =>
                        {
                            Schedule(async () =>
                            {
                                hideOverlays();

                                editPlaylistTitleBox.Current.Value = data.Snippet.Title;
                                switch (data.Status.PrivacyStatus)
                                {
                                    case "public":
                                        editPlaylistPrivacyStatusDropdown.Current.Value = PrivacyStatus.Public;
                                        break;
                                    case "unlisted":
                                        editPlaylistPrivacyStatusDropdown.Current.Value = PrivacyStatus.Unlisted;
                                        break;
                                    case "private":
                                        editPlaylistPrivacyStatusDropdown.Current.Value = PrivacyStatus.Private;
                                        break;
                                }

                                showOverlayContainer(editPlaylistOverlay);

                                updatePlaylistButton.Action = async () =>
                                {
                                    await Task.Run(async () =>
                                    {
                                        Schedule(async () =>
                                        {
                                            hideOverlays();
                                        });

                                        await api.UpdatePlaylistInfo(data.Id, editPlaylistTitleBox.Current.Value, editPlaylistPrivacyStatusDropdown.Current.Value);

                                        await Task.Delay(1000);

                                        Schedule(async () =>
                                        {
                                            fetchMyPlaylists();
                                        });
                                    });
                                };
                            });
                        },
                    };

                    Schedule(() =>
                    {
                        playlistItemView.Data = playlist;
                        myPlaylistItemsView.Add(playlistItemView);
                        playlistItemView.UpdateData();
                    });
                }
            });
        }

        private void saveVideoToPlaylist(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return;

            Task.Run(async () =>
            {
                await api.SaveVideoToPlaylist(myPlaylistsDropdown.Current.Value.Id, videoId);

                saveVideoOpenButton.Icon = FontAwesome.Solid.Bookmark;

                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.Playlists, NekoPlayerStrings.VideoSavedToPlaylist(myPlaylistsDropdown.Current.Value.Snippet.Title), FontAwesome.Solid.List);

                Schedule(() => onScreenDisplay.Display(toast));

                await Task.Delay(1000);

                Schedule(async () =>
                {
                    fetchMyPlaylists();
                });
            });
        }

        private void removeVideoFromPlaylist(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return;

            Task.Run(async () =>
            {
                await api.RemoveVideoFromPlaylist(myPlaylistsDropdown.Current.Value.Id, videoId);

                saveVideoOpenButton.Icon = FontAwesome.Regular.Bookmark;

                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.Playlists, NekoPlayerStrings.VideoRemovedFromPlaylist(myPlaylistsDropdown.Current.Value.Snippet.Title), FontAwesome.Solid.List);

                Schedule(() => onScreenDisplay.Display(toast));

                await Task.Delay(1000);

                Schedule(async () =>
                {
                    fetchMyPlaylists();
                });
            });
        }

        public void GetReportReasons()
        {
            if (googleOAuth2.SignedIn.Value == false)
                return;

            IList<VideoAbuseReportReasonItem> wth2 = api.GetVideoAbuseReportReasons();

            Schedule(() =>
            {
                reportReason.Items = wth2;
                reportReason.Current.Value = wth2[0];
            });
        }

        [Resolved]
        private ScreenshotManager screenshotManager { get; set; }

        private AdaptiveTextFlowContainer infoForNerds, playlistName;

        private Bindable<float> scalingPositionX = null!;
        private Bindable<float> scalingPositionY = null!;
        private Bindable<float> scalingSizeX = null!;
        private Bindable<float> scalingSizeY = null!;

        private FormSliderBar<float> dimSlider = null!;
        private FillFlowContainer<SettingsItemV2> scalingSettings = null!;
        private Bindable<ScalingMode> scalingMode = null!;

        private bool automaticRendererInUse;

        private IBindable<bool> uiVisible;

        private void hideControls()
        {
            if (!alwaysShowControl.Value)
            {
                if (isControlVisible == true)
                {
                    isControlVisible = false;
                    uiContainer.FadeOutFromOne(250);
                    uiGradientContainer.FadeOutFromOne(250);
                    sessionStatics.GetBindable<bool>(Static.IsControlVisible).Value = false;
                }
            }
        }

        [Resolved]
        private SessionStatics sessionStatics { get; set; }

        private async Task checkForUpdates()
        {
            if (updateManager == null || game == null)
                return;

            // ------------------------------
            //                              |
            //                              v      bro what
            CancellationTokenSource nanahiraSingsHope = new CancellationTokenSource();

            game.UpdateManagerVersionText.Value = NekoPlayerStrings.CheckingUpdate;

            settingsContainer.CheckForUpdatesButton.Enabled.Value = false;

            try
            {
                bool foundUpdate = await updateManager.CheckForUpdateAsync(nanahiraSingsHope.Token).ConfigureAwait(true);

                if (!foundUpdate)
                {
                    /*
                    alert.Text = NekoPlayerStrings.RunningLatestRelease(game.Version);
                    alert.Show();
                    */
                    if (settingsContainer.IsVisible)
                    {
                        ToastBase toast = new ToastWithIcon(NekoPlayerStrings.Updates, NekoPlayerStrings.RunningLatestRelease(game.Version), FontAwesome.Solid.CheckCircle);

                        onScreenDisplay.Display(toast);
                    }

                    game.UpdateManagerVersionText.Value = game.Version;
                    settingsContainer.CheckForUpdatesButton.Enabled.Value = true;
                }
            }
            catch
            {
                game.UpdateManagerVersionText.Value = game.Version;
                settingsContainer.CheckForUpdatesButton.Enabled.Value = true;
            }
            finally
            {
            }
        }

        public override bool CursorVisible => (isControlVisible || isAnyOverlayOpen.Value);

        private void showControls()
        {
            if (isControlVisible == false)
            {
                isControlVisible = true;
                uiContainer.FadeInFromZero(125);
                uiGradientContainer.FadeInFromZero(125);
                sessionStatics.GetBindable<bool>(Static.IsControlVisible).Value = true;
            }
        }

        private IBindable<bool> cursorInWindow;
#nullable enable
        private IWindow? window;
#nullable disable

        private partial class RoundedSeekBar : NekoPlayerSeekBar<double>
        {
            public override LocalisableString TooltipText => "";
        }

        private void updateRepeatState()
        {
            repeat.Value = !repeat.Value;
            repeatButton.SetEnabledValueLeftSide(repeat.Value);
            repeatButton.IconObject.FadeColour(repeat.Value ? bgColor : accentColor, 250, Easing.OutQuint);
            //repeatButton.TransformTo(nameof(Width), repeat.Value ? 50f : 40f, 1000, Easing.OutElastic);
        }

        private void updatePinState()
        {
            alwaysShowControl.Value = !alwaysShowControl.Value;
            pinButton.SetEnabledValueRightSide(alwaysShowControl.Value);
            pinButton.IconObject.FadeColour(alwaysShowControl.Value ? bgColor : accentColor, 250, Easing.OutQuint);
            //pinButton.TransformTo(nameof(Width), alwaysShowControl.Value ? 50f : 40f, 1000, Easing.OutElastic);
        }

#nullable enable
        private readonly Bindable<SettingsNote.Data?> windowModeDropdownNote = new Bindable<SettingsNote.Data?>();
#nullable disable

        private BindableNumber<double> playbackSpeed = new BindableNumber<double>(1)
        {
            MinValue = 0.1,
            MaxValue = 8,
            Precision = 0.01,
        };

#nullable enable
        private IDisposable? duckOperation;
#nullable disable

        private void showOverlayContainer(OverlayContainer overlayContent)
        {
            /*
            duckOperation = game.Duck(new DuckParameters
            {
                DuckVolumeTo = 1,
                DuckDuration = 100,
                RestoreDuration = 100,
            });
            */

            if ((overlayContent == commentsContainer) && (commentContainer.Children.Count == 0))
            {
                Sample sample = audio.Samples.Get(broWhat[Random.Shared.Next(0, broWhat.Length)]);
                sample.Play();
            }

            if (overlayContent.Name == "Menu Overlay")
            {
                //menuOverlayCharacter.FadeIn(500, Easing.OutQuint);
            }

            if (overlayContent == settingsContainer)
            {
                //settingsOverlayCharacter.FadeIn(500, Easing.OutQuint);
            }

            if (overlayContent.Name == "Audio Effects Overlay")
            {
                //audioEffectsOverlayCharacter.FadeIn(500, Easing.OutQuint);
            }

            if (playOverlaySFX.Value)
                overlayShowSample.Play();

            if (overlayContent is BottomOverlayContainer)
            {
                isAnyOverlayOpen.Value = true;
                overlayContent.IsVisible = true;
                if (overlayContent.DrawHeight >= 450)
                {
                    videoScalingContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                    videoContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                }
                //videoContainer.ScaleTo(1.03f, 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0.5f, 250, Easing.OutQuart);
                overlayContent.Show();
                overlayContent.MoveToY(200);
                overlayContent.MoveToY(0, 500, Easing.OutExpo);
                overlayContent.FadeInFromZero(250, Easing.OutQuart);
                //overlayShowSample.Play();
            }
            else if (overlayContent is SideOverlayContainer)
            {
                isAnyOverlayOpen.Value = true;
                overlayContent.IsVisible = true;
                if (overlayContent.DrawWidth >= 600)
                {
                    videoScalingContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                    videoContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                }
                //videoContainer.ScaleTo(1.03f, 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0.5f, 250, Easing.OutQuart);
                overlayContent.Show();
                overlayContent.MoveToX(200);
                overlayContent.MoveToX(0, 500, Easing.OutExpo);
                overlayContent.FadeInFromZero(250, Easing.OutQuart);
                //overlayShowSample.Play();
            }
            else
            {
                isAnyOverlayOpen.Value = true;
                overlayContent.IsVisible = true;
                videoScalingContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                videoContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                //videoContainer.ScaleTo(1.03f, 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0.5f, 250, Easing.OutQuart);
                overlayContent.Show();
                overlayContent.ScaleTo(0.8f);
                overlayContent.ScaleTo(1f, 750, Easing.OutExpo);
                overlayContent.FadeInFromZero(250, Easing.OutQuart);
                //overlayShowSample.Play();
            }
        }

        private void refreshSFX()
        {
            if (appGlobalConfig.Get<SFXType>(NekoPlayerSetting.OverlaySFXType) == SFXType.Legacy)
            {
                overlayShowSample = sampleStoreGlobal.Get(@"overlay-pop-in");
                overlayHideSample = sampleStoreGlobal.Get(@"overlay-pop-out");
            }
            else
            {
                overlayShowSample = sampleStoreGlobal.Get(@"New_Fix/overlay-pop-in");
                overlayHideSample = sampleStoreGlobal.Get(@"New_Fix/overlay-pop-out");
            }
        }

        [Resolved]
        private ISampleStore sampleStoreGlobal { get; set; }

        private void hideOverlayContainer(OverlayContainer overlayContent)
        {
            //duckOperation?.Dispose();

            if (overlayContent.Name == "Menu Overlay")
            {
                //menuOverlayCharacter.FadeOut(250, Easing.OutQuint);
            }

            if (overlayContent == settingsContainer)
            {
                //settingsOverlayCharacter.FadeOut(250, Easing.OutQuint);
            }

            if (overlayContent.Name == "Audio Effects Overlay")
            {
                //audioEffectsOverlayCharacter.FadeOut(250, Easing.OutQuint);
            }

            if (playOverlaySFX.Value)
                overlayHideSample.Play();

            if (overlayContent is BottomOverlayContainer)
            {
                overlayContent.IsVisible = false;
                isAnyOverlayOpen.Value = false;
                //overlayHideSample.Play();
                videoScalingContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                videoContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                //videoContainer.ScaleTo(1f, 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0f, 250, Easing.OutQuart);
                overlayContent.MoveToY(200, 500, Easing.OutQuart);
                overlayContent.FadeOutFromOne(250, Easing.OutQuart);
            }
            else if (overlayContent is SideOverlayContainer)
            {
                overlayContent.IsVisible = false;
                isAnyOverlayOpen.Value = false;
                //overlayHideSample.Play();
                videoScalingContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                videoContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                //videoContainer.ScaleTo(1f, 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0f, 250, Easing.OutQuart);
                overlayContent.MoveToX(200, 500, Easing.OutQuart);
                overlayContent.FadeOutFromOne(250, Easing.OutQuart);
            }
            else
            {
                overlayContent.IsVisible = false;
                isAnyOverlayOpen.Value = false;
                //overlayHideSample.Play();
                videoScalingContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                videoContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                //videoContainer.ScaleTo(1f, 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0f, 250, Easing.OutQuart);
                overlayContent.ScaleTo(0.8f, 250, Easing.OutQuart);
                overlayContent.FadeOutFromOne(250, Easing.OutQuart);
            }
        }

        private Bindable<bool> isAnyOverlayOpen;

        [Resolved]
        private NekoPlayerAppBase app { get; set; }

        [Resolved(canBeNull: true)]
        private UpdateManager updateManager { get; set; }

        private YouTubeVideoPlayer currentVideoSource;

        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private YouTubeAPI api { get; set; }

        public void Search()
        {
            foreach (var item in searchResultContainer.Children)
            {
                item.Expire();
            }

            Google.Apis.YouTube.v3.SearchResource.ListRequest.OrderEnum orderEnum = Google.Apis.YouTube.v3.SearchResource.ListRequest.OrderEnum.Relevance;

            switch (SearchSort.Value)
            {
                case SearchSortCriteria.Date:
                {
                    orderEnum = SearchResource.ListRequest.OrderEnum.Date;
                    break;
                }
                case SearchSortCriteria.Alphabet:
                {
                    orderEnum = SearchResource.ListRequest.OrderEnum.Title;
                    break;
                }
                case SearchSortCriteria.Relevance:
                {
                    orderEnum = SearchResource.ListRequest.OrderEnum.Relevance;
                    break;
                }
            }

            IList<SearchResult> searchResults = api.GetSearchResult(searchTextBox.Text, orderEnum);
            foreach (SearchResult item in searchResults)
            {
                if (item.Id.Kind == "youtube#video")
                {
                    YouTubeSearchResultView trickcal_is_good_game = new YouTubeSearchResultView()
                    {
                        RelativeSizeAxes = Axes.X,
                    };

                    searchResultContainer.Add(trickcal_is_good_game);

                    trickcal_is_good_game.ClickAction = async _ =>
                    {
                        ClearPlaylistItems();
                        Schedule(async () =>
                        {
                            SetVideoSource(item.Id.VideoId);
                        });
                    };

                    trickcal_is_good_game.Enabled.Value = true;

                    trickcal_is_good_game.Data = item;

                    trickcal_is_good_game.UpdateData();
                }
                else if (item.Id.Kind == "youtube#playlist")
                {
                    YouTubeSearchResultView wth = new YouTubeSearchResultView()
                    {
                        RelativeSizeAxes = Axes.X,
                    };

                    searchResultContainer.Add(wth);

                    wth.ClickAction = async _ =>
                    {
                        SetPlaylist(item.Id.PlaylistId).FireAndForget();
                    };

                    wth.Enabled.Value = true;

                    wth.Data = item;

                    wth.UpdateData();
                }
            }
        }

        public string TruncateWithEllipsis(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            // If the string is already short enough, return it as-is
            if (value.Length <= maxLength) return value;

            // Ensure we don't get a negative length if maxLength is smaller than the ellipsis
            int truncateLength = Math.Max(0, maxLength - 3);

            return value.Substring(0, truncateLength) + "...";
        }

        private void updatePresence(DiscordRichPresenceMode mode)
        {
            Timestamps timestamps = Timestamps.Now;
            ActivityType activityType = ActivityType.Watching;

            string state = NekoPlayer_DiscordRPCStrings.WatchingVideo;
            string buttonLabel = NekoPlayer_DiscordRPCStrings.WatchOnYouTube;

            try
            {
                if (videoData != null)
                {
                    //state = api.GetLocalizedChannelTitle(api.GetChannel(videoData.Snippet.ChannelId));
                    if (trayIconVisible.Value)
                    {
                        state = videoData.TopicDetails.TopicCategories.Contains("https://en.wikipedia.org/wiki/Music") ? NekoPlayer_DiscordRPCStrings.ListeningMusic : NekoPlayer_DiscordRPCStrings.ListeningOnBackground;
                        activityType = ActivityType.Listening;
                    }
                    else
                    {
                        state = videoData.TopicDetails.TopicCategories.Contains("https://en.wikipedia.org/wiki/Music") ? NekoPlayer_DiscordRPCStrings.ListeningMusic : NekoPlayer_DiscordRPCStrings.WatchingVideo;
                        activityType = videoData.TopicDetails.TopicCategories.Contains("https://en.wikipedia.org/wiki/Music") ? ActivityType.Listening : ActivityType.Watching;
                    }
                    buttonLabel = videoData.TopicDetails.TopicCategories.Contains("https://en.wikipedia.org/wiki/Music") ? NekoPlayer_DiscordRPCStrings.ListenOnYouTube : NekoPlayer_DiscordRPCStrings.WatchOnYouTube;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, e.GetDescription());
                state = NekoPlayer_DiscordRPCStrings.WatchingVideo;
                activityType = ActivityType.Watching;
                buttonLabel = NekoPlayer_DiscordRPCStrings.WatchOnYouTube;
            }

            switch (mode)
            {
                case DiscordRichPresenceMode.Full:
                {
                    if (videoData != null)
                    {
                        discordRPC?.UpdatePresence(new RichPresence()
                        {
                            Type = activityType,
                            Details = TruncateWithEllipsis(api.GetLocalizedVideoTitle(videoData), 128),
                            State = TruncateWithEllipsis(api.GetLocalizedChannelTitle(api.GetChannel(videoData.Snippet.ChannelId)), 128),
                            Timestamps = timestamps,
                            StatusDisplay = StatusDisplayType.Details,
                            StateUrl = $"https://www.youtube.com/channel/{api.GetChannel(videoData.Snippet.ChannelId).Id}",
                            DetailsUrl = $"https://youtu.be/{videoData.Id}",
                            Assets = new Assets()
                            {
                                LargeImageKey = videoData.Snippet.Thumbnails.High.Url,
                                LargeImageUrl = $"https://youtu.be/{videoData.Id}",
                                SmallImageText = "NekoPlayer",
                                SmallImageKey = "nekoplayer_liquidglass_remake_withbg"
                            },
                            Buttons =
                            [
                                new DiscordRPC.Button
                                {
                                    Label = buttonLabel,
                                    Url = $"https://youtu.be/{videoData.Id}",
                                }
                            ]
                        });
                    }
                    else
                    {
                        discordRPC?.UpdatePresence(new RichPresence()
                        {
                            Type = activityType,
                            State = NekoPlayer_DiscordRPCStrings.IdleString,
                            Assets = new Assets()
                            {
                                LargeImageKey = "nekoplayer_liquidglass_remake_withbg",
                            },
                        });
                    }
                    break;
                }
                case DiscordRichPresenceMode.Limited:
                {
                    if (videoData != null)
                    {
                        discordRPC?.UpdatePresence(new RichPresence()
                        {
                            Type = activityType,
                            State = state,
                            Timestamps = timestamps,
                            Assets = new Assets()
                            {
                                LargeImageKey = "nekoplayer_liquidglass_remake_withbg"
                            },
                            Buttons =
                            [
                                new DiscordRPC.Button
                                {
                                    Label = buttonLabel,
                                    Url = $"https://youtu.be/{videoData.Id}",
                                }
                            ]
                        });
                    }
                    else
                    {
                        discordRPC?.UpdatePresence(new RichPresence()
                        {
                            Type = activityType,
                            State = NekoPlayer_DiscordRPCStrings.IdleString,
                            Assets = new Assets()
                            {
                                LargeImageKey = "nekoplayer_liquidglass_remake_withbg",
                            },
                        });
                    }
                    break;
                }
                case DiscordRichPresenceMode.Off:
                {
                    discordRPC?.ClearPresence();
                    break;
                }
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            menuOverlayCharacter.FadeOut();
            audioEffectsOverlayCharacter.FadeOut();
            settingsOverlayCharacter.FadeOut();

            ghostIcon.Loop(t =>
                t.MoveToY(-10, 2000, Easing.InOutSine)
                 .Then()
                 .MoveToY(0, 2000, Easing.InOutSine)
            );

            discordRichPresence.BindValueChanged(mode => updatePresence(mode.NewValue), true);
            usernameDisplayMode.BindValueChanged(_ => updatePresence(discordRichPresence.Value), true);
            localeBindable.BindValueChanged(_ => updatePresence(discordRichPresence.Value), true);
            trayIconVisible.BindValueChanged(_ => updatePresence(discordRichPresence.Value), true);

            //check updates for LoadComplete
            if (game.IsDeployedBuild)
                checkForUpdates().FireAndForget();

            if (appGlobalConfig.Get<string>(NekoPlayerSetting.AccessToken) != string.Empty)
            {
                Task.Run(async () => await googleOAuth2.SignIn());
            }

            sessionStatics.GetBindable<bool>(Static.IsControlVisible).Value = true;

            cursorInWindow?.BindValueChanged(active =>
            {
                if (active.NewValue == false)
                {
                    Schedule(() => hideControls());
                }
                else
                {
                    Schedule(() => showControls());
                }
            });

            loadBtn.Action = async () =>
            {
                ClearPlaylistItems();
                Schedule(async () =>
                {
                    YoutubeExplode.Playlists.PlaylistId? playlistId = YoutubeExplode.Playlists.PlaylistId.TryParse(videoIdBox.Text);
                    YoutubeExplode.Videos.VideoId? videoId = YoutubeExplode.Videos.VideoId.TryParse(videoIdBox.Text);

                    if (videoId != null && !string.IsNullOrEmpty(videoId.Value))
                    {
                        SetVideoSource(videoIdBox.Text);
                    }
                    else
                    {
                        SetPlaylist(videoIdBox.Text).FireAndForget();
                    }
                });
            };

            loadPlaylistBtn.ClickAction = async _ =>
            {
                SetPlaylist(playlistIdBox.Text).FireAndForget();
            };

            searchButton.ClickAction = _ =>
            {
                Search();
            };

            searchOpenButton.Action = () =>
            {
                hideOverlays();
                showOverlayContainer(searchContainer);
                searchTextBox.TakeFocus();
            };

            reportOpenButton.Action = () =>
            {
                hideOverlays();
                showOverlayContainer(reportAbuseOverlay);
            };

            playlistOpenButton.Action = () =>
            {
                hideOverlays();
                showOverlayContainer(playlistOverlay);
            };

            audioEffectsOpenButton.Action = () =>
            {
                hideOverlays();
                showOverlayContainer(audioEffectsOverlay);
            };

            saveVideoOpenButton.Action = () =>
            {
                hideOverlays();
                showOverlayContainer(videoSaveLocationOverlay);
            };

            commentOpenButton.Action = () =>
            {
                if (!commentsDisabled)
                {
                    hideOverlays();
                    showOverlayContainer(commentsContainer);
                }
            };

            loadBtnOverlayShow.Action = () =>
            {
                hideOverlays();
                showOverlayContainer(loadVideoContainer);
                videoIdBox.TakeFocus();
            };

            settingsOverlayShowBtn.Action = () =>
            {
                hideOverlays();
                showOverlayContainer(settingsContainer);
            };
        }

        private bool isDownloading;

        private void hideOverlays()
        {
            if (isDownloading)
                return;

            foreach (var item in overlayContainers)
            {
                if (item.IsVisible == true)
                {
                    hideOverlayContainer(item);
                }
            }
        }

        public async Task SetPlaylist(string playlistId)
        {
            playlistId = YoutubeExplode.Playlists.PlaylistId.Parse(playlistId);
            Schedule(async () =>
            {
                videoIdBox.Text = string.Empty;
                if (loadVideoContainer.IsVisible == true)
                {
                    Schedule(() => hideOverlayContainer(loadVideoContainer));
                }

                Playlist playlist = api.GetPlaylistInfo(playlistId);
                IList<PlaylistItem> playlistItems = await api.GetPlaylistItems(playlistId);

                SetPlaylistInfo(playlist);
                await SetPlaylistItems(playlistItems);

                playlistIdBox.Text = string.Empty;
            });
        }

        private List<OverlayContainer> overlayContainers = new List<OverlayContainer>();

        public void RegisterOverlayContainer(OverlayContainer overlayContainer)
        {
            overlayContainer.Hide();
            overlayContainers.Add(overlayContainer);
        }

        [Resolved]
        private VolumeOverlay volume { get; set; }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            // ignore seek shortcuts on focused to text box
            if (currentVideoSource == null && ((e.Target.GetType() == typeof(AdaptiveTextBox)) || (e.Target.GetType() == typeof(FormTextBox)) || (e.Target.GetType() == typeof(FormNumberBox))))
                return true;

            if (e.Key >= Key.Number0 && e.Key <= Key.Number9)
            {
                int digit = e.Key - Key.Number0;
                double target = currentVideoSource.VideoProgress.MaxValue * (digit / 10.0);

                currentVideoSource?.SeekTo(target * 1000);
            }

            if (e.Key >= Key.Keypad0 && e.Key <= Key.Keypad9)
            {
                int digit = e.Key - Key.Keypad0;
                double target = currentVideoSource.VideoProgress.MaxValue * (digit / 10.0);

                currentVideoSource?.SeekTo(target * 1000);
            }

            return true;
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Target is TextBox)
                return false;

            switch (e.Action)
            {
                case GlobalAction.DecreaseVolume:
                case GlobalAction.IncreaseVolume:
                    return volume.Adjust(e.Action);

                case GlobalAction.FastForward_10sec:
                    currentVideoSource?.FastForward10Sec();
                    return true;

                case GlobalAction.FastRewind_10sec:
                    currentVideoSource?.FastRewind10Sec();
                    return true;

                case GlobalAction.DecreasePlaybackSpeed:
                    playbackSpeed.Value -= 0.05;
                    osd.Display(new SpeedChangeToast(playbackSpeed.Value));
                    return true;

                case GlobalAction.ResetPlaybackSpeed:
                    playbackSpeed.Value = 1;
                    osd.Display(new SpeedChangeToast(playbackSpeed.Value));
                    return true;

                case GlobalAction.IncreasePlaybackSpeed:
                    playbackSpeed.Value += 0.05;
                    osd.Display(new SpeedChangeToast(playbackSpeed.Value));
                    return true;

                case GlobalAction.DecreaseVideoVolume:
                    videoVolume.Value -= 0.05;
                    return true;

                case GlobalAction.IncreaseVideoVolume:
                    videoVolume.Value += 0.05;
                    return true;

                case GlobalAction.DecreasePlaybackSpeed2:
                    playbackSpeed.Value -= 0.01;
                    osd.Display(new SpeedChangeToast(playbackSpeed.Value));
                    return true;

                case GlobalAction.IncreasePlaybackSpeed2:
                    playbackSpeed.Value += 0.01;
                    osd.Display(new SpeedChangeToast(playbackSpeed.Value));
                    return true;
            }

            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case GlobalAction.ToggleMute:
                case GlobalAction.NextVolumeMeter:
                case GlobalAction.PreviousVolumeMeter:
                    return volume.Adjust(e.Action);

                case GlobalAction.RestartApp:
                    if (game?.RestartAppWhenExited() == true)
                    {
                        game.AttemptExit();
                    }
                    return true;

                case GlobalAction.QuitApp:
                    game.AttemptExit();
                    return true;

                case GlobalAction.Back:
                    hideOverlays();
                    return true;

                case GlobalAction.ToggleRepeatVideo:
                    updateRepeatState();
                    return true;

                case GlobalAction.OpenLoadVideo:
                    if (!loadBtnOverlayShow.Enabled.Value)
                        return true;

                    if (!loadVideoContainer.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(loadVideoContainer);
                        videoIdBox.TakeFocus();
                    }
                    else
                        hideOverlayContainer(loadVideoContainer);

                    return true;

                case GlobalAction.OpenSearch:
                    if (!searchContainer.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(searchContainer);
                        searchTextBox.TakeFocus();
                    }
                    else
                        hideOverlayContainer(searchContainer);

                    return true;

                case GlobalAction.OpenMyPlaylists:
                    if (!myPlaylistsOverlay.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(myPlaylistsOverlay);
                    }
                    else
                        hideOverlayContainer(myPlaylistsOverlay);

                    return true;

                case GlobalAction.OpenMenu:
                    if (!menuOverlay.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(menuOverlay);
                    }
                    else
                        hideOverlayContainer(menuOverlay);

                    return true;

                case GlobalAction.AddPlaylistKey:
                    if (!addPlaylistOverlay.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(addPlaylistOverlay);
                    }
                    else
                        hideOverlayContainer(addPlaylistOverlay);

                    return true;

                case GlobalAction.SaveVideoToPlaylist:
                    if (videoData == null)
                        return true;

                    if (!videoSaveLocationOverlay.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(videoSaveLocationOverlay);
                    }
                    else
                        hideOverlayContainer(videoSaveLocationOverlay);

                    return true;

                case GlobalAction.OpenSettings:
                    if (!settingsContainer.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(settingsContainer);
                    }
                    else
                        hideOverlayContainer(settingsContainer);

                    return true;

                case GlobalAction.OpenDescription:
                    if (!videoDescriptionContainer.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(videoDescriptionContainer);
                    }
                    else
                        hideOverlayContainer(videoDescriptionContainer);

                    return true;

                case GlobalAction.ReportAbuse:
                    if (videoData == null)
                        return true;

                    if (!reportAbuseOverlay.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(reportAbuseOverlay);
                    }
                    else
                        hideOverlayContainer(reportAbuseOverlay);

                    return true;

                /*
            case GlobalAction.DownloadVideo:
                if (videoData == null)
                    return true;

                if (!downloadReadyContainer.IsVisible)
                {
                    currentVideoSource?.Pause();
                    hideOverlays();
                    showOverlayContainer(downloadReadyContainer);
                }
                else
                    hideOverlayContainer(downloadReadyContainer);

                return true;
                */

                case GlobalAction.OpenComments:
                    if (videoData == null)
                        return true;

                    if (commentsDisabled)
                        return true;

                    if (!commentsContainer.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(commentsContainer);
                    }
                    else
                        hideOverlayContainer(commentsContainer);

                    return true;

                case GlobalAction.OpenPlaylist:
                    if (!playlistOverlay.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(playlistOverlay);
                    }
                    else
                        hideOverlayContainer(playlistOverlay);

                    return true;

                case GlobalAction.OpenAudioEffects:
                    if (!audioEffectsOverlay.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(audioEffectsOverlay);
                    }
                    else
                        hideOverlayContainer(audioEffectsOverlay);

                    return true;

                case GlobalAction.PlayPause:
                    if (currentVideoSource != null)
                    {
                        if (currentVideoSource.IsPlaying())
                            currentVideoSource.Pause(true);
                        else
                            currentVideoSource.Play(true);
                    }
                    showControls();
                    return true;

                case GlobalAction.PrevVideo:
                    if (currentVideoSource != null)
                    {
                        if (playlists.Count > 0)
                        {
                            if (playlistItemIndex != 0)
                                playlistItemIndex--;

                            Task.Run(async () =>
                            {
                                Schedule(async () =>
                                {
                                    SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId);
                                });
                            });
                        }
                    }
                    return true;

                case GlobalAction.NextVideo:
                    if (currentVideoSource != null)
                    {
                        if (playlists.Count > 0)
                        {
                            if (playlistItemIndex != playlists.Count - 1)
                                playlistItemIndex++;

                            Task.Run(async () =>
                            {
                                Schedule(async () =>
                                {
                                    SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId);
                                });
                            });
                        }
                    }
                    return true;

                case GlobalAction.ToggleAdjustPitchOnSpeedChange:
                    adjustPitch.Value = !adjustPitch.Value;
                    return true;

                case GlobalAction.ToggleFPSDisplay:
                    fpsDisplay.Value = !fpsDisplay.Value;
                    return true;

                case GlobalAction.CycleCaptionLanguage:
                    CycleCaptionLanguage();
                    return true;

                case GlobalAction.CycleAspectRatio:
                    CycleAspectRatio();
                    return true;

                case GlobalAction.CycleScalingMode:
                    CycleScalingMode();
                    return true;

                case GlobalAction.ToggleReverbEffect:
                    reverbEnabled.Value = !reverbEnabled.Value;
                    return true;

                case GlobalAction.ToggleRotateEffect:
                    rotateEnabled.Value = !rotateEnabled.Value;
                    return true;

                case GlobalAction.ToggleEchoEffect:
                    echoEnabled.Value = !echoEnabled.Value;
                    return true;

                case GlobalAction.ToggleDistortionEffect:
                    distortionEnabled.Value = !distortionEnabled.Value;
                    return true;

                case GlobalAction.ToggleKaraokeEffect:
                    karaokeEnabled.Value = !karaokeEnabled.Value;
                    return true;

                case GlobalAction.ToggleChorusEffect:
                    chorusEnabled.Value = !chorusEnabled.Value;
                    return true;
            }

            return false;
        }

        [Resolved]
        private OnScreenDisplay osd { get; set; } = null!;

        private int playlistItemIndex = 0;

        protected void CycleCaptionLanguage()
        {
            if (captionEnabled.Disabled)
                return;

            captionEnabled.Value = !captionEnabled.Value;
        }

        private IList<PlaylistItem> playlists = new List<PlaylistItem>();
        private List<PlaylistItemView> playlistItemViews = new List<PlaylistItemView>();

        private YoutubeExplode.Videos.Streams.VideoQuality currentVideoQuality;

        public void ClearPlaylistItems()
        {
            playlists.Clear();
            playlistItemViews.Clear();

            foreach (var item in playlistItemsView.Children)
            {
                Schedule(() => item.Expire());
            }

            playlistName.Text = NekoPlayerStrings.PlaylistNotLoaded;
            playlistAuthor.Text = NekoPlayerStrings.PlaylistNotLoadedDesc;

            if (playlists.Count == 0)
            {
                Schedule(() => prevVideoButton.Enabled.Value = false);
                Schedule(() => nextVideoButton.Enabled.Value = false);
            }
        }

        private Google.Apis.YouTube.v3.Data.Video videoData;
        private Google.Apis.YouTube.v3.Data.Channel channelData;

        public async Task SetPlaylistItems(IList<PlaylistItem> playlists)
        {
            this.playlists = playlists;

            playlistItemViews.Clear();

            foreach (var item in playlistItemsView.Children)
            {
                Schedule(() => item.Expire());
            }

            int i = 0;

            foreach (var item in playlists)
            {
                try
                {
                    Google.Apis.YouTube.v3.Data.Video videoData = api.GetVideo(item.Snippet.ResourceId.VideoId);

                    PlaylistItemView playlistItemView = new PlaylistItemView(i)
                    {
                        RelativeSizeAxes = Axes.X,
                        Enabled = { Value = true },
                    };

                    playlistItemView.ClickAction = async v =>
                    {
                        Schedule(async () =>
                        {
                            playlistItemIndex = playlistItemView.Index;
                            Schedule(async () =>
                            {
                                SetVideoSource(item.Snippet.ResourceId.VideoId);
                            });
                        });
                    };

                    playlistItemViews.Add(playlistItemView);

                    Schedule(() =>
                    {
                        playlistItemView.Data = videoData;
                        playlistItemsView.Add(playlistItemView);
                        playlistItemView.UpdateData();
                    });

                    i++;
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.GetDescription());
                }
            }

            Schedule(async () =>
            {
                SetVideoSource(playlists[0].Snippet.ResourceId.VideoId);
            });
        }

        public void SetPlaylistInfo(Playlist playlist)
        {
            Schedule(() =>
            {
                playlistName.Text = playlist.Snippet.Title;
                playlistAuthor.Text = string.Empty;
                playlistAuthor.AddLink(playlist.Snippet.ChannelTitle, $"https://www.youtube.com/channel/{playlist.Snippet.ChannelId}");
            });
        }

        protected void CycleScalingMode()
        {
            switch (scalingMode.Value)
            {
                case ScalingMode.Off:
                    scalingMode.Value = ScalingMode.Everything;
                    break;

                case ScalingMode.Everything:
                    scalingMode.Value = ScalingMode.Video;
                    break;

                case ScalingMode.Video:
                    scalingMode.Value = ScalingMode.Off;
                    break;
            }
        }

        protected void CycleAspectRatio()
        {
            switch (aspectRatioMethod.Value)
            {
                case AspectRatioMethod.Letterbox:
                    aspectRatioMethod.Value = AspectRatioMethod.Fill;
                    break;

                case AspectRatioMethod.Fill:
                    aspectRatioMethod.Value = AspectRatioMethod.Letterbox;
                    break;
            }
        }

        private Bindable<bool> videoPlaying;

        protected override void Update()
        {
            base.Update();

            //seekbar.PlaybackSpeed.Value = playbackSpeed.Value;

            if (game.UseSystemCursor.Value == true)
            {
                game.SetCursorVisibility(CursorVisible);
            }

            if (commentTextBoxContainerFocused != commentTextBox.HasFocus)
            {
                commentTextBoxContainerFocused = commentTextBox.HasFocus;
                commentTextBoxContainer.TransformTo(nameof(Padding), commentTextBoxContainerFocused ? new MarginPadding { Horizontal = 32 } : new MarginPadding { Horizontal = 48 }, 1000, Easing.OutQuint);
                commentTextBoxContainer.TransformTo(nameof(Margin), commentTextBoxContainerFocused ? new MarginPadding { Bottom = 16 } : new MarginPadding { Bottom = 12 }, 500, Easing.OutBack);
            }

            if (searchTextBoxContainerFocused != searchTextBox.HasFocus)
            {
                searchTextBoxContainerFocused = searchTextBox.HasFocus;
                searchTextBoxContainer.TransformTo(nameof(Padding), searchTextBoxContainerFocused ? new MarginPadding { Horizontal = 32 } : new MarginPadding { Horizontal = 48 }, 1000, Easing.OutQuint);
                searchTextBoxContainer.TransformTo(nameof(Margin), searchTextBoxContainerFocused ? new MarginPadding { Bottom = 16 } : new MarginPadding { Bottom = 12 }, 500, Easing.OutBack);
            }


            if (currentVideoSource != null)
            {
                playPause.Icon = (currentVideoSource.IsPlaying() ? FontAwesome.Solid.Pause : FontAwesome.Solid.Play);
                playPause.TooltipText = (currentVideoSource.IsPlaying() ? NekoPlayerStrings.Pause : NekoPlayerStrings.Play);
                videoProgress.MaxValue = currentVideoSource.VideoProgress.MaxValue;

                if (videoPlaying.Value != currentVideoSource.IsPlaying())
                {
                    seekbar.IsPlaying.Value = currentVideoSource.IsPlaying();
                    playPause.SetEnabledValue2(!currentVideoSource.IsPlaying());
                    playPause.IconObject.FadeColour(currentVideoSource.IsPlaying() ? bgColor : accentColor, 250, Easing.OutQuint);
                    //playPause.TransformTo(nameof(Width), currentVideoSource.IsPlaying() ? 50f : 40f, 1000, Easing.OutElastic);

                    //prevVideoButton.TransformTo(nameof(Width), currentVideoSource.IsPlaying() ? 35f : 40f, 1000, Easing.OutElastic);
                    //prevVideoButton.TransformTo(nameof(CornerRadius), currentVideoSource.IsPlaying() ? new CornersInfo(15f, 15f, NekoPlayerApp.UI_CORNER_RADIUS / 1.5f, NekoPlayerApp.UI_CORNER_RADIUS / 1.5f) : new CornersInfo(15), 250, Easing.OutQuint);

                    //nextVideoButton.TransformTo(nameof(Width), currentVideoSource.IsPlaying() ? 35f : 40f, 1000, Easing.OutElastic);
                    //nextVideoButton.TransformTo(nameof(CornerRadius), currentVideoSource.IsPlaying() ? new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 1.5f, NekoPlayerApp.UI_CORNER_RADIUS / 1.5f, 15f, 15f) : new CornersInfo(15), 250, Easing.OutQuint);
                }

                videoPlaying.Value = currentVideoSource.IsPlaying();

                string currentTime;

                TimeSpan duration = TimeSpan.FromSeconds(currentVideoSource.VideoProgress.Value);
                /*
                if (duration.Hours > 0)
                {
                    currentTime.Text = $"{duration.Hours.ToString("00")}:{duration.Minutes.ToString("00")}:{duration.Seconds.ToString("00")}";
                }
                else
                {
                    currentTime.Text = $"{duration.Minutes.ToString("0")}:{duration.Seconds.ToString("00")}";
                }
                */

                if (duration.Hours > 0)
                {
                    currentTime = $"{duration.Hours.ToString("00")}:{duration.Minutes.ToString("00")}:{duration.Seconds.ToString("00")}";
                }
                else
                {
                    currentTime = $"{duration.Minutes.ToString("0")}:{duration.Seconds.ToString("00")}";
                }

                timeText.Text = $"{currentTime} / {totalTimeText}";

                currentVideoSource?.UpdateSeekingState(seekbar.IsDragged);

                if (seekbar.IsDragged == false)
                    videoProgress.Value = currentVideoSource.VideoProgress.Value;
            }
        }

        private TimeSpan totalTimeSpan;

        private ControlBarIconButton playPause;

        private bool commentsDisabled = false;

        public void GetLocalizedVideoDescriptionRemake(Google.Apis.YouTube.v3.Data.Video videoData)
        {
            string str = api.GetLocalizedVideoDescription(videoData);

            videoDescription.Text = string.Empty;

            List<YouTubeDescriptionTextToken> list = NekoPlayerDescriptionParser.Parse(str);

            foreach (YouTubeDescriptionTextToken item in list)
            {
                switch (item.Type)
                {
                    case YouTubeDescriptionTokenType.Text:
                        videoDescription.AddText(item.Value);
                        break;
                    case YouTubeDescriptionTokenType.Url:
                        videoDescription.AddArbitraryDrawable(new UrlRedirectDisplay(item.Value));
                        break;
                    case YouTubeDescriptionTokenType.Mention:
                        videoDescription.AddLink(item.Value, $"https://www.youtube.com/{item.Value}", NekoPlayerStrings.YouTubeHandleViewProfile(item.Value));
                        break;
                    case YouTubeDescriptionTokenType.Hashtag:
                        videoDescription.AddLink(item.Value, $"https://www.youtube.com/hashtag/{item.Value.Replace("#", string.Empty)}", NekoPlayerStrings.Hashtag(item.Value));
                        break;
                    case YouTubeDescriptionTokenType.Timestamp:
                        videoDescription.AddArbitraryDrawable(new TimestampButton(item.Value)
                        {
                            TimestampClicked = second =>
                            {
                                Logger.Log(second.ToString());
                                hideOverlays();
                                seekTo((second / 60) * 1000);
                            },
                        });
                        break;
                }
            }
        }

        private partial class PlaybackSpeedSliderBar : RoundedSliderBar<double>
        {
            public override LocalisableString TooltipText => NekoPlayerStrings.PlaybackSpeed(Current.Value);
        }

        private RoundedIconButton viewMoreComments;
        private Container viewMoreCommentsContainer;

        private void updateComments(string videoId, string pageToken = "")
        {
            if (commentsDisabled)
            {
                Schedule(() =>
                {
                    quickCommentOpenButton.TooltipText = NekoPlayerStrings.Comments(NekoPlayerStrings.DisabledByUploader);
                    commentCount.Text = NekoPlayerStrings.DisabledByUploader;
                    commentsContainerTitle.Text = NekoPlayerStrings.Comments(NekoPlayerStrings.DisabledByUploader);
                    commentsEmpty.Show();
                });
                return;
            }

            Schedule(() =>
            {
                foreach (var item in commentContainer.Children)
                {
                    if (string.IsNullOrEmpty(pageToken))
                        Schedule(() => item.Expire());
                }

                quickCommentOpenButton.TooltipText = NekoPlayerStrings.Comments(videoData.Statistics.CommentCount != null ? Convert.ToInt32(videoData.Statistics.CommentCount).ToStandardFormattedString(0) : NekoPlayerStrings.DisabledByUploader);
                commentCount.Text = videoData.Statistics.CommentCount != null ? Convert.ToDouble(videoData.Statistics.CommentCount).ToMetric(decimals: 2) : NekoPlayerStrings.DisabledByUploader;
                commentsContainerTitle.Text = NekoPlayerStrings.Comments(videoData.Statistics.CommentCount != null ? Convert.ToInt32(videoData.Statistics.CommentCount).ToStandardFormattedString(0) : NekoPlayerStrings.DisabledByUploader);

                OrderEnum orderEnum = CommentsSort.Value == CommentsSortCriteria.Top ? OrderEnum.Relevance : OrderEnum.Time;

                CommentThreadListResponse commentThreadListResponse;

                try
                {
                    // comments area
                    commentThreadListResponse = api.GetCommentThread(videoId, pageToken, orderEnum);

                    currentCommentsNextPageToken = commentThreadListResponse.NextPageToken;

                    if (!string.IsNullOrEmpty(commentThreadListResponse.NextPageToken))
                    {
                        viewMoreComments = new RoundedIconButton(FontAwesome.Solid.ArrowDown)
                        {
                            Width = 250,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = NekoPlayerStrings.ViewMoreComments,
                            Action = () =>
                            {
                                viewMoreCommentsContainer.Expire();
                                updateComments(videoId, currentCommentsNextPageToken);
                            }
                        };

                        viewMoreCommentsContainer = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                viewMoreComments,
                            }
                        };
                    }

                    for (int i = 0; i < commentThreadListResponse.Items.Count; i++)
                    {
                        CommentThread item = commentThreadListResponse.Items[i];
#pragma warning disable CS4014 // 이 호출을 대기하지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다.
                        Task.Run(async () =>
                        {
                            Schedule(() =>
                            {
                                commentContainer.Add(new CommentDisplay(item)
                                {
                                    RelativeSizeAxes = Axes.X,
                                    TimestampClicked = second =>
                                    {
                                        Logger.Log(second.ToString());
                                        hideOverlays();
                                        seekTo((second / 60) * 1000);
                                    }
                                });

                                if (item.Replies != null && item.Replies.Comments != null && item.Replies.Comments.Count > 0)
                                {
                                    for (int i2 = 0; i2 < item.Replies.Comments.Count; i2++)
                                    {
                                        Comment item2 = item.Replies.Comments[i2];
                                        commentContainer.Add(new CommentDisplay(item, item2)
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            TimestampClicked = second =>
                                            {
                                                Logger.Log(second.ToString());
                                                hideOverlays();
                                                seekTo((second / 60) * 1000);
                                            }
                                        });
                                    }
                                }
                            });
                        }).ContinueWith(_ =>
                        {
                            if (!string.IsNullOrEmpty(commentThreadListResponse.NextPageToken))
                            {
                                Schedule(() =>
                                {
                                    commentContainer.Remove(viewMoreCommentsContainer, false);
                                    commentContainer.Add(viewMoreCommentsContainer);
                                });
                            }
                        });
#pragma warning restore CS4014 // 이 호출을 대기하지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다.
                    }

                    if (commentThreadListResponse.Items.Count > 0)
                        commentsEmpty.Hide();
                    else
                        commentsEmpty.Show();
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.GetDescription());
                    commentsEmpty.Show();
                }
            });
        }

        private bool isVideoLoading;
        private VideosResource.RateRequest.RatingEnum currentVideoLikeOrDislike;

        private void updateRatingButtons(string videoId, bool ratingButtonsEnabled)
        {
            Task.Run(async () =>
            {
                VideosResource.RateRequest.RatingEnum things;
                if (googleOAuth2.SignedIn.Value)
                {
                    things = await api.GetVideoRating(videoId);
                }
                else
                {
                    things = VideosResource.RateRequest.RatingEnum.None;
                }
                currentVideoLikeOrDislike = things;

                switch (things)
                {
                    case VideosResource.RateRequest.RatingEnum.None:
                    {
                        Schedule(() =>
                        {
                            dislikeButtonBackgroundSelected.FadeOut(250, Easing.OutQuint);
                            likeButtonBackgroundSelected.FadeOut(250, Easing.OutQuint);
                            dislikeButton.TransformTo(nameof(CornerRadius), new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f, 16, 16), 250, Easing.OutQuint);
                            likeButton.TransformTo(nameof(CornerRadius), new CornersInfo(16, 16, NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f), 250, Easing.OutQuint);
                            likeButtonForeground.FadeColour(overlayColourProvider1.Content2, 250, Easing.OutQuint);
                            dislikeButtonForeground.FadeColour(overlayColourProvider1.Content2, 250, Easing.OutQuint);
                            quickLikeButton.SetEnabledValueLeftSide(false);
                            quickLikeButton.IconObject.FadeColour(accentColor, 250, Easing.OutQuint);
                            quickDislikeButton.SetEnabledValueRightSide(false);
                            quickDislikeButton.IconObject.FadeColour(accentColor, 250, Easing.OutQuint);

                            if (ratingButtonsEnabled)
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Like);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Dislike);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                quickLikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Like);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                quickDislikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Dislike);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };
                            }
                            else
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                };

                                quickLikeButton.ClickAction = async _ =>
                                {
                                };

                                quickDislikeButton.ClickAction = async _ =>
                                {
                                };
                            }
                        });
                        break;
                    }
                    case VideosResource.RateRequest.RatingEnum.Like:
                    {
                        Schedule(() =>
                        {
                            dislikeButtonBackgroundSelected.FadeOut(250, Easing.OutQuint);
                            likeButtonBackgroundSelected.FadeIn(250, Easing.OutQuint);
                            dislikeButton.TransformTo(nameof(CornerRadius), new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f, 16, 16), 250, Easing.OutQuint);
                            likeButton.TransformTo(nameof(CornerRadius), new CornersInfo(16f), 250, Easing.OutQuint);
                            likeButtonForeground.FadeColour(overlayColourProvider1.Background4, 250, Easing.OutQuint);
                            dislikeButtonForeground.FadeColour(overlayColourProvider1.Content2, 250, Easing.OutQuint);
                            quickLikeButton.SetEnabledValueLeftSide(true);
                            quickLikeButton.IconObject.FadeColour(bgColor, 250, Easing.OutQuint);
                            quickDislikeButton.SetEnabledValueRightSide(false);
                            quickDislikeButton.IconObject.FadeColour(accentColor, 250, Easing.OutQuint);

                            if (ratingButtonsEnabled)
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.None);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Dislike);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                quickLikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.None);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                quickDislikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Dislike);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };
                            }
                            else
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                };

                                quickLikeButton.ClickAction = async _ =>
                                {
                                };

                                quickDislikeButton.ClickAction = async _ =>
                                {
                                };
                            }
                        });
                        break;
                    }
                    case VideosResource.RateRequest.RatingEnum.Dislike:
                    {
                        Schedule(() =>
                        {
                            dislikeButtonBackgroundSelected.FadeIn(250, Easing.OutQuint);
                            likeButtonBackgroundSelected.FadeOut(250, Easing.OutQuint);
                            dislikeButton.TransformTo(nameof(CornerRadius), new CornersInfo(16f), 250, Easing.OutQuint);
                            likeButton.TransformTo(nameof(CornerRadius), new CornersInfo(16, 16, NekoPlayerApp.UI_CORNER_RADIUS / 3f, NekoPlayerApp.UI_CORNER_RADIUS / 3f), 250, Easing.OutQuint);
                            likeButtonForeground.FadeColour(overlayColourProvider1.Content2, 250, Easing.OutQuint);
                            dislikeButtonForeground.FadeColour(overlayColourProvider1.Background4, 250, Easing.OutQuint);
                            quickLikeButton.SetEnabledValueLeftSide(false);
                            quickLikeButton.IconObject.FadeColour(accentColor, 250, Easing.OutQuint);
                            quickDislikeButton.SetEnabledValueRightSide(true);
                            quickDislikeButton.IconObject.FadeColour(bgColor, 250, Easing.OutQuint);

                            if (ratingButtonsEnabled)
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Like);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.None);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                quickLikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Like);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };

                                quickDislikeButton.ClickAction = async _ =>
                                {
                                    if (!googleOAuth2.SignedIn.Value)
                                        return;

                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.None);
                                    Schedule(() =>
                                    {
                                        refreshLikeDislikeCount(videoId);
                                        updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);
                                    });
                                };
                            }
                            else
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                };

                                quickLikeButton.ClickAction = async _ =>
                                {
                                };

                                quickDislikeButton.ClickAction = async _ =>
                                {
                                };
                            }
                        });
                        break;
                    }
                }
            });
        }

        [Resolved]
        private OverlayColourProvider overlayColourProvider1 { get; set; }

        private void refreshLikeDislikeCount(string videoId)
        {
            Task.Run(() =>
            {
                try
                {
                    dislikeCount.Text = ReturnYouTubeDislike.GetDislikes(videoId).Dislikes > 0 ? Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).Dislikes).ToMetric(decimals: 2) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).RawDislikes).ToMetric(decimals: 2);
                    dislikeButton.TooltipText = NekoPlayerStrings.DislikeCountTooltip(ReturnYouTubeDislike.GetDislikes(videoId).Dislikes.ToStandardFormattedString(0), ReturnYouTubeDislike.GetDislikes(videoId).RawDislikes.ToStandardFormattedString(0));
                }
                catch
                {
                    dislikeCount.Text = "0";
                }

                likeCount.Text = videoData.Statistics.LikeCount != null ? Convert.ToDouble(videoData.Statistics.LikeCount).ToMetric(decimals: 2) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).RawLikes).ToMetric(decimals: 2);
            });
        }

        public void SetVideoMetadataDisplayAlignment(VideoMetadataDisplayAlignment alignment)
        {
            videoMetadataDisplay.SetVideoMetadataDisplayAlignment(alignment);
            switch (alignment)
            {
                case VideoMetadataDisplayAlignment.Left:
                {
                    videoMetadataDisplay.Anchor = Anchor.TopLeft;
                    videoMetadataDisplay.Origin = Anchor.TopLeft;
                    break;
                }
                case VideoMetadataDisplayAlignment.Right:
                {
                    videoMetadataDisplay.Anchor = Anchor.TopRight;
                    videoMetadataDisplay.Origin = Anchor.TopRight;
                    break;
                }
            }
        }

        private Color4 bgColor2;

        public void GetPalette(Google.Apis.YouTube.v3.Data.Video video)
        {
            Task.Run(async () =>
            {
                try
                {
                    var cachePath = app.Host.CacheStorage.GetStorageForDirectory("videoThumbnailCache_").GetFullPath($"{video.Id}.png");

                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var imageBytes = await httpClient.GetByteArrayAsync(video.Snippet.Thumbnails.High.Url);
                        await System.IO.File.WriteAllBytesAsync(cachePath, imageBytes);
                    }

                    using SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> bitmap = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(app.Host.CacheStorage.GetStorageForDirectory("videoThumbnailCache_").GetFullPath($"{video.Id}.png"));

                    IBitmapHelper bitmapHelper = new BitmapHelper(bitmap);
                    PaletteBuilder paletteBuilder = new PaletteBuilder();
                    Palette palette = paletteBuilder.Generate(bitmapHelper);
                    try
                    {
                        int? rgbColor = palette.LightMutedSwatch?.Rgb;
                        int? rgbColor2 = palette.DarkMutedSwatch?.Rgb;

                        if (rgbColor != null && rgbColor2 != null)
                        {
                            accentColor = System.Drawing.Color.FromArgb((int)rgbColor);
                            bgColor = System.Drawing.Color.FromArgb((int)rgbColor2);
                            bgColor2 = System.Drawing.Color.FromArgb((int)rgbColor2);
                        }
                        else
                        {
                            accentColor = overlayColourProvider1.Content2;
                            bgColor = overlayColourProvider1.Background3;
                            bgColor2 = overlayColourProvider1.Content2.Darken(1);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, e.GetDescription());
                    }
                }
                catch
                {
                    accentColor = overlayColourProvider1.Content2;
                    bgColor = overlayColourProvider1.Background3;
                    bgColor2 = overlayColourProvider1.Content2.Darken(1);
                }

                #region video controls color area

                Schedule(() =>
                {
                    seekbar.AccentColour = accentColor;
                    seekbar.BackgroundColour = bgColor2;

                    spinner.AccentColor = accentColor;
                    spinner.BackgroundColour = bgColor;

                    prevVideoButton.AccentColor = accentColor;
                    prevVideoButton.BackgroundColour = bgColor;
                    prevVideoButton.IconObject.FadeColour(accentColor);

                    nextVideoButton.AccentColor = accentColor;
                    nextVideoButton.BackgroundColour = bgColor;
                    nextVideoButton.IconObject.FadeColour(accentColor);

                    playPause.AccentColor = accentColor;
                    playPause.BackgroundColour = bgColor;

                    if (currentVideoSource != null)
                        playPause.IconObject.FadeColour(currentVideoSource.IsPlaying() ? bgColor : accentColor);
                    else
                        playPause.IconObject.FadeColour(accentColor);

                    speedBarBG.Colour = bgColor;
                    speedBarIcon.Colour = accentColor;

                    repeatButton.IconObject.FadeColour(repeat.Value ? bgColor : accentColor);
                    repeatButton.AccentColor = accentColor;
                    repeatButton.BackgroundColour = bgColor;

                    quickLikeButton.IconObject.FadeColour(currentVideoLikeOrDislike == VideosResource.RateRequest.RatingEnum.Like ? bgColor : accentColor, 250, Easing.OutQuint);
                    quickLikeButton.AccentColor = accentColor;
                    quickLikeButton.BackgroundColour = bgColor;
                    quickDislikeButton.IconObject.FadeColour(currentVideoLikeOrDislike == VideosResource.RateRequest.RatingEnum.Dislike ? bgColor : accentColor, 250, Easing.OutQuint);
                    quickDislikeButton.AccentColor = accentColor;
                    quickDislikeButton.BackgroundColour = bgColor;

                    quickCommentOpenButton.IconObject.FadeColour(accentColor);
                    quickCommentOpenButton.AccentColor = accentColor;
                    quickCommentOpenButton.BackgroundColour = bgColor;

                    videoSettingsButton.IconObject.FadeColour(accentColor);
                    videoSettingsButton.AccentColor = accentColor;
                    videoSettingsButton.BackgroundColour = bgColor;

                    playlistButton.IconObject.FadeColour(accentColor);
                    playlistButton.AccentColor = accentColor;
                    playlistButton.BackgroundColour = bgColor;

                    captionButton.IconObject.FadeColour(captionEnabled.Value ? bgColor : accentColor);
                    captionButton.AccentColor = accentColor;
                    captionButton.BackgroundColour = bgColor;

                    pinButton.IconObject.FadeColour(alwaysShowControl.Value ? bgColor : accentColor);
                    pinButton.AccentColor = accentColor;
                    pinButton.BackgroundColour = bgColor;

                    speedBarSlider.AccentColour = accentColor;
                    speedBarSlider.BackgroundColour = bgColor.Lighten(0.5f);

                    speedText.Colour = accentColor;

                    volumeBarBG.Colour = bgColor;
                    volumeIcon.Colour = accentColor;

                    volumeBarSlider.AccentColour = accentColor;
                    volumeBarSlider.BackgroundColour = bgColor.Lighten(0.5f);

                    volumeText.Colour = accentColor;

                    timeBG.Colour = bgColor;
                    timeText.Colour = accentColor;

                    menuOverlayShow.BackgroundColour = bgColor;
                    menuOverlayShow.IconColour = accentColor;
                });

                #endregion
            });
        }

        private Color4 accentColor;
        private Color4 bgColor;

        private string currentCommentsNextPageToken;

        private void updateVideoMetadata(string videoId)
        {
            videoMetadataDisplay.UpdateVideo(videoId);
            videoMetadataDisplayDetails.UpdateVideo(videoId);
            Task.Run(async () =>
            {
                // metadata area
                videoData = api.GetVideo(videoId);
                channelData = api.GetChannel(videoData.Snippet.ChannelId);
                updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);

                Schedule(() => GetPalette(videoData));
                Schedule(() => commentOpenButton.Enabled.Value = videoData.Statistics.CommentCount != null);

                if (googleOAuth2.SignedIn.Value)
                {
                    Schedule(() => reportOpenButton.Enabled.Value = true);
                    Schedule(() => saveVideoOpenButton.Enabled.Value = true);
                    Schedule(() => quickLikeButton.Enabled.Value = true);
                    Schedule(() => quickDislikeButton.Enabled.Value = true);
                }
                //Schedule(() => seekbar.GetPalette(videoData));

                commentsDisabled = videoData.Statistics.CommentCount == null;

                if (videoData.Statistics.CommentCount != null)
                {
                    Schedule(() => quickCommentOpenButton.Enabled.Value = true);
                }
                else
                {
                    Schedule(() => quickCommentOpenButton.Enabled.Value = false);
                }

                updateWindowTitle();
                //game.RequestUpdateWindowTitle($"{api.GetLocalizedChannelTitle(api.GetChannel(videoData.Snippet.ChannelId))} - {api.GetLocalizedVideoTitle(videoData)}");

                DateTimeOffset? dateTime = videoData.Snippet.PublishedAtDateTimeOffset;
                DateTime now = DateTime.Now;
                if (!string.IsNullOrEmpty(api.GetLocalizedVideoDescription(videoData)))
                {
                    //Schedule(() => videoDescription.Text = api.GetLocalizedVideoDescription(videoData));
                    Schedule(() => GetLocalizedVideoDescriptionRemake(videoData));
                }
                else
                {
                    videoDescription.Text = string.Empty;
                    Schedule(() => videoDescription.AddText(NekoPlayerStrings.NoDescription, text =>
                    {
                        text.Font = NekoPlayerApp.DefaultFont.With(weight: "SemiBold");
                        text.Colour = overlayColourProvider1.Background1;
                    }));
                }
                sessionStatics.GetBindable<string>(Static.CurrentThumbnailUrl).Value = videoData.Snippet.Thumbnails.High.Url;
                //commentCount.Text = videoData.Statistics.CommentCount != null ? Convert.ToInt32(videoData.Statistics.CommentCount).ToStandardFormattedString(0) : NekoPlayerStrings.DisabledByUploader;
                /*
                try
                {
                    dislikeCount.Text = ReturnYouTubeDislike.GetDislikes(videoId).Dislikes > 0 ? Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).Dislikes).ToMetric(decimals: 2) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).RawDislikes).ToMetric(decimals: 2);
                    dislikeButton.TooltipText = NekoPlayerStrings.DislikeCountTooltip(ReturnYouTubeDislike.GetDislikes(videoId).Dislikes.ToStandardFormattedString(0), ReturnYouTubeDislike.GetDislikes(videoId).RawDislikes.ToStandardFormattedString(0));
                }
                catch
                {
                    dislikeCount.Text = "0";
                }
                */

                string uploadDateRaw = videoData.Snippet.PublishedAtRaw;

                DateTime.TryParseExact(uploadDateRaw, @"yyyy-MM-dd\THH:mm:ss\Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var uploadDate);

                //likeCount.Text = videoData.Statistics.LikeCount != null ? Convert.ToDouble(videoData.Statistics.LikeCount).ToMetric(decimals: 2) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).RawLikes).ToMetric(decimals: 2);
                //commentsContainerTitle.Text = NekoPlayerStrings.Comments(videoData.Statistics.CommentCount != null ? Convert.ToInt32(videoData.Statistics.CommentCount).ToStandardFormattedString(0) : NekoPlayerStrings.Disabled);
                videoInfoDetails.Text = NekoPlayerStrings.VideoMetadataDescWithoutChannelName(Convert.ToInt32(videoData.Statistics.ViewCount).ToStandardFormattedString(0), uploadDate.ToString());

                updateComments(videoId);

                refreshLikeDislikeCount(videoId);

                Schedule(() =>
                {
                    updatePresence(discordRichPresence.Value);

                    videoMetadataDisplayDetails.SubscribeClickAction = () =>
                    {
                        Task.Run(async () =>
                        {
                            if (!googleOAuth2.SignedIn.Value)
                                return; //log in to more actions

                            bool result = await api.IsChannelSubscribed(videoData.Snippet.ChannelId);
                            string subscriptionId = await api.GetSubscriptionId(videoData.Snippet.ChannelId);

                            Logger.Log("SubscribeClickAction clicked");

                            if (result)
                            {
                                Schedule(() => youtubeChannelMetadataDisplay.UpdateUser(api.GetChannel(videoData.Snippet.ChannelId)));

                                declineButton.Action = async () =>
                                {
                                    hideOverlayContainer(unsubscribeDialog);
                                };
                                acceptButton.Action = async () =>
                                {
                                    hideOverlayContainer(unsubscribeDialog);
                                    await api.UnsubscribeChannel(subscriptionId);

                                    Logger.Log("UnsubscribeChannel()");

                                    ToastBase toast = new ToastWithIcon(NekoPlayerStrings.General, NekoPlayerStrings.SubscriptionRemoved, FontAwesome.Solid.SignOutAlt);

                                    Schedule(() => onScreenDisplay.Display(toast));
                                    Schedule(() => videoMetadataDisplayDetails.UpdateChannelSubscribeState(videoData.Snippet.ChannelId));
                                };

                                Schedule(() =>
                                {
                                    hideOverlays();
                                    showOverlayContainer(unsubscribeDialog);
                                });
                            }
                            else
                            {
                                await api.SubscribeChannel(videoData.Snippet.ChannelId);

                                Logger.Log("SubscribeChannel()");

                                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.General, NekoPlayerStrings.SubscriptionAdded, FontAwesome.Solid.SignInAlt);

                                Schedule(() => onScreenDisplay.Display(toast));
                                Schedule(() => videoMetadataDisplayDetails.UpdateChannelSubscribeState(videoData.Snippet.ChannelId));
                            }
                        });
                    };

                    reportButton.Action = () =>
                    {
                        if (!googleOAuth2.SignedIn.Value)
                            return;

                        ToastBase toast = new ToastWithIcon(NekoPlayerStrings.Report, NekoPlayerStrings.ReportSuccess, FontAwesome.Solid.CheckCircle);
                        api.ReportAbuse(videoId, reportReason.Current.Value.Id, (reportReason.Current.Value.ContainsSecondaryReasons ? reportSubReason.Current.Value.Id : null), (!string.IsNullOrEmpty(reportComment.Current.Value) ? reportComment.Current.Value : null));
                        Schedule(() => onScreenDisplay.Display(toast));
                        reportComment.Current.Value = string.Empty;
                        reportReason.Current.Value = reportReason.Items.ToArray()[0];
                        reportSubReason.Current.Value = reportSubReason.Items.ToArray()[0];
                        hideOverlayContainer(reportAbuseOverlay);
                    };

                    commentSendButton.ClickAction = _ =>
                    {
                        if (videoData == null)
                            return;

                        if (!googleOAuth2.SignedIn.Value)
                            return;

                        ToastBase toast = new ToastWithIcon(NekoPlayerStrings.General, NekoPlayerStrings.CommentAdded, FontAwesome.Regular.Comment);
                        api.SendComment(videoId, commentTextBox.Text);

                        Scheduler.AddDelayed(() => updateComments(videoId), 2000);

                        Schedule(() => onScreenDisplay.Display(toast));

                        commentTextBox.Text = string.Empty;
                    };
                });

                CommentsSort.BindValueChanged(sort =>
                {
                    updateComments(videoId);
                });

                usernameDisplayMode.BindValueChanged(locale =>
                {
                    Schedule(() =>
                    {
                        updateWindowTitle();
                        //game.RequestUpdateWindowTitle($"{api.GetLocalizedChannelTitle(api.GetChannel(videoData.Snippet.ChannelId))} - {api.GetLocalizedVideoTitle(videoData)}");
                        if (api.TryToGetMineChannel() != null)
                            commentTextBox.PlaceholderText = NekoPlayerStrings.CommentWith;
                    });
                }, true);

                showVideoMetadataOnWindowTitle.BindValueChanged(_ =>
                {
                    Schedule(() =>
                    {
                        updateWindowTitle();
                    });
                }, true);

                localeBindable.BindValueChanged(locale =>
                {
                    Task.Run(async () =>
                    {
                        Schedule(() =>
                        {
                            updateWindowTitle();
                            //game.RequestUpdateWindowTitle($"{api.GetLocalizedChannelTitle(api.GetChannel(videoData.Snippet.ChannelId))} - {api.GetLocalizedVideoTitle(videoData)}");
                            if (!string.IsNullOrEmpty(api.GetLocalizedVideoDescription(videoData)))
                            {
                                //Schedule(() => videoDescription.Text = api.GetLocalizedVideoDescription(videoData));
                                Schedule(() => GetLocalizedVideoDescriptionRemake(videoData));
                            }
                            else
                            {
                                videoDescription.Text = string.Empty;
                                Schedule(() => videoDescription.AddText(NekoPlayerStrings.NoDescription, text =>
                                {
                                    text.Font = NekoPlayerApp.DefaultFont.With(weight: "SemiBold");
                                    text.Colour = overlayColourProvider1.Background1;
                                }));
                            }
                            videoInfoDetails.Text = NekoPlayerStrings.VideoMetadataDescWithoutChannelName(Convert.ToInt32(videoData.Statistics.ViewCount).ToStandardFormattedString(0), uploadDate.ToString());
                        });
                    });
                });

                if (googleOAuth2.SignedIn.Value)
                {
                    try
                    {
                        foreach (var item in myPlaylistsDropdown.Items)
                        {
                            bool result = await api.IsVideoExistsOnPlaylist(item.Id, videoId);
                            Schedule(() => saveVideoOpenButton.Icon = result ? FontAwesome.Solid.Bookmark : FontAwesome.Regular.Bookmark);
                        }
                    }
                    catch
                    {
                    }
                }

                TimeSpan duration = XmlConvert.ToTimeSpan(videoData.ContentDetails.Duration);
                videoDuration = duration;
                if (duration.Hours > 0)
                {
                    totalTimeText = $"{duration.Hours.ToString("0")}:{duration.Minutes.ToString("00")}:{duration.Seconds.ToString("00")}";
                }
                else
                {
                    totalTimeText = $"{duration.Minutes.ToString("0")}:{duration.Seconds.ToString("00")}";
                }
            });
        }

        private string totalTimeText;

        private TimeSpan videoDuration;

        private Bindable<UsernameDisplayMode> usernameDisplayMode;

        private Bindable<bool> showVideoMetadataOnWindowTitle;

        private void updateWindowTitle()
        {
            if ((showVideoMetadataOnWindowTitle.Value) && (videoData != null))
            {
                game.RequestUpdateWindowTitle($"{TruncateWithEllipsis(api.GetLocalizedChannelTitle(api.GetChannel(videoData.Snippet.ChannelId)), 40)} - {api.GetLocalizedVideoTitle(videoData)}");
            }
            else
            {
                game.RequestUpdateWindowTitle(string.Empty);
            }
        }

        private void addVideoToScreen()
        {
            //Task.Run(async () => await api.SendPlayerResponseAsync(videoId));

            string audioFile = app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/audio.ogg";

            AudioNormalization audioNormalization = new AudioNormalization(audioFile);

            if (audioNormalization.IntegratedLoudness == null)
            {
                Logger.Log($"Failed to calculate audio normalization values for {api.GetChannel(videoData.Snippet.ChannelId).Snippet.Title} - {videoData.Snippet.Title}", LoggingTarget.Runtime, LogLevel.Error);
            }

            app.CurrentTrackNormalizeVolume.Value = audioNormalization?.IntegratedLoudnessInVolumeOffset ?? AudioNormalizationManager.FALLBACK_VOLUME;

            videoContainer.Add(currentVideoSource);

            Schedule(() => videoLoadingProgress.Text = "");

            videoProgress.BindValueChanged(seek =>
            {
                if (currentVideoSource != null && currentVideoSource.IsPlaying() == false)
                    seekTo(seek.NewValue * 1000);
            });

            playbackSpeed.BindValueChanged(speed =>
            {
                setPlaybackSpeed(speed.NewValue);
            }, true);

            if (playlists.Count > 0)
            {
                currentVideoSource.OnVideoCompleted = async () =>
                {
                    if (repeat.Value)
                    {
                        currentVideoSource.Play();
                        return;
                    }

                    if (playlistItemIndex != playlists.Count - 1)
                        playlistItemIndex++;

                    Schedule(async () =>
                    {
                        SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId);
                    });
                };
            }
            else
            {
                currentVideoSource.OnVideoCompleted = async () =>
                {
                    if (!repeat.Value)
                        return;

                    currentVideoSource.Play();
                };
            }

            updatePresence(discordRichPresence.Value);
        }

        private void seekTo(double pos)
        {
            currentVideoSource?.SeekTo(pos);
        }

        private void setPlaybackSpeed(double speed)
        {
            currentVideoSource?.SetPlaybackSpeed(speed);
        }

        private void playVideo()
        {
            currentVideoSource.Play();
        }

        private string videoUrl = string.Empty;
        private string videoId = string.Empty;
        private double pausedTime = 0;

        private string videoFile = string.Empty;

        [Resolved]
        private NekoPlayerConfigManager appGlobalConfig { get; set; }

        public void SetVideoSource(string videoId, bool clearCache = false, LoadType loadType = LoadType.Full)
        {
            videoIdBox.Text = string.Empty;
            if (loadVideoContainer.IsVisible == true)
            {
                Schedule(() => hideOverlayContainer(loadVideoContainer));
            }
            if (searchContainer.IsVisible == true)
            {
                Schedule(() => hideOverlayContainer(searchContainer));
            }
            if (playlistOverlay.IsVisible == true)
            {
                Schedule(() => hideOverlayContainer(playlistOverlay));
            }

            if (isVideoLoading)
                return;

            if (videoLoadProcess != null)
                videoLoadProcess.Cancel();

            videoLoadProcess = new CancellationTokenSource();
            CancellationToken cancellationToken = videoLoadProcess.Token;
            Task.Run(() =>
            {
                Schedule(async () =>
                {
                    loadBtnOverlayShow.Enabled.Value = false;
                    if (string.IsNullOrEmpty(videoId))
                    {
                        ToastBase toast = new ToastWithIcon(NekoPlayerStrings.General, NekoPlayerStrings.NoVideoIdError, FontAwesome.Solid.MinusCircle);

                        onScreenDisplay.Display(toast);
                        return;
                    }

                    try
                    {
                        YoutubeExplode.Videos.VideoId.Parse(videoId);
                    }
                    catch (Exception e)
                    {
                        return;
                    }
                    this.videoId = YoutubeExplode.Videos.VideoId.Parse(videoId);
                    //ClearPlaylistItems();
                    pausedTime = clearCache ? currentVideoSource.VideoProgress.Value : 0;
                    Schedule(() => currentVideoSource?.Expire());
                    CommentsSort.UnbindEvents();

                    if (playlists.Count > 0)
                    {
                        if (playlistItemIndex == playlists.Count - 1)
                        {
                            Schedule(() => nextVideoButton.Enabled.Value = false);
                        }
                        else
                        {
                            Schedule(() => nextVideoButton.Enabled.Value = true);
                        }

                        if (playlistItemIndex == 0)
                        {
                            Schedule(() => prevVideoButton.Enabled.Value = false);
                        }
                        else
                        {
                            Schedule(() => prevVideoButton.Enabled.Value = true);
                        }
                    }
                    else
                    {
                        Schedule(() => prevVideoButton.Enabled.Value = false);
                        Schedule(() => nextVideoButton.Enabled.Value = false);
                    }

                    if (playlistItemViews.Count > 0)
                    {
                        foreach (PlaylistItemView playlistItemView in playlistItemViews)
                        {
                            playlistItemView.UpdateState(false);
                        }

                        playlistItemViews[playlistItemIndex].UpdateState(true);
                    }

                    foreach (var item in commentContainer.Children)
                    {
                        Schedule(() => item.Expire());
                    }

                    Schedule(() => videoProgress.Value = 0);

                    if (clearCache == true)
                    {
                        await Task.Delay(1000); // Wait for any ongoing operations to complete
                        switch (loadType)
                        {
                            case LoadType.Full:
                            {
                                foreach (var cacheItem in Directory.GetFiles(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}")))
                                {
                                    File.Delete(cacheItem);
                                }
                                break;
                            }
                            case LoadType.VideoOnly:
                            {
                                File.Delete(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/video.webm");
                                break;
                            }
                            case LoadType.AudioOnly:
                            {
                                File.Delete(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/audio.ogg");
                                break;
                            }
                        }
                    }

                    if (videoId.Length != 0)
                    {
                        Google.Apis.YouTube.v3.Data.Video videoData = api.GetVideo(this.videoId);

                        if (videoData.Status.PrivacyStatus == "private")
                        {
                            Schedule(() =>
                            {
                                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.General, NekoPlayerStrings.CannotPlayPrivateVideos, FontAwesome.Solid.MinusCircle);

                                onScreenDisplay.Display(toast);
                            });
                            return;
                        }

                        IProgress<double> audioDownloadProgress = new Progress<double>((percent) => Schedule(() => videoLoadingProgress.Text = NekoPlayerStrings.DownloadingAudioStream($"{(percent * 100):N0}%")));
                        IProgress<double> videoDownloadProgress = new Progress<double>((percent) => Schedule(() => videoLoadingProgress.Text = NekoPlayerStrings.DownloadingVideoStream($"{(percent * 100):N0}%")));

                        spinnerShow = Scheduler.AddDelayed(spinner.Show, 0);

                        Schedule(() => videoProgress.MaxValue = 1);
                        videoUrl = $"https://youtube.com/watch?v={this.videoId}";

                        if (loadType == LoadType.Full)
                        {
                            Schedule(() => updateVideoMetadata(this.videoId));
                        }
                        Schedule(() => thumbnailContainer.Show());

                        try
                        {
                            await settingsContainer.VideoQualitySettings.RefreshQualityList(videoUrl);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, e.GetDescription());
                        }

                        isVideoLoading = true;
                        if (!File.Exists(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/audio.ogg") || !File.Exists(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/video.webm"))
                        {
                            if (loadType == LoadType.Full)
                                Directory.CreateDirectory(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}"));

                            var streamManifest = await app.YouTubeClient.Videos.Streams.GetManifestAsync(videoUrl);

                            IAudioStreamInfo audioStreamInfo;

                            try
                            {
                                if (audioQuality.Value == Config.AudioQuality.PreferHighQuality)
                                {
                                    if (alwaysUseOriginalAudio.Value == true)
                                    {
                                        Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                                else if (audioQuality.Value == Config.AudioQuality.PreferMp4a)
                                {
                                    if (alwaysUseOriginalAudio.Value == true)
                                    {
                                        Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .Where(s => s.AudioCodec.Contains("mp4a"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()))
                                            .Where(s => s.AudioCodec.Contains("mp4a"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                                else if (audioQuality.Value == Config.AudioQuality.PreferOpus)
                                {
                                    if (alwaysUseOriginalAudio.Value == true)
                                    {
                                        Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .Where(s => s.AudioCodec.Contains("opus"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()))
                                            .Where(s => s.AudioCodec.Contains("opus"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                                else
                                {
                                    if (alwaysUseOriginalAudio.Value == true)
                                    {
                                        Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .First();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()))
                                            .First();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    /*
                                    // Select best audio stream (highest bitrate)
                                    audioStreamInfo = streamManifest
                                        .GetAudioOnlyStreams()
                                        .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                        .TryGetWithHighestBitrate();
                                    */

                                    if (audioQuality.Value == Config.AudioQuality.PreferHighQuality)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else if (audioQuality.Value == Config.AudioQuality.PreferMp4a)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .Where(s => s.AudioCodec.Contains("mp4a"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else if (audioQuality.Value == Config.AudioQuality.PreferOpus)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .Where(s => s.AudioCodec.Contains("opus"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        audioStreamInfo = streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .First();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }

                                    Logger.Error(e, e.GetDescription());
                                    Logger.Log($"Prefer default audio language: {videoData.Snippet.DefaultLanguage}");
                                }
                                catch
                                {
                                    Logger.Log($"Prefer default audio language failed.\nFalling back to default audio language.");
                                    // Select best audio stream (highest bitrate)
                                    /*
                                    audioStreamInfo = streamManifest
                                        .GetAudioOnlyStreams()
                                        .TryGetWithHighestBitrate();
                                    */

                                    if (audioQuality.Value == Config.AudioQuality.PreferHighQuality)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else if (audioQuality.Value == Config.AudioQuality.PreferMp4a)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioCodec.Contains("mp4a"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else if (audioQuality.Value == Config.AudioQuality.PreferOpus)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioCodec.Contains("opus"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        audioStreamInfo = streamManifest
                                            .GetAudioOnlyStreams()
                                            .First();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                            }

                            IVideoStreamInfo videoStreamInfo;

                            try
                            {
                                // Select best video stream (1080p60 in this example)
                                videoStreamInfo = streamManifest
                                    .GetVideoOnlyStreams()
                                    .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.WebM)
                                    .Where(s => s.VideoQuality.Label.Contains(settingsContainer.VideoQualitySettings.Current.Value))
                                    .First();

                                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.VideoQuality, videoStreamInfo.VideoQuality.Label, FontAwesome.Solid.Video);

                                onScreenDisplay.Display(toast);
                                settingsContainer.VideoQualitySettings.Caption = NekoPlayerStrings.VideoQualityWithLabel($"{videoStreamInfo.VideoQuality.Label}, {videoStreamInfo.VideoCodec}, {videoStreamInfo.VideoQuality.Framerate}fps");
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    Logger.Error(e, e.GetDescription());
                                    // Select best video stream (1080p60 in this example)
                                    videoStreamInfo = streamManifest
                                        .GetVideoOnlyStreams()
                                        .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.WebM)
                                        .TryGetWithHighestVideoQuality();

                                    ToastBase toast = new ToastWithIcon(NekoPlayerStrings.VideoQuality, videoStreamInfo.VideoQuality.Label, FontAwesome.Solid.Video);

                                    onScreenDisplay.Display(toast);
                                    settingsContainer.VideoQualitySettings.Caption = NekoPlayerStrings.VideoQualityWithLabel($"{videoStreamInfo.VideoQuality.Label}, {videoStreamInfo.VideoCodec}, {videoStreamInfo.VideoQuality.Framerate}fps");
                                }
                                catch (Exception e2)
                                {
                                    Logger.Error(e2, e2.GetDescription());
                                    // Select best video stream (1080p60 in this example)
                                    videoStreamInfo = streamManifest
                                        .GetVideoOnlyStreams()
                                        .Where(s => s.VideoQuality.Label.Contains(settingsContainer.VideoQualitySettings.Current.Value))
                                        .First();

                                    ToastBase toast = new ToastWithIcon(NekoPlayerStrings.VideoQuality, videoStreamInfo.VideoQuality.Label, FontAwesome.Solid.Video);

                                    onScreenDisplay.Display(toast);
                                    settingsContainer.VideoQualitySettings.Caption = NekoPlayerStrings.VideoQualityWithLabel($"{videoStreamInfo.VideoQuality.Label}, {videoStreamInfo.VideoCodec}, {videoStreamInfo.VideoQuality.Framerate}fps");
                                }
                            }

                            if (videoStreamInfo.VideoCodec.Contains("avc1"))
                            {
                                videoFile = app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/video.mp4";
                            }
                            else
                            {
                                videoFile = app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/video.webm";
                            }

                            try
                            {
                                await settingsContainer.CaptionLangDropdown.RefreshCaptionLanguages(videoUrl);
                                captionEnabled.Disabled = false;
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e, e.GetDescription());
                            }

                            ClosedCaptionTrack captionTrack = null;

                            try
                            {
                                if (captionEnabled.Value)
                                {
                                    var trackManifest = await game.YouTubeClient.Videos.ClosedCaptions.GetManifestAsync(videoUrl);

                                    if (trackManifest.Tracks.Count == 0)
                                    {
                                        captionEnabled.Value = false;
                                        captionEnabled.Disabled = true;
                                    }

                                    string preferedLang = string.Empty;

                                    if (settingsContainer.CaptionLangDropdown.Current.Value != null)
                                    {
                                        preferedLang = settingsContainer.CaptionLangDropdown.Current.Value.Hl.ToString();
                                    }
                                    else
                                    {
                                        preferedLang = CultureInfo.CurrentCulture.Name;
                                    }

                                    settingsContainer.CaptionLangDropdown.Current.Value = settingsContainer.CaptionLangDropdown.Items.Where(lang => lang.Hl.Contains(preferedLang)).First();

                                    var trackInfo = trackManifest.Tracks.Where(track => track.Language.Code.Contains(preferedLang)).First();

                                    if (trackInfo != null)
                                    {
                                        if (captionEnabled.Value)
                                        {
                                            Schedule(() =>
                                            {
                                                /*
                                                alert.Text = captionLanguage.Value != ClosedCaptionLanguage.Disabled ? (trackInfo.IsAutoGenerated ? NekoPlayerStrings.SelectedCaptionAutoGen(captionLanguage.Value.GetLocalisableDescription()) : NekoPlayerStrings.SelectedCaption(captionLanguage.Value.GetLocalisableDescription())) : NekoPlayerStrings.SelectedCaption(captionLanguage.Value.GetLocalisableDescription());
                                                alert.Show();
                                                spinnerShow = Scheduler.AddDelayed(alert.Hide, 3000);
                                                */

                                                try
                                                {
                                                    ToastBase toast = new ToastWithIcon(NekoPlayerStrings.CaptionLanguage, settingsContainer.CaptionLangDropdown.Current.Value.Name, FontAwesome.Solid.ClosedCaptioning);
                                                    onScreenDisplay.Display(toast);
                                                }
                                                catch (Exception e)
                                                {
                                                    Logger.Error(e, e.GetDescription());
                                                }
                                            });
                                        }

                                        captionTrack = await game.YouTubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);
                                    }
                                }
                                else
                                {
                                    currentVideoSource?.UpdateCaptionTrack(null);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e, e.GetDescription());
                            }

                            switch (loadType)
                            {
                                case LoadType.Full:
                                {
                                    await app.YouTubeClient.Videos.DownloadAsync([audioStreamInfo], new ConversionRequestBuilder(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"\audio.ogg").SetFFmpegPath(app.GetFFmpegPath()).Build(), audioDownloadProgress);
                                    await app.YouTubeClient.Videos.DownloadAsync([videoStreamInfo], new ConversionRequestBuilder(videoFile).SetFFmpegPath(app.GetFFmpegPath()).Build(), videoDownloadProgress);
                                    break;
                                }
                                case LoadType.AudioOnly:
                                {
                                    await app.YouTubeClient.Videos.DownloadAsync([audioStreamInfo], new ConversionRequestBuilder(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"\audio.ogg").SetFFmpegPath(app.GetFFmpegPath()).Build(), audioDownloadProgress);
                                    break;
                                }
                                case LoadType.VideoOnly:
                                {
                                    await app.YouTubeClient.Videos.DownloadAsync([videoStreamInfo], new ConversionRequestBuilder(videoFile).SetFFmpegPath(app.GetFFmpegPath()).Build(), videoDownloadProgress);
                                    break;
                                }
                            }

                            currentVideoSource = new YouTubeVideoPlayer(videoFile, app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/audio.ogg", captionTrack, videoData, pausedTime)
                            {
                                RelativeSizeAxes = Axes.Both
                            };

                            spinnerShow = Scheduler.AddDelayed(spinner.Hide, 0);

                            spinnerShow = Scheduler.AddDelayed(addVideoToScreen, 0);

                            spinnerShow = Scheduler.AddDelayed(() => playVideo(), 0);
                            Schedule(() => thumbnailContainer.Hide());
                            isVideoLoading = false;
                            loadBtnOverlayShow.Enabled.Value = true;
                        }
                        else
                        {
                            await settingsContainer.CaptionLangDropdown.RefreshCaptionLanguages(videoUrl);
                            captionEnabled.Disabled = false;

                            var streamManifest = await app.YouTubeClient.Videos.Streams.GetManifestAsync(videoUrl);

                            IAudioStreamInfo audioStreamInfo;

                            try
                            {
                                if (audioQuality.Value == Config.AudioQuality.PreferHighQuality)
                                {
                                    if (alwaysUseOriginalAudio.Value == true)
                                    {
                                        Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                                else if (audioQuality.Value == Config.AudioQuality.PreferMp4a)
                                {
                                    if (alwaysUseOriginalAudio.Value == true)
                                    {
                                        Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .Where(s => s.AudioCodec.Contains("mp4a"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()))
                                            .Where(s => s.AudioCodec.Contains("mp4a"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                                else if (audioQuality.Value == Config.AudioQuality.PreferOpus)
                                {
                                    if (alwaysUseOriginalAudio.Value == true)
                                    {
                                        Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .Where(s => s.AudioCodec.Contains("opus"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()))
                                            .Where(s => s.AudioCodec.Contains("opus"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                                else
                                {
                                    if (alwaysUseOriginalAudio.Value == true)
                                    {
                                        Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .First();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()}");
                                        // Select best audio stream (highest bitrate)
                                        audioStreamInfo = streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(NekoPlayerSetting.AudioLanguage).ToString()))
                                            .First();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    /*
                                    // Select best audio stream (highest bitrate)
                                    audioStreamInfo = streamManifest
                                        .GetAudioOnlyStreams()
                                        .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                        .TryGetWithHighestBitrate();
                                    */

                                    if (audioQuality.Value == Config.AudioQuality.PreferHighQuality)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else if (audioQuality.Value == Config.AudioQuality.PreferMp4a)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .Where(s => s.AudioCodec.Contains("mp4a"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else if (audioQuality.Value == Config.AudioQuality.PreferOpus)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .Where(s => s.AudioCodec.Contains("opus"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        audioStreamInfo = streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                            .First();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }

                                    Logger.Error(e, e.GetDescription());
                                    Logger.Log($"Prefer default audio language: {videoData.Snippet.DefaultLanguage}");
                                }
                                catch
                                {
                                    Logger.Log($"Prefer default audio language failed.\nFalling back to default audio language.");
                                    // Select best audio stream (highest bitrate)
                                    /*
                                    audioStreamInfo = streamManifest
                                        .GetAudioOnlyStreams()
                                        .TryGetWithHighestBitrate();
                                    */

                                    if (audioQuality.Value == Config.AudioQuality.PreferHighQuality)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else if (audioQuality.Value == Config.AudioQuality.PreferMp4a)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioCodec.Contains("mp4a"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else if (audioQuality.Value == Config.AudioQuality.PreferOpus)
                                    {
                                        audioStreamInfo = (IAudioStreamInfo)streamManifest
                                            .GetAudioOnlyStreams()
                                            .Where(s => s.AudioCodec.Contains("opus"))
                                            .TryGetWithHighestBitrate();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                    else
                                    {
                                        audioStreamInfo = streamManifest
                                            .GetAudioOnlyStreams()
                                            .First();
                                        settingsContainer.AudioQualitySettings.Caption = NekoPlayerStrings.AudioQualityWithLabel($"{audioStreamInfo.AudioCodec}, {audioStreamInfo.Bitrate.KiloBitsPerSecond:N0}kbps");
                                    }
                                }
                            }

                            IVideoStreamInfo videoStreamInfo;

                            try
                            {
                                // Select best video stream (1080p60 in this example)
                                videoStreamInfo = streamManifest
                                    .GetVideoOnlyStreams()
                                    .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.WebM)
                                    .Where(s => s.VideoQuality.Label.Contains(settingsContainer.VideoQualitySettings.Current.Value))
                                    .First();

                                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.VideoQuality, videoStreamInfo.VideoQuality.Label, FontAwesome.Solid.Video);

                                onScreenDisplay.Display(toast);
                                settingsContainer.VideoQualitySettings.Caption = NekoPlayerStrings.VideoQualityWithLabel($"{videoStreamInfo.VideoQuality.Label}, {videoStreamInfo.VideoCodec}, {videoStreamInfo.VideoQuality.Framerate}fps");
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    Logger.Error(e, e.GetDescription());
                                    // Select best video stream (1080p60 in this example)
                                    videoStreamInfo = streamManifest
                                        .GetVideoOnlyStreams()
                                        .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.WebM)
                                        .TryGetWithHighestVideoQuality();

                                    ToastBase toast = new ToastWithIcon(NekoPlayerStrings.VideoQuality, videoStreamInfo.VideoQuality.Label, FontAwesome.Solid.Video);

                                    onScreenDisplay.Display(toast);
                                    settingsContainer.VideoQualitySettings.Caption = NekoPlayerStrings.VideoQualityWithLabel($"{videoStreamInfo.VideoQuality.Label}, {videoStreamInfo.VideoCodec}, {videoStreamInfo.VideoQuality.Framerate}fps");
                                }
                                catch (Exception e2)
                                {
                                    Logger.Error(e2, e2.GetDescription());
                                    // Select best video stream (1080p60 in this example)
                                    videoStreamInfo = streamManifest
                                        .GetVideoOnlyStreams()
                                        .Where(s => s.VideoQuality.Label.Contains(settingsContainer.VideoQualitySettings.Current.Value))
                                        .First();

                                    ToastBase toast = new ToastWithIcon(NekoPlayerStrings.VideoQuality, videoStreamInfo.VideoQuality.Label, FontAwesome.Solid.Video);

                                    onScreenDisplay.Display(toast);
                                    settingsContainer.VideoQualitySettings.Caption = NekoPlayerStrings.VideoQualityWithLabel($"{videoStreamInfo.VideoQuality.Label}, {videoStreamInfo.VideoCodec}, {videoStreamInfo.VideoQuality.Framerate}fps");
                                }
                            }

                            ClosedCaptionTrack captionTrack = null;

                            try
                            {
                                if (captionEnabled.Value)
                                {
                                    var trackManifest = await game.YouTubeClient.Videos.ClosedCaptions.GetManifestAsync(videoUrl);

                                    if (trackManifest.Tracks.Count == 0)
                                    {
                                        captionEnabled.Value = false;
                                        captionEnabled.Disabled = true;
                                    }

                                    string preferedLang = string.Empty;

                                    if (settingsContainer.CaptionLangDropdown.Current.Value != null)
                                    {
                                        preferedLang = settingsContainer.CaptionLangDropdown.Current.Value.Hl.ToString();
                                    }
                                    else
                                    {
                                        preferedLang = CultureInfo.CurrentCulture.Name;
                                    }

                                    settingsContainer.CaptionLangDropdown.Current.Value = settingsContainer.CaptionLangDropdown.Items.Where(lang => lang.Hl.Contains(preferedLang)).First();

                                    var trackInfo = trackManifest.Tracks.Where(track => track.Language.Code.Contains(preferedLang)).First();

                                    if (trackInfo != null)
                                    {
                                        if (captionEnabled.Value)
                                        {
                                            Schedule(() =>
                                            {
                                                try
                                                {
                                                    ToastBase toast = new ToastWithIcon(NekoPlayerStrings.CaptionLanguage, settingsContainer.CaptionLangDropdown.Current.Value.Name, FontAwesome.Solid.ClosedCaptioning);
                                                    onScreenDisplay.Display(toast);
                                                }
                                                catch (Exception e)
                                                {
                                                    Logger.Error(e, e.GetDescription());
                                                }
                                            });
                                        }

                                        captionTrack = await game.YouTubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);
                                    }
                                }
                                else
                                {
                                    currentVideoSource?.UpdateCaptionTrack(null);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e, e.GetDescription());
                            }

                            if (videoStreamInfo.VideoCodec.Contains("avc1"))
                            {
                                videoFile = app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/video.mp4";
                            }
                            else
                            {
                                videoFile = app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/video.webm";
                            }

                            currentVideoSource = new YouTubeVideoPlayer(videoFile, app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{this.videoId}") + @"/audio.ogg", captionTrack, videoData, pausedTime)
                            {
                                RelativeSizeAxes = Axes.Both
                            };

                            spinnerShow = Scheduler.AddDelayed(spinner.Hide, 0);

                            spinnerShow = Scheduler.AddDelayed(addVideoToScreen, 0);

                            spinnerShow = Scheduler.AddDelayed(() => playVideo(), 0);
                            Schedule(() => thumbnailContainer.Hide());
                            isVideoLoading = false;
                            loadBtnOverlayShow.Enabled.Value = true;
                        }
                    }
                    else
                    {
                        /*
                        Toast toast = new Toast(NekoPlayerStrings.General, NekoPlayerStrings.NoVideoIdError);

                        onScreenDisplay.Display(toast);
                        */
                    }
                });
            }, cancellationToken).FireAndForget();
        }

        [Resolved]
        private OnScreenDisplay onScreenDisplay { get; set; }

        [Resolved]
        private NekoPlayerApp game { get; set; }

        public void ShowSettingsOverlayAtName(string name)
        {
            if (!settingsContainer.IsVisible)
                showOverlayContainer(settingsContainer);

            settingsContainer.ShowSettingsOverlayAtName(name);
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        public void OpenSettings()
        {
            Schedule(() =>
            {
                hideOverlays();
                showOverlayContainer(settingsContainer);
            });
        }

        public void TogglePreservePitch()
        {
            Schedule(() => adjustPitch.Value = !adjustPitch.Value);
        }

        public void SelectPlaylist(string id)
        {
            Task.Run(async () => SetPlaylist(id));
        }

        public void OpenMyPlaylists()
        {
            Schedule(() =>
            {
                hideOverlays();
                showOverlayContainer(myPlaylistsOverlay);
            });
        }

        public void OpenAudioEffects()
        {
            Schedule(() =>
            {
                hideOverlays();
                showOverlayContainer(audioEffectsOverlay);
            });
        }

        public void SelectVideo(string id)
        {
            Schedule(() => hideOverlays());
            ClearPlaylistItems();
            Task.Run(async () =>
            {
                Schedule(async () =>
                {
                    SetVideoSource(id);
                });
            });
        }

        public enum LoadType
        {
            Full,
            VideoOnly,
            AudioOnly,
        }
    }
}
