using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CoinFlip.Settings;
using CoinFlip.UI.Dialogs;
using CoinFlip.UI.GfxObjects;
using CoinFlip.UI.Indicators;
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
			IndicatorViews = new ObservableCollection<IIndicatorView>();
			Chart = m_chart_candles;
			Legend = m_indicator_legend;
			AxisLinkTo = None;
			Model = model;

			GfxSpotPrices = new GfxObjects.SpotPrices(Chart);
			GfxUpdatingText = new GfxObjects.UpdatingText(Chart);

			// Commands
			ToggleShowOpenOrders = Command.Create(this, ToggleShowOpenOrdersInternal);
			ToggleShowCompletedOrders = Command.Create(this, ToggleShowCompletedOrdersInternal);
			ToggleVolume = Command.Create(this, ToggleVolumeInternal);
			ToggleMarketDepth = Command.Create(this, ToggleMarketDepthInternal);
			ShowChartOptions = Command.Create(this, ShowChartOptionsInternal);
			EditTrade = Command.Create(this, EditTradeInternal);

			ModifyContextMenus();
			DataContext = this;
		}
		public void Dispose()
		{
			GfxOpenOrders = null;
			GfxCompletedOrders = null;
			GfxUpdatingText = null;
			GfxSpotPrices = null;
			GfxVolume = null;
			GfxMarketDepth = null;
			GfxRemaingCandleTime = null;
			GfxCandles = null;
			ChartSelector = null;
			Instrument = null;
			IndicatorViews = null;
			Model = null;
			Chart = null;
			DockControl = null;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.EditingTrade -= EditingTrade;
					m_model.OrdersChanging -= RefreshChart;
					m_model.HistoryChanging -= RefreshChart;
					m_model.SelectedOpenOrders.CollectionChanged -= RefreshChart;
					m_model.SelectedCompletedOrders.CollectionChanged -= RefreshChart;
					m_model.Charts.CollectionChanged -= HandleChartsCollectionChanged;
					m_model.Indicators.CollectionChanged -= HandleIndicatorCollectionChanged;
					SettingsData.Settings.Chart.SettingChange -= HandleSettingChange;
					CoinData.LivePriceChanged -= HandleLivePricesChanged;
					CoinData.BalanceChanged -= HandleBalanceChanged;
					m_model.Charts.Remove(this);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Charts.Add(this);
					CoinData.BalanceChanged += HandleBalanceChanged;
					CoinData.LivePriceChanged += HandleLivePricesChanged;
					SettingsData.Settings.Chart.SettingChange += HandleSettingChange;
					m_model.Indicators.CollectionChanged += HandleIndicatorCollectionChanged;
					m_model.Charts.CollectionChanged += HandleChartsCollectionChanged;
					m_model.SelectedCompletedOrders.CollectionChanged += RefreshChart;
					m_model.SelectedOpenOrders.CollectionChanged += RefreshChart;
					m_model.HistoryChanging += RefreshChart;
					m_model.OrdersChanging += RefreshChart;
					m_model.EditingTrade += EditingTrade;
				}
				HandleChartsCollectionChanged(this, null);

				// Handler
				void HandleBalanceChanged(object sender, CoinEventArgs e)
				{
					if (e.Coin == Pair?.Base || e.Coin == Pair?.Quote)
					{
						NotifyPropertyChanged(nameof(AccountPosition));
						NotifyPropertyChanged(nameof(AccountPositionDesc));
						NotifyPropertyChanged(nameof(AccountPositionColour));
					}
				}
				void HandleLivePricesChanged(object sender, CoinEventArgs e)
				{
					if (e.Coin == Pair?.Base || e.Coin == Pair?.Quote)
					{
						NotifyPropertyChanged(nameof(AccountPosition));
						NotifyPropertyChanged(nameof(AccountPositionDesc));
						NotifyPropertyChanged(nameof(AccountPositionColour));
					}
				}
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
					case nameof(ChartSettings.SelectionDistance):
						Chart.Options.MinSelectionDistance = SettingsData.Settings.Chart.SelectionDistance;
						break;
					case nameof(ChartSettings.ShowOpenOrders):
						NotifyPropertyChanged(nameof(ShowOpenOrders));
						Chart.Invalidate();
						break;
					case nameof(ChartSettings.ShowCompletedOrders):
						NotifyPropertyChanged(nameof(ShowCompletedOrders));
						Chart.Invalidate();
						break;
					case nameof(ChartSettings.ShowVolume):
						NotifyPropertyChanged(nameof(ShowVolume));
						Chart.Invalidate();
						break;
					case nameof(ChartSettings.ShowMarketDepth):
						NotifyPropertyChanged(nameof(ShowMarketDepth));
						Chart.Invalidate();
						break;
					case nameof(ChartSettings.XAxisLabelMode):
						Chart.XAxisPanel.Invalidate();
						break;
					case nameof(ChartSettings.ConfettiLabelSize):
						Chart.Invalidate();
						break;
					case nameof(ChartSettings.ConfettiLabelTransparency):
						Chart.Invalidate();
						break;
					case nameof(ChartSettings.ConfettiDescriptionsVisible):
						Chart.Invalidate();
						break;
					}
				}
				void HandleChartsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					DockControl.TabText = ChartName;
					NotifyPropertyChanged(nameof(ChartName));
				}
				void HandleIndicatorCollectionChanged(object sender, IndicatorEventArgs e)
				{
					switch (e.Action)
					{
					case NotifyCollectionChangedAction.Reset:
						{
							PopulateIndicators();
							break;
						}
					case NotifyCollectionChangedAction.Add:
						{
							if (Instrument != null && e.PairName == Pair.Name)
								IndicatorViews.Add(e.Indicator.CreateView(this));
							break;
						}
					case NotifyCollectionChangedAction.Remove:
						{
							IndicatorViews.RemoveIf(x => x.Indicator.Id == e.Indicator.Id, dispose: true);
							break;
						}
					}
					Chart?.Invalidate();
				}
				void RefreshChart(object sender, EventArgs args)
				{
					Chart?.Invalidate();
				}
				void EditingTrade(object sender, EditTradeContext e)
				{
					var indy = new GfxObjects.TradeWidget(e.Trade, this) { Chart = Chart };
					e.Closed += delegate
					{
						Util.Dispose(indy);
						Chart.Invalidate();
					};
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
					if (Instrument != null)
						SettingsData.Settings.LastChart = $"{Instrument.Exchange.Name}-{Instrument.Pair.Name}-{Instrument.TimeFrame}";
				}
			}
		}
		private ExchPairTimeFrame m_chart_selector;

		/// <summary>The chart control</summary>
		public ChartControl Chart
		{
			get => m_chart;
			private set
			{
				if (m_chart == value) return;
				if (m_chart != null)
				{
					m_chart.TranslateKey -= HandleKey;
					m_chart.ChartMoved -= HandleMoved;
					m_chart.BuildScene -= HandleBuildScene;
					m_chart.MouseDown -= HandleMouseDown;
					m_chart.AutoRanging -= HandleAutoRanging;
					m_chart.XAxis.TickText = m_chart.XAxis.DefaultTickText;
					m_chart.YAxis.TickText = m_chart.YAxis.DefaultTickText;
					m_chart.TapeMeasureStringFormat = ChartControl.DefaultTapeMeasureStringFormat;
					Util.Dispose(ref m_chart);
				}
				m_chart = value;
				if (m_chart != null)
				{
					// Customise the chart for candles
					m_chart.AllowSelection = true;
					m_chart.AllowElementDragging = false;
					m_chart.Options.AreaSelectMode = ChartControl.EAreaSelectMode.ZoomIfNoSelection;
					m_chart.Options.AreaSelectRequiresShiftKey = true;
					m_chart.Options.AntiAliasing = false;
					m_chart.Options.Orthographic = true;
					m_chart.Options.SelectionColour = new Colour32(0x8092A1B1);
					m_chart.Options.CrossHairZOffset = ZOrder.Cursors;
					m_chart.Options.MinSelectionDistance = SettingsData.Settings.Chart.SelectionDistance;
					m_chart.XAxis.Options.PixelsPerTick = 50.0;
					m_chart.XAxis.Options.TickTextTemplate = "XX:XX\r\nXXX XX XXXX";
					m_chart.YAxis.Options.TickTextTemplate = "X.XXXX";
					m_chart.XAxis.TickText = HandleChartXAxisTickText;
					m_chart.YAxis.TickText = HandleChartYAxisTickText;
					m_chart.TapeMeasureStringFormat = HandleTapeMeasureStringFormat;
					m_chart.YAxis.Options.Side = Dock.Right;
					m_chart.AutoRanging += HandleAutoRanging;
					m_chart.MouseDown += HandleMouseDown;
					m_chart.BuildScene += HandleBuildScene;
					m_chart.ChartMoved += HandleMoved;
					m_chart.TranslateKey += HandleKey;
				}

				// Handlers
				string HandleChartXAxisTickText(double x, double? step = null)
				{
					// Note:
					// The X axis is not a linear time axis because the markets are not online all the time.
					// The X axis is actually an index into the 'Instrument' data where 0 = latest and negative
					// values go further back in the history. Use 'Instrument.TimeToIndexRange' to convert time
					// values to X Axis values.

					// Draw the X Axis labels as indices instead of time stamps
					if (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.AxisIndex)
						return x.ToString();

					if (Instrument == null || Instrument.Count == 0)
						return string.Empty;

					// The range of indices
					var first = 0;
					var last = Instrument.Count;

					// If the ticks are within the range of instrument data, use the actual time stamp.
					// This accounts for missing candles in the data range.
					var prev = (int)(x - step ?? 0.0);
					var curr = (int)(x);

					// If the current tick mark represents the same candle as the previous one, no text is required
					if (prev == curr && step != null)
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
					if (dt_curr == default || dt_prev == default)
						return string.Empty;

					// Get the date time values in the correct time zone
					dt_curr = (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.LocalTime)
						? dt_curr.LocalDateTime
						: dt_curr.UtcDateTime;
					dt_prev = (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.LocalTime)
						? dt_prev.LocalDateTime
						: dt_prev.UtcDateTime;

					// First tick on the x axis
					var first_tick = curr == first || prev < first || step == null || x - step < Chart.XAxis.Min;

					// Show more of the time stamp depending on how it differs from the previous time stamp
					return Misc.ShortTimeString(dt_curr, dt_prev, first_tick);
				}
				string HandleChartYAxisTickText(double x, double? step = null)
				{
					if (Instrument == null)
						return string.Empty;

					// This solves the rounding problem for values near zero when the axis span could be anything
					return !Math_.FEql(x / Chart.YAxis.Span, 0.0) ? Math_.RoundSD(x, 5).ToString("G8") : "0";
				}
				ChartControl.TapeMeasure.LabelText HandleTapeMeasureStringFormat(Point beg, Point end)
				{
					var dx = end.X - beg.X;
					var dy = end.Y - beg.Y;
					return new ChartControl.TapeMeasure.LabelText
					{
						LabelX = $"{new TimeFrameTime(dx, TimeFrame).ExactTimeSpan.ToPrettyString()}\n{Math.Floor(dx)} candles",
						LabelY = $"{dy._(Instrument.Pair.Quote).ToString(4, true)} ({dy / beg.Y:P2})",
						LabelD = null,
					};
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
					BuildScene();
				}
				void HandleMoved(object sender, ChartControl.ChartMovedEventArgs e)
				{
					NotifyPropertyChanged(nameof(VisibleTimeSpan));
				}
				void HandleKey(object sender, KeyEventArgs e)
				{
					switch (e.Key)
					{
					case Key.Delete:
						{
							// Delete selected indicators
							foreach (var indy in IndicatorViews.Where(x => x.Selected).ToList())
								Model.Indicators.Remove(Pair.Name, indy.Indicator.Id);

							e.Handled = true;
							break;
						}
					}
				}
			}
		}
		private ChartControl m_chart;

		/// <summary>The indicator legend</summary>
		public IndicatorLegend Legend
		{
			get => m_legend;
			private set
			{
				if (m_legend == value) return;
				if (m_legend != null)
				{
					m_legend.IndicatorsReordered -= HandleReordred;
					m_legend.PreviewKeyDown -= HandleKey;
				}
				m_legend = value;
				if (m_legend != null)
				{
					m_legend.PreviewKeyDown += HandleKey;
					m_legend.IndicatorsReordered += HandleReordred;
				}

				// Handlers
				void HandleKey(object sender, KeyEventArgs e)
				{
					switch (e.Key)
					{
					case Key.Delete:
						{
							// Delete the selected indicator
							var indy = Legend.SelectedIndicator;
							if (indy != null)
								Model.Indicators.Remove(Pair.Name, indy.Indicator.Id);

							e.Handled = true;
							break;
						}
					}
				}
				void HandleReordred(object sender, EventArgs e)
				{
					// Set the new display order
					int i = 0;
					foreach (var indy in IndicatorViews.OfType<IIndicatorView>())
						indy.Indicator.DisplayOrder = i++;

					Model.Indicators.Save();
					PopulateIndicators();
				}
			}
		}
		private IndicatorLegend m_legend;

		/// <summary>The data displayed on the chart</summary>
		public Instrument Instrument
		{
			get => m_instrument;
			private set
			{
				if (m_instrument == value) return;
				if (m_instrument != null)
				{
					m_instrument.DataSyncingChanged -= HandleDataSyncingChanged;
					m_instrument.DataChanged -= HandleDataChanged;
					Util.Dispose(ref m_instrument);
					GfxVolume = null;
					GfxMarketDepth = null;
					GfxCompletedOrders = null;
					GfxOpenOrders = null;
					GfxRemaingCandleTime = null;
					GfxCandles = null;
				}
				m_instrument = value;
				if (m_instrument != null)
				{
					m_instrument.CandleStyle = CandleStyle;
					GfxCandles = new GfxObjects.Candles(m_instrument);
					GfxRemaingCandleTime = new GfxObjects.RemainingTime(Chart, m_instrument);
					GfxOpenOrders = new GfxObjects.Confetti() { Position = OrderToPosition };
					GfxCompletedOrders = new GfxObjects.Confetti() { Position = OrderToPosition };
					GfxVolume = new GfxObjects.Volume(m_instrument, Chart);
					GfxMarketDepth = new GfxObjects.MarketDepth(m_instrument.Pair.MarketDepth, Chart);
					m_instrument.DataChanged += HandleDataChanged;
					m_instrument.DataSyncingChanged += HandleDataSyncingChanged;
				}

				// Add indicators
				PopulateIndicators();

				// Auto Range and refresh
				Chart.AutoRange();

				// Notify
				NotifyPropertyChanged(nameof(AccountPositionDesc));
				NotifyPropertyChanged(nameof(AccountPositionColour));

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
						//if (Chart.XAxis.Max < 0 || Chart.XAxis.Min > latest_count)
						//{
						//	Chart.AutoRange();
						//}
						//else
						// If the latest candle is visible, move the X-Axis range so that it stays in the same place on screen
						if (((double)m_prev_count).Within(Chart.XAxis.Min, Chart.XAxis.Max))
						{
							Chart.XAxis.Shift(latest_count - m_prev_count);
							Chart.SetCameraFromRange();
						}
					}
					m_prev_count = latest_count;

					// Signal a refresh
					Chart.Invalidate();
				}
				void HandleDataSyncingChanged(object sender, EventArgs e)
				{
					// Signal a refresh
					Chart.Invalidate();
				}
				Point OrderToPosition(Confetti.IItem item)
				{
					if (item is OrderConfettiAdapter oca)
					{
						var time = Model.UtcNow;
						var X = m_instrument.IndexAt(new TimeFrameTime(time, m_instrument.TimeFrame));
						var Y = oca.Order.PriceQ2B.ToDouble();
						return new Point(X, Y);
					}
					if (item is OrderCompletedConfettiAdapter cca)
					{
						var time = cca.Order.Created;
						var X = m_instrument.IndexAt(new TimeFrameTime(time, m_instrument.TimeFrame));
						var Y = cca.Order.PriceQ2B.ToDouble();
						return new Point(X, Y);
					}
					return new Point();
				}
			}
		}
		private Instrument m_instrument;
		private int m_prev_count;

		/// <summary>Indicators on this chart</summary>
		public ObservableCollection<IIndicatorView> IndicatorViews
		{
			get => m_indicator_views;
			private set
			{
				// Notes:
				//  - This collection is maintained by Model.Indicators.CollectionChanged to match indicator
				//    views 1-to-1 with the indicators in the model associated with this instrument. Do not try
				//    to delete Model.Indicators based on removals from this collection. Instead, delete from
				//    the Model.Indicators then update this collection.
				//  - This collection is observable so that the DataGrid updates in the UI automatically.

				if (m_indicator_views == value) return;
				if (m_indicator_views != null)
				{
					m_indicator_legend.Indicators = null;
					Util.DisposeAll(m_indicator_views);
				}
				m_indicator_views = value;
				if (m_indicator_views != null)
				{
					m_indicator_legend.Indicators = CollectionViewSource.GetDefaultView(m_indicator_views);
				}
			}
		}
		private ObservableCollection<IIndicatorView> m_indicator_views;

		/// <summary>A string description of the period of time shown in the chart</summary>
		public string VisibleTimeSpan
		{
			get
			{
				try
				{
					if (Instrument == null)
						return string.Empty;

					var candle_span = Chart.XAxis.Range.Size;
					var ticks = Misc.TimeFrameToTicks(candle_span, Instrument.TimeFrame);
					return TimeSpan.FromTicks(ticks).ToPrettyString();
				}
				catch
				{
					return string.Empty;
				}
			}
		}

		/// <summary>The account balance weighting for this chart. -1 = %100 bearish, +1 = %100 bullish</summary>
		public double AccountPosition
		{
			get
			{
				if (Exchange == null || Pair == null) return 0.0;
				var total_base = Exchange.Balance[Pair.Base].NettTotal;
				var total_quote = Exchange.Balance[Pair.Quote].NettTotal;
				var value_base = (double)Pair.Base.ValueOf(total_base).ToDouble();
				var value_quote = (double)Pair.Quote.ValueOf(total_quote).ToDouble();
				var denom = value_base + value_quote;
				return !Math_.FEql(denom, 0) ? (value_base - value_quote) / denom : 0.0;
			}
		}
		public string AccountPositionDesc
		{
			get
			{
				var pos = AccountPosition;
				return
					pos > 0 ? $"{+pos:P0} Bullish" :
					pos < 0 ? $"{-pos:P0} Bearish" :
					"0% Neutral";
			}
		}
		public Colour32 AccountPositionColour
		{
			get
			{
				var pos = AccountPosition;
				return
					pos > 0 ? SettingsData.Settings.Chart.Q2BColour.Alpha(+pos) :
					pos < 0 ? SettingsData.Settings.Chart.B2QColour.Alpha(-pos) :
					Colour32.Transparent;
			}
		}

		/// <summary>The style of candles to use</summary>
		public ECandleStyle CandleStyle
		{
			get => m_candle_style;
			set
			{
				if (m_candle_style == value) return;
				m_candle_style = value;

				// Update the current instrument
				if (Instrument != null)
				{
					Instrument.CandleStyle = value;
					GfxCandles?.Invalidate();
					Chart.Invalidate();
				}
			}
		}
		private ECandleStyle m_candle_style;

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

		/// <summary>Show the volume indicator</summary>
		public bool ShowVolume
		{
			get => SettingsData.Settings.Chart.ShowVolume;
			set => SettingsData.Settings.Chart.ShowVolume = value;
		}

		/// <summary>Show current market depth</summary>
		public bool ShowMarketDepth
		{
			get => SettingsData.Settings.Chart.ShowMarketDepth;
			set => SettingsData.Settings.Chart.ShowMarketDepth = value;
		}

		/// <summary>Add graphics and elements to the chart</summary>
		private void BuildScene()
		{
			if (Instrument == null || !DockControl.IsVisible)
				return;

			// Candles
			GfxCandles.BuildScene(Chart);

			// Price Data
			GfxSpotPrices.BuildScene(Instrument.Pair, Chart);

			// Updating text
			GfxUpdatingText.BuildScene(Instrument?.DataSyncing == true, Chart);

			// Candle remaining time
			GfxRemaingCandleTime.BuildScene(Chart);

			// Trade volume
			GfxVolume.BuildScene();

			// Market Depth
			GfxMarketDepth.BuildScene();

			// Open Orders
			{
				// Draw all positions on this instruction
				switch (ShowOpenOrders)
				{
				default: throw new Exception($"Unknown 'ShowOpenOrders' mode: {ShowOpenOrders}");
				case EShowItems.Disabled:
					{
						GfxOpenOrders.ClearScene();
						break;
					}
				case EShowItems.Selected:
					{
						if (Instrument.Count != 0)
						{
							var orders = Model.SelectedOpenOrders.Where(Visible)
								.Select(x => new OrderConfettiAdapter(x));

							GfxOpenOrders.BuildScene(orders, null, Chart);
						}
						else
						{
							GfxOpenOrders.ClearScene();
						}
						break;
					}
				case EShowItems.All:
					{
						if (Exchange != null && Instrument.Count != 0)
						{
							var orders = Exchange.Orders.Where(Visible)
								.Select(x => new OrderConfettiAdapter(x));
							var highlight = Model.SelectedOpenOrders
								.Select(x => new OrderConfettiAdapter(x));

							GfxOpenOrders.BuildScene(orders, highlight, Chart);
						}
						else
						{
							GfxOpenOrders.ClearScene();
						}
						break;
					}
				}
				bool Visible(Order ord)
				{
					var time_min = Instrument.TimeAtFIndex(Chart.XAxis.Min);
					var time_max = Instrument.TimeAtFIndex(Chart.XAxis.Max);
					var price_min = ((decimal)Chart.YAxis.Min)._(Instrument.RateUnits);
					var price_max = ((decimal)Chart.YAxis.Max)._(Instrument.RateUnits);
					return
						ord.Pair.Equals(Pair) &&
						Model.UtcNow.Ticks.Within(time_min, time_max) &&
						ord.PriceQ2B.Within(price_min, price_max) &&
						!Model.IsOpenInEditor(ord);
				}
			}

			// Completed Orders
			{
				// Draw the completed orders
				switch (ShowCompletedOrders)
				{
				default: throw new Exception($"Unknown 'ShowCompletedOrders' mode: {ShowCompletedOrders}");
				case EShowItems.Disabled:
					{
						GfxCompletedOrders.ClearScene();
						break;
					}
				case EShowItems.Selected:
					{
						if (Instrument.Count != 0)
						{
							var orders = Model.SelectedCompletedOrders.Where(Visible)
								.Select(x => new OrderCompletedConfettiAdapter(x));

							GfxCompletedOrders.BuildScene(orders, null, Chart);
						}
						else
						{
							GfxCompletedOrders.ClearScene();
						}
						break;
					}
				case EShowItems.All:
					{
						if (Exchange != null && Instrument.Count != 0)
						{
							var orders = Exchange.History.Where(Visible)
								.Select(x => new OrderCompletedConfettiAdapter(x));
							var highlighted = Model.SelectedCompletedOrders
								.Select(x => new OrderCompletedConfettiAdapter(x));

							GfxCompletedOrders.BuildScene(orders, highlighted, Chart);
						}
						else
						{
							GfxCompletedOrders.ClearScene();
						}
						break;
					}
				}
				bool Visible(OrderCompleted ord)
				{
					var time_min = Instrument.TimeAtFIndex(Chart.XAxis.Min);
					var time_max = Instrument.TimeAtFIndex(Chart.XAxis.Max);
					var price_min = ((decimal)Chart.YAxis.Min)._(Instrument.RateUnits);
					var price_max = ((decimal)Chart.YAxis.Max)._(Instrument.RateUnits);
					return
						ord.Pair.Equals(Pair) &&
						ord.Created.Ticks.Within(time_min, time_max) &&
						ord.PriceQ2B.Within(price_min, price_max);
				}
			}

			// Bots
			{
				//// See if any bots want to draw on this chart
				//foreach (var bot in Model.Bots.Where(x => x.Active))
				//	bot.OnChartRendering(Instrument, ChartSettings, args);
			}

			// Indicator Views
			foreach (var indy in IndicatorViews)
				indy.BuildScene(this);
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

		/// <summary>Graphics for the quote->base price line</summary>
		private GfxObjects.SpotPrices GfxSpotPrices
		{
			get => m_gfx_spot_price;
			set
			{
				if (m_gfx_spot_price == value) return;
				Util.Dispose(ref m_gfx_spot_price);
				m_gfx_spot_price = value;
			}
		}
		private GfxObjects.SpotPrices m_gfx_spot_price;

		/// <summary>The time remaining on the current latest candle</summary>
		private GfxObjects.RemainingTime GfxRemaingCandleTime
		{
			get => m_gfx_remaining_time;
			set
			{
				if (m_gfx_remaining_time == value) return;
				Util.Dispose(ref m_gfx_remaining_time);
				m_gfx_remaining_time = value;
			}
		}
		private GfxObjects.RemainingTime m_gfx_remaining_time;

		/// <summary>A message to indicate the chart is updating</summary>
		private GfxObjects.UpdatingText GfxUpdatingText
		{
			get => m_gfx_updating_text;
			set
			{
				if (m_gfx_updating_text == value) return;
				Util.Dispose(ref m_gfx_updating_text);
				m_gfx_updating_text = value;
			}
		}
		private GfxObjects.UpdatingText m_gfx_updating_text;

		/// <summary>Graphics for open orders</summary>
		private GfxObjects.Confetti GfxOpenOrders
		{
			get => m_gfx_open_orders;
			set
			{
				if (m_gfx_open_orders == value) return;
				if (m_gfx_open_orders != null)
				{
					m_gfx_open_orders.ItemSelected -= HandleOrderSelected;
					m_gfx_open_orders.EditItem -= HandleEditOrder;
					Util.Dispose(ref m_gfx_open_orders);
				}
				m_gfx_open_orders = value;
				if (m_gfx_open_orders != null)
				{
					m_gfx_open_orders.ItemSelected += HandleOrderSelected;
					m_gfx_open_orders.EditItem += HandleEditOrder;
				}

				// Handlers
				void HandleEditOrder(object sender, Confetti.ItemEventArgs e)
				{
					if (e.Item is Order order)
						Model.EditTrade(order);
				}
				void HandleOrderSelected(object sender, Confetti.ItemEventArgs e)
				{
					if (e.Item is Order order)
					{
						Model.SelectedOpenOrders.Clear();
						Model.SelectedOpenOrders.Add(order);
					}
				}
			}
		}
		private GfxObjects.Confetti m_gfx_open_orders;
		private class OrderConfettiAdapter :GfxObjects.Confetti.IItem
		{
			public OrderConfettiAdapter(Order order)
			{
				Order = order;
			}
			public Order Order { get; }
			public Polygon Icon => new Polygon
			{
				Stroke = Brushes.Black,
				Points = Order.TradeType == ETradeType.Q2B
					? new PointCollection(new[] { new Point(0, 0), new Point(-5, +5), new Point(+5, +5) })
					: new PointCollection(new[] { new Point(0, 0), new Point(-5, -5), new Point(+5, -5) }),
			};
			public TextBlock Label => new TextBlock
			{
				Text = Order.Description,
				FontSize = 8.0,
			};
			public Colour32 Colour => Order.TradeType == ETradeType.Q2B
				? SettingsData.Settings.Chart.Q2BColour.Darken(0.5f)
				: SettingsData.Settings.Chart.B2QColour.Darken(0.5f);

			#region Equals
			public static bool operator ==(OrderConfettiAdapter lhs, OrderConfettiAdapter rhs)
			{
				return ReferenceEquals(lhs.Order, rhs.Order);
			}
			public static bool operator !=(OrderConfettiAdapter lhs, OrderConfettiAdapter rhs)
			{
				return !ReferenceEquals(lhs.Order, rhs.Order);
			}
			public override bool Equals(object obj)
			{
				return obj is OrderConfettiAdapter rhs && Order == rhs.Order;
			}
			public override int GetHashCode()
			{
				return Order.GetHashCode();
			}
			#endregion
		}

		/// <summary>Graphics for completed orders</summary>
		private GfxObjects.Confetti GfxCompletedOrders
		{
			get => m_gfx_completed_orders;
			set
			{
				if (m_gfx_completed_orders == value) return;
				if (m_gfx_completed_orders != null)
				{
					m_gfx_completed_orders.ItemSelected -= HandleOrderSelected;
					Util.Dispose(ref m_gfx_completed_orders);
				}
				m_gfx_completed_orders = value;
				if (m_gfx_completed_orders != null)
				{
					m_gfx_completed_orders.ItemSelected += HandleOrderSelected;
				}

				// Handler
				void HandleOrderSelected(object sender, Confetti.ItemEventArgs e)
				{
					if (e.Item is OrderCompleted order)
					{
						Model.SelectedCompletedOrders.Clear();
						Model.SelectedCompletedOrders.Add(order);
					}
				}
			}
		}
		private GfxObjects.Confetti m_gfx_completed_orders;
		private class OrderCompletedConfettiAdapter :GfxObjects.Confetti.IItem
		{
			public OrderCompletedConfettiAdapter(OrderCompleted order)
			{
				Order = order;
			}
			public OrderCompleted Order { get; }
			public Polygon Icon => new Polygon
			{
				Stroke = Brushes.Black,
				Points = Order.TradeType == ETradeType.Q2B
					? new PointCollection(new[] { new Point(0, 0), new Point(-5, +5), new Point(+5, +5) })
					: new PointCollection(new[] { new Point(0, 0), new Point(-5, -5), new Point(+5, -5) }),
			};
			public TextBlock Label => new TextBlock
			{
				Text = Order.Description,
				FontSize = 8.0,
			};
			public Colour32 Colour => Order.TradeType == ETradeType.Q2B
				? SettingsData.Settings.Chart.Q2BColour.Darken(0.5f)
				: SettingsData.Settings.Chart.B2QColour.Darken(0.5f);

			#region Equals
			public static bool operator ==(OrderCompletedConfettiAdapter lhs, OrderCompletedConfettiAdapter rhs)
			{
				return ReferenceEquals(lhs.Order, rhs.Order);
			}
			public static bool operator !=(OrderCompletedConfettiAdapter lhs, OrderCompletedConfettiAdapter rhs)
			{
				return !ReferenceEquals(lhs.Order, rhs.Order);
			}
			public override bool Equals(object obj)
			{
				return obj is OrderCompletedConfettiAdapter rhs && Order == rhs.Order;
			}
			public override int GetHashCode()
			{
				return Order.GetHashCode();
			}
			#endregion
		}

		/// <summary>Graphics for trade volume</summary>
		private GfxObjects.Volume GfxVolume
		{
			get { return m_gfx_volume; }
			set
			{
				if (m_gfx_volume == value) return;
				Util.Dispose(ref m_gfx_volume);
				m_gfx_volume = value;
			}
		}
		private GfxObjects.Volume m_gfx_volume;

		/// <summary>Graphics for market depth</summary>
		private GfxObjects.MarketDepth GfxMarketDepth
		{
			get => m_gfx_market_depth;
			set
			{
				if (m_gfx_market_depth == value) return;
				Util.Dispose(ref m_gfx_market_depth);
				m_gfx_market_depth = value;
			}
		}
		private GfxObjects.MarketDepth m_gfx_market_depth;

		/// <summary>Add an ordinal to the chart name based on it's position in the model's chart view collection</summary>
		public string ChartName => Model != null && Model.Charts.Count != 1 ? $"Chart:{Model.Charts.IndexOf(this)+1}" : $"Chart";

		/// <summary>Another chart that this chart is linked</summary>
		public IChartView AxisLinkTo
		{
			get => m_axis_link_to ?? (IChartView)None;
			set
			{
				if (m_axis_link_to == value || m_in_axis_link_to != 0) return;
				using (Scope.Create(() => ++m_in_axis_link_to, () => --m_in_axis_link_to))
				{
					if (m_axis_link_to != null)
					{
						// Remove the old link
						//2-way link? m_axis_link_to.AxisLinkTo = null;
						Chart.XAxis.LinkTo = null;
					}
					m_axis_link_to = value as CandleChart;
					if (m_axis_link_to != null)
					{
						// Set up the new link
						Chart.XAxis.LinkTo = new ChartControl.AxisLinkData(m_axis_link_to.Chart.XAxis, r => ConvertXRange(m_axis_link_to, this, r));
						//2-way link? m_axis_link_to.AxisLinkTo = this;
					}
					NotifyPropertyChanged(nameof(AxisLinkTo));
				}

				// Handler
				RangeF ConvertXRange(CandleChart src, CandleChart dst, RangeF range)
				{
					// Copy 'src's axis to 'dst's axis
					if (src.Instrument == null || src.Instrument.Count == 0 || src.TimeFrame == ETimeFrame.None) return range;
					if (dst.Instrument == null || dst.Instrument.Count == 0 || dst.TimeFrame == ETimeFrame.None) return range;

					// Convert from the index range on 'src' to a time range
					var r0 = src.Instrument.FIndexToTimeRange(range.Beg, range.End);

					// Convert from the time range to an index range on 'dst'
					var r1 = dst.Instrument.TimeToFIndexRange(new TimeFrameTime(r0.Beg, dst.TimeFrame), new TimeFrameTime(r0.End, dst.TimeFrame));

					return r1;
				}
			}
		}
		private CandleChart m_axis_link_to;
		private int m_in_axis_link_to;

		/// <summary>The source exchange</summary>
		public Exchange Exchange
		{
			get => ChartSelector?.Exchange;
			set => ChartSelector.Exchange = value;
		}

		/// <summary>The source pair</summary>
		public TradePair Pair
		{
			get => ChartSelector?.Pair;
			set => ChartSelector.Pair = value;
		}

		/// <summary>The source time frame</summary>
		public ETimeFrame TimeFrame
		{
			get => ChartSelector?.TimeFrame ?? ETimeFrame.None;
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
				Chart.Invalidate();
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

		/// <summary>Show/Hide the market depth graphics on the chart</summary>
		public Command ToggleVolume { get; }
		private void ToggleVolumeInternal()
		{
			ShowVolume = !ShowVolume;
		}

		/// <summary>Show/Hide the market depth graphics on the chart</summary>
		public Command ToggleMarketDepth { get; }
		private void ToggleMarketDepthInternal()
		{
			ShowMarketDepth = !ShowMarketDepth;
		}

		/// <summary>Show the options dialog for chart behaviour</summary>
		public Command ShowChartOptions { get; }
		private void ShowChartOptionsInternal()
		{
			var owner = Window.GetWindow(this);
			var pt = owner.PointToScreen(Mouse.GetPosition(owner));
			ChartOptionsUI.Show(owner, pt);
		}

		/// <summary>Edit a trade</summary>
		public Command EditTrade { get; }
		private void EditTradeInternal(object x)
		{
			Model.EditTrade((Trade)x);
		}

		/// <summary>Replace the chart context menus</summary>
		private void ModifyContextMenus()
		{
			// Modify the main chart area menu
			{
				var cmenu = Chart.Scene.ContextMenu;

				// Move the existing chart menu into a sub menu
				var chart_options_menu = new MenuItem { Header = "Chart Options" };
				{
					var items = cmenu.Items.Cast<MenuItem>().ToList();
					cmenu.Items.Clear();
					chart_options_menu.Items.AddRange(items);
				}

				// Drawing tools menu
				var drawing_menu = cmenu.Items.Add2(new MenuItem { Header = "Drawing" });
				foreach (var drawing in Indicator_.EnumDrawings())
				{
					var opt = drawing_menu.Items.Add2(new MenuItem { Header = Indicator_.Name(drawing) });
					opt.Click += (s, a) =>
					{
						if (Instrument == null) return;

						var instance = Indicator_.CreateInstance(drawing, this);
						if (instance is ChartControl.MouseOp op)
						{
							Chart.MouseOperations.Pending[MouseButton.Left] = op;
							return;
						}
						if (instance is IIndicator indy)
						{
							Model.Indicators.Add(Instrument.Pair.Name, indy);
							return;
						}
					};
				}

				// Indicators sub menu
				var indicators_menu = cmenu.Items.Add2(new MenuItem { Header = "Indicators" });
				foreach (var indicator in Indicator_.EnumIndicators())
				{
					var opt = indicators_menu.Items.Add2(new MenuItem { Header = Indicator_.Name(indicator) });
					opt.Click += (s, a) =>
					{
						if (Instrument == null) return;

						var instance = Indicator_.CreateInstance(indicator, this);
						if (instance is ChartControl.MouseOp op)
						{
							Chart.MouseOperations.Pending[MouseButton.Left] = op;
							return;
						}
						if (instance is IIndicator indy)
						{
							Model.Indicators.Add(Instrument.Pair.Name, indy);
							return;
						}
					};
				}

				// Candle style menu
				var candles_menu = cmenu.Items.Add2(new MenuItem { Header = "Candles", DataContext = this });
				{
					var cb = candles_menu.Items.Add2(new ComboBox
					{
						Style = (Style)FindResource(System.Windows.Controls.ToolBar.ComboBoxStyleKey),
						ItemsSource = Enum<ECandleStyle>.Values,
						SelectedItem = CandleStyle,
						Background = SystemColors.ControlBrush,
						BorderThickness = new Thickness(0),
						Margin = new Thickness(1, 1, 20, 1),
						MinWidth = 80,
					});
					cb.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(CandleStyle)) { Mode = BindingMode.TwoWay });
				}

				cmenu.Items.Add(new Separator());

				// Add place holder menu items for creating buy/sell orders.
				// There are updated whenever the menu opens based on the mouse location
				var buy = cmenu.Items.Add2(new MenuItem
				{
					Header = "base",
					Foreground = new SolidColorBrush(SettingsData.Settings.Chart.Q2BColour.ToMediaColor()),
					Command = EditTrade,
				});
				var sel = cmenu.Items.Add2(new MenuItem
				{
					Header = "quote",
					Foreground = new SolidColorBrush(SettingsData.Settings.Chart.B2QColour.ToMediaColor()),
					Command = EditTrade,
				});
				cmenu.Opened += delegate
				{
					// Get the price at the mouse location
					var hit = Chart.HitTestZoneCS(Mouse.GetPosition(Chart), ModifierKeys.None, WPFUtil.MouseBtns());
					var visible = Instrument != null && hit.ChartPoint.Y > 0;

					// Hide the buy/sell options when there is no instrument or the click price is invalid
					buy.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
					sel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
					if (!visible)
						return;

					var click_price = ((decimal)hit.ChartPoint.Y)._(Pair.RateUnits);

					// Buy base currency (Q2B)
					{
						var order_type = Pair.OrderType(ETradeType.Q2B, click_price) ?? EOrderType.Market;
						var trade = new Trade(Fund.Default, Pair, order_type, ETradeType.Q2B, 0m._(Pair.Quote), 0m._(Pair.Base)) { PriceQ2B = click_price };
						buy.Header = $"Buy {Pair.Base} at {trade.PriceQ2B.ToString(5, true)}...";
						buy.CommandParameter = trade;
					}

					// Buy quote currency (B2Q)
					{
						var order_type = Pair.OrderType(ETradeType.B2Q, click_price) ?? EOrderType.Market;
						var trade = new Trade(Fund.Default, Pair, order_type, ETradeType.B2Q, 0m._(Pair.Base), 0m._(Pair.Quote)) { PriceQ2B = click_price };
						sel.Header = $"Sell {Pair.Base} at {trade.PriceQ2B.ToString(5, true)}...";
						sel.CommandParameter = trade;
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
				var cmenu = Chart.XAxis.ContextMenu;
				{
					// Add an option for changing the units of the XAxis
					var opt = cmenu.Items.Add2(new MenuItem { Header = "Label Mode", DataContext = SettingsData.Settings.Chart });
					{
						var cb = opt.Items.Add2(new ComboBox
						{
							Style = (Style)FindResource(System.Windows.Controls.ToolBar.ComboBoxStyleKey),
							ItemsSource = Enum<EXAxisLabelMode>.ValuesArray,
							Background = SystemColors.ControlBrush,
							BorderThickness = new Thickness(0),
							Margin = new Thickness(1, 1, 20, 1),
							MinWidth = 80,
						});
						cb.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(ChartSettings.XAxisLabelMode)) { Mode = BindingMode.TwoWay });
					}
				}
				{
					// Add an option to link this axis to the axis of another chart
					var opt = cmenu.Items.Add2(new MenuItem { Header = "Link Axis", DataContext = this });
					cmenu.Opened += delegate { opt.IsEnabled = Model.Charts.Count > 1; };
					{
						var other_charts = CollectionViewSource.GetDefaultView(Model.Charts.Except(this).Prepend(None));
						cmenu.Opened += delegate { other_charts.Refresh(); };

						var cb = opt.Items.Add2(new ComboBox
						{
							Style = FindResource(System.Windows.Controls.ToolBar.ComboBoxStyleKey) as Style,
							ItemsSource = other_charts,
							DisplayMemberPath = nameof(IChartView.ChartName),
							Background = SystemColors.ControlBrush,
							BorderThickness = new Thickness(0),
							Margin = new Thickness(1, 1, 20, 1),
							MinWidth = 80,
						});
						cb.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(AxisLinkTo)) { Mode = BindingMode.TwoWay });
					}
				}
			}
		}

		/// <summary>Create indicator views for the current instrument</summary>
		private void PopulateIndicators()
		{
			Util.DisposeAll(IndicatorViews);
			if (Pair == null || Instrument == null)
				return;

			foreach (var indy in Model.Indicators[Pair])
				IndicatorViews.Add(indy.CreateView(this));
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

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

		/// <summary>Proxy chart to represent 'no chart'</summary>
		private static readonly NullChart None = new NullChart();
		private class NullChart :IChartView
		{
			public string ChartName => "None";
			public Exchange Exchange { get => null; set { } }
			public TradePair Pair { get => null; set { } }
			public ETimeFrame TimeFrame { get => ETimeFrame.None; set { } }
			public Instrument Instrument { get => null; set { } }
			public bool IsActiveContentInPane { get => false; set { } }
			public void EnsureActiveContent() { throw new NotImplementedException(); }
			public void ScrollToTime(DateTimeOffset _) { throw new NotImplementedException(); }
		}
	}
}
