// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;
using NekoPlayer.App.Config;

namespace NekoPlayer.App.Graphics.Containers
{
    public partial class ScalingContainer : DrawSizePreservingFillContainer
    {
        private Bindable<float>? uiScale;

        protected float CurrentScale { get; private set; } = 1;

        [BackgroundDependencyLoader]
        private void load(NekoPlayerConfigManager appConfig)
        {
            uiScale = appConfig.GetBindable<float>(NekoPlayerSetting.UIScale);
            uiScale.BindValueChanged(args => this.TransformTo(nameof(CurrentScale), args.NewValue, 500, Easing.OutQuart), true);
        }

        protected override void Update()
        {
            Scale = new Vector2(CurrentScale);
            Size = new Vector2(1 / CurrentScale);

            base.Update();
        }
    }
}
