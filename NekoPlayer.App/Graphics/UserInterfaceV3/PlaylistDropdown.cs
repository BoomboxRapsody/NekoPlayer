// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Google.Apis.YouTube.v3.Data;
using NekoPlayer.App.Graphics.UserInterfaceV2;
using osu.Framework.Localisation;

namespace NekoPlayer.App.Graphics.UserInterfaceV3
{
    public partial class PlaylistDropdown : FormDropdown<Playlist>
    {
        protected override LocalisableString GenerateItemText(Playlist item)
            => item.Snippet.Title;
    }
}
