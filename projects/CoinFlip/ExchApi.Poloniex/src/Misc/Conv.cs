using System;

namespace Poloniex.API
{
	/// <summary>Global functions</summary>
	internal static class Conv
	{
		/// <summary>Parse the buy/sell string</summary>
		public static EOrderType ToOrderType(string value)
		{
			switch (value) {
			default: throw new ArgumentOutOfRangeException("value");
			case "buy": return EOrderType.Buy;
			case "bid": return EOrderType.Buy;
			case "sell": return EOrderType.Sell;
			case "ask": return EOrderType.Sell;
			}
		}
		public static string ToString(EOrderType order_type)
		{
			switch (order_type) {
			default: throw new ArgumentException("order_type");
			case EOrderType.Buy: return "buy";
			case EOrderType.Sell: return "sell";
			}
		}
	}
}
