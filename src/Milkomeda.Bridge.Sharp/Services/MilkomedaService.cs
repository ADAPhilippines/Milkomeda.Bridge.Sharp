using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using Conclave.EVM;
using Milkomeda.Bridge.Sharp.Models;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Metamask;
using Nethereum.UI;
using Nethereum.Web3;

namespace Milkomeda.Bridge.Sharp.Services;


public class MilkomedaService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly string _STARGATE_URL;
    private readonly IEthereumHostProvider _ethereumHostProvider;
    private readonly EvmService _evmService;
    private string? Erc20Abi { get; set; }
    private string? SidechainBridgeAbi { get; set; }

    public event Func<string, Task>? SelectedAccountChanged;

    public MilkomedaService(
        IConfiguration configuration,
        HttpClient httpClient,
        MetamaskHostProvider metamaskHostProvider,
        EvmService evmService
    )
    {
        _ethereumHostProvider = metamaskHostProvider;
        _evmService = evmService;
        _configuration = configuration;
        _STARGATE_URL = configuration["StargateUrl"] ?? "https://allowedlist-service.herokuapp.com/v1/stargate";
        _httpClient = httpClient;

        _ethereumHostProvider.SelectedAccountChanged += async (walletAddress) =>
        {
            if (SelectedAccountChanged is not null)
                await SelectedAccountChanged.Invoke(walletAddress);
        };
    }

    public async Task<StargateResponse?> GetStargateDataAsync()
        => await _httpClient.GetFromJsonAsync<StargateResponse>(_STARGATE_URL);

    public async Task<string> ConnectWalletAsync()
    {

        var connectResult = await _ethereumHostProvider.CheckProviderAvailabilityAsync();
        await _ethereumHostProvider.EnableProviderAsync();
        var metamaskWeb3 = await _ethereumHostProvider.GetWeb3Async();
        _evmService.Web3 = metamaskWeb3;
        _evmService.Web3.TransactionManager.UseLegacyAsDefault = true;
        _evmService.Web3.TransactionManager.EstimateOrSetDefaultGasIfNotSet = false;
        _evmService.Web3.TransactionManager.CalculateOrSetDefaultGasPriceFeesIfNotSet = false;

        return _evmService.Web3.TransactionManager.Account != null ? _evmService.Web3.TransactionManager.Account.Address :
            await _ethereumHostProvider.GetProviderSelectedAccountAsync();
    }

    public async Task<decimal> GetErc20BalanceAsync(string contractAddress, string walletAddress)
    {
        (string abi, _) = await LoadAbisAsync();
        LoadEvmServiceWeb3();
        BigInteger balanceInWei = await _evmService.CallContractReadFunctionAsync<BigInteger>(contractAddress, abi, "balanceOf", walletAddress);
        return Web3.Convert.FromWei(balanceInWei, await GetErc20DecimalsAsync(contractAddress));
    }

    public async Task<string> GetErc20NameAsync(string contractAddress)
    {
        (string abi, _) = await LoadAbisAsync();
        LoadEvmServiceWeb3();
        return await _evmService.CallContractReadFunctionAsync<string>(contractAddress, abi, "name");
    }

    public async Task<string> GetErc20SymbolAsync(string contractAddress)
    {
        (string abi, _) = await LoadAbisAsync();
        LoadEvmServiceWeb3();
        return await _evmService.CallContractReadFunctionAsync<string>(contractAddress, abi, "symbol");
    }

    public async Task<int> GetErc20DecimalsAsync(string contractAddress)
    {
        (string abi, _) = await LoadAbisAsync();
        LoadEvmServiceWeb3();
        return await _evmService.CallContractReadFunctionAsync<int>(contractAddress, abi, "decimals");
    }

    public async Task ApprovaErc20Async(string contractAddress, string from, string to, decimal amount)
    {
        (string abi, _) = await LoadAbisAsync();
        LoadEvmServiceWeb3();
        int decimals = await GetErc20DecimalsAsync(contractAddress);
        await _evmService.CallContractWriteFunctionAsync(
            contractAddress,
            from,
            abi,
            0,
            "approve",
            to,
            Web3.Convert.ToWei(amount, decimals)
        );
    }

    public async Task<decimal> GetMilkomedaBalanceAsync(string walletAddress)
    {
        IWeb3 web3 = await _ethereumHostProvider.GetWeb3Async();
        HexBigInteger balanceInWei = await web3.Eth.GetBalance.SendRequestAsync(walletAddress);
        return Web3.Convert.FromWei(balanceInWei);
    }

    public async Task SendBaseTokenAsync(string from, string to, decimal amount, BigInteger gasLimit)
    {
        LoadEvmServiceWeb3();
        await _evmService.SendBaseTokenAsync(amount, from, to, gasLimit);
    }

    public async Task UnwrapMilkAdaAsync(string contractAddress, string from, string to, decimal amount)
    {
        (_, string abi) = await LoadAbisAsync();
        await _evmService.CallContractWriteFunctionAsync(
            contractAddress,
            from,
            abi,
            amount + 1,
            "submitUnwrappingRequest",
            new UnwrapRequest
            {
                AssetId = string.Empty.HexToByteArray(),
                From = from,
                To = System.Text.UTF8Encoding.UTF8.GetBytes(to),
                Amount = Web3.Convert.ToWei(amount)
            }
        );
    }

    public async Task UnwrapMilkTokenAsync(string contractAddress, string from, string to, string assetId, decimal amount, int decimals)
    {
        (_, string abi) = await LoadAbisAsync();
        await _evmService.CallContractWriteFunctionAsync(
            contractAddress,
            from,
            abi,
            4,
            "submitUnwrappingRequest",
            new UnwrapRequest
            {
                AssetId = assetId.HexToByteArray(),
                From = from,
                To = System.Text.UTF8Encoding.UTF8.GetBytes(to),
                Amount = Web3.Convert.ToWei(amount, decimals)
            }
        );
    }

    private byte[] GetBytes(string str)
    {
        byte[] bytes = new byte[str.Length * sizeof(char)];
        System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private async Task<(string, string)> LoadAbisAsync()
    {
        if (Erc20Abi is null)
            Erc20Abi = await _httpClient.GetStringAsync("/abi/ERC20.json");
        if (SidechainBridgeAbi is null)
            SidechainBridgeAbi = await _httpClient.GetStringAsync("/abi/SidechainBridgeV3.json");
        return (Erc20Abi, SidechainBridgeAbi);
    }

    private void LoadEvmServiceWeb3()
    {
        if (_evmService.Web3 is null)
            _evmService.Web3 = new Web3(_configuration["MilkomedaRpcUrl"]);
    }
}