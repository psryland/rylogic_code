using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	public class Equity :Buffers
	{
		// TODO:
		// - need the union of all completed orders on all exchanges, or on the exchanges selected
		// - When a completed order is added, our cached list of the important details needs re-creating.
		// - The graphics actually need updating with each price tick, because the value of each
		//   coin changes relative to the other coins. Could have a required value change amount before
		//   updating the graphics... for performance
		// - Use the current balances, and all completed orders to determine the initial balance
		// 

		/// <summary>The history of the changes in amounts of each currency over time</summary>
		private readonly List<BalanceChange> m_data;

		/// <summary>The value of each currency at the last evaluation</summary>
		private readonly List<CoinBalanceInfo> m_coin_info;

		/// <summary>A data series for the overall nett worth vs. time</summary>
		private readonly ChartDataSeries m_nett_worth;

		public Equity()
		{
			m_data = new List<BalanceChange>();
			m_coin_info = new List<CoinBalanceInfo>();
			m_nett_worth = new ChartDataSeries("Nett Worth", ChartDataSeries.EFormat.XRealYReal, new ChartDataSeries.OptionsData
			{
				PlotType = ChartDataSeries.EPlotType.Bar,
				Colour = SettingsData.Settings.Equity.NettWorthColour.Alpha(0.5f),
				BarWidth = 1f,
			});
			Order = EOrderBy.DecendingTotal;
		}
		public override void Dispose()
		{
			Gfx = null;
			m_nett_worth.Chart = null;
			foreach (var ci in m_coin_info)
				ci.Plot.Chart = null;
			base.Dispose();
		}

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

		/// <summary></summary>
		private View3d.Object Gfx
		{
			get => m_gfx;
			set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx;

		/// <summary>Update the equity state using the data from the model</summary>
		public void Update(Model model)
		{
			// No price data yet, ignore
			if (model.NettWorth == 0)
				return;

			// See if the graphics need updating
			var update_gfx = false;
			update_gfx |= CompileHistory(model);
			update_gfx |= CompilePrices(model);

			// Invalidate the graphics objects if out of date
			if (update_gfx)
			{
				Gfx = null;

				// Update the data series for overall nett worth
				UpdateNettWorthPlot(model.NettWorth);

				// Update the data series for each coin
				foreach (var ci in m_coin_info)
					UpdateCoinPlot(ci, ci.Coin.NettTotal(model.Exchanges));
			}
		}

		/// <summary>Update the data series for the overall nett worth</summary>
		private void UpdateNettWorthPlot(Unit<double> nett_worth)
		{
			using (var plot = m_nett_worth.Lock())
			{
				// Resize the series data
				plot.Count = m_data.Count + 1;

				// Create a quick lookup table of the value of each coin
				var rate_of = m_coin_info.ToDictionary(c => c.Coin.Symbol, c => c.Value / 1.0._(c.Coin));

				// Add the current amount as the last entry
				var i = m_data.Count;
				var x = (Model.UtcNow - CoinFlip.Misc.CryptoCurrencyEpoch).TotalDays;
				plot[i--] = new ChartDataSeries.Pt(x, nett_worth);

				// Work backwards through the balance changes
				var amount = nett_worth;
				foreach (var chg in m_data.Reversed())
				{
					// Apply the inverse change in value
					if (chg.SymbolIn != null)
						amount += chg.AmountIn * rate_of[chg.SymbolIn];
					if (chg.SymbolOut != null)
						amount -= chg.AmountNett * rate_of[chg.SymbolOut];

					// Add the value
					x = (chg.Time - CoinFlip.Misc.CryptoCurrencyEpoch).TotalDays;
					plot[i--] = new ChartDataSeries.Pt(x, amount);
				}
			}
		}

		/// <summary>Update the data series for the given coin</summary>
		private void UpdateCoinPlot(CoinBalanceInfo ci, Unit<double> current_amount)
		{
			using (var plot = ci.Plot.Lock())
			{
				// Resize the series data
				plot.Count = m_data.Count + 1;

				// Get the conversion to valuation currency for this coin
				var rate = ci.Value / 1.0._(ci.Coin);

				// Add the current amount as the last entry
				var i = m_data.Count;
				var x = (Model.UtcNow - CoinFlip.Misc.CryptoCurrencyEpoch).TotalDays;
				plot[i--] = new ChartDataSeries.Pt(x, current_amount * rate);

				// Work backwards through the balance changes
				var amount = current_amount;
				foreach (var chg in m_data.Reversed())
				{
					// Apply the inverse change in value
					if (chg.SymbolIn == ci.Coin.Symbol)
						amount += chg.AmountIn;
					if (chg.SymbolOut == ci.Coin.Symbol)
						amount -= chg.AmountNett;

					// Add the value
					x = (chg.Time - CoinFlip.Misc.CryptoCurrencyEpoch).TotalDays;
					plot[i--] = new ChartDataSeries.Pt(x, amount * rate);
				}
			}
		}

		/// <summary>Add the graphics objects to the scene</summary>
		public void BuildScene(ChartControl chart, View3d.Window window, Canvas overlay)
		{

			//TODO: Make the region line "nett" a new chart dataq series plot type
			// Ensure the plots are added to the scene
			m_nett_worth.Chart = chart;
			foreach (var ci in m_coin_info)
				ci.Plot.Chart = chart;
		}

		/// <summary>Create the graphics for the equity</summary>
		private View3d.Object CreateGfx()
		{
			//TODO:
			// - get deposits/withdrawals working, these data are needed for this to be accurate

			// The number of currencies
			var num = m_coin_info.Count;
			if (num == 0)
				return null;

			// The number of balance changes + 1 for the current time
			var data_count = m_data.Count + 1;
			if (data_count < 2)
				return null;

			// Quick lookup of currency value
			var rate_of = m_coin_info.ToDictionary(x => x.Coin.Symbol, x => x.Value / 1.0._(x.Coin));

			// Determine the initial value from the current nett value by working backwards
			var value = 0.0;
			foreach (var chg in m_data.Reversed())
			{
				// Apply the inverse change in value
				if (chg.SymbolIn != null)
					value += chg.AmountIn * rate_of[chg.SymbolIn];
				if (chg.SymbolOut != null)
					value -= chg.AmountNett * rate_of[chg.SymbolOut];
			}

			// Resize the cache buffers
			// Use a TriStrip for the filled region and LineStrips for the per-coin values.
			// vbuf = [nett outline, nett quads, coin0 value, coin1 value, coin2 value, ...]
			// ibuf = [nett outline, nett quads, coin0 value, coin1 value, coin2 value, ...]
			m_vbuf.Resize(data_count + data_count * 2 + num * data_count);
			m_ibuf.Resize(data_count + data_count * 2 + num * data_count);
			m_nbuf.Resize(1 + 1 + num);

			// Create the nett value filled region and its outline
			var col0 = SettingsData.Settings.Equity.NettWorthColour.Alpha(1.0f).Darken(0.25f);
			var col1 = SettingsData.Settings.Equity.NettWorthColour.Alpha(0.5f);
			int vidx = 0, iidx = 0, vidx2 = data_count, iidx2 = data_count;
			for (int i = 0; i != data_count; ++i)
			{
				// Get the balance change at 'i'
				var chg = i < m_data.Count ? m_data[i] : new BalanceChange(Model.UtcNow);

				// Apply the change in value
				if (chg.SymbolIn != null)
					value -= chg.AmountIn * rate_of[chg.SymbolIn];
				if (chg.SymbolOut != null)
					value += chg.AmountNett * rate_of[chg.SymbolOut];

				var x = (float)(chg.Time - CoinFlip.Misc.CryptoCurrencyEpoch).TotalDays;
				var y = (float)value;
				var y0 = value >= 0 ? 0f : y;
				var y1 = value <  0 ? 0f : y;
				var v = vidx;
				var v2 = vidx2;

				m_vbuf[vidx++] = new View3d.Vertex(new v4(x, y, 0, 1), col0);
				m_vbuf[vidx2++] = new View3d.Vertex(new v4(x, y1, 0, 1), col1);
				m_vbuf[vidx2++] = new View3d.Vertex(new v4(x, y0, 0, 1), col1);

				m_ibuf[iidx++] = (ushort)v;
				m_ibuf[iidx2++] = (ushort)(v2 + 0);
				m_ibuf[iidx2++] = (ushort)(v2 + 1);
			}
			vidx = vidx2;
			iidx = iidx2;

			// Create the value lines per coin
			for (int j = 0; j != num; ++j)
			{
				// Get the coin/balance info for coin 'j'
				var coin_info = m_coin_info[j];
				
				value = 0.0;
				var col = coin_info.Colour.Darken(0.25f);
				for (int i = 0; i != data_count; ++i)
				{
					// Get the balance change at 'i'
					var chg = i < m_data.Count ? m_data[i] : new BalanceChange(Model.UtcNow);

					// Apply the change in value
					if (chg.SymbolIn == coin_info.Coin.Symbol)
						value -= chg.AmountIn * rate_of[chg.SymbolIn];
					if (chg.SymbolOut == coin_info.Coin.Symbol)
						value += chg.AmountNett * rate_of[chg.SymbolOut];

					var x = (float)(chg.Time - CoinFlip.Misc.CryptoCurrencyEpoch).TotalDays;
					var y = (float)value;

					var v = vidx;
					m_vbuf[vidx++] = new View3d.Vertex(new v4(x, y, 0, 1), col);
					m_ibuf[iidx++] = (ushort)v;
				}
			}

			// Nuggets
			var line_material = View3d.Material.New();
			line_material.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.ThickLineListGS, "*LineWidth {3}");

			// Nett value outline
			uint v0 = 0, v1 = (uint)data_count;
			uint i0 = 0, i1 = (uint)data_count;
			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, v0, v1, i0, i1, mat: line_material);

			// Nett value filled region
			v0 = v1; v1 += (uint)data_count * 2;
			i0 = i1; i1 += (uint)data_count * 2;
			var has_alpha = SettingsData.Settings.Equity.NettWorthColour.A != 0xFF;
			m_nbuf[1] = new View3d.Nugget(View3d.EPrim.TriStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, v0, v1, i0, i1, has_alpha: has_alpha, range_overlaps:true);

			// Per-coin values
			for (int i = 0; i != num; ++i)
			{
				// Line strip
				v0 = v1; v1 += (uint)data_count;
				i0 = i1; i1 += (uint)data_count;
				m_nbuf[2 + i] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, v0, v1, i0, i1, mat:line_material);
			}

			// Create the graphics
			var gfx = new View3d.Object("Equity", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), EquityChart.CtxId);
			return gfx;
		}

		/// <summary>Collect the trade history from all exchanges</summary>
		private bool CompileHistory(Model model)
		{
			var data_added = false;

			// Look for any new completed orders
			var since = m_data.Count != 0 ? m_data.Back().Time : CoinFlip.Misc.CryptoCurrencyEpoch;

			// Collect the trade history from each exchange
			foreach (var exch in model.Exchanges)
			{
				foreach (var order in exch.History.Values)
				{
					if (order.Created <= since) continue;
					m_data.Add(new BalanceChange(order));
					data_added = true;
				}
				foreach (var txn in exch.Transfers.Values)
				{
					if (txn.Created <= since) continue;
					m_data.Add(new BalanceChange(txn));
					data_added = true;
				}
			}

			// Order the trade history by time
			if (data_added)
				m_data.Sort(x => x.Time);

			return data_added;
		}

		/// <summary>Compare the current currency prices to those last used to generate the graphics. Returns true if they're significantly different</summary>
		private bool CompilePrices(Model model)
		{
			var values_changed = false;
			foreach (var coin in model.Coins)
			{
				var new_value = coin.AverageValue(model.Exchanges);
				var new_total = coin.NettTotal(model.Exchanges);

				// If our data is up to date then don't signal prices changed
				var old = m_coin_info.FirstOrDefault(x => x.Coin == coin);
				if (old != null && old.Total == new_total && Math_.FEqlRelative(old.Value, new_value, 0.01))
					continue;

				// Add or update the new value for 'coin'
				old = old ?? m_coin_info.Add2(new CoinBalanceInfo(coin));
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
			Func<CoinBalanceInfo, double> pred;
			switch (order_by) {
			case EOrderBy.AscendingValue: pred = x => +x.Value; break;
			case EOrderBy.DecendingValue: pred = x => -x.Value; break;
			case EOrderBy.AscendingTotal: pred = x => +x.Total; break;
			case EOrderBy.DecendingTotal: pred = x => -x.Total; break;
			default: throw new Exception($"Unknown order by value: {order_by}");
			}

			// Assign the order values
			int i = 0;
			foreach (var x in m_coin_info.OrderBy(pred))
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
		private class BalanceChange
		{
			// Notes:
			// - This type can also represent deposits and withdrawals.
			//   A deposit has SymbolIn/AmountIn = null/0.
			//   A withdrawal has SymbolOut/AmountOut = null/0.
			public BalanceChange(DateTimeOffset time, string symbol_in = null, string symbol_out = null, double? amount_in = null, double? amount_out = null, double? amount_nett = null)
			{
				Time = time;
				SymbolIn = symbol_in;
				SymbolOut = symbol_out;
				AmountIn = amount_in ?? 0;
				AmountOut = amount_out ?? 0;
				AmountNett = amount_nett ?? amount_out ?? 0;
			}
			public BalanceChange(OrderCompleted order)
			{
				Time = order.Created;
				SymbolIn = order.CoinIn;
				SymbolOut = order.CoinOut;
				AmountIn = order.AmountIn;
				AmountOut = order.AmountOut;
				AmountNett = order.AmountNett;
			}
			public BalanceChange(Transfer transfer)
			{
				Time = transfer.Created;
				SymbolIn = transfer.Type == ETransfer.Withdrawal ? transfer.Coin : null;
				SymbolOut = transfer.Type == ETransfer.Deposit ? transfer.Coin : null;
				AmountIn = transfer.Type == ETransfer.Withdrawal ? transfer.Amount : 0.0._(transfer.Coin);
				AmountOut = transfer.Type == ETransfer.Deposit ? transfer.Amount : 0.0._(transfer.Coin);
				AmountNett = AmountOut;
			}

			// The timestamp of this amount snapshot
			public DateTimeOffset Time { get; }

			/// <summary>The currency sold</summary>
			public string SymbolIn { get; }

			/// <summary>The currency bought</summary>
			public string SymbolOut { get; }

			/// <summary>The amount sold</summary>
			public Unit<double> AmountIn { get; }

			/// <summary>The amount bought</summary>
			public Unit<double> AmountOut { get; }

			/// <summary>The amount bought after fees</summary>
			public Unit<double> AmountNett { get; }
		}
		private class CoinBalanceInfo
		{
			public CoinBalanceInfo(CoinData coin)
				:this(coin, 0.0._(SettingsData.Settings.ValuationCurrency), 0.0._(coin), 0)
			{ }
			public CoinBalanceInfo(CoinData coin, Unit<double> value, Unit<double> total, int order)
			{
				if (value < 0.0._(SettingsData.Settings.ValuationCurrency))
					throw new Exception("Invalid coin value");
				if (total < 0.0._(coin))
					throw new Exception("Invalid total value");

				Coin = coin;
				Value = value;
				Total = total;
				Order = order;
				Colour = Colours[m_colour_index++ % Colours.Length];
				Plot = new ChartDataSeries(Coin.Symbol, ChartDataSeries.EFormat.XRealYReal, new ChartDataSeries.OptionsData
				{
					PlotType = ChartDataSeries.EPlotType.StepLine,
					Colour = Colour.Alpha(1.0f).Darken(0.25f),
					LineWidth = 3,
					PointSize = 2,
				});
			}
			//public CoinBalanceInfo(CoinBalanceInfo rhs)
			//	:this(rhs.Coin, rhs.Value, rhs.Total, rhs.Order)
			//{}

			/// <summary>The coin symbol name</summary>
			public CoinData Coin { get; }

			/// <summary>The last known value of this coin in common value units</summary>
			public Unit<double> Value { get; set; }

			/// <summary>The current balance of this coin</summary>
			public Unit<double> Total { get; set; }

			/// <summary>The colour to plot this coin info with</summary>
			public Colour32 Colour { get; set; }

			/// <summary>Sorting order</summary>
			public int Order { get; set; }

			/// <summary>A chart plot of the value of this coin</summary>
			public ChartDataSeries Plot { get; }
		}

		/// <summary>Colours for coins</summary>
		private static uint[] Colours = new[] { 0xffffc3c6, 0xffc3ffc6, 0xffc3c6ff, 0xffc3ffff, 0xffffc3ff, 0xffffffc3, 0xff75ff72, 0xffe0a8ff, 0xff8682ff, 0xffffa699, 0xff97ff89 };
		private static int m_colour_index;
	}
}
