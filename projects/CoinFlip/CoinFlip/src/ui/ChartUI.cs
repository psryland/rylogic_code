using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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
		#endregion

		public ChartUI(Model model)
			:base(model, "Chart")
		{
			try
			{
				InitializeComponent();

				// Create the chart control
				ChartCtrl = new ChartControl(string.Empty, new ChartControl.RdrOptions());
				ChartSettings = model.Settings.ChartTemplate;

				// Graphics
				GfxCandles = new GfxObjectCandles(this);
				GfxMarketDepth = new GfxObjectMarketDepth(this);
				GfxPositions = new GfxObjectPositions(this);

				// Chart indicators
				Indicators = new BindingSource<Indicator> { DataSource = new BindingListEx<Indicator>(), PerItem = true };

				SetupUI();
			}
			catch (Exception)
			{
				Dispose();
				throw;
			}
		}
		protected override void Dispose(bool disposing)
		{
			ChartSettings = null;
			Indicators.Clear();
			Instrument = null;
			GfxAsk = null;
			GfxBid = null;
			GfxPositions = null;
			GfxMarketDepth = null;
			GfxCandles = null;
			ChartCtrl = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void SetModelCore(Model model)
		{
			if (Model != null)
			{
				Model.Pairs.ListChanging -= HandleModelPairsListChanging;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.Pairs.ListChanging += HandleModelPairsListChanging;
				HandleModelPairsListChanging();
			}

			// Handlers
			void HandleModelPairsListChanging(object sender = null, ListChgEventArgs<TradePair> e = null)
			{
				if (e == null || e.IsDataChanged)
					UpdateAvailablePairs();
			}
		}

		/// <summary>Settings for this chart instance</summary>
		public Settings.ChartSettings ChartSettings
		{
			get { return m_chart_settings; }
			private set
			{
				if (m_chart_settings == value) return;
				m_chart_settings = value;
				if (m_chart_settings != null)
				{
					// Apply new settings
					m_chk_show_positions.Checked = m_chart_settings.ShowPositions;
					m_chk_show_depth.Checked = m_chart_settings.ShowMarketDepth;
				}
			}
		}
		private Settings.ChartSettings m_chart_settings;

		/// <summary>The tab text for the chart</summary>
		public string ChartTitle
		{
			get { return $"{Instrument?.SymbolCode ?? "Chart"},{Instrument?.TimeFrame.ToString() ?? string.Empty}"; }
		}

		/// <summary>The ChartControl</summary>
		public ChartControl ChartCtrl
		{
			get { return m_chart_ctrl; }
			set
			{
				if (m_chart_ctrl == value) return;
				Util.Dispose(ref m_chart_ctrl);
				m_chart_ctrl = value;
			}
		}
		private ChartControl m_chart_ctrl;

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

				// Handlers
				void HandleDataChanged(object sender, DataEventArgs e)
				{
					// If the event is specific to one candle, invalidate the graphics object
					// that contains the representation of that candle.
					if (e.Candle != null)
					{
						// Invalidate the cache value that contains 'idx'
						var idx = Instrument.IndexAt(new TimeFrameTime(e.Candle.Timestamp, Instrument.TimeFrame));
						GfxCandles.Invalidate(idx);
					}
					else
					{
						// Otherwise, just invalidate the range of candles that have changed
						GfxCandles.Invalidate(e.IndexRange);
					}

					// Auto scroll if 'e.Candle' is a new candle
					if (e.CandleType == DataEventArgs.ECandleType.New)
					{
						// If the latest candle is visible, move the X-Axis range forward.
						var latest_idx = (double)Instrument.Count;
						if (latest_idx.Within(ChartCtrl.XAxis.Min, ChartCtrl.XAxis.Max))
							ChartCtrl.XAxis.Set(ChartCtrl.XAxis.Min + 1, ChartCtrl.XAxis.Max + 1);
					}

					// Invalidate other instrument data dependent graphics
					GfxMarketDepth.Invalidate();

					// Signal a refresh
					ChartCtrl.Invalidate();
				}
			}
		}
		private Instrument m_instrument;

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

				// Handlers
				void HandleIndicatorsListChanging(object sender, ListChgEventArgs<Indicator> args)
				{
					// Handle indicators being added/removed from the chart
					switch (args.ChangeType)
					{
					case ListChg.ItemAdded:
						{
							// Ensure the 'Chart' property is set on the indicator
							args.Item.Instrument = Instrument;
							args.Item.Chart = ChartCtrl;

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
				void HandleIndicatorChanged(object sender = null, EventArgs e = null)
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
			}
		}
		private BindingSource<Indicator> m_indicators;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Instrument Tool bar
			
			// Combo for choosing the pair to display
			UpdateAvailablePairs();
			m_cb_pair.ToolTipText = "Select the currency pair to display";
			m_cb_pair.ComboBox.DisplayProperty = nameof(TradePair.NameWithExchange);
			m_cb_pair.ComboBox.DropDown += (s,a) =>
			{
				UpdateAvailablePairs();
			};
			m_cb_pair.ComboBox.SelectedIndexChanged += (s,a) =>
			{
				if (IsChartLocked || m_in_update_available_pairs != 0) return;
				UpdateAvailableTimeFrames();
				var tp = (TradePair)m_cb_pair.ComboBox.SelectedItem;
				var tf = (ETimeFrame)m_cb_time_frame.ComboBox.SelectedItem;
				SetInstrument(tp, tf);
			};

			// Combo for choosing the time frame
			UpdateAvailableTimeFrames();
			m_cb_time_frame.ToolTipText = "Select the time frame to display";
			m_cb_time_frame.SelectedItem = ETimeFrame.None;
			m_cb_time_frame.ComboBox.DropDown += (s,a) =>
			{
				UpdateAvailableTimeFrames();
			};
			m_cb_time_frame.ComboBox.SelectedIndexChanged += (s,a) =>
			{
				if (IsChartLocked || m_in_update_time_frames != 0) return;
				var tp = (TradePair)m_cb_pair.ComboBox.SelectedItem;
				var tf = (ETimeFrame)m_cb_time_frame.ComboBox.SelectedItem;
				SetInstrument(tp, tf);
			};

			// Show positions
			m_chk_show_positions.ToolTipText = "Show/Hide current trades";
			m_chk_show_positions.Checked = ChartSettings.ShowPositions;
			m_chk_show_positions.CheckedChanged += (s,a) =>
			{
				ChartSettings.ShowPositions = m_chk_show_positions.Checked;
				ChartCtrl.Invalidate();
			};

			// Show market depth
			m_chk_show_depth.ToolTipText = "Show/Hide market depth";
			m_chk_show_depth.Checked = ChartSettings.ShowMarketDepth;
			m_chk_show_depth.CheckedChanged += (s,a) =>
			{
				ChartSettings.ShowMarketDepth = m_chk_show_depth.Checked;
				ChartCtrl.Invalidate();
			};

			#endregion

			#region Drawing Tools Tool bar

			m_btn_horz_line.ToolTipText = "Add a Support/Resistance line";
			m_btn_horz_line.CheckOnClick = false;
			m_btn_horz_line.Click += (s,a) =>
			{
				m_btn_horz_line.Checked = true;
				ChartCtrl.MouseOperations.SetPending(MouseButtons.Left,
					new DropNewIndicatorOp(this,
						() => new IndicatorHorzLine{ Price = (decimal)ChartCtrl.YAxis.Centre },
						() => m_btn_horz_line.Checked = false));
			};

			#endregion

			#region Chart
			ChartCtrl.Name                     = "m_chart";
			ChartCtrl.BorderStyle              = BorderStyle.FixedSingle;
			ChartCtrl.Dock                     = DockStyle.Fill;
			ChartCtrl.DefaultMouseControl      = true;
			ChartCtrl.DefaultKeyboardShortcuts = true;

			ChartCtrl.XAxis.Options.ShowGridLines = true;
			ChartCtrl.XAxis.Options.PixelsPerTick = 50;
			ChartCtrl.XAxis.Options.TickFont = new Font("tahoma", 7f, FontStyle.Regular, GraphicsUnit.Point);
			ChartCtrl.XAxis.Options.MinTickSize = ChartCtrl.XAxis.Options.TickFont.Height * 2.2f;
			ChartCtrl.XAxis.TickText = HandleChartXAxisLabels;

			ChartCtrl.YAxis.Options.ShowGridLines = true;
			ChartCtrl.YAxis.Options.PixelsPerTick = 20;
			ChartCtrl.YAxis.Options.MinTickSize = 50;
			ChartCtrl.YAxis.Options.TickFont = new Font("tahoma", 7f, FontStyle.Regular, GraphicsUnit.Point);
			ChartCtrl.YAxis.TickText = HandleChartYAxisTickText;
			ChartCtrl.YAxis.MeasureTickText = HandleChartYAxisMeasureTickText;

			ChartCtrl.ChartMoved          += HandleChartMoved;
			ChartCtrl.ChartRendering      += HandleChartRendering;
			ChartCtrl.MouseDown           += HandleChartMouseDown;
			ChartCtrl.AddOverlaysOnPaint  += HandleAddOverlaysOnPaint;
			ChartCtrl.AddUserMenuOptions  += HandleChartCMenu;
			ChartCtrl.AutoRanging         += HandleAutoRanging;

			ChartCtrl.OptionsChanged += (s,a) =>
			{
				// Have to manually invoke save because the reference 'm_chart.Options'
				// doesn't change, only the properties within it.
				ChartSettings.Style = ChartCtrl.Options;
				Model.Settings.Save();
			};
			ChartCtrl.ChartAreaSelect += (s,a) =>
			{
				// Area select zoom if control is held down
				//todo var rect = new RectangleF(a.SelectionArea.MinX, a.SelectionArea.MinY, a.SelectionArea.SizeX, a.SelectionArea.SizeY);
				//todo if (rect.Width > 0 && rect.Height > 0 && ModifierKeys == Keys.Shift)
				//todo 	m_chart.ZoomArea(rect, false);

				a.Handled = true;
			};

			m_tsc.ContentPanel.Controls.Add(ChartCtrl);
			#endregion

			#region Chart Graphics
			GfxAsk = new View3d.Object($"*Line Ask {ChartSettings.AskColour.ToArgbU():X8} {{ 0 0 0 1 0 0 }}", file:false);
			GfxBid = new View3d.Object($"*Line Ask {ChartSettings.BidColour.ToArgbU():X8} {{ 0 0 0 1 0 0 }}", file:false);
			#endregion
		}

		/// <summary>Change the instrument displayed in this chart</summary>
		private void SetInstrument(TradePair pair, ETimeFrame time_frame)
		{
			Debug.Assert(!IsChartLocked);

			// Clear the previous instrument and settings
			Instrument = null;
			ChartSettings = null;

			if (pair != null && time_frame != ETimeFrame.None)
			{
				// Create the new instrument
				Instrument = new Instrument($"Chart {pair.Name} {time_frame}", Model.PriceData[pair, time_frame]);

				// Find the chart settings associated with this instrument
				ChartSettings = Model.ChartSettings(pair);
				ChartCtrl.Options = ChartSettings.Style;

				// Create indicators
				UpdateIndicators();

				// Update UI elements
				DockControl.TabText = ChartTitle;
				GfxCandles.Flush();
				ChartCtrl.AutoRange();
			}
		}

		/// <summary>The pairs with chart data available</summary>
		private IEnumerable<TradePair> AvailablePairs
		{
			get { return Model.Pairs.Where(x => x.Exchange.ChartDataAvailable(x).Any()); }
		}
		private void UpdateAvailablePairs()
		{
			using (m_cb_pair.ComboBox.PreserveSelectedItem())
			using (Scope.Create(() => ++m_in_update_available_pairs, () => --m_in_update_available_pairs))
			{
				m_cb_pair.ComboBox.Items.Merge(AvailablePairs);
				m_cb_pair.ComboBox.Items.Sort();
			}
		}
		private int m_in_update_available_pairs;

		/// <summary>The time frames available for the current instrument</summary>
		private IEnumerable<ETimeFrame> AvailableTimeFrames
		{
			get
			{
				var pair = m_cb_pair.SelectedItem as TradePair;
				var exch = pair?.Exchange;
				yield return ETimeFrame.None;
				if (exch == null) yield break;
				foreach (var tf in exch.ChartDataAvailable(pair))
					yield return tf;
			}
		}
		private void UpdateAvailableTimeFrames()
		{
			using (m_cb_time_frame.ComboBox.PreserveSelectedItem())
			using (Scope.Create(() => ++m_in_update_time_frames, () => --m_in_update_time_frames))
			{
				m_cb_time_frame.ComboBox.Items.Merge(AvailableTimeFrames);
				m_cb_time_frame.ComboBox.Items.Sort();
			}
			if (m_cb_time_frame.ComboBox.SelectedItem == null)
				m_cb_time_frame.ComboBox.SelectedItem = ETimeFrame.None;
		}
		private int m_in_update_time_frames;

		/// <summary>Get all positions on the instrument displayed in this chart</summary>
		private IEnumerable<Position> AllPositions
		{
			get
			{
				return Instrument != null
					? Model.AllPositions(Instrument.Pair.Name)
					: new Position[0];
			}
		}

		/// <summary>(Re)create indicators from settings</summary>
		private void UpdateIndicators()
		{
			Debug.Assert(!IsChartLocked, "Should not be called while the chart is locked");

			Indicators.Clear();
			if (ChartSettings.Indicators != null)
				foreach (var node in ChartSettings.Indicators.Elements(nameof(Indicator)))
					Indicators.Add((Indicator)node.ToObject());
		}

		/// <summary>Place a new order directly from the chart</summary>
		private void PlaceNewOrder(ETradeType tt)
		{
			// Add an order/trade indicator to the chart

			// Display a UI with the properties of the order (non-modal)

			// Allow the order to be dragged up and down on the chart

			//		Model.Positions.Orders.Add2(new Order(0, Instrument, tt, Trade.EState.Visualising)
			//		{
			//			EntryPrice = click_price,
			//			StopLossRel = m_chart.YAxis.Span * 0.1,
			//			TakeProfitRel = m_chart.YAxis.Span * 0.1,
			//		});
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
			if (m_xaxis_label_mode == EXAxisLabelMode.CandleIndex)
				return x.ToString();

			if (Instrument == null)
				return string.Empty;

			// The range of indices
			var first = 0;
			var last  = Instrument.Count;

			var prev = (int)(x - step);
			var curr = (int)(x);
			if (curr < first || curr >= last)
				return string.Empty;

			// Get the time stamp of the candle at the tick mark in local time
			var dt_curr = new DateTimeOffset(Instrument[curr].Timestamp, TimeSpan.Zero).DateTime;
			if (m_xaxis_label_mode == EXAxisLabelMode.LocalTime) dt_curr = TimeZone.CurrentTimeZone.ToLocalTime(dt_curr);
			if (curr == first || prev < first || x - step < ChartCtrl.XAxis.Min) // First tick on the x axis
				return dt_curr.ToString("HH:mm'\r\n'dd-MMM-yy");

			// If the current tick mark represents the same candle as the previous one, no text is required
			if (prev == curr)
				return string.Empty;

			// Get the time stamp of the candle at the previous tick mark in local time
			if (prev >= first && prev < last)
			{
				var dt_prev = new DateTimeOffset(Instrument[prev].Timestamp, TimeSpan.Zero).DateTime;
				if (m_xaxis_label_mode == EXAxisLabelMode.LocalTime) dt_prev = TimeZone.CurrentTimeZone.ToLocalTime(dt_prev);
				if (dt_curr.Year != dt_prev.Year)
					return dt_curr.ToString("HH:mm'\r\n'dd-MMM-yy");
				if (dt_curr.Month != dt_prev.Month)
					return dt_curr.ToString("HH:mm'\r\n'dd-MMM");
				if (dt_curr.Day != dt_prev.Day)
					return dt_curr.ToString("HH:mm'\r\n'ddd dd");
			}

			return dt_curr.ToString("HH:mm");
		}
		private enum EXAxisLabelMode { LocalTime, UtcTime, CandleIndex }
		private EXAxisLabelMode m_xaxis_label_mode;

		/// <summary>Convert the YAxis values into pretty price strings</summary>
		private string HandleChartYAxisTickText(double x, double step)
		{
			if (Instrument == null)
				return string.Empty;

			var str = new StringBuilder(Math.Round(x, 5, MidpointRounding.AwayFromZero).ToString("F5"));
			for (; str.Length > 6 && str[str.Length-1] == '0';) str.Length--;
			return str.ToString();
		}

		/// <summary>Measure the size of the y axis text</summary>
		private float HandleChartYAxisMeasureTickText(Graphics gfx, bool width)
		{
			var sz0 = gfx.MeasureString(HandleChartYAxisTickText(ChartCtrl.YAxis.Min, 0.0), ChartCtrl.YAxis.Options.TickFont);
			var sz1 = gfx.MeasureString(HandleChartYAxisTickText(ChartCtrl.YAxis.Max, 0.0), ChartCtrl.YAxis.Options.TickFont);
			return width ? Math.Max(sz0.Width, sz1.Width) : Math.Max(sz0.Height, sz1.Height);
		}

		/// <summary>Handle chart mouse down</summary>
		private void HandleChartMouseDown(object sender, MouseEventArgs args)
		{
			// Edit chart elements when Control is held down
			if (ModifierKeys == Keys.Control)
			{
				// Look for hit drag-able indicators
				var hit = ChartCtrl.HitTestCS(args.Location, ModifierKeys, x => (x as Indicator)?.Dragable ?? false);
				if (hit.Hits.Count != 0)
				{
					var indy = (Indicator)hit.Hits[0].Element;
			
					// Create a mouse operation for dragging the indicator
					var op = indy.CreateDragMouseOp();
					ChartCtrl.MouseOperations.SetPending(MouseButtons.Left, op);
				}
			}
		}

		/// <summary>Called when the chart paints</summary>
		private void HandleAddOverlaysOnPaint(object sender, ChartControl.AddOverlaysOnPaintEventArgs e)
		{
			 if (Instrument == null)
				return;

			// Add the ask/bid price to the Y axis
			using (var bsh = new SolidBrush(ChartSettings.AskColour))
			{
				var price     = (float)(decimal)Instrument.Q2BPrice;
				var price_str = e.Chart.YAxis.TickText(price, 0.0);
				var pt        = e.Chart.ChartToClient(new PointF((float)e.Chart.XAxis.Min, price));
				var sz        = e.Gfx.MeasureString(price_str, e.Chart.YAxis.Options.TickFont);
				var box       = sz.Scaled(1.05f);
				e.Gfx.FillRectangle(bsh, new RectangleF(pt.X - box.Width, pt.Y - box.Height/2, box.Width, box.Height));
				e.Gfx.DrawString(price_str, e.Chart.YAxis.Options.TickFont, Brushes.White, pt.X - sz.Width, pt.Y - sz.Height/2);
			}
			using (var bsh = new SolidBrush(ChartSettings.BidColour))
			{
				var price     = (float)(decimal)Instrument.B2QPrice;
				var price_str = e.Chart.YAxis.TickText(price, 0.0);
				var pt        = e.Chart.ChartToClient(new PointF((float)e.Chart.XAxis.Min, price));
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
				var one = Misc.TimeFrameToTimeSpan(1.0, Instrument.TimeFrame);
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
						var click_price = ((decimal)e.HitResult.ChartPoint.Y)._(Instrument.Pair.RateUnits);
						{// Buy
							var type = click_price > Instrument.B2QPrice ? "Stop" : "Limit";
							var opt = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem($"Buy {type} at {click_price}") { ForeColor = ChartSettings.AskColour });
							opt.Click += (s,a) => PlaceNewOrder(ETradeType.B2Q);
						}
						{// Sell
							var type = click_price < Instrument.Q2BPrice ? "Stop" : "Limit";
							var opt = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem($"Sell {type} at {click_price}") { ForeColor = ChartSettings.BidColour });
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
					var opt = e.Menu.Items.Add2(new ToolStripMenuItem(Enum<EXAxisLabelMode>.Cycle(m_xaxis_label_mode).ToString(word_sep:Str.ESeparate.Add)));
					opt.Click += (s,a) =>
					{
						m_xaxis_label_mode = Enum<EXAxisLabelMode>.Cycle(m_xaxis_label_mode);
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
			if (Instrument == null)
				return;

			// Display the last few candles
			var bb = BBox.Reset;
			var idx_min = Instrument.Count - 100;
			var idx_max = Instrument.Count + 20;
			foreach (var candle in Instrument.CandleRange(idx_min, idx_max))
			{
				bb = BBox.Encompass(bb, new v4(idx_min, (float)candle.Low , -ZOrder.Max, 1f));
				bb = BBox.Encompass(bb, new v4(idx_max, (float)candle.High, +ZOrder.Max, 1f));
			}

			// Include trades
			if (ChartSettings.ShowPositions)
			{
				foreach (var pos in AllPositions)
					bb = BBox.Encompass(bb, new v4(bb.Centre.x, (float)(decimal)pos.Price, ZOrder.Trades, 1f));
			}

			// Swell the box a little for margins
			if (bb.IsValid)
			{
				bb.Radius = new v4(bb.Radius.x, bb.Radius.y * 1.1f, bb.Radius.z, 0f);
				e.ViewBBox = bb;
				e.Handled = true;
			}
		}

		/// <summary>Handle the chart moving (zoom or scroll)</summary>
		private void HandleChartMoved(object sender, ChartControl.ChartMovedEventArgs args)
		{
			if (Bit.AnySet(args.MoveType, ChartControl.EMoveType.XZoomed|ChartControl.EMoveType.YZoomed))
				GfxMarketDepth.Invalidate();
		}

		/// <summary>Handle the chart about to render</summary>
		private void HandleChartRendering(object sender, ChartControl.ChartRenderingEventArgs args)
		{
			if (Instrument == null)
				return;

			#region Candles
			{
				// Convert the XAxis values into an index range.
				// (indices, not time frame units, because of the gaps in the price data).
				var range = Instrument.IndexRange((int)(ChartCtrl.XAxis.Min - 1), (int)(ChartCtrl.XAxis.Max + 1));

				// Add the candles that cover 'range'
				foreach (var gfx in GfxCandles.Get(range))
					args.AddToScene(gfx);
			}
			#endregion

			#region Price Data
			{
				// Add the ask and bid lines
				if (GfxAsk != null)
				{
					var price = (float)(decimal)Instrument.Q2BPrice;
					GfxAsk.O2P = m4x4.Scale((float)ChartCtrl.XAxis.Span, 1f, 1f, new v4((float)ChartCtrl.XAxis.Min, price, ZOrder.CurrentPrice, 1f));
					args.AddToScene(GfxAsk);
				}
				if (GfxBid != null)
				{
					var price = (float)(decimal)Instrument.B2QPrice;
					GfxBid.O2P = m4x4.Scale((float)ChartCtrl.XAxis.Span, 1f, 1f, new v4((float)ChartCtrl.XAxis.Min, price, ZOrder.CurrentPrice, 1f));
					args.AddToScene(GfxBid);
				}
			}
			#endregion

			#region Trades
			if (ChartSettings.ShowPositions)
			{
				// Get all positions on this instruction
				foreach (var pos in AllPositions)
					args.AddToScene(GfxPositions.Get(pos));
			}
			#endregion

			#region Market Depth
			if (ChartSettings.ShowMarketDepth)
			{
				var gfx = GfxMarketDepth.Gfx;
				if (gfx != null)
				{
					// Market depth graphics created at x = 0
					gfx.O2P = m4x4.Translation(Instrument.Count, 0f, ZOrder.Indicators);
					args.AddToScene(gfx);
				}
			}
			#endregion
		}

		#region Indicators

		/// <summary>Edit an existing indicator</summary>
		private void EditIndicator(object indicator)
		{
			if (indicator is IndicatorHorzLine hl) { EditHorzLineIndicator(hl); return; }
			if (indicator is IndicatorMA ma)       { EditMaIndicator(ma); return; }
			//if (indicator is SnRIndicator) EditSnRLevel    ((SnRIndicator)indicator);
			//if (indicator is SnR         ) EditSnRIndicator((SnR         )indicator);
			throw new Exception($"Unknown indicator type: {indicator.GetType().Name}");
		}

		/// <summary>Add/Edit a horizontal line indicator</summary>
		private void EditHorzLineIndicator(IndicatorHorzLine line = null)
		{
			line = line ?? Indicators.Add2(new IndicatorHorzLine());
			using (var dlg = new EditHorzLineUI(this, line))
				dlg.ShowDialog(this);
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

		/// <summary>Base class for graphics V,I,N buffers</summary>
		private class GfxBuffers :IDisposable
		{
			/// <summary>Buffers for creating the graphics</summary>
			protected List<View3d.Vertex> m_vbuf;
			protected List<ushort>        m_ibuf;
			protected List<View3d.Nugget> m_nbuf;

			public GfxBuffers()
			{
				m_vbuf = new List<View3d.Vertex>();
				m_ibuf = new List<ushort>();
				m_nbuf = new List<View3d.Nugget>();
			}
			public virtual void Dispose()
			{
				m_vbuf = null;
				m_ibuf = null;
				m_nbuf = null;
			}
		}

		/// <summary>A graphics instance that can be invalidated and recreated asynchronously</summary>
		private abstract class GfxObject :GfxBuffers
		{
			public GfxObject()
			{
				m_issue0 = 0;
				m_issue1 = 1;
			}
			public override void Dispose()
			{
				IsDisposed = true;
				Invalidate();
				Gfx = null;
				base.Dispose();
			}
			protected bool IsDisposed { get; private set; }

			/// <summary>The model to draw</summary>
			public View3d.Object Gfx
			{
				get
				{
					// If the model is out of date, kick off an update
					if (IsInvalidated && !IsDisposed)
					{
						m_issue0 = m_issue1;
						UpdateGfx();
					}
					return m_gfx;
				}
				protected set
				{
					if (m_gfx == value) return;
					Util.Dispose(ref m_gfx);
					m_gfx = value;
				}
			}
			private View3d.Object m_gfx;

			/// <summary>Invalidate this graphics instance</summary>
			public void Invalidate()
			{
				if (IsInvalidated) return;
				++m_issue1;
			}

			/// <summary>True if the graphics object is out of date</summary>
			public bool IsInvalidated
			{
				get { return m_issue1 != m_issue0; }
			}
			protected int m_issue1;
			protected int m_issue0;

			/// <summary>Update the graphic object asynchronously</summary>
			protected abstract void UpdateGfx();
		}

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

		/// <summary>Graphics objects for the candle data</summary>
		private GfxObjectCandles GfxCandles
		{
			get { return m_gfx_candles; }
			set
			{
				if (m_gfx_candles == value) return;
				Util.Dispose(ref m_gfx_candles);
				m_gfx_candles = value;
			}
		}
		private GfxObjectCandles m_gfx_candles;
		private class GfxObjectCandles :GfxBuffers
		{
			private const int BatchSize = 1024;

			private readonly ChartUI m_chart;
			private Cache<int, CandleGfx> m_cache;
			public GfxObjectCandles(ChartUI chart)
			{
				m_chart = chart;
				m_cache = new Cache<int, CandleGfx>();
			}
			public override void Dispose()
			{
				Util.Dispose(ref m_cache);
				base.Dispose();
			}

			/// <summary>Returns the candles graphics model containing the data point with index 'cache_idx'</summary>
			private CandleGfx At(int cache_idx)
			{
				// On miss, generate the graphics model for the data range [idx, idx + min(BatchSize, Count-idx))
				return m_cache.Get(cache_idx, i =>
				{
					var instrument = m_chart.Instrument;
					var colour_bullish = m_chart.ChartSettings.BullishColour.ToArgbU();
					var colour_bearish = m_chart.ChartSettings.BearishColour.ToArgbU();
					var db_idx_range = new Range((i+0) * BatchSize, (i+1) * BatchSize);

					// Get the series data over the time range specified
					var rng = instrument.IndexRange(db_idx_range.Begi, db_idx_range.Endi);
					if (rng.Counti == 0)
						return new CandleGfx(null, rng);

					// Using TriList for the bodies, and LineList for the wicks.
					// So:    6 indices for the body, 4 for the wicks
					//   __|__
					//  |\    |
					//  |  \  |
					//  |____\|
					//     |
					// Dividing the index buffer into [bodies, wicks]
					var candles = instrument.CandleRange(rng.Begi, rng.Endi);
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
						// Use the spot price for the close of the 'Latest' candle.
						var close = rng.Begi + candle_idx == instrument.Count-1
							? (double)(decimal)instrument.SpotPrice(ETradeType.B2Q)
							: candle.Close;

						// Create the graphics with the first candle at x == 0
						var x = (float)candle_idx++;
						var o = (float)Math.Max(candle.Open, close);
						var h = (float)candle.High;
						var l = (float)candle.Low;
						var c = (float)Math.Min(candle.Open, close);
						var col = candle.Bullish ? colour_bullish : candle.Bearish ? colour_bearish : 0xFFA0A0A0;
						var v = vert;

						// Prevent degenerate triangles
						if (o == c)
						{
							o += m_chart.ChartCtrl.ClientToChart(new SizeF(0, 0.25f)).Height;
							c -= m_chart.ChartCtrl.ClientToChart(new SizeF(0, 0.25f)).Height;
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
					return new CandleGfx(gfx, db_idx_range);
				});
			}

			/// <summary>Return graphics objects for the candles over the given range</summary>
			public IEnumerable<View3d.Object> Get(Range candle_range)
			{
				// Convert the index range into a cache index range
				var cache0 = candle_range.Begi / BatchSize;
				var cache1 = candle_range.Endi / BatchSize;
				for (int i = cache0; i <= cache1; ++i)
				{
					// Get the graphics model that contains candle 'i'
					var gfx = At(i);
					if (gfx.Gfx != null)
					{
						// Position the graphics object
						gfx.Gfx.O2P = m4x4.Translation(new v4(gfx.DBIndexRange.Begi, 0.0f, ZOrder.Candles, 1.0f));
						yield return gfx.Gfx;
					}
				}
			}

			/// <summary>Invalidate the graphics object that contains the candle at 'candle_index'</summary>
			public void Invalidate(int candle_index)
			{
				var cache_idx = candle_index / BatchSize;
				m_cache.Invalidate(cache_idx);
			}
			public void Invalidate(Range candle_index_range)
			{
				var idx_beg = candle_index_range.Begi / BatchSize;
				var idx_end = candle_index_range.Endi / BatchSize;
				for (var i = idx_beg; i != idx_end; ++i)
					m_cache.Invalidate(i);
			}

			/// <summary>Invalidate all candle graphics</summary>
			public void Flush()
			{
				m_cache.Flush();
			}

			private class CandleGfx :IDisposable
			{
				public CandleGfx(View3d.Object gfx, Range db_index_range)
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
		}

		/// <summary>Graphics for the market depth indicator</summary>
		private GfxObjectMarketDepth GfxMarketDepth
		{
			get { return m_gfx_market_depth; }
			set
			{
				if (m_gfx_market_depth == value) return;
				Util.Dispose(ref m_gfx_market_depth);
				m_gfx_market_depth = value;
			}
		}
		private GfxObjectMarketDepth m_gfx_market_depth;
		private class GfxObjectMarketDepth :GfxObject
		{
			private readonly ChartUI m_chart;
			public GfxObjectMarketDepth(ChartUI chart)
			{
				m_chart = chart;
			}
			protected override void UpdateGfx()
			{
				var pair = m_chart.Instrument.Pair;
				var b2q_colour = m_chart.ChartSettings.AskColour.Alpha(0.5f).ToArgbU();
				var q2b_colour = m_chart.ChartSettings.BidColour.Alpha(0.5f).ToArgbU();

				// Update the graphics. Abort if a new invalidate arrives, or cancel is signalled
				ThreadPool.QueueUserWorkItem(_ =>
				{
					if (IsInvalidated)
						return;

					// Get the market depth data (order book)
					MarketDepth market_depth;
					try { market_depth = pair.Exchange.MarketDepth(pair, 500); }
					catch (Exception ex)
					{
						pair.Exchange.HandleException(nameof(Exchange.MarketDepth), ex);
						return;
					}
					if (IsInvalidated)
						return;

					var b2q = market_depth.B2Q;
					var q2b = market_depth.Q2B;
					var count_b2q = b2q.Count;
					var count_q2b = q2b.Count;
					if (count_b2q == 0 && count_q2b == 0)
						return;

					// Find the bounds on the order book
					var b2q_volume = 0m;
					var q2b_volume = 0m;
					var price_range = RangeF<decimal>.Invalid;
					foreach (var order in b2q)
					{
						price_range.Encompass(order.Price);
						b2q_volume += order.VolumeBase;
					}
					foreach (var order in q2b)
					{
						price_range.Encompass(order.Price);
						q2b_volume += order.VolumeBase;
					}
					var max_volume = Math.Max(b2q_volume, q2b_volume);

					// Update the graphics model in the GUI thread
					m_chart.Model.RunOnGuiThread(() =>
					{
						if (IsInvalidated || IsDisposed)
							return;

						// Create triangles, 2 per order
						//  [..---''']
						//    [.--']     = q2b
						//      [/]
						//      []
						//     [   ]     = b2q
						//  [         ]
						var count = count_b2q + count_q2b;
						var width_scale = 0.01f; // todo settings
					
						// Resize the cache buffers
						m_vbuf.Resize(4 * count);
						m_ibuf.Resize(6 * count);
						m_nbuf.Resize(1);

						var vert = 0;
						var indx = 0;

						//  b2q = /\
						var volume = 0m;
						for (int i = 0; i != count_b2q; ++i)
						{
							volume += b2q[i].VolumeBase;
							var y1 = (float)(decimal)b2q[i].Price;
							var y0 = i+1 != count_b2q ? (float)(decimal)b2q[i+1].Price : y1;
							var hx = width_scale * (float)(volume / 2);
							var v = vert;

							m_vbuf[vert++] = new View3d.Vertex(new v4(-hx, y1, ZOrder.Indicators, 1f), b2q_colour);
							m_vbuf[vert++] = new View3d.Vertex(new v4(+hx, y1, ZOrder.Indicators, 1f), b2q_colour);
							m_vbuf[vert++] = new View3d.Vertex(new v4(-hx, y0, ZOrder.Indicators, 1f), b2q_colour);
							m_vbuf[vert++] = new View3d.Vertex(new v4(+hx, y0, ZOrder.Indicators, 1f), b2q_colour);

							m_ibuf[indx++] = (ushort)(v + 0);
							m_ibuf[indx++] = (ushort)(v + 2);
							m_ibuf[indx++] = (ushort)(v + 3);
							m_ibuf[indx++] = (ushort)(v + 3);
							m_ibuf[indx++] = (ushort)(v + 1);
							m_ibuf[indx++] = (ushort)(v + 0);
						}

						// q2b = \/
						volume = 0m;
						for (int i = 0; i != count_q2b; ++i)
						{
							volume += q2b[i].VolumeBase;
							var y0 = (float)(decimal)q2b[i].Price;
							var y1 = i+1 != count_q2b ? (float)(decimal)q2b[i+1].Price : y0;
							var hx = width_scale * (float)volume / 2;
							var v = vert;

							m_vbuf[vert++] = new View3d.Vertex(new v4(-hx, y1, ZOrder.Indicators, 1f), q2b_colour);
							m_vbuf[vert++] = new View3d.Vertex(new v4(+hx, y1, ZOrder.Indicators, 1f), q2b_colour);
							m_vbuf[vert++] = new View3d.Vertex(new v4(-hx, y0, ZOrder.Indicators, 1f), q2b_colour);
							m_vbuf[vert++] = new View3d.Vertex(new v4(+hx, y0, ZOrder.Indicators, 1f), q2b_colour);

							m_ibuf[indx++] = (ushort)(v + 0);
							m_ibuf[indx++] = (ushort)(v + 2);
							m_ibuf[indx++] = (ushort)(v + 3);
							m_ibuf[indx++] = (ushort)(v + 3);
							m_ibuf[indx++] = (ushort)(v + 1);
							m_ibuf[indx++] = (ushort)(v + 0);
						}

						m_nbuf[0] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)indx, true);
						var gfx = new View3d.Object("MarketDepth", 0xFFFFFFFF, vert, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());

						Gfx = gfx;
						m_chart.Invalidate();
					});
				});
			}
		}

		/// <summary>Graphics for positions</summary>
		private GfxObjectPositions GfxPositions
		{
			get { return m_gfx_positions; }
			set
			{
				if (m_gfx_positions == value) return;
				Util.Dispose(ref m_gfx_positions);
				m_gfx_positions = value;
			}
		}
		private GfxObjectPositions m_gfx_positions;
		private class GfxObjectPositions :IDisposable
		{
			private readonly ChartUI m_chart;
			private Cache<Guid, View3d.Object> m_cache;
			public GfxObjectPositions(ChartUI chart)
			{
				m_chart = chart;
				m_cache = new Cache<Guid, View3d.Object>();
			}
			public virtual void Dispose()
			{
				Util.Dispose(ref m_cache);
			}

			/// <summary>Get the graphics for the given position</summary>
			public View3d.Object Get(Position pos)
			{
				var instrument = m_chart.Instrument;

				var gfx = m_cache.Get(pos.UniqueKey, g => new View3d.Object($"*Line Position_{pos.OrderId} FF8080FF {{ 0 0 0 1 0 0 }}", file:false));
				var x = instrument.IndexAt(new TimeFrameTime(pos.Created.Value, instrument.TimeFrame));
				var y = (float)(decimal)pos.Price;
				gfx.O2P = m4x4.Scale(instrument.Count + 10 - x, 1f, 1f, new v4(x, y, ZOrder.Trades, 1f));
				return gfx;
			}	
		}

		/// <summary>Debugging method</summary>
		public void InvalidateCandleGfx()
		{
			GfxCandles.Flush();
		}

		#endregion

		#region Mouse Ops

		/// <summary>Mouse operation for adding a new indicator to the chart</summary>
		public class DropNewIndicatorOp :ChartControl.MouseOp
		{
			private readonly ChartUI m_chart_ui;
			private readonly Func<Indicator> m_factory;
			private readonly Action m_on_complete;
			private ChartControl.MouseOp m_drag;
			private Scope m_chart_lock;

			public DropNewIndicatorOp(ChartUI chart_ui, Func<Indicator> factory, Action on_complete)
				:base(chart_ui.ChartCtrl)
			{
				StartOnMouseDown = true;
				m_chart_ui = chart_ui;
				m_factory = factory;
				m_on_complete = on_complete;
				m_chart_lock = chart_ui.LockChartSource();
			}
			public override void Dispose()
			{
				Indy = null;
				Util.Dispose(ref m_chart_lock);
				base.Dispose();
			}

			/// <summary>The created indicator</summary>
			public Indicator Indy
			{
				get { return m_indy; }
				set
				{
					if (m_indy == value) return;
					if (m_indy != null)
					{
						m_indy.Selected = false;
					}
					m_indy = value;
					if (m_indy != null)
					{
						m_indy.Selected = true;
					}
				}
			}
			private Indicator m_indy;

			public override void MouseDown(MouseEventArgs e)
			{
				// Create the indicator on mouse down
				Indy = m_chart_ui.Indicators.Add2(m_factory());

				// Set the initial position if drag-able
				if (Indy.Dragable)
				{
					m_drag = Indy.CreateDragMouseOp();
					m_drag.MouseDown(e);
				}

				base.MouseDown(e);
			}
			public override void MouseMove(MouseEventArgs e)
			{
				if (m_drag != null)
					m_drag.MouseMove(e);

				base.MouseMove(e);
			}
			public override void MouseUp(MouseEventArgs e)
			{
				if (m_drag != null)
					m_drag.MouseUp(e);

				m_on_complete.Raise();
				base.MouseUp(e);
			}
		}

		#endregion

		#region Lock Chart

		/// <summary>Raised when the chart is unlocked</summary>
		private event EventHandler ChartUnlocked;

		/// <summary>Prevent the chart source data changing</summary>
		private Scope LockChartSource()
		{
			return Scope.Create(
				() => ++m_lock_chart_source,
				() =>
				{
					--m_lock_chart_source;
					if (m_lock_chart_source == 0)
					{
						// When the chart unlocks, raise the event on a later window message
						// (only if it has not been locked again in the interim)
						this.BeginInvoke(() =>
						{
							if (IsChartLocked) return;
							ChartUnlocked.Raise();
						});
					}
				});
		}
		private bool IsChartLocked
		{
			get { return m_lock_chart_source != 0; }
		}
		private int m_lock_chart_source;

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
			this.m_chk_show_depth.CheckOnClick = true;
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
