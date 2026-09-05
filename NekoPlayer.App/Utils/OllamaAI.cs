// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using NekoPlayer.App.Graphics.Containers;
using NekoPlayer.App.Overlays;
using Ollama;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace NekoPlayer.App.Utils
{
    public partial class OllamaAI : Drawable
    {
        private OllamaClient ollama;

        [BackgroundDependencyLoader]
        private void load()
        {
            ollama = new OllamaClient();
            Task.Run(() => DownloadModel("qwen3.5-0.8b"));
        }

        public async Task<ListResponse> ListModel()
        {
            var models = await ollama.ListAsync();
            return models;
        }

        [Resolved]
        private PushNotificationOverlay pushNotificationOverlay { get; set; }

        public async Task DownloadModel(string model)
        {
            PushNotificationContainer pushNotificationContainer = new PushNotificationContainer(FontAwesome.Solid.Download, Color4.Green, $"Downloading {model} model...", "");

            Schedule(() => pushNotificationOverlay.Push(pushNotificationContainer));

            await foreach (var response in ollama.PullAsStreamAsync(model))
            {
                Schedule(() => pushNotificationContainer.UpdateDesc($"Progress: {response.Completed}/{response.Total}"));
            }
        }
    }
}
