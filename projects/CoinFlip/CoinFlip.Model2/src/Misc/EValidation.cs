using System;

namespace CoinFlip
{
	/// <summary>Trade validation</summary>
	[Flags]
	public enum EValidation
	{
		Valid               = 0,
		VolumeInOutOfRange  = 1 << 0,
		VolumeOutOutOfRange = 1 << 1,
		PriceOutOfRange     = 1 << 2,
		InsufficientBalance = 1 << 3,
		PriceIsInvalid      = 1 << 4,
		VolumeInIsInvalid   = 1 << 5,
		VolumeOutIsInvalid  = 1 << 6,
	}
}
