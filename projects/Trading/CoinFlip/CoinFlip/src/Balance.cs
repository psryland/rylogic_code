using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinFlip
{
	public class Balance
	{
		public Balance(Coin coin, decimal total, decimal available, decimal uncomfirmed, decimal held_for_trades, decimal pending_withdraw)
		{
			Coin            = coin;
			Total           = total;
			Available       = available;
			Unconfirmed     = uncomfirmed;
			HeldForTrades   = held_for_trades;
			PendingWithdraw = pending_withdraw;
		}

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { get; private set; }

		/// <summary>The net account value</summary>
		public decimal Total { get; private set; }

		/// <summary>The nett available balance</summary>
		public decimal Available { get; private set; }

		/// <summary>Deposits that have not been confirmed yet</summary>
		public decimal Unconfirmed { get; private set; }

		/// <summary>Amount set aside for pending orders</summary>
		public decimal HeldForTrades { get; private set; }

		/// <summary>Amount pending withdraw from the account</summary>
		public decimal PendingWithdraw { get; private set; }
	}
}
