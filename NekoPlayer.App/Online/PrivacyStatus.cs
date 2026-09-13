// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using NekoPlayer.App.Localisation;

namespace NekoPlayer.App.Online
{
    public enum PrivacyStatus
    {
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.Public))]
        Public,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.Unlisted))]
        Unlisted,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.Private))]
        Private
    }
}
