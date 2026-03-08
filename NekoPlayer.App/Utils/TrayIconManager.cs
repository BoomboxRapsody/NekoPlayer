// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.YouTube.v3.Data;
using NekoPlayer.App.Config;
using NekoPlayer.App.Online;
using NekoPlayer.App.Overlays;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Platform;
using SDL;

namespace NekoPlayer.App.Utils
{
    public unsafe partial class TrayIconManager : Component
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private VolumeOverlay volumeOverlay { get; set; } = null!;

        private IDisposable? activeTrayIcon = null!;

        [Resolved]
        private NekoPlayerConfigManager config { get; set; }

        [Resolved]
        private AudioManager audioManager { get; set; }

        private Bindable<double> muteBindable = new Bindable<double>(0);

        private SDL_Surface* appIcon;

        [Resolved]
        private YouTubeAPI api { get; set; }

        [Resolved]
        private GoogleOAuth2 googleOAuth2 { get; set; }

        [Resolved]
        private NekoPlayerApp app { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            appIcon = SDL3.SDL_LoadBMP(Directory.GetCurrentDirectory() + @"/appIcon.bmp");
        }

        public void HideToTrayIcon()
        {
            bool previousState = volumeOverlay.IsMuted.Value;

            if (host.Window is ISDLWindow window)
            {
                host.Window.Hide();
                Schedule(() => audioManager.Samples.AddAdjustment(AdjustableProperty.Volume, muteBindable));

                var icon = new TrayIcon
                {
                    Label = "NekoPlayer",
                    Icon = appIcon,
                    Menu = new TrayMenuEntry[]
                    {
                        new TrayButton
                        {
                            Label = NekoPlayerTrayIconStrings.OpenApp,
                            Action = () => onShow(previousState),
                        },
                        new TraySeparator(),
                        new TrayButton
                        {
                            Label = NekoPlayerTrayIconStrings.OpenSettings,
                            Action = () =>
                            {
                                onShow(previousState);
                                app.AppMessageHandler.OpenSettings();
                            },
                        },
                        new TrayCheckBox
                        {
                            Label = NekoPlayerTrayIconStrings.AdjustPitchOnSpeedChange,
                            Checked = config.Get<bool>(NekoPlayerSetting.AdjustPitchOnSpeedChange),
                            Action = () =>
                            {
                                app.AppMessageHandler.TogglePreservePitch();
                            },
                        },
                        new TraySeparator(),
                        new TrayButton
                        {
                            Label = NekoPlayerTrayIconStrings.MyPlaylists,
                            Enabled = googleOAuth2.SignedIn.Value,
                            Action = () =>
                            {
                                onShow(previousState);
                                app.AppMessageHandler.OpenMyPlaylists();
                            },
                        },
                        new TraySeparator(),
                        new TrayButton
                        {
                            Label = NekoPlayerTrayIconStrings.Quit,
                            Action = () =>
                            {
                                Schedule(() => app.AttemptExit(true));
                            },
                        },
                    }
                };

                try
                {
                    activeTrayIcon = window.CreateTrayIcon(icon);
                }
                catch (PlatformNotSupportedException ex)
                {
                    Logger.Log($"aaaa");
                    return;
                }

                Logger.Log($"Created notification tray icon");
            }
        }

        private void onShow(bool previousState)
        {
            Logger.Log($"Notification tray icon clicked");
            host.Window.Show();
            //host.Window.Raise();

            activeTrayIcon.Dispose();
            Logger.Log($"Notification tray icon removed");
            Schedule(() => audioManager.Samples.RemoveAdjustment(AdjustableProperty.Volume, muteBindable));
        }
    }
}
