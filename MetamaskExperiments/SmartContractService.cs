namespace MetamaskExperiments;

public class SmartContractService
{
	public async Task<T2> ContractQuery<T1, T2>(IWeb3 web3, string contractAddress, T1 functionMessage = null) where T1 : FunctionMessage, new()
	{
		var result = await web3.Eth.GetContractQueryHandler<T1>().QueryAsync<T2>(contractAddress, functionMessage);
		return result;
	}

	public async Task<TransactionReceipt> ContractSendRequestAndWaitForReceipt<T>(IWeb3 web3, string contractAddress, T functionMessage, bool escalateGas = true) where T : FunctionMessage, new()
	{
		var handler = web3.Eth.GetContractTransactionHandler<T>();

		if (escalateGas)
		{
			await EstimateGas(web3, contractAddress, functionMessage);
			IncreaseGas(); // Give the gas a boost upfront as it tends to fail with the estimated gas.

			for (var i = 0; i < 2; i++)
			{
				try
				{
					var transactionReceipt = await SendRequestAndWaitForReceiptAsync();
					if (transactionReceipt.Failed())
					{
						IncreaseGas();
						continue;
					}

					return transactionReceipt;
				}
				catch (RpcResponseException e)
				{
					if (e.Message.Contains("gas allowance"))
					{
						IncreaseGas();
					}
					else
					{
						throw;
					}
				}
			}

			void IncreaseGas() => functionMessage.Gas = (functionMessage.Gas.GetValueOrDefault() * (Rational)1.2).WholePart;
		}

		return await SendRequestAndWaitForReceiptAsync();

		async Task<TransactionReceipt> SendRequestAndWaitForReceiptAsync()
		{
			return await handler.SendRequestAndWaitForReceiptAsync(contractAddress, functionMessage);
		}
	}

	private async Task EstimateGas<T>(IWeb3 web3, string contractAddress, T functionMessage) where T : FunctionMessage, new()
	{
		try
		{
			//TODO: Convert to ??= operator when this issue is fixed: https://github.com/dotnet/roslyn/issues/49148
			if (functionMessage.Gas is null)
			{
				functionMessage.Gas = await web3.Eth.GetContractTransactionHandler<T>().EstimateGasAsync(contractAddress, functionMessage);
			}
		}
		catch (SmartContractRevertException e)
		{
			throw;
		}
	}
}