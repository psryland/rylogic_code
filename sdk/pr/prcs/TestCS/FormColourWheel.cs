using System.Windows.Forms;
using pr.gui;

namespace TestCS
{
	public class FormColourWheel :Form
	{
		public FormColourWheel()
		{
			InitializeComponent();

			m_btn_dlg.Click += (s,a) =>
				{
					new ColourUI().ShowDialog();
				};
		}

		private ColourWheel m_wheel;
		private Button m_btn_dlg;

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
			this.m_wheel = new pr.gui.ColourWheel();
			this.m_btn_dlg = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_wheel
			// 
			this.m_wheel.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_wheel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_wheel.Location = new System.Drawing.Point(0, 0);
			this.m_wheel.Name = "m_wheel";
			this.m_wheel.Parts = ((pr.gui.ColourWheel.EParts)((((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider) 
            | pr.gui.ColourWheel.EParts.ASlider) 
            | pr.gui.ColourWheel.EParts.ColourSelection) 
            | pr.gui.ColourWheel.EParts.VSelection) 
            | pr.gui.ColourWheel.EParts.ASelection)));
			this.m_wheel.Size = new System.Drawing.Size(182, 188);
			this.m_wheel.SliderWidth = 20;
			this.m_wheel.TabIndex = 0;
			this.m_wheel.VerticalLayout = false;
			// 
			// m_btn_dlg
			// 
			this.m_btn_dlg.Location = new System.Drawing.Point(25, 153);
			this.m_btn_dlg.Name = "m_btn_dlg";
			this.m_btn_dlg.Size = new System.Drawing.Size(127, 23);
			this.m_btn_dlg.TabIndex = 1;
			this.m_btn_dlg.Text = "Colour Picker Dialog";
			this.m_btn_dlg.UseVisualStyleBackColor = true;
			// 
			// FormColourWheel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(182, 188);
			this.Controls.Add(this.m_btn_dlg);
			this.Controls.Add(this.m_wheel);
			this.Name = "FormColourWheel";
			this.Text = "FormColourWheel";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
