// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;

namespace NekoPlayer.App.Extensions
{
    /// <summary>
    /// Utility class that contains the osu!framework BlendingParameters equivalent of MonoGame BlendMode.
    /// </summary>
    // these were made by chatgpt not me really, I will revise it soon to maximize the usage of this
    public static class SpineBlendModes
    {
        /// <summary>
        /// Blend mode for textures with premultiplied alpha.
        /// Equivalent to <see cref="BlendState.AlphaBlend"/> in MonoGame.
        /// </summary>
        public static readonly BlendingParameters AlphaBlend = new()
        {
            Source = BlendingType.One,
            Destination = BlendingType.OneMinusSrcAlpha,
            SourceAlpha = BlendingType.One,
            DestinationAlpha = BlendingType.OneMinusSrcAlpha,
            RGBEquation = BlendingEquation.Add,
            AlphaEquation = BlendingEquation.Add
        };

        /// <summary>
        /// Blend mode for non-premultiplied alpha.
        /// Equivalent to <see cref="BlendState.NonPremultiplied"/> in MonoGame.
        /// </summary>
        public static readonly BlendingParameters NonPremultiplied = new()
        {
            Source = BlendingType.SrcAlpha,
            Destination = BlendingType.OneMinusSrcAlpha,
            SourceAlpha = BlendingType.One,
            DestinationAlpha = BlendingType.OneMinusSrcAlpha,
            RGBEquation = BlendingEquation.Add,
            AlphaEquation = BlendingEquation.Add
        };

        /// <summary>
        /// Multiplicative blending (DstColor * Src).
        /// Useful for shadow and light effects.
        /// </summary>
        public static readonly BlendingParameters Multiply = new()
        {
            Source = BlendingType.DstColor,
            Destination = BlendingType.Zero,
            SourceAlpha = BlendingType.One,
            DestinationAlpha = BlendingType.OneMinusSrcAlpha,
            RGBEquation = BlendingEquation.Add,
            AlphaEquation = BlendingEquation.Add
        };

        /// <summary>
        /// Maximum color blending (max of each component).
        /// </summary>
        public static readonly BlendingParameters Max = new()
        {
            Source = BlendingType.One,
            Destination = BlendingType.One,
            SourceAlpha = BlendingType.One,
            DestinationAlpha = BlendingType.One,
            RGBEquation = BlendingEquation.Max,
            AlphaEquation = BlendingEquation.Max
        };

        /// <summary>
        /// Helper method to select the appropriate default blend mode.
        /// </summary>
        public static BlendingParameters GetDefault(bool premultipliedAlpha)
            => premultipliedAlpha ? AlphaBlend : NonPremultiplied;
    }
}
