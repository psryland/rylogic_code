using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class CandleChart : Grid, IDockable, IDisposable, INotifyPropertyChanged, IChartView
	{
		public CandleChart(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Chart") { DestroyOnClose = true };
			ChartSelector = new ExchPairTimeFrame(model);
			Chart = m_chart_control;
			Model = model;

			GfxAsk = new GfxObjects.SpotPrice(SettingsData.Settings.Chart.AskColour);
			GfxBid = new GfxObjects.SpotPrice(SettingsData.Settings.Chart.BidColour);
			GfxUpdatingText = new GfxObjects.UpdatingText();
			SpotPriceLabel = new TextBlock();
			CrossHairCandleLabel = new TextBlock();
			CrossHairPriceLabel = new TextBlock();

			// Commands
			ToggleShowOpenOrders = Command.Create(this, ToggleShowOpenOrdersInternal);
			ToggleShowCompletedOrders = Command.Create(this, ToggleShowCompletedOrdersInternal);
			EditTrade = Command.Create(this, EditTradeInternal);

			ModifyContextMenus();
			DataContext = this;
		}
		public void Dispose()
		{
			GfxOpenOrder = null;
			GfxCompletedOrder = null;
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
					m_model.Charts.CollectionChanged -= HandleChartsCollectionChanged;
					SettingsData.Settings.Chart.SettingChange -= HandleSettingChange;
					m_model.Charts.Remove(this);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Charts.Add(this);
					SettingsData.Settings.Chart.SettingChange += HandleSettingChange;
					m_model.Charts.CollectionChanged += HandleChartsCollectionChanged;
				}
				HandleChartsCollectionChanged(this, null);

				// Handler
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					switch (e.Key)
					{
					case nameof(ChartSettings.ShowOpenOrders):
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowOpenOrders)));
						Chart.Scene.Invalidate();
						break;
					case nameof(ChartSettings.ShowCompletedOrders):
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowCompletedOrders)));
						Chart.Scene.Invalidate();
						break;
					case nameof(ChartSettings.XAxisLabelMode):
						Chart.XAxisPanel.Invalidate();
						break;
					}
				}
				void HandleChartsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					DockControl.TabText = ChartName;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChartName)));
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
					m_chart.ChartMoved -= HandleMoved;
					m_chart.BuildScene -= HandleBuildScene;
					m_chart.MouseDown -= HandleMouseDown;
					m_chart.AutoRanging -= HandleAutoRanging;
					m_chart.XAxis.TickText = m_chart.XAxis.DefaultTickText;
					m_chart.YAxis.TickText = m_chart.YAxis.DefaultTickText;
					Util.Dispose(ref m_chart);
				}
				m_chart = value;
				if (m_chart != null)
				{
					// Customise the chart for candles
					m_chart.Options.AntiAliasing = false;
					m_chart.Options.Orthographic = true;
					m_chart.Options.SelectionColour = new Colour32(0x8092A1B1);
					m_chart.Options.CrossHairZOffset = ZOrder.Cursors;
					m_chart.XAxis.Options.PixelsPerTick = 50.0;
					m_chart.XAxis.Options.TickTextTemplate = "XX:XX\r\nXXX XX XXXX";
					m_chart.YAxis.Options.TickTextTemplate = "X.XXXX";
					m_chart.XAxis.TickText = HandleChartXAxisTickText;
					m_chart.YAxis.TickText = HandleChartYAxisTickText;
					m_chart.YAxis.Options.Side = Dock.Right;
					m_chart.AutoRanging += HandleAutoRanging;
					m_chart.MouseDown += HandleMouseDown;
					m_chart.BuildScene += HandleBuildScene;
					m_chart.ChartMoved += HandleMoved;
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
					// To prevent date time overflow, limit the extrapolation of tick values
					var dt_curr =
						curr < first - 1000 ? default :
						curr < first ? Instrument[0].TimestampUTC + Misc.TimeFrameToTimeSpan(curr - first, Instrument.TimeFrame) :
						curr < last ? Instrument[curr].TimestampUTC :
						curr < last + 1000 ? Instrument[last - 1].TimestampUTC + Misc.TimeFrameToTimeSpan(curr - last + 1, Instrument.TimeFrame) :
						default;
					var dt_prev =
						prev < first - 1000 ? default :
						prev < first ? Instrument[0].TimestampUTC + Misc.TimeFrameToTimeSpan(prev - first, Instrument.TimeFrame) :
						prev < last ? Instrument[prev].TimestampUTC :
						prev < last + 1000 ? Instrument[last - 1].TimestampUTC + Misc.TimeFrameToTimeSpan(prev - last + 1, Instrument.TimeFrame) :
						default;
					if (dt_curr == default ||
						dt_prev == default)
						return string.Empty;

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
					return !Math_.FEql(x / Chart.YAxis.Span, 0.0) ? Math_.RoundSD(x, 5).ToString("G8") : "0";
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

					var bb = BBox.Reset;
					if (e.Axes.HasFlag(ChartControl.EAxis.XAxis) && e.Axes.HasFlag(ChartControl.EAxis.YAxis))
					{
						// Display the last few candles @ N pixels per candle
						var width = (int)(Chart.Scene.ActualWidth / 6); // in candles
						var idx_min = Instrument.Count - width * 4 / 5;
						var idx_max = Instrument.Count + width * 1 / 5;
						foreach (var candle in Instrument.CandleRange(idx_min, idx_max))
						{
							bb = BBox.Encompass(bb, new v4(idx_min, (float)candle.Low, -ZOrder.Max, 1f));
							bb = BBox.Encompass(bb, new v4(idx_max, (float)candle.High, +ZOrder.Max, 1f));
						}
					}
					else if (e.Axes.HasFlag(ChartControl.EAxis.YAxis))
					{
						var idx_min = (int)Chart.XAxis.Min;
						var idx_max = (int)Chart.XAxis.Max;
						foreach (var candle in Instrument.CandleRange(idx_min, idx_max))
						{
							bb = BBox.Encompass(bb, new v4(idx_min, (float)candle.Low, -ZOrder.Max, 1f));
							bb = BBox.Encompass(bb, new v4(idx_max, (float)candle.High, +ZOrder.Max, 1f));
						}
					}
					else if (e.Axes.HasFlag(ChartControl.EAxis.XAxis))
					{
						// Display the last few candles @ N pixels per candle
						var width = (int)(Chart.Scene.ActualWidth / 6); // in candles
						var idx_min = Instrument.Count - width * 4 / 5;
						var idx_max = Instrument.Count + width * 1 / 5;
						bb = BBox.Encompass(bb, new v4(idx_min, (float)Chart.YAxis.Min, -ZOrder.Max, 1f));
						bb = BBox.Encompass(bb, new v4(idx_max, (float)Chart.YAxis.Max, +ZOrder.Max, 1f));
					}
					else
					{
						bb = BBox.Encompass(bb, new v4((float)Chart.XAxis.Min, (float)Chart.YAxis.Min, -ZOrder.Max, 1f));
						bb = BBox.Encompass(bb, new v4((float)Chart.XAxis.Max, (float)Chart.YAxis.Max, +ZOrder.Max, 1f));
					}

					// Include trades
					switch (ShowOpenOrders)
					{
					default: break;
						// Add 'thick' bounding boxes for the trades
						//throw new NotImplementedException();
						//foreach (var pos in AllPositions)
						//{
						//	bb = BBox.Encompass(bb, new v4(bb.Centre.x, (float)(decimal)pos.PriceQ2B * 1.01f, ZOrder.Trades, 1f));
						//	bb = BBox.Encompass(bb, new v4(bb.Centre.x, (float)(decimal)pos.PriceQ2B * 0.99f, ZOrder.Trades, 1f));
						//}
					}

					// Include trade history
					switch (ShowCompletedOrders)
					{
					default: break;
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
				void HandleMoved(object sender, ChartControl.ChartMovedEventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisibleTimeSpan)));
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
					SettingsData.Settings.LastChart = string.Empty;
					m_instrument.DataSyncingChanged -= HandleDataSyncingChanged;
					m_instrument.DataChanged -= HandleDataChanged;
					Util.Dispose(m_instrument);
					GfxCompletedOrder = null;
					GfxOpenOrder = null;
					GfxCandles = null;
				}
				m_instrument = value;
				if (m_instrument != null)
				{
					GfxCandles = new GfxObjects.Candles(m_instrument);
					GfxOpenOrder = new GfxObjects.OpenOrder(m_instrument);
					GfxCompletedOrder = new GfxObjects.CompletedOrder(m_instrument);
					m_instrument.DataChanged += HandleDataChanged;
					m_instrument.DataSyncingChanged += HandleDataSyncingChanged;
					SettingsData.Settings.LastChart = $"{m_instrument.Exchange.Name}-{m_instrument.Pair.Name}-{m_instrument.TimeFrame}";
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

					// Only auto when the user isn't interacting with the chart
					var latest_count = Instrument.Count;
					if (Chart.MouseOperations.Active == null)
					{
						// Auto range if the current chart range does not overlap the instrument range
						if (Chart.XAxis.Max < 0 || Chart.XAxis.Min > latest_count)
						{
							Chart.AutoRange();
						}
						// If the latest candle is visible, move the X-Axis range so that it stays in the same place on screen
						else if (((double)m_prev_count).Within(Chart.XAxis.Min, Chart.XAxis.Max))
						{
							Chart.XAxis.Shift(latest_count - m_prev_count);
							Chart.SetCameraFromRange();
						}
					}
					m_prev_count = latest_count;

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
		private int m_prev_count;

		/// <summary>A string description of the period of time shown in the chart</summary>
		public string VisibleTimeSpan
		{
			get
			{
				if (Instrument == null)
					return string.Empty;

				var candle_span = Chart.XAxis.Range.Size;
				var ticks = Misc.TimeFrameToTicks(candle_span, Instrument.TimeFrame);
				return TimeSpan.FromTicks(ticks).ToPrettyString();
			}
		}

		/// <summary>Show currently open orders on the chart</summary>
		public EShowItems ShowOpenOrders
		{
			get => SettingsData.Settings.Chart.ShowOpenOrders;
			set => SettingsData.Settings.Chart.ShowOpenOrders = value;
		}

		/// <summary>Show completed orders on the chart</summary>
		public EShowItems ShowCompletedOrders
		{
			get => SettingsData.Settings.Chart.ShowCompletedOrders;
			set => SettingsData.Settings.Chart.ShowCompletedOrders = value;
		}

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

				// Price label
				if (spot_q2b != null)
				{
					var pt = Chart.ChartToClient(new Point(Chart.XAxis.Max, (double)(decimal)spot_q2b.Value));
					pt = Gui_.MapPoint(Chart, Chart.YAxisPanel, pt);

					Canvas.SetLeft(SpotPriceLabel, 0);
					Canvas.SetTop(SpotPriceLabel, pt.Y - SpotPriceLabel.RenderSize.Height/2);
					SpotPriceLabel.Text = spot_q2b.Value.ToString(5, false);
					SpotPriceLabel.Visibility = Visibility.Visible;
				}
				else
				{
					SpotPriceLabel.Visibility = Visibility.Collapsed;
				}
			}

			// Open Orders
			{
				// Draw all positions on this instruction
				switch (ShowOpenOrders)
				{
				default: throw new Exception($"Unknown 'ShowOpenOrders' mode: {ShowOpenOrders}");
				case EShowItems.Disabled: break;
				case EShowItems.Selected:
					{
						foreach (var item in Model.SelectedOpenOrders)
							window.AddObject(GfxOpenOrder.Get(item, Chart.XAxis.Span, Chart.YAxis.Span));

						break;
					}
				case EShowItems.All:
					{
						if (Instrument.Count == 0) break;
						var time_min = Instrument.TimeAtFIndex(Chart.XAxis.Min);
						var time_max = Instrument.TimeAtFIndex(Chart.XAxis.Max);
						var price_min = ((decimal)Chart.YAxis.Min)._(Instrument.RateUnits);
						var price_max = ((decimal)Chart.YAxis.Max)._(Instrument.RateUnits);
						foreach (var item in Exchange.Orders.Values.Where(Visible))
							window.AddObject(GfxOpenOrder.Get(item, Chart.XAxis.Span, Chart.YAxis.Span));

						bool Visible(Order ord)
						{
							return
								ord.Pair.Equals(Pair) &&
								ord.Created != null && ord.Created.Value.Ticks.Within(time_min, time_max) &&
								ord.PriceQ2B.Within(price_min, price_max);
						}
						break;
					}
				}
			}

			// Completed Orders
			{
				// Draw the selected history trade
				switch (ShowCompletedOrders)
				{
				default: throw new Exception($"Unknown 'ShowCompletedOrders' mode: {ShowCompletedOrders}");
				case EShowItems.Disabled: break;
				case EShowItems.Selected:
					{
						foreach (var item in Model.SelectedCompletedOrders)
							window.AddObject(GfxCompletedOrder.Get(item, Chart.XAxis.Span, Chart.YAxis.Span));

						break;
					}
				case EShowItems.All:
					{
						if (Instrument.Count == 0) break;
						var time_min = Instrument.TimeAtFIndex(Chart.XAxis.Min);
						var time_max = Instrument.TimeAtFIndex(Chart.XAxis.Max);
						var price_min = ((decimal)Chart.YAxis.Min)._(Instrument.RateUnits);
						var price_max = ((decimal)Chart.YAxis.Max)._(Instrument.RateUnits);
						foreach (var item in Exchange.History.Values.Where(Visible))
							window.AddObject(GfxCompletedOrder.Get(item, Chart.XAxis.Span, Chart.YAxis.Span));

						bool Visible(OrderCompleted his)
						{
							return
								his.Pair.Equals(Pair) &&
								his.Created.Ticks.Within(time_min, time_max) &&
								his.PriceQ2B.Within(price_min, price_max);
						}
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

				// Cross hair labels
				if (Chart.ShowCrossHair)
				{
					var xhair = Chart.Tools.CrossHair.O2P.pos.xy;
					var pt = Chart.ChartToClient(new Point(xhair.x, xhair.y));

					// Position the candle label
					CrossHairCandleLabel.Visibility = Visibility.Visible;
					CrossHairCandleLabel.Text = Chart.XAxis.TickText(xhair.x, Chart.XAxis.Span);
					Canvas.SetLeft(CrossHairCandleLabel, Gui_.MapPoint(Chart, Chart.XAxisPanel, pt).X - CrossHairPriceLabel.RenderSize.Width / 2);
					Canvas.SetTop(CrossHairCandleLabel, 0);

					// Position the price label
					CrossHairPriceLabel.Visibility = Visibility.Visible;
					CrossHairPriceLabel.Text = ((decimal)xhair.y).ToString(5);
					Canvas.SetLeft(CrossHairPriceLabel, 0);
					Canvas.SetTop(CrossHairPriceLabel, Gui_.MapPoint(Chart, Chart.YAxisPanel, pt).Y - CrossHairPriceLabel.RenderSize.Height / 2);
				}
				else
				{
					CrossHairCandleLabel.Visibility = Visibility.Collapsed;
					CrossHairPriceLabel.Visibility = Visibility.Collapsed;
				}
			}

			// Put the call out so others can draw on this chart
			//BuildScene?.Invoke(this, args);
		}

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

		/// <summary>Graphics for open orders</summary>
		private GfxObjects.OpenOrder GfxOpenOrder
		{
			get { return m_gfx_open_order; }
			set
			{
				if (m_gfx_open_order == value) return;
				Util.Dispose(ref m_gfx_open_order);
				m_gfx_open_order = value;
			}
		}
		private GfxObjects.OpenOrder m_gfx_open_order;

		/// <summary>Graphics for completed orders</summary>
		private GfxObjects.CompletedOrder GfxCompletedOrder
		{
			get { return m_gfx_completed_order; }
			set
			{
				if (m_gfx_completed_order == value) return;
				Util.Dispose(ref m_gfx_completed_order);
				m_gfx_completed_order = value;
			}
		}
		private GfxObjects.CompletedOrder m_gfx_completed_order;

		/// <summary>A text label for the current spot price</summary>
		public TextBlock SpotPriceLabel
		{
			get { return m_gfx_spot_price; }
			private set
			{
				if (m_gfx_spot_price == value) return;
				if (m_gfx_spot_price != null)
				{
					Chart.YAxisPanel.Children.Remove(m_gfx_spot_price);
				}
				m_gfx_spot_price = value;
				if (m_gfx_spot_price != null)
				{
					m_gfx_spot_price.Name = "m_gfx_spot_price";
					m_gfx_spot_price.Visibility = Visibility.Collapsed;
					m_gfx_spot_price.Typeface(Chart.YAxisPanel.Typeface, Chart.YAxisPanel.FontSize);
					m_gfx_spot_price.Background = new SolidColorBrush(SettingsData.Settings.Chart.AskColour.ToMediaColor());
					m_gfx_spot_price.Foreground = Brushes.White;
					Chart.YAxisPanel.Children.Add(m_gfx_spot_price);
				}
			}
		}
		private TextBlock m_gfx_spot_price;

		/// <summary>A text label for the cross hair candle/time</summary>
		public TextBlock CrossHairCandleLabel
		{
			get { return m_gfx_cross_hair_candle_label; }
			private set
			{
				if (m_gfx_cross_hair_candle_label == value) return;
				if (m_gfx_cross_hair_candle_label != null)
				{
					Chart.XAxisPanel.Children.Remove(m_gfx_cross_hair_candle_label);
				}
				m_gfx_cross_hair_candle_label = value;
				if (m_gfx_cross_hair_candle_label != null)
				{
					m_gfx_cross_hair_candle_label.Name = "m_gfx_cross_hair_candle_label";
					m_gfx_cross_hair_candle_label.Visibility = Visibility.Collapsed;
					m_gfx_cross_hair_candle_label.TextAlignment = TextAlignment.Center;
					m_gfx_cross_hair_candle_label.Typeface(Chart.YAxisPanel.Typeface, Chart.YAxisPanel.FontSize);
					m_gfx_cross_hair_candle_label.Background = new SolidColorBrush(Chart.Tools.CrossHairColour.ToMediaColor());
					m_gfx_cross_hair_candle_label.Foreground = new SolidColorBrush(Chart.Tools.CrossHairColour.Invert().ToMediaColor());
					Chart.XAxisPanel.Children.Add(m_gfx_cross_hair_candle_label);
				}
			}
		}
		private TextBlock m_gfx_cross_hair_candle_label;

		/// <summary>A text label for the cross hair price</summary>
		public TextBlock CrossHairPriceLabel
		{
			get { return m_gfx_cross_hair_price_label; }
			private set
			{
				if (m_gfx_cross_hair_price_label == value) return;
				if (m_gfx_cross_hair_price_label != null)
				{
					Chart.YAxisPanel.Children.Remove(m_gfx_cross_hair_price_label);
				}
				m_gfx_cross_hair_price_label = value;
				if (m_gfx_cross_hair_price_label != null)
				{
					m_gfx_cross_hair_price_label.Name = "m_gfx_cross_hair_price";
					m_gfx_cross_hair_price_label.Visibility = Visibility.Collapsed;
					m_gfx_cross_hair_price_label.TextAlignment = TextAlignment.Left;
					m_gfx_cross_hair_price_label.Typeface(Chart.YAxisPanel.Typeface, Chart.YAxisPanel.FontSize);
					m_gfx_cross_hair_price_label.Background = new SolidColorBrush(Chart.Tools.CrossHairColour.ToMediaColor());
					m_gfx_cross_hair_price_label.Foreground = new SolidColorBrush(Chart.Tools.CrossHairColour.Invert().ToMediaColor());
					Chart.YAxisPanel.Children.Add(m_gfx_cross_hair_price_label);
				}
			}
		}
		private TextBlock m_gfx_cross_hair_price_label;

		/// <summary>Add an ordinal to the chart name based on it's position in the model's chart view collection</summary>
		public string ChartName => Model != null && Model.Charts.Count != 1 ? $"Chart:{Model.Charts.IndexOf(this)+1}" : $"Chart";

		/// <summary>The source exchange</summary>
		public Exchange Exchange
		{
			get => ChartSelector.Exchange;
			set => ChartSelector.Exchange = value;
		}

		/// <summary>The source pair</summary>
		public TradePair Pair
		{
			get => ChartSelector.Pair;
			set => ChartSelector.Pair = value;
		}

		/// <summary>The source time frame</summary>
		public ETimeFrame TimeFrame
		{
			get => ChartSelector.TimeFrame;
			set => ChartSelector.TimeFrame = value;
		}

		/// <summary>True if this chart is the active content in it's dock pane</summary>
		public bool IsActiveContentInPane => DockControl.IsActiveContentInPane;

		/// <summary>Bring this chart to the front of any dock pane it's in</summary>
		public void EnsureActiveContent()
		{
			DockControl.DockContainer.FindAndShow(this);
		}

		/// <summary>Scroll the chart to make 'time' visible</summary>
		public void ScrollToTime(DateTimeOffset time)
		{
			var tft = new TimeFrameTime(time, TimeFrame);
			var idx_range = Instrument.TimeToIndexRange(tft, tft);
			if (!Chart.XAxis.Range.Contains(idx_range))
			{
				Chart.XAxis.Centre = idx_range.Beg;
				Chart.AutoRange(axes: ChartControl.EAxis.YAxis);
				Chart.Scene.Invalidate();
			}
		}

		/// <summary>Cycle the 'show open orders' state</summary>
		public Command ToggleShowOpenOrders { get; }
		private void ToggleShowOpenOrdersInternal()
		{
			ShowOpenOrders = Enum<EShowItems>.Cycle(ShowOpenOrders);
		}

		/// <summary>Cycle the 'show completed orders' state</summary>
		public Command ToggleShowCompletedOrders { get; }
		private void ToggleShowCompletedOrdersInternal()
		{
			ShowCompletedOrders = Enum<EShowItems>.Cycle(ShowCompletedOrders);
		}

		/// <summary>Edit a trade</summary>
		public Command EditTrade { get; }
		private void EditTradeInternal(object x)
		{
			var trade = (Trade)x;
			var order_id = (trade as Order)?.OrderId;

			// Create a graphic to represent the trade on the chart
			var indy = new GfxObjects.TradeIndicator(trade) { Chart = Chart };

			// Create an editor window for the trade
			var ui = new EditTradeUI(Window.GetWindow(this), Model, trade, order_id);
			ui.Closed += (s, a) =>
			{
				indy.Chart = null;
				Util.Dispose(ref indy);
				if (ui.Result == true)
				{
					// Create or Update trade on the exchange
					Dispatcher_.BeginInvoke(async () =>
					{
						try { await trade.CreateOrder(Model.Shutdown.Token); }
						catch (Exception ex)
						{
							MsgBox.Show(Window.GetWindow(this), ex.MessageFull(), "Create/Modify Order", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
						}
					});
				}
			};
			ui.Show();
		}

		/// <summary>Replace the chart context menus</summary>
		private void ModifyContextMenus()
		{
			// Modify the main chart area menu
			{
				var cmenu = m_chart.Scene.ContextMenu;

				// Move the existing chart menu into a sub menu
				var chart_options_menu = new MenuItem { Header = "Chart Options" };
				{
					var items = cmenu.Items.Cast<MenuItem>().ToList();
					cmenu.Items.Clear();
					chart_options_menu.Items.AddRange(items);
				}

				// Indicators sub menu
				var indicators_menu = cmenu.Items.Add2(new MenuItem { Header = "Indicators" });
				{
					// Add an option to add an indicator to the chart
					var add_menu = indicators_menu.Items.Add2(new MenuItem { Header = "Add Indicator" });
					{
						// New Moving Average
						var opt = add_menu.Items.Add2(new MenuItem { Header = "Moving Average" });
						opt.Click += (s, a) =>
						{
							throw new NotImplementedException("Edit Moving Average Indicator not implemented");
						};
					}
					{
						// Trend line
						var opt = add_menu.Items.Add2(new MenuItem { Header = "Trend Line" });
						opt.Click += (s, a) =>
						{
							throw new NotImplementedException("Edit Trend Line Indicator not implemented");
						};
					}
					{
						// Horizontal line
						var opt = add_menu.Items.Add2(new MenuItem { Header = "Horizontal Line" });
						opt.Click += (s, a) =>
						{
							throw new NotImplementedException("Edit Horizontal Line Indicator not implemented");
						};
					}
					{
						// Vertical line
						var opt = add_menu.Items.Add2(new MenuItem { Header = "Vertical Line" });
						opt.Click += (s, a) =>
						{
							throw new NotImplementedException("Edit Vertical Line Indicator not implemented");
						};
					}

					// When the indicators menu opens, add menu items for any existing indicators
					indicators_menu.SubmenuOpened += (s, a) =>
					{
						for (; indicators_menu.Items.Count > 1;)
							indicators_menu.Items.RemoveAt(1);

						indicators_menu.Items.Add(new Separator());
						indicators_menu.Items.Add2(new MenuItem { Header = "<placeholder1>" });
						indicators_menu.Items.Add2(new MenuItem { Header = "<placeholder2>" });
					};
				}

				cmenu.Items.Add(new Separator());

				// Add place holder menu items for creating buy/sell orders.
				// There are updated whenever the menu opens based on the mouse location
				var buy = cmenu.Items.Add2(new MenuItem
				{
					Header = "base",
					Foreground = new SolidColorBrush(SettingsData.Settings.Chart.AskColour.ToMediaColor()),
					Command = EditTrade,
				});
				var sel = cmenu.Items.Add2(new MenuItem
				{
					Header = "quote",
					Foreground = new SolidColorBrush(SettingsData.Settings.Chart.BidColour.ToMediaColor()),
					Command = EditTrade,
				});
				cmenu.Opened += (s, a) =>
				{
					// Get the spot price at the mouse location
					var hit = Chart.HitTestZoneCS(Mouse.GetPosition(Chart), ModifierKeys.None);
					var visible = Instrument != null && hit.ChartPoint.Y > 0;

					// Hide the buy/sell options when there is no instrument or the click price is invalid
					buy.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
					sel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
					if (!visible)
						return;

					var click_price = ((decimal)hit.ChartPoint.Y)._(Instrument.Pair.RateUnits);

					// Buy base currency (Q2B)
					{
						var trade = new Trade(Fund.Main, ETradeType.Q2B, Instrument.Pair, click_price);
						buy.Header = $"{OrderType(trade)} at {trade.PriceQ2B.ToString(5, true)}";
						buy.CommandParameter = trade;
					}

					// Buy quote currency (B2Q)
					{
						var trade = new Trade(Fund.Main, ETradeType.B2Q, Instrument.Pair, click_price);
						sel.Header = $"{OrderType(trade)} at {trade.PriceQ2B.ToString(5, true)}";
						sel.CommandParameter = trade;
					}

					// Convert the trade order type to a string
					string OrderType(Trade trade)
					{
						return
							trade == null ? "Market order" :
							trade.OrderType == EPlaceOrderType.Market ? (trade.TradeType == ETradeType.Q2B ? "Buy" : "Sell") :
							trade.OrderType == EPlaceOrderType.Limit ? "Limit order" :
							trade.OrderType == EPlaceOrderType.Stop ? "Stop order" :
							string.Empty;
					}
				};

				cmenu.Items.Add(new Separator());

				// Add the chart options menu at the end
				cmenu.Items.Add(chart_options_menu);

				// Add this last so it occurs after the other handlers attached to 'Opened'
				cmenu.Opened += Gui_.TidySeparators;
			}

			// Modify the XAxis context menu
			{
				var cmenu = m_chart.XAxis.ContextMenu;

				// Insert an option for changing the units of the XAxis
				var idx = 0;
				{
					var opt = cmenu.Items.Insert2(idx++, new MenuItem { Header = "Label Mode" });
					{
						var cb = opt.Items.Add2(new ComboBox
						{
							ItemsSource = Enum<EXAxisLabelMode>.ValuesArray,
							//SelectedItem = SettingsData.Settings.Chart.XAxisLabelMode,
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
						};
					}
				}
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Z-values for chart elements</summary>
		public static class ZOrder
		{
			// Grid lines are drawn at 0
			public const float Min = 0f;
			public const float Candles = 0.005f;
			public const float CurrentPrice = 0.010f;
			public const float Indicators = 0.012f;
			public const float Trades = 0.013f;
			public const float Cursors = 0.020f;
			public const float Max = 0.1f;
		}

		/// <summary>Context for chart graphics</summary>
		public static readonly Guid CtxId = Guid.NewGuid();
	}
}
