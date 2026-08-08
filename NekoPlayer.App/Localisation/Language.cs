// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NekoPlayer.App.Localisation
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public enum Language
    {
        [Description(@"English")]
        en,

        [Description(@"日本語")]
        ja,

        [Description(@"한국어")]
        ko,

#if DEBUG
        [Description(@"Debug (show raw keys)")]
        debug
#endif
    }
}
