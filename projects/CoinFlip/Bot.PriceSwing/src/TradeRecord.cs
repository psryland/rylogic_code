using System;
using System.Xml.Linq;
using CoinFlip;
using pr.extn;
using pr.util;

namespace Bot.PriceSwing
{
	/// <summary>A record of a single trade</summary>
	public class TradeRecord
	{
		public TradeRecord(ETradeType tt, ulong order_id, Unit<decimal> price, Unit<decimal> volume_in, Unit<decimal> volume_out)
		{
			// Check units
			if (price < 0m._(volume_out) / 1m._(volume_in))
				throw new Exception("Invalid trade price");

			TradeType = tt;
			OrderId = order_id;
			Price = price;
			VolumeIn = volume_in;
			VolumeOut = volume_out;
		}
		public TradeRecord(XElement node)
		{
			TradeType = node.Element(nameof(TradeType)).As(TradeType);
			OrderId   = node.Element(nameof(OrderId  )).As(OrderId);
			Price     = node.Element(nameof(Price    )).As(Price);
			VolumeIn  = node.Element(nameof(VolumeIn )).As(VolumeIn );
			VolumeOut = node.Element(nameof(VolumeOut)).As(VolumeOut);
		}
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(TradeType), TradeType, false);
			node.Add2(nameof(OrderId  ), OrderId  , false);
			node.Add2(nameof(Price    ), Price    , false);
			node.Add2(nameof(VolumeIn ), VolumeIn , false);
			node.Add2(nameof(VolumeOut), VolumeOut, false);
			return node;
		}

		/// <summary>The direction of the trade</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>The order Id of the trade</summary>
		public ulong OrderId { get; private set; }

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
	}
}
