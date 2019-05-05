using System.Diagnostics;

namespace EDTradeAdvisor.DomainObjects
{
	[DebuggerDisplay("{Name}")]
	public class Commodity
	{
		/// <summary></summary>
		public long ID { get; set; }

		/// <summary></summary>
		public string Name { get; set; }

		/// <summary></summary>
		public long CategoryID { get; set; }

		/// <summary></summary>
		public bool IsRare { get; set; }

		/// <summary></summary>
		public int? MinBuyPrice { get; set; }

		/// <summary></summary>
		public int? MaxBuyPrice { get; set; }

		/// <summary></summary>
		public int? MinSellPrice { get; set; }

		/// <summary></summary>
		public int? MaxSellPrice { get; set; }

		/// <summary></summary>
		public int? AveragePrice { get; set; }

		/// <summary></summary>
		public int BuyPriceLowerAverage { get; set; }

		/// <summary></summary>
		public int SellPriceUpperAverage { get; set; }

		/// <summary></summary>
		public int IsNonMarketable { get; set; }

		/// <summary></summary>
		public long EDID { get; set; }

		#region Equals
		public bool Equals(Commodity rhs)
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
