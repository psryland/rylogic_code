using System.Diagnostics;

namespace EDTradeAdvisor.DomainObjects
{
	[DebuggerDisplay("{Description,nq}")]
	public class TradeRoute
	{
		public TradeRoute()
		{ }
		public TradeRoute(Location origin, Location destination, string commodity_name, long commodity_id, int buy_price, int sell_price, long quantity)
		{
			Origin = origin;
			Destination = destination;
			CommodityName = commodity_name;
			CommodityID = commodity_id;
			BuyPrice = buy_price;
			SellPrice = sell_price;
			Quantity = quantity;
		}

		/// <summary>The start point of the trade route</summary>
		public Location Origin{ get; set; }
		
		/// <summary>The end point of the trade route</summary>
		public Location Destination { get; set; }

		/// <summary>What to buy at 'Origin'</summary>
		public string CommodityName { get; set; }
		public long CommodityID { get; set; }

		/// <summary>What 'Commodity' should cost at 'Origin'</summary>
		public int BuyPrice { get; set; }

		/// <summary>What 'Commodity' can be sold for at 'Destination'</summary>
		public int SellPrice { get; set; }

		/// <summary>How many 'Commodity' to buy at 'Origin'</summary>
		public long Quantity { get; set; }

		/// <summary>The expected profit</summary>
		public long Profit => Quantity * (SellPrice - BuyPrice);

		/// <summary>Distance between star systems</summary>
		public double Distance => (Destination.System.Position - Origin.System.Position).Length;

		/// <summary></summary>
		public string Description => $"{Quantity}x {CommodityName} @{Origin.Station.Name} → @{Destination.System.Name}/{Destination.Station.Name} Profit={Profit}";

		#region Equals
		public bool Equals(TradeRoute rhs)
		{
			return Origin.Equals(rhs.Origin) && Destination.Equals(rhs.Destination);
		}
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return new { Origin, Destination }.GetHashCode();
		}
		#endregion
	}
}
