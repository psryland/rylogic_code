using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestCS
{
	public class FormColourWheel :Form
	{
		public FormColourWheel()
		{
			InitializeComponent();
		}

		private pr.gui.ColourWheel colourWheel1;

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
			this.colourWheel1 = new pr.gui.ColourWheel();
			this.SuspendLayout();
			// 
			// colourWheel1
			// 
			this.colourWheel1.Colour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.colourWheel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.colourWheel1.Location = new System.Drawing.Point(0, 0);
			this.colourWheel1.Name = "colourWheel1";
			this.colourWheel1.Parts = ((pr.gui.ColourWheel.EParts)((((((pr.gui.ColourWheel.EParts.Wheel | pr.gui.ColourWheel.EParts.VSlider) 
            | pr.gui.ColourWheel.EParts.ASlider) 
            | pr.gui.ColourWheel.EParts.ColourSelection) 
            | pr.gui.ColourWheel.EParts.VSelection) 
            | pr.gui.ColourWheel.EParts.ASelection)));
			this.colourWheel1.Size = new System.Drawing.Size(182, 112);
			this.colourWheel1.SliderWidth = 20;
			this.colourWheel1.TabIndex = 0;
			this.colourWheel1.VerticalLayout = false;
			// 
			// FormColourWheel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(182, 112);
			this.Controls.Add(this.colourWheel1);
			this.Name = "FormColourWheel";
			this.Text = "FormColourWheel";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
