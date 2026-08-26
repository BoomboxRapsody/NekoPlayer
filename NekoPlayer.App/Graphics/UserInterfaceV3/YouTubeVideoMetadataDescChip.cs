// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Humanizer;
using NekoPlayer.App.Config;
using NekoPlayer.App.Extensions;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Localisation;
using NekoPlayer.App.Online;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class YouTubeVideoMetadataDescChip : CompositeDrawable
    {
        private ProfileImage profileImage;
        private ProjectYomiTextFlowContainer videoName;
        private TruncatingSpriteText desc;
        public Action<YouTubeVideoMetadataDescChip> ClickEvent;

        private Box bgLayer, hover;

        [Resolved]
        private YouTubeAPI api { get; set; }

        [Resolved]
        private FrameworkConfigManager frameworkConfig { get; set; }

        [Resolved]
        private NekoPlayerConfigManager appConfig { get; set; }

        [Resolved]
        private NekoPlayerAppBase app { get; set; }

        private Bindable<Localisation.Language> uiLanguage;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            uiLanguage = app.CurrentLanguage.GetBoundCopy();

            Height = 20;
            AutoSizeAxes = Axes.X;

            CornerRadius = 10;
            Masking = true;

            InternalChildren = new Drawable[]
            {
                samples,
                bgLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = overlayColourProvider.Background4,
                    Alpha = 1f,
                },
                profileImage = new ProfileImage(20)
                {
                    Enabled = { Value = false }
                },
                new Container {
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Padding = new MarginPadding(4),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            AutoSizeAxes = Axes.X,
                            RelativeSizeAxes = Axes.Y,
                            Padding = new MarginPadding
                            {
                                Left = 20,
                                Right = 5,
                            },
                            Children = new Drawable[]
                            {
                                videoName = new ProjectYomiTextFlowContainer(f => f.Font = NekoPlayerApp.DefaultFont.With(size: 12, weight: "ExtraBold"))
                                {
                                    AutoSizeAxes = Axes.X,
                                    RelativeSizeAxes = Axes.Y,
                                    Text = "",
                                    Colour = overlayColourProvider.Content2,
                                },
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

            UpdateDesc(VideoData);
        }

        public Video VideoData;

        protected override bool OnClick(ClickEvent e)
        {
            ClickEvent?.Invoke(this);

            return base.OnClick(e);
        }

        private HoverSounds samples = new HoverClickSounds(HoverSampleSet.Default);

        protected override bool OnHover(HoverEvent e)
        {
            if (ClickEvent != null)
                hover.FadeTo(0.1f, 500, Easing.OutQuint);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            if (ClickEvent != null)
                hover.FadeOut(500, Easing.OutQuint);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            (samples as HoverClickSounds).Enabled.Value = (ClickEvent != null);
        }

        public string TruncateWithEllipsis(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            // If the string is already short enough, return it as-is
            if (value.Length <= maxLength) return value;

            // Ensure we don't get a negative length if maxLength is smaller than the ellipsis
            int truncateLength = Math.Max(0, maxLength - 3);

            return value.Substring(0, truncateLength) + "...";
        }

        public void UpdateDesc(Video video)
        {
            DateTimeOffset? dateTime = video.Snippet.PublishedAtDateTimeOffset;
            DateTimeOffset now = DateTime.Now;

            Channel channelData = api.GetChannel(video.Snippet.ChannelId);

            uiLanguage.UnbindEvents();
            VideoData = video;
            Task.Run(async () =>
            {
                Schedule(() =>
                {
                    videoName.Text = NekoPlayerStrings.VideoMetadataDescWithLikeCount(TruncateWithEllipsis(api.GetLocalizedChannelTitle(channelData), 20), video.Statistics.LikeCount != null ? Convert.ToDouble(video.Statistics.LikeCount).ToStandardFormattedString(0) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(video.Id).RawLikes).ToStandardFormattedString(0), Convert.ToInt32(video.Statistics.ViewCount).ToStandardFormattedString(0), dateTime.Value.Humanize(dateToCompareAgainst: now));
                    profileImage.UpdateProfileImage(video.Snippet.ChannelId);
                });
            });

            uiLanguage.BindValueChanged(locale =>
            {
                Task.Run(async () =>
                {
                    Schedule(() =>
                    {
                        videoName.Text = NekoPlayerStrings.VideoMetadataDescWithLikeCount(TruncateWithEllipsis(api.GetLocalizedChannelTitle(channelData), 20), video.Statistics.LikeCount != null ? Convert.ToDouble(video.Statistics.LikeCount).ToStandardFormattedString(0) : Convert.ToDouble(ReturnYouTubeDislike.GetDislikes(video.Id).RawLikes).ToStandardFormattedString(0), Convert.ToInt32(video.Statistics.ViewCount).ToStandardFormattedString(0), dateTime.Value.Humanize(dateToCompareAgainst: now));
                    });
                });
            });
        }
    }
}
