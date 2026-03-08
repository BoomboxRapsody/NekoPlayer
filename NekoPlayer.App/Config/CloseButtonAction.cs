// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Localisation;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Config
{
    public enum CloseButtonAction
    {
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.HideToTrayIcon))]
        HideToTrayIcon,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.CloseApp))]
        CloseApp,
    }
}
