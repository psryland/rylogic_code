using System;
using System.Drawing;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoinFlip.Properties;
using pr.common;
using pr.extn;
using pr.gui;

namespace CoinFlip
{
	public enum ETradeType
	{
		/// <summary>Quote->Base, Buy, Bid, Short</summary>
		Q2B,

		/// <summary>Base->Quote, Sell, Ask, Long</summary>
		B2Q,
	}
	[Flags] public enum ETradeDirection
	{
		/// <summary></summary>
		None = 0,

		/// <summary>Quote->Base, Buy, Bid, Short</summary>
		Q2B = 1 << ETradeType.Q2B,

		/// <summary>Base->Quote, Sell, Ask, Long</summary>
		B2Q = 1 << ETradeType.B2Q,

		/// <summary>Both directions</summary>
		Both = Q2B | B2Q,
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

		/// <summary>Regex pattern for log UIs</summary>
		public static readonly Regex LogEntryPattern = new Regex(@"^(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)",RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);
		public static readonly LogUI.HLPattern[] LogHighlighting = new[]
		{
			new LogUI.HLPattern(Color_.FromArgb(0xffb5ffd2), Color.Black, EPattern.Substring, "Fishing started"),
			new LogUI.HLPattern(Color_.FromArgb(0xffb5ffd2), Color.Black, EPattern.Substring, "Fishing stopped"),
			new LogUI.HLPattern(Color_.FromArgb(0xffe7a5ff), Color.Black, EPattern.Substring, "!Profit!"),
			new LogUI.HLPattern(Color_.FromArgb(0xfffcffae), Color.Black, EPattern.Substring, "filled"),
			new LogUI.HLPattern(Color_.FromArgb(0xffff7e39), Color.Black, EPattern.Substring, "ignored"),
		};

		/// <summary>Convert a trade type string to the enumeration value</summary>
		public static ETradeType TradeType(string trade_type)
		{
			switch (trade_type.ToLowerInvariant()) {
			case "q2b": case "buy": case "bid": case "short": return ETradeType.Q2B;
			case "b2q": case "sell": case "ask": case "long": return ETradeType.B2Q;
			}
			throw new Exception("Unknown trade type string");
		}

		/// <summary>Convert a Cryptopia trade type to ETradeType</summary>
		public static ETradeType TradeType(global::Cryptopia.API.DataObjects.TradeType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Cryptopia.API.DataObjects.TradeType.Buy: return ETradeType.Q2B;
			case global::Cryptopia.API.DataObjects.TradeType.Sell: return ETradeType.B2Q;
			}
		}

		/// <summary>Convert a Poloniex trade type to ETradeType</summary>
		public static ETradeType TradeType(global::Poloniex.API.EOrderType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Poloniex.API.EOrderType.Buy: return ETradeType.Q2B;
			case global::Poloniex.API.EOrderType.Sell: return ETradeType.B2Q;
			}
		}

		/// <summary>Convert a Bittrex trade type to ETradeType</summary>
		public static ETradeType TradeType(global::Bittrex.API.EOrderType order_type)
		{
			switch (order_type) {
			default: throw new Exception("Unknown trade type string");
			case global::Bittrex.API.EOrderType.Buy: return ETradeType.Q2B;
			case global::Bittrex.API.EOrderType.Sell: return ETradeType.B2Q;
			}
		}

		/// <summary>Convert this trade type to the Cryptopia definition of a trade type</summary>
		public static global::Cryptopia.API.DataObjects.TradeType ToCryptopiaTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Cryptopia.API.DataObjects.TradeType.Buy;
			case ETradeType.B2Q: return global::Cryptopia.API.DataObjects.TradeType.Sell;
			}
		}

		/// <summary>Convert this trade type to the Poloniex definition of a trade type</summary>
		public static global::Poloniex.API.EOrderType ToPoloniexTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Poloniex.API.EOrderType.Buy;
			case ETradeType.B2Q: return global::Poloniex.API.EOrderType.Sell;
			}
		}

		/// <summary>Convert this trade type to the Poloniex definition of a trade type</summary>
		public static global::Bittrex.API.EOrderType ToBittrexTT(this ETradeType trade_type)
		{
			switch (trade_type) {
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B: return global::Bittrex.API.EOrderType.Buy;
			case ETradeType.B2Q: return global::Bittrex.API.EOrderType.Sell;
			}
		}
	}

	public static class Res
	{
		public static readonly Image Active   = new Bitmap(Resources.active, new Size(28,28));
		public static readonly Image Inactive = new Bitmap(Resources.inactive, new Size(28,28));
		public static readonly Image Changing = new Bitmap(Resources.changing, new Size(28,28));
		public static readonly SoundPlayer Coins = new SoundPlayer(Resources.coins);
	}
}
