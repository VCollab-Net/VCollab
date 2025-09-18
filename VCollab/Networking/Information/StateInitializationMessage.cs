using MemoryPack;

namespace VCollab.Networking.Information;

[MemoryPackable]
public partial record StateInitializationMessage(
    byte ChannelOffset,
    PeerInfo[] PeerInfos
) : IInformationMessage;