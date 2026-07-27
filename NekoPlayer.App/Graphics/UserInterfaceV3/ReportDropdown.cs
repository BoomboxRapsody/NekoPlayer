// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Graphics.UserInterfaceV2;
using NekoPlayer.App.Online;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Graphics.UserInterfaceV3
{
    public partial class ReportDropdown : FormDropdown<VideoAbuseReportReasonItem>
    {
        protected override LocalisableString GenerateItemText(VideoAbuseReportReasonItem item)
            => item.Label;
    }
}
