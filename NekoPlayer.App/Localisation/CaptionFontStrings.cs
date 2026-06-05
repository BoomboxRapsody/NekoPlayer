// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Humanizer;
using NekoPlayer.App.Extensions;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Localisation
{
    public static class CaptionFontStrings
    {
        private const string prefix = @"NekoPlayer.App.BuiltInResources.Localisation.CaptionFonts";

        /// <summary>
        /// "Hungeul"
        /// </summary>
        public static LocalisableString Hungeul => new TranslatableString(getKey(@"hungeul"), "Hungeul");

        /// <summary>
        /// "Ownglyph PDH"
        /// </summary>
        public static LocalisableString Ownglyph_PDH => new TranslatableString(getKey(@"ownglyph_pdh"), "Ownglyph PDH");

        /// <summary>
        /// "Dovemayo Gothic"
        /// </summary>
        public static LocalisableString Dovemayo_Gothic => new TranslatableString(getKey(@"dovemayo_gothic"), "Dovemayo Gothic");

        /// <summary>
        /// "Pretendard"
        /// </summary>
        public static LocalisableString Pretendard => new TranslatableString(getKey(@"pretendard"), "Pretendard");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
