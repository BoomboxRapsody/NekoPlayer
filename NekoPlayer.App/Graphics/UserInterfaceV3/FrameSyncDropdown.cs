// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Graphics.UserInterfaceV2;
using NekoPlayer.App.Localisation;
using osu.Framework.Configuration;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Graphics.UserInterfaceV3
{
    public partial class FrameSyncDropdown : FormEnumDropdown<FrameSync>
    {
        protected override LocalisableString GenerateItemText(FrameSync item)
        {
            switch (item)
            {
                case FrameSync.VSync:
                    return NekoPlayerStrings.VSync;

                case FrameSync.Limit2x:
                    return NekoPlayerStrings.RefreshRate2X;

                case FrameSync.Limit4x:
                    return NekoPlayerStrings.RefreshRate4X;

                case FrameSync.Limit8x:
                    return NekoPlayerStrings.RefreshRate8X;

                case FrameSync.Unlimited:
                    return NekoPlayerStrings.Unlimited;
            }
            return base.GenerateItemText(item);
        }
    }
}
