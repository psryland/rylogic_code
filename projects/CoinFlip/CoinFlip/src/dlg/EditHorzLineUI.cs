using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

namespace CoinFlip
{
	public class EditHorzLineUI :ToolForm
	{
		#region UI Elements
		private Label m_lbl_region_colour;
		private ColourWheel m_colour_region;
		private Label m_lbl_line_width;
		private Label m_lbl_line_colour;
		private ColourWheel m_colour_line;
		private Label m_lbl_region_height;
		private Label m_lbl_price;
		private Button m_btn_delete;
		private ValueBox m_tb_price_level;
		private ValueBox m_tb_region_size;
		private ValueBox m_tb_line_width;
		private ToolTip m_tt;
		private Button m_btn_ok;
		#endregion

		public EditHorzLineUI(ChartUI chart, IndicatorHorzLine line)
			:base(chart, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = false;
			Chart = chart;
			Line = line;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The indicator whose settings we're editing</summary>
		public IndicatorHorzLine Line { get; private set; }

		/// <summary>The chart that the indicator is on</summary>
		public ChartUI Chart { get; private set; }

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			// Price level
			m_tb_price_level.ToolTip(m_tt, "The vertical position of the line");
			m_tb_price_level.ValueType = typeof(decimal);
			m_tb_price_level.ValidateText = t => decimal.TryParse(t, out var y) && y > 0m;
			m_tb_price_level.Value = Line.Price;
			m_tb_price_level.ValueChanged += (s,a) =>
			{
				Line.Price = (decimal)m_tb_price_level.Value;
			};

			// Region Size
			m_tb_region_size.ToolTip(m_tt, "The width of the region surrounding the line");
			m_tb_region_size.ValueType = typeof(double);
			m_tb_region_size.ValidateText = t => double.TryParse(t, out var w) && w >= 0f;
			m_tb_region_size.Value = Line.Settings.RegionWidth;
			m_tb_region_size.ValueChanged += (s,a) =>
			{
				Line.Settings.RegionWidth = (double)m_tb_region_size.Value;
			};

			// Line width
			m_tb_line_width.ToolTip(m_tt, "The width of the line");
			m_tb_line_width.ValueType = typeof(double);
			m_tb_line_width.ValidateText = t => double.TryParse(t, out var w) && w > 0f;
			m_tb_line_width.Value = Line.Settings.Width;
			m_tb_line_width.ValueChanged += (s,a) =>
			{
				Line.Settings.Width = (double)m_tb_line_width.Value;
			};

			// Colour Line
			m_colour_line.Colour = Line.Settings.Colour;
			m_colour_line.ColourChanged += (s,a) =>
			{
				Line.Settings.Colour = m_colour_line.Colour;
			};

			// Colour region
			m_colour_region.Colour = Line.Settings.RegionColour;
			m_colour_region.ColourChanged += (s,a) =>
			{
				Line.Settings.RegionColour = m_colour_region.Colour;
			};

			// Remove this indicator from the chart
			m_btn_delete.Click += (s,a) =>
			{
				Chart.Indicators.Remove(Line);
				Close();
			};
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_lbl_region_colour = new System.Windows.Forms.Label();
			this.m_colour_region = new Rylogic.Gui.ColourWheel();
			this.m_lbl_line_width = new System.Windows.Forms.Label();
			this.m_lbl_line_colour = new System.Windows.Forms.Label();
			this.m_colour_line = new Rylogic.Gui.ColourWheel();
			this.m_lbl_region_height = new System.Windows.Forms.Label();
			this.m_lbl_price = new System.Windows.Forms.Label();
			this.m_btn_delete = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_tb_price_level = new Rylogic.Gui.ValueBox();
			this.m_tb_region_size = new Rylogic.Gui.ValueBox();
			this.m_tb_line_width = new Rylogic.Gui.ValueBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// m_lbl_region_colour
			// 
			this.m_lbl_region_colour.AutoSize = true;
			this.m_lbl_region_colour.Location = new System.Drawing.Point(7, 160);
			this.m_lbl_region_colour.Name = "m_lbl_region_colour";
			this.m_lbl_region_colour.Size = new System.Drawing.Size(77, 13);
			this.m_lbl_region_colour.TabIndex = 39;
			this.m_lbl_region_colour.Text = "Region Colour:";
			// 
			// m_colour_region
			// 
			this.m_colour_region.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_region.Location = new System.Drawing.Point(96, 160);
			this.m_colour_region.Name = "m_colour_region";
			this.m_colour_region.Parts = ((Rylogic.Gui.ColourWheel.EParts)((((((Rylogic.Gui.ColourWheel.EParts.Wheel | Rylogic.Gui.ColourWheel.EParts.VSlider) 
            | Rylogic.Gui.ColourWheel.EParts.ASlider) 
            | Rylogic.Gui.ColourWheel.EParts.ColourSelection) 
            | Rylogic.Gui.ColourWheel.EParts.VSelection) 
            | Rylogic.Gui.ColourWheel.EParts.ASelection)));
			this.m_colour_region.Size = new System.Drawing.Size(120, 63);
			this.m_colour_region.SliderWidth = 20;
			this.m_colour_region.TabIndex = 38;
			this.m_colour_region.VerticalLayout = false;
			// 
			// m_lbl_line_width
			// 
			this.m_lbl_line_width.AutoSize = true;
			this.m_lbl_line_width.Location = new System.Drawing.Point(26, 67);
			this.m_lbl_line_width.Name = "m_lbl_line_width";
			this.m_lbl_line_width.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_line_width.TabIndex = 36;
			this.m_lbl_line_width.Text = "Line Width:";
			// 
			// m_lbl_line_colour
			// 
			this.m_lbl_line_colour.AutoSize = true;
			this.m_lbl_line_colour.Location = new System.Drawing.Point(21, 91);
			this.m_lbl_line_colour.Name = "m_lbl_line_colour";
			this.m_lbl_line_colour.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_line_colour.TabIndex = 35;
			this.m_lbl_line_colour.Text = "Line Colour:";
			// 
			// m_colour_line
			// 
			this.m_colour_line.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_line.Location = new System.Drawing.Point(96, 91);
			this.m_colour_line.Name = "m_colour_line";
			this.m_colour_line.Parts = ((Rylogic.Gui.ColourWheel.EParts)((((((Rylogic.Gui.ColourWheel.EParts.Wheel | Rylogic.Gui.ColourWheel.EParts.VSlider) 
            | Rylogic.Gui.ColourWheel.EParts.ASlider) 
            | Rylogic.Gui.ColourWheel.EParts.ColourSelection) 
            | Rylogic.Gui.ColourWheel.EParts.VSelection) 
            | Rylogic.Gui.ColourWheel.EParts.ASelection)));
			this.m_colour_line.Size = new System.Drawing.Size(120, 63);
			this.m_colour_line.SliderWidth = 20;
			this.m_colour_line.TabIndex = 34;
			this.m_colour_line.VerticalLayout = false;
			// 
			// m_lbl_region_height
			// 
			this.m_lbl_region_height.AutoSize = true;
			this.m_lbl_region_height.Location = new System.Drawing.Point(20, 41);
			this.m_lbl_region_height.Name = "m_lbl_region_height";
			this.m_lbl_region_height.Size = new System.Drawing.Size(67, 13);
			this.m_lbl_region_height.TabIndex = 33;
			this.m_lbl_region_height.Text = "Region Size:";
			// 
			// m_lbl_price
			// 
			this.m_lbl_price.AutoSize = true;
			this.m_lbl_price.Location = new System.Drawing.Point(21, 14);
			this.m_lbl_price.Name = "m_lbl_price";
			this.m_lbl_price.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_price.TabIndex = 30;
			this.m_lbl_price.Text = "Price Level:";
			// 
			// m_btn_delete
			// 
			this.m_btn_delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_delete.Location = new System.Drawing.Point(12, 232);
			this.m_btn_delete.Name = "m_btn_delete";
			this.m_btn_delete.Size = new System.Drawing.Size(75, 23);
			this.m_btn_delete.TabIndex = 29;
			this.m_btn_delete.Text = "Delete";
			this.m_btn_delete.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(141, 232);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 28;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_tb_price_level
			// 
			this.m_tb_price_level.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_price_level.BackColor = System.Drawing.Color.White;
			this.m_tb_price_level.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_price_level.BackColorValid = System.Drawing.Color.White;
			this.m_tb_price_level.CommitValueOnFocusLost = true;
			this.m_tb_price_level.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_price_level.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_price_level.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_price_level.Location = new System.Drawing.Point(93, 11);
			this.m_tb_price_level.Name = "m_tb_price_level";
			this.m_tb_price_level.Size = new System.Drawing.Size(123, 20);
			this.m_tb_price_level.TabIndex = 40;
			this.m_tb_price_level.UseValidityColours = true;
			this.m_tb_price_level.Value = null;
			// 
			// m_tb_region_size
			// 
			this.m_tb_region_size.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_region_size.BackColor = System.Drawing.Color.White;
			this.m_tb_region_size.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_region_size.BackColorValid = System.Drawing.Color.White;
			this.m_tb_region_size.CommitValueOnFocusLost = true;
			this.m_tb_region_size.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_region_size.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_region_size.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_region_size.Location = new System.Drawing.Point(93, 38);
			this.m_tb_region_size.Name = "m_tb_region_size";
			this.m_tb_region_size.Size = new System.Drawing.Size(123, 20);
			this.m_tb_region_size.TabIndex = 41;
			this.m_tb_region_size.UseValidityColours = true;
			this.m_tb_region_size.Value = null;
			// 
			// m_tb_line_width
			// 
			this.m_tb_line_width.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_line_width.BackColor = System.Drawing.Color.White;
			this.m_tb_line_width.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_line_width.BackColorValid = System.Drawing.Color.White;
			this.m_tb_line_width.CommitValueOnFocusLost = true;
			this.m_tb_line_width.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_line_width.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_line_width.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_line_width.Location = new System.Drawing.Point(93, 64);
			this.m_tb_line_width.Name = "m_tb_line_width";
			this.m_tb_line_width.Size = new System.Drawing.Size(123, 20);
			this.m_tb_line_width.TabIndex = 42;
			this.m_tb_line_width.UseValidityColours = true;
			this.m_tb_line_width.Value = null;
			// 
			// EditHorzLineUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(228, 267);
			this.Controls.Add(this.m_tb_line_width);
			this.Controls.Add(this.m_tb_region_size);
			this.Controls.Add(this.m_tb_price_level);
			this.Controls.Add(this.m_lbl_region_colour);
			this.Controls.Add(this.m_colour_region);
			this.Controls.Add(this.m_lbl_line_width);
			this.Controls.Add(this.m_lbl_line_colour);
			this.Controls.Add(this.m_colour_line);
			this.Controls.Add(this.m_lbl_region_height);
			this.Controls.Add(this.m_lbl_price);
			this.Controls.Add(this.m_btn_delete);
			this.Controls.Add(this.m_btn_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "EditHorzLineUI";
			this.Text = "Support/Resistance Level";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
