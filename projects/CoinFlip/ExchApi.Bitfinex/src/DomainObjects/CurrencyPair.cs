using System.Diagnostics;

namespace Bitfinex.API
{
	/// <summary>Base/Quote currency trading pair</summary>
	[DebuggerDisplay("{Base,nq}/{Quote,nq}")]
	public struct CurrencyPair
	{
		public CurrencyPair(string base_, string quote)
		{
			Base  = base_.ToUpperInvariant();
			Quote = quote.ToUpperInvariant();

			// Special case USD
			if (Base == "USD") Base = "USDT";
			if (Quote == "USD") Quote = "USDT";
		}

		/// <summary>The base currency</summary>
		public string Base { get; private set; }

		/// <summary>The quote currency</summary>
		public string Quote { get; private set; }

		/// <summary>The name of the pair when querying Bitfinex (i.e. tBaseQuote)</summary>
		public string Id
		{
			get { return $"t{Base.Substring(0,3)}{Quote.Substring(0,3)}"; }
		}

		/// <summary>Parse the base/quote currency from a string</summary>
		public static CurrencyPair Parse(string str)
		{
			// tBaseQuote
			if (str.StartsWith("t") && str.Length == 7)
				return new CurrencyPair(str.Substring(1, 3), str.Substring(4,3));

			// BaseQuote
			return new CurrencyPair(str.Substring(0, 3), str.Substring(3, 3));
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
