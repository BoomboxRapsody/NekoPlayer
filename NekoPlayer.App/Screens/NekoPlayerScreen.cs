// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace NekoPlayer.App.Screens
{
    public abstract partial class NekoPlayerScreen : Screen, INekoPlayerScreen
    {
        protected new NekoPlayerAppBase Game => base.Game as NekoPlayerAppBase;

        public virtual bool CursorVisible => true;

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);
            this.FadeOut(250, Easing.OutQuint);
            this.ScaleTo(0.9f, 250, Easing.OutQuint);
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
            this.FadeIn(250, Easing.OutQuint);
            this.ScaleTo(1.1f).Then().ScaleTo(1f, 250, Easing.OutQuint);
        }
    }
}
