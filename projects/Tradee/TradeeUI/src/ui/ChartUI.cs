using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using pr.attrib;
using pr.common;
using pr.container;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>The chart for an instrument</summary>
	public class ChartUI :BaseUI
	{
		// Notes:
		// - XAxis x = 0 corresponds to the Latest candle in the instrument, prior candles are rendered
		//   at index positions back from the end. i.e. x = 0 = latest, x = -1 = latest-1, x = -2 = latest-2

		/// <summary>A cache of the candle graphics</summary>
		private const int GfxModelBatchSize = 1024;
		private Cache<int, CachedGfx> m_gfx_cache;

		/// <summary>Buffers for creating the chart graphics</summary>
		private List<View3d.Vertex> m_vbuf;
		private List<ushort>        m_ibuf;
		private List<View3d.Nugget> m_nbuf;

		#region UI Elements
		private pr.gui.ToolStripContainer m_tsc;
		private ToolStrip                 m_ts_timeframes;
		private ToolStripSplitButton      m_splitbtn_timeframes;
		private ToolStrip                 m_ts_trades;
		private ToolStripButton           m_btn_trade_long;
		private ToolStripButton           m_btn_trade_short;
		private ToolStripSeparator        m_sep0;
		private ToolStripButton           m_chk_show_historic;
		private pr.gui.ChartControl       m_chart;
		#endregion

		public ChartUI(MainModel model, Instrument instr)
			:base(model, ToChartName(instr.SymbolCode))
		{
			InitializeComponent();

			m_vbuf = new List<View3d.Vertex>();
			m_ibuf = new List<ushort>();
			m_nbuf = new List<View3d.Nugget>();
			m_gfx_cache = new Cache<int, CachedGfx>();
			m_chart = new ChartControl(string.Empty, Settings.Chart.Style);
			Indicators = new BindingSource<IndicatorBase> { DataSource = new BindingListEx<IndicatorBase>(), PerItemClear = true };

			// Set the instrument displayed in this chart
			Instrument = instr;

			// Find the stored settings for charts of this type
			ChartSettings = Model.FindChartSettings(Instrument.SymbolCode);

			SetupUI();
			UpdateUI();

			// Select the time frame from the chart settings
			TimeFrame = ChartSettings.TimeFrame;

			// Create a current price indicator
			CurrentPrice = new CurrentPrice(Model, Instrument);

			// Create graphics for the related trades
			SignalUpdateTradeGraphics();

			// Initialise the chart to the default range
			ResetToDefaultRange();
		}
		protected override void Dispose(bool disposing)
		{
			ChartSettings = null;
			Indicators.Clear();
			CurrentPrice = null;
			Instrument = null;
			Util.Dispose(ref m_gfx_cache);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void SetModelCore(MainModel model)
		{
			if (Model != null)
			{
				Model.ConnectionChanged            -= HandleConnectionChanged;
				Model.MarketData.InstrumentRemoved -= HandleInstrumentRemoved;
				Model.Positions.Changed            -= SignalUpdateTradeGraphics;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.Positions.Changed            += SignalUpdateTradeGraphics;
				Model.MarketData.InstrumentRemoved += HandleInstrumentRemoved;
				Model.ConnectionChanged            += HandleConnectionChanged;
			}
		}

		/// <summary>Setting specific to this chart</summary>
		public PerChartSettings ChartSettings
		{
			[DebuggerStepThrough] get { return m_chart_settings; }
			private set
			{
				if (m_chart_settings == value) return;
				m_chart_settings = value;
				if (m_chart_settings != null)
				{
					// Pretend there is no settings while we create the saved indicators.
					// This prevents the settings being changed as indicators are added
				//	using (Scope.Create(() => m_chart_settings = null, () => m_chart_settings = value))
				//	{
				//		Indicators.Clear();
				//		foreach (var node in value.Indicators.Elements(XmlTag.Indicator))
				//			Indicators.Add((IndicatorBase)node.ToObject());
				//	}
				}
			}
		}
		private PerChartSettings m_chart_settings;

		/// <summary>The current instrument displayed in the chart</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_impl_instr; }
			private set
			{
				if (m_impl_instr == value) return;
				if (m_impl_instr != null)
				{
					// Remove the SnRLevel indicators from the chart
					// and stop watching for SnRLevels added/removed on the instrument.
					m_impl_instr.SupportResistLevels.ListChanging -= HandleInstrumentSnRLevelsChanging;
					var ids = m_impl_instr.SupportResistLevels.Select(x => x.Id).ToHashSet();
					Indicators.RemoveIf(x => ids.Contains(x.Id));

					// Stop watching for instrument data updates
					m_impl_instr.DataChanged -= HandleInstrumentDataChanged;

					// Release our reference to the instrument
					m_impl_instr.ReferenceCount--;
				}
				m_impl_instr = value;
				if (m_impl_instr != null)
				{
					// Add a reference to the instrument
					m_impl_instr.ReferenceCount++;

					// Ensure a valid time frame is selected
					if (m_impl_instr.TimeFrame == ETimeFrame.None)
						m_impl_instr.TimeFrame = Settings.General.DefaultTimeFrame;

					// Add indicators for the instrument's SnRLevels to this chart.
					// Start watching for SnRLevels added/removed on the instrument.
					m_impl_instr.SupportResistLevels.ListChanging += HandleInstrumentSnRLevelsChanging;
					foreach (var snr in Instrument.SupportResistLevels)
						Indicators.Add(new SnRIndicator(snr));

					// Watch for instrument data updates
					m_impl_instr.DataChanged += HandleInstrumentDataChanged;
				}
				m_chart?.Invalidate();
			}
		}
		private Instrument m_impl_instr;

		/// <summary>The trades associated with the instrument of this chart</summary>
		public IEnumerable<Trade> Trades
		{
			get { return Model.Positions.Trades.Where(x => x.SymbolCode == SymbolCode); }
		}

		/// <summary>Chart elements</summary>
		public BindingListEx<ChartControl.Element> Elements
		{
			get { return m_chart.Elements; }
		}

		/// <summary>The collection of indicators on this chart</summary>
		public BindingSource<IndicatorBase> Indicators
		{
			[DebuggerStepThrough] get { return m_indicators; }
			private set
			{
				if (m_indicators == value) return;
				if (m_indicators != null)
				{
					m_indicators.ListChanging -= HandleIndicatorsListChanging;
				}
				m_indicators = value;
				if (m_indicators != null)
				{
					m_indicators.ListChanging += HandleIndicatorsListChanging;
				}
			}
		}
		private BindingSource<IndicatorBase> m_indicators;

		/// <summary>The symbol displayed on the chart (or empty string if none)</summary>
		public string SymbolCode
		{
			get { return Instrument.SymbolCode; }
		}

		/// <summary>The selected time frame</summary>
		public ETimeFrame TimeFrame
		{
			[DebuggerStepThrough] get { return Instrument.TimeFrame; }
			set
			{
				if (TimeFrame == value) return;
				Instrument.TimeFrame = value;
				DockControl.TabText = ChartTitle;
				m_gfx_cache.Flush();
				ResetToDefaultRange();
				UpdateUI();
			}
		}

		/// <summary>The tab text for the chart</summary>
		public string ChartTitle
		{
			get { return "{0},{1}".Fmt(SymbolCode, TimeFrame); }
		}

		/// <summary>True if graphics for all trades are shown</summary>
		public bool ShowHistoricTrades
		{
			get;
			set;
		}

		/// <summary>A chart element for showing the current price</summary>
		private CurrentPrice CurrentPrice
		{
			get { return m_current_price; }
			set
			{
				if (m_current_price == value) return;
				if (m_current_price != null)
				{
					Util.Dispose(m_current_price);
				}
				m_current_price = value;
				if (m_current_price != null)
				{
					m_chart.Elements.Add(m_current_price);
				}
			}
		}
		private CurrentPrice m_current_price;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Docking
			DockControl.TabText = ChartTitle;
			DockControl.TabCMenu = CreateTabContextMenu();
			#endregion

			#region Tool bar

			// Create trade or add an order/position
			m_btn_trade_long.Click += (s,a) =>
			{
				AddNewOrder(ETradeType.Long);
			};
			m_btn_trade_short.Click += (s,a) =>
			{
				AddNewOrder(ETradeType.Short);
			};
			m_chk_show_historic.CheckedChanged += (s,a) =>
			{
				ShowHistoricTrades = m_chk_show_historic.Checked;
			};

			// Time frame shortcut buttons
			UpdateTimeFrameShortcutButtons();

			// Populate the split button drop down with the available time frames
			foreach (var tf in Enum<ETimeFrame>.Values)
			{
				if (tf == ETimeFrame.None) continue;
				var item = m_splitbtn_timeframes.DropDownItems.Add2(new ToolStripMenuItem(tf.Desc()) { Tag = tf, CheckOnClick = true });
				item.Click += (s,a) =>
				{
					if (ModifierKeys == Keys.Shift)
					{
						// Add or remove the time frame from the button list
						Settings.Chart.TimeFrameBtns = item.Checked
							? Settings.Chart.TimeFrameBtns.Concat(tf).OrderBy(x => x).ToArray()
							: Settings.Chart.TimeFrameBtns.Except(tf).OrderBy(x => x).ToArray();
						UpdateTimeFrameShortcutButtons();
					}
					else
					{
						// Select the clicked time frame
						TimeFrame = tf;
					}
				};
			}

			#endregion

			#region Chart
			m_chart.Name                = "m_chart";
			m_chart.BorderStyle         = BorderStyle.FixedSingle;
			m_chart.Dock                = DockStyle.Fill;
			m_chart.DefaultMouseControl = true;

			m_chart.XAxis.Options.ShowGridLines = true;
			m_chart.XAxis.Options.PixelsPerTick = 50;
			m_chart.XAxis.Options.TickFont = new Font("tahoma", 7f, FontStyle.Regular, GraphicsUnit.Point);
			m_chart.XAxis.Options.MinTickSize = m_chart.XAxis.Options.TickFont.Height * 2.2f;
			m_chart.XAxis.TickText = HandleChartXAxisLabels;

			m_chart.YAxis.Options.ShowGridLines = true;
			m_chart.YAxis.Options.PixelsPerTick = 20;
			m_chart.YAxis.Options.MinTickSize = 50;
			m_chart.YAxis.Options.TickFont = new Font("tahoma", 7f, FontStyle.Regular, GraphicsUnit.Point);
			m_chart.YAxis.TickText = HandleChartYAxisTickText;
			m_chart.YAxis.MeasureTickText = HandleChartYAxisMeasureTickText;

			//m_chart.FindingDefaultRange += HandleFindingDefaultRange;
			m_chart.ChartRendering      += HandleChartRendering;
			m_chart.KeyDown             += HandleChartKeyDown;
			m_chart.MouseDown           += HandleChartMouseDown;
			m_chart.AddOverlaysOnPaint  += HandleAddOverlaysOnPaint;
			m_chart.AddUserMenuOptions  += HandleChartCMenu;

			m_chart.OptionsChanged += (s,a) =>
			{
				// Have to manually invoke save because the reference 'm_chart.Options'
				// doesn't change, only the properties within it.
				Model.Settings.Chart.Style = m_chart.Options;
				Model.Settings.Save();
			};
			m_chart.ChartAreaSelect += (s,a) =>
			{
				// Area select zoom if control is held down
//fixme			var rect = new RectangleF(a.SelectionArea.MinX, a.SelectionArea.MinY, a.SelectionArea.SizeX, a.SelectionArea.SizeY);
//fixme			if (rect.Width > 0 && rect.Height > 0 && ModifierKeys == Keys.Shift)
//fixme				m_chart.ZoomArea(rect, false);

				a.Handled = true;
			};
			m_tsc.ContentPanel.Controls.Add(m_chart);
			#endregion
		}

		/// <summary>Update the state of UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			// Update the check state of time frame shortcut buttons
			foreach (var item in m_ts_timeframes.Items.OfType<ToolStripButton>())
				item.Checked = (ETimeFrame)item.Tag == TimeFrame;

			// Update the checked state of the items in the split button
			m_splitbtn_timeframes.Text = TimeFrame.ToString();
			foreach (var item in m_splitbtn_timeframes.DropDownItems.OfType<ToolStripMenuItem>())
			{
				if (item.Tag == null) continue;
				var tf = (ETimeFrame)item.Tag;
				item.BackColor = tf == TimeFrame ? Color.LightSteelBlue : Color.White;
				item.Checked = Settings.Chart.TimeFrameBtns.Contains(tf);
			}
		}

		/// <summary>Set the X,Y Axis to the default range</summary>
		public void ResetToDefaultRange()
		{
//fixme			m_chart.FindDefaultRange(false);
//fixme			m_chart.ResetToDefaultRange();
		}

		/// <summary>Scroll the chart Y Axis so that the latest price is on screen</summary>
		public void EnsureLatestPriceDisplayed()
		{
			// Get the current price relative to the centre of the displayed Y Axis range
			var price = Instrument.Latest.Close;
			var _80pc = m_chart.YAxis.Max - m_chart.YAxis.Span * 0.2;
			var _20pc = m_chart.YAxis.Min + m_chart.YAxis.Span * 0.2;

			// If the price value is not within 20-80%, scroll the Y Axis to put the price at the 20-80% level
			if (price > _80pc)
				m_chart.YAxis.Shift(price - _80pc);
			if (price < _20pc)
				m_chart.YAxis.Shift(price - _20pc);
		}

		/// <summary>Create a new trade or add to the existing trade</summary>
		private void AddNewOrder(ETradeType tt)
		{
			Model.Positions.Orders.Add(new Order(0, Instrument, tt, Trade.EState.Visualising));
		}

		/// <summary>Returns the graphics model containing the data point with index 'idx'</summary>
		private CachedGfx GfxAt(int cache_idx)
		{
			// On miss, generate the graphics model for the data range [idx, idx + min(GfxModelBatchSize, Count-idx))
			return m_gfx_cache.Get(cache_idx, i =>
			{
				var gfx = new CachedGfx();
				gfx.m_db_idx_range = new Range((i+0) * GfxModelBatchSize, (i+1) * GfxModelBatchSize);

				// Get the series data over the time range specified
				var rng = Instrument.IndexRange(
					Instrument.FirstIdx + gfx.m_db_idx_range.Begi,
					Instrument.FirstIdx + gfx.m_db_idx_range.Endi);
				if (rng.Counti == 0)
					return gfx;

				// Using TriList for the bodies, and LineList for the wicks.
				// So:    6 indices for the body, 4 for the wicks
				//   __|__
				//  |\    |
				//  |  \  |
				//  |____\|
				//     |
				// Dividing the index buffer into [bodies, wicks]
				var candles = Instrument.CandleRange(rng.Begi, rng.Endi);
				var count = rng.Counti;

				// Resize the cache buffers
				m_vbuf.Resize(8 * count);
				m_ibuf.Resize((6+4) * count);
				m_nbuf.Resize(2);

				// Index of the first body index and the first wick index.
				var vert = 0;
				var body = 0;
				var wick = 6 * count;
				var nugt = 0;

				// Create the geometry
				var candle_idx = 0;
				foreach (var candle in candles)
				{
					// Create the graphics with the first candle at x == 0
					var x = (float)candle_idx++;
					var o = (float)Math.Max(candle.Open, candle.Close);
					var h = (float)candle.High;
					var l = (float)candle.Low;
					var c = (float)Math.Min(candle.Open, candle.Close);
					var col = candle.Bullish ? Settings.UI.BullishColour.ToArgbU() : candle.Bearish ? Settings.UI.BearishColour.ToArgbU() : 0xFFA0A0A0;
					var v = vert;

					// Prevent degenerate triangles
					if (o == c)
					{
						o *= 1.000005f;
						c *= 0.999995f;
					}

					// Candle verts
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , h, Z.Candles, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , o, Z.Candles, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x - 0.4f , o, Z.Candles, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x + 0.4f , o, Z.Candles, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x - 0.4f , c, Z.Candles, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x + 0.4f , c, Z.Candles, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , c, Z.Candles, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , l, Z.Candles, 1f), col);

					// Candle body
					m_ibuf[body++] = (ushort)(v + 3);
					m_ibuf[body++] = (ushort)(v + 2);
					m_ibuf[body++] = (ushort)(v + 4);
					m_ibuf[body++] = (ushort)(v + 4);
					m_ibuf[body++] = (ushort)(v + 5);
					m_ibuf[body++] = (ushort)(v + 3);

					// Candle wick
					m_ibuf[wick++] = (ushort)(v + 0);
					m_ibuf[wick++] = (ushort)(v + 1);
					m_ibuf[wick++] = (ushort)(v + 6);
					m_ibuf[wick++] = (ushort)(v + 7);
				}

				m_nbuf[nugt++] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)body);
				m_nbuf[nugt++] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, (uint)vert, (uint)body, (uint)wick);

				// Create the graphics
				gfx.m_obj = new View3d.Object("Candles-[{0},{1})".Fmt(gfx.m_db_idx_range.Begi,gfx.m_db_idx_range.Endi), 0xFFFFFFFF, vert, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());
				return gfx;
			});
		}
		private struct CachedGfx
		{
			/// <summary>The graphics object containing 'GfxModelBatchSize' candles</summary>
			public View3d.Object m_obj;

			/// <summary>
			/// The index range of candles in this graphic.
			/// This index range is zero based and positive, because new candles are being added all the 
			/// time and negative indices get invalidated whenever a new candle is added.</summary>
			public Range m_db_idx_range;
		}

		/// <summary>Update the buttons that appear on the time frame tool bar</summary>
		private void UpdateTimeFrameShortcutButtons()
		{
			using (m_ts_timeframes.SuspendLayout(true))
			{
				m_ts_timeframes.Items.Clear();

				// Add buttons for the time frames given in the chart settings
				foreach (var tf in Settings.Chart.TimeFrameBtns)
				{
					// When clicked switch to the associated time frame
					var item = m_ts_timeframes.Items.Add2(new ToolStripButton(tf.Desc()) { Tag = tf });
					item.Checked = tf == TimeFrame;
					item.Click += (s,a) => TimeFrame = tf;
				}

				// Add the split button used for selecting time frame buttons
				m_ts_timeframes.Items.Add(m_splitbtn_timeframes);
			}
		}

		/// <summary>Create the context menu for this tab</summary>
		private ContextMenuStrip CreateTabContextMenu()
		{
			var cmenu = new ContextMenuStrip();
			using (cmenu.SuspendLayout(true))
			{
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Close"));
					opt.Click += (s,a) =>
					{
						Dispose();
					};
				}
			}
			return cmenu;
		}

		/// <summary>Repopulate the chart with graphics for the trades of the shown instrument</summary>
		private void UpdateTradeGraphics()
		{
			m_sig_update_trade_graphics = false;

			// Create a set of all orders that should be on this chart
			var orders = Trades
				.Where(x => x.State != Trade.EState.Closed || ShowHistoricTrades)
				.SelectMany(x => x.Orders).ToHashSet();

			// Remove graphics for orders not in the set
			// Remove orders from the set that are already on the chart.
			for (int i = Elements.Count; i-- != 0;)
			{
				// If the element is not an OrderChartElement, skip
				var order_gfx = Elements[i] as OrderChartElement;
				if (order_gfx == null)
					continue;

				// If 'order_gfx' is in the set 'orders' then we can
				// remove it from the set since it's on the chart already.
				// If not in the set, then dispose it to remove it from the chart
				if (orders.Contains(order_gfx.Order))
					orders.Remove(order_gfx.Order);
				else
					order_gfx.Dispose();
			}

			// Create new graphics for orders not yet on the chart
			foreach (var order in orders)
				Elements.Add(new OrderChartElement(order));
		}
		private void SignalUpdateTradeGraphics(object sender = null, EventArgs e = null)
		{
			if (m_sig_update_trade_graphics) return;
			m_sig_update_trade_graphics = true;
			Dispatcher.CurrentDispatcher.BeginInvoke(UpdateTradeGraphics);
		}
		private bool m_sig_update_trade_graphics;

		/// <summary>Invalidate any cached graphics</summary>
		public void InvalidateCachedGraphics()
		{
			m_gfx_cache.Flush();
			Invalidate(true);
		}

		/// <summary>Handle the connection to the trade data source connecting/disconnecting</summary>
		private void HandleConnectionChanged(object sender, EventArgs e)
		{
			// On disconnection, the trade data source will reset its list of time frames
			// to send for the instrument this chart is based on. On reconnection, we want
			// to start instrument data being sent again
			Debug.Assert(Instrument.ReferenceCount != 0);
			if (Model.IsConnected)
				Instrument.StartDataRequest();
		}

		/// <summary>Handle instrument removed</summary>
		private void HandleInstrumentRemoved(object sender, DataEventArgs e)
		{
			// Destroy this chart if the instrument it's based on gets removed
			if (e.Instrument == Instrument)
				Dispose();
		}

		/// <summary>Handle market data being added</summary>
		private void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			// If the event is specific to one candle, invalidate the graphics object
			// that contains the representation of that candle.
			if (e.Candle != null)
			{
				// Invalidate the cache value that contains 'idx'
				var idx = Instrument.IndexAt(new TFTime(e.Candle.Timestamp, TimeFrame));
				var cache_idx = (idx - Instrument.FirstIdx) / GfxModelBatchSize;
				m_gfx_cache.Invalidate(cache_idx);
			}
			else
			{
				// Otherwise, just invalidate the whole cache
				m_gfx_cache.Flush();
			}

			// Auto scroll if 'e.Candle' is a new candle
			if (e.NewCandle)
			{
				// If 'x == 0' is not visible, move the camera back by
				// one index position so that view doesn't appear to move.
				if (m_chart.XAxis.Max < 0)
					m_chart.XAxis.Set(m_chart.XAxis.Min - 1, m_chart.XAxis.Max - 1);
				// Otherwise, scroll the YAxis to ensure the latest price is visible
				else
					EnsureLatestPriceDisplayed();
			}

			// Signal a refresh
			m_chart.Invalidate();
		}

		/// <summary>Handle the chart about to render</summary>
		private void HandleChartRendering(object sender, ChartControl.ChartRenderingEventArgs e)
		{
			// Convert the XAxis values into an index range.
			// (indices, not time frame units, because of the gaps in the price data).
			var range = Instrument.IndexRange((int)(m_chart.XAxis.Min - 1), (int)(m_chart.XAxis.Max + 1));

			// Convert the index range into a cache index range
			var cache0 = (range.Begi - Instrument.FirstIdx) / GfxModelBatchSize;
			var cache1 = (range.Endi - Instrument.FirstIdx) / GfxModelBatchSize;
			for (int i = cache0; i <= cache1; ++i)
			{
				// Get the graphics model that contains candle 'i'
				var gfx = GfxAt(i);
				if (gfx.m_obj != null)
				{
					// Position the graphics object relative to 'x == 0'
					var x = Instrument.FirstIdx + gfx.m_db_idx_range.Begi;
					gfx.m_obj.O2P = m4x4.Translation(new v4(x, 0.0f, 0.0f, 1.0f));
					e.AddToScene(gfx.m_obj);
				}
			}
		}

		/// <summary>Called when the chart paints</summary>
		private void HandleAddOverlaysOnPaint(object sender, ChartControl.AddOverlaysOnPaintEventArgs e)
		{
			// Add the ask/bid price to the Y axis
			using (var bsh = new SolidBrush(Settings.UI.AskColour))
			{
				var price     = Instrument.PriceData.AskPrice;
				var price_str = e.Chart.YAxis.TickText(price, 0.0);
				var pt        = e.Chart.ChartToClient(new PointF((float)e.Chart.XAxis.Min, (float)price));
				var sz        = e.Gfx.MeasureString(price_str, e.Chart.YAxis.Options.TickFont);
				var box       = sz.Scaled(1.05f);
				e.Gfx.FillRectangle(bsh, new RectangleF(pt.X - box.Width, pt.Y - box.Height/2, box.Width, box.Height));
				e.Gfx.DrawString(price_str, e.Chart.YAxis.Options.TickFont, Brushes.White, pt.X - sz.Width, pt.Y - sz.Height/2);
			}
			using (var bsh = new SolidBrush(Settings.UI.BidColour))
			{
				var price     = Instrument.PriceData.BidPrice;
				var price_str = e.Chart.YAxis.TickText(price, 0.0);
				var pt        = e.Chart.ChartToClient(new PointF((float)e.Chart.XAxis.Min, (float)price));
				var sz        = e.Gfx.MeasureString(price_str, e.Chart.YAxis.Options.TickFont);
				var box       = sz.Scaled(1.05f);
				e.Gfx.FillRectangle(bsh, new RectangleF(pt.X - box.Width, pt.Y - box.Height/2, box.Width, box.Height));
				e.Gfx.DrawString(price_str, e.Chart.YAxis.Options.TickFont, Brushes.White, pt.X - sz.Width, pt.Y - sz.Height/2);
			}

			// This needs to be done on a textured quad and positioned in the 3d scene
			// Draw the time remaining on the latest candle
			if (e.Chart.XAxis.Range.Contains(0.0))
			{
				// Find the remaining time
				var latest = Instrument.Latest;
				var one = Misc.TimeFrameToTimeSpan(1.0, TimeFrame);
				var age = Model.UtcNow - latest.TimestampUTC;
				var str = (one - age).ToMinimalString();

				// Measure the time remaining string
				var font = e.Chart.XAxis.Options.TickFont;
				var sz = e.Gfx.MeasureString(str, font);

				// Find the point on the chart to draw the string
				var pt = e.Chart.ChartToClient(new PointF(0f, (float)e.Chart.YAxis.Min));
				pt.X = (int)(pt.X - sz.Width / 2);
				pt.Y = (int)(pt.Y + 5.0);

				// Draw the string under the candle
				e.Gfx.FillRectangle(Brushes.Black, new RectangleF(pt.X, pt.Y, sz.Width, sz.Height));
				e.Gfx.DrawString(str, font, Brushes.White, pt.X, pt.Y);
			}
		}

		/// <summary>Handle finding the default range for the chart</summary>
		private void HandleFindingDefaultRange(object sender, ChartControl.FindingDefaultRangeEventArgs e)
		{
			// Get the initial range values (since XRange,YRange are structs)
			var xmin = e.XRange.Beg;
			var xmax = e.XRange.End;
			var ymin = e.YRange.Beg;
			var ymax = e.YRange.End;

			// Set the x range to a minimum of the preferred candle view count
			var ahead = Settings.Chart.ViewCandlesAhead;
			if (ahead - Settings.Chart.ViewCandleCount < xmin) xmin = ahead - Settings.Chart.ViewCandleCount;
			if (ahead > xmax) xmax = ahead;

			// Find the bounds of the candle price data
			foreach (var candle in Instrument.CandleRange((int)xmin, (int)xmax))
			{
				if (candle.Low  < ymin) ymin = candle.Low;
				if (candle.High > ymax) ymax = candle.High;
			}

			// Impose a minimum Y range of 20 pips
			if (ymax - ymin < 0.002)
			{
				var c = (ymin + ymax) * 0.5;
				ymin = c - 0.001;
				ymax = c + 0.001;
			}

			// Update the args
			e.XRange = new RangeF(xmin, xmax);
			e.YRange = new RangeF(ymin, ymax);
		}

		/// <summary>Handle chart keyboard shortcuts</summary>
		private void HandleChartKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
			case Keys.F5:
				{
					m_chart.Invalidate();
					break;
				}
			case Keys.F7:
				{
//fixme					m_chart.FindDefaultRange(false);
//fixme					m_chart.ResetToDefaultRange();
					break;
				}
			}
		}

		/// <summary>Handle chart mouse down</summary>
		private void HandleChartMouseDown(object sender, MouseEventArgs args)
		{
			// Edit chart elements when Control is held down
			if (ModifierKeys == Keys.Control)
			{
				// Look for hit drag-able indicators
				var hit = m_chart.HitTestCS(args.Location, ModifierKeys, x => (x as IndicatorBase)?.Dragable ?? false);
				if (hit.Hits.Count != 0)
				{
					var ind = (IndicatorBase)hit.Hits[0].Element;

					// Create a mouse operation for dragging the indicator
					var op = ind.CreateDragMouseOp();
					m_chart.MouseOperations.SetPending(MouseButtons.Left, op);
				}
			}
		}

		/// <summary>Convert the XAxis values into pretty datetime strings</summary>
		private string HandleChartXAxisLabels(double x, double step)
		{
			// Note:
			// The X axis is not a linear time axis because the markets are not online all the time.
			// The X axis is actually an index into the 'Instrument' data where 0 = latest and negative
			// values go further back in the history. Use 'Instrument.TimeToIndexRange' to convert time
			// values to X Axis values.

			// Draw the X Axis labels as indices instead of time stamps
			if (m_xaxis_in_candle_indices)
				return x.ToString();

			if (Instrument == null)
				return string.Empty;

			const string long_fmt  = "ddd dd-MMM-yy'\r\n'HH:mm";
			const string short_fmt = "HH:mm";

			// The range of indices
			var first = -(Instrument.Count-1);
			var last  = 1;

			var prev = (int)(x - step);
			var curr = (int)(x);
			if (curr < first || curr >= last)
				return string.Empty;

			// Get the time stamp of the candle at the tick mark in local time
			var dt_curr = TimeZone.CurrentTimeZone.ToLocalTime(new DateTimeOffset(Instrument[curr].Timestamp, TimeSpan.Zero).DateTime);
			if (curr == first || prev < first || x - step < m_chart.XAxis.Min) // First tick on the x axis
				return dt_curr.ToString(long_fmt);

			// If the current tick mark represents the same candle as the previous one, no text is required
			if (prev == curr)
				return string.Empty;

			// Get the time stamp of the candle at the previous tick mark in local time
			if (prev >= first && prev < last)
			{
				var dt_prev = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(Instrument[prev].Timestamp, DateTimeKind.Utc));
				if (dt_curr.Day != dt_prev.Day)
					return dt_curr.ToString(long_fmt);
			}

			return dt_curr.ToString(short_fmt);
		}
		private bool m_xaxis_in_candle_indices;

		/// <summary>Convert the YAxis values into pretty price strings</summary>
		private string HandleChartYAxisTickText(double x, double step)
		{
			if (Instrument == null)
				return string.Empty;

			var str = new StringBuilder(Math.Round(x, 5, MidpointRounding.AwayFromZero).ToString("F5"));
			for (; str.Length > 6 && str[str.Length-1] == '0';) str.Length--;
			return str.ToString();;
		}

		/// <summary>Measure the size of the y axis text</summary>
		private float HandleChartYAxisMeasureTickText(Graphics gfx, bool width)
		{
			var sz0 = gfx.MeasureString(HandleChartYAxisTickText(m_chart.YAxis.Min, 0.0), m_chart.YAxis.Options.TickFont);
			var sz1 = gfx.MeasureString(HandleChartYAxisTickText(m_chart.YAxis.Max, 0.0), m_chart.YAxis.Options.TickFont);
			return width ? Math.Max(sz0.Width, sz1.Width) : Math.Max(sz0.Height, sz1.Height);
		}

		/// <summary>Handle the chart context menu</summary>
		private void HandleChartCMenu(object sender, ChartControl.AddUserMenuOptionsEventArgs e)
		{
			switch (e.Type)
			{
			case ChartControl.AddUserMenuOptionsEventArgs.EType.Chart:
				#region
				{
					int idx = 0;

					#region Indicators
					var indicators_menu = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem("Indicators"));
					{
						// Add a new indicator
						var add_menu = indicators_menu.DropDownItems.Add2(new ToolStripMenuItem("Add Indicator"));
						{
							// S&R Level
							var opt = add_menu.DropDownItems.Add2(new ToolStripMenuItem("Support/Resistance Level"));
							opt.Click += (s,a) =>
							{
								// SnR levels are an Instrument thing. Add the SnRLevel to the instrument.
								// We'll notice it in a different handler and add an indicator for it there.
								var snr = new SnRLevel(e.HitResult.ChartPoint.Y, m_chart.YAxis.Span * 0.05 / Instrument.PriceData.PipSize);
								Instrument.SupportResistLevels.Add(snr);
								EditSnRLevel(Indicators.OfType<SnRIndicator>().First(x => x.Id == snr.Id));
							};
						}{
							// EMA
							var opt = add_menu.DropDownItems.Add2(new ToolStripMenuItem("Exponential Moving Average"));
							opt.Click += (s,a) => EditEmaIndicator();
						}{
							// S&R
							var opt = add_menu.DropDownItems.Add2(new ToolStripMenuItem("Support and Resistance"));
							opt.Click += (s,a) => EditSnRIndicator();
						}{
							// Trend Strength
							var opt = add_menu.DropDownItems.Add2(new ToolStripMenuItem("Trend Strength"));
							opt.Click += (s,a) => EditTrendStrengthIndicator();
						}

						indicators_menu.DropDownItems.AddSeparator();

						// Modify existing indicators
						var hit_indicators = e.HitResult.Hits.Where(x => x.Element is IndicatorBase).Select(x => (IndicatorBase)x.Element).ToArray();
						foreach (var indicator in Indicators)
						{
							var colour = hit_indicators.Contains(indicator) ? Color.Blue : Color.Black;
							var opt = indicators_menu.DropDownItems.Add2(new ToolStripMenuItem(indicator.Name) { ForeColor = colour });
							opt.Click += (s,a) => EditIndicator(indicator);
						}
					}
					#endregion

					e.Menu.Items.InsertSeparator(idx++);

					#region Orders
					{
						// The price where the mouse was clicked
						var click_price = Misc.RoundToNearestPip(e.HitResult.ChartPoint.Y,  Instrument.PriceData);
						Action<ETradeType> new_order = (tt) =>
						{
							Model.Positions.Orders.Add2(new Order(0, Instrument, tt, Trade.EState.Visualising)
							{
								EntryPrice = click_price,
								StopLossRel = m_chart.YAxis.Span * 0.1,
								TakeProfitRel = m_chart.YAxis.Span * 0.1,
							});
						};

						{// Buy
							var opt = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem("Buy at {0}".Fmt(click_price)) { ForeColor = Settings.UI.AskColour });
							opt.Click += (s,a) => new_order(ETradeType.Long);
						}
						{// Sell
							var opt = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem("Sell at {0}".Fmt(click_price)) { ForeColor = Settings.UI.BidColour });
							opt.Click += (s,a) => new_order(ETradeType.Short);
						}
					}
					#endregion

					e.Menu.Items.InsertSeparator(idx++);

					break;
				}
				#endregion
			case ChartControl.AddUserMenuOptionsEventArgs.EType.XAxis:
				#region
				{
					var opt = e.Menu.Items.Add2(new ToolStripMenuItem(m_xaxis_in_candle_indices ? "Local Time" : "Candle Index"));
					opt.Click += (s,a) =>
					{
						m_xaxis_in_candle_indices = !m_xaxis_in_candle_indices;
						Invalidate(true);
					};
					break;
				}
				#endregion
			}
		}

		#region Indicators

		/// <summary>Handle indicators being added/removed from the chart</summary>
		private void HandleIndicatorsListChanging(object sender, ListChgEventArgs<IndicatorBase> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				{
					// Ensure the 'Chart' property is set on the indicator
					e.Item.Instrument = Instrument;
					e.Item.Chart = m_chart;

					// When the indicator changes, update the saved chart settings
					e.Item.DataChanged += HandleIndicatorChanged;

					// Update the settings.
					HandleIndicatorChanged();

					// Refresh the chart
					Invalidate(true);
					break;
				}
			case ListChg.ItemRemoved:
				{
					// Remove from the Chart
					e.Item.Chart = null;
					e.Item.Instrument = null;

					// Ignore changes
					e.Item.DataChanged -= HandleIndicatorChanged;

					// Dispose the indicator
					Util.Dispose(e.Item);

					// Update the settings.
					HandleIndicatorChanged();

					// Refresh the chart
					Invalidate(true);
					break;
				}
			}
		}

		/// <summary>Handle an indicator on this chart changing</summary>
		private void HandleIndicatorChanged(object sender = null, EventArgs e = null)
		{
			// ChartSettings is set to null on Dispose so that removing indicators
			// doesn't write to the settings.
			if (ChartSettings == null)
				return;

			// Save the indicators to the chart settings
			var elements = new XElement("root");
			foreach (var indicator in Indicators)
				elements.Add2(XmlTag.Indicator, indicator, true);

			// Update the settings
			ChartSettings.Indicators = elements;
		}

		/// <summary>Handle SnR levels added to the instrument</summary>
		private void HandleInstrumentSnRLevelsChanging(object sender, ListChgEventArgs<SnRLevel> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				{
					// Create an indicator for the new SnR level
					Indicators.Add(new SnRIndicator(e.Item));
					break;
				}
			case ListChg.ItemRemoved:
				{
					// Remove the associated indicator
					Indicators.RemoveIf(x => x.Id == e.Item.Id);
					break;
				}
			}
		}

		/// <summary>Edit an existing indicator</summary>
		private void EditIndicator(object indicator)
		{
			if      (indicator is SnRIndicator) EditSnRLevel    ((SnRIndicator)indicator);
			else if (indicator is EmaIndicator) EditEmaIndicator((EmaIndicator)indicator);
			else if (indicator is SnR         ) EditSnRIndicator((SnR         )indicator);
			else throw new Exception("Unknown indicator type: {0}".Fmt(indicator.GetType().Name));
		}

		/// <summary>Add/Edit an EMA indicator</summary>
		private void EditEmaIndicator(EmaIndicator ema = null)
		{
			ema = ema ?? Indicators.Add2(new EmaIndicator());
			new EditEmaUI(this, ema).Show(this);
		}

		/// <summary>Add/Edit a support and resistance indicator</summary>
		private void EditSnRIndicator(SnR snr = null)
		{
			snr = snr ?? Indicators.Add2(new SnR());
			new EditSnrUI(this, snr).Show(this);
		}

		/// <summary>Add/Edit a trend strength indicator</summary>
		private void EditTrendStrengthIndicator(TrendStrength ts = null)
		{
			ts = ts ?? Indicators.Add2(new TrendStrength());
			// new EditTrendStrengthUI(this, ts).Show(this);
		}

		/// <summary>Add/Edit a support/resistance line</summary>
		private void EditSnRLevel(SnRIndicator snr)
		{
			//snr = snr ?? Instrument.SupportResistLevels.Add2(new SnRLevel(m_chart.YAxis.Centre, m_chart.YAxis.Span * 0.05 / Instrument.PriceData.PipSize));
			new EditSnrLevelUI(this, snr).Show(this);
		}

		#endregion

		#region Mouse Operations
		#endregion

		/// <summary>Convert between a chart name to a symbol code (or null)</summary>
		public static string ToSymbolCode(string name)
		{
			return name.SubstringRegex(ChartNamePatn).FirstOrDefault();
		}
		public static string ToChartName(string symbol_code)
		{
			var name = "Chart-{0}".Fmt(symbol_code);
			Debug.Assert(ToSymbolCode(name) == symbol_code);
			return name;
		}
		private const string ChartNamePatn = @"^Chart-(\w+)$";

		/// <summary>Z-values for chart elements</summary>
		public static class Z
		{
			// Grid lines are drawn at 0
			public const float SnR        = 0.009f;
			public const float Candles    = 0.010f;
			public const float Indicators = 0.011f;
			public const float Trades     = 0.012f;
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChartUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ts_trades = new System.Windows.Forms.ToolStrip();
			this.m_btn_trade_long = new System.Windows.Forms.ToolStripButton();
			this.m_btn_trade_short = new System.Windows.Forms.ToolStripButton();
			this.m_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_chk_show_historic = new System.Windows.Forms.ToolStripButton();
			this.m_ts_timeframes = new System.Windows.Forms.ToolStrip();
			this.m_splitbtn_timeframes = new System.Windows.Forms.ToolStripSplitButton();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ts_trades.SuspendLayout();
			this.m_ts_timeframes.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(668, 646);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(668, 673);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts_trades);
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts_timeframes);
			// 
			// m_ts_trades
			// 
			this.m_ts_trades.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_trades.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.m_ts_trades.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_trade_long,
            this.m_btn_trade_short,
            this.m_sep0,
            this.m_chk_show_historic});
			this.m_ts_trades.Location = new System.Drawing.Point(5, 0);
			this.m_ts_trades.Name = "m_ts_trades";
			this.m_ts_trades.Size = new System.Drawing.Size(90, 27);
			this.m_ts_trades.TabIndex = 1;
			// 
			// m_btn_trade_long
			// 
			this.m_btn_trade_long.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_trade_long.Image = global::Tradee.Properties.Resources.trade_long;
			this.m_btn_trade_long.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_trade_long.Name = "m_btn_trade_long";
			this.m_btn_trade_long.Size = new System.Drawing.Size(24, 24);
			this.m_btn_trade_long.Text = "Trade Long";
			// 
			// m_btn_trade_short
			// 
			this.m_btn_trade_short.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_trade_short.Image = global::Tradee.Properties.Resources.trade_short;
			this.m_btn_trade_short.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_trade_short.Name = "m_btn_trade_short";
			this.m_btn_trade_short.Size = new System.Drawing.Size(24, 24);
			this.m_btn_trade_short.Text = "Trade Short";
			// 
			// m_sep0
			// 
			this.m_sep0.Name = "m_sep0";
			this.m_sep0.Size = new System.Drawing.Size(6, 27);
			// 
			// m_chk_show_historic
			// 
			this.m_chk_show_historic.CheckOnClick = true;
			this.m_chk_show_historic.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_chk_show_historic.Image = ((System.Drawing.Image)(resources.GetObject("m_chk_show_historic.Image")));
			this.m_chk_show_historic.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_show_historic.Name = "m_chk_show_historic";
			this.m_chk_show_historic.Size = new System.Drawing.Size(24, 24);
			this.m_chk_show_historic.Text = "Historic Trades";
			this.m_chk_show_historic.ToolTipText = "Show/Hide historic trades on this chart";
			// 
			// m_ts_timeframes
			// 
			this.m_ts_timeframes.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_timeframes.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.m_ts_timeframes.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_splitbtn_timeframes});
			this.m_ts_timeframes.Location = new System.Drawing.Point(102, 0);
			this.m_ts_timeframes.Name = "m_ts_timeframes";
			this.m_ts_timeframes.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.m_ts_timeframes.Size = new System.Drawing.Size(188, 27);
			this.m_ts_timeframes.TabIndex = 0;
			// 
			// m_splitbtn_timeframes
			// 
			this.m_splitbtn_timeframes.Image = global::Tradee.Properties.Resources.clock;
			this.m_splitbtn_timeframes.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_splitbtn_timeframes.Name = "m_splitbtn_timeframes";
			this.m_splitbtn_timeframes.Size = new System.Drawing.Size(145, 24);
			this.m_splitbtn_timeframes.Text = "Select Time Frames";
			this.m_splitbtn_timeframes.ToolTipText = "Select Time Frames\r\nShift-Click to insert a tool bar button\r\n";
			// 
			// ChartUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_tsc);
			this.Name = "ChartUI";
			this.Size = new System.Drawing.Size(668, 673);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.m_ts_trades.ResumeLayout(false);
			this.m_ts_trades.PerformLayout();
			this.m_ts_timeframes.ResumeLayout(false);
			this.m_ts_timeframes.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
