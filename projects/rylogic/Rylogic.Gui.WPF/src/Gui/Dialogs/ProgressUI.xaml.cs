using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public sealed partial class ProgressUI : Window, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - Worker functions should test 'ui.CancelToken', not the cancel token they pass to the constructor.

		public ProgressUI(Window? owner, string title, string desc, ImageSource? image, CancellationToken? cancel)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;
			Title = title;
			Image = image;
			Description = desc;
			Done = new ManualResetEvent(false);
			Cancel = CancellationTokenSource.CreateLinkedTokenSource(cancel ?? CancellationToken.None);
			ProgressBarVisible = true;
			AllowCancel = true;
			CancelEnabled = true;
			SignalCancel = Command.Create(this, () => CancelPending = true);
			DataContext = this;
		}
		public ProgressUI(Window? owner, string title, string desc, ImageSource? image, WorkerFunc worker, CancellationToken? cancel = null, object? arg = null, ThreadPriority priority = ThreadPriority.BelowNormal)
			: this(owner, title, desc, image, cancel)
		{
			try
			{
				worker ??= DefaultWorkerFunc;
				void DefaultWorkerFunc(ProgressUI ui, object? a, ProgressCB p)
				{
					// Default worker function just waits till cancel is signalled
					for (; !CancelPending; Thread.Sleep(100))
						p(new UserState { });
				}

				// Start the worker task
				m_thread = new Thread(new ThreadStart(ThreadEntry))
				{
					Name = "ProgressUI",
					Priority = priority,
					IsBackground = true,
				};
				void ThreadEntry()
				{
					var progress_cb = new ProgressCB(UpdateProgress);
					try
					{
						// Run the worker task
						worker(this, arg, us => Dispatcher.BeginInvoke(progress_cb, DispatcherPriority.Render, us));
						Dispatcher.BeginInvoke(progress_cb, new UserState { FractionComplete = 1f });
					}
					catch (Exception ex)
					{
						if (ex is OperationCanceledException) Cancel.Cancel();
						else Error = ex;
					}

					// Set the event to say "task done"
					Done.Set();
					Dispatcher.BeginInvoke(progress_cb, DispatcherPriority.Render, new UserState { CloseDialog = true });
				}
				m_thread.Start();
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			// Not all worker threads will be testing for 'CancelPending'. A lock-up here means
			// the dialog is being disposed while the worker thread is still running. The worker
			// thread either needs to test for CancelPending, or the dialog should not be close-able
			// until the background thread is finished.
			if (m_thread != null && m_thread.IsAlive)
			{
				m_thread.Join();
				m_thread = null;
			}
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			// Don't close until we have a result
			if (!AllowCancel)
			{
				e.Cancel = true;
			}
			else if (Result == null)
			{
				Cancel.Cancel();
				e.Cancel = true;
			}
			else if (this.IsModal())
			{
				DialogResult = Result;
			}
			base.OnClosing(e);
		}
		private Thread? m_thread;

		/// <summary>The fraction complete that the task is</summary>
		public double FractionComplete
		{
			get => m_fraction_complete;
			set
			{
				if (m_fraction_complete == value) return;
				m_fraction_complete = value;
				NotifyPropertyChanged(nameof(FractionComplete));
			}
		}
		private double m_fraction_complete;

		/// <summary>True if the task can be cancelled</summary>
		public bool AllowCancel
		{
			get => m_allow_cancel;
			set
			{
				if (m_allow_cancel == value) return;
				m_allow_cancel = value;
				NotifyPropertyChanged(nameof(AllowCancel));
			}
		}
		private bool m_allow_cancel;

		/// <summary>The prompt text</summary>
		public string Description
		{
			get => (string)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}
		private void Description_Changed()
		{
			NotifyPropertyChanged(nameof(Description));
		}
		public static readonly DependencyProperty DescriptionProperty = Gui_.DPRegister<ProgressUI>(nameof(Description), string.Empty, Gui_.EDPFlags.None);

		/// <summary>Image source for the prompt icon</summary>
		public ImageSource? Image
		{
			get => (ImageSource?)GetValue(ImageProperty);
			set => SetValue(ImageProperty, value);
		}
		private void Image_Changed()
		{
			NotifyPropertyChanged(nameof(Image));
		}
		public static readonly DependencyProperty ImageProperty = Gui_.DPRegister<ProgressUI>(nameof(Image), null, Gui_.EDPFlags.None);

		/// <summary>Text displayed within the progress bar</summary>
		public string ProgressBarText
		{
			get => (string)GetValue(ProgressBarTextProperty);
			set => SetValue(ProgressBarTextProperty, value);
		}
		private void ProgressBarText_Changed()
		{
			NotifyPropertyChanged(nameof(ProgressBarText));
		}
		public static readonly DependencyProperty ProgressBarTextProperty = Gui_.DPRegister<ProgressUI>(nameof(ProgressBarText), string.Empty, Gui_.EDPFlags.None);

		/// <summary>True for tasks of unknown length</summary>
		public bool ProgressIsIndeterminate
		{
			get => (bool)GetValue(ProgressIsIndeterminateProperty);
			set => SetValue(ProgressIsIndeterminateProperty, value);
		}
		private void ProgressIsIndeterminate_Changed()
		{
			NotifyPropertyChanged(nameof(ProgressIsIndeterminate));
		}
		public static readonly DependencyProperty ProgressIsIndeterminateProperty = Gui_.DPRegister<ProgressUI>(nameof(ProgressIsIndeterminate), Boxed.False, Gui_.EDPFlags.None);

		/// <summary>True if the progress bar should be shown</summary>
		public bool ProgressBarVisible
		{
			get => (bool)GetValue(ProgressBarVisibleProperty);
			set => SetValue(ProgressBarVisibleProperty, value);
		}
		private void ProgressBarVisible_Changed()
		{
			NotifyPropertyChanged(nameof(ProgressBarVisible));
		}
		public static readonly DependencyProperty ProgressBarVisibleProperty = Gui_.DPRegister<ProgressUI>(nameof(ProgressBarVisible), Boxed.True, Gui_.EDPFlags.None);

		/// <summary>True if the cancel button is enabled</summary>
		public bool CancelEnabled
		{
			get => (bool)GetValue(CancelEnabledProperty);
			set => SetValue(CancelEnabledProperty, value);
		}
		private void CancelEnabled_Changed()
		{
			NotifyPropertyChanged(nameof(CancelEnabled));
		}
		public static readonly DependencyProperty CancelEnabledProperty = Gui_.DPRegister<ProgressUI>(nameof(CancelEnabled), Boxed.False, Gui_.EDPFlags.None);

		/// <summary>An event raised when the task is complete</summary>
		public ManualResetEvent Done { get; }

		/// <summary>Access the cancel token</summary>
		public CancellationToken CancelToken => Cancel.Token;
		private CancellationTokenSource Cancel { get; }

		/// <summary>The background task should cancel</summary>
		public bool CancelPending
		{
			get => AllowCancel && Cancel.IsCancellationRequested;
			private set { if (AllowCancel) Cancel.Cancel(); }
		}

		/// <summary>Set the state of 'CancelPending' to true</summary>
		public Command SignalCancel { get; }

		/// <summary>True if the task completed, false if cancelled, null if an error was thrown</summary>
		public bool? Result { get; private set; }

		/// <summary>Any exception that was thrown by the background task</summary>
		public Exception? Error { get; private set; }

		/// <summary>Show the progress dialog after 'delay_ms' if the task is not yet complete</summary>
		public bool? ShowDialog(int delay_ms, bool rethrow = true)
		{
			// Show the dialog after the delay (unless done already)
			if (delay_ms != 0 && Done.WaitOne(delay_ms))
				Result = true;
			else
				base.ShowDialog();

			// If an error was raised in the background thread, rethrow it
			if (Error != null)
			{
				Result = null;
				if (rethrow) throw Error;
			}

			return Result;
		}
		public new bool? ShowDialog()
		{
			return ShowDialog(0);
		}

		/// <summary>Show the dialog non-modally</summary>
		public new Scope Show()
		{
			return Scope.Create(
				() => { if (!IsVisible) base.Show(); },
				() => { Close(); });
		}

		/// <summary>Update the state of the progress dialog</summary>
		public void UpdateProgress(UserState us)
		{
			if (us.FractionComplete != null)
				FractionComplete = Math_.Clamp(us.FractionComplete.Value, 0f, 1f);

			if (us.Title != null)
				Title = us.Title;

			if (us.Description != null)
				Description = us.Description;

			if (us.Image != null)
				Image = us.Image;

			if (us.ProgressBarVisible != null)
				ProgressBarVisible = us.ProgressBarVisible.Value;

			if (us.ProgressIsIndeterminate != null)
				ProgressIsIndeterminate = us.ProgressIsIndeterminate.Value;

			if (us.ProgressBarText != null)
				ProgressBarText = us.ProgressBarText;

			if (us.EnableCancel != null)
				CancelEnabled = us.EnableCancel.Value;

			if (us.EnableAutoResize != null)
				SizeToContent = us.EnableAutoResize.Value ? SizeToContent.WidthAndHeight : SizeToContent.Manual;

			if (us.CloseDialog == true)
			{
				Result = CancelPending ? false : true;
				AllowCancel = true;
				Close();
			}
		}

		/// <summary>The task worker function signature</summary>
		public delegate void WorkerFunc(ProgressUI ui, object? arg, ProgressCB cb);

		/// <summary>Callback function for updating progress</summary>
		public delegate void ProgressCB(UserState us);

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>User state used to update the progress dialog</summary>
		public struct UserState
		{
			public static UserState Empty = new UserState();

			/// <summary>Progress completeness [0f,1f]. Null means unknown</summary>
			public double? FractionComplete { get; set; }

			/// <summary>Control the visibility of the progress bar. Null means don't change</summary>
			public bool? ProgressBarVisible { get; set; }

			/// <summary>True if the duration of the task is unknown. Null means don't change</summary>
			public bool? ProgressIsIndeterminate { get; set; }

			/// <summary>Text to display on the progress bar. Null means don't change</summary>
			public string? ProgressBarText { get; set; }

			/// <summary>Image next to the description text. Null means don't change</summary>
			public ImageSource? Image { get; set; }

			/// <summary>Dialog title. Null means don't change</summary>
			public string? Title { get; set; }

			/// <summary>Description text. Null means don't change</summary>
			public string? Description { get; set; }

			/// <summary>Enable/Disable the cancel button. Null means don't change</summary>
			public bool? EnableCancel { get; set; }

			/// <summary>Enable/Disable the automatic resize of the dialog. Null means don't change</summary>
			public bool? EnableAutoResize { get; set; }

			/// <summary>Set to true to have the dialog close (used by ProgressUI once the task is complete)</summary>
			public bool? CloseDialog { get; set; }
		}
	}
}
