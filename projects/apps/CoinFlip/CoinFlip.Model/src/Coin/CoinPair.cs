using System;
using System.Data;
using Dapper;

namespace CoinFlip
{
	/// <summary>Standard format for currency pair names</summary>
	public class CoinPair
	{
		static CoinPair()
		{
			SqlMapper.AddTypeHandler(new Mapper());
		}
		public CoinPair(string @base, string quote)
		{
			Base = @base;
			Quote = quote;
		}
		public CoinPair(string pair_name)
		{
			var s = pair_name.Split('/');
			if (s.Length != 2) throw new Exception($"Invalid pair name: {pair_name}");
			Base = s[0];
			Quote = s[1];
		}
		public CoinPair(Coin @base, Coin quote)
			: this(@base?.Symbol ?? "---", quote?.Symbol ?? "---")
		{ }

		/// <summary>The name of the base currency</summary>
		public string Base { get; }

		/// <summary>The name of the quote currency</summary>
		public string Quote { get; }

		/// <summary>Standard pair name</summary>
		public string Name => $"{Base}/{Quote}";

		/// <summary>The rates of the exchange rate: Quote/Base</summary>
		public string RateUnits => $"{Quote}/{Base}";

		#region ToString
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
		#endregion

		#region SqlMapping
		public class Mapper : SqlMapper.TypeHandler<CoinPair>
		{
			public override CoinPair Parse(object value) => new((string)value);
			public override void SetValue(IDbDataParameter parameter, CoinPair value) => parameter.Value = value.Name;
		}
		#endregion
	}
}
