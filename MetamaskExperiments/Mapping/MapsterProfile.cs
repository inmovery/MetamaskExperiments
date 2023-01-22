namespace MetamaskExperiments.Mapping;

public class MapsterProfile : IRegister
{
	public void Register(TypeAdapterConfig config)
	{
		config.NewConfig<TransactionReceipt, SummaryTransactionResponse>()
			.Map(dest => dest.From, src => src.From)
			.Map(dest => dest.To, src => src.To)
			.Map(dest => dest.CumulativeGasUsed, src => Web3.Convert.FromWei(src.CumulativeGasUsed, 18))
			.Map(dest => dest.EffectiveGasPrice, src => Web3.Convert.FromWei(src.EffectiveGasPrice, 18))
			.Map(dest => dest.GasUsed, src => Web3.Convert.FromWei(src.GasUsed, 18));
	}
}