// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DiscordRPC;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Humanizer;
using NekoPlayer.App.Audio;
using NekoPlayer.App.Config;
using NekoPlayer.App.Extensions;
using NekoPlayer.App.Graphics;
using NekoPlayer.App.Graphics.Containers;
using NekoPlayer.App.Graphics.Shaders;
using NekoPlayer.App.Graphics.Sprites;
using NekoPlayer.App.Graphics.UserInterface;
using NekoPlayer.App.Graphics.UserInterfaceV2;
using NekoPlayer.App.Graphics.UserInterfaceV3;
using NekoPlayer.App.Graphics.Videos;
using NekoPlayer.App.Input;
using NekoPlayer.App.Input.Binding;
using NekoPlayer.App.Localisation;
using NekoPlayer.App.Online;
using NekoPlayer.App.Overlays;
using NekoPlayer.App.Overlays.Containers;
using NekoPlayer.App.Overlays.OSD;
using NekoPlayer.App.Overlays.Volume;
using NekoPlayer.App.Updater;
using NekoPlayer.App.Utils;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics.Video;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using PaletteNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.ClosedCaptions;
using YoutubeExplode.Videos.Streams;
using static Google.Apis.YouTube.v3.CommentThreadsResource.ListRequest;
using Container = osu.Framework.Graphics.Containers.Container;
using Language = NekoPlayer.App.Localisation.Language;
using OverlayContainer = NekoPlayer.App.Graphics.Containers.OverlayContainer;

namespace NekoPlayer.App.Screens
{
    public partial class MainAppView
    {
        private void fetchMyPlaylists()
        {
            foreach (var item in myPlaylistItemsView.Children)
            {
                Schedule(() => item.Expire());
            }

            if (!googleOAuth2.SignedIn.Value)
                return;

            Task.Run(async () =>
            {
                IList<Playlist> playlists = await api.GetMyPlaylistItemsAsync();

                Schedule(() =>
                {
                    myPlaylistsDropdown.Items = playlists;

                    if (playlists.Count > 0)
                        myPlaylistsDropdown.Current.Value = playlists[0];
                });

                foreach (Playlist playlist in playlists)
                {
                    MyPlaylistView playlistItemView = new MyPlaylistView()
                    {
                        RelativeSizeAxes = Axes.X,
                        Enabled = { Value = true },
                        ClickAction = async v =>
                        {
                            Schedule(async () =>
                            {
                                SetPlaylist(playlist.Id).FireAndForget();
                            });
                        },
                        OptionsClickEvent = async data =>
                        {
                            Schedule(async () =>
                            {
                                hideOverlays();

                                editPlaylistTitleBox.Current.Value = data.Snippet.Title;
                                switch (data.Status.PrivacyStatus)
                                {
                                    case "public":
                                        editPlaylistPrivacyStatusDropdown.Current.Value = PrivacyStatus.Public;
                                        break;
                                    case "unlisted":
                                        editPlaylistPrivacyStatusDropdown.Current.Value = PrivacyStatus.Unlisted;
                                        break;
                                    case "private":
                                        editPlaylistPrivacyStatusDropdown.Current.Value = PrivacyStatus.Private;
                                        break;
                                }

                                showOverlayContainer(editPlaylistOverlay);

                                updatePlaylistButton.Action = async () =>
                                {
                                    await Task.Run(async () =>
                                    {
                                        Schedule(async () =>
                                        {
                                            hideOverlays();
                                        });

                                        await api.UpdatePlaylistInfo(data.Id, editPlaylistTitleBox.Current.Value, editPlaylistPrivacyStatusDropdown.Current.Value);

                                        await Task.Delay(1000);

                                        Schedule(async () =>
                                        {
                                            fetchMyPlaylists();
                                        });
                                    });
                                };
                            });
                        },
                    };

                    Schedule(() =>
                    {
                        playlistItemView.Data = playlist;
                        myPlaylistItemsView.Add(playlistItemView);
                        playlistItemView.UpdateData();
                    });
                }
            });
        }

        private void saveVideoToPlaylist(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return;

            Task.Run(async () =>
            {
                await api.SaveVideoToPlaylist(myPlaylistsDropdown.Current.Value.Id, videoId);

                saveVideoOpenButton.Icon = FontAwesome.Solid.Bookmark;

                /*
                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.Playlists, NekoPlayerStrings.VideoSavedToPlaylist(myPlaylistsDropdown.Current.Value.Snippet.Title), FontAwesome.Solid.List);

                Schedule(() => onScreenDisplay.Display(toast));
                */
                notificationOverlay.Push(new PushNotificationContainer(FontAwesome.Solid.List, Color4.Green, NekoPlayerStrings.VideoSavedToPlaylist(myPlaylistsDropdown.Current.Value.Snippet.Title), NekoPlayerStrings.Playlists));

                await Task.Delay(1000);

                Schedule(async () =>
                {
                    fetchMyPlaylists();
                });
            });
        }

        private void removeVideoFromPlaylist(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
                return;

            Task.Run(async () =>
            {
                await api.RemoveVideoFromPlaylist(myPlaylistsDropdown.Current.Value.Id, videoId);

                saveVideoOpenButton.Icon = FontAwesome.Regular.Bookmark;

                /*
                ToastBase toast = new ToastWithIcon(NekoPlayerStrings.Playlists, NekoPlayerStrings.VideoRemovedFromPlaylist(myPlaylistsDropdown.Current.Value.Snippet.Title), FontAwesome.Solid.List);

                Schedule(() => onScreenDisplay.Display(toast));
                */
                notificationOverlay.Push(new PushNotificationContainer(FontAwesome.Solid.List, Color4.Red, NekoPlayerStrings.VideoRemovedFromPlaylist(myPlaylistsDropdown.Current.Value.Snippet.Title), NekoPlayerStrings.Playlists));

                await Task.Delay(1000);

                Schedule(async () =>
                {
                    fetchMyPlaylists();
                });
            });
        }

        public void GetReportReasons()
        {
            if (googleOAuth2.SignedIn.Value == false)
                return;

            IList<VideoAbuseReportReasonItem> wth2 = api.GetVideoAbuseReportReasons();

            Schedule(() =>
            {
                reportReason.Items = wth2;
                reportReason.Current.Value = wth2[0];
            });
        }
    }
}
