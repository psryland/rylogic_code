using System.Diagnostics;

namespace Binance.API.DomainObjects
{
	/// <summary>Base/Quote currency trading pair</summary>
	[DebuggerDisplay("{Description}")]
	public struct CurrencyPair
	{
		public CurrencyPair(string base_, string quote)
		{
			Base  = base_.ToUpperInvariant();
			Quote = quote.ToUpperInvariant();
		}

		/// <summary>The base currency</summary>
		public string Base { get; }

		/// <summary>The quote currency</summary>
		public string Quote { get; }

		/// <summary>The name of the pair when querying the exchange</summary>
		public string Id => $"{Base}{Quote}";

		/// <summary></summary>
		private string Description => $"{Base}/{Quote}";

		#region Equals
		public bool Equals(CurrencyPair b)
		{
			return b.Base == Base && b.Quote == Quote;
		}
		public override bool Equals(object obj)
		{
			return Equals((CurrencyPair)obj);
		}
		public override int GetHashCode()
		{
			return new { Base, Quote }.GetHashCode();
		}
		#endregion
	}
}
