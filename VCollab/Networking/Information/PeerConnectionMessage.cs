using MemoryPack;

namespace VCollab.Networking.Information;

[MemoryPackable]
public partial record PeerConnectionMessage(string Name) : IInformationMessage;