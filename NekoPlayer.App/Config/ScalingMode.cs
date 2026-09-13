// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Localisation;
using NekoPlayer.App.Localisation;

namespace NekoPlayer.App.Config
{
    public enum ScalingMode
    {
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.ScalingOff))]
        Off,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.ScaleEverything))]
        Everything,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.ScaleVideo))]
        Video,
    }
}
