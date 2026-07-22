// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NekoPlayer.App.Localisation;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Config
{
    public enum SFXType
    {
        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.SFXNew))]
        Normal,

        [LocalisableDescription(typeof(NekoPlayerStrings), nameof(NekoPlayerStrings.SFXLegacy))]
        Legacy,
    }
}
