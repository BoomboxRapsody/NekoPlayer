// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NekoPlayer.App.Graphics.UserInterface;

namespace NekoPlayer.App.Graphics.UserInterfaceV3
{
    public partial class EnhancedFocusedTextBox : FocusedTextBox
    {
        public Action OnEnterKeyPressed;

        protected override void OnTextCommitted(bool textChanged)
        {
            base.OnTextCommitted(textChanged);
            OnEnterKeyPressed.Invoke();
        }
    }
}
