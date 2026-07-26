<p align="center">
  <img width="250" alt="NekoPlayer Logo" src="assets/NekoPlayer_LiquidGlass_Remake.png">
</p>

<h1 align="center">NekoPlayer</h1>

[![Build status](https://github.com/BoomboxRapsody/NekoPlayer/actions/workflows/ci.yml/badge.svg?branch=master&event=push)](https://github.com/BoomboxRapsody/NekoPlayer/actions/workflows/ci.yml)
[![GitHub release](https://img.shields.io/github/release/BoomboxRapsody/NekoPlayer.svg)](https://github.com/BoomboxRapsody/NekoPlayer/releases/latest)
[![Licence](https://img.shields.io/github/license/BoomboxRapsody/NekoPlayer.svg)](https://github.com/BoomboxRapsody/NekoPlayer/blob/master/LICENSE.md)
[![dev chat](https://discordapp.com/api/guilds/1474931183854026812/widget.png?style=shield)](https://discord.gg/UZWDqQ29ch)
[![CodeFactor](https://www.codefactor.io/repository/github/BoomboxRapsody/NekoPlayer/badge)](https://www.codefactor.io/repository/github/BoomboxRapsody/NekoPlayer)

The new era of YouTube Video Player written in [custom osu-framework](https://github.com/BoomboxRapsody/osu-framework).

### Latest release:

| [Windows 10+ (x64)](https://github.com/BoomboxRapsody/NekoPlayer/releases/latest/download/NekoPlayer-win-Setup.exe) |
|--------------------------------------------------------------------------------------|

## Developing NekoPlayer

### Prerequisites

Please make sure you have the following prerequisites:

- A desktop platform with the Windows 10 version 2004 or higher and [Git LFS](https://git-lfs.com/), [.NET 8.0 SDK](https://dotnet.microsoft.com/download) installed.

When working with the codebase, we recommend using an IDE with intelligent code completion and syntax highlighting, such as the latest version of [Visual Studio](https://visualstudio.microsoft.com/vs/), [Visual Studio Code](https://code.visualstudio.com/) with the [EditorConfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) and [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) plugin installed.

### Downloading the source code

Clone the repository and get required libraries with Git LFS:

```shell
git lfs install
git clone --recurse-submodules https://github.com/BoomboxRapsody/NekoPlayer
cd NekoPlayer
git lfs pull
```

To update the source code to the latest commit, run the following command inside the `NekoPlayer` directory:

```shell
git pull --recurse-submodules
git lfs pull
```

### Building

#### From an IDE

You should load the solution via one of the platform-specific `.slnf` files, rather than the main `.sln`. This will reduce dependencies and hide platforms that you don't care about. Valid `.slnf` files are:

- `NekoPlayer.Desktop.slnf`

Run configurations for the recommended IDEs (listed above) are included. You should use the provided Build/Run functionality of your IDE to get things going. When testing or building new components, it's highly encouraged you use the `NekoPlayer (Tests)` project/configuration. More information on this is provided [below](#contributing).

To build for mobile platforms, you will likely need to run `sudo dotnet workload restore` if you haven't done so previously.

#### From CLI

You can also build and run *NekoPlayer* from the command-line with a single command:

```shell
dotnet run --project NekoPlayer.Desktop
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

*NekoPlayer*'s code and framework are licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see [the licence file](LICENCE) for more information. [tl;dr](https://tldrlegal.com/license/mit-license) you can do whatever you want as long as you include the original copyright and license notice in any copy of the software/source.

**Note:** FFmpeg binaries are distributed under their original licenses (GPL/LGPL) from the source.
Please refer to [FFmpeg License](https://www.ffmpeg.org/legal.html) for details.

This app uses `spine-csharp` (located as Spine in the source code) the C# Spine Runtime.
Please refer to [Spine Runtimes License](https://esotericsoftware.com/spine-runtimes-license) for details.

Please also note that app resources are covered by a separate licence. Please see [the licence file](https://github.com/BoomboxRapsody/NekoPlayer/blob/master/NekoPlayer.App/BuiltInResources/LICENSE.md) for more information.