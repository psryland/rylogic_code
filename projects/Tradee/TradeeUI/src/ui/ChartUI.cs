using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.attrib;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Base class for charts</summary>
	public class ChartUI :BaseUI
	{
		// Notes:
		// XAxis x = 0 corresponds to the Latest candle in the instrument, prior candles are rendered
		// at index positions back from the end. i.e. x = 0 = latest, x = -1 = latest-1, x = -2 = latest-2

#if DEBUG
		private const int GfxModelBatchSize = 10;
#else
		private const int GfxModelBatchSize = 1024;
#endif
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
		private pr.gui.ChartControl       m_chart;
		#endregion

		public ChartUI(MainModel model, Instrument instr, ChartSettings chart_settings)
			:base(model, "Chart-{0}".Fmt(instr.SymbolCode))
		{
			InitializeComponent();

			m_vbuf = new List<View3d.Vertex>();
			m_ibuf = new List<ushort>();
			m_nbuf = new List<View3d.Nugget>();
			m_gfx_cache = new Cache<int, CachedGfx>();

			ChartSettings = chart_settings ?? new ChartSettings();
			Instrument = instr;

			SetupUI();
			UpdateUI();

			// Select the time frame from the chart settings
			TimeFrame = ChartSettings.TimeFrame;

			// Create a current price indicator
			CurrentPrice = new CurrentPrice(Model, Instrument, ChartSettings);
		}
		protected override void Dispose(bool disposing)
		{
			CurrentPrice = null;
			Trade = null;
			Instrument = null;
			Util.Dispose(ref m_gfx_cache);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Settings for this chart</summary>
		public ChartSettings ChartSettings
		{
			get;
			private set;
		}

		/// <summary>A unique id for the chart</summary>
		public Guid Id
		{
			get { return ChartSettings.Id; }
		}

		/// <summary>The current instrument displayed in the chart</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_impl_instr; }
			private set
			{
				if (m_impl_instr == value) return;
				if (m_impl_instr != null)
				{
					m_impl_instr.DataAdded -= HandleDataAdded;
				}
				m_impl_instr = value;
				if (m_impl_instr != null)
				{
					m_impl_instr.DataAdded += HandleDataAdded;
				}
				m_chart.Invalidate();
			}
		}
		private Instrument m_impl_instr;

		/// <summary>The trade associated with this chart (or null)</summary>
		public Trade Trade
		{
			[DebuggerStepThrough] get { return m_trade; }
			set
			{
				if (m_trade == value) return;
				if (m_trade != null)
				{
					Debug.Assert(value == null, "Don't change a chart from one trade to another");
				}
				m_trade = value;
				if (m_trade != null)
				{
				}
				DockControl.TabText = ChartTitle;
				UpdateUI();
			}
		}
		private Trade m_trade;

		/// <summary>The symbol displayed on the chart (or empty string if none)</summary>
		public string Symbol
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
				ChartCtrl.FindDefaultRange(false);
				ChartCtrl.ResetToDefaultRange();
				UpdateUI();
			}
		}

		/// <summary>The tab text for the chart</summary>
		private string ChartTitle
		{
			get { return "{0},{1}".Fmt(Symbol, TimeFrame); }
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
					m_chart.Elements.Remove(m_current_price);
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

		/// <summary>The underlying chart control</summary>
		public ChartControl ChartCtrl
		{
			get { return m_chart; }
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Docking
			DockControl.TabCMenu = CreateTabContextMenu();
			#endregion

			#region Tool bar

			// Create trade or add an order/position
			m_btn_trade_long.Click += (s,a) =>
			{
				AddTrade(ETradeType.Long);
			};
			m_btn_trade_short.Click += (s,a) =>
			{
				AddTrade(ETradeType.Short);
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
						ChartSettings.TimeFrameBtns = item.Checked
							? ChartSettings.TimeFrameBtns.Concat(tf).OrderBy(x => x).ToArray()
							: ChartSettings.TimeFrameBtns.Except(tf).OrderBy(x => x).ToArray();
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
			m_chart.Options = Settings.UI.ChartStyle;
			m_chart.OptionsChanged += (s,a) => Model.Settings.UI.ChartStyle = m_chart.Options;
			m_chart.FindingDefaultRange += HandleFindingDefaultRange;
			m_chart.ChartRendering += HandleChartRendering;
			m_chart.KeyDown += HandleChartKeyDown;
			m_chart.AddOverlaysOnPaint += HandleAddOverlaysOnPaint;

			m_chart.XAxis.Options.ShowGridLines = true;
			m_chart.XAxis.Options.PixelsPerTick = 50;
			m_chart.XAxis.Options.MinTickSize = 30;
			m_chart.XAxis.Options.TickFont = new Font("tahoma", 7f, FontStyle.Regular, GraphicsUnit.Point);
			m_chart.XAxis.TickText = HandleChartXAxisLabels;

			m_chart.YAxis.Options.ShowGridLines = true;
			m_chart.YAxis.Options.PixelsPerTick = 20;
			m_chart.YAxis.Options.MinTickSize = 50;
			m_chart.YAxis.Options.TickFont = new Font("tahoma", 7f, FontStyle.Regular, GraphicsUnit.Point);
			m_chart.YAxis.TickText = HandleChartYAxisLabels;
			#endregion
		}

		/// <summary>Update the state of UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			// Update the check state of time frame shortcut buttons
			foreach (var item in m_ts_timeframes.Items.OfType<ToolStripButton>())
				item.Checked = (ETimeFrame)item.Tag == TimeFrame;

			// Update the checked state of the items in the split button
			foreach (var item in m_splitbtn_timeframes.DropDownItems.OfType<ToolStripMenuItem>())
			{
				if (item.Tag == null) continue;
				var tf = (ETimeFrame)item.Tag;
				item.BackColor = tf == TimeFrame ? Color.LightSteelBlue : Color.White;
				item.Checked = ChartSettings.TimeFrameBtns.Contains(tf);
			}
		}

		/// <summary>Create a new trade or add to the existing trade</summary>
		private void AddTrade(ETradeType tt)
		{
			// If this chart is not associated with a trade, then create a new one
			if (Trade == null)
				Model.CreateNewTrade(Instrument, tt, this);

			// Otherwise, add an order/position to the current trade
			else
				Trade.AddOrder(tt);
		}

		/// <summary>Returns the graphics model containing the data point with index 'idx'</summary>
		private CachedGfx GfxAt(int cache_idx)
		{
			// On miss, generate the graphics model for the data range [idx0, idx0+GfxModelBatchSize)
			return m_gfx_cache.Get(cache_idx, i =>
			{
				var gfx = new CachedGfx();

				// Get the series data over the time range specified by the chart
				gfx.m_idx_range = Instrument.IndexRange(i * GfxModelBatchSize, (i + 1) * GfxModelBatchSize);
				var count = gfx.m_idx_range.Counti;
				if (count == 0)
					return default(CachedGfx);

				// Using TriList for the bodies, and LineList for the wicks.
				// So:    6 indices for the body, 4 for the wicks
				//   __|__
				//  |\    |
				//  |  \  |
				//  |____\|
				//     |
				// Dividing the index buffer into [bodies, wicks]
				var candles = Instrument.CandleRange(gfx.m_idx_range.Begini, gfx.m_idx_range.Endi);

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
				var idx = 0;
				foreach (var candle in candles)
				{
					// Create the graphics with the first candle at x == 0
					var x = (float)idx++;
					var o = (float)Math.Max(candle.Open, candle.Close);
					var h = (float)candle.High;
					var l = (float)candle.Low;
					var c = (float)Math.Min(candle.Open, candle.Close);
					var col = candle.Bullish ? Settings.UI.BullishColour.ToArgbU() : candle.Bearish ? Settings.UI.BearishColour.ToArgbU() : 0xFFA0A0A0;
					var v = vert;

					 // Prevent degenerate triangles
					 if (o == c)
						c = o * 0.99999f;

					// Candle verts
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , h, 0, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , o, 0, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x - 0.4f , o, 0, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x + 0.4f , o, 0, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x - 0.4f , c, 0, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x + 0.4f , c, 0, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , c, 0, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , l, 0, 1f), col);

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
				gfx.m_obj = new View3d.Object("Candles-[{0},{1})".Fmt(gfx.m_idx_range.Begini,gfx.m_idx_range.Endi), 0xFFFFFFFF, vert, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());
				return gfx;
			});
		}
		private struct CachedGfx
		{
			/// <summary>The graphics object containing 'GfxModelBatchSize' candles</summary>
			public View3d.Object m_obj;

			/// <summary>The index range of candles in this graphic</summary>
			public Range m_idx_range;
		}

		/// <summary>Update the buttons that appear on the time frame tool bar</summary>
		private void UpdateTimeFrameShortcutButtons()
		{
			using (m_ts_timeframes.SuspendLayout(true))
			{
				m_ts_timeframes.Items.Clear();

				// Add buttons for the time frames given in the chart settings
				foreach (var tf in ChartSettings.TimeFrameBtns)
				{
					// When clicked switch to the associated time frame
					var item = m_ts_timeframes.Items.Add2(new ToolStripButton(tf.Desc()) { Tag = tf });
					item.Checked = tf == TimeFrame;
					item.Click += (s,a) =>
					{
						TimeFrame = tf;
					};
				}

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

		/// <summary>Handle market data being added</summary>
		private void HandleDataAdded(object sender, DataEventArgs e)
		{
			// Invalidate the cache value that contains 'idx'
			var time_stamp = new TFTime(e.Candle.Timestamp, TimeFrame);
			var idx = Instrument.IndexAt(time_stamp);
			if (idx < 0) idx = ~idx;
			m_gfx_cache.Invalidate(idx / GfxModelBatchSize);
			m_chart.Invalidate();

			// If 'e.Candle' is a new candle and 'x == 0' is not visible,
			// move the camera back by one time frame unit so that view doesn't appear to move
			if (e.NewCandle && m_chart.XAxis.Max < 0)
				m_chart.XAxis.Set(m_chart.XAxis.Min - 1, m_chart.XAxis.Max - 1);
		}

		/// <summary>Handle the chart about to render</summary>
		private void HandleChartRendering(object sender, ChartControl.ChartRenderingEventArgs e)
		{
			var x0 = Instrument.Count;

			// Add the graphics models in the visible time range
			var range = Instrument.IndexRange(x0 + (int)m_chart.XAxis.Min, x0 + (int)(m_chart.XAxis.Max + 1.0));

			// Add models from the cache
			for (int i = range.Begini/GfxModelBatchSize, iend = range.Endi/GfxModelBatchSize; i <= iend;  ++i)
			{
				var gfx = GfxAt(i);
				if (gfx.m_obj != null)
				{
					// Position the graphics object relative to 'x == 0'
					gfx.m_obj.O2P = m4x4.Translation(new v4(gfx.m_idx_range.Begini - x0, 0.0f, 0.0f, 1.0f));
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
		}

		/// <summary>Handle finding the default range for the chart</summary>
		private void HandleFindingDefaultRange(object sender, ChartControl.FindingDefaultRangeEventArgs e)
		{
			// Get the initial range values (since XRange,YRange are structs)
			var xmin = e.XRange.Begin;
			var xmax = e.XRange.End;
			var ymin = e.YRange.Begin;
			var ymax = e.YRange.End;

			// Set the x range to a minimum of the preferred candle view count
			if (20 - ChartSettings.ViewCandleCount < xmin) xmin = 20 - ChartSettings.ViewCandleCount;
			if (20 > xmax) xmax = 20;

			// Find the bounds of the candle price data
			var imin = (int)(Instrument.Count + xmin);
			var imax = (int)(Instrument.Count + xmax);
			foreach (var candle in Instrument.CandleRange(imin, imax))
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
					m_chart.FindDefaultRange(false);
					m_chart.ResetToDefaultRange();
					break;
				}
			}
		}

		/// <summary>Convert the XAxis values into pretty datetime strings</summary>
		private string HandleChartXAxisLabels(double x, double step)
		{
			//return x.ToString();
			if (Instrument == null)
				return string.Empty;

			const string long_fmt  = "ddd dd-MMM-yy'\r\n'HH:mm";
			const string short_fmt = "HH:mm";

			// Get the index range from 'x-step' to 'x'
			var i0 = (int)(Instrument.Count + x - step);
			var i1 = (int)(Instrument.Count + x);
			if (i1 < 0 || i1 >= Instrument.Count)
				return string.Empty;

			// Get the time stamp of the candle at the tick mark in local time
			var dt_curr = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(Instrument.PriceHistory[i1].Timestamp, DateTimeKind.Utc));
			if (i1 == 0 || x - step < m_chart.XAxis.Min) // First tick on the x axis
				return dt_curr.ToString(long_fmt);

			// If the current tick mark represents the same candle as the previous one, no text is required
			if (i0 == i1)
				return string.Empty;

			// Get the time stamp of the candle at the previous tick mark in local time
			if (i0 >= 0 && i0 < Instrument.Count)
			{
				var dt_prev = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(Instrument.PriceHistory[i0].Timestamp, DateTimeKind.Utc));
				if (dt_curr.Day != dt_prev.Day)
					return dt_curr.ToString(long_fmt);
			}

			return dt_curr.ToString(short_fmt);
		}

		/// <summary>Convert the YAxis values into pretty price strings</summary>
		private string HandleChartYAxisLabels(double x, double step)
		{
			if (Instrument == null)
				return string.Empty;

			return Math.Round(x, 5, MidpointRounding.AwayFromZero).ToString("F5");
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChartUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_chart = new pr.gui.ChartControl();
			this.m_ts_trades = new System.Windows.Forms.ToolStrip();
			this.m_btn_trade_long = new System.Windows.Forms.ToolStripButton();
			this.m_btn_trade_short = new System.Windows.Forms.ToolStripButton();
			this.m_ts_timeframes = new System.Windows.Forms.ToolStrip();
			this.m_splitbtn_timeframes = new System.Windows.Forms.ToolStripSplitButton();
			this.m_tsc.ContentPanel.SuspendLayout();
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
			this.m_tsc.ContentPanel.Controls.Add(this.m_chart);
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
			// m_chart
			// 
			this.m_chart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_chart.Centre = ((System.Drawing.PointF)(resources.GetObject("m_chart.Centre")));
			this.m_chart.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_chart.Location = new System.Drawing.Point(0, 0);
			this.m_chart.MouseNavigation = true;
			this.m_chart.Name = "m_chart";
			this.m_chart.Size = new System.Drawing.Size(668, 646);
			this.m_chart.TabIndex = 0;
			this.m_chart.Title = "";
			this.m_chart.Zoom = 1D;
			this.m_chart.ZoomMax = 3.4028234663852886E+38D;
			this.m_chart.ZoomMin = 1.4012984643248171E-45D;
			// 
			// m_ts_trades
			// 
			this.m_ts_trades.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_trades.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.m_ts_trades.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_trade_long,
            this.m_btn_trade_short});
			this.m_ts_trades.Location = new System.Drawing.Point(5, 0);
			this.m_ts_trades.Name = "m_ts_trades";
			this.m_ts_trades.Size = new System.Drawing.Size(60, 27);
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
			// m_ts_timeframes
			// 
			this.m_ts_timeframes.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_timeframes.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.m_ts_timeframes.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_splitbtn_timeframes});
			this.m_ts_timeframes.Location = new System.Drawing.Point(65, 0);
			this.m_ts_timeframes.Name = "m_ts_timeframes";
			this.m_ts_timeframes.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.m_ts_timeframes.Size = new System.Drawing.Size(48, 27);
			this.m_ts_timeframes.TabIndex = 0;
			// 
			// m_splitbtn_timeframes
			// 
			this.m_splitbtn_timeframes.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_splitbtn_timeframes.Image = global::Tradee.Properties.Resources.clock;
			this.m_splitbtn_timeframes.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_splitbtn_timeframes.Name = "m_splitbtn_timeframes";
			this.m_splitbtn_timeframes.Size = new System.Drawing.Size(36, 24);
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
			this.m_tsc.ContentPanel.ResumeLayout(false);
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
