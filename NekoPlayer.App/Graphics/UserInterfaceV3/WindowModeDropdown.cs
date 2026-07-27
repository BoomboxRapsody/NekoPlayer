// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Graphics.UserInterfaceV2;
using NekoPlayer.App.Localisation;
using osu.Framework.Configuration;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Graphics.UserInterfaceV3
{
    public partial class WindowModeDropdown : FormDropdown<WindowMode>
    {
        protected override LocalisableString GenerateItemText(WindowMode item)
        {
            switch (item)
            {
                case WindowMode.Windowed:
                    return NekoPlayerStrings.Windowed;

                case WindowMode.Borderless:
                    return NekoPlayerStrings.Borderless;

                case WindowMode.Fullscreen:
                    return NekoPlayerStrings.Fullscreen;
            }
            return base.GenerateItemText(item);
        }
    }
}
