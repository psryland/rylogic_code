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
			FakeCash        = new List<decimal>();
		}

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { [DebuggerStepThrough] get; private set; }

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return Exchange.Model; }
		}

		/// <summary>The exchange that this balance is on</summary>
		public Exchange Exchange
		{
			get { return Coin.Exchange; }
		}

		/// <summary>The net account value</summary>
		public Unit<decimal> Total
		{
			[DebuggerStepThrough] get
			{
				var fake = Model.AllowTrades ? 0m : FakeCash.Sum(x => x);
				return m_total + fake._(Coin);
			}
			private set { m_total = value; }
		}
		private Unit<decimal> m_total;

		/// <summary>The nett available balance</summary>
		public Unit<decimal> Available
		{
			[DebuggerStepThrough] get
			{
				Holds.RemoveAll(x => !x.StillNeeded(this));
				var held = Holds.Sum(x => x.Volume);
				var fake = Model.AllowTrades ? 0m : FakeCash.Sum(x => x);
				return Maths.Max(0m._(Coin), m_available + fake._(Coin) - held._(Coin));
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
			get { return Coin.Value(Total); }
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

		/// <summary>A collection of fake additional balance, for testing</summary>
		public List<decimal> FakeCash { get; private set; }

		[DebuggerDisplay("{Volume}")]
		public class FundHold
		{
			public Guid Id;
			public Unit<decimal> Volume;
			public Func<Balance,bool> StillNeeded;
		}

		/// <summary>Update this balance using 'rhs'</summary>
		public void Update(Balance rhs)
		{
			// Sanity check
			if (Coin != rhs.Coin)
				throw new Exception("Update for the wrong balance");

			// Update the update-able parts
			Total           = rhs.Total;
			Available       = rhs.Available;
			HeldForTrades   = rhs.HeldForTrades;
			Unconfirmed     = rhs.Unconfirmed;
			PendingWithdraw = rhs.PendingWithdraw;
			TimeStamp       = rhs.TimeStamp;

			// Transfer the 'holds' to 'value'
			var holds = rhs.Holds; // StillNeeded can modify the collection
			foreach (var hold in holds.Where(x => x.StillNeeded(rhs)))
				Holds.Add(hold);

			// Transfer the 'fake cash' to 'value'
			foreach (var fake in rhs.FakeCash)
				FakeCash.Add(fake);
		}
	}
}
