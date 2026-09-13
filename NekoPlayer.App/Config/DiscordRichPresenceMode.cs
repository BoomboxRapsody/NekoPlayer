// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using NekoPlayer.App.Localisation;

namespace NekoPlayer.App.Config
{
    public enum DiscordRichPresenceMode
    {
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.DiscordPresenceOff))]
        Off,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.HideIdentifiableInformation))]
        Limited,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.DiscordPresenceFull))]
        Full
    }
}
