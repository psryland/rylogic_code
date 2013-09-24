using System.Drawing;
using System.Windows.Forms;
using pr.gui;

namespace RyLogViewer
{
	public class ColourPickerUI :Form
	{
		private ColourWheel m_colour_fore;
		private ColourWheel m_colour_back;
		private Label m_lbl_foreground;
		private Label m_lbl_background;
		private Label m_lbl_example;
		private Button m_btn_ok;
		private Button m_btn_cancel;

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

		public ColourPickerUI(Color font_colour, Color back_colour)
		{
			InitializeComponent();
			FontColor = font_colour;
			BkgdColor = back_colour;

			m_colour_fore.Colour = font_colour;
			m_colour_fore.ColourChanged += (s,a) =>
				{
					FontColor = m_colour_fore.Colour;
					m_lbl_example.Invalidate();
				};
			m_colour_back.Colour = back_colour;
			m_colour_back.ColourChanged += (s,a) =>
				{
					BkgdColor = m_colour_back.Colour;
					m_lbl_example.Invalidate();
				};
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
			this.m_colour_fore = new pr.gui.ColourWheel();
			this.m_colour_back = new pr.gui.ColourWheel();
			this.m_lbl_foreground = new System.Windows.Forms.Label();
			this.m_lbl_background = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_lbl_example
			//
			this.m_lbl_example.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_example.Location = new System.Drawing.Point(12, 140);
			this.m_lbl_example.Name = "m_lbl_example";
			this.m_lbl_example.Size = new System.Drawing.Size(282, 25);
			this.m_lbl_example.TabIndex = 0;
			this.m_lbl_example.Text = "Preview of selected colours";
			this.m_lbl_example.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			//
			// m_btn_ok
			//
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(138, 177);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 2;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			//
			// m_btn_cancel
			//
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(219, 177);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			//
			// m_colour_fore
			//
			this.m_colour_fore.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_fore.Location = new System.Drawing.Point(12, 33);
			this.m_colour_fore.Name = "m_colour_fore";
			this.m_colour_fore.Parts = ((pr.gui.ColourWheel.EParts)((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider)
            | pr.gui.ColourWheel.EParts.ColourSelection)
            | pr.gui.ColourWheel.EParts.VSelection)));
			this.m_colour_fore.Size = new System.Drawing.Size(128, 94);
			this.m_colour_fore.SliderWidth = 20;
			this.m_colour_fore.TabIndex = 1;
			this.m_colour_fore.VerticalLayout = false;
			//
			// m_colour_back
			//
			this.m_colour_back.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_colour_back.Location = new System.Drawing.Point(166, 33);
			this.m_colour_back.Name = "m_colour_back";
			this.m_colour_back.Parts = ((pr.gui.ColourWheel.EParts)((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider)
            | pr.gui.ColourWheel.EParts.ColourSelection)
            | pr.gui.ColourWheel.EParts.VSelection)));
			this.m_colour_back.Size = new System.Drawing.Size(128, 94);
			this.m_colour_back.SliderWidth = 20;
			this.m_colour_back.TabIndex = 4;
			this.m_colour_back.VerticalLayout = false;
			//
			// m_lbl_foreground
			//
			this.m_lbl_foreground.AutoSize = true;
			this.m_lbl_foreground.Location = new System.Drawing.Point(33, 9);
			this.m_lbl_foreground.Name = "m_lbl_foreground";
			this.m_lbl_foreground.Size = new System.Drawing.Size(64, 13);
			this.m_lbl_foreground.TabIndex = 5;
			this.m_lbl_foreground.Text = "Font Colour:";
			//
			// m_lbl_background
			//
			this.m_lbl_background.AutoSize = true;
			this.m_lbl_background.Location = new System.Drawing.Point(174, 9);
			this.m_lbl_background.Name = "m_lbl_background";
			this.m_lbl_background.Size = new System.Drawing.Size(101, 13);
			this.m_lbl_background.TabIndex = 6;
			this.m_lbl_background.Text = "Background Colour:";
			//
			// ColourPickerUI
			//
			this.AcceptButton = this.m_btn_ok;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(306, 210);
			this.Controls.Add(this.m_lbl_background);
			this.Controls.Add(this.m_lbl_foreground);
			this.Controls.Add(this.m_colour_back);
			this.Controls.Add(this.m_colour_fore);
			this.Controls.Add(this.m_lbl_example);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ColourPickerUI";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion
	}
}
