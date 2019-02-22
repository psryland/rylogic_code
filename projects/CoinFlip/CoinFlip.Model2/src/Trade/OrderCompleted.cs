using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class OrderCompleted :INotifyPropertyChanged
	{
		// Notes:
		//  - An OrderCompleted is a completed Order consisting of one or more 'TradeCompleted's
		//    that where made to complete the order.

		public OrderCompleted(long order_id, ETradeType tt, TradePair pair)
		{
			OrderId   = order_id;
			UniqueKey = Guid.NewGuid();
			TradeType = tt;
			Pair      = pair;
			Trades    = new TradeCompletedCollection(this);
		}
		public OrderCompleted(OrderCompleted rhs)
			:this(rhs.OrderId, rhs.TradeType, rhs.Pair)
		{
			foreach (var fill in rhs.Trades.Values)
				Trades.Add(fill.TradeId, new TradeCompleted(fill));
		}

		/// <summary>The Id of the order that was filled by this collection of trades</summary>
		public long OrderId { get; }

		/// <summary>A unique key assigned to this position (local only)</summary>
		public Guid UniqueKey { get; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; }

		/// <summary>The pair traded</summary>
		public TradePair Pair { get; }

		/// <summary>The trades associated with filling a single order</summary>
		public TradeCompletedCollection Trades { get; }

		/// <summary>The number of completed trades that make up this completed order</summary>
		public int TradeCount => Trades.Count;

		/// <summary>The exchange that this trade occurred on</summary>
		public Exchange Exchange => Pair?.Exchange;

		/// <summary>
		/// The approximate price of the filled order (in Quote/Base).
		/// Approximate because each trade can be at a different price.</summary>
		public Unit<decimal> PriceQ2B
		{
			get
			{
				if (Trades.Count == 0)
					return 0m._(Pair.RateUnits);

				return
					TradeType == ETradeType.B2Q ? Trades.Values.Min(x => x.PriceQ2B) :
					TradeType == ETradeType.Q2B ? Trades.Values.Max(x => x.PriceQ2B) :
					throw new Exception("Unknown trade type");
			}
		}

		/// <summary>The sum of trade base volumes</summary>
		public Unit<decimal> VolumeBase => Trades.Values.Sum(x => x.VolumeBase)._(Pair.Base);

		/// <summary>The sum of trade quote volumes</summary>
		public Unit<decimal> VolumeQuote => Trades.Values.Sum(x => x.VolumeQuote)._(Pair.Quote);

		/// <summary>The sum of trade input volumes</summary>
		public Unit<decimal> VolumeIn => Trades.Values.Sum(x => x.VolumeIn)._(CoinIn);

		/// <summary>The sum of trade output volumes</summary>
		public Unit<decimal> VolumeOut => Trades.Values.Sum(x => x.VolumeOut)._(CoinOut);

		/// <summary>The sum of trade nett output volumes</summary>
		public Unit<decimal> VolumeNett => Trades.Values.Sum(x => x.VolumeNett)._(CoinOut);

		/// <summary>The sum of trade commissions (in volume out units)</summary>
		public Unit<decimal> Commission => Trades.Values.Sum(x => x.Commission)._(CoinOut);

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn => TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote;

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut => TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base;

		/// <summary>The timestamp of when the original order was created</summary>
		public DateTimeOffset Created
		{
			get
			{
				if (Trades.Count == 0)
					return DateTimeOffset.MinValue;

				return Trades.Values.Min(x => x.Created);
			}
		}

		/// <summary>Description string for the trade</summary>
		public string Description => $"{VolumeIn.ToString("G6", true)} → {VolumeNett.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}";

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
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		#region Equals
		public bool Equals(OrderCompleted rhs)
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
			return Equals(obj as OrderCompleted);
		}
		public override int GetHashCode()
		{
			return new { OrderId, TradeType, Pair }.GetHashCode();
		}
		#endregion

		public class TradeCompletedCollection : BindingDict<long, TradeCompleted>
		{
			private readonly OrderCompleted m_order;
			public TradeCompletedCollection(OrderCompleted order)
			{
				m_order = order;
				KeyFrom = x => x.TradeId;
			}
			public override TradeCompleted this[long key]
			{
				get { return TryGetValue(key, out var pos) ? pos : null; }
				set
				{
					Debug.Assert(value.OrderId == m_order.OrderId);
					base[key] = value;
					m_order.RaisePropertyChanged(nameof(Trades));
				}
			}
		}
	}
}
