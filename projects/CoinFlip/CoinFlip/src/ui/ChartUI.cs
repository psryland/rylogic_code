using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.gfx;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;
using ToolStripComboBox = pr.gui.ToolStripComboBox;
using pr.extn;
using System.Drawing;

namespace CoinFlip
{
	public class ChartUI :BaseUI
	{
		/// <summary>A cache of the candle graphics</summary>
		private const int GfxModelBatchSize = 1024;
		private Cache<int, CachedGfx> m_gfx_cache;

		/// <summary>Buffers for creating the chart graphics</summary>
		private List<View3d.Vertex> m_vbuf;
		private List<ushort>        m_ibuf;
		private List<View3d.Nugget> m_nbuf;

		#region UI Elements
		private ToolStripContainer m_tsc;
		private ToolStripLabel m_lbl_pair;
		private ToolStripComboBox m_cb_pair;
		private ToolStripSeparator m_sep0;
		private ToolStrip m_ts;
		private ToolStripSplitButton m_splitbtn_timeframes;
		private ChartControl m_chart;
		#endregion

		public ChartUI(Model model)
			:base(model, "Chart")
		{
			InitializeComponent();

			m_vbuf = new List<View3d.Vertex>();
			m_ibuf = new List<ushort>();
			m_nbuf = new List<View3d.Nugget>();
			m_gfx_cache = new Cache<int, CachedGfx>();

			m_chart = new ChartControl(string.Empty, new ChartControl.RdrOptions());
			Pairs = new BindingSource<TradePair>{ DataSource = Model.Pairs.Distinct().ToArray() };

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			ChartSettings = null;
			Instrument = null;
			Util.Dispose(ref m_gfx_cache);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Settings for this chart instance</summary>
		public Settings.ChartSettings ChartSettings { get; private set; }

		/// <summary>The available pairs</summary>
		private BindingSource<TradePair> Pairs { get; set; }

		/// <summary>The currency pair displayed in the chart</summary>
		private Instrument Instrument
		{
			get { return m_instrument; }
			set
			{
				if (m_instrument == value) return;
				if (m_instrument != null)
				{
					Util.Dispose(ref m_instrument);
				}
				m_instrument = value;
				if (m_instrument != null)
				{
				}
			}
		}
		private void SetInstrument(TradePair pair)
		{
			// Clear the previous instrument and settings
			Instrument = null;
			ChartSettings = null;

			// Create the new instrument
			Instrument = new Instrument(Model, pair.Name);

			// Find the chart settings associated with this instrument
			var settings = Model.Settings.Charts.FirstOrDefault(x => x.SymbolCode == pair.Name);
			if (settings == null)
			{
				settings = new Settings.ChartSettings(pair.Name);
				Model.Settings.Charts = Model.Settings.Charts.Concat(settings).ToArray();
			}
			ChartSettings = settings;
			m_chart.Options = settings.Style;
		}
		private Instrument m_instrument;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Tool bar
			m_cb_pair.ToolTipText = "Select the currency pair to display";
			m_cb_pair.ComboBox.DataSource = Pairs;
			m_cb_pair.SelectedIndexChanged += (s,a) =>
			{
				SetInstrument((TradePair)m_cb_pair.SelectedItem);
			};
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
			//m_chart.ChartRendering      += HandleChartRendering;
			//m_chart.KeyDown             += HandleChartKeyDown;
			//m_chart.MouseDown           += HandleChartMouseDown;
			//m_chart.AddOverlaysOnPaint  += HandleAddOverlaysOnPaint;
			//m_chart.AddUserMenuOptions  += HandleChartCMenu;

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
			this.m_splitbtn_timeframes = new System.Windows.Forms.ToolStripSplitButton();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ts.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(766, 712);
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
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_lbl_pair,
            this.m_cb_pair,
            this.m_sep0,
            this.m_splitbtn_timeframes});
			this.m_ts.Location = new System.Drawing.Point(3, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(312, 25);
			this.m_ts.TabIndex = 0;
			// 
			// m_lbl_pair
			// 
			this.m_lbl_pair.Name = "m_lbl_pair";
			this.m_lbl_pair.Size = new System.Drawing.Size(30, 22);
			this.m_lbl_pair.Text = "Pair:";
			// 
			// m_cb_pair
			// 
			this.m_cb_pair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.Size = new System.Drawing.Size(121, 25);
			// 
			// m_sep0
			// 
			this.m_sep0.Name = "m_sep0";
			this.m_sep0.Size = new System.Drawing.Size(6, 25);
			// 
			// m_splitbtn_timeframes
			// 
			this.m_splitbtn_timeframes.Image = ((System.Drawing.Image)(resources.GetObject("m_splitbtn_timeframes.Image")));
			this.m_splitbtn_timeframes.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_splitbtn_timeframes.Name = "m_splitbtn_timeframes";
			this.m_splitbtn_timeframes.Size = new System.Drawing.Size(141, 22);
			this.m_splitbtn_timeframes.Text = "Select Time Frames";
			this.m_splitbtn_timeframes.ToolTipText = "Select Time Frames\r\nShift-Click to insert a tool bar button\r\n";
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
			this.ResumeLayout(false);

		}
		#endregion
	}
}
