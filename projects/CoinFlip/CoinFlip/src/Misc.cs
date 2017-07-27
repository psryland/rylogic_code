using System;
using System.Drawing;
using System.Threading.Tasks;
using CoinFlip.Properties;
using Cryptopia.API.DataObjects;
using Poloniex.API;

namespace CoinFlip
{
	public enum ETradeType
	{
		/// <summary>Quote->Base, Buy, Bid, Short</summary>
		Q2B,

		/// <summary>Base->Quote, Sell, Ask, Long</summary>
		B2Q,
	}

	/// <summary>The connection status of the exchange</summary>
	[Flags] public enum EStatus
	{
		Offline    = 1 << 0,
		Connecting = 1 << 1,
		Connected  = 1 << 2,
		Stopped    = 1 << 3,
		Error      = 1 << 16,
	}

	public static class Misc
	{
		/// <summary>Helper for task no-ops</summary>
		public static readonly Task CompletedTask = Task.FromResult(false);

		/// <summary>Convert a trade type string to the enumeration value</summary>
		public static ETradeType TradeType(string trade_type)
		{
			switch (trade_type.ToLowerInvariant()) {
			case "q2b": case "buy": case "bid": case "short": return ETradeType.Q2B;
			case "b2q": case "sell": case "ask": case "long": return ETradeType.B2Q;
			}
			throw new Exception("Unknown trade type string");
		}

		/// <summary>Convert a Cryptopia trade type to the enumeration value</summary>
		public static ETradeType TradeType(TradeType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Cryptopia.API.DataObjects.TradeType.Buy: return ETradeType.Q2B;
			case global::Cryptopia.API.DataObjects.TradeType.Sell: return ETradeType.B2Q;
			}
		}

		/// <summary>Convert a Poloniex trade type to the enumeration value</summary>
		public static ETradeType TradeType(EOrderType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case EOrderType.Buy: return ETradeType.Q2B;
			case EOrderType.Sell: return ETradeType.B2Q;
			}
		}

		/// <summary>Convert this trade type to the Cryptopia definition of a trade type</summary>
		public static TradeType ToCryptopiaTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Cryptopia.API.DataObjects.TradeType.Buy;
			case ETradeType.B2Q: return global::Cryptopia.API.DataObjects.TradeType.Sell;
			}
		}

		/// <summary>Convert this trade type to the Poloniex definition of a trade type</summary>
		public static EOrderType ToPoloniexTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return EOrderType.Buy;
			case ETradeType.B2Q: return EOrderType.Sell;
			}
		}
	}

	public static class Res
	{
		public static readonly Image Active   = new Bitmap(Resources.active, new Size(28,28));
		public static readonly Image Inactive = new Bitmap(Resources.inactive, new Size(28,28));
	}
}
