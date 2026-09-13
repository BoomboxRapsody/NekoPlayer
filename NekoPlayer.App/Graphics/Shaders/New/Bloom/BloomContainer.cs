// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace NekoPlayer.App.Graphics.Shaders.New.Bloom
{
    public partial class BloomContainer : VideoNewShaderContainer
    {
        protected override string FragmentShader => "Blur";
        public override ShaderType Type => ShaderType.Bloom;

        public BloomContainer()
        {
            DrawOriginal = true;
            EffectBlending = BlendingParameters.Additive;
            EffectPlacement = EffectPlacement.InFront;
        }

        protected override DrawNode CreateShaderDrawNode() => new BloomContainerDrawNode(this, SharedData);
    }
}
