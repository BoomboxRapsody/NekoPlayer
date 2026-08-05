// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Shapes;

namespace NekoPlayer.App.Graphics.Containers
{
    public partial class OverlayBackground : Box
    {
        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            Colour = overlayColourProvider.Background5;
        }
    }
}
