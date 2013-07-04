using System.Drawing;
using System.Windows.Forms;
using pr.gui;

namespace RyLogViewer
{
	public class ColourPickerUI :Form
	{
		private Label m_lbl_example;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Panel m_panel;
		private ColourWheel m_colour_picker;

		/// <summary>The font colour</summary>
		public Color FontColor
		{
			get { return m_lbl_example.ForeColor; }
			set { m_lbl_example.ForeColor = value; }
		}

		/// <summary>The background colour</summary>
		public Color BkgdColor
		{
			get { return m_lbl_example.BackColor; }
			set { m_lbl_example.BackColor = value; }
		}

		public ColourPickerUI()
		{
			InitializeComponent();

			int which = 0;
			m_colour_picker.ColourSelection += (s,a) =>
				{
					which = 0;
					if (a.Button == MouseButtons.None) return;
					if (a.Button == MouseButtons.Left ) { which = 1; m_colour_picker.Colour = FontColor; }
					if (a.Button == MouseButtons.Right) { which = 2; m_colour_picker.Colour = BkgdColor; }
				};
			m_colour_picker.ColourChanged += (s,a) =>
				{
					if (which == 1) { FontColor = m_colour_picker.Colour; m_lbl_example.Invalidate(); }
					if (which == 2) { BkgdColor = m_colour_picker.Colour; m_lbl_example.Invalidate(); }
				};
		}

		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				cp.ClassStyle |= pr.util.Win32.CS_DROPSHADOW;
				return cp;
			}
		}

		#region Windows Form Designer generated code
		
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
			this.m_lbl_example = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_colour_picker = new pr.gui.ColourWheel();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_lbl_example
			// 
			this.m_lbl_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_example.Location = new System.Drawing.Point(128, 6);
			this.m_lbl_example.Name = "m_lbl_example";
			this.m_lbl_example.Size = new System.Drawing.Size(156, 118);
			this.m_lbl_example.TabIndex = 0;
			this.m_lbl_example.Text = "              Example Text.\r\n\r\nLeft click to set the font colour.\r\n\r\nRight click " +
    "to set the background colour.";
			this.m_lbl_example.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(128, 127);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 2;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(209, 127);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_panel
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BackColor = System.Drawing.SystemColors.Control;
			this.m_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel.Controls.Add(this.m_colour_picker);
			this.m_panel.Controls.Add(this.m_btn_cancel);
			this.m_panel.Controls.Add(this.m_btn_ok);
			this.m_panel.Controls.Add(this.m_lbl_example);
			this.m_panel.Location = new System.Drawing.Point(3, 3);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(294, 155);
			this.m_panel.TabIndex = 4;
			// 
			// m_colour_picker
			// 
			this.m_colour_picker.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_picker.Location = new System.Drawing.Point(6, 6);
			this.m_colour_picker.Name = "m_colour_picker";
			this.m_colour_picker.Parts = ((pr.gui.ColourWheel.EParts)((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider) 
            | pr.gui.ColourWheel.EParts.ColourSelection) 
            | pr.gui.ColourWheel.EParts.VSelection)));
			this.m_colour_picker.Size = new System.Drawing.Size(116, 144);
			this.m_colour_picker.SliderWidth = 20;
			this.m_colour_picker.TabIndex = 1;
			this.m_colour_picker.VerticalLayout = true;
			// 
			// ColourPickerUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(299, 161);
			this.ControlBox = false;
			this.Controls.Add(this.m_panel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ColourPickerUI";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
