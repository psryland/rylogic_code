using System;
using System.Diagnostics;
using System.Linq;

namespace Cryptopia.API
{
	/// <summary>Base/Quote currency trading pair</summary>
	[DebuggerDisplay("{Base,nq}/{Quote,nq}")]
	public struct CurrencyPair
	{
		public CurrencyPair(string base_, string quote)
		{
			Base  = base_.ToUpperInvariant();
			Quote = quote.ToUpperInvariant();
		}

		/// <summary>The base currency</summary>
		public string Base { get; private set; }

		/// <summary>The quote currency</summary>
		public string Quote { get; private set; }

		/// <summary>The name of the pair when querying Cryptopia (i.e. Quote_Base)</summary>
		public string Id
		{
			get { return string.Format("{0}_{1}", Quote, Base); }
		}

		/// <summary>Parse the base/quote currency from a string</summary>
		public static CurrencyPair Parse(string str)
		{
			if (str.Contains('/'))
			{
				var sym = str.Split('/');
				return new CurrencyPair(sym[0], sym[1]);
			}
			if (str.Contains('_'))
			{
				var sym = str.Split('_');
				return new CurrencyPair(sym[1], sym[0]);
			}
			throw new Exception("Unknown currency pair format");
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
