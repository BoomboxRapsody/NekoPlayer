// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using osu.Framework.Input.Events;

namespace NekoPlayer.App.Graphics.UserInterface
{
    public partial class RoundedButtonContainer : ProjectYomiClickableContainer
    {
        public Action<RoundedButtonContainer>? ClickAction { get; set; }

        public RoundedButtonContainer()
        {
            Enabled.Value = true;
        }

        private void trigger()
        {
            ClickAction?.Invoke(this);
        }

        protected override bool OnHover(HoverEvent e)
        {
            return base.OnHover(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            trigger();

            return base.OnClick(e);
        }
    }
}
