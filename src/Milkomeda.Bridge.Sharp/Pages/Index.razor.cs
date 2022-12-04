using System.Linq;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Milkomeda.Bridge.Sharp.Models;
using Milkomeda.Bridge.Sharp.Services;

namespace Milkomeda.Bridge.Sharp.Pages;

partial class Index
{
    [Inject]
    public MilkomedaService? MilkomedaService { get; set; }

    [Inject]
    public ILocalStorageService? LocalStorage { get; set; }

    [Inject]
    public IConfiguration? Configuration { get; set; }

    private StargateResponse? MilkomedaStargateData { get; set; }
    private ICollection<CardanoAsset> BridgeAssets { get; set; } = new List<CardanoAsset>();
    private string? MilkomedaAddress { get; set; }
    private CardanoAsset SelectedCardanoAsset { get; set; } = new(
        "",
        "0x0000000000000000000000000000000000000000000000000000000000000000",
        "Cardano ADA",
        "ADA"
    );

    private bool IsMilkomedaConnected => !string.IsNullOrEmpty(MilkomedaAddress);
    private bool IsLoading { get; set; }
    private Dictionary<CardanoAsset, decimal> UserAssetBalances { get; set; } = new Dictionary<CardanoAsset, decimal>();
    private decimal AmountToSend { get; set; }
    private string? TargetAddress { get; set; }

    protected override async Task OnInitializedAsync()
    {

        IsLoading = true;
        await InvokeAsync(StateHasChanged);
        ArgumentNullException.ThrowIfNull(MilkomedaService);
        ArgumentNullException.ThrowIfNull(LocalStorage);

        // Event Handlers
        MilkomedaService.SelectedAccountChanged += OnSeletedMilkomedaAccountChanged;

        // Default Token is ADA
        BridgeAssets.Add(SelectedCardanoAsset);

        // Load ERC20 Metadata and rerender UI
        MilkomedaStargateData = await MilkomedaService.GetStargateDataAsync();
        if (MilkomedaStargateData is not null && MilkomedaStargateData.Assets is not null)
        {
            foreach (StargateAsset asset in MilkomedaStargateData.Assets)
            {
                string contractAddress = $"0x{asset.IdMilkomeda}";
                BridgeAssets.Add(new(
                    asset.IdCardano,
                    $"0x{asset.IdMilkomeda}",
                    await MilkomedaService.GetErc20NameAsync(contractAddress),
                    await MilkomedaService.GetErc20SymbolAsync(contractAddress)
                ));
            }
        }

        // Check local storage and rerender UI
        if (await LocalStorage.GetItemAsync<bool>("isMilkomedaConnected"))
            await OnBtnConnectMilkomedaClickedAsync();


        IsLoading = false;
        await InvokeAsync(StateHasChanged);

        await base.OnInitializedAsync();
    }

    private async Task OnSeletedMilkomedaAccountChanged(string address)
    {
        IsLoading = true;
        await InvokeAsync(StateHasChanged);
        ArgumentNullException.ThrowIfNull(MilkomedaService);
        MilkomedaAddress = address;
        // Update Balances if connected to wallet, rerender iteration.
        if (!string.IsNullOrEmpty(MilkomedaAddress))
        {
            foreach (CardanoAsset bridgeAsset in BridgeAssets)
            {
                // This is ADA asset
                if (bridgeAsset.MainchainId == string.Empty)
                {
                    UserAssetBalances.Add(bridgeAsset, await MilkomedaService.GetMilkomedaBalanceAsync(MilkomedaAddress));
                }
                else
                {
                    UserAssetBalances.Add(bridgeAsset, await MilkomedaService.GetErc20BalanceAsync(bridgeAsset.SidechainId, MilkomedaAddress));
                }
            }
        }
        IsLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnBtnConnectMilkomedaClickedAsync()
    {
        ArgumentNullException.ThrowIfNull(LocalStorage);
        if (!IsMilkomedaConnected)
        {
            ArgumentNullException.ThrowIfNull(MilkomedaService);
            MilkomedaAddress = await MilkomedaService.ConnectWalletAsync();
            await LocalStorage.SetItemAsync<bool>("isMilkomedaConnected", true);
        }
        else
        {
            MilkomedaAddress = string.Empty;
            await LocalStorage.SetItemAsync<bool>("isMilkomedaConnected", false);
            UserAssetBalances.Clear();
        }
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnTransferClickedAsync()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Configuration);
            ArgumentNullException.ThrowIfNull(MilkomedaAddress);
            ArgumentNullException.ThrowIfNull(MilkomedaService);
            ArgumentNullException.ThrowIfNull(TargetAddress);
            string? bridgeAddress = Configuration["MilkomedaEvmBridgeAddress"];
            if (bridgeAddress is not null)
                if (SelectedCardanoAsset.MainchainId == string.Empty)
                    await MilkomedaService.UnwrapMilkAdaAsync(
                        bridgeAddress,
                        MilkomedaAddress,
                        TargetAddress,
                        AmountToSend
                    );
                else
                {
                    await MilkomedaService.ApprovaErc20Async(
                        SelectedCardanoAsset.SidechainId,
                        MilkomedaAddress,
                        bridgeAddress,
                        AmountToSend
                    );

                    int decimals = await MilkomedaService.GetErc20DecimalsAsync(SelectedCardanoAsset.SidechainId);

                    await MilkomedaService.UnwrapMilkTokenAsync(
                        bridgeAddress,
                        MilkomedaAddress,
                        TargetAddress,
                        SelectedCardanoAsset.MainchainId,
                        AmountToSend,
                        decimals
                    );
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

}