// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Screens;

namespace NekoPlayer.App.Screens
{
    public interface INekoPlayerScreen : IScreen
    {
        /// <summary>
        /// Whether this <see cref="NekoPlayerScreen"/> allows the cursor to be displayed.
        /// </summary>
        bool CursorVisible { get; }
    }
}
