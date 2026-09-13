// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Framework.Graphics;
using NekoPlayer.App.Graphics.Shaders;

namespace NekoPlayer.App.Graphics
{
    public interface ISingleShaderBufferedDrawable : IBufferedDrawable
    {
        ICustomizedShader? Shader { get; }
    }
}
