// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;

namespace NekoPlayer.App.Graphics.Shaders.New.Chromatic
{
    public partial class ChromaticContainer : VideoNewShaderContainer
    {
        protected override string FragmentShader => "ChromaticAberration";
        public override ShaderType Type => ShaderType.Chromatic;
        protected override DrawNode CreateShaderDrawNode() => new ChromaticContainerDrawNode(this, SharedData);
    }
}
