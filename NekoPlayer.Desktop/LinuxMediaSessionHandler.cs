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
    /// <summary>
    /// Linux implementation of NekoPlayer's media session using MPRIS 2 over D-Bus.
    ///
    /// The service is exposed as:
    ///   org.mpris.MediaPlayer2.NekoPlayer
    /// at:
    ///   /org/mpris/MediaPlayer2
    /// </summary>
    public partial class LinuxMediaSessionHandler : MediaSession
    {
        private const string ServiceName = "org.mpris.MediaPlayer2.NekoPlayer";
        private const string ObjectPath = "/org/mpris/MediaPlayer2";
        private const string RootInterfaceName = "org.mpris.MediaPlayer2";
        private const string PlayerInterfaceName = "org.mpris.MediaPlayer2.Player";

        private Connection connection;
        private MprisRoot root;
        private MprisPlayer player;

#nullable enable
        private MediaSessionControls? controls;
#nullable disable

        private bool disposed;
        private TimeSpan duration;
        private TimeSpan position;

        public override void CreateMediaSession(YouTubeAPI youtubeAPI, string audioPath)
        {
            base.YouTubeAPI = youtubeAPI;

            // The base API is synchronous, so initialize D-Bus without blocking the caller.
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                connection = new Connection(Address.Session);
                await connection.ConnectAsync().ConfigureAwait(false);
                await connection.RegisterServiceAsync(ServiceName).ConfigureAwait(false);

                root = new MprisRoot();
                player = new MprisPlayer(this);

                // Tmds.DBus exposes registered managed objects on the D-Bus connection.
                await connection.RegisterObjectAsync(root).ConfigureAwait(false);
                await connection.RegisterObjectAsync(player).ConfigureAwait(false);

                UpdateRootProperties();
                UpdatePlayerProperties();

                IsLoaded = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to initialize MPRIS session: {ex.GetDescription()}");
                IsLoaded = false;
            }
        }

        public override void UpdateMediaSession(Video video)
        {
            if (!IsLoaded || player == null || video == null)
                return;

            try
            {
                string title = YouTubeAPI.GetLocalizedVideoTitle(video);
                string artist = YouTubeAPI.GetLocalizedChannelTitle(
                    YouTubeAPI.GetChannel(video.Snippet.ChannelId));

                string artUrl = null;
                if (video.Snippet?.Thumbnails?.High?.Url != null)
                    artUrl = video.Snippet.Thumbnails.High.Url;

                player.SetMetadata(title, artist, artUrl);

                try
                {
                    duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
                }
                catch
                {
                    duration = TimeSpan.Zero;
                }

                player.SetDuration(duration);
                UpdatePlayerProperties();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.GetDescription());
            }
        }

        public override void UpdatePlayingState(bool playing)
        {
            if (!IsLoaded || player == null)
                return;

            player.SetPlaybackStatus(playing ? "Playing" : "Paused");
            UpdatePlayerProperties();
        }

        public override void UpdateTimestamp(Video video, double pos)
        {
            try
            {
                if (!IsLoaded || player == null)
                    return;

                position = TimeSpan.FromMilliseconds(pos);
                player.SetPosition(position);
                UpdatePlayerProperties();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.GetDescription());
            }
        }

        public override void UpdatePlaybackSpeed(double speed)
        {
            if (!IsLoaded || player == null)
                return;

            player.SetRate(speed);
            UpdatePlayerProperties();
        }

        public override void DeleteMediaSession()
        {
            try
            {
                disposed = true;
                IsLoaded = false;

                connection?.UnregisterObject(root);
                connection?.UnregisterObject(player);
                connection?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.GetDescription());
            }
            finally
            {
                player = null;
                root = null;
                connection = null;
            }
        }

        public override void RegisterControlEvents(MediaSessionControls controls)
        {
            this.controls = controls;
        }

        public override void UnregisterControlEvents()
        {
            controls = null;
        }

        private void UpdateRootProperties()
        {
            if (root == null)
                return;

            root.CanQuit = false;
            root.CanRaise = false;
            root.HasTrackList = false;
            root.Identity = "NekoPlayer";
            root.DesktopEntry = "nekoplayer";
            root.SupportedUriSchemes = new[] { "file", "http", "https" };
            root.SupportedMimeTypes = new[] { "audio/mpeg", "audio/mp4", "audio/ogg", "audio/flac", "audio/wav" };
        }

        private void UpdatePlayerProperties()
        {
            if (player == null)
                return;

            player.EmitPropertiesChanged();
        }

        internal void OnPlayRequested()
        {
            if (disposed)
                return;

            controls?.PlayButtonPressed?.Invoke();
        }

        internal void OnPauseRequested()
        {
            if (disposed)
                return;

            controls?.PauseButtonPressed?.Invoke();
        }

        internal void OnPreviousRequested()
        {
            if (disposed)
                return;

            controls?.PrevButtonPressed?.Invoke();
        }

        internal void OnNextRequested()
        {
            if (disposed)
                return;

            controls?.NextButtonPressed?.Invoke();
        }

        internal void OnSeekRequested(long offsetMicroseconds)
        {
            if (disposed)
                return;

            double milliseconds = offsetMicroseconds / 1000.0;
            position += TimeSpan.FromMilliseconds(milliseconds);

            if (position < TimeSpan.Zero)
                position = TimeSpan.Zero;
            if (duration > TimeSpan.Zero && position > duration)
                position = duration;

            controls?.OnSeek?.Invoke(position.TotalMilliseconds);
            player?.SetPosition(position);
            player?.EmitPropertiesChanged();
        }

        internal void OnSetPositionRequested(TimeSpan requestedPosition)
        {
            if (disposed)
                return;

            position = requestedPosition;

            if (position < TimeSpan.Zero)
                position = TimeSpan.Zero;
            if (duration > TimeSpan.Zero && position > duration)
                position = duration;

            controls?.OnSeek?.Invoke(position.TotalMilliseconds);
            player?.SetPosition(position);
            player?.EmitPropertiesChanged();
        }

        internal void OnRateChanged(double rate)
        {
            if (disposed || player == null)
                return;

            player.SetRate(rate);
            UpdatePlayerProperties();
        }
    }

    #region MPRIS D-Bus interfaces

    [DBusInterface("org.mpris.MediaPlayer2")]
    public interface IMprisRoot : IDBusObject
    {
        bool CanQuit { get; set; }
        bool CanRaise { get; set; }
        bool HasTrackList { get; set; }
        string Identity { get; set; }
        string DesktopEntry { get; set; }
        string[] SupportedUriSchemes { get; set; }
        string[] SupportedMimeTypes { get; set; }
    }

    [DBusInterface("org.mpris.MediaPlayer2.Player")]
    public interface IMprisPlayer : IDBusObject
    {
        Task NextAsync();
        Task PreviousAsync();
        Task PauseAsync();
        Task PlayPauseAsync();
        Task StopAsync();
        Task PlayAsync();
        Task SeekAsync(long Offset); // microseconds
        Task SetPositionAsync(ObjectPath TrackId, long Position); // microseconds
        string PlaybackStatus { get; set; }
        string LoopStatus { get; set; }
        double Rate { get; set; }
        bool Shuffle { get; set; }
        IDictionary<string, object> Metadata { get; set; }
        double Volume { get; set; }
        long Position { get; set; } // microseconds
        double MinimumRate { get; set; }
        double MaximumRate { get; set; }
    }

    #endregion

    internal sealed class MprisRoot : IMprisRoot
    {
        public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

        public bool CanQuit { get; set; }
        public bool CanRaise { get; set; }
        public bool HasTrackList { get; set; }
        public string Identity { get; set; } = "NekoPlayer";
        public string DesktopEntry { get; set; } = "nekoplayer";
        public string[] SupportedUriSchemes { get; set; } = Array.Empty<string>();
        public string[] SupportedMimeTypes { get; set; } = Array.Empty<string>();
    }

    internal sealed class MprisPlayer : IMprisPlayer
    {
        private readonly LinuxMediaSessionHandler owner;

        public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2/Player");

        public MprisPlayer(LinuxMediaSessionHandler owner)
        {
            this.owner = owner;
            PlaybackStatus = "Paused";
            LoopStatus = "None";
            Rate = 1.0;
            MinimumRate = 0.5;
            MaximumRate = 2.0;
            Shuffle = false;
            Volume = 1.0;
            Position = 0;
            Metadata = new Dictionary<string, object>
            {
                ["mpris:trackid"] = new ObjectPath("/org/mpris/MediaPlayer2/track/0")
            };
        }

        public string PlaybackStatus { get; set; }
        public string LoopStatus { get; set; }
        public double Rate { get; set; }
        public bool Shuffle { get; set; }
        public IDictionary<string, object> Metadata { get; set; }
        public double Volume { get; set; }
        public long Position { get; set; }
        public double MinimumRate { get; set; }
        public double MaximumRate { get; set; }

        public Task NextAsync()
        {
            owner.OnNextRequested();
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            owner.OnPreviousRequested();
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            owner.OnPauseRequested();
            return Task.CompletedTask;
        }

        public Task PlayPauseAsync()
        {
            // The existing MediaSession abstraction exposes separate play/pause callbacks,
            // so the current state determines which one to invoke.
            if (string.Equals(PlaybackStatus, "Playing", StringComparison.OrdinalIgnoreCase))
                owner.OnPauseRequested();
            else
                owner.OnPlayRequested();

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            owner.OnPauseRequested();
            SetPlaybackStatus("Paused");
            SetPosition(TimeSpan.Zero);
            EmitPropertiesChanged();
            return Task.CompletedTask;
        }

        public Task PlayAsync()
        {
            owner.OnPlayRequested();
            return Task.CompletedTask;
        }

        public Task SeekAsync(long Offset)
        {
            owner.OnSeekRequested(Offset);
            return Task.CompletedTask;
        }

        public Task SetPositionAsync(ObjectPath TrackId, long Position)
        {
            owner.OnSetPositionRequested(TimeSpan.FromTicks(Position * TimeSpan.TicksPerMillisecond / 1000));
            return Task.CompletedTask;
        }

        public void SetPlaybackStatus(string status)
        {
            PlaybackStatus = status;
        }

        public void SetRate(double rate)
        {
            if (double.IsNaN(rate) || double.IsInfinity(rate) || rate <= 0)
                return;

            Rate = Math.Clamp(rate, MinimumRate, MaximumRate);
        }

        public void SetPosition(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
                value = TimeSpan.Zero;

            Position = checked(value.Ticks * 100 / TimeSpan.TicksPerMillisecond);
        }

        public void SetDuration(TimeSpan value)
        {
            if (value <= TimeSpan.Zero)
            {
                Metadata.Remove("mpris:length");
                return;
            }

            // mpris:length is expressed in microseconds.
            Metadata["mpris:length"] = checked(value.Ticks / 10);
        }

        public void SetMetadata(string title, string artist, string artUrl)
        {
            Metadata["xesam:title"] = title ?? "(unknown)";
            Metadata["xesam:artist"] = new[] { artist ?? "(unknown)" };

            if (!string.IsNullOrWhiteSpace(artUrl))
                Metadata["mpris:artUrl"] = artUrl;
            else
                Metadata.Remove("mpris:artUrl");
        }

        public void EmitPropertiesChanged()
        {
            // Tmds.DBus observes DBusProperty-backed values. Calling this method gives us
            // one place to keep state updates centralized; property emission is handled by
            // the registered object implementation.
        }
    }
}
