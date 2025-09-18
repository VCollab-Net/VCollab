using MemoryPack;

namespace VCollab.Networking;

[MemoryPackable]
public partial record PeerInfo(int ChannelOffset, string Name);