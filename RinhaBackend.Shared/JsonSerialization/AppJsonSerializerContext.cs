using System.Text.Json.Serialization;
using RinhaBackend.Shared.Models;

namespace RinhaBackend.Shared.JsonSerialization;

[JsonSerializable(typeof(Todo[]))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}