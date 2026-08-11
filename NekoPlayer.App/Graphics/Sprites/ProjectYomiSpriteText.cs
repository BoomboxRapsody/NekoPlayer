// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;

namespace NekoPlayer.App.Graphics.Sprites
{
    public partial class ProjectYomiSpriteText : SpriteText
    {
        [Obsolete("Use TruncatingSpriteText instead.")]
        public new bool Truncate
        {
            set => throw new InvalidOperationException($"Use {nameof(TruncatingSpriteText)} instead.");
        }

        public ProjectYomiSpriteText(bool enableShadow = true)
        {
            Font = NekoPlayerApp.DefaultFont;
            Shadow = enableShadow;
        }
    }
}
