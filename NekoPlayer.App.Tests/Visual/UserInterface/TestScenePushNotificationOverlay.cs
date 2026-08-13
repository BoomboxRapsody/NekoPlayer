// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Graphics.Containers;
using NekoPlayer.App.Graphics.UserInterface;
using NUnit.Framework;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace NekoPlayer.App.Tests.Visual.UserInterface
{
    [TestFixture]
    public partial class TestScenePushNotificationOverlay : NekoPlayerTestScene
    {
        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        private PushNotificationOverlay pushNotificationOverlay;

        public TestScenePushNotificationOverlay()
        {
            Add(pushNotificationOverlay = new PushNotificationOverlay
            {
                Anchor = osu.Framework.Graphics.Anchor.Centre,
                Origin = osu.Framework.Graphics.Anchor.Centre,
            });

            AddStep("push notification", () =>
            {
                pushNotificationOverlay.Push(new PushNotificationContainer(FontAwesome.Solid.Bell, Color4.Yellow, "Test notification", "test テスト 테스트 123"));
            });
        }
    }
}
