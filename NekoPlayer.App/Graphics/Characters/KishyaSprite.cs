// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using NekoPlayer.App.Graphics.Spine;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using Spine;

namespace NekoPlayer.App.Graphics.Characters
{
    public partial class KishyaSprite : SpineSprite
    {
        [BackgroundDependencyLoader]
        private void load(LargeTextureStore largeTextureStore)
        {
            using StreamReader atlasReader = OpenStream("Spine/Kishya/Kishya.atlas.txt");
            Atlas = new Atlas(atlasReader, "Textures/Spine/Kishya/", new SpineTextureLoader(largeTextureStore));

            using StreamReader jsonReader = OpenStream("Spine/Kishya/Kishya.skel.json");
            SkeletonBinary json = new SkeletonBinary(Atlas) { Scale = 1f };
            SkeletonData skeletonData = json.ReadSkeletonData(jsonReader.BaseStream);

            Skeleton = new Skeleton(skeletonData);
            AnimationStateData stateData = new AnimationStateData(Skeleton.Data);
            State = new AnimationState(stateData);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Position the skeleton to the center of the sprite bounds (screen)
            Skeleton.X = DrawWidth / 2;
            Skeleton.Y = DrawHeight * 2F / 3F;

            Skeleton.SetSkin("Normal");
            State.SetAnimation(0, "Idle_1", true);
        }
    }
}
