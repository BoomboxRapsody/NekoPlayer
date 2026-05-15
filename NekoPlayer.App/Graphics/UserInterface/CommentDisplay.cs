// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Humanizer;
using NekoPlayer.App.Config;
using NekoPlayer.App.Extensions;
using NekoPlayer.App.Graphics.Containers;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Localisation;
using NekoPlayer.App.Online;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osuTK;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class CommentDisplay : CompositeDrawable
    {
        private ProfileImage profileImage = null!;
        private AdaptiveTextFlowContainer channelName = null!;
        private LinkFlowContainer commentText = null!;
        public Action<VideoMetadataDisplay> ClickEvent = null!;
        private AdaptiveSpriteText likeCount = null!, replyCount = null!, translateToText = null!;

        public Action<double> TimestampClicked;

        private LoadingLayer loading;

        [Resolved]
        private YouTubeAPI api { get; set; } = null!;

        [Resolved]
        private NekoPlayerAppBase app { get; set; } = null!;

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; } = null!;

        [Resolved]
        private NekoPlayerConfigManager appConfig { get; set; } = null!;

        private Bindable<Localisation.Language> uiLanguage = null!;
        private Bindable<UsernameDisplayMode> usernameDisplayMode = null!;

        public CommentDisplay(CommentThread comment)
        {
            commentData = comment;
            AutoSizeAxes = Axes.Y;
            Task.Run(async () =>
            {
                UpdateData();
            });
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            uiLanguage = app.CurrentLanguage.GetBoundCopy();
            usernameDisplayMode = appConfig.GetBindable<UsernameDisplayMode>(NekoPlayerSetting.UsernameDisplayMode);

            CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS;
            Masking = true;
            InternalChildren = new Drawable[]
            {
                samples,
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = overlayColourProvider.Background4,
                    Alpha = 0.7f,
                },
                new Container {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding(7),
                    Children = new Drawable[]
                    {
                        profileImage = new ProfileImage(35),
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Padding = new MarginPadding
                            {
                                Top = 5,
                                Left = 42,
                                Right = 5,
                            },
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 2),
                            Children = new Drawable[]
                            {
                                channelName = new AdaptiveTextFlowContainer(f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "SemiBold"))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Colour = overlayColourProvider.Background1,
                                },
                                commentText = new LinkFlowContainer(font => font.Font = NekoPlayerApp.DefaultFont.With(size: 17, weight: "Regular"))
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Colour = overlayColourProvider.Content2,
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(4, 0),
                                    Children = new Drawable[]
                                    {
                                        new Container
                                        {
                                            AutoSizeAxes = Axes.X,
                                            Height = 27,
                                            CornerRadius = 12,
                                            Masking = true,
                                            AlwaysPresent = true,
                                            Children = new Drawable[]
                                            {
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    CornerRadius = 12,
                                                    Child = new Box
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Colour = overlayColourProvider.Background3,
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
                                                            Width = 12,
                                                            Height = 12,
                                                            Icon = FontAwesome.Solid.ThumbsUp,
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        likeCount = new AdaptiveSpriteText
                                                        {
                                                            Colour = overlayColourProvider.Content2,
                                                            Font = NekoPlayerApp.DefaultFont.With(size: 13.5f, weight: "Regular"),
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                        new Container
                                        {
                                            AutoSizeAxes = Axes.X,
                                            Height = 27,
                                            CornerRadius = 12,
                                            Masking = true,
                                            AlwaysPresent = true,
                                            Children = new Drawable[]
                                            {
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    CornerRadius = 12,
                                                    Child = new Box
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Colour = overlayColourProvider.Background3,
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
                                                        translateToText = new AdaptiveSpriteText
                                                        {
                                                            Colour = overlayColourProvider.Content2,
                                                            Font = NekoPlayerApp.DefaultFont.With(size: 13.5f, weight: "Regular"),
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                        new Container
                                        {
                                            AutoSizeAxes = Axes.X,
                                            Height = 27,
                                            CornerRadius = 12,
                                            Masking = true,
                                            AlwaysPresent = true,
                                            Children = new Drawable[]
                                            {
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    CornerRadius = 12,
                                                    Child = new Box
                                                    {
                                                        RelativeSizeAxes = Axes.Both,
                                                        Colour = overlayColourProvider.Background3,
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
                                                            Width = 12,
                                                            Height = 12,
                                                            Icon = FontAwesome.Solid.CommentAlt,
                                                            Colour = overlayColourProvider.Content2,
                                                        },
                                                        replyCount = new AdaptiveSpriteText
                                                        {
                                                            Colour = overlayColourProvider.Content2,
                                                            Font = NekoPlayerApp.DefaultFont.With(size: 13.5f, weight: "Regular"),
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                            }
                        }
                    }
                },
                loading = new LoadingLayer(true, false, true),
            };
        }

        private bool translated;

        private CommentThread commentData;

        private HoverSounds samples = new HoverClickSounds(HoverSampleSet.Default);

        private void translateComment()
        {
            if (translated == false)
            {
                Task.Run(async () => {
                    translateToText.Text = NekoPlayerStrings.Translating;
                    commentText.Text = await translate.TranslateAsync(commentData.Snippet.TopLevelComment.Snippet.TextOriginal, GoogleTranslateLanguage.auto);
                    translateToText.Text = NekoPlayerStrings.TranslateViewOriginal;
                });
                translated = true;
            }
            else
            {
                commentText.Text = commentData.Snippet.TopLevelComment.Snippet.TextOriginal;
                translateToText.Text = NekoPlayerStrings.TranslateTo(app.CurrentLanguage.Value.GetLocalisableDescription());
                translated = false;
            }
        }

        [Resolved]
        private GoogleTranslate translate { get; set; } = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            if (samples is HoverClickSounds hoverClickSounds)
                hoverClickSounds.Enabled.Value = (ClickEvent != null);
        }

        private void setText(string text)
        {
            List<YouTubeDescriptionTextToken> list = NekoPlayerDescriptionParser.Parse(text);

            foreach (YouTubeDescriptionTextToken item in list)
            {
                switch (item.Type)
                {
                    case YouTubeDescriptionTokenType.Text:
                        commentText.AddText(item.Value);
                        break;
                    case YouTubeDescriptionTokenType.Url:
                        commentText.AddArbitraryDrawable(new UrlRedirectDisplay(item.Value));
                        break;
                    case YouTubeDescriptionTokenType.Mention:
                        commentText.AddLink(item.Value, $"https://www.youtube.com/{item.Value}");
                        break;
                    case YouTubeDescriptionTokenType.Hashtag:
                        commentText.AddLink(item.Value, $"https://www.youtube.com/hashtag/{item.Value.Replace("#", string.Empty)}");
                        break;
                    case YouTubeDescriptionTokenType.Timestamp:
                        commentText.AddArbitraryDrawable(new TimestampButton(item.Value)
                        {
                            TimestampClicked = TimestampClicked,
                        });
                        break;
                }
            }
        }

        public void UpdateData()
        {
            Task.Run(async () =>
            {
                try
                {
                    Schedule(() => loading.Show());
                    DateTimeOffset? dateTime = commentData.Snippet.TopLevelComment.Snippet.PublishedAtDateTimeOffset;
                    DateTimeOffset now = DateTime.Now;

                    try
                    {
                        Channel? channelData = api.TryGetChannel(commentData.Snippet.TopLevelComment.Snippet.AuthorChannelId.Value);

                        Schedule(() =>
                        {
                            channelName.Text = channelData != null ? api.GetLocalizedChannelTitle(channelData) : commentData.Snippet.TopLevelComment.Snippet.AuthorDisplayName;
                            channelName.AddText(" • ", f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
#pragma warning disable CS8629 // Nullable 값 형식이 null일 수 있습니다.
                            channelName.AddText(dateTime.Value.Humanize(dateToCompareAgainst: now), f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
#pragma warning restore CS8629 // Nullable 값 형식이 null일 수 있습니다.
                            setText(commentData.Snippet.TopLevelComment.Snippet.TextOriginal);
                            likeCount.Text = Convert.ToInt32(commentData.Snippet.TopLevelComment.Snippet.LikeCount).ToStandardFormattedString(0);
                            //translateToText.Text = NekoPlayerStrings.TranslateTo(app.CurrentLanguage.Value.GetLocalisableDescription());
                            profileImage.UpdateProfileImage(commentData.Snippet.TopLevelComment.Snippet.AuthorChannelId.Value);
                            replyCount.Text = Convert.ToInt32(commentData.Snippet.TotalReplyCount).ToStandardFormattedString(0);

                            usernameDisplayMode.BindValueChanged(locale =>
                            {
                                Schedule(() =>
                                {
                                    channelName.Text = string.Empty;
                                    channelName.Text = channelData != null ? api.GetLocalizedChannelTitle(channelData) : commentData.Snippet.TopLevelComment.Snippet.AuthorDisplayName;
                                    channelName.AddText(" • ", f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
                                    channelName.AddText(dateTime.Value.Humanize(dateToCompareAgainst: now), f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
                                });
                            }, true);

                            uiLanguage.BindValueChanged(locale =>
                            {
                                Schedule(() =>
                                {
                                    channelName.Text = string.Empty;
                                    channelName.Text = channelData != null ? api.GetLocalizedChannelTitle(channelData) : commentData.Snippet.TopLevelComment.Snippet.AuthorDisplayName;
                                    channelName.AddText(" • ", f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
                                    channelName.AddText(dateTime.Value.Humanize(dateToCompareAgainst: now), f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
                                    //translateToText.Text = NekoPlayerStrings.TranslateTo(app.CurrentLanguage.Value.GetLocalisableDescription());
                                });
                            });

                            Schedule(() => loading.Hide());
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, e.GetDescription());
                        Schedule(() =>
                        {
                            channelName.Text = commentData.Snippet.TopLevelComment.Snippet.AuthorDisplayName;
                            channelName.AddText(" • ", f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
#pragma warning disable CS8629 // Nullable 값 형식이 null일 수 있습니다.
                            channelName.AddText(dateTime.Value.Humanize(dateToCompareAgainst: now), f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
#pragma warning restore CS8629 // Nullable 값 형식이 null일 수 있습니다.
                            setText(commentData.Snippet.TopLevelComment.Snippet.TextOriginal);
                            likeCount.Text = Convert.ToInt32(commentData.Snippet.TopLevelComment.Snippet.LikeCount).ToStandardFormattedString(0);
                            //translateToText.Text = NekoPlayerStrings.TranslateTo(app.CurrentLanguage.Value.GetLocalisableDescription());
                            profileImage.UpdateProfileImage(commentData.Snippet.TopLevelComment.Snippet.AuthorChannelId.Value);
                            replyCount.Text = Convert.ToInt32(commentData.Snippet.TotalReplyCount).ToStandardFormattedString(0);

                            usernameDisplayMode.BindValueChanged(locale =>
                            {
                                Schedule(() =>
                                {
                                    channelName.Text = string.Empty;
                                    channelName.Text = commentData.Snippet.TopLevelComment.Snippet.AuthorDisplayName;
                                    channelName.AddText(" • ", f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
                                    channelName.AddText(dateTime.Value.Humanize(dateToCompareAgainst: now), f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
                                });
                            }, true);

                            uiLanguage.BindValueChanged(locale =>
                            {
                                Schedule(() =>
                                {
                                    channelName.Text = string.Empty;
                                    channelName.Text = commentData.Snippet.TopLevelComment.Snippet.AuthorDisplayName;
                                    channelName.AddText(" • ", f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
                                    channelName.AddText(dateTime.Value.Humanize(dateToCompareAgainst: now), f => f.Font = NekoPlayerApp.DefaultFont.With(size: 13, weight: "Regular"));
                                    //translateToText.Text = NekoPlayerStrings.TranslateTo(app.CurrentLanguage.Value.GetLocalisableDescription());
                                });
                            });

                            Schedule(() => loading.Hide());
                        });
                    }
                }
                catch
                {
                    Hide();
                }
            });
        }
    }
}
