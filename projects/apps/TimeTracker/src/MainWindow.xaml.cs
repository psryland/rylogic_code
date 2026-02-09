using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Interop.Win32;
using Rylogic.Utility;

namespace TimeTracker
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public MainWindow(Settings settings)
		{
			InitializeComponent();
			TaskNamesView = new ListCollectionView(settings.TaskNames.OrderBy(x => x).ToList());
			TimePeriodsView = new ListCollectionView(new List<TimePeriod>());
			TimeTotalsView = new ListCollectionView(new List<TimeTotal>());
			Settings = settings;

			Loaded += HandleLoaded;

			AddTask = Command.Create(this, AddTaskInternal);
			ClearData = Command.Create(this, ClearDataInternal);
			ShowOptions = Command.Create(this, ShowOptionsInternal);

			LoadTimePeriods();
			
			Settings.NotifyAllSettingsChanged();
			MonitorEnabled = true;
			DataContext = this;
		}
		protected override void OnLocationChanged(EventArgs e)
		{
			base.OnLocationChanged(e);
			Settings.ScreenPosition = new Point(Left, Top);
		}
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			Reminding = false;
		}
		protected override void OnDeactivated(EventArgs e)
		{
			base.OnDeactivated(e);
			Reminding = false;
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			MonitorEnabled = false;
			SaveTimePeriods();
		}
		private void HandleLoaded(object? sender, EventArgs e)
		{
			// Ensure the window is on screen
			var pt = Gui_.OnScreen(new Point(Left, Top), RenderSize);
			this.SetLocation(pt);
		}

		/// <summary>History file</summary>
		private string TimePeriodsFilepath => Util.ResolveUserDocumentsPath("Rylogic", "TimeTracker", "history.csv");

		/// <summary>App settings</summary>
		public Settings Settings
		{
			get => m_settings;
			set
			{
				if (m_settings == value) return;
				if (m_settings != null)
				{
					m_settings.SettingChange -= HandleSettingChange;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChange += HandleSettingChange;
				}

				// Handler
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
						case nameof(TimeTracker.Settings.AlwaysOnTop):
						{
							Topmost = Settings.AlwaysOnTop;
							break;
						}
						case nameof(TimeTracker.Settings.TaskNames):
						{
							TaskNames.Assign(Settings.TaskNames.OrderBy(x => x));
							TaskNamesView.Refresh();
							break;
						}
						case nameof(TimeTracker.Settings.ScreenPosition):
						{
							Left = Settings.ScreenPosition.X;
							Top = Settings.ScreenPosition.Y;
							break;
						}
						case nameof(TimeTracker.Settings.ReminderTime):
						{
							m_last_reminder = DateTimeOffset.Now;
							break;
						}
						case nameof(TimeTracker.Settings.RemindersEnabled):
						{
							EnableReminder = Settings.RemindersEnabled;
							break;
						}
					}
				}
			}
		}
		private Settings m_settings = null!;

		/// <summary>Monitor timer</summary>
		public bool MonitorEnabled
		{
			get => m_monitor != null;
			private set
			{
				if (MonitorEnabled == value) return;
				if (m_monitor != null)
				{
					m_monitor.Stop();
				}
				m_monitor = value ? new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, HandleTick, Dispatcher) : null;
				if (m_monitor != null)
				{
					m_monitor.Start();
				}

				// Handler
				void HandleTick(object? sender, EventArgs e)
				{
					// Notify time updated on the latest TimePeriod
					if (TimePeriods.Count != 0)
					{
						TimePeriods[0].NotifyPropertyChanged(nameof(TimePeriod.Duration));
					}

					// Remove tasks not created today
					if (Settings.ResetEachDay)
					{
						var today = DateTimeOffset.Now.Date;
						if (TimePeriods.RemoveIf(x => x.Start.Date != today) != 0)
						{
							TimePeriodsView.Refresh();
							UpdateTotals();
						}
					}
				
					// Remove tasks that are too old
					if (Settings.AutoRemoveOldTasks)
					{
						var age = Settings.MaxAge;
						var now = DateTimeOffset.Now;
						if (TimePeriods.RemoveIf(x => now - x.Start > age) != 0)
						{
							TimePeriodsView.Refresh();
							UpdateTotals();
						}
					}
					
					// Show the reminder alert
					if (EnableReminder)
					{
						if (!IsActive && DateTimeOffset.Now - m_last_reminder > Settings.ReminderTime)
							Reminding = true;
					}
				}
			}
		}
		private DispatcherTimer? m_monitor;
		private DateTimeOffset m_last_reminder = DateTimeOffset.MinValue;

		/// <summary>Expand the view</summary>
		public bool Expanded
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				SizeToContent = SizeToContent.Height;
				NotifyPropertyChanged(nameof(Expanded));
			}
		}

		/// <summary>True if the app periodically flashes</summary>
		public bool EnableReminder
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(EnableReminder));
			}
		}

		/// <summary>The name of the current task</summary>
		public string CurrentTaskName
		{
			get;
			set
			{
				if (field == value) return;
				field = value ?? string.Empty;
				NotifyPropertyChanged(nameof(CurrentTaskName));
			}
		} = string.Empty;

		/// <summary>True if the reminder is active</summary>
		public bool Reminding
		{
			get => m_reminding;
			set
			{
				if (m_reminding == value) return;
				m_reminding = value;
				m_last_reminder = DateTimeOffset.Now;
				if (Reminding)
					Win32.FlashWindow(this.Hwnd(), Win32.EFlashWindowFlags.FLASHW_TRAY | Win32.EFlashWindowFlags.FLASHW_TIMERNOFG);
			}
		}
		private bool m_reminding;

		/// <summary>The set of task names to choose from</summary>
		public ICollectionView TaskNamesView { get; }
		private List<string> TaskNames => (List<string>)TaskNamesView.SourceCollection;

		/// <summary>The recorded time periods </summary>
		public ICollectionView TimePeriodsView { get; }
		private List<TimePeriod> TimePeriods => (List<TimePeriod>)TimePeriodsView.SourceCollection;

		/// <summary>Accumulated time for each task</summary>
		public ICollectionView TimeTotalsView { get; }
		private List<TimeTotal> TimeTotals => (List<TimeTotal>)TimeTotalsView.SourceCollection;

		/// <summary>Add a new task to the collection</summary>
		public Command AddTask { get; }
		private void AddTaskInternal()
		{
			// No task name, ignore...
			if (CurrentTaskName.Length == 0)
				return;

			// If the current task if different to current active task, add a new task
			if (TimePeriods.Count != 0 && Misc.SameTaskName(TimePeriods[0].TaskName, CurrentTaskName))
			{
				// Update the task name (in case the case is different)
				TimePeriods[0].TaskName = CurrentTaskName;
				return;
			}

			// If the task name is new, add it to the collection
			if (!Settings.TaskNames.Contains(CurrentTaskName))
			{
				Settings.TaskNames.Add(CurrentTaskName);
				Settings.NotifySettingChanged(nameof(Settings.TaskNames));
				TaskNamesView.MoveCurrentTo(CurrentTaskName);
			}

			// If we get to here, then this is a new task
			AddNewTask();
		}

		/// <summary>Reset all the time data</summary>
		public Command ClearData { get; }
		private void ClearDataInternal()
		{
			// Check first
			if (MsgBox.Show(this, "Clear data?", Util.AppProductName, MsgBox.EButtons.OKCancel, MsgBox.EIcon.Question) != true)
				return;

			TimePeriods.Clear();
			TimePeriodsView.Refresh();
			UpdateTotals();
		}

		/// <summary>Show the options dialog</summary>
		public Command ShowOptions { get; }
		private void ShowOptionsInternal()
		{
			var dlg = new OptionsUI(this, Settings);
			dlg.ShowDialog();
		}

		/// <summary>Add a new task to the collection</summary>
		private void AddNewTask()
		{
			// Start a new task
			var tp = new TimePeriod(CurrentTaskName, TimePeriods);
			TimePeriods.Insert(0, tp);
			TimePeriodsView.Refresh();
			UpdateTotals();

			// Save time periods
			SaveTimePeriods();
		}

		/// <summary>Update the totals collection</summary>
		private void UpdateTotals()
		{
			TimeTotals.Clear();
			foreach (var grp in TimePeriods.GroupBy(x => x.TaskName))
			{
				var total = new TimeTotal(grp.Key, grp);
				TimeTotals.Add(total);
			}
			TimeTotalsView.Refresh();
		}

		/// <summary>Load data from disk</summary>
		private void LoadTimePeriods()
		{
			if (!Path_.FileExists(TimePeriodsFilepath))
				return;

			// Load the time periods
			var periods = new List<TimePeriod>();
			var errors = new AggregateException("Errors while loading time period data");
			try
			{
				foreach (var row in CSVData.Parse(TimePeriodsFilepath, true))
				{
					try
					{
						var tp = new TimePeriod(DateTimeOffset.Parse(row[0]), row[1], TimePeriods);
						periods.Add(tp);
					}
					catch (Exception ex)
					{
						errors.InnerExceptions.Add(ex);
					}
				}
				periods.Sort(x => x.Start, -1);
			}
			catch (Exception ex)
			{
				errors.InnerExceptions.Add(ex);
			}
			if (errors.InnerExceptions.Count != 0)
			{
				// Notify of any errors
				MsgBox.Show(this, $"Errors loading existing task data from {TimePeriodsFilepath}. {errors.MessageFull()}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Exclamation);
				return;
			}

			// Updates the data
			TimePeriods.Assign(periods);
			TimePeriodsView.Refresh();
			UpdateTotals();
		}

		/// <summary>Save data to disk</summary>
		private void SaveTimePeriods()
		{
			try
			{
				var csv = new CSVData();
				foreach (var tp in TimePeriods)
					csv.Add(tp.Start.ToString("O"), tp.TaskName);

				csv.Save(TimePeriodsFilepath);
			}
			catch (Exception ex)
			{
				MsgBox.Show(this, $"Error saving task data to {TimePeriodsFilepath}. {ex.MessageFull()}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Exclamation);
			}
		}

		/// <summary>Watch for enter key presses</summary>
		private void HandlePreviewKeyUp(object? sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter)
				return;

			AddTask.Execute();
			e.Handled = true;
		}

		/// <summary>When the task combo loses focus, add a new task</summary>
		private void HandleLostFocus(object? sender, KeyboardFocusChangedEventArgs e)
		{
			AddTask.Execute();
		}

		/// <summary>Handle key down on the grid</summary>
		private void HandleGridKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete)
				return;

			// Delete the selected rows
			TimePeriods.RemoveSet(m_grid_tasks.SelectedItems.Cast<TimePeriod>());
			TimePeriodsView.Refresh();

			// Update the totals
			UpdateTotals();
			e.Handled = true;
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
