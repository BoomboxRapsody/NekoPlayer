// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using NekoPlayer.App.Localisation;
using System.ComponentModel;

namespace NekoPlayer.App.Config
{
    public enum ScreenshotFormat
    {
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.Jpg))]
        Jpg = 1,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.Png))]
        Png = 2,

        [Description("WebP")]
        Webp = 3,
    }
}
