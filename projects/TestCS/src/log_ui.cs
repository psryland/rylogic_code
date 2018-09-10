using System.Windows.Forms;
using Rylogic.Utility;

namespace TestCS
{
	public class LogUI :Form
	{
		private Rylogic.Gui.WinForms.LogUI m_log_ui;
		public LogUI()
		{
			InitializeComponent();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		public void AddMessage(string msg)
		{
			m_log_ui.AddMessage(msg);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_log_ui = new Rylogic.Gui.WinForms.LogUI();
			this.SuspendLayout();
			// 
			// m_log_ui
			// 
			this.m_log_ui.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_log_ui.Location = new System.Drawing.Point(0, 0);
			this.m_log_ui.Name = "m_log_ui";
			this.m_log_ui.PopOutOnNewMessages = true;
			this.m_log_ui.Size = new System.Drawing.Size(539, 656);
			this.m_log_ui.TabIndex = 0;
			this.m_log_ui.Title = "LogControl";
			// 
			// LogUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(539, 656);
			this.Controls.Add(this.m_log_ui);
			this.Name = "LogUI";
			this.Text = "log_ui";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
