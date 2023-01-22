namespace MetamaskExperiments.Dto;

public readonly struct WalletDetailsDto
{
	/// <summary>
	/// Адрес кошелька
	/// </summary>
	public string? Address { get; init; }

	/// <summary>
	/// Приватный ключ кошелька
	/// </summary>
	public string? PrivateKey { get; init; }
}