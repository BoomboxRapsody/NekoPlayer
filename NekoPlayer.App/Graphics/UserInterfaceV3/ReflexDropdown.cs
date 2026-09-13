// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Graphics.UserInterfaceV2;
using NekoPlayer.App.Localisation;
using osu.Framework.Graphics.Rendering.LowLatency;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Graphics.UserInterfaceV3
{
    public partial class ReflexDropdown : FormEnumDropdown<LatencyMode>
    {
        protected override LocalisableString GenerateItemText(LatencyMode item)
        {
            switch (item)
            {
                case LatencyMode.Off:
                    return NekoPlayerStrings.ReflexOff;

                case LatencyMode.On:
                    return NekoPlayerStrings.ReflexOn;

                case LatencyMode.Boost:
                    return NekoPlayerStrings.ReflexBoost;
            }
            return base.GenerateItemText(item);
        }
    }
}
