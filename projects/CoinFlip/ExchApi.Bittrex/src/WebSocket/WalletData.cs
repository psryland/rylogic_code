using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bittrex.API.DomainObjects;
using Newtonsoft.Json.Linq;

namespace Bittrex.API.Subscriptions
{
	public class WalletData : IEnumerable<Balance>
	{
		private readonly BittrexApi m_api;
		private readonly Dictionary<string, Balance> m_data;

		public WalletData(BittrexApi api)
		{
			m_api = api;
			m_data = new Dictionary<string, Balance>();
		}

		/// <summary>Return the balance of the given currency</summary>
		public Balance this[string currency]
		{
			get
			{
				if (currency == "USD") currency = "USDT";
				return m_data.TryGetValue(currency, out var balance) ? balance : new Balance(currency);
			}
			set
			{
				if (currency == "USD") currency = "USDT";
				m_data[currency] = value;
			}
		}

		/// <summary>Parse a wallet update message</summary>
		public void ParseUpdate(JArray data)
		{
			if (data.Count < 5)
				throw new Exception("Insufficient data for Wallet update");

			//// Only care about exchange wallets for now
			//var wtype = (EWalletType)Enum.Parse(typeof(EWalletType), data[0].Value<string>(), true);
			//if (wtype != EWalletType.Exchange)
			//	return;

			//// Get the currency that the update is for
			//var sym = data[1].Value<string>();
			//
			//// Update the wallet data
			//var bal = this[sym];
			//bal.Total = data[2].Value<decimal>();
			//bal.UnsettledInterest = data[3].Value<decimal>();
			//bal.Available = data[4].Value<decimal?>() ?? bal.Available;
			//this[bal.Symbol] = bal;
			//
			//if (data[4].Value<decimal?>() == null)
			//{
			//	// Send a message to say what we want calculated
			//	using (Misc.NoSyncContext())
			//		m_api.WebSocket.SendAsync($"[0, \"calc\", null, [ [\"wallet_exchange_{sym}\"] ] ]", m_api.CancelToken).Wait();
			//}
		}

		#region IEnumerable
		public IEnumerator<Balance> GetEnumerator()
		{
			return m_data.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
