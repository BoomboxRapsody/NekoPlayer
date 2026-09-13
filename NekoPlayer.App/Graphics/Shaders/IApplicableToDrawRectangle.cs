// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace NekoPlayer.App.Graphics.Shaders
{
    public interface IApplicableToDrawRectangle
    {
        RectangleF ComputeDrawRectangle(RectangleF originDrawRectangle);
    }
}
