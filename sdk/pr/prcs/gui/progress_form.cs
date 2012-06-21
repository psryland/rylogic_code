using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using pr.maths;
using pr.util;

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
	public sealed class ProgressForm :Form
	{
		public class UserState
		{
			/// <summary>Control the visibility of the progress bar. 'null' means don't change</summary>
			public bool? ProgressBarVisible = null;
			
			/// <summary>Dialog icon</summary>
			public Icon Icon = null;
			
			/// <summary>Dialog title</summary>
			public string Title = null;
			
			/// <summary>Change the description. 'null' means don't change</summary>
			public string Description = null;
		}

		private readonly BackgroundWorker m_bgw;
		private readonly ProgressBar m_progress;
		private readonly Button m_button;
		private readonly Label m_description;
		private bool m_cancel_allowed;

		/// <summary>Allow/Disallow the cancel button to cause</summary>
		public bool AllowCancel
		{
			get { return m_cancel_allowed; }
			set
			{
				m_cancel_allowed = value;
				m_button.Enabled = m_cancel_allowed;
			}
		}
		
		/// <summary>Any exception thrown in the worker thread</summary>
		public Exception Error { get; private set; }

		public ProgressForm(string title, string description, DoWorkEventHandler func) :this(title, description, func, null) {}
		public ProgressForm(string title, string description, DoWorkEventHandler func, object argument)
		{
			// Setup the bgw
			m_bgw = new BackgroundWorker
			{
				WorkerReportsProgress = true,
				WorkerSupportsCancellation = true,
			};
			m_bgw.DoWork += func;
			m_bgw.ProgressChanged += (s,e)=>
				{
					m_progress.Value = (int)Maths.Lerp(m_progress.Minimum, m_progress.Maximum, Maths.Clamp(e.ProgressPercentage * 0.01f, 0f, 1f));
					UserState us = e.UserState as UserState;
					if (us != null)
					{
						if (us.Title              != null) Text = us.Title;
						if (us.Description        != null) m_description.Text = us.Description;
						if (us.Icon               != null) Icon = us.Icon;
						if (us.ProgressBarVisible != null) m_progress.Visible = us.ProgressBarVisible.Value;
					}
				};
			m_bgw.RunWorkerCompleted += (s,e)=>
				{
					Log.Info(this, "Progress form worker complete");
					if ((Error = e.Error) != null) DialogResult = DialogResult.Abort;
					else if (e.Cancelled)          DialogResult = DialogResult.Cancel;
					else                           DialogResult = DialogResult.OK;
					Action close = Close;
					BeginInvoke(close);
				};
			
			m_description = new Label{Text = description, AutoSize = false};
			m_progress = new ProgressBar();
			m_button = new Button{Text = "Cancel", DialogResult = DialogResult.Cancel, UseVisualStyleBackColor = true, TabIndex = 1, Enabled = false};
			m_button.Click += (s,a)=>{ if (AllowCancel) m_bgw.CancelAsync(); };
			
			Text                = title;
			StartPosition       = FormStartPosition.CenterParent;
			FormBorderStyle     = FormBorderStyle.FixedDialog;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode       = AutoScaleMode.Font;
			CancelButton        = m_button;
			Controls.Add(m_description);
			Controls.Add(m_progress);
			Controls.Add(m_button);
			DoLayout();
			
			Shown += (s,a)=>
				{
					m_bgw.RunWorkerAsync(argument);
					AllowCancel = true;
				};
			SizeChanged += (s,e) => DoLayout();
			FormClosing += (s,e) =>
				{
					e.Cancel = m_bgw.IsBusy; // Don't allow the form to close until the worker has finished
				};
			FormClosed += (s,e) =>
				{
					if (Error == null) return;
					if (Error.InnerException != null) throw Error.InnerException;
					throw Error;
				};
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
