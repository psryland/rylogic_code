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
			Funds.Add(Fund.Main, new FundBalance(Fund.Main, Coin, total, held_on_exch, last_updated));
			UpdateBalancePartitions(Enumerable.Empty<Fund>());
		}

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { get; }

		/// <summary>The collection of funds containing the partitioned balances</summary>
		public Dictionary<string, FundBalance> Funds { get; }

		/// <summary>The exchange that this balance is on</summary>
		public Exchange Exchange => Coin.Exchange;

		/// <summary>Access balances associated with the given fund. Unknown fund ids return an empty balance</summary>
		public FundBalance this[Fund fund] => this[fund.Id];
		public FundBalance this[string fund_id] => Funds.TryGetValue(fund_id, out var bal) ? bal : new FundBalance(fund_id, Coin);

		/// <summary>The nett balance</summary>
		public Unit<decimal> NettTotal => Funds.Values.Sum(x => x.Total);

		/// <summary>The nett available</summary>
		public Unit<decimal> NettAvailable => Funds.Values.Sum(x => x.Available);

		/// <summary>Get the value of this balance</summary>
		public decimal NettValue => Coin.ValueOf(NettTotal);

		/// <summary>The time when this balance was last updated</summary>
		public DateTimeOffset LastUpdated => Funds.Values.Max(x => x.LastUpdated);

		/// <summary>The maximum amount that bots are allowed to trade</summary>
		public Unit<decimal> AutoTradeLimit
		{
			get { return Coin.AutoTradeLimit; }
			set { Coin.AutoTradeLimit = ((decimal)value)._(Coin); }
		}

		/// <summary>Update the balance partitions corresponding to each fund</summary>
		public void UpdateBalancePartitions(IEnumerable<Fund> funds)
		{
			// The fund ids that we need to have
			var fund_ids = funds.Select(x => x.Id).Prepend(Fund.Main).ToHashSet();

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

		/// <summary>Update the distribution of the total available amount of this coin amongst the funds</summary>
		public void Update(Unit<decimal> total, Unit<decimal> held_on_exchange, DateTimeOffset last_updated)
		{
			// This update comes from the exchange, which doesn't know about 'funds'.
			// We need to divide the total/held amounts among the funds.
			// Assume that the adjustments made to the funds at the time orders are
			// placed are correct, with any error amount overflowing into the main fund.

			// We could ensure that the main fund balances are always >= 0 here by reducing the 
			// balances on the other funds. However, I don't know what that might do to any
			// running bots. For now, I'll just let the main fund balances go negative and
			// let the user sort it out.

			// Find the sum of total and held amounts in the non-main funds
			var main = Funds[Fund.Main];
			var funds = Funds.Values.Except(main);
			var funds_total = funds.Sum(x => x.Total);
			var funds_held = funds.Sum(x => x.HeldOnExch);

			// Set the main fund to the remainder of the non-main funds and the updated amount.
			main.Update(last_updated, total: total - funds_total, held_on_exch: held_on_exchange - funds_held);

			// Notify each non-fund of the balance update
			foreach (var fund in funds)
				fund.Update(last_updated);
		}
		public void Update(Balances balance)
		{
			if (balance.Funds.Count != 1)
				throw new Exception("Balance Update expects only the Main fund to have a value");

			var main = balance.Funds[Fund.Main];
			if (main.HeldForTrades != 0)
				throw new Exception("Balance Update expects 'HeldForTrades' to be zero");
			if (main.HeldLocally != 0)
				throw new Exception("Balance Update expects 'HeldLocally' to be zero");

			Update(main.Total, main.HeldOnExch, main.LastUpdated);
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
}
