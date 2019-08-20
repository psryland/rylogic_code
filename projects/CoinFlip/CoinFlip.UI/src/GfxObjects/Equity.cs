using System;
using System.Collections.Generic;
using CoinFlip.Settings;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
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

		public Equity(CoinFlip.Equity equity)
		{
			CoinPlots = new List<CoinPlot>();
			NettWorthPlot = new ChartDataSeries("Nett Worth", ChartDataSeries.EFormat.XRealYReal, new ChartDataSeries.OptionsData
			{
				PlotType = ChartDataSeries.EPlotType.Bar,
				Colour = SettingsData.Settings.Chart.NettWorthColour.Alpha(0.7f),
				BarWidth = 1,
				BarHorizontalAlignment = 1,
			});
			Data = equity;
		}
		public override void Dispose()
		{
			Util.DisposeAll(CoinPlots);
			NettWorthPlot = null;
			Data = null;
			base.Dispose();
		}

		/// <summary>The model data for thi</summary>
		private CoinFlip.Equity Data
		{
			get => m_data;
			set
			{
				if (m_data == value) return;
				if (m_data != null)
				{
					m_data.EquityChanged -= HandleEquityChanged;
					Util.Dispose(ref m_data);
				}
				m_data = value;
				if (m_data != null)
				{
					m_data.EquityChanged += HandleEquityChanged;
				}

				// Handle
				void HandleEquityChanged(object sender, EventArgs e)
				{
					// Update the data series for overall nett worth
					UpdateNettWorthPlot();

					// Update the data series for each coin plot
					foreach (var cp in CoinPlots)
						cp.UpdateCoinPlot(Data);
				}
			}
		}
		private CoinFlip.Equity m_data;

		/// <summary>A data series for the overall nett worth vs. time</summary>
		private ChartDataSeries NettWorthPlot
		{
			get => m_plot_nett_worth;
			set
			{
				if (m_plot_nett_worth == value) return;
				Util.Dispose(ref m_plot_nett_worth);
				m_plot_nett_worth = value;
			}
		}
		private ChartDataSeries m_plot_nett_worth;

		/// <summary>Data series for 'worth' plots of each coin</summary>
		private List<CoinPlot> CoinPlots { get; }

		/// <summary>Update the data series for the overall nett worth</summary>
		private void UpdateNettWorthPlot()
		{
			using (var plot = NettWorthPlot.Lock())
			{
				// Resize the series data
				plot.Count = Data.BalanceChanges.Count + 1;

				var i = plot.Count - 1;
				foreach (var chg in Data.NettWorthHistory())
				{
					var x = (chg.Time - CoinFlip.Misc.CryptoCurrencyEpoch).TotalDays;
					plot[i--] = new ChartDataSeries.Pt(x, chg.Worth);
				}
			}
		}

		/// <summary>Add the graphics objects to the scene</summary>
		public void BuildScene(ChartControl chart)
		{
			//TODO: Make the region line "nett" a new chart dataq series plot type
			// Ensure the plots are added to the scene
			NettWorthPlot.Chart = chart;
			//foreach (var ci in m_coin_info)
			//	ci.Plot.Chart = chart;
		}

		/// <summary></summary>
		private class CoinPlot :ChartDataSeries
		{
			public CoinPlot(CoinFlip.Equity.CoinBalanceInfo cbi)
				: base(cbi.Coin.Symbol, EFormat.XRealYReal, new OptionsData
				{
					PlotType = EPlotType.StepLine,
					Colour = Colour32.Black,
					LineWidth = 3,
					PointSize = 2,
				})
			{
				CoinInfo = cbi;
				Colour = Colours[m_colour_index++ % Colours.Length];
			}

			/// <summary>Coin balance info for this plot</summary>
			public CoinFlip.Equity.CoinBalanceInfo CoinInfo { get; }

			/// <summary>Plot colour</summary>
			public Colour32 Colour
			{
				get => m_colour;
				set
				{
					if (m_colour == value) return;
					m_colour = value;
					Options.Colour = m_colour.Alpha(1.0f).Darken(0.25f);
				}
			}
			private Colour32 m_colour;

			/// <summary>Update the data series for the given coin</summary>
			public void UpdateCoinPlot(CoinFlip.Equity data)
			{
				using (var plot = this.Lock())
				{
					// Resize the series data
					plot.Count = data.BalanceChanges.Count + 1;

					var i = plot.Count - 1;
					foreach (var chg in data.NettWorthHistory(CoinInfo.Coin))
					{
						var x = (chg.Time - CoinFlip.Misc.CryptoCurrencyEpoch).TotalDays;
						plot[i--] = new Pt(x, chg.Worth);
					}
				}
			}
		}

		/// <summary>Colours for coins</summary>
		private static uint[] Colours = new[] { 0xffffc3c6, 0xffc3ffc6, 0xffc3c6ff, 0xffc3ffff, 0xffffc3ff, 0xffffffc3, 0xff75ff72, 0xffe0a8ff, 0xff8682ff, 0xffffa699, 0xff97ff89 };
		private static int m_colour_index;
	}
}

#if false

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
#endif