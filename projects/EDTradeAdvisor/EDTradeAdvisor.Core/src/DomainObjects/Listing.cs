namespace EDTradeAdvisor.DomainObjects
{
	public class Listing
	{
		/// <summary></summary>
		public long ID { get; set; }

		/// <summary></summary>
		public long StationID { get; set; }

		/// <summary></summary>
		public long CommodityID { get; set; }

		/// <summary></summary>
		public int Supply { get; set; }

		/// <summary></summary>
		public int SupplyBracket { get; set; }

		/// <summary></summary>
		public int BuyPrice { get; set; }

		/// <summary></summary>
		public int SellPrice { get; set; }

		/// <summary></summary>
		public long Demand { get; set; }

		/// <summary></summary>
		public int DemandBracket { get; set; }

		/// <summary></summary>
		public long UpdatedAt { get; set; }
	}
}
