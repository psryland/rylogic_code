using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.maths;
using pr.util;

namespace pr.gui
{
	public class ColourUI :Form
	{
		private Label m_lbl_red;
		private Label m_lbl_hex;
		private Label m_lbl_blue;
		private Label m_lbl_green;
		private Label m_lbl_hue;
		private Label m_lbl_sat;
		private Label m_lbl_lum;
		private Label m_lbl_alpha;
		private Label m_lbl_basic;
		private Label m_lbl_recent;
		private TextBox m_edit_red;
		private TextBox m_edit_green;
		private TextBox m_edit_blue;
		private TextBox m_edit_hex;
		private TextBox m_edit_hue;
		private TextBox m_edit_sat;
		private TextBox m_edit_lum;
		private TextBox m_edit_alpha;
		private ColourWheel m_wheel;
		private TableLayoutPanel m_table_basic;
		private TableLayoutPanel m_table_recent;
		private Button m_btn_ok;
		private Button m_btn_cancel;

		/// <summary>Recently selected colours</summary>
		private static Color[] m_recent = Array_.New(RecentColoursColumns*RecentColoursRows, i => Color.White);
		private const int RecentColoursColumns = 7;
		private const int RecentColoursRows = 2;

		private static readonly Color[] m_basic_colours = GenBasicColours().ToArray();
		private const int BasicColoursColumns = 7;
		private Label m_lbl_initial;
		private Panel m_panel_initial;
		private Panel m_panel_selected;
		private Label m_lbl_selected;
		private const int BasicColoursRows = 7;

		public ColourUI()
		{
			InitializeComponent();
			Debug.Assert(m_table_basic.RowCount     == BasicColoursRows    );
			Debug.Assert(m_table_basic.ColumnCount  == BasicColoursColumns );
			Debug.Assert(m_table_recent.RowCount    == RecentColoursRows   );
			Debug.Assert(m_table_recent.ColumnCount == RecentColoursColumns);

			SetupBasicColourTable();
			SetupRecentColourTable();
			SetupColourWheel();
			SetupFields();
			SetupColourPanels();

			ReadColourFieldsFromWheel();
		}

		/// <summary>The colour used for comparisons</summary>
		public Color InitialColour
		{
			get { return m_panel_initial.BackColor; }
			set { m_panel_initial.BackColor = value; }
		}

		/// <summary>The selected colour</summary>
		public Color Colour
		{
			get { return m_wheel.Colour; }
			set { m_wheel.Colour = value; ReadColourFieldsFromWheel(); }
		}

		/// <summary>Setup the basic colours grid</summary>
		private void SetupBasicColourTable()
		{
			m_table_basic.Controls.AddRange(
				m_basic_colours.Select(x =>
					{
						var panel = new Panel
							{
								Dock = DockStyle.Fill,
								BackColor = x,
								BorderStyle = BorderStyle.None,
								Margin = new Padding(1)
							};
						panel.Click += (s,a) => m_wheel.Colour = s.As<Panel>().BackColor;
						return panel;
					}).Cast<Control>().ToArray());
		}

		/// <summary>Setup the recent colours grid</summary>
		private void SetupRecentColourTable()
		{
			m_table_recent.Controls.AddRange(
			m_recent.Select(x =>
				{
					var panel = new Panel
						{
							Dock = DockStyle.Fill,
							BackColor = x,
							BorderStyle = BorderStyle.None,
							Margin = new Padding(1)
						};
					panel.Click += (s,a) => m_wheel.Colour = s.As<Panel>().BackColor;
					return panel;
				}).Cast<Control>().ToArray());
		}

		/// <summary>Wire up the colour wheel control</summary>
		private void SetupColourWheel()
		{
			m_wheel.ColourChanged += (s,a) =>
				{
					ReadColourFieldsFromWheel();
				};
		}

		/// <summary>Setup the text box fields</summary>
		private void SetupFields()
		{
			// Validation
			CancelEventHandler Validating = (s,a) =>
				{
					byte dummy;
					a.Cancel = !byte.TryParse(s.As<TextBox>().Text, out dummy);
				};
			m_edit_red  .Validating += Validating;
			m_edit_green.Validating += Validating;
			m_edit_blue .Validating += Validating;
			m_edit_alpha.Validating += Validating;
			m_edit_hue  .Validating += Validating;
			m_edit_sat  .Validating += Validating;
			m_edit_lum  .Validating += Validating;

			// Accept value
			Action<byte?,byte?,byte?,byte?> SetARGB = (a,r,g,b) =>
				{
					var c = m_wheel.Colour;
					m_wheel.Colour = Color.FromArgb(a ?? c.A, r ?? c.R, g ?? c.G, b ?? c.B);
				};
			Action<float?,float?,float?,float?> SetAHSV = (a,h,s,v) =>
				{
					var c = m_wheel.HSVColour;
					if (a.HasValue) a = Maths.Clamp(a.Value / 255f, 0f, 1f);
					if (h.HasValue) h = Maths.Clamp(h.Value / 255f, 0f, 1f);
					if (s.HasValue) s = Maths.Clamp(s.Value / 255f, 0f, 1f);
					if (v.HasValue) v = Maths.Clamp(v.Value / 255f, 0f, 1f);
					m_wheel.HSVColour = HSV.FromAHSV(a ?? c.A, h ?? c.H, s ?? c.S, v ?? c.V);
				};
			m_edit_alpha.Validated += (s,a) => SetARGB(byte.Parse(s.As<TextBox>().Text), null, null, null);
			m_edit_red  .Validated += (s,a) => SetARGB(null, byte.Parse(s.As<TextBox>().Text), null, null);
			m_edit_green.Validated += (s,a) => SetARGB(null, null, byte.Parse(s.As<TextBox>().Text), null);
			m_edit_blue .Validated += (s,a) => SetARGB(null, null, null, byte.Parse(s.As<TextBox>().Text));
			m_edit_hue  .Validated += (s,a) => SetAHSV(null, byte.Parse(s.As<TextBox>().Text), null, null);
			m_edit_sat  .Validated += (s,a) => SetAHSV(null, null, byte.Parse(s.As<TextBox>().Text), null);
			m_edit_lum  .Validated += (s,a) => SetAHSV(null, null, null, byte.Parse(s.As<TextBox>().Text));

			m_edit_hex.Validating += (s,a) =>
				{
					uint dummy;
					a.Cancel = !uint.TryParse(s.As<TextBox>().Text, NumberStyles.HexNumber, null, out dummy);
				};
			m_edit_hex.Validated += (s,a) =>
				{
					var argb = uint.Parse(s.As<TextBox>().Text, NumberStyles.HexNumber);
					unchecked { m_wheel.Colour = Color.FromArgb((int)argb); }
				};
		}

		/// <summary>Setup the Initial and Selected colour panels</summary>
		private void SetupColourPanels()
		{
			m_panel_initial.BackColor = m_recent[0];
			m_panel_initial.Click += (s,a) =>
				{
					m_wheel.Colour = m_panel_initial.BackColor;
				};
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			if (DialogResult != DialogResult.OK) return;
			m_recent = Util.AddToHistoryList(m_recent, Colour, false, m_recent.Length);
		}

		/// <summary>Set the values of the colour fields from the colour wheel</summary>
		private void ReadColourFieldsFromWheel()
		{
			var hsv = m_wheel.HSVColour;
			var rgb = m_wheel.Colour;

			m_edit_red  .Text = rgb.R.ToString(CultureInfo.InvariantCulture);
			m_edit_green.Text = rgb.G.ToString(CultureInfo.InvariantCulture);
			m_edit_blue .Text = rgb.B.ToString(CultureInfo.InvariantCulture);
			m_edit_alpha.Text = rgb.A.ToString(CultureInfo.InvariantCulture);
			m_edit_hue  .Text = Maths.Clamp((int)(255f*hsv.H),0,255).ToString(CultureInfo.InvariantCulture);
			m_edit_sat  .Text = Maths.Clamp((int)(255f*hsv.S),0,255).ToString(CultureInfo.InvariantCulture);
			m_edit_lum  .Text = Maths.Clamp((int)(255f*hsv.V),0,255).ToString(CultureInfo.InvariantCulture);

			unchecked
			{
				m_edit_hex.Text = ((uint)rgb.ToArgb()).ToString("X8");
			}

			m_panel_selected.BackColor = rgb;
		}

		/// <summary>Generates the basic colours</summary>
		private static IEnumerable<Color> GenBasicColours()
		{
			Debug.Assert(BasicColoursColumns == 7, "Expecting 7 combinations");

			//000 100 010 001 110 101 011
			for (var i = 0; i != BasicColoursRows; ++i)
			{
				var f = (float)i / (BasicColoursRows - 1);
				yield return Color.FromArgb(  0,  0,  0).Lerp(Color.FromArgb(255,255,255), f);
				yield return Color.FromArgb( 32,  0,  0).Lerp(Color.FromArgb(255,  0,  0), f);
				yield return Color.FromArgb(  0, 32,  0).Lerp(Color.FromArgb(  0,255,  0), f);
				yield return Color.FromArgb(  0,  0, 32).Lerp(Color.FromArgb(  0,  0,255), f);
				yield return Color.FromArgb( 32, 32,  0).Lerp(Color.FromArgb(255,255,  0), f);
				yield return Color.FromArgb( 32,  0, 32).Lerp(Color.FromArgb(255,  0,255), f);
				yield return Color.FromArgb(  0, 32, 32).Lerp(Color.FromArgb(  0,255,255), f);
			}
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_lbl_red = new System.Windows.Forms.Label();
			this.m_lbl_hex = new System.Windows.Forms.Label();
			this.m_lbl_blue = new System.Windows.Forms.Label();
			this.m_lbl_green = new System.Windows.Forms.Label();
			this.m_edit_red = new System.Windows.Forms.TextBox();
			this.m_edit_green = new System.Windows.Forms.TextBox();
			this.m_edit_blue = new System.Windows.Forms.TextBox();
			this.m_edit_hex = new System.Windows.Forms.TextBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_edit_hue = new System.Windows.Forms.TextBox();
			this.m_edit_sat = new System.Windows.Forms.TextBox();
			this.m_edit_lum = new System.Windows.Forms.TextBox();
			this.m_lbl_hue = new System.Windows.Forms.Label();
			this.m_lbl_sat = new System.Windows.Forms.Label();
			this.m_lbl_lum = new System.Windows.Forms.Label();
			this.m_edit_alpha = new System.Windows.Forms.TextBox();
			this.m_lbl_alpha = new System.Windows.Forms.Label();
			this.m_lbl_basic = new System.Windows.Forms.Label();
			this.m_table_basic = new System.Windows.Forms.TableLayoutPanel();
			this.m_lbl_recent = new System.Windows.Forms.Label();
			this.m_table_recent = new System.Windows.Forms.TableLayoutPanel();
			this.m_lbl_initial = new System.Windows.Forms.Label();
			this.m_panel_initial = new System.Windows.Forms.Panel();
			this.m_panel_selected = new System.Windows.Forms.Panel();
			this.m_lbl_selected = new System.Windows.Forms.Label();
			this.m_wheel = new pr.gui.ColourWheel();
			this.SuspendLayout();
			//
			// m_lbl_red
			//
			this.m_lbl_red.AutoSize = true;
			this.m_lbl_red.Location = new System.Drawing.Point(315, 159);
			this.m_lbl_red.Name = "m_lbl_red";
			this.m_lbl_red.Size = new System.Drawing.Size(30, 13);
			this.m_lbl_red.TabIndex = 1;
			this.m_lbl_red.Text = "&Red:";
			//
			// m_lbl_hex
			//
			this.m_lbl_hex.AutoSize = true;
			this.m_lbl_hex.Location = new System.Drawing.Point(265, 251);
			this.m_lbl_hex.Name = "m_lbl_hex";
			this.m_lbl_hex.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_hex.TabIndex = 2;
			this.m_lbl_hex.Text = "&Hex:";
			//
			// m_lbl_blue
			//
			this.m_lbl_blue.AutoSize = true;
			this.m_lbl_blue.Location = new System.Drawing.Point(314, 203);
			this.m_lbl_blue.Name = "m_lbl_blue";
			this.m_lbl_blue.Size = new System.Drawing.Size(31, 13);
			this.m_lbl_blue.TabIndex = 3;
			this.m_lbl_blue.Text = "&Blue:";
			//
			// m_lbl_green
			//
			this.m_lbl_green.AutoSize = true;
			this.m_lbl_green.Location = new System.Drawing.Point(306, 181);
			this.m_lbl_green.Name = "m_lbl_green";
			this.m_lbl_green.Size = new System.Drawing.Size(39, 13);
			this.m_lbl_green.TabIndex = 4;
			this.m_lbl_green.Text = "&Green:";
			//
			// m_edit_red
			//
			this.m_edit_red.Location = new System.Drawing.Point(351, 156);
			this.m_edit_red.Name = "m_edit_red";
			this.m_edit_red.Size = new System.Drawing.Size(45, 20);
			this.m_edit_red.TabIndex = 3;
			//
			// m_edit_green
			//
			this.m_edit_green.Location = new System.Drawing.Point(351, 178);
			this.m_edit_green.Name = "m_edit_green";
			this.m_edit_green.Size = new System.Drawing.Size(45, 20);
			this.m_edit_green.TabIndex = 4;
			//
			// m_edit_blue
			//
			this.m_edit_blue.Location = new System.Drawing.Point(351, 200);
			this.m_edit_blue.Name = "m_edit_blue";
			this.m_edit_blue.Size = new System.Drawing.Size(45, 20);
			this.m_edit_blue.TabIndex = 5;
			//
			// m_edit_hex
			//
			this.m_edit_hex.Location = new System.Drawing.Point(300, 248);
			this.m_edit_hex.Name = "m_edit_hex";
			this.m_edit_hex.Size = new System.Drawing.Size(87, 20);
			this.m_edit_hex.TabIndex = 7;
			//
			// m_btn_ok
			//
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(239, 285);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 8;
			this.m_btn_ok.Text = "&OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			//
			// m_btn_cancel
			//
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(321, 285);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 9;
			this.m_btn_cancel.Text = "&Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			//
			// m_edit_hue
			//
			this.m_edit_hue.Location = new System.Drawing.Point(249, 156);
			this.m_edit_hue.Name = "m_edit_hue";
			this.m_edit_hue.Size = new System.Drawing.Size(45, 20);
			this.m_edit_hue.TabIndex = 0;
			//
			// m_edit_sat
			//
			this.m_edit_sat.Location = new System.Drawing.Point(249, 178);
			this.m_edit_sat.Name = "m_edit_sat";
			this.m_edit_sat.Size = new System.Drawing.Size(45, 20);
			this.m_edit_sat.TabIndex = 1;
			//
			// m_edit_lum
			//
			this.m_edit_lum.Location = new System.Drawing.Point(249, 200);
			this.m_edit_lum.Name = "m_edit_lum";
			this.m_edit_lum.Size = new System.Drawing.Size(45, 20);
			this.m_edit_lum.TabIndex = 2;
			//
			// m_lbl_hue
			//
			this.m_lbl_hue.AutoSize = true;
			this.m_lbl_hue.Location = new System.Drawing.Point(213, 159);
			this.m_lbl_hue.Name = "m_lbl_hue";
			this.m_lbl_hue.Size = new System.Drawing.Size(30, 13);
			this.m_lbl_hue.TabIndex = 14;
			this.m_lbl_hue.Text = "&Hue:";
			//
			// m_lbl_sat
			//
			this.m_lbl_sat.AutoSize = true;
			this.m_lbl_sat.Location = new System.Drawing.Point(217, 181);
			this.m_lbl_sat.Name = "m_lbl_sat";
			this.m_lbl_sat.Size = new System.Drawing.Size(26, 13);
			this.m_lbl_sat.TabIndex = 15;
			this.m_lbl_sat.Text = "&Sat:";
			//
			// m_lbl_lum
			//
			this.m_lbl_lum.AutoSize = true;
			this.m_lbl_lum.Location = new System.Drawing.Point(213, 203);
			this.m_lbl_lum.Name = "m_lbl_lum";
			this.m_lbl_lum.Size = new System.Drawing.Size(30, 13);
			this.m_lbl_lum.TabIndex = 16;
			this.m_lbl_lum.Text = "&Lum:";
			//
			// m_edit_alpha
			//
			this.m_edit_alpha.Location = new System.Drawing.Point(300, 225);
			this.m_edit_alpha.Name = "m_edit_alpha";
			this.m_edit_alpha.Size = new System.Drawing.Size(45, 20);
			this.m_edit_alpha.TabIndex = 6;
			//
			// m_lbl_alpha
			//
			this.m_lbl_alpha.AutoSize = true;
			this.m_lbl_alpha.Location = new System.Drawing.Point(257, 228);
			this.m_lbl_alpha.Name = "m_lbl_alpha";
			this.m_lbl_alpha.Size = new System.Drawing.Size(37, 13);
			this.m_lbl_alpha.TabIndex = 18;
			this.m_lbl_alpha.Text = "&Alpha:";
			//
			// m_lbl_basic
			//
			this.m_lbl_basic.AutoSize = true;
			this.m_lbl_basic.Location = new System.Drawing.Point(12, 10);
			this.m_lbl_basic.Name = "m_lbl_basic";
			this.m_lbl_basic.Size = new System.Drawing.Size(74, 13);
			this.m_lbl_basic.TabIndex = 19;
			this.m_lbl_basic.Text = "Basic Colours:";
			//
			// m_table_basic
			//
			this.m_table_basic.ColumnCount = 7;
			this.m_table_basic.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.Location = new System.Drawing.Point(12, 25);
			this.m_table_basic.Name = "m_table_basic";
			this.m_table_basic.RowCount = 7;
			this.m_table_basic.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_basic.Size = new System.Drawing.Size(175, 175);
			this.m_table_basic.TabIndex = 20;
			//
			// m_lbl_recent
			//
			this.m_lbl_recent.AutoSize = true;
			this.m_lbl_recent.Location = new System.Drawing.Point(9, 205);
			this.m_lbl_recent.Name = "m_lbl_recent";
			this.m_lbl_recent.Size = new System.Drawing.Size(83, 13);
			this.m_lbl_recent.TabIndex = 21;
			this.m_lbl_recent.Text = "Recent Colours:";
			//
			// m_table_recent
			//
			this.m_table_recent.ColumnCount = 7;
			this.m_table_recent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.Location = new System.Drawing.Point(12, 220);
			this.m_table_recent.Name = "m_table_recent";
			this.m_table_recent.RowCount = 2;
			this.m_table_recent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
			this.m_table_recent.Size = new System.Drawing.Size(175, 50);
			this.m_table_recent.TabIndex = 21;
			//
			// m_lbl_initial
			//
			this.m_lbl_initial.AutoSize = true;
			this.m_lbl_initial.Location = new System.Drawing.Point(27, 280);
			this.m_lbl_initial.Name = "m_lbl_initial";
			this.m_lbl_initial.Size = new System.Drawing.Size(67, 13);
			this.m_lbl_initial.TabIndex = 22;
			this.m_lbl_initial.Text = "Initial Colour:\r\n";
			//
			// m_panel_initial
			//
			this.m_panel_initial.Location = new System.Drawing.Point(100, 277);
			this.m_panel_initial.Name = "m_panel_initial";
			this.m_panel_initial.Size = new System.Drawing.Size(88, 19);
			this.m_panel_initial.TabIndex = 23;
			//
			// m_panel_selected
			//
			this.m_panel_selected.Location = new System.Drawing.Point(100, 295);
			this.m_panel_selected.Name = "m_panel_selected";
			this.m_panel_selected.Size = new System.Drawing.Size(88, 19);
			this.m_panel_selected.TabIndex = 24;
			//
			// m_lbl_selected
			//
			this.m_lbl_selected.AutoSize = true;
			this.m_lbl_selected.Location = new System.Drawing.Point(9, 298);
			this.m_lbl_selected.Name = "m_lbl_selected";
			this.m_lbl_selected.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_selected.TabIndex = 25;
			this.m_lbl_selected.Text = "Selected Colour:";
			//
			// m_wheel
			//
			this.m_wheel.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_wheel.Location = new System.Drawing.Point(194, 12);
			this.m_wheel.Name = "m_wheel";
			this.m_wheel.Parts = ((pr.gui.ColourWheel.EParts)((((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider)
            | pr.gui.ColourWheel.EParts.ASlider)
            | pr.gui.ColourWheel.EParts.ColourSelection)
            | pr.gui.ColourWheel.EParts.VSelection)
            | pr.gui.ColourWheel.EParts.ASelection)));
			this.m_wheel.Size = new System.Drawing.Size(203, 138);
			this.m_wheel.SliderWidth = 20;
			this.m_wheel.TabIndex = 0;
			this.m_wheel.VerticalLayout = false;
			//
			// ColourUI
			//
			this.AcceptButton = this.m_btn_ok;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(409, 321);
			this.Controls.Add(this.m_lbl_selected);
			this.Controls.Add(this.m_panel_selected);
			this.Controls.Add(this.m_panel_initial);
			this.Controls.Add(this.m_lbl_initial);
			this.Controls.Add(this.m_table_recent);
			this.Controls.Add(this.m_lbl_recent);
			this.Controls.Add(this.m_table_basic);
			this.Controls.Add(this.m_lbl_basic);
			this.Controls.Add(this.m_lbl_alpha);
			this.Controls.Add(this.m_edit_alpha);
			this.Controls.Add(this.m_lbl_lum);
			this.Controls.Add(this.m_lbl_sat);
			this.Controls.Add(this.m_lbl_hue);
			this.Controls.Add(this.m_edit_lum);
			this.Controls.Add(this.m_edit_sat);
			this.Controls.Add(this.m_edit_hue);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_edit_hex);
			this.Controls.Add(this.m_edit_blue);
			this.Controls.Add(this.m_edit_green);
			this.Controls.Add(this.m_edit_red);
			this.Controls.Add(this.m_lbl_green);
			this.Controls.Add(this.m_lbl_blue);
			this.Controls.Add(this.m_lbl_hex);
			this.Controls.Add(this.m_lbl_red);
			this.Controls.Add(this.m_wheel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ColourUI";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Choose a Colour:";
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		#endregion
	}
}
