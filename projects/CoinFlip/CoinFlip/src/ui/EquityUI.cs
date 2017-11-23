using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.db;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class EquityUI :UserControl ,IDockable
	{
		private const string AllExchanges = "All Exchanges";
		private const ETimeFrame TimeScale = ETimeFrame.Day1;

		#region UI Elements
		private TableLayoutPanel m_table0;
		private ToolStrip m_ts;
		private ToolTip m_tt;
		private ToolStripLabel m_lbl_exchange;
		private System.Windows.Forms.ToolStripComboBox m_cb_exchange;
		private ToolStripButton m_chk_common_value;
		private ToolStripButton m_btn_combined;
		private ChartControl m_chart;
		#endregion

		private EquityUI() {}
		public EquityUI(Model model, string name)
		{
			InitializeComponent();

			Model = model;
			DockControl = new DockControl(this, name) { TabText = name };
			XAxisLabelMode = EXAxisLabelMode.LocalTime;
			Legend = new ChartDataLegend{ Chart = m_chart };
			Data = new EquityMap(this);
			m_auto_range = true;

			SeriesOptionsTemplate = new ChartDataSeries.OptionsData
			{
				Colour = Colour32.Blue,
				PlotType = ChartDataSeries.EPlotType.Line,
				PointSize = 5f,
				PointsOnLinePlot = true,
				LineWidth = 2f,
			};

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Data = null;
			Legend = null;
			Model = null;
			DockControl = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			TriggerUpdate();
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		 {
		 	get { return m_impl_dock_control; }
		 	private set
		 	{
		 		if (m_impl_dock_control == value) return;
		 		Util.Dispose(ref m_impl_dock_control);
		 		m_impl_dock_control = value;
		 	}
		 }
		private DockControl m_impl_dock_control;

		/// <summary>App logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.SimReset -= HandleSimReset;
					m_model.BackTestingChanging -= HandleBackTestingChanged;
					m_model.TradeHistoryChanged -= HandleTradeHistoryChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.TradeHistoryChanged += HandleTradeHistoryChanged;
					m_model.BackTestingChanging += HandleBackTestingChanged;
					m_model.SimReset += HandleSimReset;
				}

				// Handlers
				void HandleTradeHistoryChanged(object sender, EventArgs e)
				{
					TriggerUpdate();
				}
				void HandleBackTestingChanged(object sender, PrePostEventArgs e)
				{
				}
				void HandleSimReset(object sender, SimResetEventArgs e)
				{
				}
			}
		}
		private Model m_model;

		/// <summary>The equity data displayed in this chart</summary>
		public EquityMap Data
		{
			get { return m_data; }
			private set
			{
				if (m_data == value) return;
				if (m_data != null)
				{
					Util.Dispose(ref m_data);
					m_auto_range = true;
				}
				m_data = value;
				if (m_data != null)
				{
					// Add each data series to the chart
					foreach (var series in m_data.Values)
						series.Chart = m_chart;

					// Do an auto range if flagged
					if (m_auto_range)
						m_chart.AutoRange();

					// Recreate the legend graphics
					Legend.Invalidate();
					m_auto_range = false;
				}
				m_chart.Invalidate();
			}
		}
		private EquityMap m_data;
		private bool m_auto_range;

		/// <summary>How to display tick values on the X Axis</summary>
		public EXAxisLabelMode XAxisLabelMode
		{
			get { return m_xaxis_label_mode; }
			set
			{
				if (m_xaxis_label_mode == value) return;
				m_xaxis_label_mode = value;
				m_chart.Invalidate();
			}
		}
		public EXAxisLabelMode m_xaxis_label_mode;

		/// <summary></summary>
		public ChartDataSeries.OptionsData SeriesOptionsTemplate { get; private set; }

		/// <summary>Legend</summary>
		public ChartDataLegend Legend
		{
			get { return m_legend; }
			set
			{
				if (m_legend == value) return;
				Util.Dispose(ref m_legend);
				m_legend = value;
			}
		}
		private ChartDataLegend m_legend;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			#region Tool bar

			// Exchange
			m_cb_exchange.ToolTip(m_tt, "Display equity on the selected exchange, or all exchanges");
			m_cb_exchange.ComboBox.DataSource = Model.TradingExchanges.Select(x => x.Name).Concat("").Prepend(AllExchanges).ToArray();
			m_cb_exchange.ComboBox.SelectedItem = AllExchanges;
			m_cb_exchange.ComboBox.SelectedIndexChanged += (s,a) =>
			{
				TriggerUpdate();
			};

			// Common value
			m_chk_common_value.ToolTip(m_tt, "Convert currencies to their assigned value prices");
			m_chk_common_value.Checked = Model.Settings.Equity.CommonValues;
			m_chk_common_value.CheckedChanged += (s,a) =>
			{
				TriggerUpdate();
			};

			#endregion

			#region Chart
			m_chart.DefaultMouseControl      = true;
			m_chart.DefaultKeyboardShortcuts = true;
			m_chart.AreaSelectMode = ChartControl.EAreaSelectMode.Zoom;
			m_chart.XAxis.TickText = (double x, double step) =>
			{
				// Draw the X Axis labels as indices instead of time stamps
				if (XAxisLabelMode == EXAxisLabelMode.CandleIndex || TimeScale == ETimeFrame.None || Data.Count == 0)
					return x.ToString();

				// Get the times of 'x' and 'x-step'
				var dt_curr = new DateTimeOffset(Data.TimeRange.Beg + Misc.TimeFrameToTicks(x       , TimeScale), TimeSpan.Zero);
				var dt_prev = new DateTimeOffset(Data.TimeRange.Beg + Misc.TimeFrameToTicks(x - step, TimeScale), TimeSpan.Zero);

				// Get the date time values in the correct time zone
				dt_curr = (XAxisLabelMode == EXAxisLabelMode.LocalTime)
					? dt_curr.LocalDateTime
					: dt_curr.UtcDateTime;
				dt_prev = (XAxisLabelMode == EXAxisLabelMode.LocalTime)
					? dt_prev.LocalDateTime
					: dt_prev.UtcDateTime;

				// First tick on the x axis
				var first_tick = x - step < m_chart.XAxis.Min;

				// Show more of the time stamp depending on how it differs from the previous time stamp
				return Misc.ShortTimeString(dt_curr, dt_prev, first_tick);
			};
			m_chart.AddUserMenuOptions += (s,a) =>
			{
				switch (a.Type)
				{
				case ChartControl.AddUserMenuOptionsEventArgs.EType.XAxis:
					{
						var opt = a.Menu.Items.Add2(new ToolStripMenuItem(Enum<EXAxisLabelMode>.Cycle(XAxisLabelMode).ToString(word_sep:Str.ESeparate.Add)));
						opt.Click += (ss,aa) =>
						{
							XAxisLabelMode = Enum<EXAxisLabelMode>.Cycle(XAxisLabelMode);
							Invalidate(true);
						};
						break;
					}
				}
			};
			#endregion
		}

		/// <summary>Enumerable range of exchanges to show equity data for</summary>
		private IEnumerable<Exchange> ExchangesToConsider
		{
			get
			{
				var exch = (string)m_cb_exchange.SelectedItem;
				return exch != AllExchanges
					? Model.TradingExchanges.Where(x => x.Name == exch)
					: Model.TradingExchanges;
			}
		}

		/// <summary>True if currencies should be scaled by their assigned values</summary>
		private bool UseAssignedValues
		{
			get { return m_chk_common_value.Checked; }
		}

		/// <summary>Trigger a refresh of the graph data</summary>
		public void TriggerUpdate()
		{
			if (m_data_update.Pending || !IsHandleCreated) return;
			m_data_update.Signal();

			// Kick off an update of the equity data
			this.BeginInvoke(UpdateSeriesData);
			void UpdateSeriesData()
			{
				// Get the coins we care about
				var exchanges = ExchangesToConsider.ToList();
				var coins = exchanges.SelectMany(x => x.CoinsOfInterest).ToList();
				var transfers = exchanges.SelectMany(x => x.Transfers.Values).ToList();
				var balances = coins.ToAccumulator(x => x.Symbol, x => (double)(decimal)x.Balance.Total);
				var assigned_values = Model.Settings.Coins.ToDictionary(x => x.Symbol, x => (double)x.AssignedValue);
				var use_assigned_values = UseAssignedValues;
				var time_scale = TimeScale;
				var now = Model.UtcNow.Ticks;

				// Collate equity data in a worker thread
				ThreadPool.QueueUserWorkItem(_ =>
				{
					try
					{
						m_data_update.Actioned();

						// Load all of the trade history into memory
						var trade_history = new List<TradeRecord>();
						foreach (var exch in exchanges)
						{
							// Abort if another update is pending
							if (m_data_update.Pending)
								return;

							// Read the trade history from 'exch'
							var sql = $"select * from {SqlExpr.TradeHistory}";
							using (var db = new Sqlite.Database(exch.HistoryDBFilepath, Sqlite.OpenFlags.ReadOnly))
								trade_history.AddRange(db.EnumRows<TradeRecord>(sql));
						}

						// Generate a new equity map
						var equity = new EquityMap(this);

						// For each coin, get the changes in balance due to trades, deposits, and withdrawals
						foreach (var sym in balances.Keys)
						{
							// Abort if another update is pending
							if (m_data_update.Pending)
								return;

							var series = equity[sym];
							using (var table = series.Lock())
							{
								// Generate a collection of balance deltas with timestamps
								foreach (var xfr in transfers.Where(x => x.Coin.Symbol == sym && x.Status == Transfer.EStatus.Complete))
								{
									table.Add(new ChartDataSeries.Pt(xfr.Timestamp, (xfr.Type == ETransfer.Deposit ? +1 : -1) * (double)(decimal)xfr.Amount));
								}
								foreach (var trade in trade_history.Where(x => x.CoinIn == sym || x.CoinOut == sym))
								{
									// If 'sym' was the coin that was sold
									if (sym == trade.CoinIn)
										table.Add(new ChartDataSeries.Pt(trade.Timestamp, -(double)(decimal)trade.VolumeIn));
									
									// If 'sym' was the coin that was bought
									if (sym == trade.CoinOut)
										table.Add(new ChartDataSeries.Pt(trade.Timestamp, +(double)(decimal)trade.VolumeNett));
								}

								// Add entries for the start time and the current time
								table.Add(new ChartDataSeries.Pt(now, 0.0));
								table.Add(new ChartDataSeries.Pt(0, 0.0));

								// Sort the balance deltas by time
								table.SortI();

								// Encompass the actual time range
								equity.TimeRange.Encompass(table[1].xi);
								equity.TimeRange.Encompass(now);
							}
						}

						// For each coin, convert the balance deltas to absolute balances
						foreach (var sym in balances.Keys)
						{
							// Abort if another update is pending
							if (m_data_update.Pending)
								return;

							var series = equity[sym];
							using (var table = series.Lock())
							{
								// Set the time stamp of the first point to the same start time for all currencies
								table[0].xi = equity.TimeRange.Beg;

								// Start with the current balance and work backwards converting
								// the deltas to absolute amounts, and the timestamps to TimeScale units.
								var delta = 0.0;
								var bal = balances[sym];
								var value = use_assigned_values ? assigned_values[sym] : 1.0;
								for (int i = table.Count; i-- != 0;)
								{
									delta = table[i].yf;
									table[i].xf = Misc.TicksToTimeFrame(table[i].xi - equity.TimeRange.Beg, time_scale);
									table[i].yf = bal * value;

									// Adjust the balance by the balance delta
									bal -= delta;
								}
							}
						}

						// Generate the 'Nett' equity data as the sum of the equity of each currency using assigned values
						var sum = new List<ChartDataSeries.Pt>();
						foreach (var sym in balances.Keys)
						{
							var series = equity[sym];
							using (var sym_table = series.Lock())
							{
								// Merge the data sets
								sum = Enumerable_.ZipDistinct(sum, sym_table.Data,
									new ChartDataSeries.Pt(0.0f,0.0f),
									(l,r) => new ChartDataSeries.Pt(r.xf, l.yf + r.yf),
									(l,r) => l.xf.CompareTo(r.xf))
									.ToList();
							}
						}
						var nett = equity["Nett"];
						using (var nett_table = nett.Lock())
							nett_table.Data.AddRange(sum);

						// Update the equity data
						this.BeginInvoke(() =>
						{
							if (m_data_update.Pending) return;
							Data = equity;
						});
					}
					catch (Exception ex)
					{
						Model.Log.Write(ELogLevel.Error, ex, "Unhandled exception in equity data update");
					}
				});
			}
		}
		private Trigger m_data_update;

		/// <summary>A map from coin symbol to equity</summary>
		public class EquityMap :IDisposable
		{
			private readonly EquityUI m_ui;
			private readonly LazyDictionary<string, ChartDataSeries> m_map;

			public EquityMap(EquityUI ui)
			{
				m_ui = ui;
				m_map = new LazyDictionary<string, ChartDataSeries>(x =>
				{
					var s = new ChartDataSeries(x, new ChartDataSeries.OptionsData(m_ui.SeriesOptionsTemplate));
					s.Options.Colour = ChartDataSeries.GenerateColour(m_map.Count);
					s.Options.PlotType = ChartDataSeries.EPlotType.StepLine;
					return s;
				});
				TimeRange = Range.Invalid;
			}
			public void Dispose()
			{
				Util.DisposeAll(m_map.Values);
				m_map.Clear();
			}

			/// <summary>The range of times covered (in ticks)</summary>
			public Range TimeRange;

			/// <summary>Reset all data</summary>
			public void Reset()
			{
				m_map.Clear();
				TimeRange = Range.Invalid;
			}

			/// <summary>Return the number of symbols with data</summary>
			public int Count
			{
				get { return m_map.Count; }
			}

			/// <summary>Access the chart data for a given coin symbol</summary>
			public ChartDataSeries this[string sym]
			{
				get { return m_map[sym]; }
			}

			/// <summary>Enumerable coin symbols</summary>
			public IEnumerable<string> Keys
			{
				get { return m_map.Keys; }
			}

			/// <summary>Enumerable data series'</summary>
			public IEnumerable<ChartDataSeries> Values
			{
				get { return m_map.Values; }
			}
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EquityUI));
			pr.gui.ChartControl.RdrOptions rdrOptions1 = new pr.gui.ChartControl.RdrOptions();
			System.Drawing.Drawing2D.Matrix matrix1 = new System.Drawing.Drawing2D.Matrix();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_chart = new pr.gui.ChartControl();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_lbl_exchange = new System.Windows.Forms.ToolStripLabel();
			this.m_cb_exchange = new System.Windows.Forms.ToolStripComboBox();
			this.m_chk_common_value = new System.Windows.Forms.ToolStripButton();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_btn_combined = new System.Windows.Forms.ToolStripButton();
			this.m_table0.SuspendLayout();
			this.m_ts.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_table0
			// 
			this.m_table0.BackColor = System.Drawing.SystemColors.Control;
			this.m_table0.ColumnCount = 1;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Controls.Add(this.m_chart, 0, 1);
			this.m_table0.Controls.Add(this.m_ts, 0, 0);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 2;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Size = new System.Drawing.Size(826, 286);
			this.m_table0.TabIndex = 1;
			// 
			// m_chart
			// 
			this.m_chart.AllowEditing = false;
			this.m_chart.AllowSelection = false;
			this.m_chart.AreaSelectMode = pr.gui.ChartControl.EAreaSelectMode.Zoom;
			this.m_chart.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.m_chart.CrossHairLocation = ((System.Drawing.PointF)(resources.GetObject("m_chart.CrossHairLocation")));
			this.m_chart.CrossHairVisible = false;
			this.m_chart.DefaultKeyboardShortcuts = false;
			this.m_chart.DefaultMouseControl = true;
			this.m_chart.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_chart.Location = new System.Drawing.Point(3, 28);
			this.m_chart.Name = "m_chart";
			rdrOptions1.AntiAliasing = true;
			rdrOptions1.BkColour = System.Drawing.SystemColors.Control;
			rdrOptions1.ChartBkColour = System.Drawing.Color.White;
			rdrOptions1.CullMode = pr.view3d.View3d.ECullMode.Back;
			rdrOptions1.FillMode = pr.view3d.View3d.EFillMode.Solid;
			rdrOptions1.GridZOffset = 0.001F;
			rdrOptions1.LockAspect = null;
			rdrOptions1.Margin = new System.Windows.Forms.Padding(3);
			rdrOptions1.MinDragPixelDistance = 5F;
			rdrOptions1.MinSelectionDistance = 10F;
			rdrOptions1.NavigationMode = pr.gui.ChartControl.ENavMode.Chart2D;
			rdrOptions1.NoteFont = new System.Drawing.Font("Tahoma", 8F);
			rdrOptions1.Orthographic = false;
			rdrOptions1.PerpendicularZTranslation = false;
			rdrOptions1.ResetForward = ((pr.maths.v4)(resources.GetObject("rdrOptions1.ResetForward")));
			rdrOptions1.ResetUp = ((pr.maths.v4)(resources.GetObject("rdrOptions1.ResetUp")));
			rdrOptions1.SelectionColour = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
			rdrOptions1.ShowAxes = true;
			rdrOptions1.ShowGridLines = true;
			rdrOptions1.TitleColour = System.Drawing.Color.Black;
			rdrOptions1.TitleFont = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
			rdrOptions1.TitleTransform = matrix1;
			this.m_chart.Options = rdrOptions1;
			this.m_chart.Size = new System.Drawing.Size(820, 255);
			this.m_chart.TabIndex = 0;
			// 
			// m_ts
			// 
			this.m_ts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_ts.AutoSize = false;
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lbl_exchange,
            this.m_cb_exchange,
            this.m_chk_common_value,
            this.m_btn_combined});
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(826, 25);
			this.m_ts.TabIndex = 1;
			this.m_ts.Text = "toolStrip1";
			// 
			// m_lbl_exchange
			// 
			this.m_lbl_exchange.Name = "m_lbl_exchange";
			this.m_lbl_exchange.Size = new System.Drawing.Size(60, 22);
			this.m_lbl_exchange.Text = "Exchange:";
			// 
			// m_cb_exchange
			// 
			this.m_cb_exchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_exchange.Name = "m_cb_exchange";
			this.m_cb_exchange.Size = new System.Drawing.Size(121, 25);
			// 
			// m_chk_common_value
			// 
			this.m_chk_common_value.CheckOnClick = true;
			this.m_chk_common_value.Image = ((System.Drawing.Image)(resources.GetObject("m_chk_common_value.Image")));
			this.m_chk_common_value.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_chk_common_value.Name = "m_chk_common_value";
			this.m_chk_common_value.Size = new System.Drawing.Size(109, 22);
			this.m_chk_common_value.Text = "Common Value";
			// 
			// m_btn_combined
			// 
			this.m_btn_combined.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_combined.Image")));
			this.m_btn_combined.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_combined.Name = "m_btn_combined";
			this.m_btn_combined.Size = new System.Drawing.Size(50, 22);
			this.m_btn_combined.Text = "Nett";
			// 
			// EquityUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.ControlDark;
			this.Controls.Add(this.m_table0);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "EquityUI";
			this.Size = new System.Drawing.Size(826, 286);
			this.m_table0.ResumeLayout(false);
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
