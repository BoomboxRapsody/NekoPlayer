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
    public partial class VideoMetadataDisplayWithoutProfile : CompositeDrawable
    {
        private TruncatingSpriteText videoName;
        private TruncatingSpriteText desc;
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

            Shear = new Vector2(0.2f, 0);

            InternalChildren = new Drawable[]
            {
                //samples,
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
                    Shear = new Vector2(-0.2f, 0),
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
                                videoName = new TruncatingSpriteText
                                {
                                    Font = NekoPlayerApp.DefaultFont.With(size: 20, weight: "Bold"),
                                    RelativeSizeAxes = Axes.X,
                                    Text = NekoPlayerStrings.VideoNotLoaded,
                                    Colour = Color4.White,
                                },
                                desc = new TruncatingSpriteText
                                {
                                    Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "SemiBold"),
                                    RelativeSizeAxes = Axes.X,
                                    Text = NekoPlayerStrings.VideoNotLoadedDesc,
                                    Colour = Color4.Gray,
                                    Position = new osuTK.Vector2(0, 20),
                                }
                            }
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
        }

        private Video videoData;

        protected override bool OnClick(ClickEvent e)
        {
            ClickEvent?.Invoke(this);

            return base.OnClick(e);
        }

        private HoverSounds samples = new HoverClickSounds(HoverSampleSet.Default);

        /*
        protected override bool OnHover(HoverEvent e)
        {
            if (ClickEvent != null)
                bgLayer.FadeTo(1f, 500, Easing.OutQuint);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            if (ClickEvent != null)
                bgLayer.FadeTo(0.7f, 500, Easing.OutQuint);
        }
        */

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
                desc.Text = NekoPlayerStrings.VideoMetadataDescWithLikeCount(api.GetLocalizedChannelTitle(channelData), ReturnYouTubeDislike.GetDislikes(videoData.Id).Dislikes > 0 ? Convert.ToInt32(ReturnYouTubeDislike.GetDislikes(videoData.Id).Dislikes).ToStandardFormattedString(0) : Convert.ToInt32(ReturnYouTubeDislike.GetDislikes(videoData.Id).RawDislikes).ToStandardFormattedString(0), Convert.ToInt32(videoData.Statistics.ViewCount).ToStandardFormattedString(0), dateTime.Value.Humanize(dateToCompareAgainst: now));
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
