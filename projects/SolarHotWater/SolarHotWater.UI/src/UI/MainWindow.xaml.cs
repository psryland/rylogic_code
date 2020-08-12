using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using EweLink;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
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
			AddNewConsumer = Command.Create(this, AddNewConsumerInternal);
			RemoveConsumer = Command.Create(this, RemoveConsumerInternal);
			InspectDevice = Command.Create(this, InspectDeviceInternal);
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

				// Handler
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
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
						case nameof(Model.Consumers):
						{
							// Try to preserve the selected consumer
							var selected = SelectedConsumer?.Name;

							Consumers.Refresh();

							if (selected != null)
								SelectedConsumer = Model.Consumers.FirstOrDefault(x => x.Name == selected);
							break;
						}
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

		/// <summary>Show/Hide the consumer details part of the UI</summary>
		public bool ShowConsumerDetails
		{
			get => m_show_consumer_details;
			set
			{
				if (m_show_consumer_details == value) return;
				m_show_consumer_details = value;
				NotifyPropertyChanged(nameof(ShowConsumerDetails));
			}
		}
		private bool m_show_consumer_details;

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

		/// <summary>A string description of the consumers that are currently on</summary>
		public string ActiveConsumers => string.Join(',', Model.ActiveConsumers.Select(x => x.Name));

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
			if (SelectedConsumer == null) return;

			// Delete the selected consumer and select the first available one
			Model.RemoveConsumer(SelectedConsumer);
			SelectedConsumer = Model.Consumers.FirstOrDefault();
		}

		/// <summary>Inspect the device of the currently selected consumer</summary>
		public Command InspectDevice { get; }
		private void InspectDeviceInternal()
		{
			if (!(SelectedConsumer?.EweSwitch is EweDevice device)) return;
			var dlg = new InspectDeviceUI(this, Model, device);
			dlg.Show();
		}

		/// <summary>Toggle the switch state</summary>
		public Command ToggleSwitch { get; }
		private async void ToggleSwitchInternal()
		{
			if (!(SelectedConsumer?.EweSwitch is EweSwitch sw))
				return;

			try
			{
				await Ewe.SwitchState(sw, EweSwitch.ESwitchState.Toggle, 0, Model.Shutdown.Token);
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
	}
}

