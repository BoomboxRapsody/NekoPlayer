// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using NekoPlayer.App.Graphics.UserInterfaceV2;
using osu.Framework.Graphics.Sprites;

namespace NekoPlayer.App.Graphics
{
    public partial class SettingsIconButtonV2 : RoundedIconButton
    {
        public SettingsIconButtonV2(IconUsage icon)
            : base(icon)
        {
            RelativeSizeAxes = Axes.X;
        }
    }
}
