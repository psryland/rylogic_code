using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using EweLink;
using Newtonsoft.Json.Linq;
using Rylogic.Gui.WPF;

namespace SolarHotWater.UI
{
	public partial class InspectDeviceUI :Window, INotifyPropertyChanged
	{
		public InspectDeviceUI(Window owner, Model model, EweDevice device)
		{
			InitializeComponent();
			Owner = owner;
			Model = model;
			Device = device;
			PinState = new PinData(this, EPin.Centre);
			Title = $"Inspecting {Device.Name}";

			// Commands
			ToggleSwitch = Command.Create(this, ToggleSwitchInternal);
			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			Device = null!;
			base.OnClosed(e);
		}

		/// <summary>App logic</summary>
		private Model Model { get; }

		/// <summary>Ewelink API access</summary>
		private EweLinkAPI Ewe => Model.Ewe;

		/// <summary>Support pinning this window</summary>
		private PinData PinState { get; }

		/// <summary></summary>
		public EweDevice Device
		{
			get => m_device;
			set
			{
				if (m_device == value) return;
				if (m_device != null)
				{
					m_device.PropertyChanged -= HandlePropertyChanged;
				}
				m_device = value;
				if (m_device != null)
				{
					m_device.PropertyChanged += HandlePropertyChanged;
				}

				// Handler
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					//switch (e.PropertyName)
					//{
					//
					//}
					NotifyPropertyChanged(nameof(Device));
					NotifyPropertyChanged(nameof(Switch));
					NotifyPropertyChanged(nameof(Body));
				}
			}
		}
		private EweDevice m_device = null!;

		/// <summary>Access the device as a switch (or null if not a switch)</summary>
		public EweSwitch? Switch => Device as EweSwitch;

		/// <summary></summary>
		public string Body
		{
			get
			{
				try
				{
					var jobj = JObject.FromObject(Device);
					return jobj.ToString();
				}
				catch (Exception ex)
				{
					return ex.Message;
				}
			}
		}

		/// <summary>Toggle the switch state</summary>
		public Command ToggleSwitch { get; }
		private async void ToggleSwitchInternal()
		{
			if (!(Switch is EweSwitch sw)) return;
			try
			{
				await Ewe.SwitchState(sw, EweSwitch.ESwitchState.Toggle, 0, Model.Shutdown.Token);
			}
			catch (Exception ex)
			{
				Log.Write(Rylogic.Utility.ELogLevel.Error, ex, "Toggle switch state failed");
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
