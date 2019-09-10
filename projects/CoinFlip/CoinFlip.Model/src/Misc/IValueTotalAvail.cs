namespace CoinFlip
{
	public interface IValueTotalAvail
	{
		/// <summary>Value of the coin (probably in USD)</summary>
		decimal Value { get; }

		/// <summary>The total amount of the coin (in coin currency)</summary>
		decimal Total { get; }

		/// <summary>The available amount of the coin (in coin currency)</summary>
		decimal Available { get; }
	}
}
