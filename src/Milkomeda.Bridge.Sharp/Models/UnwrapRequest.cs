

using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Milkomeda.Bridge.Sharp.Models;

public record UnwrapRequest
{
    [Parameter("bytes32", "assetId", 1)]
    public byte[]? AssetId { get; set; }

    [Parameter("address", "from", 2)]
    public string? From { get; set; }

    [Parameter("bytes", "to", 3)]
    public byte[]? To { get; set; }

    [Parameter("uint256", "amount", 3)]
    public BigInteger? Amount { get; set; }
}