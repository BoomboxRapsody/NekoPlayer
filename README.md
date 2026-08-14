<h1 align="center">NekoPlayer</h1>
<p align="center"><img width="250" alt="NekoPlayer Logo" src="assets/NekoPlayer_LiquidGlass_Remake.png"></p>
<p align="center">A new era of YouTube Video Player with a focus on design and creativity.</p>
<p align="center">Some resources(samples) in this app are using <a href="https://github.com/ppy/osu-resources">ppy/osu-resources</a>.</p>

<p align="center">
<a href="https://github.com/BoomboxRapsody/NekoPlayer/actions/workflows/ci.yml"><img src="https://github.com/BoomboxRapsody/NekoPlayer/actions/workflows/ci.yml/badge.svg?branch=master&event=push" alt="Build status"></a>
<a href="https://github.com/BoomboxRapsody/NekoPlayer/releases/latest"><img src="https://img.shields.io/github/release/BoomboxRapsody/NekoPlayer.svg" alt="GitHub release"></a>
<img src="https://img.shields.io/badge/made_in-korea-0F64CD.svg?labelColor=CD2E3A" alt="Made In Korea">
<a href="https://github.com/BoomboxRapsody/NekoPlayer/blob/master/LICENSE.md"><img src="https://img.shields.io/github/license/BoomboxRapsody/NekoPlayer.svg" alt="Licence"></a>
<a href="https://discord.gg/UZWDqQ29ch"><img src="https://discordapp.com/api/guilds/1474931183854026812/widget.png?style=shield" alt="dev chat"></a>
<a href="https://www.codefactor.io/repository/github/BoomboxRapsody/NekoPlayer"><img src="https://www.codefactor.io/repository/github/BoomboxRapsody/NekoPlayer/badge" alt="CodeFactor"></a>
</p>

## Downloading and installing the app

### Latest release:

| [Windows 10+ (x64)](https://github.com/BoomboxRapsody/NekoPlayer/releases/latest/download/NekoPlayer-win-Setup.exe) | [Linux (x64)](https://github.com/BoomboxRapsody/NekoPlayer/releases/latest/download/NekoPlayer-linux-x64.AppImage) |
|--------------------------------------------------------------------------------------|-------------|

The YouTube API Daily Quota fills up very fast (Google has a hard limit of 10,000 units). Please do not ask about this.

## Developing NekoPlayer

### Prerequisites

Please make sure you have the following prerequisites:

- A desktop platform with the [.NET 10.0 SDK](https://dotnet.microsoft.com/download) installed.

When working with the codebase, we recommend using an IDE with intelligent code completion and syntax highlighting, such as the latest version of [Visual Studio](https://visualstudio.microsoft.com/vs/), [Visual Studio Code](https://code.visualstudio.com/) with the [EditorConfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) and [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) plugin installed.

### Downloading the source code

Clone the repository including submodules:

```shell
git clone --recurse-submodules https://github.com/BoomboxRapsody/NekoPlayer
cd NekoPlayer
```

To update the source code to the latest commit, run the following command inside the `NekoPlayer` directory:

```shell
git pull --recurse-submodules
```

### Building

#### From an IDE

You should load the solution via one of the platform-specific `.slnf` files, rather than the main `.sln`. This will reduce dependencies and hide platforms that you don't care about. Valid `.slnf` files are:

- `NekoPlayer.Desktop.Windows.slnf` (Windows platform with WinRT extensions, most common)
- `NekoPlayer.Desktop.slnf` (Linux and other platform)

Run configurations for the recommended IDEs (listed above) are included. You should use the provided Build/Run functionality of your IDE to get things going. When testing or building new components, it's highly encouraged you use the `NekoPlayer (Tests)` project/configuration. More information on this is provided [below](#contributing).

To build for mobile platforms, you will likely need to run `sudo dotnet workload restore` if you haven't done so previously.

#### From CLI

You can also build and run *NekoPlayer* from the command-line with a single command:

```shell
dotnet run --project NekoPlayer.Desktop.Windows (for Windows)
dotnet run --project NekoPlayer.Desktop (for Linux and other platform)
```

When running locally to do any kind of performance testing, make sure to add `-c Release` to the build command, as the overhead of running with the default `Debug` configuration can be large (especially when testing with local framework modifications as below).

If the build fails, try to restore NuGet packages with `dotnet restore`.

### Code analysis

Before committing your code, please run a code formatter. This can be achieved by running `dotnet format` in the command line, or using the `Format code` command in your IDE.

We have adopted some cross-platform, compiler integrated analyzers. They can provide warnings when you are editing, building inside IDE or from command line, as-if they are provided by the compiler itself.

JetBrains ReSharper InspectCode is also used for wider rule sets. You can run it from PowerShell with `.\InspectCode.ps1`. Alternatively, you can install ReSharper or use Rider to get inline support in your IDE of choice.

## Contributing

When it comes to contributing to the project, the two main things you can do to help out are reporting issues and submitting pull requests. Please refer to the [contributing guidelines](CONTRIBUTING.md) to understand how to help in the most effective way possible.

## Licence

*NekoPlayer*'s codes are licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

**Note:** FFmpeg binaries are distributed under their original licenses (GPL/LGPL) from the source.
Please refer to [FFmpeg License](https://www.ffmpeg.org/legal.html) for details.

Please also note that app resources are covered by a separate licence. Please see [the licence file](https://github.com/BoomboxRapsody/NekoPlayer/blob/master/NekoPlayer.Resources/LICENSE.md) for more information.