// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Threading.Tasks;
using NekoPlayer.App.Graphics.Containers;
using NekoPlayer.App.Online;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Platform;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class UrlRedirectDisplay : AdaptiveClickableContainer
    {
        private string url;

        private AdaptiveTextFlowContainer textFlow;

        public UrlRedirectDisplay(string url)
            : base(HoverSampleSet.Button)
        {
            this.url = url;
            Enabled.Value = true;
            Masking = true;
            TooltipText = url;
        }

        [BackgroundDependencyLoader]
        private async Task load(OverlayColourProvider overlayColourProvider)
        {
            AutoSizeAxes = Axes.Both;

            AddRangeInternal(new Drawable[]
            {
                new CircularContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = overlayColourProvider.Background2,
                        },
                        textFlow = new AdaptiveTextFlowContainer(f =>
                        {
                            f.Font = NekoPlayerApp.DefaultFont.With(size: 13.5f, weight: "Bold");
                        })
                        {
                            AutoSizeAxes = Axes.Both,
                            Margin = new MarginPadding(2),
                        }
                    }
                }
            });
        }

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private NekoPlayerAppBase app { get; set; }

        protected virtual float HoverLayerFinalAlpha => 0.1f;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            textFlow.AddIcon(FontAwesome.Solid.Globe, o => o.Margin = new MarginPadding() { Right = 4 });
            textFlow.AddText(url);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (NekoPlayerDescriptionParser.IsYouTubeVideo(url))
                app.AppMessageHandler.SelectVideo(url);
            else if (NekoPlayerDescriptionParser.IsYouTubePlaylist(url))
                app.AppMessageHandler.SelectPlaylist(url);
            else
                host.OpenUrlExternally(url);

            return base.OnClick(e);
        }
    }
}
