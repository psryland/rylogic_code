using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.maths;
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
			Holds           = new List<FundHold>();
		}

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { [DebuggerStepThrough] get; private set; }

		/// <summary>The exchange that this balance is on</summary>
		public Exchange Exchange
		{
			get { return Coin.Exchange; }
		}

		/// <summary>The net account value</summary>
		public Unit<decimal> Total { [DebuggerStepThrough] get; private set; }

		/// <summary>The nett available balance</summary>
		public Unit<decimal> Available
		{
			[DebuggerStepThrough] get
			{
				Holds.RemoveAll(x => !x.StillNeeded(this));
				var held = Holds.Sum(x => x.Volume);
				return Maths.Max(0m._(Coin), m_available - held._(Coin));
			}
			private set { m_available = value; }
		}
		private Unit<decimal> m_available;

		/// <summary>Amount set aside for pending orders</summary>
		public Unit<decimal> HeldForTrades
		{
			[DebuggerStepThrough] get
			{
				var held = Holds.Sum(x => x.Volume);
				return m_held + held;
			}
			private set { m_held = value; }
		}
		private Unit<decimal> m_held;

		/// <summary>Deposits that have not been confirmed yet</summary>
		public Unit<decimal> Unconfirmed { get; private set; }

		/// <summary>Amount pending withdraw from the account</summary>
		public Unit<decimal> PendingWithdraw { get; private set; }

		/// <summary>The time when this balance was last updated</summary>
		public DateTimeOffset TimeStamp { get; private set; }

		/// <summary>Get the value of this balance</summary>
		public decimal Value
		{
			get
			{
				return Exchange.Model.Settings.ShowLivePrices
					? Coin.LiveValue(Total)
					: Coin.ApproximateValue(Total);
			}
		}

		/// <summary>Reserve 'volume' until the next balance update</summary>
		public Guid Hold(Unit<decimal> volume)
		{
			var ts = DateTimeOffset.Now;
			return Hold(volume, b => b.TimeStamp < ts);
		}

		/// <summary>Reserve 'volume' until 'still_needed' returns false. (Called whenever available is called on this balance)</summary>
		public Guid Hold(Unit<decimal> volume, Func<Balance, bool> still_needed)
		{
			if (volume > Available)
				throw new Exception("Cannot hold more volume than is available");

			var id = Guid.NewGuid();
			Holds.Add(new FundHold{Id = id, Volume = volume, StillNeeded = still_needed });
			return id;
		}

		/// <summary>Release the hold on funds</summary>
		public void Release(Guid hold_id)
		{
			Holds.RemoveAll(x => x.Id == hold_id);
		}

		/// <summary>Return the amount held for the given id</summary>
		public Unit<decimal> Reserved(Guid hold_id)
		{
			return Holds.FirstOrDefault(x => x.Id == hold_id)?.Volume ?? 0m._(Coin);
		}

		/// <summary>A collection of reserved balance</summary>
		public List<FundHold> Holds { get; private set; }

		[DebuggerDisplay("{Volume}")]
		public class FundHold
		{
			public Guid Id;
			public Unit<decimal> Volume;
			public Func<Balance,bool> StillNeeded;
		}
	}
}
