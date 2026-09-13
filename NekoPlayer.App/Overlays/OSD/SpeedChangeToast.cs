// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Localisation;
using osu.Framework.Graphics.Sprites;

namespace NekoPlayer.App.Overlays.OSD
{
    public partial class SpeedChangeToast : ToastWithIcon
    {
        public SpeedChangeToast(double newSpeed)
            : base(NekoPlayerStrings.PlaybackSpeedWithoutValue, $@"{newSpeed:0.##}x", FontAwesome.Solid.TachometerAlt)
        {
        }
    }
}
