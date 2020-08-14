using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using EweLink;
using Newtonsoft.Json.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace SolarHotWater.UI
{
	public partial class MainWindow :Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();
			Model = new Model();
			Consumers = CollectionViewSource.GetDefaultView(Model.Consumers);
			EweDevices = CollectionViewSource.GetDefaultView(Model.EweDevices);
			Chart = m_history_chart;

			Login = Command.Create(this, LoginInternal, LoginCanExecute);
			ToggleEnableMonitor = Command.Create(this, ToggleEnableMonitorInternal, ToggleEnableMonitorCanExecute);
			RefreshDeviceInfo = Command.Create(this, RefreshDeviceInfoInternal);
			AddNewConsumer = Command.Create(this, AddNewConsumerInternal);
			RemoveConsumer = Command.Create(this, RemoveConsumerInternal);
			InspectDevice = Command.Create(this, InspectDeviceInternal);
			ChangeColour = Command.Create(this, ChangeColourInternal);
			ToggleSwitch = Command.Create(this, ToggleSwitchInternal);
			ShowLog = Command.Create(this, ShowLogInternal);

			m_password.Password = Settings.Password;
			DataContext = this;
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			if (!Model.Shutdown.IsCancellationRequested)
			{
				e.Cancel = true;
				Model.Shutdown.Cancel();
				Dispatcher.BeginInvoke(new Action(Close));
				return;
			}
			base.OnClosing(e);
		}
		protected override void OnClosed(EventArgs e)
		{
			Chart = null!;
			Model = null!;
			base.OnClosed(e);
			Log.Dispose();
		}

		/// <summary>App model</summary>
		private Model Model
		{
			get => m_model;
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.PropertyChanged -= HandlePropertyChanged;
					Util.Dispose(ref m_model!);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.PropertyChanged += HandlePropertyChanged;
				}

				// Handlers
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
						case nameof(Model.LastError):
						{
							NotifyPropertyChanged(nameof(StatusMessage));
							NotifyPropertyChanged(nameof(StatusColour));
							break;
						}
						case nameof(Model.CanLogin):
						{
							Login.NotifyCanExecuteChanged();
							break;
						}
						case nameof(Model.LoginInProgress):
						{
							Login.NotifyCanExecuteChanged();
							break;
						}
						case nameof(Model.IsLoggedOn):
						{
							Login.NotifyCanExecuteChanged();
							NotifyPropertyChanged(nameof(IsLoggedOn));
							break;
						}
						case nameof(Model.EnableMonitorAvailable):
						{
							ToggleEnableMonitor.NotifyCanExecuteChanged();
							break;
						}
						case nameof(Model.EnableMonitor):
						{
							NotifyPropertyChanged(nameof(MonitorEnabled));
							ToggleEnableMonitor.NotifyCanExecuteChanged();
							break;
						}
						case nameof(Model.Solar):
						{
							NotifyPropertyChanged(nameof(Solar));
							NotifyPropertyChanged(nameof(PowerSurplus));
							break;
						}
						case nameof(Model.PowerConsumed):
						{
							NotifyPropertyChanged(nameof(PowerConsumed));
							break;
						}
						case nameof(Model.PowerSurplus):
						{
							NotifyPropertyChanged(nameof(PowerSurplus));
							break;
						}
						case nameof(Model.EweDevices):
						{
							EweDevices.Refresh();
							NotifyPropertyChanged(nameof(EweDevices));
							break;
						}
						//case nameof(Model.Consumers):
						//{
						//	// Try to preserve the selected consumer
						//	var selected = SelectedConsumer?.Name;
						//
						//	Consumers.Refresh();
						//
						//	if (selected != null)
						//		SelectedConsumer = Model.Consumers.FirstOrDefault(x => x.Name == selected);
						//	break;
						//}
					}
				}
			}
		}
		private Model m_model = null!;

		/// <summary></summary>
		public SettingsData Settings => Model.Settings;

		/// <summary>Ewelink API access</summary>
		private EweLinkAPI Ewe => Model.Ewe;

		/// <summary>True if currently logged on</summary>
		public bool IsLoggedOn => Model.IsLoggedOn;

		/// <summary>True if the monitor is current running</summary>
		public bool MonitorEnabled => Model.EnableMonitor;

		/// <summary>The consumers</summary>
		public ICollectionView Consumers { get; }

		/// <summary>The currently selected consumer</summary>
		public Consumer? SelectedConsumer
		{
			get => m_selected_consumer;
			set
			{
				if (m_selected_consumer == value) return;
				m_selected_consumer = value;
				NotifyPropertyChanged(nameof(SelectedConsumer));
			}
		}
		private Consumer? m_selected_consumer;

		/// <summary>The available ewelink devices</summary>
		public ICollectionView EweDevices { get; }

		/// <summary>The solar data</summary>
		public SolarData Solar => Model.Solar;

		/// <summary>The sum of active consumers</summary>
		public double PowerConsumed => Model.PowerConsumed;

		/// <summary>Total unused solar power (in kWatts)</summary>
		public double PowerSurplus => Model.PowerSurplus;

		/// <summary>The schedule for when the controller is active</summary>
		public string Schedule
		{
			get => Model.Sched.Description;
			set { }// Model.Sched (value);
		}

		/// <summary></summary>
		public string StatusMessage => Model.LastError?.Message ?? "Idle";

		/// <summary></summary>
		public Brush StatusColour => Model.LastError != null ? Brushes.Red : Brushes.Black;

		/// <summary>Log</summary>
		public Command Login { get; }
		private async void LoginInternal()
		{
			if (!Model.IsLoggedOn)
				await Model.Login(Settings.Username, m_password.Password);
			else
				await Model.Logout();
		}
		private bool LoginCanExecute()
		{
			return Model.CanLogin;
		}

		/// <summary>Enable/Disable the monitor</summary>
		public Command ToggleEnableMonitor { get; }
		private void ToggleEnableMonitorInternal()
		{
			Model.EnableMonitor = !Model.EnableMonitor;
		}
		private bool ToggleEnableMonitorCanExecute()
		{
			return Model.EnableMonitorAvailable;
		}

		/// <summary>Request the latest info for a device</summary>
		public Command RefreshDeviceInfo { get; }
		private async void RefreshDeviceInfoInternal()
		{
			if (!(SelectedConsumer?.EweSwitch is EweSwitch sw))
				return;
			try
			{
				var info = await Ewe.GetDeviceInfo(sw.DeviceID, Model.Shutdown.Token);
				if (info["params"] is JObject parms)
					sw.Update(parms);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Refresh device info failed");
			}
		}

		/// <summary>Add a new consumer</summary>
		public Command AddNewConsumer { get; }
		private void AddNewConsumerInternal()
		{
			// Add a consumer and make it the selected one.
			SelectedConsumer = Model.AddNewConsumer();
		}

		/// <summary>Remove the current consumer</summary>
		public Command RemoveConsumer { get; }
		private void RemoveConsumerInternal()
		{
			if (SelectedConsumer == null)
				return;

			// Delete the selected consumer and select the first available one
			Model.RemoveConsumer(SelectedConsumer);
			SelectedConsumer = Model.Consumers.FirstOrDefault();
		}

		/// <summary>Inspect the device of the currently selected consumer</summary>
		public Command InspectDevice { get; }
		private void InspectDeviceInternal()
		{
			if (!(SelectedConsumer?.EweSwitch is EweDevice device))
				return;

			var dlg = new InspectDeviceUI(this, Model, device);
			dlg.Show();
		}

		/// <summary>Edit the colour of the consumer</summary>
		public Command ChangeColour { get; }
		private void ChangeColourInternal()
		{
			if (SelectedConsumer == null)
				return;

			var dlg = new ColourPickerUI(this, SelectedConsumer.Colour);
			if (dlg.ShowDialog() == true)
				SelectedConsumer.Colour = dlg.Colour;
		}

		/// <summary>Toggle the switch state</summary>
		public Command ToggleSwitch { get; }
		private async void ToggleSwitchInternal()
		{
			if (!(SelectedConsumer?.EweSwitch is EweSwitch sw))
				return;

			try
			{
				var state = sw.On ? EweSwitch.ESwitchState.Off : EweSwitch.ESwitchState.On;
				await Ewe.SwitchState(sw, state, 0, Model.Shutdown.Token);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Toggle switch state failed");
			}
		}

		/// <summary>Show the log window</summary>
		public Command ShowLog { get; }
		private void ShowLogInternal()
		{
			if (m_log_ui == null)
			{
				m_log_ui = new LogUI(this);
				m_log_ui.Closed += delegate { m_log_ui = null; };
				m_log_ui.Show();
			}
			m_log_ui.Focus();
		}
		private LogUI? m_log_ui;

		/// <summary>Reordered notification</summary>
		private void HandleConsumersReordered(object sender, RoutedEventArgs args)
		{
			Settings.Save();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>The history chart control</summary>
		public ChartControl Chart
		{
			get => m_chart;
			private set
			{
				if (m_chart == value) return;
				if (m_chart != null)
				{
					m_chart.AutoRanging -= HandleAutoRange;
					m_chart.XAxis.TickText = m_chart.XAxis.DefaultTickText;
					Util.Dispose(ref m_chartdata!);
					Util.Dispose(ref m_chart!);
				}
				m_chart = value;
				if (m_chart != null)
				{
					m_chart.AllowElementDragging = false;
					m_chart.Options.AreaSelectRequiresShiftKey = true;
					m_chart.Options.Orthographic = true;
					m_chart.XAxis.Options.PixelsPerTick = 50.0;
					m_chart.XAxis.Options.TickTextTemplate = "XX:XX\r\nXXX XX XXXX";
					m_chart.YAxis.Options.TickTextTemplate = "X.XXXX";
					m_chart.YAxis.Label = "Power (kWatts)";
					m_chart.XAxis.TickText = HandleChartXAxisTickText;
					m_chart.AutoRanging += HandleAutoRange;

					m_chartdata = new ChartData(m_chart, Model.History, Model.Consumers);
					m_chart.AutoRange();
				}

				// Handlers
				string HandleChartXAxisTickText(double x, double? step = null)
				{
					var prev = x - step ?? 0.0;
					var curr = x;

					// If the current tick mark represents the same time as the previous one, no text is required
					if (prev == curr && step != null)
						return string.Empty;

					// Get the date time for the tick
					var dt_curr = History.Epoch + TimeSpan.FromHours(curr);
					var dt_prev = History.Epoch + TimeSpan.FromHours(prev);
					if (dt_curr == default || dt_prev == default)
						return string.Empty;

					// Get the date time values in the correct time zone
					dt_curr = dt_curr.LocalDateTime;
					dt_prev = dt_prev.LocalDateTime;

					// First tick on the x axis
					var first_tick =
						step == null || x - step < Chart.XAxis.Min;

					// Show more of the time stamp depending on how it differs from the previous time stamp
					return ShortTimeString(dt_curr, dt_prev, first_tick);
					static string ShortTimeString(DateTimeOffset dt_curr, DateTimeOffset dt_prev, bool first)
					{
						// Return a timestamp string suitable for a chart X tick value

						// First tick on the x axis
						if (first)
							return dt_curr.ToString("HH:mm'\r\n'dd-MMM-yy");

						// Show more of the time stamp depending on how it differs from the previous time stamp
						if (dt_curr.Year != dt_prev.Year)
							return dt_curr.ToString("HH:mm'\r\n'dd-MMM-yy");
						if (dt_curr.Month != dt_prev.Month)
							return dt_curr.ToString("HH:mm'\r\n'dd-MMM");
						if (dt_curr.Day != dt_prev.Day)
							return dt_curr.ToString("HH:mm'\r\n'ddd dd");

						return dt_curr.ToString("HH:mm");
					}
				}
				void HandleAutoRange(object? sender, ChartControl.AutoRangeEventArgs e)
				{
					var xrange = RangeF.Invalid;
					var yrange = RangeF.Invalid;

					// Scan the last 24 hours
					var t0 = (DateTimeOffset.Now - History.Epoch).TotalHours - 24.0;
					var t1 = (DateTimeOffset.Now - History.Epoch).TotalHours - 0.0;
					void BoundChartDataSeries(ChartDataSeries series)
					{
						using var lk = series.Lock();
						lk.IndexRange(t0, t1, out var i0, out var i1);
						foreach (var pt in lk.Values(i0, i1))
						{
							xrange.Encompass(pt.xf);
							yrange.Encompass(pt.yf);
						}
					}

					// Bounds of the solar output data
					BoundChartDataSeries(m_chartdata.Solar);

					// Bounds of the combined consumption data
					BoundChartDataSeries(m_chartdata.Consumption);

					// Bounds of the consumer data
					foreach (var consumer in m_chartdata.Consumers)
						BoundChartDataSeries(consumer);

					// Handle ranging on a single axis
					if (!e.Axes.HasFlag(ChartControl.EAxis.XAxis))
						xrange = m_chart.XAxis.Range;
					if (!e.Axes.HasFlag(ChartControl.EAxis.YAxis))
						yrange = m_chart.YAxis.Range;
					else
						yrange.Inflate(1.2);

					e.ViewBBox = new BBox(new v4(xrange.Midf, yrange.Midf, 0f, 1f), new v4(xrange.Sizef / 2, yrange.Sizef / 2, 1f / 2, 0f));
					e.Handled = true;
				}
			}
		}
		private ChartControl m_chart = null!;
		private ChartData m_chartdata = null!;
	}
}

