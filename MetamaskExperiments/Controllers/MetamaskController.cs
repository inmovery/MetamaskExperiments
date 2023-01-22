namespace MetamaskExperiments.Controllers;

[ApiController]
[Route("[controller]")]
public class MetamaskController : ControllerBase
{
	private readonly ILogger<MetamaskController> _logger;
	private readonly IMapper _mapper;

	private const string KcalContractAddress = "0x68b2DFC494362AAE300F2C401019205d8960226b";
	private const string FitfiContractAddress = "0xb58a9d5920af6ac1a9522b0b10f55df16686d1b6";

	private const string StepAppRpc = "https://rpc.step.network";
	private const int Decimals = 18;
	private static readonly BigInteger ChainId = new(1234);

	public MetamaskController(ILogger<MetamaskController> logger, IMapper mapper)
	{
		_logger = logger;
		_mapper = mapper;
	}

	[HttpGet]
	[Route("transfer")]
	public async Task<IActionResult> Transfer([FromBody] TransferDetailsDto transferDetails, Token token)
	{
		var web3 = ConfigureWeb3(transferDetails.SenderWallet.PrivateKey);

		TransactionReceipt? transactionResult = default;
		switch (token)
		{
			case Token.Fitfi:
				transactionResult = await TransferFitfiTokenAsync(web3, transferDetails.SenderWallet.Address,
					transferDetails.ReceiverAddress, transferDetails.Amount);
				break;
			case Token.Kcal:
				transactionResult = await TransferKcalTokenAsync(web3, transferDetails.SenderWallet.Address,
					transferDetails.ReceiverAddress, transferDetails.Amount);
				break;
		}

		if (transactionResult is null)
			return BadRequest();

		var transactionHash = transactionResult.TransactionHash;
		while (await IsTransactionPendingAsync(web3, transactionHash))
			_logger.LogInformation("Waiting for new transaction hash succeed: {transactionHash}", transactionHash);


		var summaryTransactionResponse = _mapper.Map<SummaryTransactionResponse>(transactionResult);
		summaryTransactionResponse.IsSucceeded = await IsTransactionSucceedAsync(web3, transactionHash);

		return Ok(summaryTransactionResponse);
	}

	[HttpGet]
	[Route("GetBalance")]
	public async Task<IActionResult> GetBalance([FromBody] WalletDetailsDto request)
	{
		var isRequestNotSuitable = string.IsNullOrEmpty(request.Address) || string.IsNullOrEmpty(request.PrivateKey);
		if (isRequestNotSuitable)
			return BadRequest();

		var web3 = ConfigureWeb3(request.PrivateKey);
		var balance = await GetSummaryBalanceAsync(web3, request.Address);

		return Ok(balance);
	}

	/// <summary>
	/// Перевод токена KCAL с одного аккаунта на другой
	/// </summary>
	/// <param name="web3">Экземляр класса Web3</param>
	/// <param name="senderAddress">Адрес, с которого осуществляется перевод</param>
	/// <param name="receiverAddress">Адрес, на который осуществляется</param>
	/// <param name="amount">Количество токена</param>
	/// <returns>Результат операции перевода</returns>
	private async Task<TransactionReceipt> TransferKcalTokenAsync(IWeb3 web3, string? senderAddress, string? receiverAddress, decimal amount)
	{
		if (string.IsNullOrEmpty(senderAddress) || string.IsNullOrEmpty(receiverAddress))
			return new TransactionReceipt();

		var contractQueryHandler = web3.Eth.GetContractQueryHandler<AllowanceFunction>();
		var allowanceFunction = new AllowanceFunction
		{
			Owner = senderAddress,
			Spender = KcalContractAddress
		};

		var allowed = await contractQueryHandler.QueryAsync<BigInteger>(KcalContractAddress, allowanceFunction);
		if (allowed.IsZero)
		{
			var contractTransactionHandler = web3.Eth.GetContractTransactionHandler<ApproveFunction>();
			var approveFunction = new ApproveFunction
			{
				FromAddress = senderAddress,
				Spender = KcalContractAddress,
				Value = (BigInteger.One << 256) - 1,
			};

			await contractTransactionHandler.SendRequestAsync(KcalContractAddress, approveFunction);
		}

		var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
		var transferFunction = new TransferFunction
		{
			To = receiverAddress,
			Value = Web3.Convert.ToWei(amount, decimalPlacesFromUnit: Decimals)
		};

		var transferReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(KcalContractAddress, transferFunction);

		return transferReceipt;
	}

	/// <summary>
	/// Перевод токена FITFI с одного аккаунта на другой
	/// </summary>
	/// <param name="web3">Экземляр класса Web3</param>
	/// <param name="senderAddress">Адрес, с которого осуществляется перевод</param>
	/// <param name="receiverAddress">Адрес, на который осуществляется</param>
	/// <param name="amount">Количество токена</param>
	/// <returns>Результат операции перевода</returns>
	private async Task<TransactionReceipt> TransferFitfiTokenAsync(IWeb3 web3, string? senderAddress, string? receiverAddress, decimal amount)
	{
		if (string.IsNullOrEmpty(senderAddress) || string.IsNullOrEmpty(receiverAddress))
			return new TransactionReceipt();

		var wei = Web3.Convert.ToWei(amount);
		var transactionInput = new TransactionInput
		{
			From = senderAddress,
			To = receiverAddress,
			Value = new HexBigInteger(wei)
		};

		var transactionReceipt = await web3.TransactionManager.SendTransactionAndWaitForReceiptAsync(transactionInput);

		return transactionReceipt;
	}

	private async Task<bool> IsTransactionPendingAsync(IWeb3 web3, string transactionHash)
	{
		var transactionReceipt = await web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionHash);
		_logger.LogInformation("Pending: {transactionBlockNumber}", transactionReceipt.BlockNumber);

		return transactionReceipt.BlockNumber == null;
	}

	private async Task<bool> IsTransactionSucceedAsync(IWeb3 web3, string transactionHash)
	{
		var transactionReceipt = await web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionHash);
		return transactionReceipt.Succeeded();
	}

	/// <summary>
	/// Конфигурирование экземляра класса Web3
	/// </summary>
	/// <param name="privateKey">Приватный ключ кошелька</param>
	/// <returns>Экземляр класса Web3</returns>
	private IWeb3 ConfigureWeb3(string? privateKey)
	{
		var account = new Account(privateKey, ChainId);
		var web3 = new Web3(account, StepAppRpc)
		{
			TransactionManager =
			{
				UseLegacyAsDefault = true,
				//EstimateOrSetDefaultGasIfNotSet = true,
				//CalculateOrSetDefaultGasPriceFeesIfNotSet = true,
			}
		};

		return web3;
	}

	/// <summary>
	/// Получение Decimals определённого контракта
	/// </summary>
	/// <param name="web3">Экземляр класс Web3</param>
	/// <param name="contractAddress">Адрес контракта (токена)</param>
	/// <returns>Значение Decimals для определённого ChainId с соответствующим контрактом (токеном)</returns>
	private async Task<byte> GetDecimalAsync(IWeb3 web3, string contractAddress)
	{
		var contractHandler = web3.Eth.GetContractHandler(contractAddress);

		var decimalsFunction = new DecimalsFunction();
		var decimals = await contractHandler.QueryAsync<DecimalsFunction, byte>(decimalsFunction);

		return decimals;
	}

	/// <summary>
	/// Получение названия токена по контракту (адресу)
	/// </summary>
	/// <param name="web3">Экземляр класс Web3</param>
	/// <param name="contractAddress">Адрес контракта (токена)</param>
	/// <returns>Название токена</returns>
	private async Task<string> GetTokenNameAsync(IWeb3 web3, string contractAddress = FitfiContractAddress)
	{
		var contractHandler = web3.Eth.GetContractHandler(contractAddress);

		var nameFunction = new NameFunction();
		var tokenName = await contractHandler.QueryAsync<NameFunction, string>(nameFunction);

		return tokenName;
	}

	/// <summary>
	/// Получение ChainId по определённому RPC клиенту
	/// </summary>
	/// <param name="web3">"Экземляр класса Web3</param>
	/// <returns>ChainId</returns>
	private async Task<HexBigInteger> GetChainIdAsync(IWeb3 web3)
	{
		return await web3.Eth.ChainId.SendRequestAsync();
	}

	/// <summary>
	/// Получение общего баланса в соответствии с доступными токенами сети StepApp
	/// </summary>
	/// <param name="web3">Экземляр класса работы с Web3</param>
	/// <param name="address">Адрес кошелька</param>
	/// <returns>Объект баланса токенов KCAL и FITFI</returns>
	private async Task<Balance> GetSummaryBalanceAsync(IWeb3 web3, string? address)
	{
		var kcalBalance = await GetKcalBalanceAsync(web3, address);
		var fitfiBalance = await GetFitfiBalanceAsync(web3, address);

		var summaryBalance = new Balance
		{
			Kcal = kcalBalance,
			Fitfi = fitfiBalance
		};

		return summaryBalance;
	}

	/// <summary>
	/// Получение баланса токена KCAL
	/// </summary>
	/// <param name="web3">Экземляр класса работы с Web3</param>
	/// <param name="address">Адрес кошелька</param>
	private async Task<decimal> GetKcalBalanceAsync(IWeb3 web3, string? address)
	{
		var balanceOfFunctionMessage = new BalanceOfFunction
		{
			Owner = address,
		};

		var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
		var balance = await balanceHandler.QueryAsync<BigInteger>(KcalContractAddress, balanceOfFunctionMessage);

		var kcalAmount = Web3.Convert.FromWei(balance, 18);

		return kcalAmount;
	}

	/// <summary>
	/// Получение баланса токена FITFI
	/// </summary>
	/// <param name="web3">Экземляр класса работы с Web3</param>
	/// <param name="address">Адрес кошелька</param>
	private async Task<decimal> GetFitfiBalanceAsync(IWeb3 web3, string? address)
	{
		var balance = await web3.Eth.GetBalance.SendRequestAsync(address);
		var fitfiAmount = Web3.Convert.FromWei(balance.Value, 18);

		return fitfiAmount;
	}
}