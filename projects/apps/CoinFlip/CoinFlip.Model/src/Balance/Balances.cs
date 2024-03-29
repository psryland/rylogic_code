﻿using System;
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
		// - Doubles vs. Decimals:
		//   Decimals should only be used for storing the floating-point numbers and simple arithmetic operations on them.
		//   All heavy mathematics like calculating indicators for technical analysis should be performed on Double values.
		//   So, using decimal for balances, double for charts etc.
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
			:this(coin, 0m._(coin), last_updated)
		{}
		public Balances(Coin coin, Unit<decimal> total, DateTimeOffset last_updated, bool notify = false)
			:this(coin, total, 0m._(coin), last_updated, notify)
		{}
		public Balances(Coin coin, Unit<decimal> total, Unit<decimal> held_on_exch, DateTimeOffset last_updated, bool notify = false)
		{
			// Truncation error can create off by 1 LSD
			if (total < 0)
			{
				Debug.Assert(total >= -decimal_.Epsilon);
				total = 0m._(total);
			}
			if (!held_on_exch.WithinInclusive(0, total))
			{
				Debug.Assert(held_on_exch >= -decimal_.Epsilon);
				Debug.Assert(held_on_exch <= total + decimal_.Epsilon);
				held_on_exch = Math_.Clamp(held_on_exch, 0m._(held_on_exch), total);
			}

			Coin = coin;
			LastUpdated = Misc.CryptoCurrencyEpoch;

			// Initialise the funds collection. Although there is a 'Balance' instance for
			// the main fund, its just a place holder. Its accessors use the 'ExchTotal'
			// and 'ExchHeld' properties in this class.
			m_funds = new List<Balance>{};
			m_funds.Add(new Balance(this, Fund.Main));

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
				if (fd.Id == Fund.Main.Id)
					continue;

				// Ensure the fund exists
				var fund = new Fund(fd.Id);
				var bal = m_funds.Add2(new Balance(this, fund));

				// Look for balance data for this fund/exchange.
				var bal_data = fd[Exchange.Name][Coin.Symbol];

				// Attribute the balance amount to fund 'fd.Id'
				AssignFundBalance(fund, bal_data.Total._(Coin), notify: notify);
			}
		}

		/// <summary>The currency that the balance is in</summary>
		public Coin Coin { get; }

		/// <summary>The exchange that this balance is on</summary>
		public Exchange Exchange => Coin.Exchange;

		/// <summary>When the balance was last updated from the exchange</summary>
		public DateTimeOffset LastUpdated { get; private set; }

		/// <summary>The collection of funds containing the partitioned balances</summary>
		public IEnumerable<IBalance> Funds => m_funds;

		/// <summary>All funds except 'Main'</summary>
		public IEnumerable<IBalance> FundsExceptMain => Funds.Skip(1);

		/// <summary>The account balance reported by the exchange</summary>
		public Unit<decimal> ExchTotal { get; private set; }
		public Unit<decimal> ExchHeld { get; private set; }

		/// <summary>The nett total balance. The balance that the exchange reports (regardless of what the funds think they have)</summary>
		public Unit<decimal> NettTotal => ExchTotal;

		/// <summary>The nett held balance is the held amount that the exchange reports plus the local hold amounts</summary>
		public Unit<decimal> NettHeld => ExchHeld + m_funds.Select(x => x.Holds).Sum(x => x.Local);

		/// <summary>The nett available balance is the (total - held) balance reported by the exchange</summary>
		public Unit<decimal> NettAvailable => NettTotal - NettHeld;

		/// <summary>The value of the nett total in valuation currency</summary>
		public Unit<decimal> NettValue => Coin.ValueOf(NettTotal);

		/// <summary>Access balances associated with the given fund. Unknown fund ids return an empty balance</summary>
		public IBalance this[Fund fund] => Funds.FirstOrDefault(x => x.Fund == fund) ?? new Balance(this, fund);

		/// <summary>Apply an update from an exchange for this currency</summary>
		public void ExchangeUpdate(Unit<decimal> total, Unit<decimal> held, DateTimeOffset update_time)
		{
			// Ignore out-of-date data (unless back testing where time can go backwards)
			if (LastUpdated > update_time && !Model.BackTesting)
				return;

			if (total < 0m._(Coin))
				throw new Exception($"Exchange total balance is negative: {total}");
			if (held > total)
				throw new Exception($"Exchange held balance ({held}) is greater than the total balance ({total})");

			var changed = false;
			if (ExchTotal != total)
			{
				ExchTotal = total;
				changed = true;
			}
			if (ExchHeld != held)
			{
				ExchHeld = held;
				changed = true;
			}

			LastUpdated = update_time;

			if (changed)
				Invalidate();
		}

		/// <summary>Set the total balance attributed to the fund 'fund_id'</summary>
		public void AssignFundBalance(Fund fund, Unit<decimal> total, bool notify = true)
		{
			// Notes:
			// - Don't throw if the nett total becomes negative, that probably just means a fund
			//   has been assigned too much. Allow the user to reduce the allocations.

			// Get the balance info for 'fund_id', create if necessary
			var balance = (Balance?)Funds.FirstOrDefault(x => x.Fund == fund) ?? m_funds.Add2(new Balance(this, fund));
			balance.Total = total;

			// The main fund changes with each update. Other funds don't.
			// Notify *after* 'LastUpdated' and 'CheckHolds' have been set
			if (notify)
				Invalidate();
		}

		/// <summary>Add or subtract an amount from a fund</summary>
		public void ChangeFundBalance(Fund fund, Unit<decimal> change_amount, bool notify = true)
		{
			// Get the balance info for 'fund_id', create if necessary
			var balance = (Balance?)Funds.FirstOrDefault(x => x.Fund == fund);//  ?? m_funds.Add2(new Balance(this, fund));
			if (balance == null)
				return;

			// Apply the balance change to the total
			balance.Total += change_amount;

			// Changing the total for a fund effects the balance of the main fund.
			if (notify)
				Invalidate();
		}

		/// <summary>Sanity check this balance. Returns null if valid</summary>
		public Exception? Validate()
		{
			// Check exchange side balance
			if (ExchTotal < 0m._(Coin))
				return new Exception("Exchange Balance Invalid: Total < 0");
			if (ExchHeld < 0m._(Coin))
				return new Exception("Exchange Balance Invalid: Held < 0");
			if (ExchHeld > ExchTotal)
				return new Exception("Exchange Balance Invalid: Held > Total");

			// Check the fund balances are logical
			foreach (var fund in Funds)
			{
				if (fund.Total < 0m._(Coin))
					return new Exception($"Fund {fund.Fund.Id} balance invalid: Total < 0");
				if (fund.Held < 0m._(Coin))
					return new Exception($"Fund {fund.Fund.Id} balance invalid: Held < 0");
			}

			return null;
		}

		/// <summary>Notify that the main fund balance has changed</summary>
		private void Invalidate()
		{
			m_funds[0].NotifyPropertyChanged(nameof(Balance.Total));
			m_funds[0].NotifyPropertyChanged(nameof(Balance.Available));
			m_funds[0].NotifyPropertyChanged(nameof(Balance.Held));
			CoinData.NotifyBalanceChanged(Coin);
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

		/// <summary>A default for unknown coins</summary>
		public static IBalance ZeroBalance(Coin coin) => new ZeroBalanceData(coin);

		/// <summary>Implements the balance associated with a single fund for a single coin on a single exchange</summary>
		[DebuggerDisplay("{Description,nq}")]
		private class Balance :IBalance
		{
			private readonly Balances m_balances;
			public Balance(Balances balances, Fund fund)
			{
				m_balances = balances;
				Holds = new FundHoldContainer(fund);
				m_total = 0m._(balances.Coin);
				Fund = fund;
			}

			/// <summary>The Fund that this balance belongs to</summary>
			public Fund Fund { get; }

			/// <summary>The currency that the balance is in</summary>
			public Coin Coin => m_balances.Coin;

			/// <summary>When the balance was last updated</summary>
			public DateTimeOffset LastUpdated => m_balances.LastUpdated;

			/// <summary>The value of 'Coin' (probably in USD)</summary>
			public decimal Value => Coin.ValueOf(1m);

			/// <summary>The total balance associated with this fund (includes held amounts)</summary>
			public Unit<decimal> Total
			{
				get
				{
					// The Main fund contains whatever is left over after the other funds have their share
					var total = Fund == Fund.Main
						? m_balances.ExchTotal - m_balances.FundsExceptMain.Sum(x => x.Total)
						: m_total;

					return (decimal)total > 0m ? total : 0m._(Coin);
				}
				set
				{
					if (Total == value) return;
					if (Fund == Fund.Main)
						m_balances.ExchTotal = value + m_balances.FundsExceptMain.Sum(x => x.Total);
					else
						m_total = value;

					NotifyPropertyChanged(nameof(Total));
					NotifyPropertyChanged(nameof(Available));
				}
			}
			private Unit<decimal> m_total;

			/// <summary>Total amount able to be used for new trades</summary>
			public Unit<decimal> Available
			{
				get
				{
					var avail = Total - Held;
					return (decimal)avail > 0m ? avail : 0m._(Coin);
				}
			}

			/// <summary>Total amount set aside for pending orders and trade strategies</summary>
			public Unit<decimal> Held
			{
				get
				{
					// 'Hold's exist for the life of an order. The main fund held amount is the difference between
					// what the exchange says is held, and the sum of amounts held for the other funds.
					// Holds are stored per-order because having a single value representing the sum is hard to
					// accurately maintain and produces rounding errors.
					if (Fund == Fund.Main)
					{
						var held_in_other_funds = m_balances.FundsExceptMain.Cast<Balance>().Sum(x => x.Holds.ExchHeld);
						var held = Holds.Local + (m_balances.ExchHeld - held_in_other_funds);
						return (decimal)held > 0 ? held : 0m._(Coin);
					}
					else
					{
						return Holds.Total;
					}
				}
			}

			/// <summary>Holds on this fund balance</summary>
			public FundHoldContainer Holds
			{
				get => m_holds;
				set
				{
					if (m_holds == value) return;
					if (m_holds != null)
					{
						m_holds.HoldsChanged -= HandleHoldsChanged;
					}
					m_holds = value;
					if (m_holds != null)
					{
						m_holds.HoldsChanged += HandleHoldsChanged;
					}

					// Handler
					void HandleHoldsChanged(object? sender, EventArgs e)
					{
						NotifyPropertyChanged(nameof(Available));
						NotifyPropertyChanged(nameof(Held));
						CoinData.NotifyBalanceChanged(Coin);
					}
				}
			}
			private FundHoldContainer m_holds = null!;

			/// <summary>Notify property changed for 'Total', 'Available', and 'Held'</summary>
			public void Invalidate()
			{
				NotifyPropertyChanged(nameof(Total));
				NotifyPropertyChanged(nameof(Available));
				NotifyPropertyChanged(nameof(Held));
			}

			/// <inheritdoc/>
			public event PropertyChangedEventHandler? PropertyChanged;
			public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

			/// <summary>String description of this fund balance</summary>
			public string Description => $"{Coin} Fund={Fund.Id} Avail={Available} Total={Total}";
		}

		/// <summary>Zero balance </summary>
		private class ZeroBalanceData : IBalance
		{
			public ZeroBalanceData(Coin coin) => Coin = coin;
			public Coin Coin { get; }
			public Fund Fund => Fund.Unknown;
			public DateTimeOffset LastUpdated => DateTimeOffset.MinValue;
			public Unit<decimal> Total => 0m._(Coin);
			public Unit<decimal> Available => 0m._(Coin);
			public Unit<decimal> Held => 0m._(Coin);
			public FundHoldContainer Holds { get; } = new FundHoldContainer(Fund.Unknown);
			public void Invalidate() { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); }
			public event PropertyChangedEventHandler? PropertyChanged;
		}
	}
}
