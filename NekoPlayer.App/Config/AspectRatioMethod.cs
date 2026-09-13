// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using NekoPlayer.App.Localisation;

namespace NekoPlayer.App.Config
{
    public enum AspectRatioMethod
    { 
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.Letterbox))]
        Letterbox,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.Fill))]
        Fill,
    }
}
