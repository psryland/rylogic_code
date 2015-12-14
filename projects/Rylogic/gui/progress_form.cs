using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using pr.extn;
using pr.maths;
using pr.util;

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

			/// <summary>The font to use for the description text. Null means don't change</summary>
			public Font DescFont { get; set; }

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
		private Thread m_thread;
		private Exception m_error;

		/// <summary>Create a progress form for external control via the UpdateProgress method</summary>
		public ProgressForm(string title, string desc, Icon icon, ProgressBarStyle style)
		{
			m_progress = new ProgressBar
				{
					Style = style,
					Anchor = AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top
				};
			m_description = new Label
				{
					Text = desc ?? string.Empty,
					AutoSize = false,
					Anchor = AnchorStyles.Top|AnchorStyles.Left
				};
			
			m_button = new Button
				{
					Text = "Cancel",
					UseVisualStyleBackColor = true,
					TabIndex = 1,
					Anchor = AnchorStyles.Bottom|AnchorStyles.Right
				};
			m_button.Click += (s,a) =>
				{
					if (CancelSignal != null)
						CancelSignal.Set();

					m_button.Text = "Cancelling...";
					m_button.Enabled = false;
				};

			Text = title ?? string.Empty;
			if (icon != null) Icon = icon;

			AutoSizeMode        = AutoSizeMode.GrowOnly;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode       = AutoScaleMode.Font;
			StartPosition       = FormStartPosition.CenterParent;
			FormBorderStyle     = FormBorderStyle.FixedDialog;
			DialogResult        = DialogResult.OK;
			ClientSize          = new Size(136, 20);
			HideOnClose         = false;

			Controls.Add(m_description);
			Controls.Add(m_progress);
			Controls.Add(m_button);
		}

		/// <summary>Creates a progress form starting a background thread immediately</summary>
		public ProgressForm(string title, string desc, Icon icon, ProgressBarStyle style, Action<ProgressForm, object, Progress> func, object arg = null, ThreadPriority priority = ThreadPriority.Normal)
			:this(title, desc, icon, style)
		{
			Done         = new ManualResetEvent(false);
			CancelSignal = new ManualResetEvent(false);

			// Start the task
			var dispatcher = Dispatcher.CurrentDispatcher;
			m_thread = new Thread(new ThreadStart(() =>
			{
				try
				{
					func(this, arg, us => dispatcher.BeginInvoke(new Progress(UpdateProgress), us.Clone()));
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
			}))
			{
				Name = "ProgressForm",
				Priority = priority,
				IsBackground = true,
			};
			m_thread.Start();
		}
		protected override void Dispose(bool disposing)
		{
			// Not all worker threads will be testing for 'CancelPending'. A lock-up here means
			// the dialog is being destroyed while the worker thread is still running. The worker
			// thread either needs to test for CancelPending, or the dialog should not be close-able
			// until the background thread is finished.
			if (m_thread != null) m_thread.Join();
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			DoLayout();
			base.OnShown(e);
		}
		protected override void OnLayout(LayoutEventArgs levent)
		{
			using (this.SuspendLayout(false))
				DoLayout();

			base.OnLayout(levent);
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (CancelSignal != null)
			{
				// Save the result based on whether cancel was selected
				DialogResult = CancelSignal.WaitOne(0)
					? DialogResult.Cancel
					: DialogResult.OK;
			}

			base.OnFormClosing(e);

			// If the form is hide on close and the user is closing, then just
			// hide the UI. The background thread is not automatically cancelled.
			if (HideOnClose && e.CloseReason == CloseReason.UserClosing)
			{
				Hide();
				e.Cancel = true;
				if (Owner != null)
					Owner.Focus();
			}
			// If the form will be disposed and there is a Cancel event, automatically
			// signal Cancel as a convenience. Worker threads that don't test for
			// 'CancelPending' will have to prevent the form from closing until finished.
			else if (CancelSignal != null)
			{
				CancelSignal.Set();
			}
		}

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

		/// <summary>Tests whether cancel has been signalled. The background thread tests this to see if it should early out</summary>
		public bool CancelPending { get { return CancelSignal.WaitOne(0); } }

		/// <summary>Controls whether the form closes or just hides</summary>
		public bool HideOnClose { get; set; }

		/// <summary>Progress callback function, called from 'func' to update the progress bar</summary>
		public delegate void Progress(UserState us);

		/// <summary>Show the dialog after a few milliseconds</summary>
		public DialogResult ShowDialog(Control parent, int delay_ms = 0)
		{
			// Show the dialog after the delay
			var result = DialogResult.OK;
			if (delay_ms == 0 || !Done.WaitOne(delay_ms)) // not done already?
				result = base.ShowDialog(parent);

			// If an error was raised in the background thread, rethrow it
			if (m_error != null)
				throw m_error;

			return result;
		}

		/// <summary>Show the dialog non-modally</summary>
		public Scope ShowScope(Control parent)
		{
			return Scope.Create(
				() => { if (!Visible) Show(parent); },
				() => { if (HideOnClose) Hide(); else Close(); });
		}

		/// <summary>Update the state of the progress form</summary>
		public void UpdateProgress(UserState us)
		{
			var do_layout = us.ForceLayout != null && us.ForceLayout.Value;

			if (us.FractionComplete != null)
				m_progress.Value = (int)Maths.Lerp(m_progress.Minimum, m_progress.Maximum, Maths.Clamp(us.FractionComplete.Value,0f,1f));

			if (us.Title != null)
				Text = us.Title;

			if (us.Description != null)
			{
				m_description.Text = us.Description;
				do_layout |= true;
			}

			if (us.DescFont != null)
				m_description.Font = us.DescFont;

			if (us.Icon != null)
				Icon = us.Icon;

			if (us.ProgressBarVisible != null)
				m_progress.Visible = us.ProgressBarVisible.Value;

			if (us.ProgressBarStyle != null)
				m_progress.Style = us.ProgressBarStyle.Value;

			if (do_layout)
				DoLayout();

			if (us.CloseDialog)
				Close();
		}

		/// <summary>Layout the form</summary>
		private void DoLayout()
		{
			const int space = 10;

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
			var preferred_size = bounds.Size + new Size(space, space);
			switch (AutoSizeMode)
			{
			case AutoSizeMode.GrowAndShrink:
				ClientSize = preferred_size;
				break;
			case AutoSizeMode.GrowOnly:
				ClientSize = new Size(
					Math.Max(preferred_size.Width, ClientSize.Width),
					Math.Max(preferred_size.Height, ClientSize.Height));
				break;
			}

			m_progress.Width = ClientSize.Width - 2*space;
			m_button.Location = new Point(m_progress.Right - m_button.Width, m_progress.Bottom + space);
		}
	}
}
