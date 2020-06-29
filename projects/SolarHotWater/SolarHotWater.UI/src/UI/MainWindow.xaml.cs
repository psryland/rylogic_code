using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace SolarHotWater.UI
{
	public partial class MainWindow :Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();
			Model = new Model(new SettingsData(SettingsData.DefaultFilepath));
			EweDevices = new ListCollectionView(Model.Devices);

			Login = Command.Create(this, LoginInternal);

			m_username.Text = Settings.Username;
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
			Model = null!;
			base.OnClosed(e);
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
						case nameof(Model.Solar):
						{
							NotifyPropertyChanged(nameof(Solar));
							break;
						}
					}
				}
			}
		}
		private Model m_model = null!;

		/// <summary></summary>
		private SettingsData Settings => Model.Settings;

		/// <summary>The available ewelink devices</summary>
		public ICollectionView EweDevices { get; }

		/// <summary>The solar data</summary>
		public SolarData Solar => Model.Solar;

		/// <summary>The IP address of the solar inverter</summary>
		public string InverterIP
		{
			get => m_inverter_ip;
			set
			{
				if (m_inverter_ip == value) return;
				m_inverter_ip = value;
				NotifyPropertyChanged(nameof(InverterIP));
			}
		}
		private string m_inverter_ip = string.Empty;

		/// <summary>Log</summary>
		public Command Login { get; }
		private void LoginInternal()
		{
			Model.Login(m_username.Text, m_password.Password);
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

