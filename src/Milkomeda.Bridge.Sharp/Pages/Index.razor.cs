using System.Linq;
using Microsoft.AspNetCore.Components;
using Milkomeda.Bridge.Sharp.Models;
using Milkomeda.Bridge.Sharp.Services;

namespace Milkomeda.Bridge.Sharp.Pages;

partial class Index
{
    [Inject]
    public MilkomedaService? MilkomedaService { get; set; }
    private StargateResponse? MilkomedaStargateData { get; set; }
    private ICollection<CardanoAsset> BridgeAssets { get; set; } = new List<CardanoAsset>();
    private string? MilkomedaAddress { get; set; }
    private CardanoAsset SelectedCardanoAsset { get; set; } = new(
        "",
        "0x0000000000000000000000000000000000000000000000000000000000000000",
        "Cardano ADA",
        "ADA"
    );

    protected override async Task OnInitializedAsync()
    {
        ArgumentNullException.ThrowIfNull(MilkomedaService);
        BridgeAssets.Add(SelectedCardanoAsset);
        MilkomedaStargateData = await MilkomedaService.GetStargateDataAsync();
        if (MilkomedaStargateData is not null && MilkomedaStargateData.Assets is not null)
        {
            foreach (StargateAsset asset in MilkomedaStargateData.Assets)
            {
                string contractAddress = $"0x{asset.IdMilkomeda}";
                BridgeAssets.Add(new CardanoAsset(
                    asset.IdCardano,
                    $"0x{asset.IdMilkomeda}",
                    await MilkomedaService.GetErc20Name(contractAddress),
                    await MilkomedaService.GetErc20Symbol(contractAddress)
                ));
            }
        }
        await base.OnInitializedAsync();
    }

    private async Task OnBtnConnectMilkomedaClicked()
    {
        if (string.IsNullOrEmpty(MilkomedaAddress))
        {
            ArgumentNullException.ThrowIfNull(MilkomedaService);
            MilkomedaAddress = await MilkomedaService.ConnectWalletAsync();
        }
        else
        {
            MilkomedaAddress = string.Empty;
        }
    }

}