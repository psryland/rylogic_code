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
			m_spinner_width.Minimum = 0;
			m_spinner_width.Maximum = 50;
			m_spinner_width.Value = Maths.Clamp(MA.Settings.Width, m_spinner_width.Minimum, m_spinner_width.Maximum);
			m_spinner_width.ValueChanged += (s,a) =>
			{
				MA.Settings.Width = (int)m_spinner_width.Value;
			};

			// MA Colour
			m_colour_ema.Colour = MA.Settings.ColourMA;
			m_colour_ema.ColourChanged += (s,a) =>
			{
				MA.Settings.ColourMA = m_colour_ema.Colour;
			};

			// Bollinger Bands
			m_spinner_bollinger_band_stddev.Minimum = 0;
			m_spinner_bollinger_band_stddev.Maximum = 5;
			m_spinner_bollinger_band_stddev.Value = Maths.Clamp((decimal)MA.Settings.BollingerBands, m_spinner_bollinger_band_stddev.Minimum, m_spinner_bollinger_band_stddev.Maximum);
			m_spinner_bollinger_band_stddev.ValueChanged += (s,a) =>
			{
				MA.Settings.BollingerBands = (float)m_spinner_bollinger_band_stddev.Value;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditMaUI));
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
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_window_size)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_width)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_bollinger_band_stddev)).BeginInit();
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
			this.m_lbl_window_size.Size = new System.Drawing.Size(69, 13);
			this.m_lbl_window_size.TabIndex = 1;
			this.m_lbl_window_size.Text = "Window Size";
			// 
			// m_lbl_width
			// 
			this.m_lbl_width.AutoSize = true;
			this.m_lbl_width.Location = new System.Drawing.Point(116, 14);
			this.m_lbl_width.Name = "m_lbl_width";
			this.m_lbl_width.Size = new System.Drawing.Size(35, 13);
			this.m_lbl_width.TabIndex = 2;
			this.m_lbl_width.Text = "Width";
			// 
			// m_spinner_width
			// 
			this.m_spinner_width.Location = new System.Drawing.Point(130, 30);
			this.m_spinner_width.Name = "m_spinner_width";
			this.m_spinner_width.Size = new System.Drawing.Size(79, 20);
			this.m_spinner_width.TabIndex = 4;
			// 
			// m_colour_ema
			// 
			this.m_colour_ema.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_ema.Location = new System.Drawing.Point(89, 56);
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
			this.m_btn_ok.Location = new System.Drawing.Point(146, 238);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 6;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_bollinger_bands
			// 
			this.m_lbl_bollinger_bands.AutoSize = true;
			this.m_lbl_bollinger_bands.Location = new System.Drawing.Point(12, 140);
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
			this.m_spinner_bollinger_band_stddev.Location = new System.Drawing.Point(146, 138);
			this.m_spinner_bollinger_band_stddev.Name = "m_spinner_bollinger_band_stddev";
			this.m_spinner_bollinger_band_stddev.Size = new System.Drawing.Size(63, 20);
			this.m_spinner_bollinger_band_stddev.TabIndex = 8;
			// 
			// m_colour_bollinger
			// 
			this.m_colour_bollinger.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_bollinger.Location = new System.Drawing.Point(89, 164);
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
			this.m_lbl_main_colour.Location = new System.Drawing.Point(12, 56);
			this.m_lbl_main_colour.Name = "m_lbl_main_colour";
			this.m_lbl_main_colour.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_main_colour.TabIndex = 10;
			this.m_lbl_main_colour.Text = "EMA Colour";
			// 
			// m_lbl_bollinger_bands_colour
			// 
			this.m_lbl_bollinger_bands_colour.AutoSize = true;
			this.m_lbl_bollinger_bands_colour.Location = new System.Drawing.Point(11, 164);
			this.m_lbl_bollinger_bands_colour.Name = "m_lbl_bollinger_bands_colour";
			this.m_lbl_bollinger_bands_colour.Size = new System.Drawing.Size(70, 26);
			this.m_lbl_bollinger_bands_colour.TabIndex = 11;
			this.m_lbl_bollinger_bands_colour.Text = "Bollinger\r\nBands Colour";
			// 
			// m_btn_delete
			// 
			this.m_btn_delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_delete.Location = new System.Drawing.Point(12, 236);
			this.m_btn_delete.Name = "m_btn_delete";
			this.m_btn_delete.Size = new System.Drawing.Size(75, 23);
			this.m_btn_delete.TabIndex = 12;
			this.m_btn_delete.Text = "Delete";
			this.m_btn_delete.UseVisualStyleBackColor = true;
			// 
			// m_spinner_bollinger_bands
			// 
			this.m_spinner_bollinger_bands.Location = new System.Drawing.Point(0, 0);
			this.m_spinner_bollinger_bands.Name = "m_spinner_bollinger_bands";
			this.m_spinner_bollinger_bands.Size = new System.Drawing.Size(100, 20);
			this.m_spinner_bollinger_bands.TabIndex = 0;
			this.m_spinner_bollinger_bands.Value = null;
			// 
			// EditEmaUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(231, 271);
			this.Controls.Add(this.m_btn_delete);
			this.Controls.Add(this.m_lbl_bollinger_bands_colour);
			this.Controls.Add(this.m_lbl_main_colour);
			this.Controls.Add(this.m_colour_bollinger);
			this.Controls.Add(this.m_spinner_bollinger_band_stddev);
			this.Controls.Add(this.m_lbl_bollinger_bands);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_colour_ema);
			this.Controls.Add(this.m_spinner_width);
			this.Controls.Add(this.m_lbl_width);
			this.Controls.Add(this.m_lbl_window_size);
			this.Controls.Add(this.m_spinner_window_size);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditEmaUI";
			this.PinOffset = new System.Drawing.Point(-300, 0);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Exponential Moving Average";
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_window_size)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_width)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_bollinger_band_stddev)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
