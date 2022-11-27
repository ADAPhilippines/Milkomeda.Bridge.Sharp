namespace Milkomeda.Bridge.Sharp.Models;

public record StargateAdaPayload(
    string MinLovelace,
    string FromAdaFeeLovelace,
    string ToAdaFeeGWei
);