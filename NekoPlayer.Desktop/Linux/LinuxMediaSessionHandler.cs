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
        private Connection connection;
        private MprisPlayer mprisPlayer;

        private string playbackStatus = "Paused";
        private double positionSeconds;
        private double durationSeconds;
        private double playbackRate = 1.0;
        private string currentTitle = "(unknown)";
        private string currentArtist = "(unknown)";
        private string currentArtUrl;

#nullable enable
        private MediaSessionControls? controls;
#nullable disable

        // MPRIS requires a trackid path; NekoPlayer only ever has one "current track"
        // so we just bump a counter whenever UpdateMediaSession is called.
        private int trackId;

        public override void CreateMediaSession(YouTubeAPI youtubeAPI, string audioPath)
        {
            Task.Run(async () =>
            {
                try
                {
                    connection = new Connection(Address.Session);
                    await connection.ConnectAsync();

                    mprisPlayer = new MprisPlayer(this);

                    await connection.RegisterObjectAsync(mprisPlayer);
                    await connection.RegisterServiceAsync("org.mpris.MediaPlayer2.nekoplayer");

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
                currentTitle = YouTubeAPI.GetLocalizedVideoTitle(video);
                currentArtist = YouTubeAPI.GetLocalizedChannelTitle(YouTubeAPI.GetChannel(video.Snippet.ChannelId));
                currentArtUrl = video.Snippet.Thumbnails.High.Url;
                durationSeconds = XmlConvert.ToTimeSpan(video.ContentDetails.Duration).TotalSeconds;
                trackId++;

                await mprisPlayer.NotifyMetadataChangedAsync();
            });
        }

        public override void UpdatePlayingState(bool playing)
        {
            Task.Run(async () =>
            {
                playbackStatus = playing ? "Playing" : "Paused";

                await mprisPlayer.NotifyPlaybackStatusChangedAsync();
            });
        }

        public override void UpdateTimestamp(Video video, double pos)
        {
            try
            {
                if (IsLoaded)
                {
                    positionSeconds = pos * 0.001d;

                    // MPRIS doesn't push position updates via PropertiesChanged (clients poll
                    // GetPosition / the Position property instead), but we do need to emit
                    // Seeked whenever the position jumps discontinuously (e.g. YouTube video change).
                    mprisPlayer.EmitSeeked((long)(positionSeconds * 1_000_000));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.GetDescription());
            }
        }

        public override void DeleteMediaSession()
        {
            mprisPlayer?.Dispose();
            connection?.Dispose();
            connection = null;
            mprisPlayer = null;
            IsLoaded = false;
        }

        public override void RegisterControlEvents(MediaSessionControls controls)
        {
            this.controls = controls;
        }

        public override void UnregisterControlEvents()
        {
            controls = null;
        }

        public override void UpdatePlaybackSpeed(double speed)
        {
            playbackRate = speed;
            Task.Run(() => mprisPlayer.NotifyPlaybackRateChangedAsync());
        }

        // ------------------------------------------------------------------
        // MPRIS2 D-Bus object. Kept nested since it only ever wraps the
        // state living on the outer handler above (mirrors the way the
        // Windows version keeps everything on one class via SMTC events).
        // ------------------------------------------------------------------
        private sealed class MprisPlayer : IMediaPlayer2, IMediaPlayer2Player, IDisposable
        {
            private readonly LinuxMediaSessionHandler owner;

            public MprisPlayer(LinuxMediaSessionHandler owner)
            {
                this.owner = owner;
            }

            public ObjectPath ObjectPath { get; } = new ObjectPath("/org/mpris/MediaPlayer2");

            // ---- org.mpris.MediaPlayer2 ----

            public Task<bool> GetCanQuitAsync() => Task.FromResult(false);
            public Task<bool> GetCanRaiseAsync() => Task.FromResult(false);
            public Task<string> GetIdentityAsync() => Task.FromResult("NekoPlayer");
            public Task<string[]> GetSupportedUriSchemesAsync() => Task.FromResult(new[] { "file", "https" });
            public Task<string[]> GetSupportedMimeTypesAsync() => Task.FromResult(new[] { "audio/mpeg", "audio/webm" });

            public Task QuitAsync() => Task.CompletedTask;
            public Task RaiseAsync() => Task.CompletedTask;

            // ---- org.mpris.MediaPlayer2.Player ----

            public Task<string> GetPlaybackStatusAsync() => Task.FromResult(owner.playbackStatus);
            public Task<double> GetRateAsync() => Task.FromResult(owner.playbackRate);
            public Task<double> GetVolumeAsync() => Task.FromResult(0d); // audio handled elsewhere, mirrors Windows Volume = 0
            public Task<long> GetPositionAsync() => Task.FromResult((long)(owner.positionSeconds * 1_000_000));

            public Task<IDictionary<string, object>> GetMetadataAsync()
            {
                IDictionary<string, object> metadata = new Dictionary<string, object>
                {
                    ["mpris:trackid"] = new ObjectPath($"/org/nekoplayer/track/{owner.trackId}"),
                    ["mpris:length"] = (long)(owner.durationSeconds * 1_000_000),
                    ["xesam:title"] = owner.currentTitle,
                    ["xesam:artist"] = new[] { owner.currentArtist },
                };

                if (!string.IsNullOrEmpty(owner.currentArtUrl))
                    metadata["mpris:artUrl"] = owner.currentArtUrl;

                return Task.FromResult(metadata);
            }

            // ---- org.freedesktop.DBus.Properties (required so PlaybackStatus/Metadata/etc. are readable) ----
            // Tmds.DBus's own convention (see its issue #62 and PropertyObject test) is: implement
            // GetAsync/GetAllAsync/SetAsync as plain methods, and WatchPropertiesAsync just wires the
            // handler onto a local event via SignalWatcher.AddAsync. There's no interface_name argument
            // in this simplified model, so both org.mpris.MediaPlayer2 and .Player interfaces route
            // through the same table below - all NekoPlayer properties are read-only, so it never needs SetAsync.

            public event Action<PropertyChanges> OnPropertiesChanged;

            public Task<T> GetAsync<T>(string prop)
            {
                object value = prop switch
                {
                    "CanQuit" => false,
                    "CanRaise" => false,
                    "Identity" => "NekoPlayer",
                    "PlaybackStatus" => owner.playbackStatus,
                    "Rate" => owner.playbackRate,
                    "Volume" => 0d,
                    "Position" => (long)(owner.positionSeconds * 1_000_000),
                    _ => throw new ArgumentException($"Unknown property {prop}"),
                };

                return Task.FromResult((T)value);
            }

            public async Task<IDictionary<string, object>> GetAllAsync()
            {
                var metadata = await GetMetadataAsync();

                return new Dictionary<string, object>
                {
                    ["PlaybackStatus"] = owner.playbackStatus,
                    ["Rate"] = owner.playbackRate,
                    ["Volume"] = 0d,
                    ["Position"] = (long)(owner.positionSeconds * 1_000_000),
                    ["Metadata"] = metadata,
                    ["CanPlay"] = true,
                    ["CanPause"] = true,
                    ["CanGoNext"] = true,
                    ["CanGoPrevious"] = true,
                    ["CanSeek"] = true,
                    ["CanControl"] = true,
                };
            }

            public Task SetAsync(string prop, object val) => Task.CompletedTask; // nothing settable from clients

            public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
                => SignalWatcher.AddAsync(this, nameof(OnPropertiesChanged), handler);

            public Task PlayAsync()
            {
                owner.controls?.PlayButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task PauseAsync()
            {
                owner.controls?.PauseButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task PlayPauseAsync()
            {
                if (owner.playbackStatus == "Playing")
                    owner.controls?.PauseButtonPressed?.Invoke();
                else
                    owner.controls?.PlayButtonPressed?.Invoke();

                return Task.CompletedTask;
            }

            public Task StopAsync()
            {
                owner.controls?.PauseButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task NextAsync()
            {
                owner.controls?.NextButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task PreviousAsync()
            {
                owner.controls?.PrevButtonPressed?.Invoke();
                return Task.CompletedTask;
            }

            public Task SeekAsync(long offsetMicroseconds)
            {
                double newPositionMs = (owner.positionSeconds * 1000d) + (offsetMicroseconds / 1000d);
                owner.controls?.OnSeek?.Invoke(newPositionMs);
                return Task.CompletedTask;
            }

            public Task SetPositionAsync(ObjectPath trackId, long positionMicroseconds)
            {
                owner.controls?.OnSeek?.Invoke(positionMicroseconds / 1000d);
                return Task.CompletedTask;
            }

            public Task OpenUriAsync(string uri) => Task.CompletedTask;

            // ---- change notification plumbing ----

            public Task<IDisposable> WatchSeekedAsync(Action<long> handler)
                => SignalWatcher.AddAsync(this, nameof(Seeked), handler);

            public event Action<long> Seeked;

            public void EmitSeeked(long positionMicroseconds) => Seeked?.Invoke(positionMicroseconds);

            public Task NotifyPlaybackStatusChangedAsync()
            {
                OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("PlaybackStatus", owner.playbackStatus));
                return Task.CompletedTask;
            }

            public async Task NotifyMetadataChangedAsync()
            {
                var metadata = await GetMetadataAsync();
                OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("Metadata", metadata));
            }

            public Task NotifyPlaybackRateChangedAsync()
            {
                OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("Rate", owner.playbackRate));
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        // NOTE: IMediaPlayer2 / IMediaPlayer2Player interface declarations
        // (with [DBusInterface] attributes) go in a separate file, e.g.
        // IMediaPlayer2.cs, same as in the earlier example.
    }
}
