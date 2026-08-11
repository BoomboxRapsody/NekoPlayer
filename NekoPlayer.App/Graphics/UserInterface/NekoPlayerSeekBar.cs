// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using NekoPlayer.App.Utils;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK.Graphics;
using PaletteNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vector2 = osuTK.Vector2;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class NekoPlayerSeekBar<T> : ProjectYomiSliderBar<T>
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        protected readonly NekoPlayerSeekBar.SliderNub Nub;
        protected readonly Path LeftBox;
        protected readonly Box RightBox;
        protected readonly Container LeftBoxContainer;
        protected readonly Container RightBoxContainer;
        protected readonly Circle EndCircle;
        private readonly Container nubContainer;

        private readonly Container mainContent;

        private Color4 accentColour;

        public Color4 AccentColour
        {
            get => accentColour;
            set
            {
                accentColour = value;
                LeftBox.Colour = value;
                Nub.Colour = value;
            }
        }

        private Colour4 backgroundColour;

        public Color4 BackgroundColour
        {
            get => backgroundColour;
            set
            {
                backgroundColour = value;
                RightBox.Colour = value;
            }
        }

        public Bindable<double> PlaybackSpeed = new Bindable<double>(1);

        /// <summary>
        /// The action to use to reset the value of <see cref="SliderBar{T}.Current"/> to the default.
        /// Triggered on double click.
        /// </summary>
        public Action ResetToDefault { get; internal set; }

        public NekoPlayerSeekBar()
        {
            Height = NekoPlayerSeekBar.SliderNub.HEIGHT;
            RangePadding = NekoPlayerSeekBar.SliderNub.DEFAULT_EXPANDED_SIZE / 2;
            ResetToDefault = () =>
            {
                if (!Current.Disabled)
                    Current.SetDefault();
            };
            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Padding = new MarginPadding { Horizontal = 2 },
                    Child = mainContent = new CircularContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Children = new Drawable[]
                        {
                            LeftBoxContainer = new Container
                            {
                                Height = NekoPlayerSeekBar.SliderNub.HEIGHT / 3f,
                                //AutoSizeAxes = Axes.X,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Masking = false,
                                //CornerRadius = new CornersInfo((NekoPlayerSeekBar.SliderNub.HEIGHT / 3f) / 2, (NekoPlayerSeekBar.SliderNub.HEIGHT / 3f) / 2, (NekoPlayerSeekBar.SliderNub.HEIGHT / 3f) / 3, (NekoPlayerSeekBar.SliderNub.HEIGHT / 3f) / 3),
                                Children = new Drawable[] {
                                    LeftBox = new SmoothPath
                                    {
                                        AlwaysPresent = true,
                                        PathRadius = 4.5f,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                    },
                                },
                            },
                            RightBoxContainer = new Container
                            {
                                Height = NekoPlayerSeekBar.SliderNub.HEIGHT / 3f,
                                AutoSizeAxes = Axes.X,
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Masking = true,
                                CornerRadius = new CornersInfo((NekoPlayerSeekBar.SliderNub.HEIGHT / 3f) / 3, (NekoPlayerSeekBar.SliderNub.HEIGHT / 3f) / 3, (NekoPlayerSeekBar.SliderNub.HEIGHT / 3f) / 2, (NekoPlayerSeekBar.SliderNub.HEIGHT / 3f) / 2),
                                Children = new Drawable[] {
                                    RightBox = new Box
                                    {
                                        Height = NekoPlayerSeekBar.SliderNub.HEIGHT / 3f,
                                        Colour = backgroundColour,
                                        RelativeSizeAxes = Axes.None,
                                    },
                                },
                             },
                        },
                    },
                },
                nubContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = Nub = new SliderNub
                    {
                        Origin = Anchor.TopCentre,
                        Colour = AccentColour,
                        RelativePositionAxes = Axes.X,
                        Current = { Value = true },
                        OnDoubleClicked = () => ResetToDefault.Invoke(),
                    },
                },
            };
        }

        [BackgroundDependencyLoader(true)]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            AccentColour = Nub.Colour = overlayColourProvider.Content2;
            BackgroundColour = overlayColourProvider.Content2.Darken(1);

            PlaybackSpeed.BindValueChanged(speed =>
            {
                this.TransformBindableTo(speedRolling, speed.NewValue, 2400, Easing.OutQuint);
            }, true);

            IsPlaying.BindValueChanged(what =>
            {
                this.TransformBindableTo(amplitudeAnimated, what.NewValue ? 3.5f : 0f, 400, Easing.OutQuint);
            }, true);
        }

        [Resolved]
        private NekoPlayerAppBase app { get; set; }

        [Resolved]
        private OverlayColourProvider overlayColourProvider { get; set; }

        public void GetPalette(Video video)
        {
            Task.Run(async () =>
            {
                var cachePath = app.Host.CacheStorage.GetStorageForDirectory("videoThumbnailCache").GetFullPath($"{video.Id}.png");

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var imageBytes = await httpClient.GetByteArrayAsync(video.Snippet.Thumbnails.High.Url);
                    await System.IO.File.WriteAllBytesAsync(cachePath, imageBytes);
                }

                using Image<Rgba32> bitmap = SixLabors.ImageSharp.Image.Load<Rgba32>(app.Host.CacheStorage.GetStorageForDirectory("videoThumbnailCache").GetFullPath($"{video.Id}.png"));

                IBitmapHelper bitmapHelper = new BitmapHelper(bitmap);
                PaletteBuilder paletteBuilder = new PaletteBuilder();
                Palette palette = paletteBuilder.Generate(bitmapHelper);
                int? rgbColor = palette.LightMutedSwatch.Rgb;
                int? rgbColor2 = palette.DarkMutedSwatch.Rgb;

                if (rgbColor != null && rgbColor2 != null)
                {
                    Color4 accentColor = System.Drawing.Color.FromArgb((int)rgbColor);
                    Color4 bgColor = System.Drawing.Color.FromArgb((int)rgbColor2);
                    Schedule(() =>
                    {
                        AccentColour = accentColor;
                        BackgroundColour = bgColor;
                    });
                }
                else
                {
                    AccentColour = Nub.Colour = overlayColourProvider.Content2;
                    BackgroundColour = overlayColourProvider.Content2.Darken(1);
                }
            });
        }

        protected override void Update()
        {
            base.Update();

            nubContainer.Padding = new MarginPadding { Horizontal = RangePadding };

            if (Current != null && Current is BindableNumber<double>)
            {
                if (((Current as BindableNumber<double>).Value >= ((Current as BindableNumber<double>).MaxValue * 0.04)) && ((Current as BindableNumber<double>).Value <= ((Current as BindableNumber<double>).MaxValue * 0.96))) // peak
                {
                    if (!isWavy)
                    {
                        isWavy = true;
                        this.TransformBindableTo(amplitudeAnimated2, 1f, 1250, Easing.OutQuint);
                    }
                }
                else
                {
                    if (isWavy)
                    {
                        isWavy = false;
                        this.TransformBindableTo(amplitudeAnimated2, 0f, 750, Easing.OutQuint);
                    }
                }
            }

            updateWavePath();
        }

        private bool isWavy = false;

        private Bindable<double> speedRolling = new Bindable<double>(1);
        private Bindable<float> amplitudeAnimated = new Bindable<float>(0);
        private Bindable<float> amplitudeAnimated2 = new Bindable<float>(0);
        public Bindable<bool> IsPlaying = new Bindable<bool>(false);

        private void updateWavePath()
        {
            var points = new List<Vector2>();

            float frequency = 0.1f;
            float amplitude = amplitudeAnimated.Value * amplitudeAnimated2.Value;
            float step = 1f;

            for (float x = 0; x <= LeftBoxContainer.Width - 8f; x += step)
            {
                float y = MathF.Sin((x - ((float)(((Time.Current * -1f) * 0.05f) * speedRolling.Value))) * frequency) * amplitude;
                points.Add(new Vector2(x, y));
            }

            LeftBox.Vertices = points;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Current.BindDisabledChanged(disabled =>
            {
                Alpha = disabled ? 0.3f : 1;
            }, true);
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateGlow();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateGlow();
            base.OnHoverLost(e);
        }

        protected override bool ShouldHandleAsRelativeDrag(MouseDownEvent e)
            => Nub.ReceivePositionalInputAt(e.ScreenSpaceMouseDownPosition);

        protected override void OnDragEnd(DragEndEvent e)
        {
            updateGlow();
            base.OnDragEnd(e);
        }

        private void updateGlow()
        {
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // [중요] 기존 LeftBox.Scale 방식을 버리고 마스킹 컨테이너의 Width를 직접 조절합니다.
            LeftBoxContainer.Width = Math.Clamp(RangePadding + (Nub.DrawPosition.X - 8), 0, Math.Max(0, DrawWidth));

            RightBox.Scale = new Vector2(Math.Clamp(DrawWidth - (Nub.DrawPosition.X + 8) - RangePadding, 0, Math.Max(0, DrawWidth)), 1);
        }

        protected override void UpdateValue(float value)
        {
            Nub.MoveToX(value, 250, Easing.OutQuint);
        }

        public partial class SliderNub : NekoPlayerSeekBar.SliderNub
        {
            public Action? OnDoubleClicked { get; init; }

            protected override bool OnClick(ClickEvent e) => true;

            protected override bool OnDoubleClick(DoubleClickEvent e)
            {
                OnDoubleClicked?.Invoke();
                return true;
            }
        }
    }
}
