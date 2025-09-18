using MemoryPack;

namespace VCollab.Networking.Information;

[MemoryPackable]
public partial record StateInitializationMessage(
    int ChannelOffset,
    PeerInfo[] PeerInfos
) : IInformationMessage;