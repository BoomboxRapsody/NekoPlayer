// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;

namespace NekoPlayer.App.Online
{
    public partial class VideoAbuseReportReasonItem
    {
        public string Id;
        public string Label;
        public IList<VideoAbuseReportReasonItem> SecondaryReasons = new List<VideoAbuseReportReasonItem>();
        public bool ContainsSecondaryReasons;
    }
}
