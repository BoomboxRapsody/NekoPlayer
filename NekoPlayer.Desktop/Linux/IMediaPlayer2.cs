// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace NekoPlayer.Desktop.Linux
{
    [DBusInterface("org.mpris.MediaPlayer2")]
    public interface IMediaPlayer2 : IDBusObject
    {
        Task QuitAsync();
        Task RaiseAsync();
        Task<T> GetAsync<T>(string prop);
        Task<IDictionary<string, object>> GetAllAsync();
    }
}
