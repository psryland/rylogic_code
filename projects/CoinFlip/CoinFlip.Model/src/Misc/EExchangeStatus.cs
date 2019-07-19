using System;

namespace CoinFlip
{
	[Flags]
	public enum EExchangeStatus
	{
		Offline = 0,
		Connected = 1 << 2,
		Stopped = 1 << 3,
		Simulated = 1 << 4,
		PublicAPIOnly = 1 << 5,
		Error = 1 << 16,
	}
}
