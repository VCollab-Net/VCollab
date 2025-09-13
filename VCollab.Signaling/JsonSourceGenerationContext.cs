using System.Text.Json.Serialization;
using VCollab.Signaling.Shared;

namespace VCollab.Signaling;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(NatIntroductionData))]
[JsonSerializable(typeof(NatRequestData))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext
{

}