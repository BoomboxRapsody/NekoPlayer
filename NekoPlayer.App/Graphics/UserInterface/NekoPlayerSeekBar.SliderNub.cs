// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class NekoPlayerSeekBar
    {
        public partial class SliderNub : Container
        {
            public const float HEIGHT = 30;

            public const float DEFAULT_EXPANDED_SIZE = 20;

            public SliderNub(float expandedSize = DEFAULT_EXPANDED_SIZE)
            {
                Size = new Vector2(expandedSize, expandedSize);

                InternalChildren = new[]
                {
                    new CircularContainer
                    {
                        Masking = true,
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        BorderColour = ColourInfo.GradientVertical(Color4.White.Opacity(0f), Color4.White.Opacity(0.25f)),
                        BorderThickness = 2,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Alpha = 1,
                                AlwaysPresent = true,
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientVertical(Color4.White.Opacity(0.5f), Color4.White),
                                Alpha = 0.1f,
                            },
                        }
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
            }
        }
    }
}
