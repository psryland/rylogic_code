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

		/// <summary>The time this listings was last updated (in unix seconds)</summary>
		public long UpdatedAt { get; set; }

		#region Equals
		public bool Equals(Listing rhs)
		{
			return ID == rhs.ID;
		}
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return ID.GetHashCode();
		}
		#endregion
	}
}
