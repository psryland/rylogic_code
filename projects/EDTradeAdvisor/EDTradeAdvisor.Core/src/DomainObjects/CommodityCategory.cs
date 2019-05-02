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
	}
}
