namespace CoinFlip
{
	public enum EExchangeStatus
	{
		Offline = 1 << 0,
		Connected = 1 << 2,
		Stopped = 1 << 3,
		Simulated = 1 << 4,
		Error = 1 << 16,
	}
}
