using System;
using System.Diagnostics;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{Coin} Avail={Available}")]
	public class Balance
	{
		public Balance(Coin coin)
			:this(coin, 0m, 0m, 0m, 0m, 0m, DateTimeOffset.Now)
		{}
		public Balance(Coin coin, decimal total)
			:this(coin, total, total, 0m, 0m, 0m, DateTimeOffset.Now)
		{}
		public Balance(Coin coin, decimal total, decimal available, decimal uncomfirmed, decimal held_for_trades, decimal pending_withdraw, DateTimeOffset timestamp)
		{
			Coin            = coin;
			Total           = total._(coin.Symbol);
			Available       = available._(coin.Symbol);
			Unconfirmed     = uncomfirmed._(coin.Symbol);
			HeldForTrades   = held_for_trades._(coin.Symbol);
			PendingWithdraw = pending_withdraw._(coin.Symbol);
			TimeStamp       = timestamp;
		}

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { [DebuggerStepThrough] get; private set; }

		/// <summary>The net account value</summary>
		public Unit<decimal> Total { [DebuggerStepThrough] get; private set; }

		/// <summary>The nett available balance</summary>
		public Unit<decimal> Available { [DebuggerStepThrough] get; private set; }

		/// <summary>Deposits that have not been confirmed yet</summary>
		public Unit<decimal> Unconfirmed { get; private set; }

		/// <summary>Amount set aside for pending orders</summary>
		public Unit<decimal> HeldForTrades { get; private set; }

		/// <summary>Amount pending withdraw from the account</summary>
		public Unit<decimal> PendingWithdraw { get; private set; }

		/// <summary>The time when this balance was last updated</summary>
		public DateTimeOffset TimeStamp { get; private set; }

		/// <summary>Get the value of this balance</summary>
		public decimal Value
		{
			get { return Coin.NormalisedValue * Total; }
		}

		/// <summary>Place 'volume' into holding</summary>
		public void Hold(Unit<decimal> volume)
		{
			TimeStamp = DateTimeOffset.Now;
			HeldForTrades += volume;
			Available -= volume;
		}
	}
}
