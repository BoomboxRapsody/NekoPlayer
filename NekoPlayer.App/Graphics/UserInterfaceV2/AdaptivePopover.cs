// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Config;
using NekoPlayer.App.Input.Binding;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osuTK;

namespace NekoPlayer.App.Graphics.UserInterfaceV2
{
    public partial class AdaptivePopover : Popover, IKeyBindingHandler<GlobalAction>
    {
        private const float fade_duration = 250;
        private const double scale_duration = 500;

        private Sample? samplePopIn;
        private Sample? samplePopOut;

        // required due to LoadAsyncComplete() in `VisibilityContainer` calling PopOut() during load - similar workaround to `OsuDropdownMenu`
        private bool wasOpened;

        public AdaptivePopover(bool withPadding = true)
        {
            Content.Padding = withPadding ? new MarginPadding(20) : new MarginPadding();

            Body.Masking = true;
            Body.CornerRadius = 10;
            Body.Margin = new MarginPadding(10);
            Body.EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Offset = new Vector2(0, 2),
                Radius = 5,
                Colour = Colour4.Black.Opacity(0.3f)
            };
        }

        private Bindable<SFXType> overlaySFXType;

        [BackgroundDependencyLoader(true)]
        private void load(OverlayColourProvider? colourProvider, AdaptiveColour colours, AudioManager audio, NekoPlayerConfigManager appConfig)
        {
            overlaySFXType = appConfig.GetBindable<SFXType>(NekoPlayerSetting.OverlaySFXType);

            overlaySFXType.BindValueChanged(sfx =>
            {
                refreshSFX();
            }, true);

            Background.Colour = Arrow.Colour = colourProvider?.Background4 ?? colours.GreySeaFoamDarker;
        }

        [Resolved]
        private ISampleStore sampleStoreGlobal { get; set; }

        [Resolved]
        private NekoPlayerConfigManager appGlobalConfig { get; set; }

        private void refreshSFX()
        {
            if (appGlobalConfig.Get<SFXType>(NekoPlayerSetting.OverlaySFXType) == SFXType.Legacy)
            {
                samplePopIn = sampleStoreGlobal.Get(@"overlay-pop-in");
                samplePopOut = sampleStoreGlobal.Get(@"overlay-pop-out");
            }
            else
            {
                samplePopIn = sampleStoreGlobal.Get(@"New_Fix/overlay-pop-in");
                samplePopOut = sampleStoreGlobal.Get(@"New_Fix/overlay-pop-out");
            }
        }

        protected override Drawable CreateArrow() => Empty();

        protected override void PopIn()
        {
            this.ScaleTo(1, scale_duration, Easing.OutElasticHalf);
            this.FadeIn(fade_duration, Easing.OutQuint);

            if (appGlobalConfig.Get<bool>(NekoPlayerSetting.PlayOverlaySFX))
                samplePopIn?.Play();

            wasOpened = true;
        }

        protected override void PopOut()
        {
            this.ScaleTo(0.7f, scale_duration, Easing.OutQuint);
            this.FadeOut(fade_duration, Easing.OutQuint);

            if (wasOpened)
                if (appGlobalConfig.Get<bool>(NekoPlayerSetting.PlayOverlaySFX))
                    samplePopOut?.Play();
        }

        public virtual bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat)
                return false;

            if (State.Value == Visibility.Hidden)
                return false;

            if (e.Action == GlobalAction.Back)
            {
                this.HidePopover();
                return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }
    }
}
