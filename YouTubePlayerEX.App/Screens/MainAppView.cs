// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AngleSharp.Common;
using DiscordRPC;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Humanizer;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.ObjectExtensions;
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
using osu.Framework.Platform.Windows;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using SharpCompress.Archives.Zip;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;
using YouTubePlayerEX.App.Audio;
using YouTubePlayerEX.App.Config;
using YouTubePlayerEX.App.Extensions;
using YouTubePlayerEX.App.Graphics;
using YouTubePlayerEX.App.Graphics.Containers;
using YouTubePlayerEX.App.Graphics.Shaders;
using YouTubePlayerEX.App.Graphics.Sprites;
using YouTubePlayerEX.App.Graphics.UserInterface;
using YouTubePlayerEX.App.Graphics.UserInterfaceV2;
using YouTubePlayerEX.App.Graphics.Videos;
using YouTubePlayerEX.App.Input;
using YouTubePlayerEX.App.Input.Binding;
using YouTubePlayerEX.App.Localisation;
using YouTubePlayerEX.App.Online;
using YouTubePlayerEX.App.Overlays;
using YouTubePlayerEX.App.Overlays.OSD;
using YouTubePlayerEX.App.Overlays.Volume;
using YouTubePlayerEX.App.Updater;
using YouTubePlayerEX.App.Utils;
using static YouTubePlayerEX.App.YouTubePlayerEXApp;
using Container = osu.Framework.Graphics.Containers.Container;
using Language = YouTubePlayerEX.App.Localisation.Language;
using OverlayContainer = YouTubePlayerEX.App.Graphics.Containers.OverlayContainer;

namespace YouTubePlayerEX.App.Screens
{
    public partial class MainAppView : YouTubePlayerEXScreen, IKeyBindingHandler<GlobalAction>
    {
        private BufferedContainer videoContainer;
        private AdaptiveButton loadBtn, commentSendButton, searchButton, loadPlaylistBtn, loadPlaylistOpenButton, prevVideoButton, nextVideoButton, declineButton, acceptButton;
        private AdaptiveTextBox videoIdBox, commentTextBox, searchTextBox, playlistIdBox;
        private LoadingSpinner spinner;
        private ScheduledDelegate spinnerShow;
        private AdaptiveAlertContainer alert;
        private IdleTracker idleTracker;
        private Container uiContainer;
        private Container uiGradientContainer;
        private OverlayContainer loadVideoContainer, settingsContainer, videoDescriptionContainer, commentsContainer, videoInfoExpertOverlay, searchContainer, reportAbuseOverlay, loadPlaylistContainer, unsubscribeDialog, addPlaylistOverlay, videoSaveLocationOverlay;
        private SideOverlayContainer playlistOverlay, audioEffectsOverlay, menuOverlay;
        private AdaptiveButtonWithShadow menuOverlayShow;
        private MenuButtonItem loadBtnOverlayShow, settingsOverlayShowBtn, commentOpenButton, searchOpenButton, reportOpenButton, playlistOpenButton, audioEffectsOpenButton, saveVideoOpenButton;
        private VideoMetadataDisplayWithoutProfile videoMetadataDisplay;
        private VideoMetadataDisplay videoMetadataDisplayDetails;
        private RoundedButtonContainer commentOpenButtonDetails, likeButton;

        private FormEnumDropdown<PrivacyStatus> playlistPrivacyStatusDropdown;

        private LinkFlowContainer madeByText;

        private YouTubeChannelMetadataDisplay youtubeChannelMetadataDisplay;

        private SettingsItemV2 audioLanguageItem, wasapiExperimentalItem, captionLangOptions;

        private Sample overlayShowSample;
        private Sample overlayHideSample;
        private AdaptiveButtonV2 reportButton;
        private FormTextBox reportComment, playlistTitleBox;

        private FormDropdown<Playlist> myPlaylistsDropdown;

        private Container overlayFadeContainer;
        private RoundedButtonContainer dislikeButton;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // Be sure to dispose the track, otherwise memory will be leaked!
            // This is automatic for DrawableTrack.
            overlayShowSample.Dispose();
            overlayHideSample.Dispose();

            if (audio.IsNotNull())
            {
                audio.OnNewDevice -= onAudioDeviceChanged;
                audio.OnLostDevice -= onAudioDeviceChanged;
            }
        }

        private void onAudioDeviceChanged(string _)
        {
            updateAudioDeviceItems();
        }

        private void updateAudioDeviceItems()
        {
            var deviceItems = new List<string> { string.Empty };
            deviceItems.AddRange(audio.AudioDeviceNames);

            string preferredDeviceName = audio.AudioDevice.Value;
            if (deviceItems.All(kv => kv != preferredDeviceName))
                deviceItems.Add(preferredDeviceName);

            // The option dropdown for audio device selection lists all audio
            // device names. Dropdowns, however, may not have multiple identical
            // keys. Thus, we remove duplicate audio device names from
            // the dropdown. BASS does not give us a simple mechanism to select
            // specific audio devices in such a case anyways. Such
            // functionality would require involved OS-specific code.
            audioDeviceDropdown.Items = deviceItems
                             // Dropdown doesn't like null items. Somehow we are seeing some arrive here (see https://github.com/ppy/osu/issues/21271)
                             .Where(i => i.IsNotNull())
                             .Distinct()
                             .ToList();
        }

        [Resolved]
        private AudioManager audio { get; set; } = null!;

        private AudioDeviceDropdown audioDeviceDropdown = null!;

#nullable enable
        private FormCheckBox? wasapiExperimental;
#nullable disable

        private AdaptiveSpriteText videoLoadingProgress, videoInfoDetails, likeCount, dislikeCount, commentCount, commentsContainerTitle, currentTime, totalTime, playlistName, volumeText;
        private AdaptiveSpriteText speedText;
        private LinkFlowContainer videoDescription, gameVersion;
        private FillFlowContainer commentContainer, searchResultContainer, playlistItemsView;

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

        private void onDisplaysChanged(IEnumerable<Display> displays)
        {
            Scheduler.AddOnce(d =>
            {
                if (!displayDropdown.Items.SequenceEqual(d, DisplayListComparer.DEFAULT))
                    displayDropdown.Items = d;
                updateDisplaySettingsVisibility();
            }, displays);
        }

        private Bindable<Config.VideoQuality> videoQuality;
        private Bindable<HardwareVideoDecoder> hardwareVideoDecoder;
        private Bindable<Localisation.Language> audioLanguage;
        private Bindable<bool> adjustPitch;
        private Bindable<string> localeBindable = new Bindable<string>();
        private FormButton checkForUpdatesButton, login;
        private ThumbnailContainerBackground thumbnailContainer;
        private AdaptiveSliderBar<double> seekbar;
        private Bindable<LocalisableString> updateInfomationText;
        private Bindable<bool> updateButtonEnabled, fpsDisplay, captionEnabled;
        private Bindable<AspectRatioMethod> aspectRatioMethod;
        private Bindable<DiscordRichPresenceMode> discordRichPresence;

        [Resolved]
        private AudioEffectsConfigManager audioEffectsConfig { get; set; } = null!;

        private AdaptiveTextFlowContainer debugInfo;

        private FormEnumDropdown<GCLatencyMode> latencyModeDropdown;

        private BufferedContainer videoScalingContainer;

        private Box likeButtonBackground, dislikeButtonBackground, likeButtonBackgroundSelected, dislikeButtonBackgroundSelected;
        private FillFlowContainer likeButtonForeground, dislikeButtonForeground;

        private Container userInterfaceContainer;

        private Bindable<bool> alwaysUseOriginalAudio;

        [Resolved]
        private AdaptiveColour colours { get; set; } = null!;

        private Bindable<SettingsNote.Data> videoQualityWarning = new Bindable<SettingsNote.Data>();
        private Bindable<SettingsNote.Data> hwAccelNote = new Bindable<SettingsNote.Data>();

        private Bindable<float> scalingBackgroundDim = null!;

        private Bindable<double> speedTextRolling;
        private Bindable<double> volumeTextRolling;

        private BufferedContainer idleBackground;

        private SpriteIcon volumeIcon;

        private LinkFlowContainer dislikeCounterCredits, playlistAuthor;

        private Bindable<bool> signedIn;

        private ParallaxContainer thumbnailContainerBase;

        [Resolved]
        private ShaderManager shaderManager { get; set; } = null!;

        private Bindable<double> videoVolume;

        private YouTubeI18nLangDropdown captionLangDropdown;

#nullable enable
        [Resolved(canBeNull: true)]
        private Online.DiscordRPC? discordRPC { get; set; }
#nullable disable

        //effects
        private Bindable<bool> reverbEnabled, rotateEnabled, echoEnabled, distortionEnabled, karaokeEnabled;
        private FillFlowContainer reverbSettings, rotateSettings, echoSettings, distortionSettings;

        protected T GetShaderByType<T>() where T : InternalShader, new()
            => shaderManager.LocalInternalShader<T>();

        [BackgroundDependencyLoader]
        private void load(ISampleStore sampleStore, FrameworkConfigManager config, YTPlayerEXConfigManager appConfig, GameHost host, Storage storage, OverlayColourProvider overlayColourProvider, TextureStore textures, FrameworkDebugConfigManager debugConfig)
        {
            speedTextRolling = new Bindable<double>(1);
            volumeTextRolling = new Bindable<double>(1);
            appliedEffects.Value = new List<InternalShader>();
            window = host.Window;

            videoVolume = config.GetBindable<double>(FrameworkSetting.VolumeMusic);

            uiVisible = screenshotManager.CursorVisibility.GetBoundCopy();
            signedIn = googleOAuth2.SignedIn.GetBoundCopy();

            isAnyOverlayOpen = sessionStatics.GetBindable<bool>(Static.IsAnyOverlayOpen);
            videoPlaying = sessionStatics.GetBindable<bool>(Static.IsVideoPlaying);

            usernameDisplayMode = appConfig.GetBindable<UsernameDisplayMode>(YTPlayerEXSetting.UsernameDisplayMode);

            var renderer = config.GetBindable<RendererType>(FrameworkSetting.Renderer);
            automaticRendererInUse = renderer.Value == RendererType.Automatic;

            reverbEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.ReverbEnabled);
            rotateEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.RotateEnabled);
            echoEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.EchoEnabled);
            distortionEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.DistortionEnabled);
            karaokeEnabled = audioEffectsConfig.GetBindable<bool>(AudioEffectsSetting.KaraokeEnabled);

            scalingMode = appConfig.GetBindable<ScalingMode>(YTPlayerEXSetting.Scaling);
            scalingSizeX = appConfig.GetBindable<float>(YTPlayerEXSetting.ScalingSizeX);
            scalingSizeY = appConfig.GetBindable<float>(YTPlayerEXSetting.ScalingSizeY);
            scalingPositionX = appConfig.GetBindable<float>(YTPlayerEXSetting.ScalingPositionX);
            scalingPositionY = appConfig.GetBindable<float>(YTPlayerEXSetting.ScalingPositionY);
            scalingBackgroundDim = appConfig.GetBindable<float>(YTPlayerEXSetting.ScalingBackgroundDim);
            alwaysUseOriginalAudio = appConfig.GetBindable<bool>(YTPlayerEXSetting.AlwaysUseOriginalAudio);
            discordRichPresence = appConfig.GetBindable<DiscordRichPresenceMode>(YTPlayerEXSetting.DiscordRichPresence);

            captionEnabled = appConfig.GetBindable<bool>(YTPlayerEXSetting.CaptionEnabled);

            exportStorage = storage.GetStorageForDirectory(@"exports");

            localeBindable = config.GetBindable<string>(FrameworkSetting.Locale);
            fpsDisplay = appConfig.GetBindable<bool>(YTPlayerEXSetting.ShowFpsDisplay);
            adjustPitch = appConfig.GetBindable<bool>(YTPlayerEXSetting.AdjustPitchOnSpeedChange);
            videoQuality = appConfig.GetBindable<Config.VideoQuality>(YTPlayerEXSetting.VideoQuality);
            audioLanguage = appConfig.GetBindable<Localisation.Language>(YTPlayerEXSetting.AudioLanguage);
            hardwareVideoDecoder = config.GetBindable<HardwareVideoDecoder>(FrameworkSetting.HardwareVideoDecoder);
            cursorInWindow = host.Window?.CursorInWindow.GetBoundCopy();
            windowMode = config.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            captionLanguage = appConfig.GetBindable<ClosedCaptionLanguage>(YTPlayerEXSetting.ClosedCaptionLanguage);
            sizeFullscreen = config.GetBindable<Size>(FrameworkSetting.SizeFullscreen);
            sizeWindowed = config.GetBindable<Size>(FrameworkSetting.WindowedSize);
            windowedPositionX = config.GetBindable<double>(FrameworkSetting.WindowedPositionX);
            windowedPositionY = config.GetBindable<double>(FrameworkSetting.WindowedPositionY);
            updateInfomationText = game.UpdateManagerVersionText.GetBoundCopy();
            updateButtonEnabled = game.UpdateButtonEnabled.GetBoundCopy();

            aspectRatioMethod = appConfig.GetBindable<AspectRatioMethod>(YTPlayerEXSetting.AspectRatioMethod);

            windowedResolution.Value = sizeWindowed.Value;

            if (window != null)
            {
                currentDisplay.BindTo(window.CurrentDisplayBindable);
                window.DisplaysChanged += onDisplaysChanged;
            }

            if (host.Renderer is IWindowsRenderer windowsRenderer)
                fullscreenCapability.BindTo(windowsRenderer.FullscreenCapability);

            overlayShowSample = sampleStore.Get(@"overlay-pop-in");
            overlayHideSample = sampleStore.Get(@"overlay-pop-out");
            InternalChildren = new Drawable[]
            {
                idleTracker = new AppIdleTracker(3000),
                videoScalingContainer = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new ScalingContainerNew(ScalingMode.Video)
                    {
                        Children = new Drawable[] {
                            thumbnailContainerBase = new ParallaxContainer
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
                idleBackground = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    BlurSigma = new Vector2(256),
                    Alpha = 0.25f,
                    FrameBufferScale = new Vector2(.4f),
                    Child = new BubbleBackground()
                },
                videoContainer = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                },
                new GlobalScrollAdjustsVolume(),
                userInterfaceContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        spinner = new LoadingSpinner(true, true)
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
                        uiGradientContainer = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(0.35f), Color4.Black.Opacity(0)),
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    RelativeSizeAxes = Axes.X,
                                    Height = 300,
                                },
                                new Box
                                {
                                    Colour = ColourInfo.GradientVertical(Color4.Black.Opacity(0), Color4.Black.Opacity(0.35f)),
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
                                videoMetadataDisplay = new VideoMetadataDisplayWithoutProfile
                                {
                                    Width = 500,
                                    Height = 60,
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    Margin = new MarginPadding
                                    {
                                        Left = 8,
                                    },
                                    ClickEvent = _ => showOverlayContainer(videoDescriptionContainer),
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
                                        menuOverlayShow = new IconButtonWithShadow
                                        {
                                            Enabled = { Value = true },
                                            Origin = Anchor.TopRight,
                                            Anchor = Anchor.TopRight,
                                            Size = new Vector2(40, 40),
                                            Icon = FontAwesome.Solid.Bars,
                                            IconScale = new Vector2(1.2f),
                                            TooltipText = YTPlayerEXStrings.Menu,
                                        },
                                    }
                                },
                                new Container {
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    RelativeSizeAxes = Axes.X,
                                    Height = 100,
                                    Masking = true,
                                    CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                    EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                                    {
                                        Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                        Colour = Color4.Black.Opacity(0.25f),
                                        Offset = new Vector2(0, 2),
                                        Radius = 16,
                                    },
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = overlayColourProvider.Background5,
                                            Alpha = 1f,
                                        },
                                        new FillFlowContainer {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding(16),
                                            Spacing = new Vector2(0, 8),
                                            Children = new Drawable[] {
                                                seekbar = new RoundedSliderBarWithoutTooltip
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
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        totalTime = new AdaptiveSpriteText
                                                        {
                                                            Anchor = Anchor.TopRight,
                                                            Origin = Anchor.TopRight,
                                                            Text = "0:00",
                                                            Colour = overlayColourProvider.Content2,
                                                        }
                                                    },
                                                },
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Spacing = new Vector2(8, 0),
                                                    Children = new Drawable[]
                                                    {
                                                        prevVideoButton = new IconButton
                                                        {
                                                            Enabled = { Value = false },
                                                            Icon = FontAwesome.Solid.FastBackward,
                                                            TooltipText = YTPlayerEXStrings.PreviousVideo,
                                                            IconColour = overlayColourProvider.Content2,
                                                            ClickAction = async _ =>
                                                            {
                                                                if (playlists.Count > 0)
                                                                {
                                                                    if (playlistItemIndex != 0)
                                                                        playlistItemIndex--;

                                                                    await SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId);
                                                                }
                                                            }
                                                        },
                                                        playPause = new IconButton
                                                        {
                                                            Enabled = { Value = true },
                                                            Icon = FontAwesome.Solid.Play,
                                                            TooltipText = YTPlayerEXStrings.Play,
                                                            IconColour = overlayColourProvider.Content2,
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
                                                        nextVideoButton = new IconButton
                                                        {
                                                            Enabled = { Value = false },
                                                            Icon = FontAwesome.Solid.FastForward,
                                                            TooltipText = YTPlayerEXStrings.NextVideo,
                                                            IconColour = overlayColourProvider.Content2,
                                                            ClickAction = async _ =>
                                                            {
                                                                if (playlists.Count > 0)
                                                                {
                                                                    if (playlistItemIndex != playlists.Count - 1)
                                                                        playlistItemIndex++;

                                                                    await SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId);
                                                                }
                                                            }
                                                        },
                                                        new Container
                                                        {
                                                            AutoSizeAxes = Axes.X,
                                                            Height = 30,
                                                            Masking = true,
                                                            CornerRadius = 15,
                                                            Children = new Drawable[]
                                                            {
                                                                new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = overlayColourProvider.Background3,
                                                                    Alpha = 0.7f,
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
                                                                        new SpriteIcon
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
                                                                        new PlaybackSpeedSliderBar
                                                                        {
                                                                            Width = 200,
                                                                            Margin = new MarginPadding
                                                                            {
                                                                                Top = 8,
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
                                                                            Font = YouTubePlayerEXApp.DefaultFont,
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
                                                            Children = new Drawable[]
                                                            {
                                                                new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = overlayColourProvider.Background3,
                                                                    Alpha = 0.7f,
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
                                                                        new RoundedSliderBar<double>
                                                                        {
                                                                            Width = 200,
                                                                            Margin = new MarginPadding
                                                                            {
                                                                                Top = 8,
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
                                                                            Font = YouTubePlayerEXApp.DefaultFont,
                                                                            Colour = overlayColourProvider.Content2,
                                                                        },
                                                                    }
                                                                }
                                                            }
                                                        },
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
                        loadVideoContainer = new OverlayContainer
                        {
                            Width = 400,
                            Height = 200,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.LoadFromVideoId,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                loadBtn = new AdaptiveButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomRight,
                                    Anchor = Anchor.BottomRight,
                                    Text = YTPlayerEXStrings.LoadVideo,
                                    Size = new Vector2(200, 60),
                                    Margin = new MarginPadding(8),
                                },
                                videoIdBox = new AdaptiveTextBox
                                {
                                    Origin = Anchor.CentreRight,
                                    Anchor = Anchor.CentreRight,
                                    Text = "",
                                    FontSize = 30,
                                    Size = new Vector2(385, 60),
                                    Margin = new MarginPadding(8),
                                },
                            }
                        },
                        settingsContainer = new OverlayContainer
                        {
                            Size = new Vector2(0.7f),
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.Settings,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
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
                                                new FillFlowContainer {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Spacing = new Vector2(0, 4),
                                                    Direction = FillDirection.Vertical,
                                                    Children = new Drawable[] {
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.QuickAction,
                                                            Padding = new MarginPadding { Horizontal = 30, Bottom = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsButtonV2
                                                        {
                                                            Text = YTPlayerEXStrings.ExportLogs,
                                                            Padding = new MarginPadding { Horizontal = 30 },
                                                            BackgroundColour = colours.YellowDarker.Darken(0.5f),
                                                            Action = () => Task.Run(exportLogs),
                                                        },
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.General,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsItemV2(new FormEnumDropdown<Language>
                                                        {
                                                            Caption = YTPlayerEXStrings.Language,
                                                            Current = game.CurrentLanguage,
                                                            AlwaysShowSearchBar = true,
                                                        })
                                                        {
                                                            ShowRevertToDefaultButton = false,
                                                            CanBeShown = { BindTarget = displayDropdownCanBeShown }
                                                        },
                                                        new SettingsItemV2(new FormEnumDropdown<DiscordRichPresenceMode>
                                                        {
                                                            Caption = YTPlayerEXStrings.DiscordRichPresence,
                                                            Current = discordRichPresence,
                                                        }),
                                                        new SettingsItemV2(new FormEnumDropdown<VideoMetadataTranslateSource>
                                                        {
                                                            Caption = YTPlayerEXStrings.VideoMetadataTranslateSource,
                                                            Current = appConfig.GetBindable<VideoMetadataTranslateSource>(YTPlayerEXSetting.VideoMetadataTranslateSource),
                                                        }),
                                                        new SettingsItemV2(new FormEnumDropdown<UsernameDisplayMode>
                                                        {
                                                            Caption = YTPlayerEXStrings.UsernameDisplayMode,
                                                            Current = usernameDisplayMode,
                                                        }),
                                                        new SettingsItemV2(login = new FormButton
                                                        {
                                                            Caption = YTPlayerEXStrings.GoogleAccount,
                                                            Text = YTPlayerEXStrings.SignedOut,
                                                            Action = () => {
                                                                if (!googleOAuth2.SignedIn.Value)
                                                                {
                                                                    Task.Run(() => googleOAuth2.SignIn());
                                                                }
                                                                else
                                                                {
                                                                    Task.Run(() => googleOAuth2.SignOut());
                                                                }
                                                            },
                                                        }),
                                                        checkForUpdatesButtonCore = new SettingsItemV2(checkForUpdatesButton = new FormButton
                                                        {
                                                            Caption = YTPlayerEXStrings.CheckUpdate,
                                                            Text = app.Version,
                                                            ButtonIcon = FontAwesome.Solid.Sync,
                                                            Action = () => {
                                                                if (game.UpdateManager is NoActionUpdateManager)
                                                                {
                                                                    host.OpenUrlExternally(@"https://github.com/BoomboxRapsody/YouTubePlayerEX/releases");
                                                                }
                                                                else
                                                                {
                                                                    if (game.RestartRequired.Value != true)
                                                                        checkForUpdates().FireAndForget();
                                                                    else
                                                                        game.RestartAction.Invoke();
                                                                }
                                                            },
                                                        }),
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.Graphics,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsItemV2(new FormEnumDropdown<AspectRatioMethod>
                                                        {
                                                            Caption = YTPlayerEXStrings.AspectRatioMethod,
                                                            Current = aspectRatioMethod,
                                                            Hotkey = new Hotkey(GlobalAction.CycleAspectRatio),
                                                        }),
                                                        new SettingsItemV2(new FormSliderBar<double>
                                                        {
                                                            Caption = YTPlayerEXStrings.VideoDimLevel,
                                                            Current = appConfig.GetBindable<double>(YTPlayerEXSetting.VideoDimLevel),
                                                            DisplayAsPercentage = true,
                                                        }),
                                                        new SettingsItemV2(new FormSliderBar<float>
                                                        {
                                                            Caption = YTPlayerEXStrings.UIScaling,
                                                            TransferValueOnCommit = true,
                                                            Current = appConfig.GetBindable<float>(YTPlayerEXSetting.UIScale),
                                                            KeyboardStep = 0.01f,
                                                            LabelFormat = v => $@"{v:0.##}x",
                                                        }),
                                                        new SettingsItemV2(new FrameSyncDropdown
                                                        {
                                                            Caption = YTPlayerEXStrings.FrameLimiter,
                                                            Current = config.GetBindable<FrameSync>(FrameworkSetting.FrameSync),
                                                        }),
                                                        windowModeDropdownSettings = new SettingsItemV2(windowModeDropdown = new WindowModeDropdown
                                                        {
                                                            Caption = YTPlayerEXStrings.ScreenMode,
                                                            Items = window?.SupportedWindowModes,
                                                            Current = windowMode,
                                                        })
                                                        {
                                                            CanBeShown = { Value = window?.SupportedWindowModes.Count() > 1 },
                                                        },
                                                        displayDropdownCore = new SettingsItemV2(displayDropdown = new DisplaySettingsDropdown
                                                        {
                                                            Caption = YTPlayerEXStrings.Display,
                                                            Items = window?.Displays,
                                                            Current = currentDisplay,
                                                        })
                                                        {
                                                            CanBeShown = { BindTarget = displayDropdownCanBeShown }
                                                        },
                                                        resolutionFullscreenDropdownCore = new SettingsItemV2(resolutionFullscreenDropdown = new ResolutionSettingsDropdown
                                                        {
                                                            Caption = YTPlayerEXStrings.ScreenResolution,
                                                            ItemSource = resolutionsFullscreen,
                                                            Current = sizeFullscreen
                                                        })
                                                        {
                                                            ShowRevertToDefaultButton = false,
                                                            CanBeShown = { BindTarget = resolutionFullscreenCanBeShown }
                                                        },
                                                        resolutionWindowedDropdownCore = new SettingsItemV2(resolutionWindowedDropdown = new ResolutionSettingsDropdown
                                                        {
                                                            Caption = YTPlayerEXStrings.ScreenResolution,
                                                            ItemSource = resolutionsWindowed,
                                                            Current = windowedResolution
                                                        })
                                                        {
                                                            ShowRevertToDefaultButton = false,
                                                            CanBeShown = { BindTarget = resolutionWindowedCanBeShown }
                                                        },
                                                        minimiseOnFocusLossCheckboxCore = new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.MinimiseOnFocusLoss,
                                                            Current = config.GetBindable<bool>(FrameworkSetting.MinimiseOnFocusLossInFullscreen),
                                                        }),
                                                        new SettingsItemV2(new RendererSettingsDropdown
                                                        {
                                                            Caption = YTPlayerEXStrings.Renderer,
                                                            Current = renderer,
                                                            Items = host.GetPreferredRenderersForCurrentPlatform().Order()
                                                            #pragma warning disable CS0612 // Type or member is obsolete
                                                            .Where(t => t != RendererType.Vulkan && t != RendererType.OpenGLLegacy),
                                                            #pragma warning restore CS0612 // Type or member is obsolete
                                                        }),
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.ShowFPS,
                                                            Current = fpsDisplay,
                                                            Hotkey = new Hotkey(GlobalAction.ToggleFPSDisplay),
                                                        }),
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.UseSystemCursor,
                                                            Current = appConfig.GetBindable<bool>(YTPlayerEXSetting.UseSystemCursor),
                                                        }),
                                                        safeAreaConsiderationsCanBeShown = new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.ShrinkGameToSafeArea,
                                                            Current = appConfig.GetBindable<bool>(YTPlayerEXSetting.SafeAreaConsiderations),
                                                        }),
                                                        new SettingsItemV2(new FormEnumDropdown<ScalingMode>
                                                        {
                                                            Caption = YTPlayerEXStrings.ScreenScaling,
                                                            Current = appConfig.GetBindable<ScalingMode>(YTPlayerEXSetting.Scaling),
                                                            Hotkey = new Hotkey(GlobalAction.CycleScalingMode),
                                                        })
                                                        {
                                                            Keywords = new[] { "scale", "letterbox" },
                                                        },
                                                        scalingSettings = new FillFlowContainer<SettingsItemV2>
                                                        {
                                                            Direction = FillDirection.Vertical,
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Masking = true,
                                                            Spacing = new Vector2(0, 4),
                                                            Children = new[]
                                                            {
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.HorizontalPosition,
                                                                    Current = scalingPositionX,
                                                                    KeyboardStep = 0.01f,
                                                                    DisplayAsPercentage = true,
                                                                })
                                                                {
                                                                    Keywords = new[] { "screen", "scaling" },
                                                                },
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.VerticalPosition,
                                                                    Current = scalingPositionY,
                                                                    KeyboardStep = 0.01f,
                                                                    DisplayAsPercentage = true,
                                                                })
                                                                {
                                                                    Keywords = new[] { "screen", "scaling" },
                                                                },
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.HorizontalScale,
                                                                    Current = scalingSizeX,
                                                                    KeyboardStep = 0.01f,
                                                                    DisplayAsPercentage = true,
                                                                })
                                                                {
                                                                    Keywords = new[] { "screen", "scaling" },
                                                                },
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.VerticalScale,
                                                                    Current = scalingSizeY,
                                                                    KeyboardStep = 0.01f,
                                                                    DisplayAsPercentage = true,
                                                                })
                                                                {
                                                                    Keywords = new[] { "screen", "scaling" },
                                                                },
                                                                new SettingsItemV2(dimSlider = new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.ThumbnailDim,
                                                                    Current = scalingBackgroundDim,
                                                                    KeyboardStep = 0.01f,
                                                                    DisplayAsPercentage = true,
                                                                })
                                                            }
                                                        },
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.Screenshot,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsItemV2(new FormEnumDropdown<Config.ScreenshotFormat>
                                                        {
                                                            Caption = YTPlayerEXStrings.ScreenshotFormat,
                                                            Current = appConfig.GetBindable<ScreenshotFormat>(YTPlayerEXSetting.ScreenshotFormat)
                                                        }),
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.ShowCursorInScreenshots,
                                                            Current = appConfig.GetBindable<bool>(YTPlayerEXSetting.ScreenshotCaptureMenuCursor)
                                                        }),
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.Video,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsItemV2(hwAccelCheckbox = new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.UseHardwareAcceleration,
                                                        })
                                                        {
                                                            Note = { BindTarget = hwAccelNote },
                                                        },
                                                        new SettingsItemV2(new FormEnumDropdown<Config.VideoQuality>
                                                        {
                                                            Caption = YTPlayerEXStrings.VideoQuality,
                                                            Current = videoQuality,
                                                        }),
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.AlwaysUseOriginalAudio,
                                                            Current = alwaysUseOriginalAudio,
                                                        }),
                                                        audioLanguageItem = new SettingsItemV2(new FormEnumDropdown<Localisation.Language>
                                                        {
                                                            Caption = YTPlayerEXStrings.AudioLanguage,
                                                            Current = audioLanguage,
                                                        })
                                                        {
                                                            ShowRevertToDefaultButton = false,
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.ClosedCaptions,
                                                            Current = captionEnabled,
                                                            Hotkey = new Hotkey(GlobalAction.CycleCaptionLanguage),
                                                        }),
                                                        captionLangOptions = new SettingsItemV2(captionLangDropdown = new YouTubeI18nLangDropdown
                                                        {
                                                            Caption = YTPlayerEXStrings.CaptionLanguage,
                                                        })
                                                        {
                                                            ShowRevertToDefaultButton = false,
                                                        },
                                                        new SettingsItemV2(new FormEnumDropdown<UIFont>
                                                        {
                                                            Caption = YTPlayerEXStrings.CaptionFont,
                                                            Current = appConfig.GetBindable<UIFont>(YTPlayerEXSetting.UIFont),
                                                        }),
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.VisualEffects,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsItemV2(new FormSliderBar<float>
                                                        {
                                                            Caption = YTPlayerEXStrings.VideoBloomLevel,
                                                            Current = appConfig.GetBindable<float>(YTPlayerEXSetting.VideoBloomLevel),
                                                            DisplayAsPercentage = true,
                                                        }),
                                                        new SettingsItemV2(new FormSliderBar<float>
                                                        {
                                                            Caption = YTPlayerEXStrings.ChromaticAberration,
                                                            Current = appConfig.GetBindable<float>(YTPlayerEXSetting.ChromaticAberrationStrength),
                                                            DisplayAsPercentage = true,
                                                        }),
                                                        new SettingsItemV2(new FormSliderBar<float>
                                                        {
                                                            Caption = YTPlayerEXStrings.VideoGrayscaleLevel,
                                                            Current = appConfig.GetBindable<float>(YTPlayerEXSetting.VideoGrayscaleLevel),
                                                            DisplayAsPercentage = true,
                                                        }),
                                                        new SettingsItemV2(new FormSliderBar<float>
                                                        {
                                                            Caption = YTPlayerEXStrings.VideoHueShift,
                                                            Current = appConfig.GetBindable<float>(YTPlayerEXSetting.VideoHueShift),
                                                            KeyboardStep = 1,
                                                            LabelFormat = value => $"{value:N0}°"
                                                        }),
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.Audio,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsItemV2(audioDeviceDropdown = new AudioDeviceDropdown
                                                        {
                                                            Caption = YTPlayerEXStrings.OutputDevice,
                                                        }),
                                                        wasapiExperimentalItem = new SettingsItemV2(wasapiExperimental = new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.WasapiLabel,
                                                            HintText = YTPlayerEXStrings.WasapiTooltip,
                                                            Current = audio.UseExperimentalWasapi,
                                                        }),
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.AdjustPitchOnSpeedChange,
                                                            Current = adjustPitch,
                                                            Hotkey = new Hotkey(GlobalAction.ToggleAdjustPitchOnSpeedChange),
                                                        }),
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.Volume,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsItemV2(new FormSliderBar<double>
                                                        {
                                                            Caption = YTPlayerEXStrings.MasterVolume,
                                                            Current = config.GetBindable<double>(FrameworkSetting.VolumeUniversal),
                                                            DisplayAsPercentage = true,
                                                        }),
                                                        new SettingsItemV2(new FormSliderBar<double>
                                                        {
                                                            Caption = YTPlayerEXStrings.VideoVolume,
                                                            Current = videoVolume,
                                                            DisplayAsPercentage = true,
                                                        }),
                                                        new SettingsItemV2(new FormSliderBar<double>
                                                        {
                                                            Caption = YTPlayerEXStrings.SFXVolume,
                                                            Current = config.GetBindable<double>(FrameworkSetting.VolumeEffect),
                                                            DisplayAsPercentage = true,
                                                        }),
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.AudioNormalization,
                                                            Current = appConfig.GetBindable<bool>(YTPlayerEXSetting.AudioNormalization)
                                                        }),
                                                        new AdaptiveSpriteText
                                                        {
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30),
                                                            Text = YTPlayerEXStrings.Debug,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.ShowLogOverlay,
                                                            Current = config.GetBindable<bool>(FrameworkSetting.ShowLogOverlay)
                                                        }),
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.BypassFTBRenderPass,
                                                            Current = debugConfig.GetBindable<bool>(DebugSetting.BypassFrontToBackPass)
                                                        }),
                                                        new SettingsItemV2(latencyModeDropdown = new FormEnumDropdown<GCLatencyMode>
                                                        {
                                                            Caption = YTPlayerEXStrings.GC_Mode,
                                                        }),
                                                        new SettingsButtonV2
                                                        {
                                                            Text = YTPlayerEXStrings.ClearAllCaches,
                                                            Padding = new MarginPadding { Horizontal = 30 },
                                                            Action = () =>
                                                            {
                                                                host.Collect();

                                                                // host.Collect() uses GCCollectionMode.Optimized, but we should be as aggressive as possible here.
                                                                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                                                            }
                                                        },
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Margin = new MarginPadding { Top = 12 },
                                                            Child = new Container
                                                            {
                                                                AutoSizeAxes = Axes.Both,
                                                                Anchor = Anchor.Centre,
                                                                Origin = Anchor.Centre,
                                                                Child = new Sprite
                                                                {
                                                                    Width = 100,
                                                                    Height = 100,
                                                                    Texture = textures.Get(@"YouTubePlayerEXLogo"),
                                                                    FillMode = FillMode.Fit,
                                                                }
                                                            },
                                                        },
                                                        new AdaptiveTextFlowContainer(f => f.Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"))
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Text = "YouTube Player EX",
                                                            TextAnchor = Anchor.Centre,
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        gameVersion = new LinkFlowContainer(f =>
                                                        {
                                                            f.Font = YouTubePlayerEXApp.DefaultFont.With(size: 15);
                                                            f.Colour = overlayColourProvider.Content2;
                                                        })
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            TextAnchor = Anchor.Centre,
                                                        },
                                                        madeByText = new LinkFlowContainer(f =>
                                                        {
                                                            f.Font = YouTubePlayerEXApp.DefaultFont.With(size: 15);
                                                            f.Colour = overlayColourProvider.Content2;
                                                        })
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            TextAnchor = Anchor.Centre,
                                                        },
                                                        dislikeCounterCredits = new LinkFlowContainer(f =>
                                                        {
                                                            f.Font = YouTubePlayerEXApp.DefaultFont.With(size: 15);
                                                            f.Colour = overlayColourProvider.Content2;
                                                        })
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Padding = new MarginPadding { Horizontal = 30, Vertical = 12 },
                                                            TextAnchor = Anchor.Centre,
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        videoDescriptionContainer = new OverlayContainer
                        {
                            Size = new Vector2(0.7f),
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
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
                                            Spacing = new Vector2(4, 0),
                                            Children = new Drawable[]
                                            {
                                                likeButton = new RoundedButtonContainer
                                                {
                                                    AutoSizeAxes = Axes.X,
                                                    Height = 32,
                                                    CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                    Masking = true,
                                                    AlwaysPresent = true,
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                            Children = new Drawable[] {
                                                                likeButtonBackground = new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = overlayColourProvider.Background4,
                                                                    Alpha = 0.7f,
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
                                                                    Text = "[no metadata]",
                                                                },
                                                            }
                                                        }
                                                    }
                                                },
                                                dislikeButton = new RoundedButtonContainer
                                                {
                                                    AutoSizeAxes = Axes.X,
                                                    Height = 32,
                                                    CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                    Masking = true,
                                                    AlwaysPresent = true,
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                            Children = new Drawable[] {
                                                                dislikeButtonBackground = new Box
                                                                {
                                                                    RelativeSizeAxes = Axes.Both,
                                                                    Colour = overlayColourProvider.Background4,
                                                                    Alpha = 0.7f,
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
                                                                    Text = "[no metadata]",
                                                                },
                                                            }
                                                        }
                                                    }
                                                },
                                                commentOpenButtonDetails = new RoundedButtonContainer
                                                {
                                                    AutoSizeAxes = Axes.X,
                                                    Height = 32,
                                                    CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                    Masking = true,
                                                    AlwaysPresent = true,
                                                    ClickAction = f =>
                                                    {
                                                        if (!commentsDisabled) {
                                                            hideOverlays();
                                                            showOverlayContainer(commentsContainer);
                                                        }
                                                    },
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                            Child = new Box
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Colour = overlayColourProvider.Background4,
                                                                Alpha = 0.7f,
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
                                                                    Text = "[no metadata]",
                                                                    Colour = overlayColourProvider.Content2,
                                                                },
                                                            }
                                                        }
                                                    }
                                                },
                                                new RoundedButtonContainer
                                                {
                                                    Enabled = { Value = true },
                                                    AutoSizeAxes = Axes.X,
                                                    Height = 32,
                                                    CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                    Masking = true,
                                                    AlwaysPresent = true,
                                                    ClickAction = f =>
                                                    {
                                                        if (string.IsNullOrEmpty(videoUrl))
                                                            return;

                                                        LocalisableString prompt = YTPlayerEXStrings.GPTSummarizePrompt(videoUrl);

                                                        host.OpenUrlExternally($"https://chat.openai.com/?q={prompt}");
                                                    },
                                                    Children = new Drawable[]
                                                    {
                                                        new Container
                                                        {
                                                            RelativeSizeAxes = Axes.Both,
                                                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                            Child = new Box
                                                            {
                                                                RelativeSizeAxes = Axes.Both,
                                                                Colour = overlayColourProvider.Background4,
                                                                Alpha = 0.7f,
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
                                                                    Icon = FontAwesome.Regular.StickyNote,
                                                                    Colour = overlayColourProvider.Content2,
                                                                },
                                                                new AdaptiveSpriteText
                                                                {
                                                                    Text = YTPlayerEXStrings.SummarizeViaGPT,
                                                                    Colour = overlayColourProvider.Content2,
                                                                },
                                                            }
                                                        }
                                                    }
                                                },
                                            }
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Child = new AdaptiveScrollContainer
                                            {
                                                Padding = new MarginPadding()
                                                {
                                                    Bottom = 102,
                                                },
                                                RelativeSizeAxes = Axes.Both,
                                                CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                Masking = true,
                                                ScrollbarVisible = false,
                                                Children = new Drawable[]
                                                {
                                                    new Container
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
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
                                                        CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                                                        Spacing = new Vector2(0, 8),
                                                        Padding = new MarginPadding(12),
                                                        Masking = true,
                                                        Children = new Drawable[]
                                                        {
                                                            videoInfoDetails = new AdaptiveSpriteText
                                                            {
                                                                RelativeSizeAxes = Axes.X,
                                                                Font = YouTubePlayerEXApp.DefaultFont.With(weight: "Black"),
                                                                Colour = overlayColourProvider.Content2,
                                                                AlwaysPresent = true,
                                                            },
                                                            videoDescription = new LinkFlowContainer(f => f.Font = YouTubePlayerEXApp.DefaultFont)
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
                                        }
                                    }
                                },
                            }
                        },
                        commentsContainer = new OverlayContainer
                        {
                            Size = new Vector2(0.7f),
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = overlayColourProvider.Background5,
                                },
                                commentsContainerTitle = new AdaptiveSpriteText
                                {
                                    Origin = Anchor.TopLeft,
                                    Anchor = Anchor.TopLeft,
                                    Text = YTPlayerEXStrings.Comments("0"),
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                new GridContainer
                                {
                                    Margin = new MarginPadding
                                    {
                                        Top = 56,
                                    },
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    RelativeSizeAxes = Axes.X,
                                    Height = 45,
                                    ColumnDimensions = new[]
                                    {
                                        new Dimension(),
                                        new Dimension(GridSizeMode.AutoSize),
                                    },
                                    Content = new []
                                    {
                                        new Drawable[]
                                        {
                                            commentTextBox = new AdaptiveTextBox
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                Size = new Vector2(0.97f, 1f),
                                                Text = "",
                                                FontSize = 20,
                                                Height = 45,
                                            },
                                            commentSendButton = new IconButton
                                            {
                                                Origin = Anchor.Centre,
                                                Anchor = Anchor.Centre,
                                                Icon = FontAwesome.Solid.PaperPlane,
                                                Width = 50,
                                                Height = 45,
                                                AlwaysPresent = true,
                                                Enabled = { Value = true },
                                                BackgroundColour = overlayColourProvider.Background3,
                                            },
                                        },
                                    },
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                        Bottom = 16,
                                        Top = (56 * 2),
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
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
                                        }
                                    }
                                }
                            }
                        },
                        videoInfoExpertOverlay = new OverlayContainer
                        {
                            Size = new Vector2(0.7f),
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = "Video info (Expert)",
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
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
                                                infoForNerds = new AdaptiveTextFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Colour = overlayColourProvider.Content2,
                                                    AlwaysPresent = true,
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        searchContainer = new OverlayContainer
                        {
                            Size = new Vector2(0.7f),
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.Search,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                new GridContainer
                                {
                                    Margin = new MarginPadding
                                    {
                                        Top = 56,
                                    },
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                    },
                                    RelativeSizeAxes = Axes.X,
                                    Height = 45,
                                    ColumnDimensions = new[]
                                    {
                                        new Dimension(),
                                        new Dimension(GridSizeMode.AutoSize),
                                    },
                                    Content = new []
                                    {
                                        new Drawable[]
                                        {
                                            searchTextBox = new AdaptiveTextBox
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                Size = new Vector2(0.97f, 1f),
                                                Text = "",
                                                PlaceholderText = YTPlayerEXStrings.SearchPlaceholder,
                                                FontSize = 20,
                                                Height = 45,
                                            },
                                            searchButton = new IconButton
                                            {
                                                Origin = Anchor.Centre,
                                                Anchor = Anchor.Centre,
                                                Icon = FontAwesome.Solid.Search,
                                                Width = 50,
                                                Height = 45,
                                                AlwaysPresent = true,
                                                Enabled = { Value = true },
                                            },
                                        },
                                    },
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding
                                    {
                                        Horizontal = 16,
                                        Bottom = 16,
                                        Top = (56 * 2),
                                    },
                                    Children = new Drawable[] {
                                        new AdaptiveScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ScrollbarVisible = false,
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
                                }
                            }
                        },
                        reportAbuseOverlay = new OverlayContainer
                        {
                            Size = new Vector2(0.7f, 1f),
                            Height = 276,
                            RelativeSizeAxes = Axes.X,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.Report,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
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
                                                        new TruncatingSpriteText
                                                        {
                                                            Text = YTPlayerEXStrings.WhatsGoingOn,
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 27, weight: "Bold"),
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        new AdaptiveTextFlowContainer(f => f.Font = YouTubePlayerEXApp.DefaultFont.With(size: 17, weight: "Regular"))
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Text = YTPlayerEXStrings.ReportDesc,
                                                            Colour = overlayColourProvider.Background1,
                                                        },
                                                        reportReason = new ReportDropdown
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Caption = YTPlayerEXStrings.ReportReason,
                                                        },
                                                        reportSubReason = new ReportDropdown
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Caption = YTPlayerEXStrings.ReportSubReason,
                                                        },
                                                        reportComment = new FormTextBox
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = YTPlayerEXStrings.Description,
                                                        },
                                                        reportButton = new SettingsButtonV2
                                                        {
                                                            Height = 40,
                                                            Text = YTPlayerEXStrings.Submit,
                                                            BackgroundColour = colours.Yellow,
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        playlistOverlay = new SideOverlayContainer
                        {
                            Size = new Vector2(1f, .95f),
                            Width = 400,
                            RelativeSizeAxes = Axes.Y,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                            Margin = new MarginPadding
                            {
                                Right = 16,
                            },
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.Playlists,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                loadPlaylistOpenButton = new IconButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.TopRight,
                                    Anchor = Anchor.TopRight,
                                    Size = new Vector2(40, 40),
                                    Icon = FontAwesome.Regular.FolderOpen,
                                    Margin = new MarginPadding(16),
                                    IconScale = new Vector2(1.2f),
                                    TooltipText = YTPlayerEXStrings.LoadFromPlaylistId,
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
                                                        playlistName = new TruncatingSpriteText
                                                        {
                                                            Origin = Anchor.TopLeft,
                                                            Anchor = Anchor.TopLeft,
                                                            Text = "[playlist name]",
                                                            RelativeSizeAxes = Axes.X,
                                                            Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        playlistAuthor = new LinkFlowContainer(f =>
                                                        {
                                                            f.Font = YouTubePlayerEXApp.TorusAlternate.With(size: 16, weight: "SemiBold");
                                                            f.Colour = overlayColourProvider.Background1;
                                                        })
                                                        {
                                                            Origin = Anchor.TopLeft,
                                                            Anchor = Anchor.TopLeft,
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Text = "[channel name]",
                                                        },
                                                        playlistItemsView = new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Vertical,
                                                            Spacing = new Vector2(4),
                                                            Children = new Drawable[]
                                                            {
                                                            }
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        loadPlaylistContainer = new OverlayContainer
                        {
                            Width = 400,
                            Height = 200,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.LoadFromPlaylistId,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
                                    Colour = overlayColourProvider.Content2,
                                },
                                loadPlaylistBtn = new AdaptiveButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomRight,
                                    Anchor = Anchor.BottomRight,
                                    Text = YTPlayerEXStrings.LoadPlaylist,
                                    Size = new Vector2(200, 60),
                                    Margin = new MarginPadding(8),
                                },
                                playlistIdBox = new AdaptiveTextBox
                                {
                                    Origin = Anchor.CentreRight,
                                    Anchor = Anchor.CentreRight,
                                    Text = "",
                                    FontSize = 30,
                                    Size = new Vector2(385, 60),
                                    Margin = new MarginPadding(8),
                                },
                            }
                        },
                        audioEffectsOverlay = new SideOverlayContainer
                        {
                            Size = new Vector2(1f, .95f),
                            Width = 400,
                            RelativeSizeAxes = Axes.Y,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                            Margin = new MarginPadding
                            {
                                Right = 16,
                            },
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.AudioEffects,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
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
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.ReverbEffect,
                                                            Current = reverbEnabled,
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
                                                                    Caption = YTPlayerEXStrings.WetMix,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ReverbWetMix),
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.StereoWidth,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ReverbStereoWidth),
                                                                    DisplayAsPercentage = true,
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.HighFreqDamp,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ReverbDamp),
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.RoomSize,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.ReverbRoomSize),
                                                                    DisplayAsPercentage = true,
                                                                }),
                                                            }
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.RotateParameters_Enabled,
                                                            Current = rotateEnabled,
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
                                                                    Caption = YTPlayerEXStrings.RotateParameters_fRate,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.RotateRate),
                                                                    DisplayAsPercentage = true,
                                                                }),
                                                            }
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.EchoEffect,
                                                            Current = echoEnabled,
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
                                                                    Caption = YTPlayerEXStrings.DryMix,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.EchoDryMix),
                                                                    LabelFormat = f => $"{f - 2}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.EchoWetMix,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.EchoWetMix),
                                                                    LabelFormat = f => $"{f - 2}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.EchoFeedback,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.EchoFeedback),
                                                                    LabelFormat = f => $"{f - 1}",
                                                                }),
                                                                new SettingsItemV2(new FormSliderBar<float>
                                                                {
                                                                    Caption = YTPlayerEXStrings.EchoDelay,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.EchoDelay),
                                                                }),
                                                            }
                                                        },
                                                        new SettingsItemV2(new FormCheckBox
                                                        {
                                                            Caption = YTPlayerEXStrings.DistortionEffect,
                                                            Current = distortionEnabled,
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
                                                                    Caption = YTPlayerEXStrings.DistortionVolume,
                                                                    Current = audioEffectsConfig.GetBindable<float>(AudioEffectsSetting.DistortionVolume),
                                                                    DisplayAsPercentage = true,
                                                                }),
                                                            }
                                                        },
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        unsubscribeDialog = new OverlayContainer
                        {
                            Width = 450,
                            Height = 200,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                new AdaptiveTextFlowContainer(f => f.Font = YouTubePlayerEXApp.DefaultFont.With(size: 20))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre,
                                    TextAnchor = Anchor.Centre,
                                    AlwaysPresent = true,
                                    Text = YTPlayerEXStrings.UnsubscribeDesc,
                                    Colour = overlayColourProvider.Content2,
                                },
                                declineButton = new AdaptiveButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomLeft,
                                    Anchor = Anchor.BottomLeft,
                                    Text = YTPlayerEXStrings.Cancel,
                                    Size = new Vector2(200, 60),
                                    Margin = new MarginPadding(8),
                                },
                                acceptButton = new AdaptiveButton
                                {
                                    Enabled = { Value = true },
                                    Origin = Anchor.BottomRight,
                                    Anchor = Anchor.BottomRight,
                                    Text = YTPlayerEXStrings.Yes,
                                    Size = new Vector2(200, 60),
                                    BackgroundColour = colours.RedDark,
                                    Margin = new MarginPadding(8),
                                },
                            }
                        },
                        videoSaveLocationOverlay = new OverlayContainer
                        {
                            Size = new Vector2(1f, 1f),
                            Width = 450,
                            Height = 250,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.SaveLocation,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
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
                                                        myPlaylistsDropdown = new PlaylistDropdown
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = YTPlayerEXStrings.Playlists,
                                                        },
                                                        new AdaptiveButton
                                                        {
                                                            Enabled = { Value = true },
                                                            Text = YTPlayerEXStrings.AddNewPlaylist,
                                                            RelativeSizeAxes = Axes.X,
                                                            Size = new Vector2(1, 40),
                                                            Margin = new MarginPadding(4),
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
                                                                new AdaptiveButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = YTPlayerEXStrings.Cancel,
                                                                    Size = new Vector2(200, 60),
                                                                    Margin = new MarginPadding(4),
                                                                    Action = () =>
                                                                    {
                                                                        hideOverlayContainer(videoSaveLocationOverlay);
                                                                    }
                                                                },
                                                                new AdaptiveButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = YTPlayerEXStrings.SaveOrRemove,
                                                                    Size = new Vector2(200, 60),
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
                                }
                            }
                        },
                        addPlaylistOverlay = new OverlayContainer
                        {
                            Size = new Vector2(1f, 1f),
                            Width = 450,
                            Height = 250,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.AddNewPlaylist,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
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
                                                        playlistTitleBox = new FormTextBox
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = YTPlayerEXStrings.Title,
                                                            PlaceholderText = YTPlayerEXStrings.TitlePlaceholder,
                                                        },
                                                        playlistPrivacyStatusDropdown = new FormEnumDropdown<PrivacyStatus>
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            Height = 50,
                                                            Caption = YTPlayerEXStrings.PrivacyStatus,
                                                        },
                                                        new FillFlowContainer
                                                        {
                                                            RelativeSizeAxes = Axes.X,
                                                            AutoSizeAxes = Axes.Y,
                                                            Direction = FillDirection.Horizontal,
                                                            Children = new Drawable[]
                                                            {
                                                                new AdaptiveButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = YTPlayerEXStrings.Cancel,
                                                                    Size = new Vector2(200, 60),
                                                                    Margin = new MarginPadding(4),
                                                                    Action = () =>
                                                                    {
                                                                        hideOverlayContainer(addPlaylistOverlay);
                                                                    }
                                                                },
                                                                new AdaptiveButton
                                                                {
                                                                    Enabled = { Value = true },
                                                                    Text = YTPlayerEXStrings.Create,
                                                                    Size = new Vector2(200, 60),
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
                                }
                            }
                        },
                        menuOverlay = new SideOverlayContainer
                        {
                            Size = new Vector2(1f, .95f),
                            Width = 400,
                            RelativeSizeAxes = Axes.Y,
                            CornerRadius = YouTubePlayerEXApp.UI_CORNER_RADIUS,
                            Masking = true,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                            Margin = new MarginPadding
                            {
                                Right = 16,
                            },
                            EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                            {
                                Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.25f),
                                Offset = new Vector2(0, 2),
                                Radius = 16,
                            },
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
                                    Text = YTPlayerEXStrings.Menu,
                                    Margin = new MarginPadding(16),
                                    Font = YouTubePlayerEXApp.TorusAlternate.With(size: 30, weight: "Bold"),
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
                                                        loadBtnOverlayShow = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Regular.FolderOpen,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = YTPlayerEXStrings.LoadVideo,
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
                                                            Text = YTPlayerEXStrings.Settings,
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
                                                            Text = YTPlayerEXStrings.CommentsWithoutCount,
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
                                                            Text = YTPlayerEXStrings.Search,
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
                                                            Text = YTPlayerEXStrings.Report,
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
                                                            Text = YTPlayerEXStrings.Playlists,
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
                                                            Text = YTPlayerEXStrings.AudioEffects,
                                                        },
                                                        saveVideoOpenButton = new MenuButtonItem
                                                        {
                                                            Enabled = { Value = true },
                                                            Origin = Anchor.TopRight,
                                                            Anchor = Anchor.TopRight,
                                                            Size = new Vector2(1, 45),
                                                            RelativeSizeAxes = Axes.X,
                                                            Icon = FontAwesome.Regular.Bookmark,
                                                            IconScale = new Vector2(1.2f),
                                                            Text = YTPlayerEXStrings.Save,
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
                                                            Text = YTPlayerEXStrings.Exit,
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
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = alert = new AdaptiveAlertContainer
                            {
                                Origin = Anchor.TopCentre,
                                Anchor = Anchor.TopCentre,
                                Size = new Vector2(600, 60),
                            },
                        }
                    }
                }
            };

            thumbnailContainer.BlurTo(Vector2.Divide(new Vector2(10, 10), 1));
            loadVideoContainer.Hide();
            overlayFadeContainer.Hide();
            settingsContainer.Hide();
            videoDescriptionContainer.Hide();
            commentsContainer.Hide();
            searchContainer.Hide();
            videoInfoExpertOverlay.Hide();
            reportAbuseOverlay.Hide();
            playlistOverlay.Hide();
            loadPlaylistContainer.Hide();
            audioEffectsOverlay.Hide();
            unsubscribeDialog.Hide();
            videoSaveLocationOverlay.Hide();
            addPlaylistOverlay.Hide();
            menuOverlay.Hide();

            captionEnabled.Disabled = true;

            menuOverlayShow.ClickAction = _ =>
            {
                showOverlayContainer(menuOverlay);
            };

            madeByText.AddText("made by ");
            madeByText.AddLink("MayoDev Studios", "https://boomboxrapsody.github.io/youtube-player-ex.html");

            latencyModeDropdown.Current.BindValueChanged(mode =>
            {
                Logger.Log($"Changing latency mode: {mode.NewValue}");

                switch (mode.NewValue)
                {
                    case GCLatencyMode.Default:
                        // https://github.com/ppy/osu-framework/blob/1d5301018dfed1a28702be56e1d53c4835b199f2/osu.Framework/Platform/GameHost.cs#L703
                        GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
                        break;

                    case GCLatencyMode.Interactive:
                        GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Interactive;
                        break;
                }
            });

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

                    Task.Run(async () =>
                    {
                        IList<Playlist> playlists = await api.GetMyPlaylistItems();

                        Schedule(() =>
                        {
                            myPlaylistsDropdown.Items = playlists;
                            myPlaylistsDropdown.Current.Value = playlists[0];
                        });
                    });

                    Schedule(() => commentSendButton.Enabled.Value = true);
                    Channel wth = api.GetMineChannel();
                    login.Text = YTPlayerEXStrings.SignedIn(api.GetLocalizedChannelTitle(wth, true));

                    if (api.TryToGetMineChannel() != null)
                        commentTextBox.PlaceholderText = YTPlayerEXStrings.CommentWith(api.GetLocalizedChannelTitle(api.GetMineChannel()));
                }
                else
                {
                    Schedule(() => commentSendButton.Enabled.Value = false);
                    login.Text = YTPlayerEXStrings.SignedOut;

                    commentTextBox.PlaceholderText = string.Empty;
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

            playPause.BackgroundColour = searchButton.BackgroundColour = commentSendButton.BackgroundColour = nextVideoButton.BackgroundColour = prevVideoButton.BackgroundColour = loadPlaylistOpenButton.BackgroundColour = overlayColourProvider.Background3;

            hwAccelCheckbox.Current.Default = hardwareVideoDecoder.Default != HardwareVideoDecoder.None;
            hwAccelCheckbox.Current.Value = hardwareVideoDecoder.Value != HardwareVideoDecoder.None;

            hwAccelCheckbox.Current.BindValueChanged(val =>
            {
                hwAccelNote.Value = val.NewValue ? new SettingsNote.Data(YTPlayerEXStrings.HardwareAccelerationEnabledNote, SettingsNote.Type.Informational) : null;
                hardwareVideoDecoder.Value = val.NewValue ? HardwareVideoDecoder.Any : HardwareVideoDecoder.None;
            }, true);

            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
            {
                wasapiExperimentalItem.Hide();
            }

            overlayContainers.Add(loadVideoContainer);
            overlayContainers.Add(settingsContainer);
            overlayContainers.Add(videoDescriptionContainer);
            overlayContainers.Add(commentsContainer);
            overlayContainers.Add(videoInfoExpertOverlay);
            overlayContainers.Add(searchContainer);
            overlayContainers.Add(reportAbuseOverlay);
            overlayContainers.Add(playlistOverlay);
            overlayContainers.Add(loadPlaylistContainer);
            overlayContainers.Add(audioEffectsOverlay);
            overlayContainers.Add(unsubscribeDialog);
            overlayContainers.Add(addPlaylistOverlay);
            overlayContainers.Add(videoSaveLocationOverlay);
            overlayContainers.Add(menuOverlay);

            playlistName.Text = "please choose a playlist!";
            playlistAuthor.Text = "[no metadata available]";

            infoForNerds.AddText("Codec: ");
            infoForNerds.AddText("[unknown]", f => f.Font = YouTubePlayerEXApp.DefaultFont.With(weight: "Bold"));
            infoForNerds.AddText("\nWidth: ");
            infoForNerds.AddText("[unknown]", f => f.Font = YouTubePlayerEXApp.DefaultFont.With(weight: "Bold"));
            infoForNerds.AddText("\nHeight: ");
            infoForNerds.AddText("[unknown]", f => f.Font = YouTubePlayerEXApp.DefaultFont.With(weight: "Bold"));
            infoForNerds.AddText("\nFPS: ");
            infoForNerds.AddText("[unknown]", f => f.Font = YouTubePlayerEXApp.DefaultFont.With(weight: "Bold"));
            infoForNerds.AddText("\nBitrate: ");
            infoForNerds.AddText("[unknown]", f => f.Font = YouTubePlayerEXApp.DefaultFont.With(weight: "Bold"));

            audio.OnNewDevice += onAudioDeviceChanged;
            audio.OnLostDevice += onAudioDeviceChanged;
            audioDeviceDropdown.Current = audio.AudioDevice;

            onAudioDeviceChanged(string.Empty);

            videoQuality.BindValueChanged(quality =>
            {
                videoQualityWarning.Value = (quality.NewValue == Config.VideoQuality.Quality_8K) ? new SettingsNote.Data(YTPlayerEXStrings.VideoQuality8KWarning, SettingsNote.Type.Warning) : null;
                if (currentVideoSource != null)
                {
                    Task.Run(async () =>
                    {
                        await SetVideoSource(videoId, true, LoadType.VideoOnly);
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
                if (enabled.NewValue)
                    captionLangOptions.Show();
                else
                    captionLangOptions.Hide();
            }, true);

            alwaysUseOriginalAudio.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    audioLanguageItem.Hide();
                }
                else
                {
                    audioLanguageItem.Show();
                }

                if (currentVideoSource != null)
                {
                    Task.Run(async () =>
                    {
                        await SetVideoSource(videoId, true, LoadType.AudioOnly);
                    });
                }
            }, true);

            adjustPitch.BindValueChanged(value =>
            {
                currentVideoSource?.UpdatePreservePitch(value.NewValue);
            });

            dislikeCounterCredits.AddText(YTPlayerEXStrings.DislikeCounterCredits_1);
            dislikeCounterCredits.AddLink("Return YouTube Dislike API", "https://returnyoutubedislike.com/");
            dislikeCounterCredits.AddText(YTPlayerEXStrings.DislikeCounterCredits_2);

            audioLanguage.BindValueChanged(_ =>
            {
                if (currentVideoSource != null)
                {
                    Task.Run(async () =>
                    {
                        await SetVideoSource(videoId, true, LoadType.AudioOnly);
                    });
                }
            });

            captionLangDropdown.Current.BindValueChanged(lang =>
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
                                    Toast toast = new Toast(YTPlayerEXStrings.CaptionLanguage, lang.NewValue.Name);

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

                            var trackInfo = trackManifest.Tracks.Where(track => track.Language.Name == captionLangDropdown.Current.Value.Name).First();

                            ClosedCaptionTrack captionTrack = null;

                            if (enabled.NewValue)
                            {
                                Schedule(() =>
                                {
                                    Toast toast = new Toast(YTPlayerEXStrings.CaptionLanguage, captionLangDropdown.Current.Value.Name);

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

            idleTracker.IsIdle.BindValueChanged(idle =>
            {
                if (idle.NewValue == true)
                {
                    hideControls();
                }
                else
                {
                    showControls();
                }
            }, true);

            if (window?.SupportedWindowModes.Count() > 1)
            {
                windowModeDropdownSettings.Show();
            }
            else
            {
                windowModeDropdownSettings.Hide();
            }

            playbackSpeed.BindValueChanged(speed =>
            {
                this.TransformBindableTo(speedTextRolling, speed.NewValue, 400, Easing.OutQuint);
            }, true);

            speedTextRolling.BindValueChanged(speed =>
            {
                int intValue = (int)Math.Round(speed.NewValue * 100);
                speedText.Text = $"{intValue}%";
            }, true);

            volumeTextRolling.BindValueChanged(volume =>
            {
                int intValue = (int)Math.Round(volume.NewValue * 100);
                volumeText.Text = $"{intValue}%";
            }, true);

            scalingMode.BindValueChanged(_ =>
            {
                scalingSettings.ClearTransforms();
                scalingSettings.AutoSizeDuration = 400;
                scalingSettings.AutoSizeEasing = Easing.OutQuint;

                updateScalingModeVisibility();
            });
            updateScalingModeVisibility();

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

            if (game.IsDeployedBuild)
            {
                gameVersion.AddLink(game.Version, $"https://github.com/BoomboxRapsody/YouTubePlayerEX/releases/{game.Version}", tooltipText: YTPlayerEXStrings.ViewChangelog(game.Version));
            }
            else
            {
                gameVersion.AddText(game.Version);
            }

            updateInfomationText.BindValueChanged(text =>
            {
                Schedule(() => checkForUpdatesButton.Text = text.NewValue);
            });

            updateButtonEnabled.BindValueChanged(enabled =>
            {
                Schedule(() => checkForUpdatesButton.Enabled.Value = enabled.NewValue);
            });

            renderer.BindValueChanged(r =>
            {
                if (r.NewValue == host.ResolvedRenderer)
                    return;

                // Need to check startup renderer for the "automatic" case, as ResolvedRenderer above will track the final resolved renderer instead.
                if (r.NewValue == RendererType.Automatic && automaticRendererInUse)
                    return;

                if (game?.RestartAppWhenExited() == true)
                {
                    game.AttemptExit();
                }
            });

            void updateScalingModeVisibility()
            {
                try
                {
                    if (scalingMode.Value == ScalingMode.Off)
                        scalingSettings.ResizeHeightTo(0, 400, Easing.OutQuint);

                    scalingSettings.AutoSizeAxes = scalingMode.Value != ScalingMode.Off ? Axes.Y : Axes.None;

                    foreach (SettingsItemV2 item in scalingSettings)
                    {
                        FormSliderBar<float> slider = (FormSliderBar<float>)item.Control;

                        if (slider == dimSlider)
                            item.CanBeShown.Value = scalingMode.Value == ScalingMode.Everything || scalingMode.Value == ScalingMode.Video;
                        else
                        {
                            slider.TransferValueOnCommit = scalingMode.Value == ScalingMode.Everything;
                            item.CanBeShown.Value = scalingMode.Value != ScalingMode.Off;
                        }
                    }
                }
                catch
                {
                }
            }

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
                }
                catch
                {
                }
            }
        }

        private void saveVideoToPlaylist(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return;

            Task.Run(async () =>
            {
                await api.SaveVideoToPlaylist(myPlaylistsDropdown.Current.Value.Id, videoId);

                saveVideoOpenButton.Icon = FontAwesome.Solid.Bookmark;

                Toast toast = new Toast(YTPlayerEXStrings.Playlists, YTPlayerEXStrings.VideoSavedToPlaylist(videoId, myPlaylistsDropdown.Current.Value.Snippet.Title));

                Schedule(() => onScreenDisplay.Display(toast));
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

                Toast toast = new Toast(YTPlayerEXStrings.Playlists, YTPlayerEXStrings.VideoRemovedFromPlaylist(videoId, myPlaylistsDropdown.Current.Value.Snippet.Title));

                Schedule(() => onScreenDisplay.Display(toast));
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

        private AdaptiveTextFlowContainer infoForNerds;

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
            if (isControlVisible == true)
            {
                isControlVisible = false;
                uiContainer.FadeOutFromOne(250);
                uiGradientContainer.FadeOutFromOne(250);
                sessionStatics.GetBindable<bool>(Static.IsControlVisible).Value = false;
            }
        }

        [Resolved]
        private SessionStatics sessionStatics { get; set; }

        private async Task checkForUpdates()
        {
            if (updateManager == null || game == null)
                return;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            game.UpdateManagerVersionText.Value = YTPlayerEXStrings.CheckingUpdate;

            checkForUpdatesButton.Enabled.Value = false;

            try
            {
                bool foundUpdate = await updateManager.CheckForUpdateAsync(cancellationTokenSource.Token).ConfigureAwait(true);

                if (!foundUpdate)
                {
                    /*
                    alert.Text = YTPlayerEXStrings.RunningLatestRelease(game.Version);
                    alert.Show();
                    */
                    Toast toast = new Toast(YTPlayerEXStrings.General, YTPlayerEXStrings.RunningLatestRelease(game.Version));

                    onScreenDisplay.Display(toast);

                    game.UpdateManagerVersionText.Value = game.Version;
                    checkForUpdatesButton.Enabled.Value = true;
                    spinnerShow = Scheduler.AddDelayed(alert.Hide, 3000);
                }
            }
            catch
            {
                game.UpdateManagerVersionText.Value = game.Version;
                checkForUpdatesButton.Enabled.Value = true;
            }
            finally
            {
            }
        }

        public override bool CursorVisible => (isControlVisible || isAnyOverlayOpen.Value);

        private SettingsItemV2 resolutionFullscreenDropdownCore, resolutionWindowedDropdownCore, displayDropdownCore, minimiseOnFocusLossCheckboxCore, checkForUpdatesButtonCore;

        private FormCheckBox hwAccelCheckbox;

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

        private SettingsItemV2 windowModeDropdownSettings;

        private partial class RoundedSliderBarWithoutTooltip : RoundedSliderBar<double>
        {
            public override LocalisableString TooltipText => "";
        }

        private void updateDisplaySettingsVisibility()
        {
            if (windowModeDropdown.Current.Value == WindowMode.Fullscreen && resolutionsFullscreen.Count > 1)
            {
                resolutionFullscreenDropdownCore.Show();
            }
            else
            {
                resolutionFullscreenDropdownCore.Hide();
            }

            if (windowModeDropdown.Current.Value == WindowMode.Windowed && resolutionsFullscreen.Count > 1)
            {
                resolutionWindowedDropdownCore.Show();
            }
            else
            {
                resolutionWindowedDropdownCore.Hide();
            }

            if (displayDropdown.Items.Count() > 1)
            {
                displayDropdownCore.Show();
            }
            else
            {
                displayDropdownCore.Hide();
            }

            if (RuntimeInfo.IsDesktop && windowModeDropdown.Current.Value == WindowMode.Fullscreen)
            {
                minimiseOnFocusLossCheckboxCore.Show();
            }
            else
            {
                minimiseOnFocusLossCheckboxCore.Hide();
            }

            if (host.Window?.SafeAreaPadding.Value.Total != Vector2.Zero)
            {
                safeAreaConsiderationsCanBeShown.Show();
            }
            else
            {
                safeAreaConsiderationsCanBeShown.Hide();
            }

            /*
        resolutionFullscreenCanBeShown.Value = windowModeDropdown.Current.Value == WindowMode.Fullscreen && resolutionsFullscreen.Count > 1;
        displayDropdownCanBeShown.Value = windowModeDropdown.Current.Value == WindowMode.Windowed && resolutionsWindowed.Count > 1;
        minimiseOnFocusLossCanBeShown.Value = RuntimeInfo.IsDesktop && windowModeDropdown.Current.Value == WindowMode.Fullscreen;
            */
        }

        private readonly BindableList<Size> resolutionsFullscreen = new BindableList<Size>(new[] { new Size(9999, 9999) });
        private readonly BindableList<Size> resolutionsWindowed = new BindableList<Size>();
        private readonly Bindable<Size> windowedResolution = new Bindable<Size>();
        private readonly IBindable<FullscreenCapability> fullscreenCapability = new Bindable<FullscreenCapability>(FullscreenCapability.Capable);

        private Bindable<Size> sizeFullscreen = null!;
        private Bindable<Size> sizeWindowed = null!;

        private readonly BindableBool resolutionFullscreenCanBeShown = new BindableBool(true);
        private readonly BindableBool resolutionWindowedCanBeShown = new BindableBool(true);
        private readonly BindableBool displayDropdownCanBeShown = new BindableBool(true);
        private readonly BindableBool minimiseOnFocusLossCanBeShown = new BindableBool(true);
        private SettingsItemV2 safeAreaConsiderationsCanBeShown;

        private FormDropdown<Size> resolutionFullscreenDropdown = null!;
        private FormDropdown<Size> resolutionWindowedDropdown = null!;
        private FormDropdown<Display> displayDropdown = null!;
        private FormDropdown<WindowMode> windowModeDropdown = null!;

#nullable enable
        private readonly Bindable<SettingsNote.Data?> windowModeDropdownNote = new Bindable<SettingsNote.Data?>();
#nullable disable

        private BindableNumber<double> playbackSpeed = new BindableNumber<double>(1)
        {
            MinValue = 0.1,
            MaxValue = 4,
            Precision = 0.01,
        };

        private partial class DisplaySettingsDropdown : FormDropdown<Display>
        {
            protected override LocalisableString GenerateItemText(Display item)
            {
                return $"{item.Index}: {item.Name} ({item.Bounds.Width}x{item.Bounds.Height})";
            }
        }

        private partial class ResolutionSettingsDropdown : FormDropdown<Size>
        {
            protected override LocalisableString GenerateItemText(Size item)
            {
                if (item == new Size(9999, 9999))
                    return YTPlayerEXStrings.Default;

                return $"{item.Width}x{item.Height}";
            }
        }

#nullable enable
        private IDisposable? duckOperation;
#nullable disable

        private void showOverlayContainer(OverlayContainer overlayContent)
        {
            duckOperation = game.Duck(new DuckParameters
            {
                DuckVolumeTo = 1,
                DuckDuration = 100,
                RestoreDuration = 100,
            });

            if (overlayContent is SideOverlayContainer)
            {
                isAnyOverlayOpen.Value = true;
                overlayContent.IsVisible = true;
                videoScalingContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                videoContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0.5f, 250, Easing.OutQuart);
                overlayContent.Show();
                overlayContent.MoveToX(500);
                overlayContent.MoveToX(0, 500, Easing.OutQuint);
                overlayContent.FadeInFromZero(250, Easing.OutQuart);
                overlayShowSample.Play();
            }
            else
            {
                isAnyOverlayOpen.Value = true;
                overlayContent.IsVisible = true;
                videoScalingContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                videoContainer?.BlurTo(new Vector2(4), 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0.5f, 250, Easing.OutQuart);
                overlayContent.Show();
                overlayContent.ScaleTo(0.8f);
                overlayContent.ScaleTo(1f, 750, Easing.OutElastic);
                overlayContent.FadeInFromZero(250, Easing.OutQuart);
                overlayShowSample.Play();
            }
        }

        private void hideOverlayContainer(OverlayContainer overlayContent)
        {
            duckOperation?.Dispose();
            if (overlayContent is SideOverlayContainer)
            {
                overlayContent.IsVisible = false;
                isAnyOverlayOpen.Value = false;
                overlayHideSample.Play();
                videoScalingContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                videoContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0f, 250, Easing.OutQuart);
                overlayContent.MoveToX(500, 500, Easing.OutQuart);
                overlayContent.FadeOutFromOne(250, Easing.OutQuart);
            }
            else
            {
                overlayContent.IsVisible = false;
                isAnyOverlayOpen.Value = false;
                overlayHideSample.Play();
                videoScalingContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                videoContainer?.BlurTo(new Vector2(0), 250, Easing.OutQuart);
                overlayFadeContainer.FadeTo(0f, 250, Easing.OutQuart);
                overlayContent.ScaleTo(0.8f, 250, Easing.OutQuart);
                overlayContent.FadeOutFromOne(250, Easing.OutQuart);
            }
        }

        private bool isLoadVideoContainerVisible = false;

        private Bindable<bool> isAnyOverlayOpen;

        private readonly Bindable<Display> currentDisplay = new Bindable<Display>();

        [Resolved]
        private YouTubePlayerEXAppBase app { get; set; }

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

            IList<SearchResult> searchResults = api.GetSearchResult(searchTextBox.Text);
            foreach (SearchResult item in searchResults)
            {
                if (item.Id.Kind == "youtube#video")
                {
                    YouTubeSearchResultView wth = new YouTubeSearchResultView()
                    {
                        RelativeSizeAxes = Axes.X,
                    };

                    searchResultContainer.Add(wth);

                    wth.ClickAction = async _ =>
                    {
                        ClearPlaylistItems();
                        await SetVideoSource(item.Id.VideoId);
                    };

                    wth.Enabled.Value = true;

                    wth.Data = item;

                    wth.UpdateData();
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

        private Random random;

        private void updatePresence(DiscordRichPresenceMode mode)
        {
            switch (mode)
            {
                case DiscordRichPresenceMode.Full:
                {
                    if (videoData != null)
                    {
                        discordRPC?.UpdatePresence(new RichPresence()
                        {
                            Details = $"{videoData.Snippet.ChannelTitle} - {videoData.Snippet.Title}",
                            State = "Watching Video",
                            Assets = new Assets()
                            {
                                LargeImageKey = videoData.Snippet.Thumbnails.High.Url,
                                LargeImageUrl = $"https://youtu.be/{videoData.Id}",
                                LargeImageText = videoData.Snippet.Title,
                                SmallImageText = "YouTube Player EX",
                                SmallImageKey = "youtube_player_ex_logo"
                            },
                        });
                    }
                    else
                    {
                        discordRPC?.UpdatePresence(new RichPresence()
                        {
                            State = "Idle",
                            Assets = new Assets()
                            {
                                LargeImageKey = "youtube_player_ex_logo",
                                LargeImageText = "YouTube Player EX",
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
                            State = "Watching Video",
                            Assets = new Assets()
                            {
                                LargeImageText = "YouTube Player EX",
                                LargeImageKey = "youtube_player_ex_logo"
                            },
                        });
                    }
                    else
                    {
                        discordRPC?.UpdatePresence(new RichPresence()
                        {
                            State = "Idle",
                            Assets = new Assets()
                            {
                                LargeImageKey = "youtube_player_ex_logo",
                                LargeImageText = "YouTube Player EX",
                            },
                        });
                    }
                    break;
                }
                case DiscordRichPresenceMode.Off:
                {
                    discordRPC.ClearPresence();
                    break;
                }
            }
        }

        private int partySize = 1;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            random = new Random();

            discordRichPresence.BindValueChanged(mode => updatePresence(mode.NewValue), true);

            //check updates for LoadComplete
            if (game.IsDeployedBuild)
                checkForUpdates().FireAndForget();

            if (appGlobalConfig.Get<bool>(YTPlayerEXSetting.FinalLoginState) == true)
            {
                Task.Run(async () => await googleOAuth2.SignIn());
            }

            if (!game.IsDeployedBuild)
                checkForUpdatesButtonCore.Hide();

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

            loadBtn.ClickAction = async _ =>
            {
                ClearPlaylistItems();
                await SetVideoSource(videoIdBox.Text);
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

            loadPlaylistOpenButton.ClickAction = _ =>
            {
                hideOverlays();
                showOverlayContainer(loadPlaylistContainer);
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
            };

            settingsOverlayShowBtn.Action = () =>
            {
                hideOverlays();
                showOverlayContainer(settingsContainer);
            };

            windowModeDropdown.Current.BindValueChanged(_ =>
            {
                updateDisplaySettingsVisibility();
            }, true);

            currentDisplay.BindValueChanged(display => Schedule(() =>
            {
                if (display.NewValue == null)
                {
                    resolutionsFullscreen.Clear();
                    resolutionsWindowed.Clear();
                    return;
                }

                var buffer = new Bindable<Size>(windowedResolution.Value);
                resolutionWindowedDropdown.Current = buffer;

                var fullscreenResolutions = display.NewValue.DisplayModes
                                                   .Where(m => m.Size.Width >= 800 && m.Size.Height >= 600)
                                                   .OrderByDescending(m => Math.Max(m.Size.Height, m.Size.Width))
                                                   .Select(m => m.Size)
                                                   .Distinct()
                                                   .ToList();
                var windowedResolutions = fullscreenResolutions
                                          .Where(res => res.Width <= display.NewValue.UsableBounds.Width && res.Height <= display.NewValue.UsableBounds.Height)
                                          .ToList();

                resolutionsFullscreen.ReplaceRange(1, resolutionsFullscreen.Count - 1, fullscreenResolutions);
                resolutionsWindowed.ReplaceRange(0, resolutionsWindowed.Count, windowedResolutions);

                resolutionWindowedDropdown.Current = windowedResolution;

                updateDisplaySettingsVisibility();
            }), true);

            windowedResolution.BindValueChanged(size =>
            {
                if (size.NewValue == sizeWindowed.Value || windowModeDropdown.Current.Value != WindowMode.Windowed)
                    return;

                if (window?.WindowState == osu.Framework.Platform.WindowState.Maximised)
                {
                    window.WindowState = osu.Framework.Platform.WindowState.Normal;
                }

                // Adjust only for top decorations (assuming system titlebar).
                // Bottom/left/right borders are ignored as invisible padding, which don't align with the screen.
                var dBounds = currentDisplay.Value.Bounds;
                var dUsable = currentDisplay.Value.UsableBounds;
                float topBar = host.Window?.BorderSize.Value.Top ?? 0;

                int w = Math.Min(size.NewValue.Width, dUsable.Width);
                int h = (int)Math.Min(size.NewValue.Height, dUsable.Height - topBar);

                windowedResolution.Value = new Size(w, h);
                sizeWindowed.Value = windowedResolution.Value;

                float adjustedY = Math.Max(
                    dUsable.Y + (dUsable.Height - h) / 2f,
                    dUsable.Y + topBar // titlebar adjustment
                );
                windowedPositionY.Value = dBounds.Height - h != 0 ? (adjustedY - dBounds.Y) / (dBounds.Height - h) : 0;
                windowedPositionX.Value = dBounds.Width - w != 0 ? (dUsable.X - dBounds.X + (dUsable.Width - w) / 2f) / (dBounds.Width - w) : 0;
            });

            sizeWindowed.BindValueChanged(size =>
            {
                if (size.NewValue != windowedResolution.Value)
                    windowedResolution.Value = size.NewValue;
            });
        }

        private void hideOverlays()
        {
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
            Schedule(async () =>
            {
                Playlist playlist = api.GetPlaylistInfo(playlistId);
                IList<PlaylistItem> playlistItems = await api.GetPlaylistItems(playlistId);

                SetPlaylistInfo(playlist);
                await SetPlaylistItems(playlistItems);

                playlistIdBox.Text = string.Empty;
            });
        }

        private List<OverlayContainer> overlayContainers = new List<OverlayContainer>();

        [Resolved]
        private VolumeOverlay volume { get; set; }

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

                case GlobalAction.Back:
                    hideOverlays();
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
                    if (!reportAbuseOverlay.IsVisible)
                    {
                        hideOverlays();
                        showOverlayContainer(reportAbuseOverlay);
                    }
                    else
                        hideOverlayContainer(reportAbuseOverlay);

                    return true;

                case GlobalAction.OpenComments:
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

                case GlobalAction.Select:
                    if (isLoadVideoContainerVisible)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await SetVideoSource(videoIdBox.Text);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"¿¹¿Ü ¹ß»ý: {ex.Message}");
                            }
                        });
                    }
                    return true;

                case GlobalAction.PlayPause:
                    if (currentVideoSource != null)
                    {
                        if (currentVideoSource.IsPlaying())
                            currentVideoSource.Pause(true);
                        else
                            currentVideoSource.Play(true);
                    }
                    return true;

                case GlobalAction.PrevVideo:
                    if (currentVideoSource != null)
                    {
                        if (playlists.Count > 0)
                        {
                            if (playlistItemIndex != 0)
                                playlistItemIndex--;

                            Task.Run(async () => await SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId));
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

                            Task.Run(async () => await SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId));
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
            }

            return false;
        }

        private void restartApp()
        {
            throw new NotImplementedException();
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

        public void ClearPlaylistItems()
        {
            playlists.Clear();
            playlistItemViews.Clear();

            foreach (var item in playlistItemsView.Children)
            {
                Schedule(() => item.Expire());
            }

            playlistName.Text = "please choose a playlist!";
            playlistAuthor.Text = "[no metadata available]";

            if (playlists.Count == 0)
            {
                Schedule(() => prevVideoButton.Enabled.Value = false);
                Schedule(() => nextVideoButton.Enabled.Value = false);
            }
        }

        private Google.Apis.YouTube.v3.Data.Video videoData;

        public async Task SetPlaylistItems(IList<PlaylistItem> playlists)
        {
            this.playlists = playlists;

            playlistItemViews.Clear();

            foreach (var item in playlistItemsView.Children)
            {
                Schedule(() => item.Expire());
            }

            foreach (var item in playlists)
            {
                Google.Apis.YouTube.v3.Data.Video videoData = api.GetVideo(item.Snippet.ResourceId.VideoId);

                PlaylistItemView playlistItemView = new PlaylistItemView(playlists.IndexOf(item))
                {
                    RelativeSizeAxes = Axes.X,
                    Enabled = { Value = true },
                    ClickAction = async v =>
                    {
                        Schedule(async () =>
                        {
                            playlistItemIndex = playlists.IndexOf(item);
                            await SetVideoSource(item.Snippet.ResourceId.VideoId);
                        });
                    },
                };

                playlistItemViews.Add(playlistItemView);

                Schedule(() =>
                {
                    playlistItemView.Data = videoData;
                    playlistItemsView.Add(playlistItemView);
                    playlistItemView.UpdateData();
                });
            }

            await SetVideoSource(playlists[0].Snippet.ResourceId.VideoId);
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

            if (game.UseSystemCursor.Value == true)
            {
                game.SetCursorVisibility(CursorVisible);
            }

            if (currentVideoSource != null)
            {
                playPause.Icon = (currentVideoSource.IsPlaying() ? FontAwesome.Solid.Pause : FontAwesome.Solid.Play);
                playPause.TooltipText = (currentVideoSource.IsPlaying() ? YTPlayerEXStrings.Pause : YTPlayerEXStrings.Play);
                videoProgress.MaxValue = currentVideoSource.VideoProgress.MaxValue;

                videoPlaying.Value = currentVideoSource.IsPlaying();

                TimeSpan duration = TimeSpan.FromSeconds(currentVideoSource.VideoProgress.Value);
                if (duration.Hours > 0)
                {
                    currentTime.Text = $"{duration.Hours.ToString("00")}:{duration.Minutes.ToString("00")}:{duration.Seconds.ToString("00")}";
                }
                else
                {
                    currentTime.Text = $"{duration.Minutes.ToString("0")}:{duration.Seconds.ToString("00")}";
                }

                if (seekbar.IsDragged == false)
                    videoProgress.Value = currentVideoSource.VideoProgress.Value;
            }
        }

        private IconButton playPause;

        private bool commentsDisabled = false;

        /*
        public void GetLocalizedVideoDescriptionRemake(Google.Apis.YouTube.v3.Data.Video videoData)
        {
            string[] splitArg = new string[] { " " };

            string str = api.GetLocalizedVideoDescription(videoData);
            string pattern = @"https?://[^\s/$.?#].[^\s]*"; // Basic URL pattern

            videoDescription.Text = "";

            MatchCollection matches = Regex.Matches(str, pattern);
            foreach (Match match in matches)
            {
                Logger.Log($"Found URL: {match.Value}");
                // To get the end part of a specific match:
                string url = match.Value;
                string lastSegment = url.Split('/').Last();
                if (url.Contains("https://"))
                {
                    videoDescription.AddText(str[..str.IndexOf(url)]);
                    videoDescription.AddLink(str[str.IndexOf("https://")..(url.Length + str.IndexOf("https://"))], str[str.IndexOf("https://")..(url.Length + str.IndexOf("https://"))]);
                    videoDescription.AddText(str[(url.Length + str.IndexOf("https://"))..]);
                }
                else
                {
                    videoDescription.AddText(str[..str.IndexOf("http://")]);
                    videoDescription.AddLink(str[str.IndexOf("http://")..(url.Length + str.IndexOf("https://"))], str[str.IndexOf("http://")..(url.Length + str.IndexOf("https://"))]);
                    videoDescription.AddText(str[(url.Length + str.IndexOf("https://"))..]);
                }
            }
        }
        */

        private partial class PlaybackSpeedSliderBar : RoundedSliderBar<double>
        {
            public override LocalisableString TooltipText => YTPlayerEXStrings.PlaybackSpeed(Current.Value);
        }

        private void updateRatingButtons(string videoId, bool ratingButtonsEnabled)
        {
            if (!googleOAuth2.SignedIn.Value)
                return;

            Task.Run(async () =>
            {
                VideosResource.RateRequest.RatingEnum things = await api.GetVideoRating(videoId);

                switch (things)
                {
                    case VideosResource.RateRequest.RatingEnum.None:
                    {
                        Schedule(() =>
                        {
                            dislikeButtonBackgroundSelected.Hide();
                            likeButtonBackgroundSelected.Hide();
                            likeButtonForeground.Colour = dislikeButtonForeground.Colour = overlayColourProvider1.Content2;

                            if (ratingButtonsEnabled)
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Like);
                                    Schedule(() =>
                                    {
                                        dislikeButtonBackgroundSelected.Hide();
                                        likeButtonBackgroundSelected.Show();
                                        likeButtonForeground.Colour = overlayColourProvider1.Background4;
                                        dislikeButtonForeground.Colour = overlayColourProvider1.Content2;
                                    });
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Dislike);
                                    Schedule(() =>
                                    {
                                        dislikeButtonBackgroundSelected.Show();
                                        likeButtonBackgroundSelected.Hide();
                                        likeButtonForeground.Colour = overlayColourProvider1.Content2;
                                        dislikeButtonForeground.Colour = overlayColourProvider1.Background4;
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
                            }
                        });
                        break;
                    }
                    case VideosResource.RateRequest.RatingEnum.Like:
                    {
                        Schedule(() =>
                        {
                            dislikeButtonBackgroundSelected.Hide();
                            likeButtonBackgroundSelected.Show();
                            likeButtonForeground.Colour = overlayColourProvider1.Background4;
                            dislikeButtonForeground.Colour = overlayColourProvider1.Content2;

                            if (ratingButtonsEnabled)
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.None);
                                    Schedule(() =>
                                    {
                                        dislikeButtonBackgroundSelected.Hide();
                                        likeButtonBackgroundSelected.Hide();
                                        likeButtonForeground.Colour = dislikeButtonForeground.Colour = overlayColourProvider1.Content2;
                                    });
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Dislike);
                                    Schedule(() =>
                                    {
                                        dislikeButtonBackgroundSelected.Show();
                                        likeButtonBackgroundSelected.Hide();
                                        likeButtonForeground.Colour = overlayColourProvider1.Content2;
                                        dislikeButtonForeground.Colour = overlayColourProvider1.Background4;
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
                            }
                        });
                        break;
                    }
                    case VideosResource.RateRequest.RatingEnum.Dislike:
                    {
                        Schedule(() =>
                        {
                            dislikeButtonBackgroundSelected.Show();
                            likeButtonBackgroundSelected.Hide();
                            likeButtonForeground.Colour = overlayColourProvider1.Content2;
                            dislikeButtonForeground.Colour = overlayColourProvider1.Background4;

                            if (ratingButtonsEnabled)
                            {
                                likeButton.ClickAction = async _ =>
                                {
                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.Like);
                                    Schedule(() =>
                                    {
                                        dislikeButtonBackgroundSelected.Hide();
                                        likeButtonBackgroundSelected.Show();
                                        likeButtonForeground.Colour = overlayColourProvider1.Background4;
                                        dislikeButtonForeground.Colour = overlayColourProvider1.Content2;
                                    });
                                };

                                dislikeButton.ClickAction = async _ =>
                                {
                                    await api.RateVideo(videoId, VideosResource.RateRequest.RatingEnum.None);
                                    Schedule(() =>
                                    {
                                        dislikeButtonBackgroundSelected.Hide();
                                        likeButtonBackgroundSelected.Hide();
                                        likeButtonForeground.Colour = dislikeButtonForeground.Colour = overlayColourProvider1.Content2;
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
                            }
                        });
                        break;
                    }
                }
            });
        }

        [Resolved]
        private OverlayColourProvider overlayColourProvider1 { get; set; }

        private void updateVideoMetadata(string videoId)
        {
            videoMetadataDisplay.UpdateVideo(videoId);
            videoMetadataDisplayDetails.UpdateVideo(videoId);
            Task.Run(async () =>
            {
                // metadata area
                videoData = api.GetVideo(videoId);
                updateRatingButtons(videoId, videoData.Statistics.LikeCount != null);

                Schedule(() => commentOpenButton.Enabled.Value = videoData.Statistics.CommentCount != null);

                if (googleOAuth2.SignedIn.Value)
                    Schedule(() => reportOpenButton.Enabled.Value = true);

                commentsDisabled = videoData.Statistics.CommentCount == null;

                if (videoData.Statistics.CommentCount != null)
                    Schedule(() => commentOpenButtonDetails.Show());
                else
                    Schedule(() => commentOpenButtonDetails.Hide());

                game.RequestUpdateWindowTitle($"{videoData.Snippet.ChannelTitle} - {videoData.Snippet.Title}");

                DateTimeOffset? dateTime = videoData.Snippet.PublishedAtDateTimeOffset;
                DateTime now = DateTime.Now;
                if (!string.IsNullOrEmpty(api.GetLocalizedVideoDescription(videoData)))
                {
                    Schedule(() => videoDescription.Text = api.GetLocalizedVideoDescription(videoData));
                }
                else
                {
                    Schedule(() => videoDescription.AddText(YTPlayerEXStrings.NoDescription, text =>
                    {
                        text.Font = YouTubePlayerEXApp.DefaultFont.With(weight: "SemiBold");
                        text.Colour = overlayColourProvider1.Background1;
                    }));
                }
                sessionStatics.GetBindable<string>(Static.CurrentThumbnailUrl).Value = videoData.Snippet.Thumbnails.High.Url;
                commentCount.Text = videoData.Statistics.CommentCount != null ? Convert.ToInt32(videoData.Statistics.CommentCount).ToStandardFormattedString(0) : YTPlayerEXStrings.DisabledByUploader;
                try
                {
                    dislikeCount.Text = ReturnYouTubeDislike.GetDislikes(videoId).Dislikes > 0 ? Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).Dislikes).ToMetric(decimals: 2) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).RawDislikes).ToMetric(decimals: 2);
                    dislikeButton.TooltipText = YTPlayerEXStrings.DislikeCountTooltip(ReturnYouTubeDislike.GetDislikes(videoId).Dislikes.ToStandardFormattedString(0), ReturnYouTubeDislike.GetDislikes(videoId).RawDislikes.ToStandardFormattedString(0));
                }
                catch
                {
                    dislikeCount.Text = "0";
                }

                string uploadDateRaw = videoData.Snippet.PublishedAtRaw;

                DateTime.TryParseExact(uploadDateRaw, @"yyyy-MM-dd\THH:mm:ss\Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var uploadDate);

                likeCount.Text = videoData.Statistics.LikeCount != null ? Convert.ToDouble(videoData.Statistics.LikeCount).ToMetric(decimals: 2) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoId).RawLikes).ToMetric(decimals: 2);
                commentsContainerTitle.Text = YTPlayerEXStrings.Comments(videoData.Statistics.CommentCount != null ? Convert.ToInt32(videoData.Statistics.CommentCount).ToStandardFormattedString(0) : YTPlayerEXStrings.Disabled);
                videoInfoDetails.Text = YTPlayerEXStrings.VideoMetadataDescWithoutChannelName(Convert.ToInt32(videoData.Statistics.ViewCount).ToStandardFormattedString(0), uploadDate.ToString());

                Schedule(() =>
                {
                    updatePresence(discordRichPresence.Value);

                    videoMetadataDisplayDetails.SubscribeClickAction = () =>
                    {
                        Task.Run(async () =>
                        {
                            bool result = await api.IsChannelSubscribed(videoData.Snippet.ChannelId);
                            string subscriptionId = await api.GetSubscriptionId(videoData.Snippet.ChannelId);

                            Logger.Log("SubscribeClickAction clicked");

                            if (result)
                            {
                                Schedule(() => youtubeChannelMetadataDisplay.UpdateUser(api.GetChannel(videoData.Snippet.ChannelId)));

                                declineButton.ClickAction = async _ =>
                                {
                                    hideOverlayContainer(unsubscribeDialog);
                                };
                                acceptButton.ClickAction = async _ =>
                                {
                                    hideOverlayContainer(unsubscribeDialog);
                                    await api.UnsubscribeChannel(subscriptionId);

                                    Logger.Log("UnsubscribeChannel()");

                                    Toast toast = new Toast(YTPlayerEXStrings.General, YTPlayerEXStrings.SubscriptionRemoved);

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

                                Toast toast = new Toast(YTPlayerEXStrings.General, YTPlayerEXStrings.SubscriptionAdded);

                                Schedule(() => onScreenDisplay.Display(toast));
                                Schedule(() => videoMetadataDisplayDetails.UpdateChannelSubscribeState(videoData.Snippet.ChannelId));
                            }
                        });
                    };

                    reportButton.Action = () =>
                    {
                        if (!googleOAuth2.SignedIn.Value)
                            return;

                        Toast toast = new Toast(YTPlayerEXStrings.Report, YTPlayerEXStrings.ReportSuccess);
                        api.ReportAbuse(videoId, reportReason.Current.Value.Id, (reportReason.Current.Value.ContainsSecondaryReasons ? reportSubReason.Current.Value.Id : null), (!string.IsNullOrEmpty(reportComment.Current.Value) ? reportComment.Current.Value : null));
                        Schedule(() => onScreenDisplay.Display(toast));
                        reportComment.Current.Value = string.Empty;
                        reportReason.Current.Value = reportReason.Items.ToArray()[0];
                        reportSubReason.Current.Value = reportSubReason.Items.ToArray()[0];
                        hideOverlayContainer(reportAbuseOverlay);
                    };

                    commentSendButton.ClickAction = _ =>
                    {
                        if (!googleOAuth2.SignedIn.Value)
                            return;

                        Toast toast = new Toast(YTPlayerEXStrings.General, YTPlayerEXStrings.CommentAdded);
                        api.SendComment(videoId, commentTextBox.Text);

                        Task.Run(async () =>
                        {
                            Channel myChannel = await api.GetMineChannelAsync();

                            Comment dummy = new Comment();

                            CommentSnippet wth = new CommentSnippet
                            {
                                PublishedAtDateTimeOffset = DateTimeOffset.Now,
                                AuthorChannelId = { Value = myChannel.Id },
                                TextDisplay = commentTextBox.Text,
                                TextOriginal = commentTextBox.Text,
                                LikeCount = 0,
                            };

                            dummy.Snippet = wth;

                            Schedule(() =>
                            {
                                commentContainer.Add(new CommentDisplay(dummy)
                                {
                                    RelativeSizeAxes = Axes.X,
                                });
                            });
                        });

                        Schedule(() => onScreenDisplay.Display(toast));

                        commentTextBox.Text = string.Empty;
                    };
                });

                // comments area
                IList<CommentThread> commentThreadData = api.GetCommentThread(videoId);
                foreach (CommentThread item in commentThreadData)
                {
                    if (item.Snippet.IsPublic == true)
                    {
                        Task.Run(async () =>
                        {
                            Comment comment = await api.GetComment(item.Id);

                            Schedule(() =>
                            {
                                commentContainer.Add(new CommentDisplay(comment)
                                {
                                    RelativeSizeAxes = Axes.X,
                                });
                            });
                        });
                    }
                }

                usernameDisplayMode.BindValueChanged(locale =>
                {
                    Schedule(() =>
                    {
                        if (api.TryToGetMineChannel() != null)
                            commentTextBox.PlaceholderText = YTPlayerEXStrings.CommentWith(api.GetLocalizedChannelTitle(api.GetMineChannel()));
                    });
                }, true);

                localeBindable.BindValueChanged(locale =>
                {
                    Task.Run(async () =>
                    {
                        Schedule(() => videoDescription.Text = api.GetLocalizedVideoDescription(videoData));
                        videoInfoDetails.Text = YTPlayerEXStrings.VideoMetadataDescWithoutChannelName(Convert.ToInt32(videoData.Statistics.ViewCount).ToStandardFormattedString(0), uploadDate.ToString());
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
                if (duration.Hours > 0)
                {
                    totalTime.Text = $"{duration.Hours.ToString("0")}:{duration.Minutes.ToString("00")}:{duration.Seconds.ToString("00")}";
                }
                else
                {
                    totalTime.Text = $"{duration.Minutes.ToString("0")}:{duration.Seconds.ToString("00")}";
                }
            });
        }

        private Bindable<UsernameDisplayMode> usernameDisplayMode;

        private void addVideoToScreen()
        {
            string audioFile = app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/audio.mp3";

            AudioNormalization audioNormalization = new AudioNormalization(audioFile);

            if (audioNormalization.IntegratedLoudness == null)
            {
                Logger.Log($"Failed to calculate audio normalization values for {api.GetChannel(videoData.Snippet.ChannelId)} - {videoData.Snippet.Title}", LoggingTarget.Runtime, LogLevel.Error);
            }

            app.CurrentTrackNormalizeVolume.Value = audioNormalization?.IntegratedLoudnessInVolumeOffset ?? AudioNormalizationManager.FALLBACK_VOLUME;

            videoContainer.Add(currentVideoSource);

            videoLoadingProgress.Text = "";

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
                    if (playlistItemIndex != playlists.Count - 1)
                        playlistItemIndex++;

                    await SetVideoSource(playlists[playlistItemIndex].Snippet.ResourceId.VideoId);
                };
            }
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

        [Resolved]
        private YTPlayerEXConfigManager appGlobalConfig { get; set; }

        public async Task SetVideoSource(string videoId, bool clearCache = false, LoadType loadType = LoadType.Full)
        {
            this.videoId = videoId;
            pausedTime = clearCache ? currentVideoSource.VideoProgress.Value : 0;
            Schedule(() => currentVideoSource?.Expire());
            if (loadVideoContainer.IsVisible == true)
            {
                Schedule(() => hideOverlayContainer(loadVideoContainer));
            }
            if (searchContainer.IsVisible == true)
            {
                Schedule(() => hideOverlayContainer(searchContainer));
            }
            if (loadPlaylistContainer.IsVisible == true)
            {
                Schedule(() => hideOverlayContainer(loadPlaylistContainer));
            }
            if (playlistOverlay.IsVisible == true)
            {
                Schedule(() => hideOverlayContainer(playlistOverlay));
            }

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

            videoIdBox.Text = string.Empty;

            foreach (var item in commentContainer.Children)
            {
                Schedule(() => item.Expire());
            }

            if (clearCache == true)
            {
                await Task.Delay(1000); // Wait for any ongoing operations to complete
                switch (loadType)
                {
                    case LoadType.Full:
                    {
                        foreach (var cacheItem in Directory.GetFiles(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}")))
                        {
                            File.Delete(cacheItem);
                        }
                        break;
                    }
                    case LoadType.VideoOnly:
                    {
                        File.Delete(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/video.mp4");
                        break;
                    }
                    case LoadType.AudioOnly:
                    {
                        File.Delete(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/audio.mp3");
                        break;
                    }
                }
            }

            if (videoId.Length != 0)
            {
                Google.Apis.YouTube.v3.Data.Video videoData = api.GetVideo(videoId);

                if (videoData.Status.PrivacyStatus == "private")
                {
                    Schedule(() =>
                    {
                        Toast toast = new Toast(YTPlayerEXStrings.General, YTPlayerEXStrings.CannotPlayPrivateVideos);

                        onScreenDisplay.Display(toast);
                    });
                    return;
                }

                /*
                IProgress<double> audioDownloadProgress = new Progress<double>((percent) => videoLoadingProgress.Text = $"Downloading audio cache: {(percent * 100):N0}%");
                IProgress<double> videoDownloadProgress = new Progress<double>((percent) => videoLoadingProgress.Text = $"Downloading video cache: {(percent * 100):N0}%");
                */

                spinnerShow = Scheduler.AddDelayed(spinner.Show, 0);

                Schedule(() => videoProgress.MaxValue = 1);
                videoUrl = $"https://youtube.com/watch?v={videoId}";

                spinnerShow = Scheduler.AddDelayed(() => updateVideoMetadata(videoId), 0);
                Schedule(() => idleBackground.Hide());
                Schedule(() => thumbnailContainer.Show());

                if (!File.Exists(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/audio.mp3") || !File.Exists(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/video.mp4"))
                {
                    Schedule(() => videoQuality.Disabled = audioLanguage.Disabled = alwaysUseOriginalAudio.Disabled = true);

                    if (loadType == LoadType.Full)
                        Directory.CreateDirectory(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}"));

                    var streamManifest = await app.YouTubeClient.Videos.Streams.GetManifestAsync(videoUrl);

                    IStreamInfo audioStreamInfo;

                    try
                    {
                        if (alwaysUseOriginalAudio.Value == true)
                        {
                            Logger.Log($"Preferred audio language is: {videoData.Snippet.DefaultLanguage}");
                            // Select best audio stream (highest bitrate)
                            audioStreamInfo = streamManifest
                                .GetAudioOnlyStreams()
                                .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                .TryGetWithHighestBitrate();
                        }
                        else
                        {
                            Logger.Log($"Preferred audio language is: {appGlobalConfig.Get<Language>(YTPlayerEXSetting.AudioLanguage).ToString()}");
                            // Select best audio stream (highest bitrate)
                            audioStreamInfo = streamManifest
                                .GetAudioOnlyStreams()
                                .Where(s => s.AudioLanguage.Value.Code.Contains(appGlobalConfig.Get<Language>(YTPlayerEXSetting.AudioLanguage).ToString()))
                                .TryGetWithHighestBitrate();
                        }
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            // Select best audio stream (highest bitrate)
                            audioStreamInfo = streamManifest
                                .GetAudioOnlyStreams()
                                .Where(s => s.AudioLanguage.Value.Code.Contains(videoData.Snippet.DefaultLanguage))
                                .TryGetWithHighestBitrate();
                            Logger.Error(e, e.GetDescription());
                            Logger.Log($"Prefer default audio language: {videoData.Snippet.DefaultLanguage}");
                        }
                        catch
                        {
                            Logger.Log($"Prefer default audio language failed.\nFalling back to default audio language.");
                            // Select best audio stream (highest bitrate)
                            audioStreamInfo = streamManifest
                                .GetAudioOnlyStreams()
                                .TryGetWithHighestBitrate();
                        }
                    }

                    IVideoStreamInfo videoStreamInfo;

                    if (videoQuality.Value == Config.VideoQuality.PreferHighQuality)
                    {
                        // Select best video stream (1080p60 in this example)
                        videoStreamInfo = streamManifest
                            .GetVideoOnlyStreams()
                            .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                            .TryGetWithHighestVideoQuality();

                        Toast toast = new Toast(YTPlayerEXStrings.VideoQuality, videoStreamInfo.VideoQuality.Label);

                        onScreenDisplay.Display(toast);
                    }
                    else
                    {
                        // Select best video stream (1080p60 in this example)
                        videoStreamInfo = streamManifest
                            .GetVideoOnlyStreams()
                            .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                            .Where(s => s.VideoQuality.Label.Contains(app.ParseVideoQuality()))
                            .TryGetWithHighestVideoQuality();

                        Toast toast = new Toast(YTPlayerEXStrings.VideoQuality, videoStreamInfo.VideoQuality.Label);

                        onScreenDisplay.Display(toast);
                    }

                    await captionLangDropdown.RefreshCaptionLanguages(videoUrl);
                    captionEnabled.Disabled = false;

                    ClosedCaptionTrack captionTrack = null;

                    try
                    {
                        if (captionEnabled.Value)
                        {
                            var trackManifest = await game.YouTubeClient.Videos.ClosedCaptions.GetManifestAsync(videoUrl);

                            captionEnabled.Disabled = trackManifest.Tracks.Count == 0;

                            var trackInfo = trackManifest.Tracks.Where(track => track.Language.Name == captionLangDropdown.Current.Value.Name).First();

                            if (trackInfo != null)
                            {
                                if (captionEnabled.Value)
                                {
                                    Schedule(() =>
                                    {
                                        /*
                                        alert.Text = captionLanguage.Value != ClosedCaptionLanguage.Disabled ? (trackInfo.IsAutoGenerated ? YTPlayerEXStrings.SelectedCaptionAutoGen(captionLanguage.Value.GetLocalisableDescription()) : YTPlayerEXStrings.SelectedCaption(captionLanguage.Value.GetLocalisableDescription())) : YTPlayerEXStrings.SelectedCaption(captionLanguage.Value.GetLocalisableDescription());
                                        alert.Show();
                                        spinnerShow = Scheduler.AddDelayed(alert.Hide, 3000);
                                        */

                                        Toast toast = new Toast(YTPlayerEXStrings.CaptionLanguage, captionLangDropdown.Current.Value.Name);

                                        onScreenDisplay.Display(toast);
                                    });
                                }

                                captionTrack = await game.YouTubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);
                            }
                        }
                        else
                        {
                            currentVideoSource.UpdateCaptionTrack(null);
                        }
                    }
                    catch (Exception e)
                    {
                        captionEnabled.Disabled = true;
                        Logger.Error(e, e.GetDescription());
                    }

                    switch (loadType)
                    {
                        case LoadType.Full:
                        {
                            await app.YouTubeClient.Videos.DownloadAsync([audioStreamInfo], new ConversionRequestBuilder(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"\audio.mp3").Build());
                            await app.YouTubeClient.Videos.DownloadAsync([videoStreamInfo], new ConversionRequestBuilder(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"\video.mp4").Build());
                            break;
                        }
                        case LoadType.AudioOnly:
                        {
                            await app.YouTubeClient.Videos.DownloadAsync([audioStreamInfo], new ConversionRequestBuilder(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"\audio.mp3").Build());
                            break;
                        }
                        case LoadType.VideoOnly:
                        {
                            await app.YouTubeClient.Videos.DownloadAsync([videoStreamInfo], new ConversionRequestBuilder(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"\video.mp4").Build());
                            break;
                        }
                    }

                    currentVideoSource = new YouTubeVideoPlayer(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/video.mp4", app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/audio.mp3", captionTrack, videoData, pausedTime)
                    {
                        RelativeSizeAxes = Axes.Both
                    };

                    spinnerShow = Scheduler.AddDelayed(spinner.Hide, 0);

                    spinnerShow = Scheduler.AddDelayed(addVideoToScreen, 0);

                    spinnerShow = Scheduler.AddDelayed(() => playVideo(), 0);
                    Schedule(() => thumbnailContainer.Hide());

                    Schedule(() => videoQuality.Disabled = audioLanguage.Disabled = alwaysUseOriginalAudio.Disabled = false);
                }
                else
                {
                    var streamManifest = await app.YouTubeClient.Videos.Streams.GetManifestAsync(videoUrl);

                    IVideoStreamInfo videoStreamInfo;

                    if (videoQuality.Value == Config.VideoQuality.PreferHighQuality)
                    {
                        // Select best video stream (1080p60 in this example)
                        videoStreamInfo = streamManifest
                            .GetVideoOnlyStreams()
                            .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                            .TryGetWithHighestVideoQuality();

                        Toast toast = new Toast(YTPlayerEXStrings.VideoQuality, videoStreamInfo.VideoQuality.Label);

                        onScreenDisplay.Display(toast);
                    }
                    else
                    {
                        try
                        {
                            // Select best video stream (1080p60 in this example)
                            videoStreamInfo = streamManifest
                                .GetVideoOnlyStreams()
                                .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                                .Where(s => s.VideoQuality.Label.Contains(app.ParseVideoQuality()))
                                .TryGetWithHighestVideoQuality();

                            Toast toast = new Toast(YTPlayerEXStrings.VideoQuality, videoStreamInfo.VideoQuality.Label);

                            onScreenDisplay.Display(toast);
                        }
                        catch
                        {
                            // Select best video stream (1080p60 in this example)
                            videoStreamInfo = streamManifest
                                .GetVideoOnlyStreams()
                                .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                                .TryGetWithHighestVideoQuality();

                            Toast toast = new Toast(YTPlayerEXStrings.VideoQuality, videoStreamInfo.VideoQuality.Label);

                            onScreenDisplay.Display(toast);
                        }
                    }

                    await captionLangDropdown.RefreshCaptionLanguages(videoUrl);
                    captionEnabled.Disabled = false;

                    ClosedCaptionTrack captionTrack = null;

                    try
                    {
                        if (captionEnabled.Value)
                        {
                            var trackManifest = await game.YouTubeClient.Videos.ClosedCaptions.GetManifestAsync(videoUrl);

                            captionEnabled.Disabled = trackManifest.Tracks.Count == 0;

                            var trackInfo = trackManifest.Tracks.Where(track => track.Language.Name == captionLangDropdown.Current.Value.Name).First();

                            if (trackInfo != null)
                            {
                                if (captionEnabled.Value)
                                {
                                    Schedule(() =>
                                    {
                                        Toast toast = new Toast(YTPlayerEXStrings.CaptionLanguage, captionLangDropdown.Current.Value.Name);

                                        onScreenDisplay.Display(toast);
                                    });
                                }

                                captionTrack = await game.YouTubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);
                            }
                        }
                        else
                        {
                            currentVideoSource.UpdateCaptionTrack(null);
                        }
                    }
                    catch (Exception e)
                    {
                        captionEnabled.Disabled = true;
                        Logger.Error(e, e.GetDescription());
                    }

                    currentVideoSource = new YouTubeVideoPlayer(app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/video.mp4", app.Host.CacheStorage.GetStorageForDirectory("videos").GetFullPath($"{videoId}") + @"/audio.mp3", captionTrack, videoData, pausedTime)
                    {
                        RelativeSizeAxes = Axes.Both
                    };

                    spinnerShow = Scheduler.AddDelayed(spinner.Hide, 0);

                    spinnerShow = Scheduler.AddDelayed(addVideoToScreen, 0);

                    spinnerShow = Scheduler.AddDelayed(() => playVideo(), 0);
                    Schedule(() => thumbnailContainer.Hide());
                }
            }
            else
            {
                Toast toast = new Toast(YTPlayerEXStrings.General, YTPlayerEXStrings.NoVideoIdError);

                onScreenDisplay.Display(toast);
            }
        }

        private Storage exportStorage = null!;

        [Resolved]
        private OnScreenDisplay onScreenDisplay { get; set; }

#nullable enable
        private void exportLogs()
        {
            const string archive_filename = "compressed-logs.zip";

            try
            {
                GlobalStatistics.OutputToLog();
                Logger.Flush();

                var logStorage = Logger.Storage;

                using (var outStream = exportStorage.CreateFileSafely(archive_filename))
                using (var zip = ZipArchive.Create())
                {
                    foreach (string? f in logStorage.GetFiles(string.Empty, "*.log"))
                        FileUtils.AttemptOperation(z => z.AddEntry(f, logStorage.GetStream(f), closeStream: true), zip, throwOnFailure: false);

                    zip.SaveTo(outStream);
                }
            }
            catch
            {
                // cleanup if export is failed or canceled.
                exportStorage.Delete(archive_filename);
                throw;
            }

            Schedule(() =>
            {
                Toast toast = new Toast(YTPlayerEXStrings.General, YTPlayerEXStrings.LogsExportFinished);

                onScreenDisplay.Display(toast);
                exportStorage.PresentFileExternally(archive_filename);
            });
        }
#nullable disable

        [Resolved]
        private YouTubePlayerEXApp game { get; set; }

        private enum GCLatencyMode
        {
            Default,
            Interactive,
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

#nullable enable
        /// <summary>
        /// Contrary to <see cref="Display.Equals(osu.Framework.Platform.Display?)"/>, this comparer disregards the value of <see cref="Display.Bounds"/>.
        /// We want to just show a list of displays, and for the purposes of settings we don't care about their bounds when it comes to the list.
        /// However, <see cref="IWindow.DisplaysChanged"/> fires even if only the resolution of the current display was changed
        /// (because it causes the bounds of all displays to also change).
        /// We're not interested in those changes, so compare only the rest that we actually care about.
        /// This helps to avoid a bindable/event feedback loop, in which a resolution change
        /// would trigger a display "change", which would in turn reset resolution again.
        /// </summary>
        private class DisplayListComparer : IEqualityComparer<Display>
        {
            public static readonly DisplayListComparer DEFAULT = new DisplayListComparer();

            public bool Equals(Display? x, Display? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;

                return x.Index == y.Index
                       && x.Name == y.Name
                       && x.DisplayModes.SequenceEqual(y.DisplayModes);
            }

            public int GetHashCode(Display obj)
            {
                var hashCode = new HashCode();

                hashCode.Add(obj.Index);
                hashCode.Add(obj.Name);
                hashCode.Add(obj.DisplayModes.Length);
                foreach (var displayMode in obj.DisplayModes)
                    hashCode.Add(displayMode);

                return hashCode.ToHashCode();
            }
        }
#nullable disable

        private partial class RendererSettingsDropdown : FormEnumDropdown<RendererType>
        {
            private RendererType hostResolvedRenderer;
            private bool automaticRendererInUse;

            [BackgroundDependencyLoader]
            private void load(FrameworkConfigManager config, GameHost host)
            {
                var renderer = config.GetBindable<RendererType>(FrameworkSetting.Renderer);
                automaticRendererInUse = renderer.Value == RendererType.Automatic;
                hostResolvedRenderer = host.ResolvedRenderer;
            }

            protected override LocalisableString GenerateItemText(RendererType item)
            {
                if (item == RendererType.Automatic && automaticRendererInUse)
                    return YTPlayerEXStrings.RenderTypeAutomaticIsUse(hostResolvedRenderer.GetDescription());

                if (item == RendererType.Automatic)
                {
                    return YTPlayerEXStrings.RenderTypeAutomatic;
                }

                return base.GenerateItemText(item);
            }
        }

        private partial class ReportDropdown : FormDropdown<VideoAbuseReportReasonItem>
        {
            protected override LocalisableString GenerateItemText(VideoAbuseReportReasonItem item)
                => item.Label;
        }

        private partial class WindowModeDropdown : FormDropdown<WindowMode>
        {
            protected override LocalisableString GenerateItemText(WindowMode item)
            {
                switch (item)
                {
                    case WindowMode.Windowed:
                        return YTPlayerEXStrings.Windowed;

                    case WindowMode.Borderless:
                        return YTPlayerEXStrings.Borderless;

                    case WindowMode.Fullscreen:
                        return YTPlayerEXStrings.Fullscreen;
                }
                return base.GenerateItemText(item);
            }
        }

        private partial class AudioDeviceDropdown : FormDropdown<string>
        {
            protected override LocalisableString GenerateItemText(string item)
                => string.IsNullOrEmpty(item) ? YTPlayerEXStrings.Default : base.GenerateItemText(item);
        }

        private partial class PlaylistDropdown : FormDropdown<Playlist>
        {
            protected override LocalisableString GenerateItemText(Playlist item)
                => item.Snippet.Title;
        }

        private partial class FrameSyncDropdown : FormEnumDropdown<FrameSync>
        {
            protected override LocalisableString GenerateItemText(FrameSync item)
            {
                switch (item)
                {
                    case FrameSync.VSync:
                        return YTPlayerEXStrings.VSync;

                    case FrameSync.Limit2x:
                        return YTPlayerEXStrings.RefreshRate2X;

                    case FrameSync.Limit4x:
                        return YTPlayerEXStrings.RefreshRate4X;

                    case FrameSync.Limit8x:
                        return YTPlayerEXStrings.RefreshRate8X;

                    case FrameSync.Unlimited:
                        return YTPlayerEXStrings.Unlimited;
                }
                return base.GenerateItemText(item);
            }
        }

        private partial class YouTubeI18nLangDropdown : FormDropdown<YouTubeI18nLangItem>
        {
            [Resolved]
            private YouTubePlayerEXApp app { get; set; }

            [Resolved]
            private YTPlayerEXConfigManager config { get; set; }

            [Resolved]
            private YoutubeExplode.YoutubeClient youtubeService { get; set; }

            private Bindable<int> closedCaptionLanguageValue;

            [BackgroundDependencyLoader]
            private void load()
            {
            }

            public async Task RefreshCaptionLanguages(string videoId)
            {
                try
                {
                    var trackManifest = await youtubeService.Videos.ClosedCaptions.GetManifestAsync(videoId);

                    List<YouTubeI18nLangItem> items = new List<YouTubeI18nLangItem>();

                    foreach (var item in trackManifest.Tracks)
                    {
                        YouTubeI18nLangItem youTubeI18NLangItem = new YouTubeI18nLangItem
                        {
                            Hl = item.Language.Code,
                            Name = item.Language.Name,
                        };

                        items.Add(youTubeI18NLangItem);
                    }

                    Items = items;
                    Current.Value = Current.Default = items.Where(lang => lang.Hl.Contains(CultureInfo.CurrentCulture.TwoLetterISOLanguageName)).First();
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.GetDescription());
                }
            }

            protected override LocalisableString GenerateItemText(YouTubeI18nLangItem item)
            {
                try
                {
                    return item.Name;
                }
                catch
                {
                    return base.GenerateItemText(item);
                }
            }
        }

        public enum LoadType
        {
            Full,
            VideoOnly,
            AudioOnly,
        }
    }
}
