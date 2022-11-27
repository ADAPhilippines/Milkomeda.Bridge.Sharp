namespace Milkomeda.Bridge.Sharp.Models;

public record CardanoAsset(
    string MainchainId,
    string SidechainId,
    string Name,
    string Symbol
);