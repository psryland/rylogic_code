using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using pr.maths;

namespace pr.gui
{
	public sealed class ProgressForm :Form
	{
		public class UserState
		{
			public static UserState Empty = new UserState();

			/// <summary>Progress completeness [0f,1f]. Null means unknown</summary>
			public float? FractionComplete { get; set; }

			/// <summary>Control the visibility of the progress bar. Null means don't change</summary>
			public bool? ProgressBarVisible { get; set; }

			/// <summary>Control the style of the progress bar. Null means don't change</summary>
			public ProgressBarStyle? ProgressBarStyle { get; set; }

			/// <summary>Dialog icon. Null means don't change</summary>
			public Icon Icon { get; set; }

			/// <summary>Dialog title. Null means don't change</summary>
			public string Title { get; set; }

			/// <summary>Change the description and re-layout (if ForceLayout is null). Null means don't change</summary>
			public string Description { get; set; }

			/// <summary>Force recalculation of the form layout (or not). Null means layout if needed</summary>
			public bool? ForceLayout { get; set; }

			/// <summary>Set to true to have the dialog close (used by ProgressForm once the task is complete)</summary>
			public bool CloseDialog { get; set; }

			/// <summary>Duplicate this object</summary>
			public UserState Clone() { return (UserState)MemberwiseClone(); }
		}

		private readonly ProgressBar m_progress;
		private readonly Label m_description;
		private readonly Button m_button;
		private Exception m_error;

		/// <summary>An event raised when the task is complete</summary>
		public ManualResetEvent Done { get; private set; }

		/// <summary>Allow/Disallow cancel</summary>
		public bool AllowCancel
		{
			get { return m_button.Visible; }
			set { m_button.Visible = false; }
		}

		/// <summary>An event used to signal the other thread to cancel</summary>
		public ManualResetEvent CancelSignal { get; private set; }
		public bool CancelPending { get { return CancelSignal.WaitOne(0); } }

		/// <summary>Progress callback function, called from 'func' to update the progress bar</summary>
		public delegate void Progress(UserState us);

		public ProgressForm(string title, string desc, Icon icon, ProgressBarStyle style, Action<ProgressForm, object, Progress> func, object arg = null)
		{
			m_progress = new ProgressBar{Style = style, Anchor = AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top};
			m_description = new Label{Text = desc ?? string.Empty, AutoSize = false, Anchor = AnchorStyles.Top|AnchorStyles.Left};
			m_button = new Button{Text = "Cancel", UseVisualStyleBackColor = true, TabIndex = 1, Anchor = AnchorStyles.Bottom|AnchorStyles.Right};
			m_button.Click += (s,a) => { CancelSignal.Set(); m_button.Text = "Cancelling..."; m_button.Enabled = false; };

			Done         = new ManualResetEvent(false);
			CancelSignal = new ManualResetEvent(false);
			var dispatcher = Dispatcher.CurrentDispatcher;

			// Start the task
			ThreadPool.QueueUserWorkItem(x =>
				{
					try
					{
						func(this, x, us => dispatcher.BeginInvoke(new Progress(UpdateProgress), us.Clone()));
						dispatcher.BeginInvoke(new Progress(UpdateProgress), new UserState{FractionComplete = 1f});
					}
					catch (OperationCanceledException)
					{
						CancelSignal.Set();
					}
					catch (AggregateException ex)
					{
						m_error = ex.InnerExceptions.FirstOrDefault(e => !(e is OperationCanceledException));
						if (m_error == null) CancelSignal.Set();
					}
					catch (Exception ex)
					{
						m_error = ex;
					}
					Done.Set();
					dispatcher.BeginInvoke(new Progress(UpdateProgress), new UserState{CloseDialog = true});
				}, arg);

			Text = title ?? string.Empty;
			if (icon != null) Icon = icon;

			StartPosition       = FormStartPosition.CenterParent;
			FormBorderStyle     = FormBorderStyle.FixedDialog;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode       = AutoScaleMode.Font;
			Controls.Add(m_description);
			Controls.Add(m_progress);
			Controls.Add(m_button);
			DoLayout();

			SizeChanged += (s,e) => DoLayout();
			FormClosing += (s,a) =>
				{
					if (Done.WaitOne(0) && m_error != null)
						throw m_error;

					DialogResult = CancelSignal.WaitOne(0)
						? DialogResult.Cancel
						: DialogResult.OK;
				};
		}

		/// <summary>Show the dialog after a few milliseconds</summary>
		public DialogResult ShowDialog(Form parent, int delay_ms = 0)
		{
			if (Done.WaitOne(delay_ms))
				return DialogResult.OK; // done already

			return base.ShowDialog(parent);
		}

		/// <summary>Update the state of the progress form</summary>
		private void UpdateProgress(UserState us)
		{
			if (us.FractionComplete != null)
				m_progress.Value = (int)Maths.Lerp(m_progress.Minimum, m_progress.Maximum, Maths.Clamp(us.FractionComplete.Value,0f,1f));

			if (us.Title != null)
				Text = us.Title;

			if (us.Description != null)
			{
				m_description.Text = us.Description;
				if (us.ForceLayout == null)
					DoLayout();
			}

			if (us.Icon != null)
				Icon = us.Icon;

			if (us.ProgressBarVisible != null)
				m_progress.Visible = us.ProgressBarVisible.Value;

			if (us.ProgressBarStyle != null)
				m_progress.Style = us.ProgressBarStyle.Value;

			if (us.ForceLayout != null && us.ForceLayout.Value)
				DoLayout();

			if (us.CloseDialog)
				Close();
		}

		/// <summary>Layout the form</summary>
		private void DoLayout()
		{
			const int space = 10;
			SuspendLayout();

			// Determine the vertical and left positions
			m_description.Location = new Point(space, space);
			m_description.Size     = new Size(m_description.PreferredWidth, m_description.PreferredHeight);
			m_progress.Location    = new Point(space, m_description.Bottom + space);
			m_progress.Width       = Math.Max(300, m_description.PreferredWidth);
			m_button.Location      = new Point(m_progress.Right - m_button.Width, m_progress.Bottom + space);

			// Find the bounds of the controls
			Rectangle bounds = Rectangle.Empty;
			foreach (Control c in Controls)
				bounds = Rectangle.Union(bounds, c.Bounds);

			// Set the dialog size
			ClientSize = bounds.Size + new Size(space, space);

			ResumeLayout(false);
			PerformLayout();
		}
	}
}
