using System.Diagnostics;

namespace Poloniex.API
{
	/// <summary>Base/Quote currency trading pair</summary>
	[DebuggerDisplay("{Base,nq}/{Quote,nq}")]
	public struct CurrencyPair
	{
		public CurrencyPair(string base_, string quote)
		{
			Base = base_;
			Quote = quote;
		}

		/// <summary>The base currency</summary>
		public string Base { get; private set; }

		/// <summary>The quote currency</summary>
		public string Quote { get; private set; }

		/// <summary>The name of the pair when querying Poloniex (i.e. Quote_Base)</summary>
		public string Id
		{
			// Poloniex pairs are given as 'Quote_Base'.. :-/
			get { return string.Format("{0}_{1}", Quote, Base); }
		}

		/// <summary>Parse the base/quote currency from a string</summary>
		public static CurrencyPair Parse(string str)
		{
			// Poloniex pairs are given as 'Quote_Base'.. :-/
			var coin = str.Split('_');
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
