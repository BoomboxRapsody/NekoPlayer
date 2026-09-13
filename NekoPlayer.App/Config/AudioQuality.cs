// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Localisation;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Config
{
    public enum AudioQuality
    {
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.PreferHighQualityAudio))]
        PreferHighQuality,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.PreferOpus))]
        PreferOpus,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.PreferMp4a))]
        PreferMp4a,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.LowQuality))]
        LowQuality,
    }
}
