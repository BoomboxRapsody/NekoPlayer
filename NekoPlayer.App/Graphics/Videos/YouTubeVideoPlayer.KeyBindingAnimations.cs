// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NekoPlayer.App.Graphics.Sprites;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Threading;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.Videos
{
    public partial class YouTubeVideoPlayer
    {
        public partial class KeyBindingAnimations : Container
        {
            private SeekAnimation leftContent, centerContent, rightContent;

            [BackgroundDependencyLoader]
            private void load()
            {
                AddRange(new Drawable[] {
                    leftContent = new SeekAnimation(SeekAction.FastRewind10sec)
                    {
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.TopLeft,
                        RelativeSizeAxes = Axes.Y,
                        Width = 200,
                    },
                    centerContent = new SeekAnimation(SeekAction.PlayPause)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        RelativeSizeAxes = Axes.Both,
                    },
                    rightContent = new SeekAnimation(SeekAction.FastForward10sec)
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        RelativeSizeAxes = Axes.Y,
                        Width = 200,
                    }
                });
            }

            public void PlaySeekAnimation(SeekAction seekAction, IconUsage icon)
            {
                switch (seekAction)
                {
                    case SeekAction.FastRewind10sec:
                    {
                        rightContent.RepeatCount = 0;
                        rightContent.HideNow();
                        leftContent.PlaySeekAnimation(FontAwesome.Solid.ChevronLeft);
                        break;
                    }
                    case SeekAction.FastForward10sec:
                    {
                        leftContent.RepeatCount = 0;
                        leftContent.HideNow();
                        rightContent.PlaySeekAnimation(FontAwesome.Solid.ChevronRight);
                        break;
                    }
                    case SeekAction.PlayPause:
                    {
                        centerContent.PlaySeekAnimation(icon);
                        break;
                    }
                }
            }

            private partial class SeekAnimation : Container
            {
                private SeekAction trackAction;
                private SpriteIcon seekArrow;
                private AdaptiveSpriteText seekValue;

                private Container content;
                public int RepeatCount = 0;

                private ScheduledDelegate fadeIn;
                private ScheduledDelegate fadeOut;

                public SeekAnimation(SeekAction trackAction)
                {
                    this.trackAction = trackAction;
                    if (trackAction == SeekAction.FastRewind10sec)
                    {
                        Add(content = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = ColourInfo.GradientHorizontal(Color4.Black.Opacity(0.5f), Color4.Black.Opacity(0f)),
                                },
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Children = new Drawable[]
                                    {
                                        seekArrow = new SpriteIcon
                                        {
                                            Width = 25,
                                            Height = 25,
                                            Scale = new osuTK.Vector2(0.8f, 1),
                                            Position = new osuTK.Vector2(20, 0),
                                            Icon = FontAwesome.Solid.ChevronLeft,
                                        },
                                        seekValue = new AdaptiveSpriteText
                                        {
                                            Text = $"- 5",
                                            Margin = new MarginPadding
                                            {
                                                Left = 30,
                                            },
                                            Font = NekoPlayerApp.DefaultFont.With(size: 25),
                                        },
                                    }
                                }
                            }
                        });
                    }
                    else if (trackAction == SeekAction.FastForward10sec)
                    {
                        Add(content = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = ColourInfo.GradientHorizontal(Color4.Black.Opacity(0f), Color4.Black.Opacity(0.5f)),
                                },
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Children = new Drawable[]
                                    {
                                        seekArrow = new SpriteIcon
                                        {
                                            Width = 25,
                                            Height = 25,
                                            Scale = new osuTK.Vector2(0.8f, 1),
                                            Position = new osuTK.Vector2(20, 0),
                                            Icon = FontAwesome.Solid.ChevronRight,
                                        },
                                        seekValue = new AdaptiveSpriteText
                                        {
                                            Text = $"+ 5",
                                            Margin = new MarginPadding
                                            {
                                                Right = 45,
                                            },
                                            Font = NekoPlayerApp.DefaultFont.With(family: "Torus", size: 25),
                                        },
                                    }
                                }
                            }
                        });
                    }
                    else
                    {
                        Add(content = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Children = new Drawable[]
                            {
                                new CircularContainer
                                {
                                    Width = 100,
                                    Height = 100,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Masking = true,
                                    Child = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Alpha = 0.7f,
                                        Colour = Color4.Black,
                                    }
                                },
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Children = new Drawable[]
                                    {
                                        seekArrow = new SpriteIcon
                                        {
                                            Width = 25,
                                            Height = 25,
                                            Icon = FontAwesome.Solid.ChevronRight,
                                        },
                                    }
                                }
                            }
                        });
                    }
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    content.FadeOut();
                }

                public void HideNow()
                {
                    if (trackAction != SeekAction.PlayPause)
                    {
                        content.FadeOut(250, Easing.In);
                        using (BeginDelayedSequence(250))
                        {
                            seekArrow.ScaleTo(new osuTK.Vector2(0.8f, 1));
                            //count = 0;
                            if (trackAction == SeekAction.FastRewind10sec)
                            {
                                seekArrow.MoveTo(new osuTK.Vector2(20, 0));
                            }
                            else
                            {
                                seekArrow.MoveTo(new osuTK.Vector2(40, 0));
                            }
                        }
                    }
                }

                public void PlaySeekAnimation(IconUsage icon)
                {
                    seekArrow.Icon = icon;
                    if (trackAction != SeekAction.PlayPause)
                    {
                        content.FadeInFromZero(250, Easing.Out);
                        seekArrow.ScaleTo(new osuTK.Vector2(0.7f, 1));
                        if (trackAction == SeekAction.FastRewind10sec)
                        {
                            seekArrow.MoveTo(new osuTK.Vector2(20, 0));
                        }
                        else
                        {
                            seekArrow.MoveTo(new osuTK.Vector2(20, 0));
                        }
                        seekArrow.ScaleTo(1, 250, Easing.Out);
                        if (trackAction == SeekAction.FastRewind10sec)
                        {
                            seekArrow.MoveTo(new osuTK.Vector2(0), 500, Easing.OutQuart);
                        }
                        else
                        {
                            seekArrow.MoveTo(new osuTK.Vector2(40, 0), 500, Easing.OutQuart);
                        }
                        using (BeginDelayedSequence(1250))
                        {
                            HideNow();
                        }
                    }
                    else
                    {
                        content.FadeInFromZero(500, Easing.OutQuint);
                        content.ScaleTo(new osuTK.Vector2(.9f)).ScaleTo(new osuTK.Vector2(1.5f), 250, Easing.OutQuint);
                        using (BeginDelayedSequence(750))
                        {
                            content.FadeOut(500, Easing.OutQuint);
                            content.ScaleTo(new osuTK.Vector2(.9f), 250, Easing.OutQuint);
                        }
                    }
                }
            }

            public enum SeekAction
            {
                FastForward10sec,
                FastRewind10sec,
                PlayPause,
            }
        }
    }
}
