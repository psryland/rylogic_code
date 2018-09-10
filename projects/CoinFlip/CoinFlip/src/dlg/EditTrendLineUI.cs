using System;
using System.Globalization;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;
using Util = Rylogic.Utility.Util;

namespace CoinFlip
{
	public class EditTrendLineUI :ToolForm
	{
		#region UI Elements
		private ValueBox m_tb_line_width;
		private Label m_lbl_line_width;
		private Label m_lbl_line_colour;
		private ColourWheel m_colour_line;
		private Button m_btn_delete;
		private Button m_btn_ok;
		private ToolTip m_tt;
		private ValueBox m_tb_beg_time;
		private ValueBox m_tb_beg_price;
		private Label m_lbl_start;
		private Label m_lbl_price;
		private Label m_lbl_start_time;
		private ValueBox m_tb_end_time;
		private ValueBox m_tb_end_price;
		private Label m_lbl_end;
		private CheckBox m_chk_extrapolate;
		#endregion

		public EditTrendLineUI(ChartUI chart, IndicatorTrendLine trend_line)
			:base(chart, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = false;
			Chart = chart;
			TrendLine = trend_line;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The indicator whose settings we're editing</summary>
		public IndicatorTrendLine TrendLine { get; private set; }

		/// <summary>The chart that the indicator is on</summary>
		public ChartUI Chart { get; private set; }

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			// X values
			m_tb_beg_time.ToolTip(m_tt, "The X-position of the start of the trend line");
			m_tb_end_time.ToolTip(m_tt, "The X-position of the end of the trend line");
			foreach (var tb in new[]{ m_tb_beg_time, m_tb_end_time })
			{
				tb.ValueType = typeof(long);
				tb.ValidateText = t =>
				{
					switch (Chart.XAxisLabelMode) {
					default: throw new Exception("unknown X-Axis label format");
					case EXAxisLabelMode.LocalTime:   return DateTimeOffset.TryParse(t, null, DateTimeStyles.AssumeLocal, out var dt1);
					case EXAxisLabelMode.UtcTime:     return DateTimeOffset.TryParse(t, null, DateTimeStyles.AssumeUniversal, out var dt0);
					case EXAxisLabelMode.CandleIndex: return double.TryParse(t, out var d);
					}
				};
				tb.TextToValue = t =>
				{
					switch (Chart.XAxisLabelMode) {
					default: throw new Exception("unknown X-Axis label format");
					case EXAxisLabelMode.LocalTime:   return DateTimeOffset.Parse(t, null, DateTimeStyles.AssumeLocal).Ticks;
					case EXAxisLabelMode.UtcTime:     return DateTimeOffset.Parse(t, null, DateTimeStyles.AssumeUniversal).Ticks;
					case EXAxisLabelMode.CandleIndex: return TrendLine.Instrument.TimeAtFIndex(double.Parse(t));
					}
				};
				tb.ValueToText = v =>
				{
					switch (Chart.XAxisLabelMode) {
					default: throw new Exception("unknown X-Axis label format");
					case EXAxisLabelMode.LocalTime:   return new DateTimeOffset((long)v, TimeSpan.Zero).ToLocalTime().ToString();
					case EXAxisLabelMode.UtcTime:     return new DateTimeOffset((long)v, TimeSpan.Zero).ToString();
					case EXAxisLabelMode.CandleIndex: return TrendLine.Instrument.FIndexAt(new TimeFrameTime((long)v, TrendLine.Instrument.TimeFrame)).ToString();
					}
				};
			}
			m_tb_beg_time.Value = TrendLine.Settings.BegX;
			m_tb_end_time.Value = TrendLine.Settings.EndX;
			m_tb_beg_time.ValueCommitted += (s,a) =>
			{
				TrendLine.Settings.BegX = (long)a.Value;
			};
			m_tb_end_time.ValueCommitted += (s,a) =>
			{
				TrendLine.Settings.EndX = (long)a.Value;
			};

			// Y Values
			m_tb_beg_price.ToolTip(m_tt, "The Y-position of the start of the trend line");
			m_tb_end_price.ToolTip(m_tt, "The Y-position of the end of the trend line");
			foreach (var tb in new[]{ m_tb_beg_price, m_tb_end_price })
			{
				tb.ValueType = typeof(decimal);
				tb.ValidateText = t => decimal.TryParse(t, out var p) && p >= 0m;
			}
			m_tb_beg_price.Value = TrendLine.Settings.BegY;
			m_tb_end_price.Value = TrendLine.Settings.EndY;
			m_tb_beg_price.ValueCommitted += (s,a) =>
			{
				TrendLine.Settings.BegY = (decimal)a.Value;
			};
			m_tb_end_price.ValueCommitted += (s,a) =>
			{
				TrendLine.Settings.EndY = (decimal)a.Value;
			};

			// Extrapolate
			m_chk_extrapolate.ToolTip(m_tt, "Extend the trend-line beyond the end point");
			m_chk_extrapolate.Checked = TrendLine.Settings.Extrapolate;
			m_chk_extrapolate.CheckedChanged += (s,a) =>
			{
				TrendLine.Settings.Extrapolate = m_chk_extrapolate.Checked;
			};

			// Line width
			m_tb_line_width.ToolTip(m_tt, "The width of the line");
			m_tb_line_width.ValueType = typeof(double);
			m_tb_line_width.ValidateText = t => double.TryParse(t, out var w) && w > 0f;
			m_tb_line_width.Value = TrendLine.Settings.Width;
			m_tb_line_width.ValueChanged += (s,a) =>
			{
				TrendLine.Settings.Width = (double)m_tb_line_width.Value;
			};

			// Colour Line
			m_colour_line.Colour = TrendLine.Settings.Colour;
			m_colour_line.ColourChanged += (s,a) =>
			{
				TrendLine.Settings.Colour = m_colour_line.Colour;
			};

			// Remove this indicator from the chart
			m_btn_delete.Click += (s,a) =>
			{
				Chart.Indicators.Remove(TrendLine);
				Close();
			};
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_tb_line_width = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_line_width = new System.Windows.Forms.Label();
			this.m_lbl_line_colour = new System.Windows.Forms.Label();
			this.m_colour_line = new Rylogic.Gui.WinForms.ColourWheel();
			this.m_btn_delete = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_tb_beg_time = new Rylogic.Gui.WinForms.ValueBox();
			this.m_tb_beg_price = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_start = new System.Windows.Forms.Label();
			this.m_lbl_price = new System.Windows.Forms.Label();
			this.m_lbl_start_time = new System.Windows.Forms.Label();
			this.m_tb_end_time = new Rylogic.Gui.WinForms.ValueBox();
			this.m_tb_end_price = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_end = new System.Windows.Forms.Label();
			this.m_chk_extrapolate = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// m_tb_line_width
			// 
			this.m_tb_line_width.BackColor = System.Drawing.Color.White;
			this.m_tb_line_width.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_line_width.BackColorValid = System.Drawing.Color.White;
			this.m_tb_line_width.CommitValueOnFocusLost = true;
			this.m_tb_line_width.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_line_width.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_line_width.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_line_width.Location = new System.Drawing.Point(88, 106);
			this.m_tb_line_width.Name = "m_tb_line_width";
			this.m_tb_line_width.Size = new System.Drawing.Size(122, 20);
			this.m_tb_line_width.TabIndex = 5;
			this.m_tb_line_width.UseValidityColours = true;
			this.m_tb_line_width.Value = null;
			// 
			// m_lbl_line_width
			// 
			this.m_lbl_line_width.AutoSize = true;
			this.m_lbl_line_width.Location = new System.Drawing.Point(21, 109);
			this.m_lbl_line_width.Name = "m_lbl_line_width";
			this.m_lbl_line_width.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_line_width.TabIndex = 47;
			this.m_lbl_line_width.Text = "Line Width:";
			// 
			// m_lbl_line_colour
			// 
			this.m_lbl_line_colour.AutoSize = true;
			this.m_lbl_line_colour.Location = new System.Drawing.Point(21, 133);
			this.m_lbl_line_colour.Name = "m_lbl_line_colour";
			this.m_lbl_line_colour.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_line_colour.TabIndex = 46;
			this.m_lbl_line_colour.Text = "Line Colour:";
			// 
			// m_colour_line
			// 
			this.m_colour_line.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_line.Location = new System.Drawing.Point(90, 133);
			this.m_colour_line.Name = "m_colour_line";
			this.m_colour_line.Parts = ((Rylogic.Gui.WinForms.ColourWheel.EParts)((((((Rylogic.Gui.WinForms.ColourWheel.EParts.Wheel | Rylogic.Gui.WinForms.ColourWheel.EParts.VSlider) 
            | Rylogic.Gui.WinForms.ColourWheel.EParts.ASlider) 
            | Rylogic.Gui.WinForms.ColourWheel.EParts.ColourSelection) 
            | Rylogic.Gui.WinForms.ColourWheel.EParts.VSelection) 
            | Rylogic.Gui.WinForms.ColourWheel.EParts.ASelection)));
			this.m_colour_line.Size = new System.Drawing.Size(120, 63);
			this.m_colour_line.SliderWidth = 20;
			this.m_colour_line.TabIndex = 6;
			this.m_colour_line.VerticalLayout = false;
			// 
			// m_btn_delete
			// 
			this.m_btn_delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_delete.Location = new System.Drawing.Point(12, 220);
			this.m_btn_delete.Name = "m_btn_delete";
			this.m_btn_delete.Size = new System.Drawing.Size(75, 23);
			this.m_btn_delete.TabIndex = 7;
			this.m_btn_delete.Text = "Delete";
			this.m_btn_delete.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(178, 220);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 8;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_tb_beg_time
			// 
			this.m_tb_beg_time.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_beg_time.BackColor = System.Drawing.Color.White;
			this.m_tb_beg_time.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_beg_time.BackColorValid = System.Drawing.Color.White;
			this.m_tb_beg_time.CommitValueOnFocusLost = true;
			this.m_tb_beg_time.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_beg_time.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_beg_time.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_beg_time.Location = new System.Drawing.Point(44, 25);
			this.m_tb_beg_time.Name = "m_tb_beg_time";
			this.m_tb_beg_time.Size = new System.Drawing.Size(128, 20);
			this.m_tb_beg_time.TabIndex = 0;
			this.m_tb_beg_time.UseValidityColours = true;
			this.m_tb_beg_time.Value = null;
			// 
			// m_tb_beg_price
			// 
			this.m_tb_beg_price.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_beg_price.BackColor = System.Drawing.Color.White;
			this.m_tb_beg_price.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_beg_price.BackColorValid = System.Drawing.Color.White;
			this.m_tb_beg_price.CommitValueOnFocusLost = true;
			this.m_tb_beg_price.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_beg_price.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_beg_price.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_beg_price.Location = new System.Drawing.Point(178, 25);
			this.m_tb_beg_price.Name = "m_tb_beg_price";
			this.m_tb_beg_price.Size = new System.Drawing.Size(75, 20);
			this.m_tb_beg_price.TabIndex = 1;
			this.m_tb_beg_price.UseValidityColours = true;
			this.m_tb_beg_price.Value = null;
			// 
			// m_lbl_start
			// 
			this.m_lbl_start.AutoSize = true;
			this.m_lbl_start.Location = new System.Drawing.Point(6, 28);
			this.m_lbl_start.Name = "m_lbl_start";
			this.m_lbl_start.Size = new System.Drawing.Size(32, 13);
			this.m_lbl_start.TabIndex = 50;
			this.m_lbl_start.Text = "Start:";
			// 
			// m_lbl_price
			// 
			this.m_lbl_price.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_price.AutoSize = true;
			this.m_lbl_price.Location = new System.Drawing.Point(168, 9);
			this.m_lbl_price.Name = "m_lbl_price";
			this.m_lbl_price.Size = new System.Drawing.Size(31, 13);
			this.m_lbl_price.TabIndex = 49;
			this.m_lbl_price.Text = "Price";
			// 
			// m_lbl_start_time
			// 
			this.m_lbl_start_time.AutoSize = true;
			this.m_lbl_start_time.Location = new System.Drawing.Point(41, 9);
			this.m_lbl_start_time.Name = "m_lbl_start_time";
			this.m_lbl_start_time.Size = new System.Drawing.Size(30, 13);
			this.m_lbl_start_time.TabIndex = 53;
			this.m_lbl_start_time.Text = "Time";
			// 
			// m_tb_end_time
			// 
			this.m_tb_end_time.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_end_time.BackColor = System.Drawing.Color.White;
			this.m_tb_end_time.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_end_time.BackColorValid = System.Drawing.Color.White;
			this.m_tb_end_time.CommitValueOnFocusLost = true;
			this.m_tb_end_time.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_end_time.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_end_time.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_end_time.Location = new System.Drawing.Point(44, 51);
			this.m_tb_end_time.Name = "m_tb_end_time";
			this.m_tb_end_time.Size = new System.Drawing.Size(128, 20);
			this.m_tb_end_time.TabIndex = 2;
			this.m_tb_end_time.UseValidityColours = true;
			this.m_tb_end_time.Value = null;
			// 
			// m_tb_end_price
			// 
			this.m_tb_end_price.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_end_price.BackColor = System.Drawing.Color.White;
			this.m_tb_end_price.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_end_price.BackColorValid = System.Drawing.Color.White;
			this.m_tb_end_price.CommitValueOnFocusLost = true;
			this.m_tb_end_price.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_end_price.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_end_price.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_end_price.Location = new System.Drawing.Point(178, 51);
			this.m_tb_end_price.Name = "m_tb_end_price";
			this.m_tb_end_price.Size = new System.Drawing.Size(75, 20);
			this.m_tb_end_price.TabIndex = 3;
			this.m_tb_end_price.UseValidityColours = true;
			this.m_tb_end_price.Value = null;
			// 
			// m_lbl_end
			// 
			this.m_lbl_end.AutoSize = true;
			this.m_lbl_end.Location = new System.Drawing.Point(9, 54);
			this.m_lbl_end.Name = "m_lbl_end";
			this.m_lbl_end.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_end.TabIndex = 54;
			this.m_lbl_end.Text = "End:";
			// 
			// m_chk_extrapolate
			// 
			this.m_chk_extrapolate.AutoSize = true;
			this.m_chk_extrapolate.Location = new System.Drawing.Point(44, 77);
			this.m_chk_extrapolate.Name = "m_chk_extrapolate";
			this.m_chk_extrapolate.Size = new System.Drawing.Size(98, 17);
			this.m_chk_extrapolate.TabIndex = 4;
			this.m_chk_extrapolate.Text = "Extrapolate line";
			this.m_chk_extrapolate.UseVisualStyleBackColor = true;
			// 
			// EditTrendLineUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(265, 255);
			this.Controls.Add(this.m_chk_extrapolate);
			this.Controls.Add(this.m_tb_end_time);
			this.Controls.Add(this.m_tb_end_price);
			this.Controls.Add(this.m_lbl_end);
			this.Controls.Add(this.m_lbl_start_time);
			this.Controls.Add(this.m_tb_beg_time);
			this.Controls.Add(this.m_tb_beg_price);
			this.Controls.Add(this.m_lbl_start);
			this.Controls.Add(this.m_lbl_price);
			this.Controls.Add(this.m_tb_line_width);
			this.Controls.Add(this.m_lbl_line_width);
			this.Controls.Add(this.m_lbl_line_colour);
			this.Controls.Add(this.m_colour_line);
			this.Controls.Add(this.m_btn_delete);
			this.Controls.Add(this.m_btn_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimumSize = new System.Drawing.Size(241, 279);
			this.Name = "EditTrendLineUI";
			this.Text = "Trend Line";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
