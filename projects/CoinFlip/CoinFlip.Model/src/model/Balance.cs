using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	[DebuggerDisplay("{Coin} Avail={Available}")]
	public class Balance
	{
		public Balance(Coin coin, DateTimeOffset timestamp)
			:this(coin, 0m._(coin), timestamp)
		{}
		public Balance(Coin coin, Unit<decimal> total, DateTimeOffset timestamp)
			:this(coin, total, 0m._(coin), timestamp)
		{}
		public Balance(Coin coin, Unit<decimal> total, Unit<decimal> held_for_trades, DateTimeOffset timestamp)
			:this(coin, total, held_for_trades, timestamp, 0m._(coin), 0m._(coin))
		{}
		public Balance(Coin coin, Unit<decimal> total, Unit<decimal> held_for_trades, DateTimeOffset timestamp, Unit<decimal> uncomfirmed, Unit<decimal> pending_withdraw)
		{
			// Truncation error can create off by 1 LSF
			if (total < 0)
			{
				Debug.Assert(total >= -decimal_.Epsilon);
				total = 0m._(total);
			}
			if (held_for_trades < 0 || held_for_trades > total)
			{
				Debug.Assert(held_for_trades >= -decimal_.Epsilon);
				Debug.Assert(held_for_trades <= total + decimal_.Epsilon);
				held_for_trades = Maths.Clamp(held_for_trades, 0m._(held_for_trades), total);
			}

			Coin            = coin;
			Total           = total;
			HeldForTrades   = held_for_trades;
			TimeStamp       = timestamp;
			Unconfirmed     = uncomfirmed;
			PendingWithdraw = pending_withdraw;
			Holds           = new List<FundHold>();
			FakeCash        = new List<decimal>();

			Debug.Assert(AssertValid());
		}
		public Balance(Balance rhs)
			:this(rhs.Coin, rhs.Total, rhs.HeldForTrades, rhs.TimeStamp, rhs.Unconfirmed, rhs.PendingWithdraw)
		{}

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
				return Maths.Max(0m._(Coin), Total - HeldForTrades + fake._(Coin) - held._(Coin));
			}
		}

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

		/// <summary>Amount pending withdraw from the account</summary>
		public Unit<decimal> PendingWithdraw { get; private set; }

		/// <summary>Deposits that have not been confirmed yet</summary>
		public Unit<decimal> Unconfirmed { get; private set; }

		/// <summary>The time when this balance was last updated</summary>
		public DateTimeOffset TimeStamp { get; private set; }

		/// <summary>Get the value of this balance</summary>
		public decimal Value
		{
			get { return Coin.Value(Total); }
		}

		/// <summary>The maximum amount that bots are allowed to trade</summary>
		public Unit<decimal> AutoTradeLimit
		{
			get { return Coin.AutoTradeLimit; }
			set { Coin.AutoTradeLimit = ((decimal)value)._(Coin); }
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

		/// <summary>Update the still needed function for a balance hold</summary>
		public void Hold(Guid hold_id, Func<Balance, bool> still_needed)
		{
			var hold = Holds.First(x => x.Id == hold_id);
			hold.StillNeeded = still_needed;
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
			HeldForTrades   = rhs.HeldForTrades;
			Unconfirmed     = rhs.Unconfirmed;
			PendingWithdraw = rhs.PendingWithdraw;
			TimeStamp       = rhs.TimeStamp;

			// Transfer the 'holds' to 'value'
			var holds = rhs.Holds.ToList(); // StillNeeded can modify the collection
			foreach (var hold in holds.Where(x => x.StillNeeded(rhs)))
				Holds.Add(hold);

			// Transfer the 'fake cash' to 'value'
			foreach (var fake in rhs.FakeCash)
				FakeCash.Add(fake);
		}

		/// <summary>Sanity check this balance</summary>
		public bool AssertValid()
		{
			if (Total < 0m._(Coin))
				throw new Exception("Balance Invalid: Total < 0");
			if (HeldForTrades < 0m._(Coin))
				throw new Exception("Balance Invalid: HeldForTrades < 0");
			if (Unconfirmed < 0m._(Coin))
				throw new Exception("Balance Invalid: Unconfirmed < 0");
			if (PendingWithdraw < 0m._(Coin))
				throw new Exception("Balance Invalid: PendingWithdraw < 0");
			if (HeldForTrades > Total)
				throw new Exception("Balance Invalid: HeldForTrades > Total");

			return true;
		}
	}
}
