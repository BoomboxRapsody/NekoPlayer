// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NekoPlayer.App.Graphics.Containers;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class PushNotificationOverlay : Container
    {
        private Box bg;
        private FillFlowContainer notifications;
        private BufferedContainer content;

        public PushNotificationOverlay()
        {
            RelativeSizeAxes = Axes.Both;

            Add(content = new BufferedContainer
            {
                Width = 296,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = NekoPlayerApp.UI_CORNER_RADIUS + 24,
                Margin = new MarginPadding(16),
                Origin = Anchor.TopRight,
                Anchor = Anchor.TopRight,
                AutoSizeDuration = 400,
                AutoSizeEasing = Easing.OutQuint,
                Children = new Drawable[]
                {
                    bg = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    notifications = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(8),
                        Spacing = new Vector2(8),
                        Children = Array.Empty<Drawable>()
                        {
                        }
                    }
                }
            });
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            bg.Colour = overlayColourProvider.Background5;
            content.MoveToX(100);
            content.FadeOut();
        }

        [Resolved]
        private AudioManager audio { get; set; }

        private TransformSequence<BufferedContainer> fadeIn;
        private ScheduledDelegate fadeOut;

        public void Push(PushNotificationContainer container)
        {
            Sample sample = audio.Samples.Get("NotificationPush");
            sample.Play();

            notifications.Add(container);

            // avoid starting a new fade-in if one is already active.
            if (fadeIn == null)
            {
                fadeIn = content.Animate(
                    b => b.BlurTo(new Vector2(0), 250, Easing.OutExpo),
                    b => b.FadeIn(500, Easing.OutQuint),
                    b => b.MoveToX(0f, 500, Easing.OutQuint)
                );

                fadeIn.Finally(_ => fadeIn = null);
            }

            fadeOut?.Cancel();
            fadeOut = Scheduler.AddDelayed(() =>
            {
                content.Animate(
                    b => b.BlurTo(new Vector2(15), 250, Easing.OutQuint),
                    b => b.FadeOutFromOne(250, Easing.OutQuint),
                    b => b.MoveToX(100, 250, Easing.OutQuint)
                );

                foreach (var item in notifications.AliveChildren)
                {
                    Scheduler.AddDelayed(() => item.Expire(), 250);
                }
            }, 3500);

            container.MoveToX(296);

            container.MoveToX(0, 500, Easing.OutQuint);
        }
    }
}
