// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;
using YoutubeExplode.Videos.ClosedCaptions;
using NekoPlayer.App.Config;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Graphics.Videos;

namespace NekoPlayer.App.Graphics.Caption
{
    public partial class ClosedCaptionContainer : Container
    {
        public Bindable<bool> UIVisiblity = new Bindable<bool>();

        private AdaptiveSpriteText spriteText;
        private YouTubeVideoPlayer videoPlayer;
        private ClosedCaptionTrack captionTrack;
        private Bindable<bool> captionEnabled;
        private Bindable<CaptionFonts> captionFont;
        private Container captionContainer;

        private Bindable<float> bottomMargin = new Bindable<float>();

        public ClosedCaptionContainer(YouTubeVideoPlayer videoPlayer, ClosedCaptionTrack captionTrack)
        {
            this.videoPlayer = videoPlayer;
            this.captionTrack = captionTrack;
            Padding = new MarginPadding(32);
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            AlwaysPresent = true;
        }

        public void UpdateCaptionTrack(ClosedCaptionLanguage captionLanguage, ClosedCaptionTrack captionTrack)
        {
            if (captionTrack != null)
                this.captionTrack = captionTrack;
            else
                this.captionTrack = null;
        }

        private Bindable<bool> controlsVisibleState = null!;

        [BackgroundDependencyLoader]
        private void load(NekoPlayerConfigManager config, SessionStatics sessionStatics)
        {
            controlsVisibleState = sessionStatics.GetBindable<bool>(Static.IsControlVisible);
            captionEnabled = config.GetBindable<bool>(NekoPlayerSetting.CaptionEnabled);
            captionFont = config.GetBindable<CaptionFonts>(NekoPlayerSetting.CaptionFont);

            Add(captionContainer = new Container
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                AutoSizeDuration = 350,
                AutoSizeEasing = Easing.OutQuart,
                Masking = true,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.5f
                    },
                    spriteText = new AdaptiveSpriteText(false)
                    {
                        Font = NekoPlayerApp.DefaultFont.With(size: 24),
                        Margin = new MarginPadding(4),
                    }
                }
            });

            captionFont.BindValueChanged(v =>
            {
                switch (v.NewValue)
                {
                    case CaptionFonts.GoogleSansFlex:
                    {
                        spriteText.Font = NekoPlayerApp.DefaultFont.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Rubik:
                    {
                        spriteText.Font = NekoPlayerApp.Rubik.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Pretendard:
                    {
                        spriteText.Font = NekoPlayerApp.Pretendard.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Hungeul:
                    {
                        spriteText.Font = NekoPlayerApp.Hungeul.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Ownglyph_PDH:
                    {
                        spriteText.Font = NekoPlayerApp.Ownglyph_PDH.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Dovemayo_Gothic:
                    {
                        spriteText.Font = NekoPlayerApp.Dovemayo_Gothic.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Griun_Mongtori:
                    {
                        spriteText.Font = NekoPlayerApp.Griun_Mongtori.With(size: 24);
                        break;
                    }
                    case CaptionFonts.ONE_Mobile_POP:
                    {
                        spriteText.Font = NekoPlayerApp.ONE_Mobile_POP.With(size: 24);
                        break;
                    }
                    case CaptionFonts.HayuFont:
                    {
                        spriteText.Font = NekoPlayerApp.HayuFont.With(size: 24);
                        break;
                    }
                }
            }, true);

            controlsVisibleState.BindValueChanged(v =>
            {
                UpdateControlsVisibleState(v.NewValue);
            }, true);

            bottomMargin.BindValueChanged(v =>
            {
                captionContainer.Margin = new MarginPadding
                {
                    Bottom = v.NewValue
                };
            }, true);
        }

        public void UpdateControlsVisibleState(bool state)
        {
            /*
            captionContainer.Margin = new MarginPadding
            {
                Bottom = state ? 90 : 0
            };
            */

            this.TransformBindableTo(bottomMargin, state ? 55 : 0, 500, Easing.OutQuint);
        }

        protected override void Update()
        {
            base.Update();

            if (captionTrack == null)
                Hide();
            else
                Show();

            if (captionTrack != null)
            {
                try
                {
                    var caption = captionTrack.TryGetByTime(TimeSpan.FromSeconds(videoPlayer.VideoProgress.Value));
                    if (caption != null)
                    {
                        var text = caption.Text; // "collection acts as the parent collection"
                        spriteText.Text = text;
                        captionContainer.FadeIn(150, Easing.OutQuart);
                    }
                    else
                    {
                        captionContainer.FadeOut(150, Easing.OutQuart);
                    }
                }
                catch
                {
                    captionContainer.FadeOut(150, Easing.OutQuart);
                }
            }
        }
    }
}
