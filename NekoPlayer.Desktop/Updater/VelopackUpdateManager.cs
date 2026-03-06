// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Logging;
using osu.Framework.Threading;
using Velopack;
using Velopack.Sources;
using NekoPlayer.App;
using NekoPlayer.App.Localisation;
using UpdateManager = NekoPlayer.App.Updater.UpdateManager;

namespace NekoPlayer.Desktop.Updater
{
    public partial class VelopackUpdateManager : UpdateManager
    {
        [Resolved]
        private YouTubePlayerEXAppBase game { get; set; } = null!;

#nullable enable
        private ScheduledDelegate? scheduledBackgroundCheck;
#nullable disable

        private void scheduleNextUpdateCheck()
        {
            scheduledBackgroundCheck?.Cancel();
            scheduledBackgroundCheck = Scheduler.AddDelayed(() =>
            {
                log("Running scheduled background update check...");
                CheckForUpdate();
            }, 60000 * 30);
        }

#nullable enable
        protected override async Task<bool> PerformUpdateCheck(CancellationToken cancellationToken)
        {
            scheduledBackgroundCheck?.Cancel();

            try
            {
                IUpdateSource updateSource = new GithubSource(@"https://github.com/BoomboxRapsody/NekoPlayer", null, false);
                Velopack.UpdateManager updateManager = new Velopack.UpdateManager(updateSource, new UpdateOptions
                {
                    AllowVersionDowngrade = true
                });

                UpdateInfo? update = await updateManager.CheckForUpdatesAsync().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    log("Update check cancelled");
                    scheduleNextUpdateCheck();
                    return true;
                }

                if (update == null)
                {
                    // No update is available.
                    log("No update found");
                    scheduleNextUpdateCheck();
                    return false;
                }

                // Download update in the background while notifying awaiters of the update being available.
                log($"New update available: {update.TargetFullRelease.Version}");
                downloadUpdate(updateManager, update, cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                log($"Update check failed with error ({e.Message})");

                // we shouldn't crash on a web failure. or any failure for the matter.
                scheduleNextUpdateCheck();
                return true;
            }
        }
#nullable disable

        private void downloadUpdate(Velopack.UpdateManager updateManager, UpdateInfo update, CancellationToken cancellationToken) => Task.Run(async () =>
        {
            log($"Beginning download of update {update.TargetFullRelease.Version}...");
            game.UpdateButtonEnabled.Value = false;

            try
            {
                await updateManager.DownloadUpdatesAsync(update, p =>
                {
                    game.UpdateManagerVersionText.Value = NekoPlayerStrings.DownloadingUpdate($"{p}");
                    game.UpdateButtonEnabled.Value = false;
                }, cancellationToken).ConfigureAwait(false);
                game.UpdateManagerVersionText.Value = NekoPlayerStrings.RestartRequired;
                game.RestartRequired.Value = true;
                game.RestartAction = () => restartToApplyUpdate(updateManager, update);
                game.UpdateButtonEnabled.Value = true;
            }
            catch (Exception e)
            {
                // In the case of an error, a separate notification will be displayed.
                game.UpdateManagerVersionText.Value = NekoPlayerStrings.UpdateFailed;
                game.UpdateButtonEnabled.Value = true;
                Logger.Error(e, @"Update failed!");
            }

            return true;
        }, cancellationToken);

        private void restartToApplyUpdate(Velopack.UpdateManager updateManager, UpdateInfo update) => Task.Run(async () =>
        {
            await updateManager.WaitExitThenApplyUpdatesAsync(update.TargetFullRelease).ConfigureAwait(false);
            Schedule(() => game.AttemptExit());
        });

        private static void log(string text) => Logger.Log($"VelopackUpdateManager: {text}");
    }
}
