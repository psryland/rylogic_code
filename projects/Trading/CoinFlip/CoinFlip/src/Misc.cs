using System;
using System.Threading;
using System.Threading.Tasks;
using Cryptopia.API.DataObjects;
using Poloniex.API;

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
		Stopped    = 1 << 3,
		Error      = 1 << 16,
	}

	public static class Misc
	{
		/// <summary>Helper for task no-ops</summary>
		public static readonly Task CompletedTask = Task.FromResult(false);

		/// <summary>Check that the current thread is the main thread</summary>
		public static bool AssertMainThread()
		{
			if (Thread.CurrentThread.ManagedThreadId == m_main_thread_id) return true;
			throw new Exception("Cross-Thread call detected");
		}
		internal static int m_main_thread_id;

		/// <summary>Check that the current thread is not access data that might be changing</summary>
		public static bool AssertReadOnly()
		{
			if (m_read_only || AssertMainThread()) return true;
			throw new Exception("Read during non-read only phase");
		}
		internal static bool m_read_only;

		/// <summary>Convert a trade type string to the enumeration value</summary>
		public static ETradeType TradeType(string trade_type)
		{
			var tt = trade_type.ToLowerInvariant();
			if (tt == "buy") return ETradeType.Buy;
			if (tt == "sell") return ETradeType.Sell;
			throw new Exception("Unknown trade type string");
		}

		/// <summary>Convert a Cryptopia trade type to the enumeration value</summary>
		public static ETradeType TradeType(TradeType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Cryptopia.API.DataObjects.TradeType.Buy: return ETradeType.Buy;
			case global::Cryptopia.API.DataObjects.TradeType.Sell: return ETradeType.Sell;
			}
		}

		/// <summary>Convert a Poloniex trade type to the enumeration value</summary>
		public static ETradeType TradeType(EOrderType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case EOrderType.Buy: return ETradeType.Buy;
			case EOrderType.Sell: return ETradeType.Sell;
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

		/// <summary>Convert this trade type to the Poloniex definition of a trade type</summary>
		public static EOrderType ToPoloniexTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Buy: return EOrderType.Buy;
			case ETradeType.Sell: return EOrderType.Sell;
			}
		}
	}
}
