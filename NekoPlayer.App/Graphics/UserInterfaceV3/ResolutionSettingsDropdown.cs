// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using NekoPlayer.App.Graphics.UserInterfaceV2;
using NekoPlayer.App.Localisation;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Graphics.UserInterfaceV3
{
    public partial class ResolutionSettingsDropdown : FormDropdown<Size>
    {
        protected override LocalisableString GenerateItemText(Size item)
        {
            if (item == new Size(9999, 9999))
                return NekoPlayerStrings.Default;

            return $"{item.Width}x{item.Height}";
        }
    }
}
