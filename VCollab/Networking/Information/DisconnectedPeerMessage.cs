using MemoryPack;

namespace VCollab.Networking.Information;

[MemoryPackable]
public partial record DisconnectedPeerMessage(
    int ChannelOffset
) : IInformationMessage;