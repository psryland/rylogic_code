using System.Windows.Forms;
using pr.util;

namespace TestCS
{
	public class LogUI :Form
	{
		public LogUI()
		{
			InitializeComponent();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.logUI1 = new pr.gui.LogUI();
			this.SuspendLayout();
			// 
			// logUI1
			// 
			this.logUI1.Location = new System.Drawing.Point(12, 12);
			this.logUI1.Name = "logUI1";
			this.logUI1.PopOutOnNewMessages = true;
			this.logUI1.Size = new System.Drawing.Size(515, 632);
			this.logUI1.TabIndex = 0;
			this.logUI1.Title = "LogControl";
			// 
			// LogUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(539, 656);
			this.Controls.Add(this.logUI1);
			this.Name = "LogUI";
			this.Text = "log_ui";
			this.ResumeLayout(false);

		}
		#endregion

		private pr.gui.LogUI logUI1;
	}
}
