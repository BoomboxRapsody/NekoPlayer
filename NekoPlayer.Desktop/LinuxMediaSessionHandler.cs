// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using NekoPlayer.App;
using NekoPlayer.App.Online;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using Tmds.DBus;

namespace NekoPlayer.Desktop
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

                    // D-Bus 세션 버스 연결 및 객체/서비스 등록
                    dbusConnection = new Connection(Address.Session!);
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
                mprisPlayer.PlaybackStatus = playing ? "Playing" : "Paused";
            });
        }

        public override void UpdateTimestamp(Video video, double pos)
        {
            try
            {
                if (IsLoaded && mprisPlayer != null)
                {
                    // pos는 ms 단위 -> MPRIS는 마이크로초(microseconds) 단위 사용
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

        #region MPRIS D-Bus Implementation

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
            Task SetPositionAsync(Tmds.DBus.ObjectPath trackId, long position);
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

        public class MprisPlayer : IMediaPlayer2, IPlayer
        {
            private readonly LinuxMediaSessionHandler handler;
            private readonly Dictionary<string, object> metadata = new();

            public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

            // D-Bus 속성 변경 알림 시그널 이벤트
            public event Action<PropertyChanges> OnPropertiesChanged;

            private string playbackStatus = "Paused";
            public string PlaybackStatus
            {
                get => playbackStatus;
                set
                {
                    if (playbackStatus != value)
                    {
                        playbackStatus = value;
                        // OS 알림 센터에 재생/일시정지 상태 변경 전파
                        OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("PlaybackStatus", playbackStatus));
                    }
                }
            }

            public long Position { get; set; }
            public double Rate { get; set; } = 1.0;

            public MprisPlayer(LinuxMediaSessionHandler handler)
            {
                this.handler = handler;

                // 1. 초기 기본 메타데이터 세팅 (OS가 플레이어로 인지하도록 최소 정보 설정)
                metadata["mpris:trackid"] = new ObjectPath("/org/mpris/Null");
                metadata["xesam:title"] = "NekoPlayer";
                metadata["xesam:artist"] = new string[] { "NekoPlayer" };
            }

            public void SetMetadata(string title, string artist, string artUrl, long lengthMicroseconds)
            {
                metadata["mpris:trackid"] = new ObjectPath("/org/mpris/MediaPlayer2/Track/1");
                metadata["mpris:length"] = lengthMicroseconds;
                metadata["xesam:title"] = string.IsNullOrEmpty(title) ? "Unknown" : title;
                metadata["xesam:artist"] = new string[] { string.IsNullOrEmpty(artist) ? "Unknown" : artist };

                if (!string.IsNullOrEmpty(artUrl))
                    metadata["mpris:artUrl"] = artUrl;

                // 2. OS 알림 센터에 메타데이터(곡 정보/앨범아트) 변경 전파
                OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("Metadata", metadata));
            }

            // .desktop 파일이 없는 경우 빈 문자열을 반환해야 OS가 유효하지 않은 앱으로 판단하지 않습니다.
            public Task<string> GetDesktopEntryAsync() => Task.FromResult(string.Empty);

            // IMediaPlayer2 기본 구현
            public Task RaiseAsync() => Task.CompletedTask;
            public Task QuitAsync() => Task.CompletedTask;
            public Task<bool> GetCanQuitAsync() => Task.FromResult(false);
            public Task<bool> GetCanRaiseAsync() => Task.FromResult(false);
            public Task<string> GetIdentityAsync() => Task.FromResult("NekoPlayer");

            // IPlayer 제어 메서드
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

            // Property Getters
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
