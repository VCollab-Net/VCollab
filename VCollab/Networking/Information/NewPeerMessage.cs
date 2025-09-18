using MemoryPack;

namespace VCollab.Networking.Information;

[MemoryPackable]
public partial record NewPeerMessage(
    int ChannelOffset,
    string Name
): IInformationMessage;