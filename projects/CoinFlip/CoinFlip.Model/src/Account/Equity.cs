using System;
using System.Collections.Generic;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	public class Equity :IDisposable
	{
		public Equity(Model model)
		{
			BalanceChanges = new List<BalanceChange>();
			CoinInfo = new List<CoinBalanceInfo>();
			Order = EOrderBy.DecendingTotal;
			Model = model;

			Update();
		}
		public virtual void Dispose()
		{
			Model = null;
		}

		/// <summary>The app logic</summary>
		private Model Model
		{
			get => m_model;
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.NettWorthChanged -= HandleNettWorthChanged;
					Model.BackTestingChange -= HandleBackTestingChanged;
					SettingsData.Settings.SettingChange -= HandleSettingChange;
				}
				m_model = value;
				if (m_model != null)
				{
					SettingsData.Settings.SettingChange += HandleSettingChange;
					Model.BackTestingChange += HandleBackTestingChanged;
					m_model.NettWorthChanged += HandleNettWorthChanged;
				}

				// Handler
				void HandleNettWorthChanged(object sender, ValueChangedEventArgs<Unit<decimal>> e)
				{
					Update();
				}
				void HandleBackTestingChanged(object sender, PrePostEventArgs e)
				{
					Invalidate();
				}
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
					case nameof(EquitySettings.Since):
						Invalidate();
						Update();
						break;
					case nameof(EquitySettings.IncludeTransfers):
						Invalidate();
						Update();
						break;
					}
				}
			}
		}
		private Model m_model;

		/// <summary>The order to display currencies in</summary>
		public EOrderBy Order
		{
			get => m_order;
			set
			{
				if (m_order == value) return;
				m_order = value;
				OrderCoins(m_order);
			}
		}
		private EOrderBy m_order;

		/// <summary>Return the time range (in ticks) spanned by the given index range</summary>
		public RangeI TimeRange(RangeF index_range)
		{
			if (Count == 0)
				return new RangeI(Misc.CryptoCurrencyEpoch.Ticks, Misc.CryptoCurrencyEpoch.Ticks);

			var now = Model.UtcNow.Ticks;
			var idx0 = Math_.Clamp((int)(index_range.Beg + 0), 0, Count);
			var idx1 = Math_.Clamp((int)(index_range.End + 1), 0, Count);
			var range = new RangeI(
					idx0 != Count ? BalanceChanges[idx0].Time.Ticks : now,
					idx1 != Count ? BalanceChanges[idx1].Time.Ticks : now);

			return range;
		}

		/// <summary>The number of changes in equity</summary>
		public int Count => BalanceChanges.Count;

		/// <summary>The history of the changes in amounts of each currency over time</summary>
		public List<BalanceChange> BalanceChanges { get; }

		/// <summary>The value of each currency at the last evaluation</summary>
		public List<CoinBalanceInfo> CoinInfo { get; }

		/// <summary>Return the global nett worth (coin == null) or nett worth of the given coin</summary>
		public Unit<decimal> NettWorth(CoinData coin = null)
		{
			return coin != null ? coin.NettTotal(Model.Exchanges) : Model.NettWorth;
		}

		/// <summary>Returns the Nett worth at the points in time where it changes (in reverse time order)</summary>
		public IEnumerable<TimeWorthPair> NettWorthHistory(CoinData coin = null)
		{
			var nett_worth = NettWorth();

			// Create a quick lookup table of the value of each coin
			var rate_of = CoinInfo.ToDictionary(c => c.Coin.Symbol, c => c.Value / 1m._(c.Coin));

			// Yield the current worth as the first data point
			yield return new TimeWorthPair(Model.UtcNow, nett_worth);

			// Work backwards through the balance changes
			var amount = nett_worth;
			foreach (var chg in BalanceChanges.Reversed())
			{
				// Apply the inverse change in value
				if (coin == null)
				{
					if (chg.SymbolIn != null)
						amount += chg.AmountIn * rate_of[chg.SymbolIn];
					if (chg.SymbolOut != null)
						amount -= chg.AmountOut * rate_of[chg.SymbolOut];
					if (chg.SymbolComm != null)
						amount += chg.AmountComm * rate_of[chg.SymbolComm];
				}
				else
				{
					if (chg.SymbolIn == coin.Symbol)
						amount += chg.AmountIn * rate_of[chg.SymbolIn];
					if (chg.SymbolOut == coin.Symbol)
						amount -= chg.AmountOut * rate_of[chg.SymbolOut];
					if (chg.SymbolComm == coin.Symbol)
						amount += chg.AmountComm * rate_of[chg.SymbolComm];
				}

				// Yield the balance change value
				yield return new TimeWorthPair(chg.Time, amount);
			}
		}

		/// <summary>Invalidate the equity data</summary>
		public void Invalidate()
		{
			if (m_invalidated) return;
			BalanceChanges.Clear();
			m_invalidated = true;
		}
		private bool m_invalidated;

		/// <summary>Raised when the equity data has changed</summary>
		public event EventHandler EquityChanged;

		/// <summary>Update the equity state using the data from the model</summary>
		private void Update()
		{
			// No point in updating until we have a nett worth
			if (Model.NettWorth == 0)
				return;

			// Update data and notify if changed
			m_invalidated |= CompileHistory();
			m_invalidated |= CompilePrices();
			if (m_invalidated)
				EquityChanged?.Invoke(this, EventArgs.Empty);

			m_invalidated = false;
		}

		/// <summary>Collect the trade history from all exchanges</summary>
		private bool CompileHistory()
		{
			var data_added = false;
			var include_transfers = SettingsData.Settings.Equity.IncludeTransfers;

			// Look for any new completed orders.
			var since = BalanceChanges.Count != 0 ? BalanceChanges.Back().Time : SettingsData.Settings.Equity.Since;

			// Collect the trade history from each exchange
			foreach (var exch in Model.Exchanges)
			{
				foreach (var order in exch.History)
				{
					if (order.Created <= since) continue;
					BalanceChanges.Add(new BalanceChange(order));
					data_added = true;
				}
				if (include_transfers)
				{
					foreach (var txn in exch.Transfers)
					{
						if (txn.Created <= since) continue;
						BalanceChanges.Add(new BalanceChange(txn));
						data_added = true;
					}
				}
			}

			// Order the trade history by time
			if (data_added)
				BalanceChanges.Sort(x => x.Time);

			return data_added;
		}

		/// <summary>Compare the current currency prices to those last used to generate the graphics. Returns true if they're significantly different</summary>
		private bool CompilePrices()
		{
			var values_changed = false;
			foreach (var coin in Model.Coins)
			{
				var new_value = coin.AverageValue(Model.Exchanges);
				var new_total = coin.NettTotal(Model.Exchanges);

				// If our data is up to date then don't signal prices changed
				var old = CoinInfo.FirstOrDefault(x => x.Coin == coin);
				if (old != null && old.Total == new_total && Misc.EqlPrice(old.Value, new_value))
					continue;

				// Add or update the new value for 'coin'
				old = old ?? CoinInfo.Add2(new CoinBalanceInfo(coin));
				old.Value = new_value;
				old.Total = new_total;
				values_changed = true;
			}

			// Update the order values
			if (values_changed)
				OrderCoins(Order);

			return values_changed;
		}

		/// <summary>Set the order to display the coins in</summary>
		private void OrderCoins(EOrderBy order_by)
		{
			Func<CoinBalanceInfo, decimal> pred;
			switch (order_by) {
			case EOrderBy.AscendingValue: pred = x => +x.Value; break;
			case EOrderBy.DecendingValue: pred = x => -x.Value; break;
			case EOrderBy.AscendingTotal: pred = x => +x.Total; break;
			case EOrderBy.DecendingTotal: pred = x => -x.Total; break;
			default: throw new Exception($"Unknown order by value: {order_by}");
			}

			// Assign the order values
			int i = 0;
			foreach (var x in CoinInfo.OrderBy(pred))
				x.Order = i++; 
		}

		/// <summary>Options for ordering the displayed coins</summary>
		public enum EOrderBy
		{
			AscendingValue,
			DecendingValue,
			AscendingTotal,
			DecendingTotal,
		}

		/// <summary></summary>
		public class BalanceChange
		{
			// Notes:
			// - This type can also represent deposits and withdrawals.
			//   A deposit has SymbolIn/AmountIn = null/0.
			//   A withdrawal has SymbolOut/AmountOut = null/0.
			public BalanceChange(DateTimeOffset time, string symbol_in = null, string symbol_out = null, string symbol_commission = null, decimal? amount_in = null, decimal? amount_out = null, decimal? amount_commission = null)
			{
				Time = time;
				SymbolIn = symbol_in;
				SymbolOut = symbol_out;
				SymbolComm = symbol_commission;
				AmountIn = amount_in ?? 0m;
				AmountOut = amount_out ?? 0m;
				AmountComm = amount_commission ?? 0m;
			}
			public BalanceChange(OrderCompleted order)
			{
				Time = order.Created;
				SymbolIn = order.CoinIn;
				SymbolOut = order.CoinOut;
				SymbolComm = order.CommissionCoin.Symbol;
				AmountIn = order.AmountIn;
				AmountOut = order.AmountOut;
				AmountComm = order.Commission;
			}
			public BalanceChange(Transfer transfer)
			{
				Time = transfer.Created;
				SymbolIn = transfer.Type == ETransfer.Withdrawal ? transfer.Coin : null;
				SymbolOut = transfer.Type == ETransfer.Deposit ? transfer.Coin : null;
				SymbolComm = null;
				AmountIn = transfer.Type == ETransfer.Withdrawal ? transfer.Amount : 0m._(transfer.Coin);
				AmountOut = transfer.Type == ETransfer.Deposit ? transfer.Amount : 0m._(transfer.Coin);
				AmountComm = 0;
			}

			// The timestamp of this amount snapshot
			public DateTimeOffset Time { get; }

			/// <summary>The currency sold</summary>
			public string SymbolIn { get; }

			/// <summary>The currency bought</summary>
			public string SymbolOut { get; }

			/// <summary>The currency that commission was charged in</summary>
			public string SymbolComm { get; }

			/// <summary>The amount sold</summary>
			public Unit<decimal> AmountIn { get; }

			/// <summary>The amount bought</summary>
			public Unit<decimal> AmountOut { get; }

			/// <summary>The amount charged in commission</summary>
			public Unit<decimal> AmountComm { get; }
		}
		public class CoinBalanceInfo
		{
			public CoinBalanceInfo(CoinData coin)
				:this(coin, 0m._(SettingsData.Settings.ValuationCurrency), 0m._(coin), 0)
			{ }
			public CoinBalanceInfo(CoinData coin, Unit<decimal> value, Unit<decimal> total, int order)
			{
				if (value < 0m._(SettingsData.Settings.ValuationCurrency))
					throw new Exception("Invalid coin value");
				if (total < 0m._(coin))
					throw new Exception("Invalid total value");

				Coin = coin;
				Value = value;
				Total = total;
				Order = order;
			}

			/// <summary>The coin symbol name</summary>
			public CoinData Coin { get; }

			/// <summary>The last known value of this coin in common value units</summary>
			public Unit<decimal> Value { get; set; }

			/// <summary>The current balance of this coin</summary>
			public Unit<decimal> Total { get; set; }

			/// <summary>Sorting order</summary>
			public int Order { get; set; }
		}
		public struct TimeWorthPair
		{
			public TimeWorthPair(DateTimeOffset time, Unit<decimal> worth)
			{
				Time = time;
				Worth = worth;
			}
			public DateTimeOffset Time { get; }
			public Unit<decimal> Worth { get; }
		}
	}
}
