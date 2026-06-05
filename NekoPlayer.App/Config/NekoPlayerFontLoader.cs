// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NekoPlayer.App.Graphics;
using osu.Framework;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Text;

namespace NekoPlayer.App.Config
{
    public class NekoPlayerFontLoader
    {
        public static void LoadFonts(Game game, ResourceStore<byte[]> Resources)
        {
            Logger.Log($"Initialising fonts to render texts.");

            var chironGoRoundTC = game.AddVariableFont(Resources, @"Fonts/UIFonts/ChironGoRoundTC");
            chironGoRoundTC.AddInstance(@"ChironGoRoundTC-Regular");
            chironGoRoundTC.AddInstance(@"ChironGoRoundTC-Bold");
            chironGoRoundTC.AddInstance(@"ChironGoRoundTC-SemiBold");
            chironGoRoundTC.AddInstance(@"ChironGoRoundTC-Light");

            var rubik = game.AddVariableFont(Resources, @"Fonts/UIFonts/Rubik");
            rubik.AddInstance(@"Rubik-Regular");
            rubik.AddInstance(@"Rubik-Bold");
            rubik.AddInstance(@"Rubik-SemiBold");
            rubik.AddInstance(@"Rubik-Light");

            var googleSansFlex = game.AddVariableFont(Resources, @"Fonts/UIFonts/GoogleSansFlex");
            googleSansFlex.AddInstance(
                new FontVariation
                {
                    Axes = new Dictionary<string, double>
                    {
                        { @"opsz", 144 },
                        { @"wght", 400 },
                        { @"wdth", 100 },
                        { @"ROND", 100 },
                    },
                },
                @"GoogleSansFlex-Regular");
            googleSansFlex.AddInstance(
                new FontVariation
                {
                    Axes = new Dictionary<string, double>
                    {
                        { @"opsz", 144 },
                        { @"wght", 700 },
                        { @"wdth", 100 },
                        { @"ROND", 100 },
                    },
                },
                @"GoogleSansFlex-Bold");
            googleSansFlex.AddInstance(
                new FontVariation
                {
                    Axes = new Dictionary<string, double>
                    {
                        { @"opsz", 144 },
                        { @"wght", 600 },
                        { @"wdth", 100 },
                        { @"ROND", 100 },
                    },
                },
                @"GoogleSansFlex-SemiBold");
            googleSansFlex.AddInstance(
                new FontVariation
                {
                    Axes = new Dictionary<string, double>
                    {
                        { @"opsz", 144 },
                        { @"wght", 300 },
                        { @"wdth", 100 },
                        { @"ROND", 100 },
                    },
                },
                @"GoogleSansFlex-Light");

            /*
            AddFont(Resources, @"Fonts/UIFonts/Noto/Noto-Basic");
            AddFont(Resources, @"Fonts/UIFonts/Noto/Noto-Bopomofo");
            AddFont(Resources, @"Fonts/UIFonts/Noto/Noto-CJK-Basic");
            AddFont(Resources, @"Fonts/UIFonts/Noto/Noto-CJK-Compatibility");
            AddFont(Resources, @"Fonts/UIFonts/Noto/Noto-Hangul");
            AddFont(Resources, @"Fonts/UIFonts/Noto/Noto-Thai");
            */

            var Pretendard = game.AddVariableFont(Resources, @"Fonts/UIFonts/PretendardVariable");
            Pretendard.AddInstance(@"PretendardVariable-Regular");
            Pretendard.AddInstance(@"PretendardVariable-Bold");
            Pretendard.AddInstance(@"PretendardVariable-SemiBold");
            Pretendard.AddInstance(@"PretendardVariable-Light");

            #region Noto Fonts
            var notoSans = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSans");
            notoSans.AddInstance(@"NotoSans-Regular");
            notoSans.AddInstance(@"NotoSans-Bold");
            notoSans.AddInstance(@"NotoSans-SemiBold");
            notoSans.AddInstance(@"NotoSans-Light");

            var notoSansOriya = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSansOriya");
            notoSansOriya.AddInstance(@"NotoSansOriya-Regular");
            notoSansOriya.AddInstance(@"NotoSansOriya-Bold");
            notoSansOriya.AddInstance(@"NotoSansOriya-SemiBold");
            notoSansOriya.AddInstance(@"NotoSansOriya-Light");

            var notoSansKR = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSansKR");
            notoSansKR.AddInstance(@"NotoSansKR-Regular");
            notoSansKR.AddInstance(@"NotoSansKR-Bold");
            notoSansKR.AddInstance(@"NotoSansKR-SemiBold");
            notoSansKR.AddInstance(@"NotoSansKR-Light");

            var notoSansKhmer = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSansKhmer");
            notoSansKhmer.AddInstance(@"NotoSansKhmer-Regular");
            notoSansKhmer.AddInstance(@"NotoSansKhmer-Bold");
            notoSansKhmer.AddInstance(@"NotoSansKhmer-SemiBold");
            notoSansKhmer.AddInstance(@"NotoSansKhmer-Light");

            var notoSansTC = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSansTC");
            notoSansTC.AddInstance(@"NotoSansTC-Regular");
            notoSansTC.AddInstance(@"NotoSansTC-Bold");
            notoSansTC.AddInstance(@"NotoSansTC-SemiBold");
            notoSansTC.AddInstance(@"NotoSansTC-Light");

            var notoSansSC = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSansSC");
            notoSansSC.AddInstance(@"NotoSansSC-Regular");
            notoSansSC.AddInstance(@"NotoSansSC-Bold");
            notoSansSC.AddInstance(@"NotoSansSC-SemiBold");
            notoSansSC.AddInstance(@"NotoSansSC-Light");

            var NotoSansTelugu = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSansTelugu");
            NotoSansTelugu.AddInstance(@"NotoSansTelugu-Regular");
            NotoSansTelugu.AddInstance(@"NotoSansTelugu-Bold");
            NotoSansTelugu.AddInstance(@"NotoSansTelugu-SemiBold");
            NotoSansTelugu.AddInstance(@"NotoSansTelugu-Light");

            var NotoSansArabic = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSansArabic");
            NotoSansArabic.AddInstance(@"NotoSansArabic-Regular");
            NotoSansArabic.AddInstance(@"NotoSansArabic-Bold");
            NotoSansArabic.AddInstance(@"NotoSansArabic-SemiBold");
            NotoSansArabic.AddInstance(@"NotoSansArabic-Light");

            var NotoSansThai = game.AddVariableFont(Resources, @"Fonts/UIFonts/NotoSansThai");
            NotoSansThai.AddInstance(@"NotoSansThai-Regular");
            NotoSansThai.AddInstance(@"NotoSansThai-Bold");
            NotoSansThai.AddInstance(@"NotoSansThai-SemiBold");
            NotoSansThai.AddInstance(@"NotoSansThai-Light");
            #endregion

            var clockFont = game.AddVariableFont(Resources, @"Fonts/UIFonts/InflateVF");
            clockFont.AddInstance(
                new FontVariation
                {
                    Axes = new Dictionary<string, double>
                    {
                        { @"wght", 1000 },
                    },
                },
            @"InflateVF-ClockFont");

            game.Fonts.AddStore(new EmojiStore(game.Host.Renderer, Resources));

            Logger.Log($"❤️👏 Colored emoji loaded");

            //caption fonts
            game.AddOutlineFont(Resources, @"Fonts/UIFonts/Hungeul");
            game.AddOutlineFont(Resources, @"Fonts/UIFonts/Ownglyph_PDH");
            game.AddOutlineFont(Resources, @"Fonts/UIFonts/Dovemayo_Gothic");
            game.AddOutlineFont(Resources, @"Fonts/UIFonts/Griun_Mongtori");
            game.AddOutlineFont(Resources, @"Fonts/UIFonts/ONE_Mobile_POP");
        }
    }
}
