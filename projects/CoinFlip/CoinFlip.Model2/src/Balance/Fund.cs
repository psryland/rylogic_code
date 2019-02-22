using System;
using System.Collections.Generic;
using System.Diagnostics;
using CoinFlip.Settings;

namespace CoinFlip
{
	/// <summary>A fund is a partition of the user's balances on all exchanges</summary>
	[DebuggerDisplay("{Id}")]
	public class Fund
	{
		// Notes:
		// - A fund is a virtual sub-account 
		// - Fund provides an interface to the balances of a single fund
		// - A fund only manages the creation/destruction of balance contexts,
		//   not the amounts in each balance context.
		// - 'Fund' is used so that exchanges can create the balance contexts.
		// - This is basically a wrapper around a string

		/// <summary>The default fund</summary>
		public const string Main = "Main";

		public Fund(string id)
		{
			Id = id;
		}

		/// <summary>The unique Id for this fund</summary>
		public string Id { get; }

		/// <summary>Return the balance of 'coin' associated with this fund on the given exchange</summary>
		public FundBalance this[Coin coin] => coin.Balances[Id];

		/// <summary>Export settings data for this fund</summary>
		public FundData Export(IEnumerable<Exchange> exchanges)
		{
			// Return the non-zero balances associated with this fund on each exchange.
			// Don't bother with balances for the main fund because it is overwritten by updates.
			var exch_data = new List<FundData.ExchData>();
			if (Id != Main)
			{
				foreach (var exch in exchanges)
				{
					var bal_data = new List<FundData.BalData>();
					foreach (var bal in exch.Balance.Values)
					{
						if (bal[Id].Total == 0) continue;
						bal_data.Add(new FundData.BalData(bal.Coin.Symbol, bal[Id].Total));
					}
					if (bal_data.Count == 0) continue;
					exch_data.Add(new FundData.ExchData(exch.Name, bal_data.ToArray()));
				}
			}
			return new FundData(Id, exch_data.ToArray());
		}

		/// <summary>Set the fund balances for this fund from 'fund_data'</summary>
		public void Import(FundData fund_data, IDictionary<string, Exchange> exchanges, DateTimeOffset now)
		{
			if (fund_data.Id != Id)
				throw new Exception("Fund data not for this fund");

			foreach (var exch_data in fund_data.Exchanges)
			{
				var exch = exchanges[exch_data.Name];
				if (exch == null)
					continue;

				foreach (var bal_data in exch_data.Balances)
				{
					var coin = exch.Coins[bal_data.Symbol];
					exch.Balance[coin][Id].Update(now, total: bal_data.Total);
				}
			}
		}

		#region Equals
		public bool Equals(Fund rhs)
		{
			return rhs != null && rhs.Id == Id;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Fund);
		}
		public override int GetHashCode()
		{
			return new { Id }.GetHashCode();
		}
		#endregion

		public override string ToString()
		{
			return Id;
		}
	}
}
