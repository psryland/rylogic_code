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

namespace Tradee
{
	public class EditSnrUI :ToolForm
	{
		private readonly SnR m_snr;

		#region UI Elements
		private Button m_btn_delete;
		private Label m_lbl_src_time_frame;
		private pr.gui.ComboBox m_cb_src_time_frame;
		private Label m_lbl_history_length;
		private NumericUpDown m_spinner_history_length;
		private ColourWheel m_colour;
		private Label m_lbl_colour;
		private pr.gui.ComboBox m_cb_attribute;
		private Label m_lbl_attribute;
		private Label m_lbl_gfx_width;
		private NumericUpDown m_spinner_gfx_width;
		private NumericUpDown m_spinner_power;
		private Label m_lbl_power;
		private NumericUpDown m_spinner_window_size;
		private Label m_lbl_window_size;
		private Button m_btn_ok;
		#endregion

		public EditSnrUI(ChartUI chart, SnR snr)
			:base(chart, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = false;
			Chart = chart;
			m_snr = snr;

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
			// Source time frame
			m_cb_src_time_frame.DataSource = Enum<ETimeFrame>.Values.Except(ETimeFrame.None).ToArray();
			m_cb_src_time_frame.SelectedItem = m_snr.Settings.SourceTimeFrame;
			m_cb_src_time_frame.SelectedIndexChanged += (s,a) =>
			{
				m_snr.Settings.SourceTimeFrame = (ETimeFrame)m_cb_src_time_frame.SelectedItem;
			};

			// Attribute
			m_cb_attribute.DataSource = Enum<SnR.EAttribute>.ValuesArray;
			m_cb_attribute.SelectedIndexChanged += (s,a) =>
			{
				m_snr.Settings.Attribute = (SnR.EAttribute)m_cb_attribute.SelectedItem;
			};

			// History length
			m_spinner_history_length.Minimum = 1;
			m_spinner_history_length.Maximum = SnRSettings.MaxHistoryLength;
			m_spinner_history_length.Value = Maths.Clamp(m_snr.Settings.HistoryLength, m_spinner_history_length.Minimum, m_spinner_history_length.Maximum);
			m_spinner_history_length.ValueChanged += (s,a) =>
			{
				m_snr.Settings.HistoryLength = (int)m_spinner_history_length.Value;
			};

			m_spinner_window_size.Minimum = 1;
			m_spinner_window_size.Maximum = SnRSettings.MaxWindowSize;
			m_spinner_window_size.Value = Maths.Clamp(m_snr.Settings.WindowSize, m_spinner_window_size.Minimum, m_spinner_window_size.Maximum);
			m_spinner_window_size.ValueChanged += (s,a) =>
			{
				m_snr.Settings.WindowSize = (int)m_spinner_window_size.Value;
			};

			// Colour of the graphics
			m_colour.Colour = m_snr.Settings.Colour;
			m_colour.ColourChanged += (s,a) =>
			{
				m_snr.Settings.Colour = m_colour.Colour;
			};

			// Graphics width
			m_spinner_gfx_width.Minimum = 0.01m;
			m_spinner_gfx_width.Maximum = 1.00m;
			m_spinner_gfx_width.Value = (decimal)m_snr.Settings.GraphicsWidth;
			m_spinner_gfx_width.ValueChanged += (s,a) =>
			{
				m_snr.Settings.GraphicsWidth = (float)m_spinner_gfx_width.Value;
			};

			// Power
			m_spinner_power.Minimum = 1;
			m_spinner_power.Maximum = 100;
			m_spinner_power.Value = (decimal)m_snr.Settings.Power;
			m_spinner_power.ValueChanged += (s,a) =>
			{
				m_snr.Settings.Power = (float)m_spinner_power.Value;
			};

			// Remove this indicator from the chart
			m_btn_delete.Click += (s,a) =>
			{
				Chart.Indicators.Remove(m_snr);
				Close();
			};
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_btn_delete = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_src_time_frame = new System.Windows.Forms.Label();
			this.m_cb_src_time_frame = new pr.gui.ComboBox();
			this.m_lbl_history_length = new System.Windows.Forms.Label();
			this.m_spinner_history_length = new System.Windows.Forms.NumericUpDown();
			this.m_colour = new pr.gui.ColourWheel();
			this.m_lbl_colour = new System.Windows.Forms.Label();
			this.m_cb_attribute = new pr.gui.ComboBox();
			this.m_lbl_attribute = new System.Windows.Forms.Label();
			this.m_lbl_gfx_width = new System.Windows.Forms.Label();
			this.m_spinner_gfx_width = new System.Windows.Forms.NumericUpDown();
			this.m_spinner_power = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_power = new System.Windows.Forms.Label();
			this.m_spinner_window_size = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_window_size = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_history_length)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_gfx_width)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_power)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_window_size)).BeginInit();
			this.SuspendLayout();
			// 
			// m_btn_delete
			// 
			this.m_btn_delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_delete.Location = new System.Drawing.Point(12, 240);
			this.m_btn_delete.Name = "m_btn_delete";
			this.m_btn_delete.Size = new System.Drawing.Size(75, 23);
			this.m_btn_delete.TabIndex = 14;
			this.m_btn_delete.Text = "Delete";
			this.m_btn_delete.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(144, 240);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 13;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_src_time_frame
			// 
			this.m_lbl_src_time_frame.AutoSize = true;
			this.m_lbl_src_time_frame.Location = new System.Drawing.Point(9, 12);
			this.m_lbl_src_time_frame.Name = "m_lbl_src_time_frame";
			this.m_lbl_src_time_frame.Size = new System.Drawing.Size(102, 13);
			this.m_lbl_src_time_frame.TabIndex = 15;
			this.m_lbl_src_time_frame.Text = "Source Time Frame:";
			// 
			// m_cb_src_time_frame
			// 
			this.m_cb_src_time_frame.DisplayProperty = null;
			this.m_cb_src_time_frame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_src_time_frame.FormattingEnabled = true;
			this.m_cb_src_time_frame.Location = new System.Drawing.Point(117, 9);
			this.m_cb_src_time_frame.Name = "m_cb_src_time_frame";
			this.m_cb_src_time_frame.PreserveSelectionThruFocusChange = false;
			this.m_cb_src_time_frame.Size = new System.Drawing.Size(102, 21);
			this.m_cb_src_time_frame.TabIndex = 16;
			// 
			// m_lbl_history_length
			// 
			this.m_lbl_history_length.AutoSize = true;
			this.m_lbl_history_length.Location = new System.Drawing.Point(33, 65);
			this.m_lbl_history_length.Name = "m_lbl_history_length";
			this.m_lbl_history_length.Size = new System.Drawing.Size(78, 13);
			this.m_lbl_history_length.TabIndex = 17;
			this.m_lbl_history_length.Text = "History Length:";
			// 
			// m_spinner_history_length
			// 
			this.m_spinner_history_length.Location = new System.Drawing.Point(117, 63);
			this.m_spinner_history_length.Name = "m_spinner_history_length";
			this.m_spinner_history_length.Size = new System.Drawing.Size(102, 20);
			this.m_spinner_history_length.TabIndex = 18;
			// 
			// m_colour
			// 
			this.m_colour.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour.Location = new System.Drawing.Point(99, 119);
			this.m_colour.Name = "m_colour";
			this.m_colour.Parts = ((pr.gui.ColourWheel.EParts)((((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider) 
            | pr.gui.ColourWheel.EParts.ASlider) 
            | pr.gui.ColourWheel.EParts.ColourSelection) 
            | pr.gui.ColourWheel.EParts.VSelection) 
            | pr.gui.ColourWheel.EParts.ASelection)));
			this.m_colour.Size = new System.Drawing.Size(120, 63);
			this.m_colour.SliderWidth = 20;
			this.m_colour.TabIndex = 19;
			this.m_colour.VerticalLayout = false;
			// 
			// m_lbl_colour
			// 
			this.m_lbl_colour.AutoSize = true;
			this.m_lbl_colour.Location = new System.Drawing.Point(47, 119);
			this.m_lbl_colour.Name = "m_lbl_colour";
			this.m_lbl_colour.Size = new System.Drawing.Size(40, 13);
			this.m_lbl_colour.TabIndex = 20;
			this.m_lbl_colour.Text = "Colour:";
			// 
			// m_cb_attribute
			// 
			this.m_cb_attribute.DisplayProperty = null;
			this.m_cb_attribute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_attribute.FormattingEnabled = true;
			this.m_cb_attribute.Location = new System.Drawing.Point(117, 36);
			this.m_cb_attribute.Name = "m_cb_attribute";
			this.m_cb_attribute.PreserveSelectionThruFocusChange = false;
			this.m_cb_attribute.Size = new System.Drawing.Size(102, 21);
			this.m_cb_attribute.TabIndex = 21;
			// 
			// m_lbl_attribute
			// 
			this.m_lbl_attribute.AutoSize = true;
			this.m_lbl_attribute.Location = new System.Drawing.Point(62, 39);
			this.m_lbl_attribute.Name = "m_lbl_attribute";
			this.m_lbl_attribute.Size = new System.Drawing.Size(49, 13);
			this.m_lbl_attribute.TabIndex = 22;
			this.m_lbl_attribute.Text = "Attribute:";
			// 
			// m_lbl_gfx_width
			// 
			this.m_lbl_gfx_width.AutoSize = true;
			this.m_lbl_gfx_width.Location = new System.Drawing.Point(73, 190);
			this.m_lbl_gfx_width.Name = "m_lbl_gfx_width";
			this.m_lbl_gfx_width.Size = new System.Drawing.Size(38, 13);
			this.m_lbl_gfx_width.TabIndex = 23;
			this.m_lbl_gfx_width.Text = "Width:";
			// 
			// m_spinner_gfx_width
			// 
			this.m_spinner_gfx_width.DecimalPlaces = 2;
			this.m_spinner_gfx_width.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this.m_spinner_gfx_width.Location = new System.Drawing.Point(117, 188);
			this.m_spinner_gfx_width.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.m_spinner_gfx_width.Name = "m_spinner_gfx_width";
			this.m_spinner_gfx_width.Size = new System.Drawing.Size(102, 20);
			this.m_spinner_gfx_width.TabIndex = 24;
			this.m_spinner_gfx_width.Value = new decimal(new int[] {
            2,
            0,
            0,
            65536});
			// 
			// m_spinner_power
			// 
			this.m_spinner_power.DecimalPlaces = 1;
			this.m_spinner_power.Location = new System.Drawing.Point(117, 214);
			this.m_spinner_power.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.m_spinner_power.Name = "m_spinner_power";
			this.m_spinner_power.Size = new System.Drawing.Size(102, 20);
			this.m_spinner_power.TabIndex = 26;
			this.m_spinner_power.Value = new decimal(new int[] {
            2,
            0,
            0,
            65536});
			// 
			// m_lbl_power
			// 
			this.m_lbl_power.AutoSize = true;
			this.m_lbl_power.Location = new System.Drawing.Point(71, 216);
			this.m_lbl_power.Name = "m_lbl_power";
			this.m_lbl_power.Size = new System.Drawing.Size(40, 13);
			this.m_lbl_power.TabIndex = 25;
			this.m_lbl_power.Text = "Power:";
			// 
			// m_spinner_window_size
			// 
			this.m_spinner_window_size.Location = new System.Drawing.Point(117, 89);
			this.m_spinner_window_size.Name = "m_spinner_window_size";
			this.m_spinner_window_size.Size = new System.Drawing.Size(102, 20);
			this.m_spinner_window_size.TabIndex = 27;
			// 
			// m_lbl_window_size
			// 
			this.m_lbl_window_size.AutoSize = true;
			this.m_lbl_window_size.Location = new System.Drawing.Point(33, 91);
			this.m_lbl_window_size.Name = "m_lbl_window_size";
			this.m_lbl_window_size.Size = new System.Drawing.Size(72, 13);
			this.m_lbl_window_size.TabIndex = 28;
			this.m_lbl_window_size.Text = "Window Size:";
			// 
			// EditSnrUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(231, 275);
			this.Controls.Add(this.m_lbl_window_size);
			this.Controls.Add(this.m_spinner_window_size);
			this.Controls.Add(this.m_spinner_power);
			this.Controls.Add(this.m_lbl_power);
			this.Controls.Add(this.m_spinner_gfx_width);
			this.Controls.Add(this.m_lbl_gfx_width);
			this.Controls.Add(this.m_lbl_attribute);
			this.Controls.Add(this.m_cb_attribute);
			this.Controls.Add(this.m_lbl_colour);
			this.Controls.Add(this.m_colour);
			this.Controls.Add(this.m_spinner_history_length);
			this.Controls.Add(this.m_lbl_history_length);
			this.Controls.Add(this.m_cb_src_time_frame);
			this.Controls.Add(this.m_lbl_src_time_frame);
			this.Controls.Add(this.m_btn_delete);
			this.Controls.Add(this.m_btn_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "EditSnrUI";
			this.PinOffset = new System.Drawing.Point(-300, 0);
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Support & Resistance";
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_history_length)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_gfx_width)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_power)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_window_size)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
