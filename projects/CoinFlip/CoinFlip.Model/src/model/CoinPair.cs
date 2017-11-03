using System;

namespace CoinFlip
{
	/// <summary>Standard format for currency pair names</summary>
	public class CoinPair
	{
		public CoinPair(string base_, string quote)
		{
			Base = base_;
			Quote = quote;
		}
		public CoinPair(string pair_name)
		{
			var s = pair_name.Split('/');
			if (s.Length != 2) throw new Exception($"Invalid pair name: {pair_name}");
			Base = s[0];
			Quote = s[1];
		}
		public CoinPair(Coin base_, Coin quote)
			:this(base_?.Symbol ?? "---", quote?.Symbol ?? "---")
		{}

		/// <summary>Standard pair name</summary>
		public string Name
		{
			get { return $"{Base}/{Quote}"; }
		}

		/// <summary>The name of the base currency</summary>
		public string Base { get; private set; }

		/// <summary>The name of the quote currency</summary>
		public string Quote { get; private set; }

		/// <summary>The rates of the exchange rate: Quote/Base</summary>
		public string RateUnits
		{
			get { return $"{Quote}/{Base}"; }
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Name;
		}
		public static implicit operator string(CoinPair p)
		{
			return p.Name;
		}
		public static implicit operator CoinPair(string pair_name)
		{
			return new CoinPair(pair_name);
		}
	}
}
