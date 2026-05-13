// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using NekoPlayer.App.Graphics.Sprites;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK.Graphics;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class ControlBarButton : AdaptiveClickableContainer
    {
        public Action<ControlBarButton>? ClickAction { get; set; }

        public LocalisableString Text
        {
            get => SpriteText.Text;
            set => SpriteText.Text = value;
        }

        public Color4 BackgroundColour
        {
            get => Background.Colour;
            set => Background.FadeColour(value);
        }

        private Color4? flashColour;

        /// <summary>
        /// The colour the background will flash with when this button is clicked.
        /// </summary>
        public Color4 FlashColour
        {
            get => flashColour ?? BackgroundColour;
            set => flashColour = value;
        }

        /// <summary>
        /// The additive colour that is applied to the background when hovered.
        /// </summary>
        public Color4 HoverColour
        {
            get => Hover.Colour;
            set => Hover.FadeColour(value);
        }

        private Color4 disabledColour = Color4.Gray;

        /// <summary>
        /// The additive colour that is applied to this button when disabled.
        /// </summary>
        public Color4 DisabledColour
        {
            get => disabledColour;
            set
            {
                if (disabledColour == value)
                    return;

                disabledColour = value;
                Enabled.TriggerChange();
            }
        }

        /// <summary>
        /// The duration of the transition when hovering.
        /// </summary>
        public double HoverFadeDuration { get; set; } = 200;

        /// <summary>
        /// The duration of the flash when this button is clicked.
        /// </summary>
        public double FlashDuration { get; set; } = 200;

        /// <summary>
        /// The duration of the transition when toggling the Enabled state.
        /// </summary>
        public double DisabledFadeDuration { get; set; } = 200;

        protected Box Hover;
        protected Box Background;
        protected SpriteText SpriteText;
        private readonly Container content;

        protected Container ForegroundContent;

        public void SetEnabledValue(bool enabled)
        {
            content.TransformTo(nameof(CornerRadius), enabled ? NekoPlayerApp.UI_CORNER_RADIUS / 1.5f : DrawHeight / 2, 250, Easing.OutQuint);
            Background.FadeColour(enabled ? overlayColourProvider.Content2 : overlayColourProvider.Background3, 250, Easing.OutQuint);
            SpriteText.FadeColour(enabled ? overlayColourProvider.Background3 : overlayColourProvider.Content2, 250, Easing.OutQuint);
        }

        public ControlBarButton(HoverSampleSet hoverSampleSet = HoverSampleSet.Default, bool transparentBackground = false)
            : base(hoverSampleSet)
        {
            base.Content.Add(content = new Container
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 15,
                EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                {
                    Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                    Colour = Color4.Black.Opacity(0.25f),
                    Offset = new Vector2(0, 2),
                    Radius = 16,
                },
                Children = new Drawable[]
                {
                    Background = new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Alpha = transparentBackground ? 0 : 1,
                    },
                    ForegroundContent = new Container
                    {
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Child = SpriteText = CreateText()
                    },
                    Hover = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Blending = BlendingParameters.Additive,
                        Alpha = 0,
                    },
                }
            });

            Enabled.BindValueChanged(enabledChanged, true);
        }

        public void SetBackgroundVisibility(bool active)
        {
            Background.Alpha = active ? 1 : 0;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider overlayColourProvider)
        {
            Background.Colour = overlayColourProvider.Background3;
            SpriteText.Colour = overlayColourProvider.Content2;
        }

        [Resolved]
        private OverlayColourProvider overlayColourProvider { get; set; }

        protected virtual SpriteText CreateText() => new AdaptiveSpriteText
        {
            Depth = -1,
            Origin = Anchor.Centre,
            Anchor = Anchor.Centre,
            Font = NekoPlayerApp.DefaultFont.With(size: 24),
            Colour = Color4.White,
        };

        protected override bool OnHover(HoverEvent e)
        {
            Hover.FadeTo(0.1f, 500, Easing.OutQuint);

            return base.OnHover(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            content.ScaleTo(0.9f, 2000, Easing.OutQuint);
            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            content.ScaleTo(1, 1000, Easing.OutElastic);
            base.OnMouseUp(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);

            Hover.FadeOut(500, Easing.OutQuint);
        }

        private void enabledChanged(ValueChangedEvent<bool> e)
        {
            this.FadeColour(e.NewValue ? Color4.White : DisabledColour, DisabledFadeDuration, Easing.OutQuint);
        }

        private void trigger()
        {
            if (Enabled.Value)
                ClickAction?.Invoke(this);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (!IsHovered)
                Hover.FadeOutFromOne(1600);

            Hover.FlashColour(FlashColour, 800, Easing.OutQuint);
            trigger();

            return base.OnClick(e);
        }
    }
}
