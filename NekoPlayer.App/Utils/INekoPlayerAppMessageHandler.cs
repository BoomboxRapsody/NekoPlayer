// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace NekoPlayer.App.Utils
{
    public interface INekoPlayerAppMessageHandler
    {
        void OpenSettings();
        void TogglePreservePitch();
        void SelectPlaylist(string id);
        void OpenMyPlaylists();
    }
}
