using System;
using System.Threading.Tasks;
using Cryptopia.API.DataObjects;

namespace CoinFlip
{
	public enum ETradeType
	{
		/// <summary>Convert Base currency to Quote currency</summary>
		Buy,

		/// <summary>Convert Quote currency to Base currency</summary>
		Sell,
	}

	/// <summary>The connection status of the exchange</summary>
	[Flags] public enum EStatus
	{
		Offline    = 1 << 0,
		Connecting = 1 << 1,
		Connected  = 1 << 2,
		Updating   = 1 << 4,
		Error      = 1 << 16,
	}

	public static class Misc
	{
		/// <summary>Helper for task no-ops</summary>
		public static readonly Task CompletedTask = Task.FromResult(false);

		/// <summary>Convert a trade type string to the enumeration value</summary>
		public static ETradeType TradeType(string trade_type)
		{
			var tt = trade_type.ToLowerInvariant();
			if (tt == "buy") return ETradeType.Buy;
			if (tt == "sell") return ETradeType.Sell;
			throw new Exception("Unknown trade type string");
		}

		/// <summary>Convert a Poloniex trade type to the enumeration value</summary>
		public static ETradeType TradeType(global::Poloniex.API.EOrderType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Poloniex.API.EOrderType.Buy: return ETradeType.Buy;
			case global::Poloniex.API.EOrderType.Sell: return ETradeType.Sell;
			}
		}

		/// <summary>Convert this trade type to the Cryptopia definition of a trade type</summary>
		public static TradeType ToCryptopiaTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Buy: return global::Cryptopia.API.DataObjects.TradeType.Buy;
			case ETradeType.Sell: return global::Cryptopia.API.DataObjects.TradeType.Sell;
			}
		}
	}
}
