using System.Text.Json.Serialization;
using VCollab.Signaling.Shared;

namespace VCollab.Utils;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(VCollabSettings))]
[JsonSerializable(typeof(NatRequestData))]
[JsonSerializable(typeof(NatIntroductionData))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{

}