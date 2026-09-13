// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;

namespace NekoPlayer.App.Online
{
    public class NekoPlayerJsonWebRequest<T> : JsonWebRequest<T>
    {
        public NekoPlayerJsonWebRequest(string uri)
            : base(uri)
        {
        }

        public NekoPlayerJsonWebRequest()
        {
        }

        protected override string UserAgent => "NekoPlayer";
    }
}
