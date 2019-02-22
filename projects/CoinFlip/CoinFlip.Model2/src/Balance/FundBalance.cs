using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>The balance of a currency on an exchange, attributed to a fund</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class FundBalance
	{
		// Notes:
		// - Negative balances are allowed because non-main funds can be assigned any balance.
		//   The main fund balance is the exchange balance minus the sum of other fund balances.

		public FundBalance(string fund_id, Coin coin)
			:this(fund_id, coin, 0m._(coin), 0m._(coin))
		{}
		public FundBalance(string fund_id, Coin coin, Unit<decimal> total, Unit<decimal> held)
		{
			FundId      = fund_id;
			Coin        = coin;
			Total       = total;
			HeldOnExch  = held;
			LastUpdated = DateTimeOffset.MinValue;
			Holds       = new List<FundHold>();

			if (fund_id != Fund.Main)
			{
				// Initialise the balance from settings data.
				var fund_data = SettingsData.Settings.Funds.FirstOrDefault(x => x.Id == fund_id);
				var exch_data = fund_data?.Exchanges.FirstOrDefault(x => x.Name == coin.Exchange.Name);
				var bal_data  = exch_data?.Balances.FirstOrDefault(x => x.Symbol == coin.Symbol);
				if (bal_data != null)
					Update(LastUpdated, total: bal_data.Total._(coin));
			}
		}

		/// <summary>The fund id that this balance belongs to</summary>
		public string FundId { get; }

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { get; }

		/// <summary>A collection of reserved amounts</summary>
		public List<FundHold> Holds { get; }

		/// <summary>The time when this balance was last updated</summary>
		public DateTimeOffset LastUpdated { get; private set; }

		/// <summary>The total amount of 'Coin' on 'Exchange' in this fund</summary>
		public Unit<decimal> Total { [DebuggerStepThrough] get; private set; }

		/// <summary>Amount set aside (on the exchange) for pending orders associated with this fund</summary>
		public Unit<decimal> HeldOnExch { [DebuggerStepThrough] get; private set; }

		/// <summary>Amount set aside (locally) for trading strategies associated with this fund</summary>
		public Unit<decimal> HeldLocally => Holds.Sum(x => x.Volume);

		/// <summary>Total amount set aside for pending orders and trade strategies</summary>
		public Unit<decimal> HeldForTrades => HeldOnExch + HeldLocally;

		/// <summary>The available amount of currency</summary>
		public Unit<decimal> Available
		{
			[DebuggerStepThrough] get
			{
				// Remove any stale holds
				CheckHolds();

				// Calculate the available funds
				var avail = Total - HeldForTrades;
				return avail > 0 ? avail : 0m._(Coin);
			}
		}

		/// <summary>Reserve 'amount' until the next balance update</summary>
		public Guid Hold(Unit<decimal> amount)
		{
			var ts = Model.UtcNow;
			return Hold(amount, b => b.LastUpdated < ts);
		}

		/// <summary>Reserve 'amount' until 'still_needed' returns false.</summary>
		public Guid Hold(Unit<decimal> amount, Func<FundBalance, bool> still_needed)
		{
			// 'Available' removes stale holds
			if (amount > Available)
				throw new Exception("Cannot hold more than is available");

			// Create a new fund hold
			var id = Guid.NewGuid();
			Holds.Add(new FundHold(id, amount, still_needed));
			return id;
		}

		/// <summary>Update the 'StillNeeded' function for a balance hold</summary>
		public void Hold(Guid hold_id, Func<FundBalance, bool> still_needed)
		{
			var hold = Holds.First(x => x.Id == hold_id);
			hold.StillNeeded = still_needed;
		}

		/// <summary>Release a hold on funds</summary>
		public void Release(Guid hold_id)
		{
			Holds.RemoveAll(x => x.Id == hold_id);
		}

		/// <summary>Return the amount held for the given id</summary>
		public Unit<decimal> Reserved(Guid hold_id)
		{
			return Holds.Where(x => x.Id == hold_id).Sum(x => x.Volume);
		}

		/// <summary>Test the 'StillNeeded' callback on each hold, removing those that are no longer needed</summary>
		private void CheckHolds()
		{
			// Check the holds are still valid.
			// Some 'StillNeeded' calls can modify 'Holds'
			foreach (var hold in Holds.ToList())
			{
				if (hold.StillNeeded(this)) continue;
				Holds.Remove(hold);
			}
		}

		/// <summary>Update this fund balance</summary>
		public void Update(DateTimeOffset last_updated, Unit<decimal>? total = null, Unit<decimal>? held_on_exch = null)
		{
			// Update the balance values
			if (total        != null) Total      = total.Value;
			if (held_on_exch != null) HeldOnExch = held_on_exch.Value;
			LastUpdated = last_updated;

			// Check the holds are still valid.
			CheckHolds();
		}

		/// <summary>String description of this fund balance</summary>
		public string Description => $"{Coin} Fund={FundId} Avail={Available} Total={Total}";
	}
}
