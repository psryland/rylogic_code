using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>The balance of a single currency on an exchange</summary>
	[DebuggerDisplay("{Coin} Funds={Funds.Count}")]
	public class Balances
	{
		// Notes:
		// - 'Balances' rather than 'Balance' because each balance is partitioned
		//   into 'funds', where each fund is an isolated virtual account. This is
		//   so that bots can operate in parallel with other bots and make assumptions
		//   about the balance available without another bots or the user creating or
		//   cancelling trades changing the balances unexpectedly.
		// - When orders are placed, they should have an associated fund id. This is
		//   used to attribute balance changes to the correct fund.
		// - The 'Balances' object provides a readonly sum of all fund balances.

		public Balances(Coin coin, DateTimeOffset last_updated)
			:this(coin, 0m._(coin), last_updated)
		{}
		public Balances(Coin coin, Unit<decimal> total, DateTimeOffset last_updated)
			:this(coin, total, 0m._(coin), last_updated)
		{}
		public Balances(Coin coin, Unit<decimal> total, Unit<decimal> held_on_exch, DateTimeOffset last_updated)
		{
			// Truncation error can create off by 1 LSF
			if (total < 0)
			{
				Debug.Assert(total >= -decimal_.Epsilon);
				total = 0m._(total);
			}
			if (!held_on_exch.WithinInclusive(0,total))
			{
				Debug.Assert(held_on_exch >= -decimal_.Epsilon);
				Debug.Assert(held_on_exch <= total + decimal_.Epsilon);
				held_on_exch = Math_.Clamp(held_on_exch, 0m._(held_on_exch), total);
			}
			
			Coin = coin;

			// Initialise the funds - Create the main fund, with the given total/held amounts.
			// Then create the additional funds from 'Model.Funds'
			Funds = new Dictionary<string, FundBalance>{};
			Funds.Add(Fund.Main, new FundBalance(Fund.Main, Coin, total, held_on_exch));
			UpdateBalancePartitions();
		}

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { [DebuggerStepThrough] get; private set; }

		/// <summary>App logic</summary>
		public Model Model { get { return Exchange.Model; } }

		/// <summary>The exchange that this balance is on</summary>
		public Exchange Exchange { get { return Coin.Exchange; } }

		/// <summary>The collection of funds containing the partitioned balances</summary>
		public Dictionary<string, FundBalance> Funds { get; private set; }

		/// <summary>Access balances associated with the given fund. Unknown fund ids return an empty balance</summary>
		public FundBalance this[Fund fund]
		{
			get { return this[fund.Id]; }
		}
		public FundBalance this[string fund_id]
		{
			get { return Funds.TryGetValue(fund_id, out var bal) ? bal : new FundBalance(fund_id, Coin); }
		}

		/// <summary>The nett balance</summary>
		public Unit<decimal> NettTotal
		{
			[DebuggerStepThrough] get { return Funds.Values.Sum(x => x.Total); }
		}

		/// <summary>The nett available</summary>
		public Unit<decimal> NettAvailable
		{
			[DebuggerStepThrough] get { return Funds.Values.Sum(x => x.Available); }
		}

		/// <summary>Get the value of this balance</summary>
		public decimal NettValue
		{
			get { return Coin.ValueOf(NettTotal); }
		}

		/// <summary>The maximum amount that bots are allowed to trade</summary>
		public Unit<decimal> AutoTradeLimit
		{
			get { return Coin.AutoTradeLimit; }
			set { Coin.AutoTradeLimit = ((decimal)value)._(Coin); }
		}

		/// <summary>The time when this balance was last updated</summary>
		public DateTimeOffset LastUpdated
		{
			get { return Funds.Values.Max(x => x.LastUpdated); }
		}

		/// <summary>Update the balance partitions corresponding to each fund</summary>
		public void UpdateBalancePartitions()
		{
			// The fund ids that we need to have
			var fund_ids = Model.Funds.Select(x => x.Id).ToHashSet();

			// Remove fund balances that aren't in the settings
			var to_remove = Funds.Keys.Where(x => !fund_ids.Contains(x)).ToList();
			foreach (var id in to_remove)
				RemoveFund(id);

			// Create fund balances for funds that don't already exist
			foreach (var id in fund_ids.Where(x => !Funds.ContainsKey(x)))
				AddFund(id);
		}

		/// <summary>Create a fund (sub account)</summary>
		private void AddFund(string fund_id, Unit<decimal>? amount = null, double? fraction = null)
		{
			// Close any existing fund associated with 'fund_id'
			RemoveFund(fund_id);

			// Get the amount to move from the main fund
			var volume =
				amount   != null ? amount.Value :                        // Create from a fixed amount
				fraction != null ? NettTotal * (decimal)fraction.Value : // Create as a fraction of the nett total balance
				0m._(Coin);

			// Check there is enough in the main fund to cover 'volume'
			var pot = Funds[Fund.Main];
			if (pot.Available < volume)
				throw new Exception($"Cannot create fund for {Coin}, insufficient available (Avail:{pot.Available}, Requested:{volume})");

			// Move balance from the main fund to this new one
			var bal = Funds[fund_id] = new FundBalance(fund_id, Coin);
			bal.Update(Model.UtcNow, total: bal.Total + volume);
			pot.Update(Model.UtcNow, total: pot.Total - volume);

			Debug.Assert(AssertValid());
		}

		/// <summary>Close a fund, returning the balance to the main fund</summary>
		private void RemoveFund(string fund_id)
		{
			// Can't remove the main fund
			if (fund_id == Fund.Main || !Funds.ContainsKey(fund_id))
				return;

			var bal = Funds[fund_id];
			var volume = bal.Total;

			// Return the funds to the main fund
			var pot = Funds[Fund.Main];
			bal.Update(Model.UtcNow, total: bal.Total - volume);
			pot.Update(Model.UtcNow, total: pot.Total + volume);
			Funds.Remove(fund_id);

			Debug.Assert(AssertValid());
		}

		/// <summary>Update the balances</summary>
		public void Update(FundBalance update)
		{
			// This update comes from the exchange, which doesn't know about 'funds'.
			// We need to divide the total/held amounts among the funds.
			// Assume that the adjustments made to the funds at the time orders are
			// placed are correct, with any error amount overflowing into the main fund.

			Debug.Assert(update.FundId == Fund.Main);
			Debug.Assert(update.Holds.Count == 0);

			// Could ensure that the main fund balances are always >= 0 here by reducing the 
			// balances on the other funds. However, I don't know what that might do to any
			// running bots. For now, I'll just let the main fund balances go negative and
			// let the user sort it out.

			// Find the sum of total and held amounts in the non-main funds
			var main = Funds[Fund.Main];
			var funds = Funds.Values.Except(main);
			var total = funds.Sum(x => x.Total);
			var held  = funds.Sum(x => x.HeldOnExch);

			// Set the main fund to the remainder of the non-main funds and the updated amount.
			main.Update(update.LastUpdated, total: update.Total - total, held_on_exch: update.HeldOnExch - held);

			// Notify each non-fund of the balance update
			foreach (var fund in funds)
				fund.Update(update.LastUpdated);
		}

		/// <summary>Sanity check this balance</summary>
		public bool AssertValid()
		{
			foreach (var fund in Funds)
			{
				var bal = fund.Value;
				if (bal.FundId != fund.Key)
					throw new Exception("Balance Invalid: Key/fund id mismatch");
				if (bal.Total < 0m._(Coin))
					throw new Exception("Balance Invalid: Total < 0");
				if (bal.HeldForTrades < 0m._(Coin))
					throw new Exception("Balance Invalid: HeldForTrades < 0");
				if (bal.HeldForTrades > bal.Total)
					throw new Exception("Balance Invalid: HeldForTrades > Total");
			}

			if (NettAvailable < 0)
				throw new Exception("Balance Invalid: Available < 0");
			if (NettAvailable > NettTotal)
				throw new Exception("Balance Invalid: Available > Total");

			return true;
		}
	}

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
			LastUpdated = DateTimeOffset.MinValue;
			Total       = total;
			HeldOnExch  = held;
			Holds       = new List<FundHold>();

			if (fund_id != Fund.Main)
			{
				// Initialise the balance from settings data.
				var fund_data = Model.Settings.Funds.FirstOrDefault(x => x.Id == fund_id);
				var exch_data = fund_data?.Exchanges.FirstOrDefault(x => x.Name == coin.Exchange.Name);
				var bal_data  = exch_data?.Balances.FirstOrDefault(x => x.Symbol == coin.Symbol);
				if (bal_data != null)
					Update(LastUpdated, total: bal_data.Total._(coin));
			}
		}

		/// <summary>The fund id that this balance belongs to</summary>
		public string FundId { get; private set; }

		/// <summary>App logic access</summary>
		public Model Model { get { return Coin.Model; } }

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { [DebuggerStepThrough] get; private set; }

		/// <summary>The time when this balance was last updated</summary>
		public DateTimeOffset LastUpdated { get; private set; }

		/// <summary>The total amount of 'Coin' on 'Exchange' in this fund</summary>
		public Unit<decimal> Total { [DebuggerStepThrough] get; private set; }

		/// <summary>Amount set aside (on the exchange) for pending orders associated with this fund</summary>
		public Unit<decimal> HeldOnExch { [DebuggerStepThrough] get; private set; }

		/// <summary>Amount set aside (locally) for trading strategies associated with this fund</summary>
		public Unit<decimal> HeldLocally { [DebuggerStepThrough] get { return Holds.Sum(x => x.Volume); } }

		/// <summary>Total amount set aside for pending orders and trade strategies</summary>
		public Unit<decimal> HeldForTrades { [DebuggerStepThrough] get { return HeldOnExch + HeldLocally; } }

		/// <summary>The available amount of currency</summary>
		public Unit<decimal> Available
		{
			[DebuggerStepThrough] get
			{
				// Remove any stale holds
				CheckHolds();

				// Calculate the available funds
				var avail = Total - HeldForTrades;
				return Math_.Max(0m._(Coin), avail);
			}
		}

		/// <summary>A collection of reserved volumes</summary>
		public List<FundHold> Holds { get; private set; }

		/// <summary>Reserve 'volume' until the next balance update</summary>
		public Guid Hold(Unit<decimal> volume)
		{
			var ts = Model.UtcNow;
			return Hold(volume, b => b.LastUpdated < ts);
		}

		/// <summary>Reserve 'volume' until 'still_needed' returns false.</summary>
		public Guid Hold(Unit<decimal> volume, Func<FundBalance, bool> still_needed)
		{
			// 'Available' removes stale holds
			if (volume > Available)
				throw new Exception("Cannot hold more volume than is available");

			// Create a new fund hold
			var id = Guid.NewGuid();
			Holds.Add(new FundHold{ Id = id, Volume = volume, StillNeeded = still_needed });
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
		public string Description
		{
			get { return $"{Coin} Fund={FundId} Avail={Available} Total={Total}"; }
		}
	}

	/// <summary>A reserve of a certain amount of currency</summary>
	[DebuggerDisplay("{Volume}")]
	public class FundHold
	{
		/// <summary>The Id of the reserver</summary>
		public Guid Id;

		/// <summary>How much to reserve</summary>
		public Unit<decimal> Volume;

		/// <summary>Callback function to test whether the reserve is still required</summary>
		public Func<FundBalance,bool> StillNeeded;
	}
}
