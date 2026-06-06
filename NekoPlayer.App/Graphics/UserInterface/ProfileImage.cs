// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Humanizer;
using NekoPlayer.App.Config;
using NekoPlayer.App.Localisation;
using NekoPlayer.App.Online;
using NekoPlayer.App.Utils;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK.Graphics;
using PaletteNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class ProfileImage : CompositeDrawable, IHasCustomTooltip<Channel>
    {
        private Sprite profileImage;

        private Google.Apis.YouTube.v3.Data.Channel channel;

        private Container profileImageBase;

        private LoadingSpinner loading;
        private Box hover, bgLayer;

        [Resolved]
        private TextureStore textureStore { get; set; }

        [Resolved]
        private YouTubeAPI api { get; set; }

        public virtual LocalisableString TooltipText { get; protected set; }

        [Resolved]
        private NekoPlayerAppBase app { get; set; }

        private Bindable<VideoMetadataTranslateSource> translationSource = new Bindable<VideoMetadataTranslateSource>();
        private Bindable<ProfileImageShape> profileImageShape;

        public ProfileImage(float size = 30)
        {
            Width = Height = size;
            //CornerRadius = size / 2;
            Masking = true;
            InternalChildren = new Drawable[]
            {
                samples,
                bgLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                },
                profileImageBase = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    Child = profileImage = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                },
                hover = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Blending = BlendingParameters.Additive,
                    Alpha = 0,
                },
                loading = new LoadingLayer(true, false, false)
            };
        }

        private Bindable<Localisation.Language> uiLanguage;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            uiLanguage = app.CurrentLanguage.GetBoundCopy();
            BorderColour = overlayColourProvider.Light4;
            BorderThickness = 0;

            profileImageShape = appConfig.GetBindable<ProfileImageShape>(NekoPlayerSetting.ProfileImageShape);
            translationSource = appConfig.GetBindable<VideoMetadataTranslateSource>(NekoPlayerSetting.VideoMetadataTranslateSource);

            profileImageShape.BindValueChanged(shape =>
            {
                switch (shape.NewValue)
                {
                    case ProfileImageShape.Circle:
                        this.TransformTo(nameof(CornerRadius), new CornersInfo(Height / 2), 500, Easing.OutQuint);
                        profileImageBase.TransformTo(nameof(CornerRadius), new CornersInfo(Height / 2), 500, Easing.OutQuint);
                        break;

                    case ProfileImageShape.Square:
                        this.TransformTo(nameof(CornerRadius), new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 2), 500, Easing.OutQuint);
                        profileImageBase.TransformTo(nameof(CornerRadius), new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 2), 500, Easing.OutQuint);
                        break;
                }
            }, true);
        }

        public void GetPalette()
        {
            Task.Run(async () =>
            {
                var cachePath = app.Host.CacheStorage.GetStorageForDirectory("profile_cache").GetFullPath($"{channel.Id}.png");

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(channel.Snippet.Thumbnails.High.Url);
                    await System.IO.File.WriteAllBytesAsync(cachePath, imageBytes);
                }

                using Image<Rgba32> bitmap = SixLabors.ImageSharp.Image.Load<Rgba32>(app.Host.CacheStorage.GetStorageForDirectory("profile_cache").GetFullPath($"{channel.Id}.png"));

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
                        BorderColour = bgColor;
                    });
                }
            });
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (Enabled.Value)
                profileImageBase.ScaleTo(0.8f, 2000, Easing.OutQuint);

            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (Enabled.Value)
                profileImageBase.ScaleTo(1f, 350, Easing.OutQuint);
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (Enabled.Value)
            {
                hover.FadeTo(0.1f, 500, Easing.OutQuint);
                this.TransformTo(nameof(BorderThickness), 2f, 250, Easing.OutQuint);
            }
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            if (Enabled.Value)
            {
                this.TransformTo(nameof(BorderThickness), 0f, 250, Easing.OutQuint);
                hover.FadeOut(500, Easing.OutQuint);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            profileImage.Dispose();
        }

        private HoverSounds samples = new HoverClickSounds(HoverSampleSet.Default);

        [Resolved]
        private NekoPlayerConfigManager appConfig { get; set; }

        public Bindable<bool> Enabled { get; set; } = new BindableBool(true);

        protected override bool OnClick(ClickEvent e)
        {
            if (channel != null && Enabled.Value)
                app.Host.OpenUrlExternally($"https://www.youtube.com/channel/{channel.Id}");

            return base.OnClick(e);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            (samples as HoverClickSounds).Enabled.Value = Enabled.Value;
        }

        public void UpdateProfileImage(string channelId)
        {
            translationSource.UnbindEvents();
            uiLanguage.UnbindEvents();
            Task.Run(async () =>
            {
                channel = api.GetChannel(channelId);
                _ = Task.Run(async () =>
                {
                    await GetProfileImage(channel.Snippet.Thumbnails.High.Url);
                });
                Schedule(() =>
                {
                    //TooltipText = NekoPlayerStrings.ProfileImageTooltip(api.GetLocalizedChannelTitle(channel, true), Convert.ToInt32(channel.Statistics.SubscriberCount).ToMetric(decimals: 2));
                });

                translationSource.BindValueChanged(locale =>
                {
                    Task.Run(async () =>
                    {
                        Schedule(() =>
                        {
                            //TooltipText = NekoPlayerStrings.ProfileImageTooltip(api.GetLocalizedChannelTitle(channel, true), Convert.ToInt32(channel.Statistics.SubscriberCount).ToMetric(decimals: 2));
                        });
                    });
                }, true);

                uiLanguage.BindValueChanged(locale =>
                {
                    Task.Run(async () =>
                    {
                        Schedule(() =>
                        {
                            //TooltipText = NekoPlayerStrings.ProfileImageTooltip(api.GetLocalizedChannelTitle(channel, true), Convert.ToInt32(channel.Statistics.SubscriberCount).ToMetric(decimals: 2));
                        });
                    });
                }, true);
            });
        }

        ITooltip<Channel> IHasCustomTooltip<Channel>.GetCustomTooltip() => new ProfileImageTooltip();

        Channel IHasCustomTooltip<Channel>.TooltipContent => channel;

        public async Task GetProfileImage(string url, CancellationToken cancellationToken = default)
        {
            Schedule(() => loading.Show());
            Texture north = await textureStore.GetAsync(channel.Snippet.Thumbnails.High.Url, cancellationToken);
            //GetPalette();
            Schedule(() => { profileImage.Texture = north; });
            Schedule(() => loading.Hide());
        }
    }
}
