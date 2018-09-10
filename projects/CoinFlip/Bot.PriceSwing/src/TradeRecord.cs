using System;
using System.Diagnostics;
using System.Xml.Linq;
using CoinFlip;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Bot.PriceSwing
{
	/// <summary>A record of a single trade</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class TradeRecord :IDisposable
	{
		public TradeRecord(ETradeType tt, ulong order_id, DateTimeOffset timestamp, Unit<decimal> price, Unit<decimal> volume_in, Unit<decimal> volume_out)
		{
			// Check units
			if (price < 0m._(volume_out) / 1m._(volume_in))
				throw new Exception("Invalid trade price");

			TradeType = tt;
			OrderId = order_id;
			Timestamp = timestamp.Ticks;
			Price = price;
			VolumeIn = volume_in;
			VolumeOut = volume_out;
		}
		public TradeRecord(XElement node)
		{
			TradeType = node.Element(nameof(TradeType)).As(TradeType);
			OrderId   = node.Element(nameof(OrderId  )).As(OrderId);
			Timestamp = node.Element(nameof(Timestamp)).As(Timestamp);
			Price     = node.Element(nameof(Price    )).As(Price);
			VolumeIn  = node.Element(nameof(VolumeIn )).As(VolumeIn );
			VolumeOut = node.Element(nameof(VolumeOut)).As(VolumeOut);
		}
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(TradeType), TradeType, false);
			node.Add2(nameof(OrderId  ), OrderId  , false);
			node.Add2(nameof(Timestamp), Timestamp, false);
			node.Add2(nameof(Price    ), Price    , false);
			node.Add2(nameof(VolumeIn ), VolumeIn , false);
			node.Add2(nameof(VolumeOut), VolumeOut, false);
			return node;
		}
		public void Dispose()
		{
			Gfx = null;
		}

		/// <summary>The direction of the trade</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The order Id of the trade</summary>
		public ulong OrderId { get; private set; }

		/// <summary>When the trade was made</summary>
		public long Timestamp { get; private set; }
		public DateTimeOffset TimestampUTC { get { return new DateTimeOffset(Timestamp, TimeSpan.Zero); } }

		/// <summary>The price of the trade (in VolumeOut/VolumeIn units)</summary>
		public Unit<decimal> Price { get; private set; }
		public Unit<decimal> PriceInv
		{
			get { return Price != 0m._(Price) ? (1m / Price) : (0m._(VolumeIn) / 1m._(VolumeOut)); }
		}
		public Unit<decimal> PriceQ2B
		{
			get { return TradeType == ETradeType.B2Q ? Price : PriceInv; }
		}

		/// <summary>The volume being sold</summary>
		public Unit<decimal> VolumeIn { get; private set; }

		/// <summary>The volume being bought</summary>
		public Unit<decimal> VolumeOut { get; private set; }

		/// <summary>The Id of an order place to match this trade record</summary>
		public ulong? MatchTradeId { get; set; }

		/// <summary>A graphics object representing this trade</summary>
		public View3d.Object Gfx
		{
			get { return m_gfx; }
			set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx;

		/// <summary>String description of the trade</summary>
		public string Description
		{
			get { return $"{VolumeIn.ToString("G6", true)} → {VolumeOut.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}"; }
		}
	}
}
