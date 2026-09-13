// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using NekoPlayer.App.Localisation;

namespace NekoPlayer.App.Config
{
    public enum UsernameDisplayMode
    {
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.UsernameDisplayMode_DisplayName))]
        DisplayName,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.UsernameDisplayMode_Handle))]
        Handle,
    }
}
