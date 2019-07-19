using System;
using Rylogic.Utility;

namespace CoinFlip
{
	public interface IOrder
	{
		/// <summary>Unique Id per order</summary>
		long OrderId { get; }

		/// <summary>The trade type</summary>
		ETradeType TradeType { get; }

		/// <summary>The price to make the trade at (Quote/Base)</summary>
		Unit<double> PriceQ2B { get; }

		/// <summary>When the order was created (null if never created)</summary>
		DateTimeOffset? Created { get; }

		/// <summary>String description of the order</summary>
		string Description { get; }
	}
}
