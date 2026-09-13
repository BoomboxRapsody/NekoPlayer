// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using NekoPlayer.App.Online;

namespace NekoPlayer.App.Updater
{
    /// <summary>
    /// An update manager that shows notifications if a newer release is detected.
    /// This is a case where updates are handled externally by a package manager or other means, so no action is performed on clicking the notification.
    /// </summary>
    public partial class NoActionUpdateManager : UpdateManager
    {
        private string version = string.Empty;

        [BackgroundDependencyLoader]
        private void load(NekoPlayerAppBase game)
        {
            version = game.Version;
        }

        protected override async Task<bool> PerformUpdateCheck(CancellationToken cancellationToken)
        {
            try
            {
                NekoPlayerJsonWebRequest<GitHubRelease[]> releasesRequest = new NekoPlayerJsonWebRequest<GitHubRelease[]>("https://api.github.com/repos/BoomboxRapsody/YouTubePlayerEX/releases?per_page=10&page=1");
                await releasesRequest.PerformAsync(cancellationToken).ConfigureAwait(false);

                GitHubRelease[] releases = releasesRequest.ResponseObject;
                GitHubRelease? latest = releases.OrderByDescending(r => r.PublishedAt).FirstOrDefault(r => !r.Prerelease);

                if (latest == null)
                    return false;

                string latestTagName = latest.TagName;

                if (latestTagName != version)
                {
                    return true;
                }
            }
            catch
            {
                // we shouldn't crash on a web failure. or any failure for the matter.
                return true;
            }

            return false;
        }
    }
}
