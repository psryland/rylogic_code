using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoinFlip
{
	/// <summary>A fund is a partition of the user's balances on all exchanges</summary>
	[DebuggerDisplay("{Id}")]
	public class Fund
	{
		// Notes:
		// - A fund is a virtual sub-account for 
		// - Fund provides an interface to the balances of a single fund
		// - A fund only manages the creation/destruction of balance contexts,
		//   not the amounts in each balance context.
		// - 'Fund' is used so that exchanges can create the balance contexts.
		// - This is basically a wrapper around a string

		/// <summary>The default fund</summary>
		public const string Main = "Main";

		public Fund(Model model, string id)
		{
			Model = model;
			Id = id;
		}

		/// <summary>The unique Id for this fund</summary>
		public string Id { get; private set; }

		/// <summary>For binding to DGV</summary>
		public Fund This { get { return this; } }

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				m_model = value;
			}
		}
		private Model m_model;

		/// <summary>Return the balance of 'coin' associated with this fund on the given exchange</summary>
		public FundBalance this[Coin coin]
		{
			get { return coin.Balances[Id]; }
		}

		/// <summary>Export settings data for this fund</summary>
		public Settings.FundData Export()
		{
			// Return the non-zero balances associated with this fund on each exchange.
			// Don't bother with balances for the main fund because it is overwritten by updates.
			var exch_data = new List<Settings.FundData.ExchData>();
			if (Id != Main)
			{
				foreach (var exch in Model.TradingExchanges)
				{
					var bal_data = new List<Settings.FundData.BalData>();
					foreach (var bal in exch.Balance.Values)
					{
						if (bal[Id].Total == 0) continue;
						bal_data.Add(new Settings.FundData.BalData(bal.Coin.Symbol, bal[Id].Total));
					}
					if (bal_data.Count == 0) continue;
					exch_data.Add(new Settings.FundData.ExchData(exch.Name, bal_data.ToArray()));
				}
			}
			return new Settings.FundData(Id, exch_data.ToArray());
		}

		/// <summary>Set the fund balances for this fund from 'fund_data'</summary>
		public void Import(Settings.FundData fund_data)
		{
			if (fund_data.Id != Id) throw new Exception("Fund data not for this fund");
			foreach (var exch_data in fund_data.Exchanges)
			{
				var exch = Model.Exchanges[exch_data.Name];
				if (exch == null) continue;
				foreach (var bal_data in exch_data.Balances)
				{
					var coin = exch.Coins[bal_data.Symbol];
					exch.Balance[coin][Id].Update(Model.UtcNow, total: bal_data.Total);
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
