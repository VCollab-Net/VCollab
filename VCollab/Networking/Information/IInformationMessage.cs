using MemoryPack;

namespace VCollab.Networking.Information;

[MemoryPackable]
[MemoryPackUnion(0, typeof(ErrorMessage))]
[MemoryPackUnion(1, typeof(NewPeerMessage))]
[MemoryPackUnion(2, typeof(StateInitializationMessage))]
[MemoryPackUnion(3, typeof(DisconnectedPeerMessage))]
[MemoryPackUnion(4, typeof(PeerConnectionMessage))]
public partial interface IInformationMessage
{

}