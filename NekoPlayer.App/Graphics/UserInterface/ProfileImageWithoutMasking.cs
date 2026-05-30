// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using NekoPlayer.App.Online;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class ProfileImageWithoutMasking : CompositeDrawable
    {
        private Sprite profileImage;

        private Google.Apis.YouTube.v3.Data.Channel channel;

        private LoadingSpinner loading;

        [Resolved]
        private TextureStore textureStore { get; set; }

        [Resolved]
        private YouTubeAPI api { get; set; }

        public ProfileImageWithoutMasking(float size = 30)
        {
            Width = Height = size;
            //CornerRadius = size / 2;
            InternalChildren = new Drawable[]
            {
                profileImage = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                loading = new LoadingLayer(true, false, false)
            };
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            profileImage.Colour = ColourInfo.GradientHorizontal(Color4.White.Opacity(0.5f), Color4.White.Opacity(0));
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            profileImage.Dispose();
        }

        public void UpdateProfileImage(string channelId)
        {
            Task.Run(async () =>
            {
                channel = api.GetChannel(channelId);
                _ = Task.Run(async () =>
                {
                    await GetProfileImage(channel.Snippet.Thumbnails.High.Url);
                });
            });
        }

        public async Task GetProfileImage(string url, CancellationToken cancellationToken = default)
        {
            Schedule(() => loading.Show());
            Texture north = await textureStore.GetAsync(channel.Snippet.Thumbnails.High.Url, cancellationToken);
            //GetPalette();
            Schedule(() => { profileImage.Texture = north; });
            Schedule(() => loading.Hide());
        }
    }
}
