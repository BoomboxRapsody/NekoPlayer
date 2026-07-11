// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using Spine;

namespace NekoPlayer.App.Extensions
{
    public static class SpineWrapExtensions
    {
        /// <summary>
        /// Converts Spine <see cref="TextureWrap"/> into osu!framework <see cref="WrapMode"/> 
        /// </summary>
        /// <param name="wrap">The target <see cref="TextureWrap"/> to convert.</param>
        /// <returns>A usable <see cref="WrapMode"/> value for <see cref="Texture"/>.</returns>
        public static WrapMode ToWrapMode(this TextureWrap wrap) => wrap switch
        {
            // Should make a shader that makes use of this properly
            TextureWrap.MirroredRepeat => WrapMode.Repeat,
            TextureWrap.Repeat => WrapMode.Repeat,
            TextureWrap.ClampToEdge => WrapMode.ClampToEdge,
            _ => WrapMode.None
        };
    }
}
