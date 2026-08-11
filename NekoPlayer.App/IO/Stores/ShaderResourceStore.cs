// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using osu.Framework.IO.Stores;
using NekoPlayer.App.Graphics.Sprites;

namespace NekoPlayer.App.IO.Stores
{
    public class ShaderResourceStore : DllResourceStore
    {
        public ShaderResourceStore()
            : base(typeof(ProjectYomiSpriteText).Assembly)
        {
            var property = typeof(DllResourceStore).GetField("prefix", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new NotSupportedException("Underlying implementation has changed, KaraokeFont needs an update");
            property.SetValue(this, "YouTubePlayerEX");
        }
    }
}
