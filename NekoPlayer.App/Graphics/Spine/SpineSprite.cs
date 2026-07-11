// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using Spine;

namespace NekoPlayer.App.Graphics.Spine
{
    /// <summary>
    /// The core of all Spine sprites, contains useful functions to make the loading of Spine sprites easier.
    /// </summary>
    public partial class SpineSprite : Sprite
    {
        [Resolved]
        private NekoPlayerAppBase Game { get; set; }

        [Resolved]
        private IRenderer Renderer { get; set; }

        protected override DrawNode CreateDrawNode() => new SpineSprite_DrawNode(this);

        public Atlas Atlas { get; protected set; }

        public Skeleton Skeleton { get; protected set; }

        protected AnimationState State { get; set; }

        public bool PremultipliedAlpha;
        public float ZSpacing;

        protected SpineSprite()
        {
            // TODO: Inherit the max size of the drawn sprite if possible or get the size from the Skeleton or Atlas
            RelativeSizeAxes = Axes.Both;
        }

        /// <summary>
        /// Once the Sprite loading it will set the Texture to <see cref="IRenderer.WhitePixel"/>
        /// <remarks>If overriden make sure to call the base method in order to trigger the <see cref="SpineSprite_DrawNode"/></remarks>
        /// </summary>
        protected override void LoadComplete()
        {
            base.LoadComplete();

            // After finishing loading it will always set the Texture to a WhitePixel, this is to trigger the
            // DrawNode to do something (basically drawing the Skeleton)
            Texture = Renderer.WhitePixel;
        }

        protected override void Update()
        {
            UpdateSpine();
        }

        /// <summary>
        /// Updates all Spine related data.
        /// <para/>
        /// Override this to customize the behaviour of the Spine updates.
        /// </summary>
        protected virtual void UpdateSpine()
        {
            // Using the raw elapsed makes it look too fast, make it the standard so it looks properly
            var delta = (float)Time.Elapsed / 1000;
            if (State == null || Skeleton == null) return;

            State.Update(delta);
            State.Apply(Skeleton);

            Skeleton.UpdateWorldTransform();
        }

        /// <summary>
        /// Quick util function to open streams to load up Spine data.
        /// </summary>
        /// <param name="path">The path of the asset inside Resources</param>
        /// <returns>A StreamReader to read the content of the asset</returns>
        protected StreamReader OpenStream(string path) => new(Game.Resources.GetStream(path)); // Should handle null streams but eh whatever lmao
    }
}
