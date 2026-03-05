<p align="center">
  <img width="500" alt="YouTube Player EX Logo" src="assets/logo.png">
</p>

# YouTube Player EX

[![Build status](https://github.com/BoomboxRapsody/YouTubePlayerEX/actions/workflows/ci.yml/badge.svg?branch=master&event=push)](https://github.com/BoomboxRapsody/YouTubePlayerEX/actions/workflows/ci.yml)
[![GitHub release](https://img.shields.io/github/release/BoomboxRapsody/YouTubePlayerEX.svg)](https://github.com/BoomboxRapsody/YouTubePlayerEX/releases/latest)
[![Licence](https://img.shields.io/github/license/BoomboxRapsody/YouTubePlayerEX.svg)](https://github.com/BoomboxRapsody/YouTubePlayerEX/blob/master/LICENSE.md)
[![dev chat](https://discordapp.com/api/guilds/1474931183854026812/widget.png?style=shield)](https://discord.gg/UZWDqQ29ch)
[![CodeFactor](https://www.codefactor.io/repository/github/boomboxrapsody/youtubeplayerex/badge)](https://www.codefactor.io/repository/github/boomboxrapsody/youtubeplayerex)

The enhanced YouTube video player written in [custom osu-framework](https://github.com/BoomboxRapsody/YouTubePlayerEX-framework).

### Latest release:

| [Windows 10+ (x64)](https://github.com/BoomboxRapsody/YouTubePlayerEX/releases/latest/download/YouTubePlayerEX-win-Setup.exe) | [Linux (x64)](https://github.com/BoomboxRapsody/YouTubePlayerEX/releases/latest/download/YouTubePlayerEX-linux-x64.AppImage)
|--------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------|

If your platform is unsupported or not listed above, there is still a chance you can run the release or manually build it by following the instructions below.

## Developing YouTube Player EX

### Prerequisites

Please make sure you have the following prerequisites:

- A desktop platform with the [.NET 8.0 SDK](https://dotnet.microsoft.com/download) installed.

When working with the codebase, we recommend using an IDE with intelligent code completion and syntax highlighting, such as the latest version of [Visual Studio](https://visualstudio.microsoft.com/vs/), [Visual Studio Code](https://code.visualstudio.com/) with the [EditorConfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) and [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) plugin installed.

### Downloading the source code

Clone the repository:

```shell
git clone --recurse-submodules https://github.com/BoomboxRapsody/YouTubePlayerEX
cd YouTubePlayerEX
```

To update the source code to the latest commit, run the following command inside the `YouTubePlayerEX` directory:

```shell
git pull --recurse-submodules
```

### Building

#### From an IDE

You should load the solution via one of the platform-specific `.slnf` files, rather than the main `.sln`. This will reduce dependencies and hide platforms that you don't care about. Valid `.slnf` files are:

- `YouTubePlayerEX.Desktop.Windows.slnf` (Windows platform with WinRT extensions, most common)
- `YouTubePlayerEX.Desktop.slnf` (Linux and other platform)

Run configurations for the recommended IDEs (listed above) are included. You should use the provided Build/Run functionality of your IDE to get things going. When testing or building new components, it's highly encouraged you use the `YouTube Player EX (Tests)` project/configuration. More information on this is provided [below](#contributing).

To build for mobile platforms, you will likely need to run `sudo dotnet workload restore` if you haven't done so previously.

#### From CLI

You can also build and run *YouTube Player EX* from the command-line with a single command:

```shell
dotnet run --project YouTubePlayerEX.Desktop.Windows (for Windows)
dotnet run --project YouTubePlayerEX.Desktop (for Linux and other platform)
```

When running locally to do any kind of performance testing, make sure to add `-c Release` to the build command, as the overhead of running with the default `Debug` configuration can be large (especially when testing with local framework modifications as below).

If the build fails, try to restore NuGet packages with `dotnet restore`.

### Code analysis

Before committing your code, please run a code formatter. This can be achieved by running `dotnet format` in the command line, or using the `Format code` command in your IDE.

We have adopted some cross-platform, compiler integrated analyzers. They can provide warnings when you are editing, building inside IDE or from command line, as-if they are provided by the compiler itself.

JetBrains ReSharper InspectCode is also used for wider rule sets. You can run it from PowerShell with `.\InspectCode.ps1`. Alternatively, you can install ReSharper or use Rider to get inline support in your IDE of choice.

## Contributing

When it comes to contributing to the project, the two main things you can do to help out are reporting issues and submitting pull requests. Please refer to the [contributing guidelines](CONTRIBUTING.md) to understand how to help in the most effective way possible.

If you wish to help with localisation efforts, head over to [this](https://app.tolgee.io/projects/28208).

## Licence

*YouTube Player EX*'s code and framework are licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

**Note:** FFmpeg binaries are distributed under their original licenses (GPL/LGPL) from the source.
Please refer to [FFmpeg License](https://www.ffmpeg.org/legal.html) for details.

Please also note that app resources are covered by a separate licence. Please see the [BoomboxRapsody/YouTubePlayerEX-resources](https://github.com/BoomboxRapsody/YouTubePlayerEX-resources) repository for clarifications.

## Star History

<a href="https://www.star-history.com/#BoomboxRapsody/YouTubePlayerEX&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/svg?repos=BoomboxRapsody/YouTubePlayerEX&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/svg?repos=BoomboxRapsody/YouTubePlayerEX&type=date&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/svg?repos=BoomboxRapsody/YouTubePlayerEX&type=date&legend=top-left" />
 </picture>
</a>
