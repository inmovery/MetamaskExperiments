namespace MetamaskExperiments.Dto;

public readonly struct TransferDetailsDto
{
	public WalletDetailsDto SenderWallet { get; init; }

	public string? ReceiverAddress { get; init; }

	public decimal Amount { get; init; }
}