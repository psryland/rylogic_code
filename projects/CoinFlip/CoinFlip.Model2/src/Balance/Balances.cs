using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Balances :IEnumerable<IBalance>
	{
		// Notes:
		// - The balances of a single currency on an exchange
		// - 'Balances' rather than 'Balance' because each balance is partitioned
		//   into 'funds', where each fund is an isolated virtual account. This is
		//   so that bots can operate in parallel with other bots and make assumptions
		//   about the balance available without another bots or the user creating or
		//   cancelling trades changing the balances unexpectedly.
		// - When orders are placed, they should have an associated fund id. This is
		//   used to attribute balance changes to the correct fund.
		// - The 'Balances' object provides a readonly sum of all fund balances.

		private readonly List<Balance> m_funds;
		public Balances(Coin coin, DateTimeOffset last_updated)
			:this(coin, 0.0._(coin), last_updated)
		{}
		public Balances(Coin coin, Unit<double> total, DateTimeOffset last_updated)
			:this(coin, total, 0.0._(coin), last_updated)
		{}
		public Balances(Coin coin, Unit<double> total, Unit<double> held_on_exch, DateTimeOffset last_updated)
		{
			// Truncation error can create off by 1 LSD
			if (total < 0)
			{
				Debug.Assert(total >= -double.Epsilon);
				total = 0.0._(total);
			}
			if (!held_on_exch.WithinInclusive(0, total))
			{
				Debug.Assert(held_on_exch >= -double.Epsilon);
				Debug.Assert(held_on_exch <= total + double.Epsilon);
				held_on_exch = Math_.Clamp(held_on_exch, 0.0._(held_on_exch), total);
			}
			
			Coin = coin;
			ExchTotal = total;
			ExchHeld = held_on_exch;

			// Initialise the funds collection. Although there is a 'Balance' instance for
			// the main fund, its just a place holder. Its accessors use the 'ExchTotal'
			// and 'ExchHeld' properties in this class.
			m_funds = new List<Balance>{};
			m_funds.Add(new Balance(this, Fund.Main));

			// Select the source of defined funds based on the backtesting state.
			// Separate instances of this class are created for live trading and back testing
			var fund_data = Model.BackTesting ? SettingsData.Settings.BackTesting.TestFunds : SettingsData.Settings.LiveFunds;

			// Create each additional fund
			foreach (var fd in fund_data)
			{
				// The settings shouldn't really contain 'Main' but just in case...
				if (fd.Id == Fund.Main)
					continue;

				// Ensure the fund exists
				var bal = m_funds.Add2(new Balance(this, fd.Id));

				// Look for balance data for this fund/exchange.
				var bal_data = fd[Exchange.Name][Coin.Symbol];

				// Attribute the balance amount to fund 'fd.Id'
				AssignFundBalance(fd.Id, bal_data.Total._(Coin), bal_data.Held._(Coin), Model.UtcNow, notify:false);
			}
		}

		/// <summary>The exchange that this balance is on</summary>
		public Exchange Exchange => Coin.Exchange;

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { get; }

		/// <summary>The collection of funds containing the partitioned balances</summary>
		public IEnumerable<IBalance> Funds => m_funds;

		/// <summary>All funds except 'Main'</summary>
		public IEnumerable<IBalance> FundsExceptMain => m_funds.Where(x => x.FundId != Fund.Main);

		/// <summary>The nett total balance is the balance that the exchange reports (regardless of what the funds think they have)</summary>
		public Unit<double> NettTotal => ExchTotal + (!Model.AllowTrades ? FakeCash : 0.0._(Coin));
		private Unit<double> ExchTotal { get; set; }

		/// <summary>The nett held balance is the held amount that the exchange reports (regardless of what the funds think)</summary>
		public Unit<double> NettHeld => ExchHeld;
		private Unit<double> ExchHeld { get; set; }

		/// <summary>The nett available balance is the (total - held) balance reported by the exchange</summary>
		public Unit<double> NettAvailable => NettTotal - NettHeld;

		/// <summary>The USD value of the nett total</summary>
		public Unit<double> NettValue => Coin.ValueOf(NettTotal);

		/// <summary>Fake additional funds</summary>
		public Unit<double> FakeCash { get; set; }

		/// <summary>Access balances associated with the given fund. Unknown fund ids return an empty balance</summary>
		public IBalance this[string fund_id] => Funds.FirstOrDefault(x => x.FundId == fund_id) ?? new Balance(this, fund_id);
		public IBalance this[Fund fund] => this[fund?.Id];

		/// <summary>Set the total balance attributed to the fund 'fund_id'. Only non-null values are changed</summary>
		public void AssignFundBalance(string fund_id, Unit<double>? total, Unit<double>? held_on_exch, DateTimeOffset now, bool notify = true)
		{
			// Notes:
			// - Don't throw if the nett total becomes negative, that probably just means a fund
			//   has been assigned too much. Allow the user to reduce the allocations.

			var balance = (Balance)Funds.FirstOrDefault(x => x.FundId == fund_id);
			if (fund_id == Fund.Main)
			{
				if (total != null)
					ExchTotal = total.Value;
				if (held_on_exch != null)
					ExchHeld = held_on_exch.Value;
			}
			else
			{
				// Get the balance info for 'fund_id', create if necessary
				balance = balance ?? m_funds.Add2(new Balance(this, fund_id));

				if (total != null)
					balance.Total = total.Value;
				if (held_on_exch != null)
					balance.HeldOnExch = held_on_exch.Value;

				balance.LastUpdated = now;
				balance.CheckHolds();
			}

			// Signal balance changed
			if (notify)
			{
				balance.Invalidate();
				CoinData.NotifyBalanceChanged(Coin);
			}
		}

		/// <summary>Add or subtract an amount from a fund</summary>
		public void ChangeFundBalance(string fund_id, Unit<double>? change_total, Unit<double>? change_held_on_exch, DateTimeOffset now, bool notify = true)
		{
			var balance = (Balance)Funds.FirstOrDefault(x => x.FundId == fund_id);
			if (fund_id == Fund.Main)
			{
				if (change_total != null)
					ExchTotal += change_total.Value;
				if (change_held_on_exch != null)
					ExchHeld += change_held_on_exch.Value;
			}
			else
			{
				// Get the balance info for 'fund_id', create if necessary
				balance = balance ?? m_funds.Add2(new Balance(this, fund_id));

				if (change_total != null)
					balance.Total += change_total.Value;
				if (change_held_on_exch != null)
					balance.HeldOnExch += change_held_on_exch.Value;

				balance.LastUpdated = now;
				balance.CheckHolds();
			}

			// Signal balance changed
			if (notify)
			{
				balance.Invalidate();
				CoinData.NotifyBalanceChanged(Coin);
			}
		}

		/// <summary>Sanity check this balance. Returns null if valid</summary>
		public Exception Validate()
		{
			// Check exchange side balance
			if (ExchTotal < 0.0._(Coin))
				return new Exception("Exchange Balance Invalid: Total < 0");
			if (ExchHeld < 0.0._(Coin))
				return new Exception("Exchange Balance Invalid: Held < 0");
			if (ExchHeld > ExchTotal)
				return new Exception("Exchange Balance Invalid: Held > Total");

			// Check the fund balances are logical
			foreach (var fund in Funds)
			{
				if (fund.Total < 0.0._(Coin))
					return new Exception($"Fund {fund.FundId} balance invalid: Total < 0");
				if (fund.HeldOnExch < 0.0._(Coin))
					return new Exception($"Fund {fund.FundId} balance invalid: Held < 0");
				if (fund.HeldOnExch > fund.Total)
					return new Exception($"Fund {fund.FundId} balance invalid: Held > Total");
			}

			return null;
		}

		/// <summary>Enumerate funds</summary>
		public IEnumerator<IBalance> GetEnumerator()
		{
			return Funds.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary></summary>
		private string Description => $"{Coin} Total={NettTotal} Avail={NettAvailable}";

		/// <summary>Implements the balance associated with a single fund for a coin on an exchange</summary>
		[DebuggerDisplay("{Description,nq}")]
		private class Balance :IBalance, IValueTotalAvail, INotifyPropertyChanged
		{
			private readonly Balances m_balances;
			public Balance(Balances balances, string fund_id)
			{
				m_balances = balances;
				m_total = 0.0._(balances.Coin);
				m_held_on_exch = 0.0._(balances.Coin);
				FundId = fund_id;
				Holds = new List<FundHold>();
			}

			/// <summary>The Fund id that this balance belongs to</summary>
			public string FundId { get; }

			/// <summary>The currency that the balance is in</summary>
			public Coin Coin => m_balances.Coin;

			/// <summary>When the balance was last updated</summary>
			public DateTimeOffset LastUpdated { get; set; }

			/// <summary>A collection of reserved amounts</summary>
			public List<FundHold> Holds { get; }

			/// <summary>The value of 'Coin' (probably in USD)</summary>
			public double Value => Coin.ValueOf(1);

			/// <summary>The total balance associated with this fund (includes held amounts)</summary>
			public Unit<double> Total
			{
				// The Main fund contains whatever is left over after the other funds have their share
				get { return FundId == Fund.Main ? m_balances.NettTotal - m_balances.FundsExceptMain.Sum(x => x.Total) : m_total; }
				set
				{
					Debug.Assert(FundId != Fund.Main);
					m_total = value;
				}
			}
			private Unit<double> m_total;

			/// <summary>Total amount able to be used for new trades</summary>
			public Unit<double> Available
			{
				get
				{
					CheckHolds();
					var avail = Total - HeldForTrades;
					return avail > 0 ? avail : 0.0._(Coin);
				}
			}

			/// <summary>Total amount set aside for pending orders and trade strategies</summary>
			public Unit<double> HeldOnExch
			{
				get { return FundId == Fund.Main ? m_balances.NettHeld - m_balances.FundsExceptMain.Sum(x => x.HeldOnExch) : m_held_on_exch; }
				set
				{
					Debug.Assert(FundId != Fund.Main);
					m_held_on_exch = value;
				}
			}
			private Unit<double> m_held_on_exch;

			/// <summary>Amount set aside (locally) for trading strategies associated with this fund</summary>
			public Unit<double> HeldLocally => Holds.Sum(x => x.Volume);

			/// <summary>Total amount set aside for pending orders and trade strategies</summary>
			public Unit<double> HeldForTrades => HeldOnExch + HeldLocally;

			/// <summary>Reserve 'amount' until the next balance update</summary>
			public Guid Hold(Unit<double> amount)
			{
				var ts = Model.UtcNow;
				return Hold(amount, b => b.LastUpdated < ts);
			}

			/// <summary>Reserve 'amount' until 'still_needed' returns false.</summary>
			public Guid Hold(Unit<double> amount, Func<IBalance, bool> still_needed)
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
			public void Hold(Guid hold_id, Func<IBalance, bool> still_needed)
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
			public Unit<double> Reserved(Guid hold_id)
			{
				return Holds.Where(x => x.Id == hold_id).Sum(x => x.Volume);
			}

			/// <summary>Test the 'StillNeeded' callback on each hold, removing those that are no longer needed</summary>
			public void CheckHolds()
			{
				// Check the holds are still valid.
				// Some 'StillNeeded' calls can modify 'Holds'
				foreach (var hold in Holds.ToList())
				{
					if (hold.StillNeeded(this)) continue;
					Holds.Remove(hold);
				}
			}

			/// <summary></summary>
			public event PropertyChangedEventHandler PropertyChanged;
			private void NotifyPropertyChanged(string prop_name)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			}

			/// <summary></summary>
			public void Invalidate()
			{
				NotifyPropertyChanged(nameof(Total));
				NotifyPropertyChanged(nameof(Available));
				NotifyPropertyChanged(nameof(HeldOnExch));
				NotifyPropertyChanged(nameof(HeldLocally));
				NotifyPropertyChanged(nameof(HeldForTrades));
			}

			/// <summary>String description of this fund balance</summary>
			public string Description => $"{Coin} Fund={FundId} Avail={Available} Total={Total}";
		}








#if false



		/// <summary>The maximum amount that bots are allowed to trade</summary>
		public Unit<double> AutoTradeLimit
		{
			get { return Coin.AutoTradeLimit; }
			set { Coin.AutoTradeLimit = value._(Coin); }
		}

		/// <summary>Update the balance partitions corresponding to each fund</summary>
		public void UpdateBalancePartitions(IEnumerable<Fund> funds)
		{
			// The fund ids that we need to have
			var fund_ids = funds.Select(x => x.Id).Prepend(Fund.Main).ToHashSet(0);

			// Remove fund balances that aren't in the settings
			var to_remove = Funds.Keys.Where(x => !fund_ids.Contains(x)).ToList();
			foreach (var id in to_remove)
				RemoveFund(id);

			// Create fund balances for funds that don't already exist
			foreach (var id in fund_ids.Where(x => !Funds.ContainsKey(x)))
				AddFund(id);
		}

		/// <summary>Create a fund (sub account)</summary>
		private void AddFund(string fund_id, Unit<double>? amount = null, double? fraction = null)
		{
			// Close any existing fund associated with 'fund_id'
			RemoveFund(fund_id);

			// Get the amount to move from the main fund
			var volume =
				amount   != null ? amount.Value :                        // Create from a fixed amount
				fraction != null ? NettTotal * (double)fraction.Value : // Create as a fraction of the nett total balance
				0.0._(Coin);

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



#endif


	}
}
