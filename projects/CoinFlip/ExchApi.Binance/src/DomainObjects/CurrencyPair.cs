using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Binance.API.DomainObjects
{
	/// <summary>Base/Quote currency trading pair</summary>
	[DebuggerDisplay("{Description}")]
	public struct CurrencyPair
	{
		// Notes:
		//  - Binance use "symbol" to mean currency pair.
		//  - Binance don't use a delimiter in their currency pairs so there
		//    isn't a clean way to parse a currency pair string.
		//  - The 'ServerRules' API provides the mapping from all supported
		//    'symbols' to currency pairs. This must be populated at runtime
		//    however.

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

		/// <summary>Mapping from Binance 'Symbol' to currency pair</summary>
		internal static Dictionary<string, CurrencyPair> SymbolToPair = new Dictionary<string, CurrencyPair>();
		public static CurrencyPair Parse(string pair_name)
		{
			if (SymbolToPair.TryGetValue(pair_name, out var pair))
				return pair;

			// Check the mapping has been populated by a call to 'ServerRules'
			if (SymbolToPair.Count == 0)
				throw new Exception("A call to 'ServerRules' must be made on the Binance API. This populates the list of supported currency pairs");

			// Check for a valid pair name
			if (pair_name?.Length == 0)
			{
				BinanceApi.Log.Write(Rylogic.Utility.ELogLevel.Warn, $"CurrencyPair.Parse failed: The given 'pair_name' has no value");
				return new CurrencyPair("Unkn", "own");
			}

			// Since there is no delimiter in the currency pair, we can't reliably convert
			// back to base/quote. Use a bunch of heuristics to and get the base/quote values.
			// Assume the minimum symbol code length is 3.
			if (pair_name.Length == 6)
				return new CurrencyPair(pair_name.Substring(0, 3), pair_name.Substring(3, 3));

			// Log the unknown coin and return an invalid pair
			BinanceApi.Log.Write(Rylogic.Utility.ELogLevel.Warn, $"Failed to determine currency pair from code: {pair}");
			return new CurrencyPair("Unkn", "own");
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
