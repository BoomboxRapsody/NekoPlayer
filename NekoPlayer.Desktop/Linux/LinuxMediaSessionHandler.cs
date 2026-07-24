// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using Tmds.DBus;
using NekoPlayer.App;
using NekoPlayer.App.Online;

namespace NekoPlayer.Desktop.Linux
{
    public partial class LinuxMediaSessionHandler : MediaSession
    {
        private Connection dbusConnection;
        private MprisPlayer mprisPlayer;

#nullable enable
        private MediaSessionControls? controls;
#nullable disable

        public override void CreateMediaSession(YouTubeAPI youtubeAPI, string audioPath)
        {
            Task.Run(async () =>
            {
                try
                {
                    mprisPlayer = new MprisPlayer(this);

                    // Direct Connection으로 D-Bus 연결
                    dbusConnection = new Connection(Address.Session);
                    await dbusConnection.ConnectAsync();

                    await dbusConnection.RegisterObjectAsync(mprisPlayer);
                    await dbusConnection.RegisterServiceAsync("org.mpris.MediaPlayer2.NekoPlayer");

                    IsLoaded = true;
                    base.YouTubeAPI = youtubeAPI;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, ex.GetDescription());
                }
            });
        }

        public override void UpdateMediaSession(Video video)
        {
            Task.Run(async () =>
            {
                if (!IsLoaded || mprisPlayer == null) return;

                try
                {
                    string title = YouTubeAPI.GetLocalizedVideoTitle(video);
                    string artist = YouTubeAPI.GetLocalizedChannelTitle(YouTubeAPI.GetChannel(video.Snippet.ChannelId));
                    string artUrl = video.Snippet.Thumbnails.High.Url;
                    long durationMicroseconds = (long)(XmlConvert.ToTimeSpan(video.ContentDetails.Duration).TotalMilliseconds * 1000);

                    mprisPlayer.SetMetadata(title, artist, artUrl, durationMicroseconds);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, ex.GetDescription());
                }
            });
        }

        public override void UpdatePlayingState(bool playing)
        {
            Task.Run(async () =>
            {
                if (!IsLoaded || mprisPlayer == null) return;
                mprisPlayer.SetPlaybackStatus(playing ? "Playing" : "Paused");
            });
        }

        public override void UpdateTimestamp(Video video, double pos)
        {
            try
            {
                if (IsLoaded && mprisPlayer != null)
                {
                    mprisPlayer.Position = (long)(pos * 1000);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.GetDescription());
            }
        }

        public override void UpdatePlaybackSpeed(double speed)
        {
            if (!IsLoaded || mprisPlayer == null) return;
            mprisPlayer.Rate = speed;
        }

        public override void DeleteMediaSession()
        {
            IsLoaded = false;
            mprisPlayer = null;
            dbusConnection?.Dispose();
            dbusConnection = null;
        }

        public override void RegisterControlEvents(MediaSessionControls controls)
        {
            this.controls = controls;
        }

        public override void UnregisterControlEvents()
        {
            controls = null;
        }

        #region MPRIS D-Bus Interfaces & Implementation

        // GNOME / KDE 등 데스크톱 환경 필수 속성 조회 인터페이스
        [DBusInterface("org.freedesktop.DBus.Properties")]
        public interface IProperties : IDBusObject
        {
            Task<object> GetAsync(string interfaceName, string propertyName);
            Task<IDictionary<string, object>> GetAllAsync(string interfaceName);
            Task SetAsync(string interfaceName, string propertyName, object value);
            Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
        }

        [DBusInterface("org.mpris.MediaPlayer2")]
        public interface IMediaPlayer2 : IDBusObject
        {
            Task RaiseAsync();
            Task QuitAsync();
            Task<bool> GetCanQuitAsync();
            Task<bool> GetCanRaiseAsync();
            Task<string> GetIdentityAsync();
            Task<string> GetDesktopEntryAsync();
        }

        [DBusInterface("org.mpris.MediaPlayer2.Player")]
        public interface IPlayer : IDBusObject
        {
            Task NextAsync();
            Task PreviousAsync();
            Task PauseAsync();
            Task PlayPauseAsync();
            Task StopAsync();
            Task PlayAsync();
            Task SeekAsync(long offset);
            Task SetPositionAsync(ObjectPath trackId, long position);
            Task OpenUriAsync(string uri);

            Task<string> GetPlaybackStatusAsync();
            Task<IDictionary<string, object>> GetMetadataAsync();
            Task<double> GetVolumeAsync();
            Task SetVolumeAsync(double volume);
            Task<long> GetPositionAsync();
            Task<double> GetRateAsync();
            Task<bool> GetCanControlAsync();
            Task<bool> GetCanPlayAsync();
            Task<bool> GetCanPauseAsync();
            Task<bool> GetCanGoNextAsync();
            Task<bool> GetCanGoPreviousAsync();
            Task<bool> GetCanSeekAsync();
        }

        public class MprisPlayer : IMediaPlayer2, IPlayer, IProperties
        {
            private readonly LinuxMediaSessionHandler handler;
            private readonly Dictionary<string, object> metadata = new();

            public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

            public event Action<PropertyChanges> OnPropertiesChanged;

            public string PlaybackStatus { get; private set; } = "Paused";
            public long Position { get; set; }
            public double Rate { get; set; } = 1.0;

            public MprisPlayer(LinuxMediaSessionHandler handler)
            {
                this.handler = handler;

                // GNOME/KDE 상단 패널 인식용 초기 메타데이터 세팅
                metadata["mpris:trackid"] = new ObjectPath("/org/mpris/MediaPlayer2/Track/1");
                metadata["xesam:title"] = "NekoPlayer";
                metadata["xesam:artist"] = new string[] { "NekoPlayer" };
            }

            public void SetPlaybackStatus(string status)
            {
                if (PlaybackStatus != status)
                {
                    PlaybackStatus = status;
                    OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("PlaybackStatus", PlaybackStatus));
                }
            }

            public void SetMetadata(string title, string artist, string artUrl, long lengthMicroseconds)
            {
                metadata["mpris:trackid"] = new ObjectPath("/org/mpris/MediaPlayer2/Track/1");
                metadata["mpris:length"] = lengthMicroseconds;
                metadata["xesam:title"] = string.IsNullOrEmpty(title) ? "Unknown Title" : title;
                metadata["xesam:artist"] = new string[] { string.IsNullOrEmpty(artist) ? "Unknown Artist" : artist };

                if (!string.IsNullOrEmpty(artUrl))
                    metadata["mpris:artUrl"] = artUrl;

                OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("Metadata", metadata));
            }

            // --- IProperties 구현 (리눅스 OS 상단 제어바 연동의 핵심) ---
            public Task<object> GetAsync(string interfaceName, string propertyName)
            {
                var props = GetAllInternal(interfaceName);
                if (props.TryGetValue(propertyName, out var value))
                    return Task.FromResult(value);

                throw new DBusException("org.freedesktop.DBus.Error.UnknownProperty", $"Property '{propertyName}' not found.");
            }

            public Task<IDictionary<string, object>> GetAllAsync(string interfaceName)
            {
                return Task.FromResult(GetAllInternal(interfaceName));
            }

            private IDictionary<string, object> GetAllInternal(string interfaceName)
            {
                if (interfaceName == "org.mpris.MediaPlayer2")
                {
                    return new Dictionary<string, object>
                    {
                        { "CanQuit", false },
                        { "CanRaise", false },
                        { "HasTrackList", false },
                        { "Identity", "NekoPlayer" },
                        { "DesktopEntry", "" }, // 빈 문자열이어야 OS가 존재하지 않는 .desktop 파일 탐색을 안 함
                        { "SupportedUriSchemes", new string[0] },
                        { "SupportedMimeTypes", new string[0] }
                    };
                }

                if (interfaceName == "org.mpris.MediaPlayer2.Player")
                {
                    return new Dictionary<string, object>
                    {
                        { "PlaybackStatus", PlaybackStatus },
                        { "Rate", Rate },
                        { "Metadata", metadata },
                        { "Volume", 1.0 },
                        { "Position", Position },
                        { "MinimumRate", 1.0 },
                        { "MaximumRate", 1.0 },
                        { "CanControl", true },
                        { "CanPlay", true },
                        { "CanPause", true },
                        { "CanGoNext", true },
                        { "CanGoPrevious", true },
                        { "CanSeek", true }
                    };
                }

                return new Dictionary<string, object>();
            }

            public Task SetAsync(string interfaceName, string propertyName, object value) => Task.CompletedTask;

            public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
            {
                return Task.FromResult<IDisposable>(new ActionDisposable(() => { }));
            }

            private class ActionDisposable : IDisposable
            {
                private readonly Action action;
                public ActionDisposable(Action action) => this.action = action;
                public void Dispose() => action?.Invoke();
            }

            // --- IMediaPlayer2 ---
            public Task RaiseAsync() => Task.CompletedTask;
            public Task QuitAsync() => Task.CompletedTask;
            public Task<bool> GetCanQuitAsync() => Task.FromResult(false);
            public Task<bool> GetCanRaiseAsync() => Task.FromResult(false);
            public Task<string> GetIdentityAsync() => Task.FromResult("NekoPlayer");
            public Task<string> GetDesktopEntryAsync() => Task.FromResult("");

            // --- IPlayer Controls ---
            public Task PlayAsync()
            {
                handler.controls?.PlayButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task PauseAsync()
            {
                handler.controls?.PauseButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task PlayPauseAsync()
            {
                if (PlaybackStatus == "Playing")
                    handler.controls?.PauseButtonPressed?.Invoke();
                else
                    handler.controls?.PlayButtonPressed?.Invoke();

                return Task.CompletedTask;
            }

            public Task StopAsync()
            {
                handler.controls?.PauseButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task NextAsync()
            {
                handler.controls?.NextButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task PreviousAsync()
            {
                handler.controls?.PrevButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task SeekAsync(long offset)
            {
                double newPosMs = (Position + offset) / 1000.0;
                handler.controls?.OnSeek?.Invoke(newPosMs);
                return Task.CompletedTask;
            }

            public Task SetPositionAsync(ObjectPath trackId, long position)
            {
                double posMs = position / 1000.0;
                handler.controls?.OnSeek?.Invoke(posMs);
                return Task.CompletedTask;
            }

            public Task OpenUriAsync(string uri) => Task.CompletedTask;

            // --- Getters ---
            public Task<string> GetPlaybackStatusAsync() => Task.FromResult(PlaybackStatus);
            public Task<IDictionary<string, object>> GetMetadataAsync() => Task.FromResult<IDictionary<string, object>>(metadata);
            public Task<double> GetVolumeAsync() => Task.FromResult(1.0);
            public Task SetVolumeAsync(double volume) => Task.CompletedTask;
            public Task<long> GetPositionAsync() => Task.FromResult(Position);
            public Task<double> GetRateAsync() => Task.FromResult(Rate);

            public Task<bool> GetCanControlAsync() => Task.FromResult(true);
            public Task<bool> GetCanPlayAsync() => Task.FromResult(true);
            public Task<bool> GetCanPauseAsync() => Task.FromResult(true);
            public Task<bool> GetCanGoNextAsync() => Task.FromResult(true);
            public Task<bool> GetCanGoPreviousAsync() => Task.FromResult(true);
            public Task<bool> GetCanSeekAsync() => Task.FromResult(true);
        }

        #endregion
    }
}
