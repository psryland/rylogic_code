using System;
using System.Diagnostics;
using System.Linq;
using pr.container;
using pr.util;

namespace CoinFlip
{
	/// <summary>A collection of trades associated with filling an order</summary>
	public class PositionFill
	{
		public PositionFill(ulong order_id, ETradeType tt, TradePair pair)
		{
			OrderId   = order_id;
			TradeType = tt;
			Pair      = pair;
			Trades    = new HistoryCollection(this);
		}

		/// <summary>The Id of the order that was filled by this collection of trades</summary>
		public ulong OrderId { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The pair traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The price of the filled order (in VolumeOut/VolumeIn units)</summary>
		public Unit<decimal> Price
		{
			get
			{
				if (Trades.Count == 0) return 0m._(VolumeOut) / 1m._(VolumeIn);
				return Trades.Values.Max(x => x.Price);
			}
		}
		public Unit<decimal> PriceInv
		{
			get { var price = Price; return price != 0m ? (1m / price) : (0m._(VolumeIn) / 1m._(VolumeOut)); }
		}
		public Unit<decimal> PriceQ2B
		{
			get { return TradeType == ETradeType.B2Q ? Price : PriceInv; }
		}

		/// <summary>The sum of trade input volumes</summary>
		public Unit<decimal> VolumeIn
		{
			get { return Trades.Values.Sum(x => x.VolumeIn); }
		}

		/// <summary>The sum of trade output volumes</summary>
		public Unit<decimal> VolumeOut
		{
			get { return Trades.Values.Sum(x => x.VolumeOut); }
		}

		/// <summary>The sum of trade nett output volumes</summary>
		public Unit<decimal> VolumeNett
		{
			get { return Trades.Values.Sum(x => x.VolumeNett); }
		}

		/// <summary>The sum of trade commissions</summary>
		public Unit<decimal> Commission
		{
			get { return Trades.Values.Sum(x => x.Commission); }
		}

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn
		{
			get { return TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote; }
		}

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut
		{
			get { return TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base; }
		}

		/// <summary>The timestamp of when the original order was created</summary>
		public DateTimeOffset Created
		{
			get
			{
				if (Trades.Count == 0) return DateTimeOffset.MinValue;
				return Trades.Values.Min(x => x.Created);
			}
		}
		
		/// <summary>Description string for the trade</summary>
		public string Description
		{
			get { return $"{VolumeIn.ToString("G6")} {CoinIn} → {VolumeNett.ToString("G6")} {CoinOut} @ {PriceQ2B.ToString("G6")}"; }
		}

		/// <summary>The trades associated with filling a single order</summary>
		public HistoryCollection Trades { get; private set; }
		public class HistoryCollection :BindingDict<ulong, Historic>
		{
			private readonly PositionFill m_fill;
			public HistoryCollection(PositionFill fill)
			{
				m_fill = fill;
				KeyFrom = x => x.TradeId;
			}
			public override Historic this[ulong key]
			{
				get
				{
					return TryGetValue(key, out var pos) ? pos : null;
				}
				set
				{
					Debug.Assert(value.OrderId == m_fill.OrderId);
					base[key] = value;
				}
			}
		}
	}
}
