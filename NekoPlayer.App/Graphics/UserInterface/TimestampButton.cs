// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Localisation;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Logging;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class TimestampButton : AdaptiveClickableContainer
    {
        private string text;
        public Action<double> TimestampClicked;

        public TimestampButton(string text)
            : base(HoverSampleSet.Button)
        {
            this.text = text;
            Enabled.Value = true;
            Masking = true;
            TooltipText = NekoPlayerStrings.JumpTo(text);
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            AutoSizeAxes = Axes.Both;

            AddRangeInternal(new Drawable[]
            {
                new CircularContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = overlayColourProvider.Background2,
                        },
                        new AdaptiveSpriteText
                        {
                            Margin = new MarginPadding(2),
                            Text = text,
                            Font = NekoPlayerApp.DefaultFont.With(size: 13.5f, weight: "Bold"),
                        }
                    }
                }
            });
        }

        protected override bool OnClick(ClickEvent e)
        {
            TimeSpan ts = TimeSpan.Parse(text);
            int seconds = (int)ts.TotalSeconds;

            Logger.Log(seconds.ToString());

            TimestampClicked.Invoke(Convert.ToDouble(seconds));

            return base.OnClick(e);
        }
    }
}
