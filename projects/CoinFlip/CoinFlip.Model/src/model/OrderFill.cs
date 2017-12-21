using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using pr.container;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>A collection of trades associated with filling an order</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class OrderFill :INotifyPropertyChanged
	{
		public OrderFill(ulong order_id, ETradeType tt, TradePair pair)
		{
			OrderId   = order_id;
			UniqueKey = Guid.NewGuid();
			TradeType = tt;
			Pair      = pair;
			Trades    = new HistoryCollection(this);
		}
		public OrderFill(OrderFill rhs)
			:this(rhs.OrderId, rhs.TradeType, rhs.Pair)
		{
			foreach (var fill in rhs.Trades.Values)
				Trades.Add(fill.TradeId, new Historic(fill));
		}

		/// <summary>The Id of the order that was filled by this collection of trades</summary>
		public ulong OrderId { get; private set; }

		/// <summary>A unique key assigned to this position (local only)</summary>
		public Guid UniqueKey { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The pair traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The exchange that this trade occurred on</summary>
		public Exchange Exchange
		{
			get { return Pair?.Exchange; }
		}

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
			get { return Trades.Values.Sum(x => x.Commission)._(CoinOut); }
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
			private readonly OrderFill m_fill;
			public HistoryCollection(OrderFill fill)
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
					m_fill.RaisePropertyChanged(nameof(Trades));
				}
			}
		}

		/// <summary>INotifyPropertyChanged</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void SetProp<T>(ref T prop, T value, string name)
		{
			if (Equals(prop, value)) return;
			prop = value;
			RaisePropertyChanged(name);
		}
		private void RaisePropertyChanged(string name)
		{
			PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
		}

		#region Equals
		public bool Equals(OrderFill rhs)
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
			return Equals(obj as OrderFill);
		}
		public override int GetHashCode()
		{
			return new { OrderId, TradeType, Pair }.GetHashCode();
		}
		#endregion
	}
}
