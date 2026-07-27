// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NekoPlayer.App.Config;
using NekoPlayer.App.Graphics.UserInterfaceV2;
using NekoPlayer.App.Online;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Localisation;
using osu.Framework.Logging;

namespace NekoPlayer.App.Graphics.UserInterfaceV3
{
    public partial class YouTubeI18nLangDropdown : FormDropdown<YouTubeI18nLangItem>
    {
        [Resolved]
        private NekoPlayerApp app { get; set; }

        [Resolved]
        private NekoPlayerConfigManager config { get; set; }

        [Resolved]
        private YoutubeExplode.YoutubeClient youtubeService { get; set; }

        [Resolved]
        private GoogleTranslate googleTranslate { get; set; }

        private Bindable<int> closedCaptionLanguageValue;

        [BackgroundDependencyLoader]
        private void load()
        {
        }

        public async Task RefreshCaptionLanguages(string videoId)
        {
            try
            {
                var trackManifest = await youtubeService.Videos.ClosedCaptions.GetManifestAsync(videoId);

                List<YouTubeI18nLangItem> items = new List<YouTubeI18nLangItem>();

                foreach (var item in trackManifest.Tracks)
                {
                    YouTubeI18nLangItem youTubeI18NLangItem = new YouTubeI18nLangItem
                    {
                        Hl = item.Language.Code,
                        Name = item.Language.Name,
                    };

                    items.Add(youTubeI18NLangItem);
                }

                Items = items;

                if (!Current.Disabled)
                {
                    if (items.Where(lang => lang.Hl.Contains(CultureInfo.CurrentCulture.Name)).First() == null)
                    {
                        Current.Value = items.First();
                        Current.Default = items.First();
                    }
                    else
                    {
                        Current.Value = items.Where(lang => lang.Hl.Contains(CultureInfo.CurrentCulture.Name)).First();
                        Current.Default = items.Where(lang => lang.Hl.Contains(CultureInfo.CurrentCulture.Name)).First();
                    }
                }
            }
            catch (Exception e)
            {
                Current.Disabled = false;
                Logger.Error(e, e.GetDescription());
            }
        }

        protected override LocalisableString GenerateItemText(YouTubeI18nLangItem item)
        {
            try
            {
                return item.Name;
            }
            catch
            {
                return base.GenerateItemText(item);
            }
        }
    }
}
