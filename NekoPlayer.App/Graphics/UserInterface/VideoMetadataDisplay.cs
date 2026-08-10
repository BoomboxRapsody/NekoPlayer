// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Humanizer;
using NekoPlayer.App.Config;
using NekoPlayer.App.Extensions;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Localisation;
using NekoPlayer.App.Online;
using NekoPlayer.App.Utils;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using PaletteNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YoutubeExplode.Videos.ClosedCaptions;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class VideoMetadataDisplay : CompositeDrawable
    {
        private ProfileImage profileImage;
        private TruncatingSpriteText videoName;
        private TruncatingSpriteText desc;
        public Action<VideoMetadataDisplay> ClickEvent;

        private Action subscribeClickAction;

        public Action SubscribeClickAction
        {
            get => subscribeClickAction;
            set
            {
                subscribeClickAction = value;
                subscribeButton.Action = value;
            }
        }

        private RoundedAdaptiveButtonV2 subscribeButton;

        private Box bgLayer, hover;

        [Resolved]
        private YouTubeAPI api { get; set; }

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; }

        [Resolved]
        private NekoPlayerConfigManager appConfig { get; set; }

        [Resolved]
        private NekoPlayerAppBase app { get; set; }

        private Bindable<Localisation.Language> uiLanguage;
        private Bindable<UsernameDisplayMode> usernameDisplayMode;
        private Bindable<VideoMetadataTranslateSource> translationSource = new Bindable<VideoMetadataTranslateSource>();

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            uiLanguage = app.CurrentLanguage.GetBoundCopy();
            usernameDisplayMode = appConfig.GetBindable<UsernameDisplayMode>(NekoPlayerSetting.UsernameDisplayMode);
            translationSource = appConfig.GetBindable<VideoMetadataTranslateSource>(NekoPlayerSetting.VideoMetadataTranslateSource);

            CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS;
            Masking = true;

            InternalChildren = new Drawable[]
            {
                samples,
                bgLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = overlayColourProvider.Background4,
                    Alpha = 1,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(7),
                    Children = new Drawable[]
                    {
                        profileImage = new ProfileImage(45),
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding
                            {
                                Vertical = 5,
                                Left = 50,
                                Right = 95,
                            },
                            Children = new Drawable[]
                            {
                                videoName = new TruncatingSpriteText
                                {
                                    Font = NekoPlayerApp.DefaultFont.With(size: 20, weight: "ExtraBold"),
                                    RelativeSizeAxes = Axes.X,
                                    Text = NekoPlayerStrings.VideoNotLoaded,
                                    Colour = overlayColourProvider.Content2,
                                },
                                desc = new TruncatingSpriteText
                                {
                                    Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "SemiBold"),
                                    RelativeSizeAxes = Axes.X,
                                    Colour = overlayColourProvider.Foreground2,
                                    Text = NekoPlayerStrings.VideoNotLoadedDesc,
                                    Position = new osuTK.Vector2(0, 20),
                                }
                            }
                        },
                        subscribeButton = new RoundedAdaptiveButtonV2
                        {
                            Enabled = { Value = true },
                            Width = 90,
                            Height = 30,
                            Text = NekoPlayerStrings.Subscribe,
                            Origin = Anchor.CentreRight,
                            Anchor = Anchor.CentreRight,
                        }
                    }
                },
                hover = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Blending = BlendingParameters.Additive,
                    Alpha = 0,
                },
            };

            subscribeButton.Action = SubscribeClickAction;
        }

        private Video videoData;

        protected override bool OnClick(ClickEvent e)
        {
            ClickEvent?.Invoke(this);

            return base.OnClick(e);
        }

        private HoverSounds samples = new HoverClickSounds(HoverSampleSet.Default);

        protected override bool OnHover(HoverEvent e)
        {
            if (ClickEvent != null)
                hover.FadeTo(0.1f, 500, Easing.OutQuint);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            if (ClickEvent != null)
                hover.FadeOut(500, Easing.OutQuint);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            (samples as HoverClickSounds).Enabled.Value = (ClickEvent != null);
        }

        public void UpdateChannelSubscribeState(string channelId)
        {
            Task.Run(async () =>
            {
                bool response = await api.IsChannelSubscribed(channelId);

                if (response == true)
                    subscribeButton.Text = NekoPlayerStrings.Unsubscribe;
                else
                    subscribeButton.Text = NekoPlayerStrings.Subscribe;
            });
        }

        [Resolved]
        private OverlayColourProvider overlayColourProvider { get; set; }

        public void GetPalette()
        {
            Task.Run(async () =>
            {
                var cachePath = app.Host.CacheStorage.GetStorageForDirectory("videoThumbnailCache").GetFullPath($"{videoData.Id}.png");

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(videoData.Snippet.Thumbnails.High.Url);
                    await System.IO.File.WriteAllBytesAsync(cachePath, imageBytes);
                }

                using Image<Rgba32> bitmap = SixLabors.ImageSharp.Image.Load<Rgba32>(app.Host.CacheStorage.GetStorageForDirectory("videoThumbnailCache").GetFullPath($"{videoData.Id}.png"));

                IBitmapHelper bitmapHelper = new BitmapHelper(bitmap);
                PaletteBuilder paletteBuilder = new PaletteBuilder();
                Palette palette = paletteBuilder.Generate(bitmapHelper);
                int? rgbColor = palette.MutedSwatch.Rgb;
                int? rgbTextColor = palette.MutedSwatch.TitleTextColor;

                if (rgbColor != null && rgbTextColor != null)
                {
                    Color4 bgColor = System.Drawing.Color.FromArgb((int)rgbColor);
                    Color4 textColor = System.Drawing.Color.FromArgb((int)rgbTextColor);
                    Schedule(() =>
                    {
                        bgLayer.Alpha = 1;
                        bgLayer.Colour = bgColor;
                        videoName.Colour = (textColor);
                        desc.Colour = (textColor);
                    });
                }
                else
                {
                    bgLayer.Colour = overlayColourProvider.Background4;
                    videoName.Colour = overlayColourProvider.Content2;
                    desc.Colour = overlayColourProvider.Foreground2;
                }
            });
        }

        private void updateDescText()
        {
            Schedule(() =>
            {
                DateTimeOffset? dateTime = videoData.Snippet.PublishedAtDateTimeOffset;
                DateTimeOffset now = DateTime.Now;
                Channel channelData = api.GetChannel(videoData.Snippet.ChannelId);
                desc.Text = NekoPlayerStrings.VideoMetadataDesc(api.GetLocalizedChannelTitle(channelData), Convert.ToInt32(videoData.Statistics.ViewCount).ToStandardFormattedString(0), dateTime.Value.Humanize(dateToCompareAgainst: now));
            });
        }

        public void UpdateVideo(string videoId)
        {
            uiLanguage.UnbindEvents();
            Task.Run(async () =>
            {
                videoData = api.GetVideo(videoId);
                UpdateChannelSubscribeState(videoData.Snippet.ChannelId);
                DateTimeOffset? dateTime = videoData.Snippet.PublishedAtDateTimeOffset;
                DateTimeOffset now = DateTime.Now;
                Channel channelData = api.GetChannel(videoData.Snippet.ChannelId);
                Schedule(() =>
                {
                    videoName.Text = api.GetLocalizedVideoTitle(videoData);
                    updateDescText();
                    profileImage.UpdateProfileImage(videoData.Snippet.ChannelId);
                });

                GetPalette();

                uiLanguage.BindValueChanged(locale =>
                {
                    Task.Run(async () =>
                    {
                        Schedule(() =>
                        {
                            videoName.Text = api.GetLocalizedVideoTitle(videoData);
                            updateDescText();
                        });
                    });
                });

                usernameDisplayMode.BindValueChanged(locale =>
                {
                    Task.Run(async () =>
                    {
                        Schedule(() =>
                        {
                            updateDescText();
                        });
                    });
                }, true);

                translationSource.BindValueChanged(locale =>
                {
                    Task.Run(async () =>
                    {
                        Schedule(() =>
                        {
                            videoName.Text = api.GetLocalizedVideoTitle(videoData);
                            updateDescText();
                        });
                    });
                }, true);
            });
        }
    }
}
