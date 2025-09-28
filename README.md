![Banner](https://vcollab.net/Resources/banner.webp)

# VCollab - The universal collaboration software for VTubers

VCollab is a software made to make vtuber collaboration more interactive by allowing the display of multiple vtubing models on the same screen. The goal of VCollab is to make this type of collaborations with models as simple and accessible as possible with a strong emphasis on performances.

This page is aimed at people interesting in the technical aspects of VCollab and how to contribute to the code, for more general information about VCollab usage and its features, take a look at the [**official website**](https://vcollab.net) _(WIP)_

## Table of content

- [Table of content](#table-of-content)
- [How it works](#how-it-works)
- [Building VCollab](#building-vcollab)
- [Bug report and help](#bug-report-and-help)
- [Contributing](#contributing)
- [Used libraries](#used-libraries)
- [Licence](#licence)


## How it works

VCollab works by receiving the output of general purpose vtubing softwares (VTube Studio, VSeeFace, VNyan, Warudo, ...) thanks to the Spout2 output of those softwares. Spout2 allows for no-latency, GPU-side texture sharing across programs.  
Likewise, VCollab own's output is shared as a Spout2 sender, which allows to display this output in softwares like OBS while maintaining pixel-perfect colors and transparency.

Once the model frames are received using Spout2, those are prepared to be sent over the network. To achieve minimal latency, low CPU/GPU usage while preserving a decent compression ratio, the frames are encoded as JPEG images using [libjpeg-turbo](https://github.com/libjpeg-turbo/libjpeg-turbo).  
As the JPEG image format does not support transparency, the alpha layer is sent separately in a simple bit-packed format compressed using [lz4](https://github.com/lz4/lz4) for fast compression and decompression. A compute shader is responsible of generating this bit-packed alpha layer from the model texture, you can see the code for it [there](./VCollab/Utils/Graphics/Compute/AlphaPackerShader.hlsl).

To send data over the network, VCollab use a simple custom network protocol built on top of [LiteNetLib](https://github.com/RevenantX/LiteNetLib/). Important information packets like handshakes and state managements are sent using the Reliable delivery method while frame data packets are sent using the unordered Unreliable delivery method. Since the data contained in one frame is too big to be sent in one single network packet, this data is split into multiple chunks that are then merged back on the receiver side. This means that frame data can arrive in any order, allowing minimal overhead and frame skipping if network is lagging behind or packets are lost in transit.

VCollab itself is built as an [osu!Framework](https://github.com/ppy/osu-framework) game. This allows to provide simple abstractions for general sprite displaying and UI elements while still being able to access and write low-level GPU commands by giving access to the inner Veldrid graphics device. One other advantage of using osu!Framework is that it comes with plenty of useful debugging tools, you can see the keybindings to use those on [this wiki page](https://github.com/ppy/osu-framework/wiki/Framework-Key-Bindings).

## Building VCollab

A lot of changes had to be made to some dependencies of VCollab (especially to make it trim-compatible by default). All of those changes can be seen in the different forks made in the [VCollab-Net](https://github.com/VCollab-Net) GitHub organization. To simplify the build process of VCollab, those libraries are included as submodules in this repository, so make sure to include them when cloning the code!

Build instructions are as following:
- Make sure you have [.Net 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed
- Clone the repository using the `git clone --recursive` command
- Open a terminal in the `VCollab` directory
- Build the solution using `dotnet build` command
- You can find the generated executable in `VCollab\bin\Debug\net9.0\VCollab.exe`

Alternatively, you can build VCollab using any C# IDE of your choice (Visual Studio, Rider, Visual Studio Code, ...).

To make a release build, use the `dotnet publish -c Release` command in the VCollab project directory (the one with `VCollab.csproj`). This will generate a build optimized for performances and size by trimming all the used assemblies. By default the output is generated in `VCollab\bin\Release\net9.0\win-x64\publish`.

## Bug report and help

VCollab is still in early access and may contain a lot of bugs and issues. If you happen to find one, please open an issue here or send me a message on the [VCollab Discord server](https://vcollab.net/discord). As of now, a blue circle button is present on the right side of the screen, this button allows you to send the log files to the VCollab developers, which will help them fix the issue. Another way to get those log files is to look in the `%appdata%\VCollab\logs` directory.

## Contributing

Progress on VCollab is made at a fast pace for now so contributions to the code are welcome but may be refused if they end up clashing with another feature that was planned or do not follow the structure of VCollab's architecture. If you want to make significant contributions to the project, it's better to contact the developers on the [Discord server](https://vcollab.net/discord) first.

## Used libraries

VCollab wouldn't exist without those awesome libraries and tools:
- [osu!Framework](https://github.com/ppy/osu-framework) - *licensed under the MIT license*
- [Spout2](https://github.com/leadedge/Spout2) - *licensed under the BSD-2-Clause license*
- [LiteNetLib](https://github.com/RevenantX/LiteNetLib/) - *licensed under the MIT license*
- [MemoryPack](https://github.com/Cysharp/MemoryPack) - *licensed under the MIT license*
- [K4os.Compression.LZ4](https://github.com/MiloszKrajewski/K4os.Compression.LZ4) - *licensed under the MIT license*
- [TextCopy](https://github.com/CopyText/TextCopy) - *licensed under the MIT license*
- [DiscordRichPresence](https://github.com/Lachee/discord-rpc-csharp) - *licensed under the MIT license*
- [Humanizer](https://github.com/Humanizr/Humanizer) - *licensed under the MIT license*

## Licence

*This software is licensed under the [MIT license](LICENSE), you can modify and redistribute it freely till you respect the respective [Used libraries](#used-libraries) licenses*