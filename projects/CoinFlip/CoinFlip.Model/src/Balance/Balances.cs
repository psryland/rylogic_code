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
		// - 'notify' is used to notify balance changed on the 'coin'
		// - m_fund[0].Total != ExchTotal. m_fund[0].Total is the exchange total minus the fund totals.

		private readonly List<Balance> m_funds;
		public Balances(Coin coin, DateTimeOffset last_updated)
			:this(coin, 0.0._(coin), last_updated)
		{}
		public Balances(Coin coin, Unit<double> total, DateTimeOffset last_updated, bool notify = false)
			:this(coin, total, 0.0._(coin), last_updated, notify)
		{}
		public Balances(Coin coin, Unit<double> total, Unit<double> held_on_exch, DateTimeOffset last_updated, bool notify = false)
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

			// Initialise the funds collection. Although there is a 'Balance' instance for
			// the main fund, its just a place holder. Its accessors use the 'ExchTotal'
			// and 'ExchHeld' properties in this class.
			m_funds = new List<Balance>{};
			m_funds.Add(new Balance(this, new Fund(Fund.Main)));

			// Assign the balance info from the exchange
			ExchTotal = total;
			ExchHeld = held_on_exch;

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
				var fund = new Fund(fd.Id);
				var bal = m_funds.Add2(new Balance(this, fund));

				// Look for balance data for this fund/exchange.
				var bal_data = fd[Exchange.Name][Coin.Symbol];

				// Attribute the balance amount to fund 'fd.Id'
				AssignFundBalance(fund, bal_data.Total._(Coin), bal_data.Held._(Coin), Model.UtcNow, notify: notify);
			}
		}

		/// <summary>The exchange that this balance is on</summary>
		public Exchange Exchange => Coin.Exchange;

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { get; }

		/// <summary>The collection of funds containing the partitioned balances</summary>
		public IEnumerable<IBalance> Funds => m_funds;

		/// <summary>All funds except 'Main'</summary>
		public IEnumerable<IBalance> FundsExceptMain => Funds.Skip(1);

		/// <summary>The nett total balance is the balance that the exchange reports (regardless of what the funds think they have)</summary>
		public Unit<double> NettTotal => ExchTotal;
		private Unit<double> ExchTotal
		{
			get => m_exch_total;
			set
			{
				if (m_exch_total == value) return;
				m_exch_total = value;
				m_funds[0].NotifyPropertyChanged(nameof(Balance.Total));
			}
		}
		private Unit<double> m_exch_total;

		/// <summary>The nett held balance is the held amount that the exchange reports (regardless of what the funds think)</summary>
		public Unit<double> NettHeld => ExchHeld;
		private Unit<double> ExchHeld
		{
			get => m_exch_held;
			set
			{
				if (m_exch_held == value) return;
				m_exch_held = value;
				m_funds[0].NotifyPropertyChanged(nameof(Balance.Available));
				m_funds[0].NotifyPropertyChanged(nameof(Balance.Held));
			}
		}
		private Unit<double> m_exch_held;

		/// <summary>The nett available balance is the (total - held) balance reported by the exchange</summary>
		public Unit<double> NettAvailable => NettTotal - NettHeld;

		/// <summary>The value of the nett total in valuation currency</summary>
		public Unit<double> NettValue => Coin.ValueOf(NettTotal);

		/// <summary>Access balances associated with the given fund. Unknown fund ids return an empty balance</summary>
		public IBalance this[Fund fund] => Funds.FirstOrDefault(x => x.Fund == fund) ?? new Balance(this, fund);

		/// <summary>Set the total balance attributed to the fund 'fund_id'. Only non-null values are changed</summary>
		public void AssignFundBalance(Fund fund, Unit<double>? total, Unit<double>? held, DateTimeOffset now, bool notify = true)
		{
			// Notes:
			// - Don't throw if the nett total becomes negative, that probably just means a fund
			//   has been assigned too much. Allow the user to reduce the allocations.

			var balance = (Balance)Funds.FirstOrDefault(x => x.Fund == fund) ?? m_funds.Add2(new Balance(this, fund));
			if (fund.Id == Fund.Main)
			{
				if (total != null)
					ExchTotal = total.Value;
				if (held != null)
					ExchHeld = held.Value;
			}
			else
			{
				// Get the balance info for 'fund_id', create if necessary
				if (total != null)
					balance.Total = total.Value;
				if (held != null)
					balance.Held = held.Value;
			}

			balance.LastUpdated = now;
			balance.CheckHolds();

			// Signal balance changed
			if (notify)
				CoinData.NotifyBalanceChanged(Coin);
		}

		/// <summary>Add or subtract an amount from a fund</summary>
		public void ChangeFundBalance(Fund fund, Unit<double>? change_amount, DateTimeOffset now, bool notify = true)
		{
			// Get the balance info for 'fund_id', create if necessary
			var balance = (Balance)Funds.FirstOrDefault(x => x.Fund == fund) ?? m_funds.Add2(new Balance(this, fund));

			// Apply the balance change to the total
			if (change_amount != null && balance.LastUpdated <= now)
			{
				balance.Total += change_amount.Value;
				balance.LastUpdated = now;
			}

			// Look for any expired holds
			balance.CheckHolds();

			// Signal balance changed
			if (notify)
				CoinData.NotifyBalanceChanged(Coin);
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
					return new Exception($"Fund {fund.Fund.Id} balance invalid: Total < 0");
				if (fund.Held < 0.0._(Coin))
					return new Exception($"Fund {fund.Fund.Id} balance invalid: Held < 0");
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
		private class Balance :IBalance, IValueTotalAvail
		{
			private readonly Balances m_balances;
			public Balance(Balances balances, Fund fund)
			{
				m_balances = balances;
				m_total = 0.0._(balances.Coin);
				m_held = 0.0._(balances.Coin);
				Fund = fund;
				Holds = new List<FundHold>();
			}

			/// <summary>The Fund that this balance belongs to</summary>
			public Fund Fund { get; }

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
				get { return Fund.Id == Fund.Main ? m_balances.ExchTotal - m_balances.FundsExceptMain.Sum(x => x.Total) : m_total; }
				set
				{
					if (Total == value) return;
					if (Fund.Id == Fund.Main)
						m_balances.ExchTotal = value + m_balances.FundsExceptMain.Sum(x => x.Total);
					else
						m_total = value;

					NotifyPropertyChanged(nameof(Total));
					NotifyPropertyChanged(nameof(Available));
				}
			}
			private Unit<double> m_total;

			/// <summary>Total amount able to be used for new trades</summary>
			public Unit<double> Available
			{
				get
				{
					CheckHolds();
					var avail = Total - Held;
					return avail > 0 ? avail : 0.0._(Coin);
				}
			}

			/// <summary>Total amount set aside for pending orders and trade strategies</summary>
			public Unit<double> Held
			{
				get { return Fund.Id == Fund.Main ? m_balances.ExchHeld - m_balances.FundsExceptMain.Sum(x => x.Held) : m_held; }
				set
				{
					if (Held == value) return;
					if (Fund.Id == Fund.Main)
						m_balances.ExchHeld = value + m_balances.FundsExceptMain.Sum(x => x.Held);
					else
						m_held = value;

					NotifyPropertyChanged(nameof(Available));
					NotifyPropertyChanged(nameof(Held));
				}
			}
			private Unit<double> m_held;

			/// <summary>Reserve 'amount' until 'still_needed' returns false.</summary>
			public Guid Hold(long? order_id, Unit<double> amount, Func<IBalance, bool> still_needed)
			{
				// 'Available' removes stale holds
				if (amount > Available)
					throw new Exception("Cannot hold more than is available");

				// Create a new fund hold
				var id = Guid.NewGuid();
				Holds.Add(new FundHold(id, order_id, amount, still_needed));
				NotifyPropertyChanged(nameof(Available));
				NotifyPropertyChanged(nameof(Held));
				return id;
			}

			/// <summary>Update the 'StillNeeded' function for a balance hold</summary>
			public void Update(Guid hold_id, long? order_id = null, Func<IBalance, bool> still_needed = null)
			{
				var hold = Holds.First(x => x.Id == hold_id);
				if (order_id != null) hold.OrderId = order_id.Value;
				if (still_needed != null) hold.StillNeeded = still_needed;
			}

			/// <summary>Release a hold on funds</summary>
			public void Release(Guid? hold_id = null, long? order_id = null)
			{
				if (Holds.RemoveAll(x => x.Id == hold_id || x.OrderId == order_id) != 0)
					NotifyPropertyChanged(nameof(Held));
			}

			/// <summary>Return the amount held for the given id</summary>
			public Unit<double> Reserved(Guid hold_id)
			{
				return Holds.Where(x => x.Id == hold_id).Sum(x => x.Amount);
			}

			/// <summary>Test the 'StillNeeded' callback on each hold, removing those that are no longer needed</summary>
			public void CheckHolds()
			{
				// Check the holds are still valid.
				// Some 'StillNeeded' calls can modify 'Holds'
				var holds_removed = false;
				foreach (var hold in Holds.ToList())
				{
					if (hold.StillNeeded(this)) continue;
					Holds.Remove(hold);
					holds_removed = true;
				}
				if (holds_removed)
					NotifyPropertyChanged(nameof(Held));
			}

			/// <summary></summary>
			public event PropertyChangedEventHandler PropertyChanged;
			public void NotifyPropertyChanged(string prop_name)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			}

			/// <summary>String description of this fund balance</summary>
			public string Description => $"{Coin} Fund={Fund.Id} Avail={Available} Total={Total}";
		}
	}
}
