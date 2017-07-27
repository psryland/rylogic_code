using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.container;

namespace CoinFlip
{
	/// <summary>A collection of trades associated with filling an order</summary>
	public class PositionFill
	{
		public PositionFill(ulong order_id, ETradeType tt, TradePair pair)
		{
			OrderId = order_id;
			TradeType = tt;
			Pair = pair;
			Trades = new PositionsCollection(this);
		}

		/// <summary>The Id of the order that was filled by this collection of trades</summary>
		public ulong OrderId { get; private set; }

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The pair traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The trades associated with filling a single order</summary>
		public PositionsCollection Trades { get; private set; }
		public class PositionsCollection :BindingDict<ulong, Position>
		{
			private readonly PositionFill m_fill;
			public PositionsCollection(PositionFill fill)
			{
				m_fill = fill;
				KeyFrom = x => x.TradeId;
			}
			public override Position this[ulong key]
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
