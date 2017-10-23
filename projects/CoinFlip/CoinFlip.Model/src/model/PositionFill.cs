using System;
using System.Diagnostics;
using System.Linq;
using pr.container;
using pr.util;

namespace CoinFlip
{
	/// <summary>A collection of trades associated with filling an order</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class PositionFill
	{
		public PositionFill(ulong order_id, ETradeType tt, TradePair pair)
		{
			OrderId   = order_id;
			TradeType = tt;
			Pair      = pair;
			Trades    = new HistoryCollection(this);
		}
		public PositionFill(PositionFill rhs)
			:this(rhs.OrderId, rhs.TradeType, rhs.Pair)
		{
			foreach (var fill in rhs.Trades.Values)
				Trades.Add(fill.TradeId, new Historic(fill));
		}

		/// <summary>The Id of the order that was filled by this collection of trades</summary>
		public ulong OrderId { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The pair traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The approximate price of the filled order (in Quote/Base)</summary>
		public Unit<decimal> PriceQ2B
		{
			get
			{
				if (Trades.Count == 0) return 0m._(Pair.RateUnits);
				return
					TradeType == ETradeType.B2Q ? Trades.Values.Min(x => x.PriceQ2B) :
					TradeType == ETradeType.Q2B ? Trades.Values.Max(x => x.PriceQ2B) :
					throw new Exception("Unknown trade type");
			}
		}

		///// <summary>The price of the filled order (in VolumeOut/VolumeIn units)</summary>
		//public Unit<decimal> Price
		//{
		//	get
		//	{
		//		if (Trades.Count == 0) return 0m._(VolumeOut) / 1m._(VolumeIn);
		//		return Trades.Values.Max(x => x.Price);
		//	}
		//}
		//public Unit<decimal> PriceInv
		//{
		//	get { var price = Price; return price != 0m ? (1m / price) : (0m._(VolumeIn) / 1m._(VolumeOut)); }
		//}
		//public Unit<decimal> PriceQ2B
		//{
		//	get { return TradeType == ETradeType.B2Q ? Price : PriceInv; }
		//}

		/// <summary>The sum of trade base volumes</summary>
		public Unit<decimal> VolumeBase
		{
			get { return Trades.Values.Sum(x => x.VolumeBase)._(Pair.Base); }
		}

		/// <summary>The sum of trade quote volumes</summary>
		public Unit<decimal> VolumeQuote
		{
			get { return Trades.Values.Sum(x => x.VolumeQuote)._(Pair.Quote); }
		}

		/// <summary>The sum of trade input volumes</summary>
		public Unit<decimal> VolumeIn
		{
			get { return Trades.Values.Sum(x => x.VolumeIn)._(CoinIn); }
		}

		/// <summary>The sum of trade output volumes</summary>
		public Unit<decimal> VolumeOut
		{
			get { return Trades.Values.Sum(x => x.VolumeOut)._(CoinOut); }
		}

		/// <summary>The sum of trade nett output volumes</summary>
		public Unit<decimal> VolumeNett
		{
			get { return Trades.Values.Sum(x => x.VolumeNett)._(CoinOut); }
		}

		/// <summary>The sum of trade commissions (in volume out units)</summary>
		public Unit<decimal> Commission
		{
			get { return Trades.Values.Sum(x => x.Commission)._(Pair.Quote); }
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
		
		/// <summary>The number of trades that make up this position fill</summary>
		public int TradeCount
		{
			get { return Trades.Count; }
		}

		/// <summary>Description string for the trade</summary>
		public string Description
		{
			get { return $"{VolumeIn.ToString("G6", true)} → {VolumeNett.ToString("G6",true)} @ {PriceQ2B.ToString("G6", true)}"; }
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

		#region Equals
		public bool Equals(PositionFill rhs)
		{
			return
				rhs        != null &&
				OrderId    == rhs.OrderId &&
				TradeType  == rhs.TradeType &&
				Pair       == rhs.Pair &&
				Trades.Values.SequenceEqual(rhs.Trades.Values);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as PositionFill);
		}
		public override int GetHashCode()
		{
			return new { OrderId, TradeType, Pair }.GetHashCode();
		}
		#endregion
	}
}
