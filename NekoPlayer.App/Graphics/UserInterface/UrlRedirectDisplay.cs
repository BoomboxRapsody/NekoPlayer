// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Threading.Tasks;
using HtmlAgilityPack;
using NekoPlayer.App.Config;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Online;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osuTK.Graphics;
using YoutubeExplode.Videos;
using YoutubeExplode.Playlists;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class UrlRedirectDisplay : AdaptiveClickableContainer
    {
        private string url;

        private AdaptiveSpriteText displayName, urlText;

        protected Box Hover;

        public UrlRedirectDisplay(string url)
            : base(HoverSampleSet.Button)
        {
            this.url = url;
            Enabled.Value = true;
            Masking = true;
            TooltipText = url;
        }

        private SpriteIcon icon;
        private Bindable<Localisation.Language> uiLanguage = null!;
        private Bindable<UsernameDisplayMode> usernameDisplayMode = null!;

        [Resolved]
        private NekoPlayerConfigManager appConfig { get; set; } = null!;

        [BackgroundDependencyLoader]
        private async Task load(OverlayColourProvider overlayColourProvider)
        {
            uiLanguage = app.CurrentLanguage.GetBoundCopy();
            usernameDisplayMode = appConfig.GetBindable<UsernameDisplayMode>(NekoPlayerSetting.UsernameDisplayMode);
            AutoSizeAxes = Axes.Both;

            AddRangeInternal(new Drawable[]
            {
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = new CornersInfo(NekoPlayerApp.UI_CORNER_RADIUS / 1.5f),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = overlayColourProvider.Background2,
                        },
                        new FillFlowContainer
                        {
                            Margin = new MarginPadding(2),
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                icon = new SpriteIcon
                                {
                                    Size = new osuTK.Vector2(12),
                                    Margin = new MarginPadding(4),
                                },
                                new FillFlowContainer
                                {
                                    Margin = new MarginPadding(2),
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Children = new Drawable[]
                                    {
                                        displayName = new AdaptiveSpriteText
                                        {
                                            Margin = new MarginPadding(2),
                                            Text = url,
                                        },
                                        urlText = new AdaptiveSpriteText
                                        {
                                            Margin = new MarginPadding(2),
                                            Text = url,
                                            Colour = overlayColourProvider.Foreground1,
                                            Font = NekoPlayerApp.DefaultFont.With(size: 12),
                                        }
                                    }
                                },
                            }
                        },
                        Hover = new Box
                        {
                            Alpha = 0,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                            Blending = BlendingParameters.Additive,
                            Depth = float.MinValue
                        },
                    }
                }
            });

#pragma warning disable CS4014 // 이 호출을 대기하지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다.
            Task.Run(async () =>
            {
                string title = await GetTitleFromLink(url);

                if (NekoPlayerDescriptionParser.IsYouTubeVideo(url))
                {
                    icon.Icon = FontAwesome.Brands.Youtube;

                    string videoId = VideoId.Parse(url);
                    Google.Apis.YouTube.v3.Data.Video video = api.GetVideo(videoId);

                    displayName.Text = api.GetLocalizedVideoTitle(video);

                    uiLanguage.BindValueChanged(locale =>
                    {
                        Schedule(() =>
                        {
                            displayName.Text = api.GetLocalizedVideoTitle(video);
                        });
                    });
                }
                else if (NekoPlayerDescriptionParser.IsYouTubePlaylist(url))
                {
                    icon.Icon = FontAwesome.Brands.Youtube;

                    string playlistId = PlaylistId.Parse(url);
                    Google.Apis.YouTube.v3.Data.Playlist video = api.GetPlaylistInfo(playlistId);

                    displayName.Text = video.Snippet.Title;
                }
                else if (NekoPlayerDescriptionParser.IsYouTubeChannel(url))
                {
                    icon.Icon = FontAwesome.Brands.Youtube;

                    string channelId = url.Replace("https://www.youtube.com/channel/", string.Empty);
                    Google.Apis.YouTube.v3.Data.Channel channel = api.GetChannel(channelId);

                    displayName.Text = api.GetLocalizedChannelTitle(channel);

                    uiLanguage.BindValueChanged(locale =>
                    {
                        Schedule(() =>
                        {
                            displayName.Text = api.GetLocalizedChannelTitle(channel);
                        });
                    });

                    usernameDisplayMode.BindValueChanged(locale =>
                    {
                        Schedule(() =>
                        {
                            displayName.Text = api.GetLocalizedChannelTitle(channel);
                        });
                    });
                }
                else if (NekoPlayerDescriptionParser.IsDiscord(url))
                {
                    icon.Icon = FontAwesome.Brands.Discord;
                    displayName.Text = title;
                }
                else if (NekoPlayerDescriptionParser.IsTwitch(url))
                {
                    icon.Icon = FontAwesome.Brands.Twitch;
                    displayName.Text = title;
                }
                else if (NekoPlayerDescriptionParser.IsTwitter(url))
                {
                    icon.Icon = FontAwesome.Brands.Twitter;
                    displayName.Text = title;
                }
                else
                {
                    icon.Icon = FontAwesome.Solid.Globe;
                    displayName.Text = title;
                }
            });
#pragma warning restore CS4014 // 이 호출을 대기하지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다.
        }

        public async Task<string> GetTitleFromLink(string url)
        {
            var web = new HtmlWeb
            {
                OverrideEncoding = System.Text.Encoding.UTF8,
                UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.52 Safari/537.36",
            };
            var doc = await web.LoadFromWebAsync(url);
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            return titleNode?.InnerText?.Trim() ?? url;
        }

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private YouTubeAPI api { get; set; }

        [Resolved]
        private NekoPlayerAppBase app { get; set; }

        protected virtual float HoverLayerFinalAlpha => 0.1f;

        protected override bool OnHover(HoverEvent e)
        {
            if (Enabled.Value)
            {
                Hover.FadeTo(0.2f, 40, Easing.OutQuint)
                     .Then()
                     .FadeTo(HoverLayerFinalAlpha, 800, Easing.OutQuint);
            }

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            Hover.FadeOut(800, Easing.OutQuint);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (NekoPlayerDescriptionParser.IsYouTubeVideo(url))
                app.AppMessageHandler.SelectVideo(url);
            if (NekoPlayerDescriptionParser.IsYouTubePlaylist(url))
                app.AppMessageHandler.SelectPlaylist(url);
            else
                host.OpenUrlExternally(url);

            return base.OnClick(e);
        }
    }
}
