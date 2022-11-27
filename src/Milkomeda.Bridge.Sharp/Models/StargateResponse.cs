using System.Text.Json.Serialization;

namespace Milkomeda.Bridge.Sharp.Models;

public record StargateResponse
{

    [JsonPropertyName("current_address")]
    public string? CurrentAddress { get; init; }

    [JsonPropertyName("ttl_expiry")]
    public ulong? TtlExpiry { get; init; }

    [JsonPropertyName("assets")]
    public IEnumerable<StargateAsset>? Assets { get; set; }
}