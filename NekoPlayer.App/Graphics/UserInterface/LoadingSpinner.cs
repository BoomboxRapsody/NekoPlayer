// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.UserInterface
{
    /// <summary>
    /// A loading spinner.
    /// </summary>
    public partial class LoadingSpinner : VisibilityContainer
    {
        private readonly SpriteIcon spinner;
        private readonly Box bg;

        protected override bool StartHidden => true;

        protected Container MainContents;

        public const float TRANSITION_DURATION = 500;

        private const float spin_duration = 900;

        private readonly bool inverted;

        /// <summary>
        /// Constuct a new loading spinner.
        /// </summary>
        /// <param name="withBox">Whether the spinner should have a surrounding black box for visibility.</param>
        /// <param name="inverted">Whether colours should be inverted (black spinner instead of white).</param>
        public LoadingSpinner(bool withBox = false, bool inverted = false)
        {
            this.inverted = inverted;
            Size = new Vector2(60);

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            Child = MainContents = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 20,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    bg = new Box
                    {
                        Colour = inverted ? Color4.White : Color4.Black,
                        RelativeSizeAxes = Axes.Both,
                        Alpha = withBox ? 0.7f : 0
                    },
                    spinner = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Custom,
                        Colour = inverted ? Color4.Black : Color4.White,
                        Scale = new Vector2(withBox ? 0.6f : 1),
                        RelativeSizeAxes = Axes.Both,
                        Icon = FontAwesome.Solid.CircleNotch
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            bg.Colour = inverted ? overlayColourProvider.Background5 : overlayColourProvider.Content2;
            spinner.Colour = inverted ? overlayColourProvider.Content2 : overlayColourProvider.Background5;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            rotate();
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // Font awesome icon isn't centered perfectly.
            spinner.OriginPosition = spinner.DrawSize * 0.4963333333f;

            MainContents.CornerRadius = MainContents.DrawWidth / 4;
        }

        protected override void PopIn()
        {
            if (Alpha < 0.5f)
                // reset animation if the user can't see us.
                rotate();

            MainContents.ScaleTo(1, TRANSITION_DURATION, Easing.OutQuint);
            this.FadeIn(TRANSITION_DURATION * 2, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            MainContents.ScaleTo(0.8f, TRANSITION_DURATION / 2, Easing.In);
            this.FadeOut(TRANSITION_DURATION, Easing.OutQuint);
        }

        private void rotate()
        {
            spinner.Spin(spin_duration * 3.5f, RotationDirection.Clockwise);

            MainContents.RotateTo(0).Then()
                        .RotateTo(90, spin_duration, Easing.InOutQuart).Then()
                        .RotateTo(180, spin_duration, Easing.InOutQuart).Then()
                        .RotateTo(270, spin_duration, Easing.InOutQuart).Then()
                        .RotateTo(360, spin_duration, Easing.InOutQuart).Then()
                        .Loop();
        }
    }
}
