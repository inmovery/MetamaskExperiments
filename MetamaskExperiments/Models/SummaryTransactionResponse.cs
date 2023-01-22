namespace MetamaskExperiments.Models;

public class SummaryTransactionResponse
{
	public bool IsSucceeded { get; set; }

	public string? From { get; set; }

	public string? To { get; set; }

	public decimal? CumulativeGasUsed { get; set; }

	public decimal? GasUsed { get; set; }

	public decimal? EffectiveGasPrice { get; set; }
}