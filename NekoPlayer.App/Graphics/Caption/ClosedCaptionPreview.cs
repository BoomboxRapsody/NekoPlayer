// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NekoPlayer.App.Config;
using NekoPlayer.App.Graphics.UserInterface;
using NekoPlayer.App.Localisation;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.Caption
{
    public partial class ClosedCaptionPreview : Container
    {
        private AdaptiveTextFlowContainer spriteText;
        private Bindable<CaptionFonts> captionFont;
        private Bindable<float> captionBGOpacity;
        private Container captionContainer;
        private Box bg;
        private Action<SpriteText> textCreationParameters;

        [BackgroundDependencyLoader]
        private void load(NekoPlayerConfigManager config, SessionStatics sessionStatics, TextureStore textureStore)
        {
            captionFont = config.GetBindable<CaptionFonts>(NekoPlayerSetting.CaptionFont);
            captionBGOpacity = config.GetBindable<float>(NekoPlayerSetting.CaptionBGOpacity);

            captionContainer = new Container
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeDuration = 350,
                AutoSizeEasing = Easing.OutQuart,
                Masking = true,
                Children = new Drawable[]
                {
                    bg = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.5f
                    },
                    spriteText = new AdaptiveTextFlowContainer(t =>
                    {
                        t.Font = NekoPlayerApp.GoogleSansFlex.With(size: 24);
                        t.Shadow = false;
                    })
                    {
                        TextAnchor = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Margin = new MarginPadding(4),
                    }
                }
            };

            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS,
                Children = new Drawable[]
                {
                    new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        FillMode = FillMode.Fill,
                        Texture = textureStore.Get("ClosedCaptionPreviewBG"),
                    },
                    captionContainer,
                }
            });

            captionBGOpacity.BindValueChanged(opacity =>
            {
                bg.Alpha = opacity.NewValue;
            }, true);

            captionFont.BindValueChanged(v =>
            {
                switch (v.NewValue)
                {
                    case CaptionFonts.GoogleSansFlex:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.GoogleSansFlex.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Rubik:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.Rubik.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Pretendard:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.Pretendard.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Hungeul:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.Hungeul.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Ownglyph_PDH:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.Ownglyph_PDH.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Dovemayo_Gothic:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.Dovemayo_Gothic.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Griun_Mongtori:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.Griun_Mongtori.With(size: 24);
                        break;
                    }
                    case CaptionFonts.ONE_Mobile_POP:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.ONE_Mobile_POP.With(size: 24);
                        break;
                    }
                    case CaptionFonts.Cafe24Syongsyong:
                    {
                        textCreationParameters = spriteText => spriteText.Font = NekoPlayerApp.Cafe24Syongsyong.With(size: 24);
                        break;
                    }
                }
                RefreshFont();
            }, true);
        }

        private void RefreshFont()
        {
            spriteText.Text = "";
            spriteText.AddText(NekoPlayerStrings.ClosedCaptionPreview, textCreationParameters);
        }
    }
}
