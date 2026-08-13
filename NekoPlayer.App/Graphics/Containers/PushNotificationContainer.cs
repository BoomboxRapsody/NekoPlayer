// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NekoPlayer.App.Graphics.Sprites;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.Containers
{
    public partial class PushNotificationContainer : ProjectYomiTweakedClickableContainer
    {
        private IconUsage icon;

        private Box bg, hover;
        private ProjectYomiSpriteText titleText, descText;
        private Container content;

        public PushNotificationContainer(IconUsage icon, Color4 iconColour, LocalisableString title, LocalisableString desc)
        {
            Height = 64;
            Width = 280;

            Add(content = new Container
            {
                Masking = true,
                CornerRadius = 32,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    bg = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new GridContainer
                    {
                        Padding = new MarginPadding(8),
                        RelativeSizeAxes = Axes.Both,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new Container
                                {
                                    Masking = true,
                                    Width = 48,
                                    Height = 48,
                                    CornerRadius = 24,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = iconColour.Darken(2f),
                                        },
                                        new SpriteIcon
                                        {
                                            Width = 24,
                                            Height = 24,
                                            Icon = icon,
                                            Colour = iconColour,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                        }
                                    }
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Margin = new MarginPadding { Left = 8 },
                                    Direction = FillDirection.Vertical,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Children = new Drawable[]
                                    {
                                        titleText = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = title,
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Font = NekoPlayerApp.DefaultFont.With(size: 20, weight: "Bold"),
                                        },
                                        descText = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Text = desc,
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Font = NekoPlayerApp.DefaultFont.With(size: 16, weight: "Bold"),
                                        }
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
                }
            });
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            bg.Colour = overlayColourProvider.Background4;
            titleText.Colour = overlayColourProvider.Content2;
            descText.Colour = overlayColourProvider.Foreground1;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            content.ScaleTo(0.9f, 2000, Easing.OutQuint);
            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            content.ScaleTo(1, 250, Easing.OutQuint);
            base.OnMouseUp(e);
        }

        protected override bool OnHover(HoverEvent e)
        {
            hover.FadeTo(0.1f, 500, Easing.OutQuint);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            hover.FadeOut(500, Easing.OutQuint);
        }
    }
}
