// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using NekoPlayer.App.Localisation;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Config
{
    public enum CaptionFonts
    {
        [Description("Google Sans Flex")]
        GoogleSansFlex,
        Rubik,
        [LocalisableDescription(typeof(CaptionFontStrings), nameof(CaptionFontStrings.Pretendard))]
        Pretendard,
        [LocalisableDescription(typeof(CaptionFontStrings), nameof(CaptionFontStrings.Hungeul))]
        Hungeul,
        [LocalisableDescription(typeof(CaptionFontStrings), nameof(CaptionFontStrings.Ownglyph_PDH))]
        Ownglyph_PDH,
        [LocalisableDescription(typeof(CaptionFontStrings), nameof(CaptionFontStrings.Dovemayo_Gothic))]
        Dovemayo_Gothic,
        [LocalisableDescription(typeof(CaptionFontStrings), nameof(CaptionFontStrings.Griun_Mongtori))]
        Griun_Mongtori,
        [LocalisableDescription(typeof(CaptionFontStrings), nameof(CaptionFontStrings.ONE_Mobile_POP))]
        ONE_Mobile_POP,
        [LocalisableDescription(typeof(CaptionFontStrings), nameof(CaptionFontStrings.PuzzleSansSuper))]
        PuzzleSansSuper,
    }
}
