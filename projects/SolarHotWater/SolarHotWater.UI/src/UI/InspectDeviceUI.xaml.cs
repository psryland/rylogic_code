using System;
using System.Windows;
using EweLink;
using Newtonsoft.Json.Linq;
using Rylogic.Gui.WPF;

namespace SolarHotWater.UI
{
	public partial class InspectDeviceUI :Window
	{
		public InspectDeviceUI(Window owner, EweDevice device)
		{
			InitializeComponent();
			Owner = owner;
			Device = device;
			PinState = new PinData(this, EPin.Centre);
			Title = $"Inspecting {Device.Name}";
			DataContext = this;
		}
		
		/// <summary>Support pinning this window</summary>
		private PinData PinState { get; }

		/// <summary></summary>
		private EweDevice Device { get; }

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
	}
}
