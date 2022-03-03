using System;

namespace Poloniex.API
{
	/// <summary>Global functions</summary>
	internal static class Conv
	{
		/// <summary>Parse the buy/sell string</summary>
		public static EOrderSide ToOrderType(string value)
		{
			switch (value) {
			default: throw new ArgumentOutOfRangeException("value");
			case "buy": return EOrderSide.Buy;
			case "bid": return EOrderSide.Buy;
			case "sell": return EOrderSide.Sell;
			case "ask": return EOrderSide.Sell;
			}
		}
		public static string ToString(EOrderSide order_type)
		{
			switch (order_type) {
			default: throw new ArgumentException("order_type");
			case EOrderSide.Buy: return "buy";
			case EOrderSide.Sell: return "sell";
			}
		}
	}
}
