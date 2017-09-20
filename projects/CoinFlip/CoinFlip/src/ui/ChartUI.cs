using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.attrib;
using pr.common;
using pr.container;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;
using ToolStripComboBox = pr.gui.ToolStripComboBox;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace CoinFlip
{
	public class ChartUI :BaseUI
	{
		#region UI Elements
		private ToolStripContainer m_tsc;
		private ToolStripLabel m_lbl_pair;
		private ToolStripComboBox m_cb_pair;
		private ToolStripSeparator m_sep0;
		private ToolStrip m_ts;
		private ToolStripLabel m_lbl_time_frame;
		private ToolStripComboBox m_cb_time_frame;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripButton m_chk_show_positions;
		private ToolStripButton m_chk_show_depth;
		private ToolStrip m_ts_drawing;
		private ToolStripLabel m_lbl_drawing_tools;
		private ToolStripButton m_btn_horz_line;
		private ToolStripButton m_btn_vert_line;
		private ToolStripButton m_btn_trend_line;
		private ChartControl m_chart;
		#endregion

		public ChartUI(Model model)
			:base(model, "Chart")
		{
			InitializeComponent();

			// Create the chart control
			m_chart = new ChartControl(string.Empty, new ChartControl.RdrOptions());
			ChartSettings = model.Settings.ChartTemplate;

			// Graphics cache
			GfxCache = new Cache<Guid, View3d.Object>{ Capacity = 100 };
			CandleGfxCache = new Cache<int, CachedGfx>();

			// Create the collections of pairs/time frames for which chart data is available
			TimeFrames = new BindingSource<ETimeFrame>();
			Pairs = new BindingSource<TradePair>();

			// Chart indicators
			Indicators = new BindingSource<Indicator> { DataSource = new BindingListEx<Indicator>(), PerItemClear = true };

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			TimeFrames = null;
			Pairs = null;
			ChartSettings = null;
			Indicators.Clear();
			Instrument = null;
			GfxAsk = null;
			GfxBid = null;
			GfxCache = null;
			CandleGfxCache = null;
			Util.Dispose(ref m_chart);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void SetModelCore(Model model)
		{
			if (Model != null)
			{
				Model.Pairs.ListChanging -= HandlePairsListChanging;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.Pairs.ListChanging += HandlePairsListChanging;
				HandlePairsListChanging();
			}
		}
		private void HandlePairsListChanging(object sender = null, ListChgEventArgs<TradePair> e = null)
		{
			// Update the available pairs
			Pairs.DataSource = Model.Pairs
				.Where(x => x.Exchange.ChartDataAvailable(x).Any())
				.ToArray();
		}

		/// <summary>Settings for this chart instance</summary>
		public Settings.ChartSettings ChartSettings { get; private set; }

		/// <summary>The tab text for the chart</summary>
		public string ChartTitle
		{
			get { return $"{Instrument?.SymbolCode ?? "Chart"},{TimeFrame}"; }
		}

		/// <summary>The pairs with chart data available</summary>
		private BindingSource<TradePair> Pairs
		{
			get { return m_pairs; }
			set
			{
				if (m_pairs == value) return;
				if (m_pairs != null)
				{
					m_pairs.PositionChanged -= HandleCurrentPairChanged;
				}
				m_pairs = value;
				if (m_pairs != null)
				{
					m_pairs.PositionChanged += HandleCurrentPairChanged;
				}
			}
		}
		private BindingSource<TradePair> m_pairs;
		private void HandleCurrentPairChanged(object sender = null, EventArgs e = null)
		{
			var pair = Pairs.Current;

			// Update the available time frames
			TimeFrames.DataSource = pair?.Exchange.ChartDataAvailable(pair).ToArray()
				?? new []{ ETimeFrame.None };

			// Select the data source
			SetInstrument(pair);
		}

		/// <summary>The available time frames for the current pair</summary>
		private BindingSource<ETimeFrame> TimeFrames
		{
			get { return m_time_frames; }
			set
			{
				if (m_time_frames == value) return;
				if (m_time_frames != null)
				{
					m_time_frames.PositionChanged -= HandleCurrentTimeFrameChanged;
				}
				m_time_frames = value;
				if (m_time_frames != null)
				{
					m_time_frames.PositionChanged += HandleCurrentTimeFrameChanged;
				}
			}
		}
		private BindingSource<ETimeFrame> m_time_frames;
		private void HandleCurrentTimeFrameChanged(object sender = null, PositionChgEventArgs e = null)
		{
			TimeFrame = TimeFrames.Current;
		}

		/// <summary>The currency pair displayed in the chart</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instrument; }
			private set
			{
				if (m_instrument == value) return;
				if (m_instrument != null)
				{
					m_instrument.DataChanged -= HandleDataChanged;
					Util.Dispose(ref m_instrument);
				}
				m_instrument = value;
				if (m_instrument != null)
				{
					m_instrument.DataChanged += HandleDataChanged;
				}
			}
		}
		private Instrument m_instrument;
		private void SetInstrument(TradePair pair)
		{
			// Clear the previous instrument and settings
			Instrument = null;
			ChartSettings = null;

			if (pair != null)
			{
				// Create the new instrument
				Instrument = new Instrument(Model, pair, TimeFrame, update_active:true);

				// Find the chart settings associated with this instrument
				ChartSettings = Model.Settings.Charts.FirstOrDefault(x => x.SymbolCode == pair.Name);
				if (ChartSettings == null)
				{
					ChartSettings = new Settings.ChartSettings(pair.Name);
					Model.Settings.Charts = Model.Settings.Charts.Concat(ChartSettings).ToArray();
				}
				ChartSettings.Inherit = Model.Settings.ChartTemplate;
				m_chart.Options = ChartSettings.Style;

				// Create indicators
				UpdateIndicators();
			}
		}
		private void HandleDataChanged(object sender, DataEventArgs e)
		{
			// Handle instrument data changing
			// If the event is specific to one candle, invalidate the graphics object
			// that contains the representation of that candle.
			if (e.Candle != null)
			{
				// Invalidate the cache value that contains 'idx'
				var idx = Instrument.IndexAt(new TimeFrameTime(e.Candle.Timestamp, TimeFrame));
				var cache_idx = idx / GfxModelBatchSize;
				CandleGfxCache.Invalidate(cache_idx);
			}
			else
			{
				// Otherwise, just invalidate the whole cache
				CandleGfxCache.Flush();
			}

			// Auto scroll if 'e.Candle' is a new candle
			if (e.NewCandle)
			{
				// If the latest candle is visible, move the X-Axis range forward.
				var latest_idx = (double)Instrument.Count;
				if (latest_idx.Within(m_chart.XAxis.Min, m_chart.XAxis.Max))
					m_chart.XAxis.Set(m_chart.XAxis.Min + 1, m_chart.XAxis.Max + 1);
				
				// Otherwise, scroll the YAxis to ensure the latest price is visible
				//else
				//	EnsureLatestPriceDisplayed();
			}

			// Signal a refresh
			m_chart.Invalidate();
		}

		/// <summary>The collection of indicators on this chart</summary>
		public BindingSource<Indicator> Indicators
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
		private BindingSource<Indicator> m_indicators;
		private void HandleIndicatorsListChanging(object sender, ListChgEventArgs<Indicator> args)
		{
			// Handle indicators being added/removed from the chart
			switch (args.ChangeType)
			{
			case ListChg.ItemAdded:
				{
					// Ensure the 'Chart' property is set on the indicator
					args.Item.Instrument = Instrument;
					args.Item.Chart = m_chart;

				//	// When the indicator changes, update the saved chart settings
				//	args.Item.DataChanged += HandleIndicatorChanged;

					// Update the settings.
					HandleIndicatorChanged();

					// Refresh the chart
					Invalidate(true);
					break;
				}
			case ListChg.ItemRemoved:
				{
					// Remove from the Chart
					args.Item.Chart = null;
					args.Item.Instrument = null;

				//	// Ignore changes
				//	args.Item.DataChanged -= HandleIndicatorChanged;

					// Dispose the indicator
					Util.Dispose(args.Item);

					// Update the settings.
					HandleIndicatorChanged();

					// Refresh the chart
					Invalidate(true);
					break;
				}
			}
		}
		private void HandleIndicatorChanged(object sender = null, EventArgs e = null)
		{
			// ChartSettings is set to null on Dispose so that removing indicators
			// doesn't write to the settings.
			if (ChartSettings == null)
				return;

			// Save the indicators to the chart settings
			var elements = new XElement(nameof(Indicators));
			foreach (var indicator in Indicators)
				elements.Add2(nameof(Indicator), indicator, true);

			// Update the settings
			ChartSettings.Indicators = elements;
		}

		/// <summary>The selected time frame</summary>
		public ETimeFrame TimeFrame
		{
			[DebuggerStepThrough] get { return Instrument?.TimeFrame ?? ETimeFrame.None; }
			set
			{
				if (TimeFrame == value) return;
				if (Instrument == null) return;
				Instrument.TimeFrame = value;
				DockControl.TabText = ChartTitle;
				CandleGfxCache.Flush();
				m_chart.AutoRange();
			}
		}

		/// <summary>Enable\Disable chart updating</summary>
		public bool UpdateThreadActive
		{
			get { return Instrument?.UpdateThreadActive ?? false;  }
			set { if (Instrument != null) Instrument.UpdateThreadActive = value; }
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Tool bar
			
			// Combo for choosing the pair to display
			m_cb_pair.ToolTipText = "Select the currency pair to display";
			m_cb_pair.ComboBox.DisplayProperty = nameof(TradePair.NameWithExchange);
			m_cb_pair.ComboBox.DataSource = Pairs;

			// Combo for choosing the time frame
			m_cb_time_frame.ToolTipText = "Select the time frame to display";
			m_cb_time_frame.ComboBox.DataSource = TimeFrames;

			// Show positions
			m_chk_show_positions.ToolTipText = "Show/Hide current trades";
			m_chk_show_positions.Checked = ChartSettings.ShowPositions;
			m_chk_show_positions.CheckedChanged += (s,a) =>
			{
				ChartSettings.ShowPositions = m_chk_show_positions.Checked;
				m_chart.Invalidate();
			};
			#endregion

			#region Chart
			m_chart.Name                     = "m_chart";
			m_chart.BorderStyle              = BorderStyle.FixedSingle;
			m_chart.Dock                     = DockStyle.Fill;
			m_chart.DefaultMouseControl      = true;
			m_chart.DefaultKeyboardShortcuts = true;

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
			m_chart.MouseDown           += HandleChartMouseDown;
			m_chart.AddOverlaysOnPaint  += HandleAddOverlaysOnPaint;
			m_chart.AddUserMenuOptions  += HandleChartCMenu;
			m_chart.AutoRanging         += HandleAutoRanging;

			m_chart.OptionsChanged += (s,a) =>
			{
				// Have to manually invoke save because the reference 'm_chart.Options'
				// doesn't change, only the properties within it.
				ChartSettings.Style = m_chart.Options;
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

			#region Chart Graphics
			GfxAsk = new View3d.Object($"*Line Ask {ChartSettings.AskColour.ToArgbU():X8} {{ 0 0 0 1 0 0 }}", file:false);
			GfxBid = new View3d.Object($"*Line Ask {ChartSettings.BidColour.ToArgbU():X8} {{ 0 0 0 1 0 0 }}", file:false);
			#endregion
		}

		/// <summary>Get all positions on this instruction</summary>
		private IEnumerable<Position> AllPositions
		{
			get { return Instrument?.AllPositions ?? new Position[0]; }
		}

		/// <summary>(Re)create indicators from settings</summary>
		private void UpdateIndicators()
		{
			Indicators.Clear();
			if (ChartSettings.Indicators != null)
				foreach (var node in ChartSettings.Indicators.Elements(nameof(Indicator)))
					Indicators.Add((Indicator)node.ToObject());
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

			const string long_fmt  = "HH:mm'\r\n'ddd dd-MMM-yy";
			const string short_fmt = "HH:mm";

			// The range of indices
			var first = 0;
			var last  = Instrument.Count;

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

		/// <summary>Handle chart mouse down</summary>
		private void HandleChartMouseDown(object sender, MouseEventArgs args)
		{
			// Edit chart elements when Control is held down
			if (ModifierKeys == Keys.Control)
			{
			//	// Look for hit drag-able indicators
			//	var hit = m_chart.HitTestCS(args.Location, ModifierKeys, x => (x as IndicatorBase)?.Dragable ?? false);
			//	if (hit.Hits.Count != 0)
			//	{
			//		var ind = (IndicatorBase)hit.Hits[0].Element;
			//
			//		// Create a mouse operation for dragging the indicator
			//		var op = ind.CreateDragMouseOp();
			//		m_chart.MouseOperations.SetPending(MouseButtons.Left, op);
			//	}
			}
		}

		/// <summary>Called when the chart paints</summary>
		private void HandleAddOverlaysOnPaint(object sender, ChartControl.AddOverlaysOnPaintEventArgs e)
		{
			 if (Instrument == null || TimeFrame == ETimeFrame.None)
				return;

			// Add the ask/bid price to the Y axis
			using (var bsh = new SolidBrush(ChartSettings.AskColour))
			{
				var price     = Instrument.Q2BPrice;
				var price_str = e.Chart.YAxis.TickText(price, 0.0);
				var pt        = e.Chart.ChartToClient(new PointF((float)e.Chart.XAxis.Min, (float)price));
				var sz        = e.Gfx.MeasureString(price_str, e.Chart.YAxis.Options.TickFont);
				var box       = sz.Scaled(1.05f);
				e.Gfx.FillRectangle(bsh, new RectangleF(pt.X - box.Width, pt.Y - box.Height/2, box.Width, box.Height));
				e.Gfx.DrawString(price_str, e.Chart.YAxis.Options.TickFont, Brushes.White, pt.X - sz.Width, pt.Y - sz.Height/2);
			}
			using (var bsh = new SolidBrush(ChartSettings.BidColour))
			{
				var price     = Instrument.B2QPrice;
				var price_str = e.Chart.YAxis.TickText(price, 0.0);
				var pt        = e.Chart.ChartToClient(new PointF((float)e.Chart.XAxis.Min, (float)price));
				var sz        = e.Gfx.MeasureString(price_str, e.Chart.YAxis.Options.TickFont);
				var box       = sz.Scaled(1.05f);
				e.Gfx.FillRectangle(bsh, new RectangleF(pt.X - box.Width, pt.Y - box.Height/2, box.Width, box.Height));
				e.Gfx.DrawString(price_str, e.Chart.YAxis.Options.TickFont, Brushes.White, pt.X - sz.Width, pt.Y - sz.Height/2);
			}

			// This needs to be done on a textured quad and positioned in the 3d scene
			// Draw the time remaining on the latest candle
			if (e.Chart.XAxis.Range.Contains(Instrument.Count - 1))
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
							{
								// MA
								var opt = add_menu.DropDownItems.Add2(new ToolStripMenuItem("Moving Average"));
								opt.Click += (s,a) => EditMaIndicator();
							}
							{
								//// S&R Level
								//var opt = add_menu.DropDownItems.Add2(new ToolStripMenuItem("Support/Resistance Level"));
								//opt.Click += (s,a) =>
								//{
								//	// SnR levels are an Instrument thing. Add the SnRLevel to the instrument.
								//	// We'll notice it in a different handler and add an indicator for it there.
								//	var snr = new SnRLevel(e.HitResult.ChartPoint.Y, m_chart.YAxis.Span * 0.05 / Instrument.PriceData.PipSize);
								//	Instrument.SupportResistLevels.Add(snr);
								//	EditSnRLevel(Indicators.OfType<SnRIndicator>().First(x => x.Id == snr.Id));
								//};
							}
							{
								//// S&R
								//var opt = add_menu.DropDownItems.Add2(new ToolStripMenuItem("Support and Resistance"));
								//opt.Click += (s,a) => EditSnRIndicator();
							}
							{
								//// Trend Strength
								//var opt = add_menu.DropDownItems.Add2(new ToolStripMenuItem("Trend Strength"));
								//opt.Click += (s,a) => EditTrendStrengthIndicator();
							}
						}

						indicators_menu.DropDownItems.AddSeparator();

						// Modify existing indicators
						var hit_indicators = e.HitResult.Hits.Where(x => x.Element is Indicator).Select(x => (Indicator)x.Element).ToArray();
						foreach (var indicator in Indicators)
						{
							var is_hit = hit_indicators.Contains(indicator);
							var colour = is_hit ? Color.Blue : Color.Black;
							var font = is_hit ? new Font(e.Menu.Font, FontStyle.Bold) : e.Menu.Font;
							var opt = indicators_menu.DropDownItems.Add2(new ToolStripMenuItem(indicator.Name) { ForeColor = colour, Font = font });
							opt.Click += (s,a) => EditIndicator(indicator);
						}
					}
					#endregion

					e.Menu.Items.InsertSeparator(idx++);

					#region Orders
					{
						// The price where the mouse was clicked
						var click_price = e.HitResult.ChartPoint.Y;
						Action<ETradeType> PlaceNewOrder = (tt) =>
						{
					//		Model.Positions.Orders.Add2(new Order(0, Instrument, tt, Trade.EState.Visualising)
					//		{
					//			EntryPrice = click_price,
					//			StopLossRel = m_chart.YAxis.Span * 0.1,
					//			TakeProfitRel = m_chart.YAxis.Span * 0.1,
					//		});
						};

						{// Buy
							var opt = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem("Buy at {0}".Fmt(click_price)) { ForeColor = ChartSettings.AskColour });
							opt.Click += (s,a) => PlaceNewOrder(ETradeType.B2Q);
						}
						{// Sell
							var opt = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem("Sell at {0}".Fmt(click_price)) { ForeColor = ChartSettings.BidColour });
							opt.Click += (s,a) => PlaceNewOrder(ETradeType.Q2B);
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

		/// <summary>Handle the auto range command of the chart</summary>
		private void HandleAutoRanging(object sender, ChartControl.AutoRangeEventArgs e)
		{
			if (Instrument == null || TimeFrame == ETimeFrame.None)
				return;

			// Display the last few candles
			var bb = BBox.Reset;
			var idx_min = Instrument.Count - 100;
			var idx_max = Instrument.Count + 20;
			foreach (var candle in Instrument.CandleRange(idx_min, idx_max))
			{
				bb = BBox.Encompass(bb, new v4(idx_min, (float)candle.Low , -Z.Max, 1f));
				bb = BBox.Encompass(bb, new v4(idx_max, (float)candle.High, +Z.Max, 1f));
			}

			// Include trades
			if (ChartSettings.ShowPositions)
			{
				foreach (var pos in AllPositions)
					bb = BBox.Encompass(bb, new v4(bb.Centre.x, (float)(decimal)pos.Price, Z.Trades, 1f));
			}

			if (bb.IsValid)
			{
				// Swell the box a little for margins
				bb.Radius = new v4(bb.Radius.x, bb.Radius.y * 1.1f, bb.Radius.z, 0f);
				e.ViewBBox = bb;
				e.Handled = true;
			}
		}

		/// <summary>Handle the chart about to render</summary>
		private void HandleChartRendering(object sender, ChartControl.ChartRenderingEventArgs args)
		{
			if (Instrument == null || TimeFrame == ETimeFrame.None)
				return;

			#region Candles
			{
				// Convert the XAxis values into an index range.
				// (indices, not time frame units, because of the gaps in the price data).
				var range = Instrument.IndexRange((int)(m_chart.XAxis.Min - 1), (int)(m_chart.XAxis.Max + 1));

				// Convert the index range into a cache index range
				var cache0 = range.Begi / GfxModelBatchSize;
				var cache1 = range.Endi / GfxModelBatchSize;
				for (int i = cache0; i <= cache1; ++i)
				{
					// Get the graphics model that contains candle 'i'
					var gfx = GfxAt(i);
					if (gfx.Gfx != null)
					{
						// Position the graphics object
						gfx.Gfx.O2P = m4x4.Translation(new v4(gfx.DBIndexRange.Begi, 0.0f, Z.Candles, 1.0f));
						args.AddToScene(gfx.Gfx);
					}
				}
			}
			#endregion

			#region Price Data
			{
				// Add the ask and bid lines
				if (GfxAsk != null)
				{
					var price = (float)Instrument.Q2BPrice;
					GfxAsk.O2P = m4x4.Scale((float)m_chart.XAxis.Span, 1f, 1f, new v4((float)m_chart.XAxis.Min, price, Z.CurrentPrice, 1f));
					args.AddToScene(GfxAsk);
				}
				if (GfxBid != null)
				{
					var price = (float)Instrument.B2QPrice;
					GfxBid.O2P = m4x4.Scale((float)m_chart.XAxis.Span, 1f, 1f, new v4((float)m_chart.XAxis.Min, price, Z.CurrentPrice, 1f));
					args.AddToScene(GfxBid);
				}
			}
			#endregion

			#region Trades
			if (ChartSettings.ShowPositions)
			{
				// Get all positions on this instruction
				foreach (var pos in AllPositions)
				{
					var gfx = GfxCache.Get(pos.UniqueKey, g => new View3d.Object($"*Line Position_{pos.OrderId} FF8080FF {{ 0 0 0 1 0 0 }}", file:false));
					var x = Instrument.IndexAt(new TimeFrameTime(pos.Created.Value, TimeFrame));
					var y = (float)(decimal)pos.Price;
					gfx.O2P = m4x4.Scale(Instrument.Count + 10 - x, 1f, 1f, new v4(x, y, Z.Trades, 1f));
					args.AddToScene(gfx);
				}
			}
			#endregion
		}

		#region Indicators

		/// <summary>Edit an existing indicator</summary>
		private void EditIndicator(object indicator)
		{
			if (indicator is IndicatorMA indy) { EditMaIndicator(indy); return; }
			//if (indicator is SnRIndicator) EditSnRLevel    ((SnRIndicator)indicator);
			//if (indicator is SnR         ) EditSnRIndicator((SnR         )indicator);
			throw new Exception($"Unknown indicator type: {indicator.GetType().Name}");
		}

		/// <summary>Add/Edit a moving average indicator</summary>
		private void EditMaIndicator(IndicatorMA ma = null)
		{
			ma = ma ?? Indicators.Add2(new IndicatorMA());
			using (var dlg = new EditMaUI(this, ma))
				dlg.ShowDialog(this);
		}

		#endregion

		#region Gfx

		/// <summary>A cache of graphics models</summary>
		private Cache<Guid, View3d.Object> GfxCache
		{
			get { return m_gfx_cache; }
			set
			{
				if (m_gfx_cache == value) return;
				Util.Dispose(ref m_gfx_cache);
				m_gfx_cache = value;
			}
		}
		private Cache<Guid, View3d.Object> m_gfx_cache;

		/// <summary>Graphics for the asking price line</summary>
		private View3d.Object GfxAsk
		{
			get { return m_gfx_ask; }
			set
			{
				if (m_gfx_ask == value) return;
				Util.Dispose(ref m_gfx_ask);
				m_gfx_ask = value;
			}
		}
		private View3d.Object m_gfx_ask;

		/// <summary>Graphics for the asking price line</summary>
		private View3d.Object GfxBid
		{
			get { return m_gfx_bid; }
			set
			{
				if (m_gfx_bid == value) return;
				Util.Dispose(ref m_gfx_bid);
				m_gfx_bid = value;
			}
		}
		private View3d.Object m_gfx_bid;

		/// <summary>A cache of the candle graphics</summary>
		private Cache<int, CachedGfx> CandleGfxCache
		{
			get { return m_gfx_candes_cache; }
			set
			{
				if (m_gfx_candes_cache == value) return;
				if (m_gfx_candes_cache != null)
				{
					m_vbuf = null;
					m_ibuf = null;
					m_nbuf = null;
					Util.Dispose(ref m_gfx_candes_cache);
				}
				m_gfx_candes_cache = value;
				if (m_gfx_candes_cache != null)
				{
					m_vbuf = new List<View3d.Vertex>();
					m_ibuf = new List<ushort>();
					m_nbuf = new List<View3d.Nugget>();
				}
			}
		}
		private Cache<int, CachedGfx> m_gfx_candes_cache;
		private const int GfxModelBatchSize = 1024;
		private class CachedGfx :IDisposable
		{
			public CachedGfx(View3d.Object gfx, Range db_index_range)
			{
				Gfx = gfx;
				DBIndexRange = db_index_range;
			}
			public void Dispose()
			{
				Gfx = Util.Dispose(Gfx);
				DBIndexRange = Range.Zero;
			}

			/// <summary>The graphics object containing 'GfxModelBatchSize' candles</summary>
			public View3d.Object Gfx { get; private set; }

			/// <summary>The index range of candles in this graphic</summary>
			public Range DBIndexRange { get; private set; }
		}

		/// <summary>Buffers for creating the chart graphics</summary>
		private List<View3d.Vertex> m_vbuf;
		private List<ushort>        m_ibuf;
		private List<View3d.Nugget> m_nbuf;

		/// <summary>Returns the graphics model containing the data point with index 'cache_idx'</summary>
		private CachedGfx GfxAt(int cache_idx)
		{
			// On miss, generate the graphics model for the data range [idx, idx + min(GfxModelBatchSize, Count-idx))
			return CandleGfxCache.Get(cache_idx, i =>
			{
				var db_idx_range = new Range((i+0) * GfxModelBatchSize, (i+1) * GfxModelBatchSize);

				// Get the series data over the time range specified
				var rng = Instrument.IndexRange(db_idx_range.Begi, db_idx_range.Endi);
				if (rng.Counti == 0)
					return new CachedGfx(null, rng);

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
					var col = candle.Bullish ? ChartSettings.BullishColour.ToArgbU() : candle.Bearish ? ChartSettings.BearishColour.ToArgbU() : 0xFFA0A0A0;
					var v = vert;

					// Prevent degenerate triangles
					if (o == c)
					{
						o += m_chart.ClientToChart(new SizeF(0, 0.25f)).Height;
						c -= m_chart.ClientToChart(new SizeF(0, 0.25f)).Height;
					}

					// Candle verts
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , h, 0f, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , o, 0f, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x - 0.4f , o, 0f, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x + 0.4f , o, 0f, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x - 0.4f , c, 0f, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x + 0.4f , c, 0f, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , c, 0f, 1f), col);
					m_vbuf[vert++] = new View3d.Vertex(new v4(x        , l, 0f, 1f), col);

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
				var gfx = new View3d.Object($"Candles-[{db_idx_range.Begi},{db_idx_range.Endi})", 0xFFFFFFFF, vert, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());
				return new CachedGfx(gfx, db_idx_range);
			});
		}

		/// <summary>Z-values for chart elements</summary>
		public static ZOrder Z = new ZOrder();
		public class ZOrder
		{
			// Grid lines are drawn at 0
			public float Min          = 0f;
			public float Candles      = 0.010f;
			public float CurrentPrice = 0.011f;
			public float Indicators   = 0.012f;
			public float Trades       = 0.013f;
			public float Max          = 0.1f;
		}

		#endregion

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChartUI));
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_lbl_pair = new System.Windows.Forms.ToolStripLabel();
			this.m_cb_pair = new pr.gui.ToolStripComboBox();
			this.m_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_lbl_time_frame = new System.Windows.Forms.ToolStripLabel();
			this.m_cb_time_frame = new pr.gui.ToolStripComboBox();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_chk_show_positions = new System.Windows.Forms.ToolStripButton();
			this.m_chk_show_depth = new System.Windows.Forms.ToolStripButton();
			this.m_ts_drawing = new System.Windows.Forms.ToolStrip();
			this.m_btn_horz_line = new System.Windows.Forms.ToolStripButton();
			this.m_btn_vert_line = new System.Windows.Forms.ToolStripButton();
			this.m_btn_trend_line = new System.Windows.Forms.ToolStripButton();
			this.m_lbl_drawing_tools = new System.Windows.Forms.ToolStripLabel();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ts.SuspendLayout();
			this.m_ts_drawing.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(766, 711);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(766, 737);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "m_tsc";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts_drawing);
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lbl_pair,
            this.m_cb_pair,
            this.m_sep0,
            this.m_lbl_time_frame,
            this.m_cb_time_frame,
            this.toolStripSeparator1,
            this.m_chk_show_positions,
            this.m_chk_show_depth});
			this.m_ts.Location = new System.Drawing.Point(3, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(364, 26);
			this.m_ts.TabIndex = 0;
			// 
			// m_lbl_pair
			// 
			this.m_lbl_pair.Name = "m_lbl_pair";
			this.m_lbl_pair.Size = new System.Drawing.Size(30, 23);
			this.m_lbl_pair.Text = "Pair:";
			// 
			// m_cb_pair
			// 
			this.m_cb_pair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_pair.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.SelectedIndex = -1;
			this.m_cb_pair.SelectedItem = null;
			this.m_cb_pair.SelectedText = "";
			this.m_cb_pair.Size = new System.Drawing.Size(121, 23);
			// 
			// m_sep0
			// 
			this.m_sep0.Name = "m_sep0";
			this.m_sep0.Size = new System.Drawing.Size(6, 26);
			// 
			// m_lbl_time_frame
			// 
			this.m_lbl_time_frame.Name = "m_lbl_time_frame";
			this.m_lbl_time_frame.Size = new System.Drawing.Size(73, 23);
			this.m_lbl_time_frame.Text = "Time Frame:";
			// 
			// m_cb_time_frame
			// 
			this.m_cb_time_frame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_time_frame.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
			this.m_cb_time_frame.Name = "m_cb_time_frame";
			this.m_cb_time_frame.SelectedIndex = -1;
			this.m_cb_time_frame.SelectedItem = null;
			this.m_cb_time_frame.SelectedText = "";
			this.m_cb_time_frame.Size = new System.Drawing.Size(70, 23);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 26);
			// 
			// m_chk_show_positions
			// 
			this.m_chk_show_positions.CheckOnClick = true;
			this.m_chk_show_positions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_chk_show_positions.Image = ((System.Drawing.Image)(resources.GetObject("m_chk_show_positions.Image")));
			this.m_chk_show_positions.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_show_positions.Name = "m_chk_show_positions";
			this.m_chk_show_positions.Size = new System.Drawing.Size(23, 23);
			this.m_chk_show_positions.Text = "Show Positions";
			// 
			// m_chk_show_depth
			// 
			this.m_chk_show_depth.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_chk_show_depth.Image = ((System.Drawing.Image)(resources.GetObject("m_chk_show_depth.Image")));
			this.m_chk_show_depth.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_show_depth.Name = "m_chk_show_depth";
			this.m_chk_show_depth.Size = new System.Drawing.Size(23, 23);
			this.m_chk_show_depth.Text = "Show Market Depth";
			// 
			// m_ts_drawing
			// 
			this.m_ts_drawing.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts_drawing.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lbl_drawing_tools,
            this.m_btn_horz_line,
            this.m_btn_vert_line,
            this.m_btn_trend_line});
			this.m_ts_drawing.Location = new System.Drawing.Point(373, 0);
			this.m_ts_drawing.Name = "m_ts_drawing";
			this.m_ts_drawing.Size = new System.Drawing.Size(118, 25);
			this.m_ts_drawing.TabIndex = 1;
			// 
			// m_btn_horz_line
			// 
			this.m_btn_horz_line.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_horz_line.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_horz_line.Image")));
			this.m_btn_horz_line.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_horz_line.Name = "m_btn_horz_line";
			this.m_btn_horz_line.Size = new System.Drawing.Size(23, 22);
			this.m_btn_horz_line.Text = "Horizontal Line";
			// 
			// m_btn_vert_line
			// 
			this.m_btn_vert_line.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_vert_line.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_vert_line.Image")));
			this.m_btn_vert_line.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_vert_line.Name = "m_btn_vert_line";
			this.m_btn_vert_line.Size = new System.Drawing.Size(23, 22);
			this.m_btn_vert_line.Text = "Vertical Line";
			// 
			// m_btn_trend_line
			// 
			this.m_btn_trend_line.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_trend_line.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_trend_line.Image")));
			this.m_btn_trend_line.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_trend_line.Name = "m_btn_trend_line";
			this.m_btn_trend_line.Size = new System.Drawing.Size(23, 22);
			this.m_btn_trend_line.Text = "Trend Line";
			// 
			// m_lbl_drawing_tools
			// 
			this.m_lbl_drawing_tools.Name = "m_lbl_drawing_tools";
			this.m_lbl_drawing_tools.Size = new System.Drawing.Size(37, 22);
			this.m_lbl_drawing_tools.Text = "Draw:";
			// 
			// ChartUI
			// 
			this.Controls.Add(this.m_tsc);
			this.Name = "ChartUI";
			this.Size = new System.Drawing.Size(766, 737);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.m_ts_drawing.ResumeLayout(false);
			this.m_ts_drawing.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
