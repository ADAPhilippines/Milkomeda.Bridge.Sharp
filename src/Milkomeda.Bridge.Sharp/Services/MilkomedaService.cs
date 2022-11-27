using System.Net.Http.Json;
using Conclave.EVM;
using Milkomeda.Bridge.Sharp.Models;
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
    }

    public async Task<StargateResponse?> GetStargateDataAsync()
        => await _httpClient.GetFromJsonAsync<StargateResponse>(_STARGATE_URL);

    public async Task<string> ConnectWalletAsync()
    {
        await LoadEvmServiceWeb3Async();
        if (_evmService.Web3 is not null)
        {
            _evmService.Web3.TransactionManager.UseLegacyAsDefault = true;
            _evmService.Web3.TransactionManager.EstimateOrSetDefaultGasIfNotSet = false;
            _evmService.Web3.TransactionManager.CalculateOrSetDefaultGasPriceFeesIfNotSet = false;

            return _evmService.Web3.TransactionManager.Account != null ? _evmService.Web3.TransactionManager.Account.Address :
                await _ethereumHostProvider.GetProviderSelectedAccountAsync();
        }
        else throw new NullReferenceException("Web3 is null.");
    }

    public async Task<decimal> GetErc20Balance(string contractAddress, string walletAddress)
    {
        (string abi, _) = await LoadAbisAsync();
        await LoadEvmServiceWeb3Async();
        HexBigInteger balanceInWei = await _evmService.CallContractReadFunctionAsync<HexBigInteger>(contractAddress, abi, "balanceOf", walletAddress);
        return Web3.Convert.FromWei(balanceInWei);
    }

    public async Task<string> GetErc20Name(string contractAddress)
    {
        (string abi, _) = await LoadAbisAsync();
        await LoadEvmServiceWeb3Async();
        return await _evmService.CallContractReadFunctionAsync<string>(contractAddress, abi, "name");
    }

    public async Task<string> GetErc20Symbol(string contractAddress)
    {
        (string abi, _) = await LoadAbisAsync();
        await LoadEvmServiceWeb3Async();
        return await _evmService.CallContractReadFunctionAsync<string>(contractAddress, abi, "symbol");
    }

    public async Task<decimal> GetMilkomedaBalance(string walletAddress)
    {
        IWeb3 web3 = await _ethereumHostProvider.GetWeb3Async();
        HexBigInteger balanceInWei = await web3.Eth.GetBalance.SendRequestAsync(walletAddress);
        return Web3.Convert.FromWei(balanceInWei);
    }

    private async Task<(string, string)> LoadAbisAsync()
    {
        if (Erc20Abi is null)
            Erc20Abi = await _httpClient.GetStringAsync("/abi/ERC20.json");
        if (SidechainBridgeAbi is null)
            SidechainBridgeAbi = await _httpClient.GetStringAsync("/abi/SidechainBridgeV3.json");
        return (Erc20Abi, SidechainBridgeAbi);
    }

    private async Task LoadEvmServiceWeb3Async()
    {
        var connectResult = await _ethereumHostProvider.CheckProviderAvailabilityAsync();
        await _ethereumHostProvider.EnableProviderAsync();

        var metamaskWeb3 = await _ethereumHostProvider.GetWeb3Async();

        if (metamaskWeb3 is null)
            _evmService.Web3 = new Web3(_configuration["MilkomedaRpcUrl"]);
        else
            _evmService.Web3 = metamaskWeb3;
    }
}