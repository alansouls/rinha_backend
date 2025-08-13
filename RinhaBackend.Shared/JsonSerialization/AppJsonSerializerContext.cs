using System.Text.Json.Serialization;
using RinhaBackend.Shared.Domain.Inbox;
using RinhaBackend.Shared.Domain.Outbox;
using RinhaBackend.Shared.Domain.Payments;
using RinhaBackend.Shared.Dtos.Payments;
using RinhaBackend.Shared.Dtos.Payments.Requests;
using RinhaBackend.Shared.Dtos.Payments.Responses;
using RinhaBackend.Shared.ThirdParty.Dtos;

namespace RinhaBackend.Shared.JsonSerialization;

[JsonSerializable(typeof(OutboxMessage))]
[JsonSerializable(typeof(InboxMessage))]
[JsonSerializable(typeof(PostPaymentDto))]
[JsonSerializable(typeof(PaymentSummaryDto))]
[JsonSerializable(typeof(PaymentLog))]
[JsonSerializable(typeof(ServiceHealthResponseDto))]
[JsonSerializable(typeof(ThirdPartyPostPaymentDto))]
[JsonSerializable(typeof(PaymentUpdated))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}