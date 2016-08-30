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
using pr.util;

namespace Tradee
{
	public class EditSnrLevelUI :ToolForm
	{
		private readonly SnRIndicator m_snr_level;

		#region UI Elements
		private Button m_btn_delete;
		private Label m_lbl_price;
		private NumericUpDown m_spinner_region_size;
		private NumericUpDown m_spinner_price_level;
		private Label m_lbl_region_height;
		private ColourWheel m_colour;
		private Label m_lbl_colour;
		private Label m_lbl_line_width;
		private NumericUpDown m_spinner_line_width;
		private Button m_btn_ok;
		#endregion

		public EditSnrLevelUI(ChartUI chart, SnRIndicator snr_level)
			:base(chart, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = false;
			Chart = chart;
			m_snr_level = snr_level;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The chart that 'm_ema' is on</summary>
		private ChartUI Chart { get; set; }

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			// Price level
			m_spinner_price_level.Minimum = 0;
			m_spinner_price_level.Maximum = decimal.MaxValue;
			m_spinner_price_level.DecimalPlaces = m_snr_level.Instrument.PriceData.DecimalPlaces;
			m_spinner_price_level.Increment = (decimal)m_snr_level.Instrument.PriceData.PipSize;
			m_spinner_price_level.Value = (decimal)m_snr_level.Level.Price;
			m_spinner_price_level.ValueChanged += (s,a) =>
			{
				m_snr_level.Level.Price = (double)m_spinner_price_level.Value;
			};

			// Region Size
			m_spinner_region_size.Minimum = 0;
			m_spinner_region_size.Maximum = decimal.MaxValue;
			m_spinner_region_size.Value = (decimal)m_snr_level.Level.WidthPips;
			m_spinner_region_size.ValueChanged += (s,a) =>
			{
				m_snr_level.Level.WidthPips = (double)m_spinner_region_size.Value;
			};

			// Line width
			m_spinner_line_width.Minimum = 1;
			m_spinner_line_width.Maximum = 500;
			m_spinner_line_width.Value = m_snr_level.Settings.LineWidth;
			m_spinner_line_width.ValueChanged += (s,a) =>
			{
				m_snr_level.Settings.LineWidth = (int)m_spinner_line_width.Value;
			};

			// Colour
			m_colour.Colour = m_snr_level.Settings.Colour;
			m_colour.ColourChanged += (s,a) =>
			{
				m_snr_level.Settings.Colour = m_colour.Colour;
				m_snr_level.Settings.RegionColour = m_colour.Colour.Alpha(0.5f);
			};

			// Remove this indicator from the chart
			m_btn_delete.Click += (s,a) =>
			{
				Chart.Indicators.Remove(m_snr_level);
				Close();
			};
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_btn_delete = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_price = new System.Windows.Forms.Label();
			this.m_spinner_region_size = new System.Windows.Forms.NumericUpDown();
			this.m_spinner_price_level = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_region_height = new System.Windows.Forms.Label();
			this.m_colour = new pr.gui.ColourWheel();
			this.m_lbl_colour = new System.Windows.Forms.Label();
			this.m_lbl_line_width = new System.Windows.Forms.Label();
			this.m_spinner_line_width = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_region_size)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_price_level)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_line_width)).BeginInit();
			this.SuspendLayout();
			// 
			// m_btn_delete
			// 
			this.m_btn_delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_delete.Location = new System.Drawing.Point(12, 170);
			this.m_btn_delete.Name = "m_btn_delete";
			this.m_btn_delete.Size = new System.Drawing.Size(75, 23);
			this.m_btn_delete.TabIndex = 16;
			this.m_btn_delete.Text = "Delete";
			this.m_btn_delete.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(144, 170);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 15;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_price
			// 
			this.m_lbl_price.AutoSize = true;
			this.m_lbl_price.Location = new System.Drawing.Point(50, 15);
			this.m_lbl_price.Name = "m_lbl_price";
			this.m_lbl_price.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_price.TabIndex = 18;
			this.m_lbl_price.Text = "Price Level:";
			// 
			// m_spinner_region_size
			// 
			this.m_spinner_region_size.Location = new System.Drawing.Point(119, 37);
			this.m_spinner_region_size.Name = "m_spinner_region_size";
			this.m_spinner_region_size.Size = new System.Drawing.Size(100, 20);
			this.m_spinner_region_size.TabIndex = 19;
			// 
			// m_spinner_price_level
			// 
			this.m_spinner_price_level.Location = new System.Drawing.Point(119, 13);
			this.m_spinner_price_level.Name = "m_spinner_price_level";
			this.m_spinner_price_level.Size = new System.Drawing.Size(100, 20);
			this.m_spinner_price_level.TabIndex = 20;
			// 
			// m_lbl_region_height
			// 
			this.m_lbl_region_height.AutoSize = true;
			this.m_lbl_region_height.Location = new System.Drawing.Point(7, 39);
			this.m_lbl_region_height.Name = "m_lbl_region_height";
			this.m_lbl_region_height.Size = new System.Drawing.Size(106, 13);
			this.m_lbl_region_height.TabIndex = 21;
			this.m_lbl_region_height.Text = "Region Size (in pips):";
			// 
			// m_colour
			// 
			this.m_colour.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour.Location = new System.Drawing.Point(99, 89);
			this.m_colour.Name = "m_colour";
			this.m_colour.Parts = ((pr.gui.ColourWheel.EParts)((((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider) 
            | pr.gui.ColourWheel.EParts.ASlider) 
            | pr.gui.ColourWheel.EParts.ColourSelection) 
            | pr.gui.ColourWheel.EParts.VSelection) 
            | pr.gui.ColourWheel.EParts.ASelection)));
			this.m_colour.Size = new System.Drawing.Size(120, 63);
			this.m_colour.SliderWidth = 20;
			this.m_colour.TabIndex = 22;
			this.m_colour.VerticalLayout = false;
			// 
			// m_lbl_colour
			// 
			this.m_lbl_colour.AutoSize = true;
			this.m_lbl_colour.Location = new System.Drawing.Point(50, 90);
			this.m_lbl_colour.Name = "m_lbl_colour";
			this.m_lbl_colour.Size = new System.Drawing.Size(40, 13);
			this.m_lbl_colour.TabIndex = 23;
			this.m_lbl_colour.Text = "Colour:";
			// 
			// m_lbl_line_width
			// 
			this.m_lbl_line_width.AutoSize = true;
			this.m_lbl_line_width.Location = new System.Drawing.Point(52, 65);
			this.m_lbl_line_width.Name = "m_lbl_line_width";
			this.m_lbl_line_width.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_line_width.TabIndex = 24;
			this.m_lbl_line_width.Text = "Line Width:";
			// 
			// m_spinner_line_width
			// 
			this.m_spinner_line_width.Location = new System.Drawing.Point(119, 63);
			this.m_spinner_line_width.Name = "m_spinner_line_width";
			this.m_spinner_line_width.Size = new System.Drawing.Size(100, 20);
			this.m_spinner_line_width.TabIndex = 25;
			// 
			// EditSnrLevelUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(231, 205);
			this.Controls.Add(this.m_spinner_line_width);
			this.Controls.Add(this.m_lbl_line_width);
			this.Controls.Add(this.m_lbl_colour);
			this.Controls.Add(this.m_colour);
			this.Controls.Add(this.m_lbl_region_height);
			this.Controls.Add(this.m_spinner_price_level);
			this.Controls.Add(this.m_spinner_region_size);
			this.Controls.Add(this.m_lbl_price);
			this.Controls.Add(this.m_btn_delete);
			this.Controls.Add(this.m_btn_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "EditSnrLevelUI";
			this.PinOffset = new System.Drawing.Point(-300, 0);
			this.Text = "Support/Resistance Level";
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_region_size)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_price_level)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_line_width)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
