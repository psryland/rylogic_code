using System;

namespace Bittrex.API
{
	/// <summary>Global functions</summary>
	internal static class Conv
	{
		/// <summary>Parse the buy/sell string</summary>
		public static EOrderSide ToOrderType(string order_type)
		{
			switch (order_type.ToLowerInvariant())
			{
			default: throw new Exception($"Unknown order type: {order_type}");
			case "limit_buy":  return EOrderSide.Buy;
			case "buylimit":   return EOrderSide.Buy;
			case "limit_sell": return EOrderSide.Sell;
			case "selllimit":  return EOrderSide.Sell;
			}
		}
		public static string ToString(EOrderSide order_type)
		{
			switch (order_type)
			{
			default: throw new Exception($"Unknown order type: {order_type}");
			case EOrderSide.Buy: return "buylimit";
			case EOrderSide.Sell: return "selllimit";
			}
		}
	}
}
