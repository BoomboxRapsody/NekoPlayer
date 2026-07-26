// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Drawing;
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
using SixLabors.ImageSharp.PixelFormats;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class VideoMetadataDisplayWithoutProfile : Container
    {
        private AdaptiveSpriteText videoName;
        private AdaptiveSpriteText desc;
        public Action<VideoMetadataDisplayWithoutProfile> ClickEvent;

        private Box bgLayer, hover;

        [Resolved]
        private YouTubeAPI api { get; set; }

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; }

        [Resolved]
        private NekoPlayerConfigManager appConfig { get; set; }

        private Bindable<Localisation.Language> uiLanguage;
        private Bindable<UsernameDisplayMode> usernameDisplayMode;
        private Bindable<VideoMetadataTranslateSource> translationSource = new Bindable<VideoMetadataTranslateSource>();

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            uiLanguage = app.CurrentLanguage.GetBoundCopy();
            usernameDisplayMode = appConfig.GetBindable<UsernameDisplayMode>(NekoPlayerSetting.UsernameDisplayMode);
            translationSource = appConfig.GetBindable<VideoMetadataTranslateSource>(NekoPlayerSetting.VideoMetadataTranslateSource);

            Masking = false;

            Shear = new Vector2(0f, 0);
            CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 1.5f);

            InternalChildren = new Drawable[]
            {
                samples,
                bgLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ColourInfo.GradientHorizontal(overlayColourProvider.Background5.Opacity(0.75f), overlayColourProvider.Background5.Opacity(0f)),
                    Alpha = 0f,
                },
                new Container {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding
                    {
                        Vertical = 18,
                        Right = 7,
                        Left = 14,
                    },
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding
                            {
                                Vertical = 5,
                                Horizontal = 5,
                            },
                            Children = new Drawable[]
                            {
                                videoName = new AdaptiveSpriteText
                                {
                                    Font = NekoPlayerApp.DefaultFont.With(size: 20, weight: "ExtraBold"),
                                    Text = NekoPlayerStrings.VideoNotLoaded,
                                    Colour = Color4.White,
                                },
                                desc = new AdaptiveSpriteText
                                {
                                    Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "SemiBold"),
                                    Text = NekoPlayerStrings.VideoNotLoadedDesc,
                                    Colour = Color4.Gray,
                                    Position = new osuTK.Vector2(0, 20),
                                }
                            }
                        }
                    }
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding
                    {
                        Bottom = 8,
                        Top = 8 + 12,
                        Horizontal = 8,
                    },
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Child = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 1.5f),
                        Child = hover = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                            Blending = BlendingParameters.Additive,
                            Alpha = 0,
                        },
                    },
                },
            };
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

        private Video videoData;

        protected override bool OnClick(ClickEvent e)
        {
            ClickEvent?.Invoke(this);

            return base.OnClick(e);
        }

        public void SetVideoMetadataDisplayAlignment(VideoMetadataDisplayAlignment alignment)
        {
            switch (alignment)
            {
                case VideoMetadataDisplayAlignment.Left:
                {
                    videoName.Anchor = Anchor.TopLeft;
                    videoName.Origin = Anchor.TopLeft;

                    desc.Anchor = Anchor.TopLeft;
                    desc.Origin = Anchor.TopLeft;
                    break;
                }
                case VideoMetadataDisplayAlignment.Right:
                {
                    videoName.Anchor = Anchor.TopRight;
                    videoName.Origin = Anchor.TopRight;

                    desc.Anchor = Anchor.TopRight;
                    desc.Origin = Anchor.TopRight;
                    break;
                }
            }
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
                hover.FadeTo(0f, 500, Easing.OutQuint);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            (samples as HoverClickSounds).Enabled.Value = (ClickEvent != null);
        }

        [Resolved]
        private NekoPlayerAppBase app { get; set; }

        public void GetPalette()
        {
            Task.Run(async () =>
            {
                var cachePath = app.Host.CacheStorage.GetStorageForDirectory("videoThumbnailCache").GetFullPath($"{videoData.Id}-2.png");

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(videoData.Snippet.Thumbnails.High.Url);
                    await System.IO.File.WriteAllBytesAsync(cachePath, imageBytes);
                }

                SixLabors.ImageSharp.Image<Rgba32> bitmap = SixLabors.ImageSharp.Image.Load<Rgba32>(app.Host.CacheStorage.GetStorageForDirectory("videoThumbnailCache").GetFullPath($"{videoData.Id}-2.png"));

                IBitmapHelper bitmapHelper = new BitmapHelper(bitmap);
                PaletteBuilder paletteBuilder = new PaletteBuilder();
                Palette palette = paletteBuilder.Generate(bitmapHelper);
                int? rgbColor = palette.VibrantSwatch.Rgb;
                int? rgbTextColor = palette.VibrantSwatch.TitleTextColor;

                if (rgbColor != null)
                {
                    Color4 bgColor = Color.FromArgb((int)rgbColor);
                    Schedule(() =>
                    {
                        bgLayer.Colour = ColourInfo.GradientHorizontal(bgColor.Opacity(0.75f), bgColor.Opacity(0f));
                    });
                }

                if (rgbTextColor != null)
                {
                    Color4 textColor = Color.FromArgb((int)rgbTextColor);
                    Schedule(() =>
                    {
                        videoName.Colour = (textColor);
                        desc.Colour = (textColor);
                    });
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
                desc.Text = NekoPlayerStrings.VideoMetadataDescWithLikeCount(TruncateWithEllipsis(api.GetLocalizedChannelTitle(channelData), 50), videoData.Statistics.LikeCount != null ? Convert.ToDouble(videoData.Statistics.LikeCount).ToStandardFormattedString(0) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(videoData.Id).RawLikes).ToStandardFormattedString(0), Convert.ToInt32(videoData.Statistics.ViewCount).ToStandardFormattedString(0), dateTime.Value.Humanize(dateToCompareAgainst: now));
            });
        }

        public void UpdateVideo(string videoId)
        {
            uiLanguage.UnbindEvents();
            Task.Run(async () =>
            {
                videoData = api.GetVideo(videoId);
                DateTimeOffset? dateTime = videoData.Snippet.PublishedAtDateTimeOffset;
                DateTimeOffset now = DateTimeOffset.Now;
                Channel channelData = api.GetChannel(videoData.Snippet.ChannelId);
                Schedule(() =>
                {
                    videoName.Text = api.GetLocalizedVideoTitle(videoData);
                    updateDescText();
                });

                //GetPalette();

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
