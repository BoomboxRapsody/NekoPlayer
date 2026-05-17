// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NekoPlayer.App.Config;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Online;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Win32;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

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

        private Sprite background;

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
                        new BufferedContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            BlurSigma = new osuTK.Vector2(3),
                            Child = background = new Sprite
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                FillMode = FillMode.Fill,
                            },
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = overlayColourProvider.Background2,
                            Alpha = 0.5f,
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
                string title = await GetTitleFromLink_v2(url);

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

        public async Task<string> GetTitleFromLink_v2(string url)
        {
            // Get illegal characters for the current OS
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            string wth = string.Empty;
            // 1. Chrome 옵션 객체 생성
            ChromeOptions options = new ChromeOptions();

            // 2. 창을 띄우지 않는 Headless 모드 설정
            options.AddArgument("--headless=new"); // 최신 Selenium/Chrome 방식
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--mute-audio");
            options.AddArgument("--disable-extensions");

            // (선택) 리소스 절약 및 에러 방지를 위한 추가 옵션
            options.AddArgument("--disable-dev-shm-usage");   // 공유 메모리 파일 사용 안 함 (리눅스 환경 등에서 필수)
            options.AddArgument("--window-size=1920,1080");   // 가상 화면 크기 설정 (요소 인식을 위해 필요할 수 있음)

            // 3. 설정된 옵션을 적용하여 드라이버 실행
            using (IWebDriver driver = new ChromeDriver(options))
            {
                // 4. 웹사이트 접속 및 작업 수행
                await driver.Navigate().GoToUrlAsync(url);

                wth = driver.Title;

                ITakesScreenshot takesScreenshot = driver as ITakesScreenshot;

                if (takesScreenshot != null)
                {
                    // 3. 스크린샷 캡처
                    Screenshot screenshot = takesScreenshot.GetScreenshot();

                    // 4. 원하는 경로에 파일 저장
                    string savePath = app.Host.CacheStorage.GetStorageForDirectory("webScreenshotCache").GetFullPath($"{Regex.Replace(url, invalidRegStr, "_")}.png");
                    screenshot.SaveAsFile(savePath);

                    using Image<Rgba32> bitmap = SixLabors.ImageSharp.Image.Load<Rgba32>(app.Host.CacheStorage.GetStorageForDirectory("webScreenshotCache").GetFullPath($"{Regex.Replace(url, invalidRegStr, "_")}.png"));

                    var bitmap2 = bitmap.Clone();

                    var tex = renderer.CreateTexture(bitmap2.Width, bitmap2.Height);
                    tex.SetData(new TextureUpload(bitmap2));

                    Schedule(() => { background.Texture = tex; });
                }

                // 드라이버 종료
                driver.Quit();
            }

            return wth;
        }

        [Resolved]
        private IRenderer renderer { get; set; }

        [Resolved]
        private TextureStore textureStore { get; set; }

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
