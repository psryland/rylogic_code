﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class OrderCompleted :IOrder, INotifyPropertyChanged
	{
		// Notes:
		//  - An OrderCompleted is a completed (or partially completed) Order consisting
		//    of one or more 'TradeCompleted's that where made to complete the order.

		public OrderCompleted(long order_id, Fund fund, TradePair pair, ETradeType tt)
		{
			OrderId   = order_id;
			Fund      = fund;
			TradeType = tt;
			Pair      = pair;
			Trades    = new TradeCompletedCollection(this);
		}
		public OrderCompleted(OrderCompleted rhs)
			:this(rhs.OrderId, rhs.Fund, rhs.Pair, rhs.TradeType)
		{
			foreach (var fill in rhs.Trades.Values)
				Trades.Add(fill.TradeId, new TradeCompleted(fill));
		}

		/// <summary>The Id of the order that was filled by this collection of trades</summary>
		public long OrderId { get; }

		/// <summary>The fund that this order was associated with</summary>
		public Fund Fund { get; }

		/// <summary>The exchange that this trade occurred on</summary>
		public Exchange Exchange => Pair?.Exchange;

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; }

		/// <summary>The trade type as a string description</summary>
		public string TradeTypeDesc =>
			TradeType == ETradeType.Q2B ? $"{Pair.Quote}→{Pair.Base} ({TradeType})" :
			TradeType == ETradeType.B2Q ? $"{Pair.Base}→{Pair.Quote} ({TradeType})" :
			"---";

		/// <summary>The pair traded</summary>
		public TradePair Pair { get; }

		/// <summary>The trades associated with filling a single order</summary>
		public TradeCompletedCollection Trades { get; }

		/// <summary>
		/// The approximate price of the filled order (in Quote/Base).
		/// Approximate because each trade can be at a different price.</summary>
		public Unit<double> PriceQ2B
		{
			get
			{
				return Trades.Count != 0
					? Trades.Values.Average(x => x.PriceQ2B)._(Pair.RateUnits)
					: 0.0._(Pair.RateUnits);
			}
		}

		/// <summary>The sum of trade base amounts</summary>
		public Unit<double> AmountBase => Trades.Values.Sum(x => x.AmountBase)._(Pair.Base);

		/// <summary>The sum of trade quote amounts</summary>
		public Unit<double> AmountQuote => Trades.Values.Sum(x => x.AmountQuote)._(Pair.Quote);

		/// <summary>The sum of trade input amounts</summary>
		public Unit<double> AmountIn => Trades.Values.Sum(x => x.AmountIn)._(CoinIn);

		/// <summary>The sum of trade output amounts</summary>
		public Unit<double> AmountOut => Trades.Values.Sum(x => x.AmountOut)._(CoinOut);

		/// <summary>The sum of trade commissions (in CommissionCoin)</summary>
		public Unit<double> Commission => Trades.Values.Sum(x => x.Commission)._(CommissionCoin);

		/// <summary>The currency that commission was charged in</summary>
		public Coin CommissionCoin => Trades.Values.FirstOrDefault()?.CommissionCoin;

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn => TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote;

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut => TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base;

		/// <summary>The timestamp of when the first trade related to this order was made</summary>
		public DateTimeOffset Created => Trades.Count == 0 ? DateTimeOffset.MinValue : Trades.Values.Min(x => x.Created);
		DateTimeOffset? IOrder.Created => Created;

		/// <summary>Description string for the trade</summary>
		public string Description => $"{AmountIn.ToString(6, true)} → {AmountOut.ToString(6, true)} @ {PriceQ2B.ToString(4, true)}";

		/// <summary>INotifyPropertyChanged</summary>
		public event PropertyChangedEventHandler PropertyChanged;
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