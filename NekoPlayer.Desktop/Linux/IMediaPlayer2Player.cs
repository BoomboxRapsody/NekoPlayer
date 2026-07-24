// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace NekoPlayer.Desktop.Linux
{
    [DBusInterface("org.mpris.MediaPlayer2.Player")]
    public interface IMediaPlayer2Player : IDBusObject
    {
        Task PlayAsync();
        Task PauseAsync();
        Task PlayPauseAsync();
        Task NextAsync();
        Task PreviousAsync();
        Task StopAsync();
        Task SeekAsync(long offsetMicroseconds);

        Task<T> GetAsync<T>(string prop);
        Task<IDictionary<string, object>> GetAllAsync();
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }
}
