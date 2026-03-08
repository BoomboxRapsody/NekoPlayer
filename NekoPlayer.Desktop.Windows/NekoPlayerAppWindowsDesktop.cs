// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using NekoPlayer.App;
using NekoPlayer.App.Extensions;
using NekoPlayer.App.Updater;
using NekoPlayer.App.Utils;
using NekoPlayer.Desktop.Windows.Updater;

namespace NekoPlayer.Desktop.Windows
{
    internal partial class NekoPlayerAppWindowsDesktop : NekoPlayerApp
    {
        protected override UpdateManager CreateUpdateManager() => new VelopackUpdateManager();

        private TrayIconManager trayIconManager;

        public override bool RestartAppWhenExited()
        {
            Task.Run(() => Velopack.UpdateExe.Start(waitPid: (uint)Environment.ProcessId)).FireAndForget();
            return true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            LoadComponentAsync(trayIconManager = new TrayIconManager(), Add);
        }

        public override void RequestHideToTrayIcon()
        {
            base.RequestHideToTrayIcon();
            trayIconManager.HideToTrayIcon();
        }

        public override MediaSession CreateMediaSession() => new WindowsMediaSessionHandler();
    }
}
