using System.Diagnostics;

namespace EDTradeAdvisor.DomainObjects
{
	[DebuggerDisplay("{Name}")]
	public class CommodityCategory
	{
		/// <summary></summary>
		public long ID { get; set; }

		/// <summary></summary>
		public string Name { get; set; }

		#region Equals
		public bool Equals(CommodityCategory rhs)
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
