using System.Diagnostics;

namespace Bitfinex.API
{
	[DebuggerDisplay("{Symbol} Total={Total} Avail={Available}")]
	public class Balance
	{
		public Balance(string sym)
		{
			Symbol = sym;
		}

		/// <summary>The symbol that this is the balance for</summary>
		public string Symbol { get; private set; }

		/// <summary>The total balance</summary>
		public decimal Total { get; internal set; }

		/// <summary>The balance not reserved for current orders</summary>
		public decimal Available { get; internal set; }

		/// <summary></summary>
		public decimal UnsettledInterest { get; internal set; }
	}
}
