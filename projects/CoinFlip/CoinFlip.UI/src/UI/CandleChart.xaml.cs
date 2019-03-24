using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class CandleChart : Grid, IDockable, IDisposable
	{
		public CandleChart(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Chart");
			ChartSelector = new ExchPairTimeFrame(model);
			Chart = m_chart_control;
			Model = model;

			GfxAsk = new GfxObjects.SpotPrice(SettingsData.Settings.Chart.AskColour);
			GfxBid = new GfxObjects.SpotPrice(SettingsData.Settings.Chart.BidColour);
			GfxUpdatingText = new GfxObjects.UpdatingText();

			DataContext = this;
		}
		public void Dispose()
		{
			GfxUpdatingText = null;
			GfxCandles = null;
			ChartSelector = null;
			Instrument = null;
			Chart = null;
			Model = null;
			DockControl = null;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				}
				m_model = value;
				if (m_model != null)
				{
				}
			}
		}
		private Model m_model;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

		/// <summary>Selects exchange/pair/timeframe</summary>
		public ExchPairTimeFrame ChartSelector
		{
			get { return m_chart_selector; }
			private set
			{
				if (m_chart_selector == value) return;
				if (m_chart_selector != null)
				{
					m_chart_selector.PropertyChanged -= HandlePropertyChanged;
					Util.Dispose(ref m_chart_selector);
				}
				m_chart_selector = value;
				if (m_chart_selector != null)
				{
					m_chart_selector.PropertyChanged += HandlePropertyChanged;
				}

				// Handler
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					Instrument = ChartSelector.GetInstrument($"Chart {ChartSelector.Address}");
				}
			}
		}
		private ExchPairTimeFrame m_chart_selector;

		/// <summary>The chart control</summary>
		public ChartControl Chart
		{
			get { return m_chart; }
			private set
			{
				if (m_chart == value) return;
				if (m_chart != null)
				{
					m_chart.BuildScene -= HandleBuildScene;
					m_chart.MouseDown -= HandleMouseDown;
					m_chart.AutoRanging -= HandleAutoRanging;
					m_chart.YAxis.TickText = m_chart.YAxis.DefaultTickText;
					m_chart.XAxis.TickText = m_chart.XAxis.DefaultTickText;
					Util.Dispose(ref m_chart);
				}
				m_chart = value;
				if (m_chart != null)
				{
					// Customise the chart for candles
					m_chart.Options.AntiAliasing = false;
					m_chart.XAxis.Options.PixelsPerTick = 50.0;
					m_chart.XAxis.Options.TickTextTemplate = "XX:XX\r\nXXX XX XXXX";
					m_chart.YAxis.Options.TickTextTemplate = "X.XXXX";
					m_chart.XAxis.TickText = HandleChartXAxisTickText;
					m_chart.YAxis.TickText = HandleChartYAxisTickText;
					ModifyContextMenus();
					m_chart.AutoRanging += HandleAutoRanging;
					m_chart.MouseDown += HandleMouseDown;
					m_chart.BuildScene += HandleBuildScene;
				}

				// Handlers
				string HandleChartXAxisTickText(double x, double step)
				{
					// Note:
					// The X axis is not a linear time axis because the markets are not online all the time.
					// The X axis is actually an index into the 'Instrument' data where 0 = latest and negative
					// values go further back in the history. Use 'Instrument.TimeToIndexRange' to convert time
					// values to X Axis values.

					// Draw the X Axis labels as indices instead of time stamps
					if (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.CandleIndex)
						return x.ToString();

					if (Instrument == null || Instrument.Count == 0)
						return string.Empty;

					// The range of indices
					var first = 0;
					var last = Instrument.Count;

					// If the ticks are within the range of instrument data, use the actual time stamp.
					// This accounts for missing candles in the data range.
					var prev = (int)(x - step);
					var curr = (int)(x);

					// If the current tick mark represents the same candle as the previous one, no text is required
					if (prev == curr)
						return string.Empty;

					// Get the date time for the tick
					var dt_curr =
						curr >= first && curr < last ? Instrument[curr].TimestampUTC :
						curr < first ? Instrument[0].TimestampUTC - Misc.TimeFrameToTimeSpan(first - curr, Instrument.TimeFrame) :
						curr >= last ? Instrument[last - 1].TimestampUTC + Misc.TimeFrameToTimeSpan(curr - last + 1, Instrument.TimeFrame) :
						throw new Exception("Impossible candle index");
					var dt_prev =
						prev >= first && prev < last ? Instrument[prev].TimestampUTC :
						prev < first ? Instrument[0].TimestampUTC - Misc.TimeFrameToTimeSpan(first - prev, Instrument.TimeFrame) :
						prev >= last ? Instrument[last - 1].TimestampUTC + Misc.TimeFrameToTimeSpan(prev - last + 1, Instrument.TimeFrame) :
						throw new Exception("Impossible candle index");

					// Get the date time values in the correct time zone
					dt_curr = (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.LocalTime)
						? dt_curr.LocalDateTime
						: dt_curr.UtcDateTime;
					dt_prev = (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.LocalTime)
						? dt_prev.LocalDateTime
						: dt_prev.UtcDateTime;

					// First tick on the x axis
					var first_tick = curr == first || prev < first || x - step < Chart.XAxis.Min;

					// Show more of the time stamp depending on how it differs from the previous time stamp
					return Misc.ShortTimeString(dt_curr, dt_prev, first_tick);
				}
				string HandleChartYAxisTickText(double x, double step)
				{
					if (Instrument == null)
						return string.Empty;

					// This solves the rounding problem for values near zero when the axis span could be anything
					return !Math_.FEql(x / Chart.YAxis.Span, 0.0) ? Math_.RoundSF(x, 5).ToString("G8") : "0";
				}
				void HandleMouseDown(object sender, MouseButtonEventArgs e)
				{
					// Edit chart elements when Control is held down
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					{
						// Look for a hit on a drag-able indicator
						var loc = e.GetPosition(Chart);
						//var hit = Chart.HitTestCS(loc, Keyboard.Modifiers, x => x is Indicator indicator && indicator.Dragable);
						//if (hit.Hits.Count != 0 && hit.Hits[0].Element is Indicator indy)
						//{
						//	// Create a mouse operation for dragging the indicator
						//	var op = indy.CreateDragMouseOp(hit);
						//	Chart.MouseOperations.SetPending(MouseButtons.Left, op);
						//}
						e.Handled = true;
					}
				}
				void HandleAutoRanging(object sender, ChartControl.AutoRangeEventArgs e)
				{
					if (Instrument == null)
						return;

					// Display the last few candles @ N pixels per candle
					var bb = BBox.Reset;
					var width = (int)(Chart.Scene.ActualWidth / 6); // in candles
					var idx_min = Instrument.Count - width * 4 / 5;
					var idx_max = Instrument.Count + width * 1 / 5;
					foreach (var candle in Instrument.CandleRange(idx_min, idx_max))
					{
						bb = BBox.Encompass(bb, new v4(idx_min, (float)candle.Low, -ZOrder.Max, 1f));
						bb = BBox.Encompass(bb, new v4(idx_max, (float)candle.High, +ZOrder.Max, 1f));
					}

					// Include trades
					if (SettingsData.Settings.Chart.ShowPositions)
					{
						// Add 'thick' bounding boxes for the trades
						throw new NotImplementedException();
						//foreach (var pos in AllPositions)
						//{
						//	bb = BBox.Encompass(bb, new v4(bb.Centre.x, (float)(decimal)pos.PriceQ2B * 1.01f, ZOrder.Trades, 1f));
						//	bb = BBox.Encompass(bb, new v4(bb.Centre.x, (float)(decimal)pos.PriceQ2B * 0.99f, ZOrder.Trades, 1f));
						//}
					}

					// Swell the box a little for margins
					if (bb.IsValid)
					{
						bb.Radius = new v4(bb.Radius.x, bb.Radius.y * 1.1f, bb.Radius.z, 0f);
						e.ViewBBox = bb;
						e.Handled = true;
					}
				}
				void HandleBuildScene(object sender, View3dControl.BuildSceneEventArgs e)
				{
					BuildScene(e.Window);
				}
				void ModifyContextMenus()
				{
					{// Chart menu
						var idx = 0;
						var cmenu = m_chart.Scene.ContextMenu;
						var indicators_menu = cmenu.Items.Insert2(idx++, new MenuItem { Header = "Indicators" });
						{
							// Add a new indicator
							var add_menu = indicators_menu.Items.Add2(new MenuItem { Header = "Add Indicator" });
							{
								{
									// MA
									var opt = add_menu.Items.Add2(new MenuItem { Header = "Moving Average" });
									opt.Click += (s, a) =>
									{
										//	EditMaIndicator();
									};
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

							indicators_menu.Items.Add2(new Separator());

							// // Modify existing indicators
							// var hit_indicators = e.HitResult.Hits.Where(x => x.Element is Indicator).Select(x => (Indicator)x.Element).ToArray();
							// foreach (var indicator in Indicators)
							// {
							// 	var is_hit = hit_indicators.Contains(indicator);
							// 	var colour = is_hit ? Color.Blue : Color.Black;
							// 	var font = is_hit ? new Font(e.Menu.Font, FontStyle.Bold) : e.Menu.Font;
							// 	var opt = indicators_menu.DropDownItems.Add2(new ToolStripIndiatorMenuItem(indicator.Name) { ForeColor = colour, Font = font });
							// 	opt.CrossClicked += (s, a) => Indicators.Remove(indicator);
							// 	opt.Click += (s, a) => EditIndicator(indicator);
							// }
						}
						var orders_menu = cmenu.Items.Insert2(idx++, new MenuItem { Header = "Orders" });
						{
							//if (Instrument != null)
							//{
							//	// The price where the mouse was clicked
							//	var click_price = ((decimal)e.HitResult.ChartPoint.Y)._(Instrument.Pair.RateUnits);
							//
							//	// The corresponding pair on the currently selected exchange
							//	var pair = Model.FindPairOnCurrentExchange(Instrument.Pair.Name);
							//	if (pair != null)
							//	{
							//		{// Buy base currency (Q2B)
							//			var trade = new Trade(Fund.Main, ETradeType.Q2B, pair, click_price);
							//			var opt = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem($"{OrderType(trade)} at {trade.PriceQ2B.ToString("G6")}") { ForeColor = ChartSettings.AskColour });
							//			opt.Click += (s, a) =>
							//			{
							//				Model.EditTrade(trade, null);
							//			};
							//		}
							//		{// Buy quote currency (B2Q)
							//			var trade = new Trade(Fund.Main, ETradeType.B2Q, pair, click_price);
							//			var opt = e.Menu.Items.Insert2(idx++, new ToolStripMenuItem($"{OrderType(trade)} at {trade.PriceQ2B.ToString("G6")}") { ForeColor = ChartSettings.BidColour });
							//			opt.Click += (s, a) =>
							//			{
							//				Model.EditTrade(trade, null);
							//			};
							//		}
							//	}
							//
							//	string OrderType(Trade trade)
							//	{
							//		return
							//			trade == null ? "Market order" :
							//			trade.OrderType == EPlaceOrderType.Market ? (trade.TradeType == ETradeType.Q2B ? "Buy" : "Sell") :
							//			trade.OrderType == EPlaceOrderType.Limit ? "Limit order" :
							//			trade.OrderType == EPlaceOrderType.Stop ? "Stop order" :
							//			string.Empty;
							//	}
							//}
						}
						var rendering_menu = cmenu.Items.Insert2(idx++, new MenuItem { Header = "Rendering" });
						{
							// Move all original chart menu options into a sub menu called 'Rendering'
							for (; cmenu.Items.Count > idx;)
							{
								var item = cmenu.Items[idx];
								cmenu.Items.RemoveAt(idx);
								rendering_menu.Items.Add(item);
							}
						}
						cmenu.Items.TidySeparators();
					}
					{// XAxis menu
						var idx = 0;
						var cmenu = m_chart.XAxis.ContextMenu;
						{
							var opt = cmenu.Items.Insert2(idx++, new MenuItem { Header = "Label Mode" });
							{
								var cb = opt.Items.Add2(new ComboBox
								{
									ItemsSource = Enum<EXAxisLabelMode>.ValuesArray,
									SelectedItem = SettingsData.Settings.Chart.XAxisLabelMode,
									Style = FindResource(System.Windows.Controls.ToolBar.ComboBoxStyleKey) as Style,
									Background = SystemColors.ControlBrush,
									BorderThickness = new Thickness(0),
									Margin = new Thickness(1, 1, 20, 1),
									MinWidth = 80,
								});
								cb.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(ChartSettings.XAxisLabelMode)) { Source = SettingsData.Settings.Chart });
								cb.SelectionChanged += (s, a) =>
								{
									SettingsData.Settings.Chart.XAxisLabelMode = (EXAxisLabelMode)cb.SelectedItem;
									m_chart.InvalidateArrange();
								};
							}
						}
						cmenu.Items.TidySeparators();
					}
					{// YAxis menu
					}
				}
			}
		}
		private ChartControl m_chart;

		/// <summary>The data displayed on the chart</summary>
		public Instrument Instrument
		{
			get { return m_instrument; }
			private set
			{
				if (m_instrument == value) return;
				if (m_instrument != null)
				{
					m_instrument.DataSyncingChanged -= HandleDataSyncingChanged;
					m_instrument.DataChanged -= HandleDataChanged;
					Util.Dispose(m_instrument);
					GfxCandles = null;
				}
				m_instrument = value;
				if (m_instrument != null)
				{
					GfxCandles = new GfxObjects.Candles(m_instrument);
					m_instrument.DataChanged += HandleDataChanged;
					m_instrument.DataSyncingChanged += HandleDataSyncingChanged;
				}

				// Auto Range and refresh
				Chart.AutoRange();

				// Handler
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
					if (e.UpdateType == DataEventArgs.EUpdateType.New)
					{
						// If the latest candle is visible, move the X-Axis range forward.
						var latest_idx = (double)Instrument.Count;
						if (latest_idx.Within(Chart.XAxis.Min, Chart.XAxis.Max))
							Chart.XAxis.Set(Chart.XAxis.Min + 1, Chart.XAxis.Max + 1);
					}

					// Invalidate other instrument data dependent graphics
					//GfxMarketDepth.Invalidate();

					// Signal a refresh
					Chart.Scene.Invalidate();
				}
				void HandleDataSyncingChanged(object sender, EventArgs e)
				{
					// Signal a refresh
					Chart.Scene.Invalidate();
				}
			}
		}
		private Instrument m_instrument;

		/// <summary>Add graphics and elements to the chart</summary>
		private void BuildScene(View3d.Window window)
		{
			window.RemoveObjects(new[] { CtxId }, 1, 0);
			if (Instrument == null)
				return;

			// Candles
			{
				 // Convert the XAxis values into an index range.
				 // (indices, not time frame units, because of the gaps in the price data).
				var range = Instrument.IndexRange((int)(Chart.XAxis.Min - 1), (int)(Chart.XAxis.Max + 1));

				// Add the candles that cover 'range'
				foreach (var gfx in GfxCandles.Get(range))
					window.AddObject(gfx);
			}

			// Price Data
			{
				// Add the ask and bid lines
				var pair = Instrument.Pair;
				var spot_q2b = pair.SpotPrice(ETradeType.Q2B);
				var spot_b2q = pair.SpotPrice(ETradeType.B2Q);
				if (GfxAsk != null)
				{
					if (spot_q2b != null)
					{
						var price = (float)(decimal)spot_q2b.Value;
						GfxAsk.O2P = m4x4.Scale((float)Chart.XAxis.Span, 1f, 1f, new v4((float)Chart.XAxis.Min, price, ZOrder.CurrentPrice, 1f));
						window.AddObject(GfxAsk);
					}
					else
					{
						window.RemoveObject(GfxAsk);
					}
				}
				if (GfxBid != null)
				{
					if (spot_b2q != null)
					{
						var price = (float)(decimal)spot_b2q.Value;
						GfxBid.O2P = m4x4.Scale((float)Chart.XAxis.Span, 1f, 1f, new v4((float)Chart.XAxis.Min, price, ZOrder.CurrentPrice, 1f));
						window.AddObject(GfxBid);
					}
					else
					{
						window.RemoveObject(GfxBid);
					}
				}
			}

			// Trades
			{
				// Draw all positions on this instruction
				if (SettingsData.Settings.Chart.ShowPositions)
				{
				//	foreach (var pos in VisiblePositions)
				//		window.AddObject(GfxPositions.Get(pos));
				}

				// Draw the selected history trade
				switch (SettingsData.Settings.Chart.ShowTradeHistory)
				{
				default: throw new Exception("Unknown trade history mode");
				case EShowTradeHistory.Disabled:
					{
						break;
					}
				case EShowTradeHistory.Selected:
					{
						//var his = Model.History.Current;
						//if (his.Pair.Name == Instrument.Pair.Name)
						//	window.AddObject(GfxHistory.Get(his));

						break;
					}
				case EShowTradeHistory.All:
					{
						//foreach (var his in VisibleHistory)
						//	window.AddObject(GfxHistory.Get(his));
						break;
					}
				}
			}

			// Market Depth
			if (SettingsData.Settings.Chart.ShowMarketDepth)
			{
				//var gfx = GfxMarketDepth.Gfx;
				//if (gfx != null)
				//{
				//	// Market depth graphics created at x = 0 with an aspect ratio of 1:2
				//	var x_scale = (float)(0.25 * ChartCtrl.XAxis.Span / ChartCtrl.YAxis.Span);
				//	gfx.O2P = m4x4.Translation(Instrument.Count + 5, 0f, ZOrder.Indicators) * m4x4.Scale(x_scale, 1f, 1f, v4.Origin);
				//	args.AddToScene(gfx);
				//}
			}

			// Bots
			{
				//// See if any bots want to draw on this chart
				//foreach (var bot in Model.Bots.Where(x => x.Active))
				//	bot.OnChartRendering(Instrument, ChartSettings, args);
			}

			// Other
			{
				// Updating text
				if (GfxUpdatingText != null && Instrument?.DataSyncing == true)
					window.AddObject(GfxUpdatingText);
			}

			// Put the call out so others can draw on this chart
			//BuildScene?.Invoke(this, args);
		}

		//public event EventHandler<View3dControl.BuildSceneEventArgs> BuildScene;

		/// <summary>Graphics objects for the candle data</summary>
		private GfxObjects.Candles GfxCandles
		{
			get { return m_gfx_candles; }
			set
			{
				if (m_gfx_candles == value) return;
				Util.Dispose(ref m_gfx_candles);
				m_gfx_candles = value;
			}
		}
		private GfxObjects.Candles m_gfx_candles;

		/// <summary>Graphics for the asking price line</summary>
		private GfxObjects.SpotPrice GfxAsk
		{
			get { return m_gfx_ask; }
			set
			{
				if (m_gfx_ask == value) return;
				Util.Dispose(ref m_gfx_ask);
				m_gfx_ask = value;
			}
		}
		private GfxObjects.SpotPrice m_gfx_ask;

		/// <summary>Graphics for the asking price line</summary>
		private GfxObjects.SpotPrice GfxBid
		{
			get { return m_gfx_bid; }
			set
			{
				if (m_gfx_bid == value) return;
				Util.Dispose(ref m_gfx_bid);
				m_gfx_bid = value;
			}
		}
		private GfxObjects.SpotPrice m_gfx_bid;

		/// <summary>A message to indicate the chart is updating</summary>
		private GfxObjects.UpdatingText GfxUpdatingText
		{
			get { return m_gfx_updating_text; }
			set
			{
				if (m_gfx_updating_text == value) return;
				Util.Dispose(ref m_gfx_updating_text);
				m_gfx_updating_text = value;
			}
		}
		private GfxObjects.UpdatingText m_gfx_updating_text;

		/// <summary>Z-values for chart elements</summary>
		public static class ZOrder
		{
			// Grid lines are drawn at 0
			public const float Min = 0f;
			public const float Candles = 0.010f;
			public const float CurrentPrice = 0.011f;
			public const float Indicators = 0.012f;
			public const float Trades = 0.013f;
			public const float Max = 0.1f;
		}

		/// <summary>Context for chart graphics</summary>
		public static readonly Guid CtxId = Guid.NewGuid();
	}
}
