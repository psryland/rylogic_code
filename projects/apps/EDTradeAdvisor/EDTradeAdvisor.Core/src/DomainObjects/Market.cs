using System.Collections.Generic;
using System.Diagnostics;

namespace EDTradeAdvisor.DomainObjects
{
	public class Market
	{
		public Market(Station station)
		{
			Listings = new List<Listing>();
			Station = station;
		}

		/// <summary>The station that this is the market for</summary>
		public Station Station { get; set; }
		
		/// <summary>The commodities traded at this station</summary>
		public List<Listing> Listings { get; }

		/// <summary></summary>
		[DebuggerDisplay("{Description,nq}")]
		public class Listing
		{
			public Listing()
			{ }
			public Listing(long comm_id, string comm_name, int buy, int sell, long supply, long demand)
			{
				CommodityID = comm_id;
				CommodityName = comm_name;
				BuyPrice = buy;
				SellPrice = sell;
				Supply = supply;
			}

			/// <summary>The name of the commodity</summary>
			public string CommodityName { get; set; }
			public long CommodityID { get; set; }

			/// <summary>What the commodity costs to buy from this station</summary>
			public int BuyPrice { get; set; }

			/// <summary>What you get if you sell the commodity at this station</summary>
			public int SellPrice { get; set; }

			/// <summary>The number of instances of this commodity available at the station</summary>
			public long Supply { get; set; }

			/// <summary>How many are wanted</summary>
			public long Demand { get; set; }

			/// <summary></summary>
			public string Description => $"{CommodityName} buy={BuyPrice} sell={SellPrice}";
		}
	}
}
