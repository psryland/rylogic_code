using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public class EditMaUI :ToolForm
	{
		#region UI Elements
		private NumericUpDown m_spinner_window_size;
		private Label m_lbl_window_size;
		private Label m_lbl_width;
		private NumericUpDown m_spinner_width;
		private ColourWheel m_colour_ema;
		private Button m_btn_ok;
		private ValueBox m_spinner_bollinger_bands;
		private Label m_lbl_bollinger_bands;
		private ColourWheel m_colour_bollinger;
		private Label m_lbl_main_colour;
		private Label m_lbl_bollinger_bands_colour;
		private Button m_btn_delete;
		private CheckBox m_chk_exponential_ma;
		private CheckedGroupBox m_grp_bollinger_bands;
		private Label m_lbl_x_offset;
		private NumericUpDown m_spinner_xoffset;
		private NumericUpDown m_spinner_bollinger_band_stddev;
		#endregion

		public EditMaUI(ChartUI chart, IndicatorMA ma)
			:base(chart, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = false;
			Chart = chart;
			MA = ma;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The indicator whose settings we're editing</summary>
		public IndicatorMA MA { get; private set; }

		/// <summary>The chart that 'm_ema' is on</summary>
		public ChartUI Chart { get; private set; }

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			// Window Size
			m_spinner_window_size.Set(MA.Settings.WindowSize, 1, IndicatorMA.SettingsData.MaxWindowSize);
			m_spinner_window_size.ValueChanged += (s,a) =>
			{
				MA.Settings.WindowSize = (int)m_spinner_window_size.Value;
			};

			// Width
			m_spinner_width.Set(MA.Settings.Width, 0, 50);
			m_spinner_width.ValueChanged += (s,a) =>
			{
				MA.Settings.Width = (int)m_spinner_width.Value;
			};

			// X Offset
			m_spinner_xoffset.Increment = 0.1m;
			m_spinner_xoffset.DecimalPlaces = 1;
			m_spinner_xoffset.Set((decimal)MA.Settings.XOffset, -1000, +1000);
			m_spinner_xoffset.ValueChanged += (s,a) =>
			{
				MA.Settings.XOffset = (float)m_spinner_xoffset.Value;
			};

			// Exponential
			m_chk_exponential_ma.Checked = MA.Settings.ExponentialMA;
			m_chk_exponential_ma.CheckedChanged += (s,a) =>
			{
				MA.Settings.ExponentialMA = m_chk_exponential_ma.Checked;
			};

			// MA Colour
			m_colour_ema.Colour = MA.Settings.ColourMA;
			m_colour_ema.ColourChanged += (s,a) =>
			{
				MA.Settings.ColourMA = m_colour_ema.Colour;
			};

			// Group Bollinger Bands
			m_grp_bollinger_bands.Checked = MA.Settings.ShowBollingerBands;
			m_grp_bollinger_bands.CheckedChanged += (s,a) =>
			{
				MA.Settings.ShowBollingerBands = m_grp_bollinger_bands.Checked;
			};

			// Bollinger Bands
			m_spinner_bollinger_band_stddev.Set((decimal)MA.Settings.BollingerBandsStdDev, 0, 5);
			m_spinner_bollinger_band_stddev.ValueChanged += (s,a) =>
			{
				MA.Settings.BollingerBandsStdDev = (float)m_spinner_bollinger_band_stddev.Value;
			};

			// Bollinger Colour
			m_colour_bollinger.Colour = MA.Settings.ColourBollingerBands;
			m_colour_bollinger.ColourChanged += (s,a) =>
			{
				MA.Settings.ColourBollingerBands = m_colour_bollinger.Colour;
			};

			// Delete button
			m_btn_delete.Click += (s,a) =>
			{
				Chart.Indicators.Remove(MA);
				Close();
			};
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_spinner_window_size = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_window_size = new System.Windows.Forms.Label();
			this.m_lbl_width = new System.Windows.Forms.Label();
			this.m_spinner_width = new System.Windows.Forms.NumericUpDown();
			this.m_colour_ema = new pr.gui.ColourWheel();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_bollinger_bands = new System.Windows.Forms.Label();
			this.m_spinner_bollinger_band_stddev = new System.Windows.Forms.NumericUpDown();
			this.m_colour_bollinger = new pr.gui.ColourWheel();
			this.m_lbl_main_colour = new System.Windows.Forms.Label();
			this.m_lbl_bollinger_bands_colour = new System.Windows.Forms.Label();
			this.m_btn_delete = new System.Windows.Forms.Button();
			this.m_spinner_bollinger_bands = new pr.gui.ValueBox();
			this.m_chk_exponential_ma = new System.Windows.Forms.CheckBox();
			this.m_grp_bollinger_bands = new pr.gui.CheckedGroupBox();
			this.m_lbl_x_offset = new System.Windows.Forms.Label();
			this.m_spinner_xoffset = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_window_size)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_width)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_bollinger_band_stddev)).BeginInit();
			this.m_grp_bollinger_bands.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_xoffset)).BeginInit();
			this.SuspendLayout();
			// 
			// m_spinner_window_size
			// 
			this.m_spinner_window_size.Location = new System.Drawing.Point(27, 30);
			this.m_spinner_window_size.Name = "m_spinner_window_size";
			this.m_spinner_window_size.Size = new System.Drawing.Size(75, 20);
			this.m_spinner_window_size.TabIndex = 0;
			// 
			// m_lbl_window_size
			// 
			this.m_lbl_window_size.AutoSize = true;
			this.m_lbl_window_size.Location = new System.Drawing.Point(12, 14);
			this.m_lbl_window_size.Name = "m_lbl_window_size";
			this.m_lbl_window_size.Size = new System.Drawing.Size(72, 13);
			this.m_lbl_window_size.TabIndex = 1;
			this.m_lbl_window_size.Text = "Window Size:";
			// 
			// m_lbl_width
			// 
			this.m_lbl_width.AutoSize = true;
			this.m_lbl_width.Location = new System.Drawing.Point(12, 53);
			this.m_lbl_width.Name = "m_lbl_width";
			this.m_lbl_width.Size = new System.Drawing.Size(38, 13);
			this.m_lbl_width.TabIndex = 2;
			this.m_lbl_width.Text = "Width:";
			// 
			// m_spinner_width
			// 
			this.m_spinner_width.Location = new System.Drawing.Point(27, 69);
			this.m_spinner_width.Name = "m_spinner_width";
			this.m_spinner_width.Size = new System.Drawing.Size(75, 20);
			this.m_spinner_width.TabIndex = 4;
			// 
			// m_colour_ema
			// 
			this.m_colour_ema.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_ema.Location = new System.Drawing.Point(144, 53);
			this.m_colour_ema.Name = "m_colour_ema";
			this.m_colour_ema.Parts = ((pr.gui.ColourWheel.EParts)((((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider) 
            | pr.gui.ColourWheel.EParts.ASlider) 
            | pr.gui.ColourWheel.EParts.ColourSelection) 
            | pr.gui.ColourWheel.EParts.VSelection) 
            | pr.gui.ColourWheel.EParts.ASelection)));
			this.m_colour_ema.Size = new System.Drawing.Size(120, 63);
			this.m_colour_ema.SliderWidth = 20;
			this.m_colour_ema.TabIndex = 5;
			this.m_colour_ema.VerticalLayout = false;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(211, 252);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 6;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_bollinger_bands
			// 
			this.m_lbl_bollinger_bands.AutoSize = true;
			this.m_lbl_bollinger_bands.Location = new System.Drawing.Point(6, 28);
			this.m_lbl_bollinger_bands.Name = "m_lbl_bollinger_bands";
			this.m_lbl_bollinger_bands.Size = new System.Drawing.Size(117, 13);
			this.m_lbl_bollinger_bands.TabIndex = 7;
			this.m_lbl_bollinger_bands.Text = "Bollinger Band StdDev:";
			// 
			// m_spinner_bollinger_band_stddev
			// 
			this.m_spinner_bollinger_band_stddev.DecimalPlaces = 1;
			this.m_spinner_bollinger_band_stddev.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this.m_spinner_bollinger_band_stddev.Location = new System.Drawing.Point(15, 44);
			this.m_spinner_bollinger_band_stddev.Name = "m_spinner_bollinger_band_stddev";
			this.m_spinner_bollinger_band_stddev.Size = new System.Drawing.Size(63, 20);
			this.m_spinner_bollinger_band_stddev.TabIndex = 8;
			// 
			// m_colour_bollinger
			// 
			this.m_colour_bollinger.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_bollinger.Location = new System.Drawing.Point(132, 32);
			this.m_colour_bollinger.Name = "m_colour_bollinger";
			this.m_colour_bollinger.Parts = ((pr.gui.ColourWheel.EParts)((((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider) 
            | pr.gui.ColourWheel.EParts.ASlider) 
            | pr.gui.ColourWheel.EParts.ColourSelection) 
            | pr.gui.ColourWheel.EParts.VSelection) 
            | pr.gui.ColourWheel.EParts.ASelection)));
			this.m_colour_bollinger.Size = new System.Drawing.Size(120, 63);
			this.m_colour_bollinger.SliderWidth = 20;
			this.m_colour_bollinger.TabIndex = 9;
			this.m_colour_bollinger.VerticalLayout = false;
			// 
			// m_lbl_main_colour
			// 
			this.m_lbl_main_colour.AutoSize = true;
			this.m_lbl_main_colour.Location = new System.Drawing.Point(141, 37);
			this.m_lbl_main_colour.Name = "m_lbl_main_colour";
			this.m_lbl_main_colour.Size = new System.Drawing.Size(56, 13);
			this.m_lbl_main_colour.TabIndex = 10;
			this.m_lbl_main_colour.Text = "MA Colour";
			// 
			// m_lbl_bollinger_bands_colour
			// 
			this.m_lbl_bollinger_bands_colour.AutoSize = true;
			this.m_lbl_bollinger_bands_colour.Location = new System.Drawing.Point(129, 16);
			this.m_lbl_bollinger_bands_colour.Name = "m_lbl_bollinger_bands_colour";
			this.m_lbl_bollinger_bands_colour.Size = new System.Drawing.Size(113, 13);
			this.m_lbl_bollinger_bands_colour.TabIndex = 11;
			this.m_lbl_bollinger_bands_colour.Text = "Bollinger Bands Colour";
			// 
			// m_btn_delete
			// 
			this.m_btn_delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_delete.Location = new System.Drawing.Point(12, 252);
			this.m_btn_delete.Name = "m_btn_delete";
			this.m_btn_delete.Size = new System.Drawing.Size(75, 23);
			this.m_btn_delete.TabIndex = 12;
			this.m_btn_delete.Text = "Delete";
			this.m_btn_delete.UseVisualStyleBackColor = true;
			// 
			// m_spinner_bollinger_bands
			// 
			this.m_spinner_bollinger_bands.BackColorInvalid = System.Drawing.Color.White;
			this.m_spinner_bollinger_bands.BackColorValid = System.Drawing.Color.White;
			this.m_spinner_bollinger_bands.CommitValueOnFocusLost = true;
			this.m_spinner_bollinger_bands.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_spinner_bollinger_bands.ForeColorValid = System.Drawing.Color.Black;
			this.m_spinner_bollinger_bands.Location = new System.Drawing.Point(0, 0);
			this.m_spinner_bollinger_bands.Name = "m_spinner_bollinger_bands";
			this.m_spinner_bollinger_bands.Size = new System.Drawing.Size(100, 20);
			this.m_spinner_bollinger_bands.TabIndex = 0;
			this.m_spinner_bollinger_bands.UseValidityColours = true;
			this.m_spinner_bollinger_bands.Value = null;
			// 
			// m_chk_exponential_ma
			// 
			this.m_chk_exponential_ma.AutoSize = true;
			this.m_chk_exponential_ma.Location = new System.Drawing.Point(122, 13);
			this.m_chk_exponential_ma.Name = "m_chk_exponential_ma";
			this.m_chk_exponential_ma.Size = new System.Drawing.Size(162, 17);
			this.m_chk_exponential_ma.TabIndex = 13;
			this.m_chk_exponential_ma.Text = "Exponential Moving Average";
			this.m_chk_exponential_ma.UseVisualStyleBackColor = true;
			// 
			// m_grp_bollinger_bands
			// 
			this.m_grp_bollinger_bands.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grp_bollinger_bands.Controls.Add(this.m_colour_bollinger);
			this.m_grp_bollinger_bands.Controls.Add(this.m_spinner_bollinger_band_stddev);
			this.m_grp_bollinger_bands.Controls.Add(this.m_lbl_bollinger_bands);
			this.m_grp_bollinger_bands.Controls.Add(this.m_lbl_bollinger_bands_colour);
			this.m_grp_bollinger_bands.Enabled = false;
			this.m_grp_bollinger_bands.Location = new System.Drawing.Point(12, 134);
			this.m_grp_bollinger_bands.Name = "m_grp_bollinger_bands";
			this.m_grp_bollinger_bands.Size = new System.Drawing.Size(274, 112);
			this.m_grp_bollinger_bands.TabIndex = 14;
			this.m_grp_bollinger_bands.TabStop = false;
			this.m_grp_bollinger_bands.Text = "Bollinger Bands";
			// 
			// m_lbl_x_offset
			// 
			this.m_lbl_x_offset.AutoSize = true;
			this.m_lbl_x_offset.Location = new System.Drawing.Point(12, 92);
			this.m_lbl_x_offset.Name = "m_lbl_x_offset";
			this.m_lbl_x_offset.Size = new System.Drawing.Size(48, 13);
			this.m_lbl_x_offset.TabIndex = 16;
			this.m_lbl_x_offset.Text = "X Offset:";
			// 
			// m_spinner_xoffset
			// 
			this.m_spinner_xoffset.Location = new System.Drawing.Point(27, 108);
			this.m_spinner_xoffset.Name = "m_spinner_xoffset";
			this.m_spinner_xoffset.Size = new System.Drawing.Size(75, 20);
			this.m_spinner_xoffset.TabIndex = 17;
			// 
			// EditMaUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(296, 285);
			this.Controls.Add(this.m_spinner_xoffset);
			this.Controls.Add(this.m_lbl_x_offset);
			this.Controls.Add(this.m_grp_bollinger_bands);
			this.Controls.Add(this.m_chk_exponential_ma);
			this.Controls.Add(this.m_btn_delete);
			this.Controls.Add(this.m_lbl_main_colour);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_colour_ema);
			this.Controls.Add(this.m_spinner_width);
			this.Controls.Add(this.m_lbl_width);
			this.Controls.Add(this.m_lbl_window_size);
			this.Controls.Add(this.m_spinner_window_size);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditMaUI";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Exponential Moving Average";
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_window_size)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_width)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_bollinger_band_stddev)).EndInit();
			this.m_grp_bollinger_bands.ResumeLayout(false);
			this.m_grp_bollinger_bands.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_xoffset)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
