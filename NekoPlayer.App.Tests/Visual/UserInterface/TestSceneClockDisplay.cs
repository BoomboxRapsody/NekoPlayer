// Copyright (c) 2026 BoomboxRapsody <boomboxrapsody@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NekoPlayer.App.Graphics.UserInterface;
using NUnit.Framework;

namespace NekoPlayer.App.Tests.Visual.UserInterface
{
    [TestFixture]
    public partial class TestSceneClockDisplay : NekoPlayerTestScene
    {
        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        public TestSceneClockDisplay()
        {
            Add(new ClockDisplay());
        }
    }
}
