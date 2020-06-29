using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using Rylogic.Extn;
using Rylogic.Utility;

namespace SolarHotWater
{
	public sealed class Model :IDisposable, INotifyPropertyChanged
	{
		public Model(SettingsData settings)
		{
			Settings = settings;
			Shutdown = new CancellationTokenSource();
			Devices = new ObservableCollection<EweDevice>();
			Ewe = new EweLinkAPI(Shutdown.Token);
			Fronius = new FroniusAPI(Settings.SolarInverterIP, Shutdown.Token);
			Solar = new SolarData();
		}
		public void Dispose()
		{
			PollSolarData = false;
			Fronius = null!;
			Ewe = null!;
		}

		/// <summary></summary>
		public SettingsData Settings { get; }

		/// <summary>Shutdown token</summary>
		public CancellationTokenSource Shutdown { get; }

		/// <summary>Access to the REST API of eWeLink</summary>
		private EweLinkAPI Ewe
		{
			get => m_ewe;
			set
			{
				if (m_ewe == value) return;
				Util.Dispose(ref m_ewe!);
				m_ewe = value;
			}
		}
		private EweLinkAPI m_ewe = null!;

		/// <summary>Access to the REST API of the fronius inverter</summary>
		public FroniusAPI Fronius
		{
			get => m_fronius;
			set
			{
				if (m_fronius == value) return;
				Util.Dispose(ref m_fronius!);
				m_fronius = value;
			}
		}
		private FroniusAPI m_fronius = null!;

		/// <summary>EweDevices</summary>
		public ObservableCollection<EweDevice> Devices { get; }

		/// <summary>Current solar output</summary>
		public SolarData Solar
		{
			get => m_solar;
			set
			{
				if (m_solar == value) return;
				m_solar = value;
				NotifyPropertyChanged(nameof(Solar));
			}
		}
		private SolarData m_solar;

		/// <summary>Enable/Disable polling the solar output data</summary>
		public bool PollSolarData
		{
			get => m_timer != null;
			set
			{
				if (PollSolarData == value) return;
				if (m_timer != null)
				{
					m_timer.Stop();
				}
				m_timer = value ? new DispatcherTimer(Settings.SolarPollRate, DispatcherPriority.Background, HandlePoll, Dispatcher.CurrentDispatcher) : null;
				if (m_timer != null)
				{
					m_timer.Start();
				}

				// Handler
				async void HandlePoll(object? sender, EventArgs e)
				{
					try
					{
						m_timer?.Stop();
						Solar = await Fronius.RealTimeData(Shutdown.Token);
						m_timer?.Start();
					}
					catch (Exception ex)
					{
						LastError = ex;
					}
				}
			}
		}
		private DispatcherTimer? m_timer;

		/// <summary>The last error</summary>
		public Exception? LastError
		{
			get => m_last_error;
			set
			{
				if (m_last_error == value) return;
				m_last_error = value;
				NotifyPropertyChanged(nameof(LastError));
			}
		}
		private Exception? m_last_error;

		/// <summary></summary>
		public async void Login(string username, string password)
		{
			Settings.Username = username;
			Settings.Password = password;
			if (username.HasValue() && password.HasValue())
			{
				try
				{
					await Ewe.Login(username, password);
				}
				catch (Exception ex)
				{
					LastError = ex;
				}
			}
		}


		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
