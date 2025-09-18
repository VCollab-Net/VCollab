using MemoryPack;

namespace VCollab.Networking.Information;

[MemoryPackable]
public partial record ErrorMessage(
    ErrorCode ErrorCode,
    string Message = ""
) : IInformationMessage;