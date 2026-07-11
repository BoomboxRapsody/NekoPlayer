// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Tests.Sprites;
using osu.Framework.Allocation;

namespace NekoPlayer.App.Tests.Visual
{
    public partial class ErpinSkin3 : NekoPlayerTestScene
    {
        private ErpinSkin3Sprite sprite;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(sprite = new ErpinSkin3Sprite { Anchor = osu.Framework.Graphics.Anchor.Centre, Origin = osu.Framework.Graphics.Anchor.Centre });
        }
    }
}
