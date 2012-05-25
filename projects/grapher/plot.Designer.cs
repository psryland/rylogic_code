namespace pr
{
	partial class Plot
	{
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Plot));
			this.m_plot = new pr.gui.GraphControl();
			((System.ComponentModel.ISupportInitialize)(this.m_plot)).BeginInit();
			this.SuspendLayout();
			// 
			// m_plot
			// 
			this.m_plot.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_plot.Centre = ((System.Drawing.PointF)(resources.GetObject("m_plot.Centre")));
			this.m_plot.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_plot.Location = new System.Drawing.Point(0, 0);
			this.m_plot.Name = "m_plot";
			this.m_plot.Size = new System.Drawing.Size(284, 264);
			this.m_plot.TabIndex = 0;
			this.m_plot.Title = "";
			this.m_plot.Zoom = 1F;
			// 
			// Plot
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 264);
			this.Controls.Add(this.m_plot);
			this.Name = "Plot";
			this.Text = "Plot";
			((System.ComponentModel.ISupportInitialize)(this.m_plot)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private pr.gui.GraphControl m_plot;
	}
}