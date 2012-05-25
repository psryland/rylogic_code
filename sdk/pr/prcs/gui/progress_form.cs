using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using pr.maths;

namespace pr.gui
{
	// A progress dialog that runs a delegate in the background while showing progress
	// Usage:
	//	int result = 0;
	//	ProgressForm task = new ProgressForm("Chore", "Doing a long winded task", delegate (object i, DoWorkEventArgs e)
	//	{
	//		BackgroundWorker bgw = (BackgroundWorker)i;
	//		for (result = 0; result != 100 && !bgw.CancellationPending; ++result)
	//		{
	//			bgw.ReportProgress(result);
	//			Thread.Sleep(100);
	//		}
	//		e.Cancel = bgw.CancellationPending;
	//	});
	//DialogResult res = task.ShowDialog(this);
	//MessageBox.Show("result got to " + result + " and the dialog returned " + res);
	public class ProgressForm :Form
	{
		private readonly BackgroundWorker m_bgw;
		private readonly ProgressBar m_progress;
		private readonly Button m_button;
		private readonly Label m_description;

		public ProgressForm(string title, string description, DoWorkEventHandler func) :this(title, description, func, null) {}
		public ProgressForm(string title, string description, DoWorkEventHandler func, object argument)
		{
			m_description = new Label{Text = description, AutoSize = false};
			m_progress = new ProgressBar();
			m_button = new Button{Text = "Cancel", DialogResult = DialogResult.Cancel, UseVisualStyleBackColor = true, TabIndex = 0};
			
			Text                = title;
			StartPosition       = FormStartPosition.CenterParent;
			FormBorderStyle     = FormBorderStyle.FixedDialog;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode       = AutoScaleMode.Font;
			Controls.Add(m_description);
			Controls.Add(m_progress);
			Controls.Add(m_button);
			DoLayout();
			
			m_bgw = new BackgroundWorker
			{
				WorkerReportsProgress = true,
				WorkerSupportsCancellation = true,
			};
			m_bgw.DoWork += func;
			m_bgw.ProgressChanged += (s,e)=>
				{
					m_progress.Value = (int)Maths.Lerp(m_progress.Minimum, m_progress.Maximum, Maths.Clamp(e.ProgressPercentage * 0.01f, 0f, 1f));
				};
			m_bgw.RunWorkerCompleted += (s,e)=>
				{
					if (e.Error != null) { DialogResult = DialogResult.Abort; Close(); if (e.Error.InnerException != null) throw e.Error.InnerException; throw e.Error; }
					if (e.Cancelled) DialogResult = DialogResult.Cancel;
					DialogResult = DialogResult.OK;
					Close();
				};
			
			Shown       += (s,e)=>{ m_bgw.RunWorkerAsync(argument); };
			SizeChanged += (s,e)=>{ DoLayout(); };
		}

		/// <summary>Layout the controls on the form</summary>
		private void DoLayout()
		{
			const int space = 10;
			
			SuspendLayout();
			m_description.Location = new Point(space, space);
			m_description.Size     = new Size(m_description.PreferredWidth, m_description.PreferredHeight);
			m_progress.Location    = new Point(space, m_description.Bottom + space);
			m_progress.Width       = Math.Max(300, ClientSize.Width - 2*space);
			m_button.Location      = new Point(m_progress.Right - m_button.Width, m_progress.Bottom + space);
			
			Rectangle bounds = Rectangle.Empty;
			foreach (Control c in Controls) bounds = Rectangle.Union(bounds, c.Bounds);
			ClientSize = bounds.Size + new Size(space, space);
			
			ResumeLayout(false);
			PerformLayout();
		}
	}
}
