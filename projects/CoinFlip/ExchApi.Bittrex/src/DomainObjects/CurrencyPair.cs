using System.Diagnostics;

namespace Bittrex.API.DomainObjects
{
	/// <summary>Base/Quote currency trading pair</summary>
	[DebuggerDisplay("{Description,nq}")]
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

		/// <summary>The name of the pair when querying Bittrex (i.e. Quote-Base)</summary>
		public string Id => $"{Quote}-{Base}";

		/// <summary></summary>
		public string Description => $"{Base}/{Quote}";

		/// <summary>Parse the base/quote currency from a string</summary>
		public static CurrencyPair Parse(string str)
		{
			var coin = str.Split('-');
			return new CurrencyPair(coin[1], coin[0]);
		}

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
