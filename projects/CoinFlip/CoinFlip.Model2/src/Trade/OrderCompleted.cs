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
		public string TradeTypeDesc =>
			TradeType == ETradeType.Q2B? $"{Pair.Quote}→{Pair.Base} ({TradeType})" :
			TradeType == ETradeType.B2Q? $"{Pair.Base}→{Pair.Quote} ({TradeType})" :
			"---";

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

		/// <summary>The sum of trade base amounts</summary>
		public Unit<decimal> AmountBase => Trades.Values.Sum(x => x.AmountBase)._(Pair.Base);

		/// <summary>The sum of trade quote amounts</summary>
		public Unit<decimal> AmountQuote => Trades.Values.Sum(x => x.AmountQuote)._(Pair.Quote);

		/// <summary>The sum of trade input amounts</summary>
		public Unit<decimal> AmountIn => Trades.Values.Sum(x => x.AmountIn)._(CoinIn);

		/// <summary>The sum of trade output amounts</summary>
		public Unit<decimal> AmountOut => Trades.Values.Sum(x => x.AmountOut)._(CoinOut);

		/// <summary>The sum of trade nett output amounts</summary>
		public Unit<decimal> AmountNett => Trades.Values.Sum(x => x.AmountNett)._(CoinOut);

		/// <summary>The sum of trade commissions (in amount out units)</summary>
		public Unit<decimal> Commission => Trades.Values.Sum(x => x.Commission)._(CoinOut);

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn => TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote;

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut => TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base;

		/// <summary>The timestamp of when the first trade related to this order was made</summary>
		public DateTimeOffset Created => Trades.Count == 0 ? DateTimeOffset.MinValue : Trades.Values.Min(x => x.Created);

		/// <summary>Description string for the trade</summary>
		public string Description => $"{AmountIn.ToString("G6", true)} → {AmountNett.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}";

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
