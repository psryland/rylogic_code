using Rylogic.Utility;

namespace CoinFlip
{
	public interface IValueTotalAvail
	{
		/// <summary>Value of the coin (probably in USD)</summary>
		decimal Value { get; }

		/// <summary>The total amount of the coin (in coin currency)</summary>
		Unit<decimal> Total { get; }

		/// <summary>The available amount of the coin (in coin currency)</summary>
		Unit<decimal> Available { get; }
	}
}
