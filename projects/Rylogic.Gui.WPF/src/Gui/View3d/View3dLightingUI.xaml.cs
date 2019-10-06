using System;
using System.Windows;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dLightingUI : Window
	{
		public View3dLightingUI()
		{
			InitializeComponent();
			Light = new View3d.Light();
		}
		public View3dLightingUI(View3dControl owner)
			:this()
		{
			Owner = GetWindow(owner);
			PinState = new PinData(this, EPin.Centre);
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			PinState = null;
		}

		/// <summary>The Light updated by this dialog</summary>
		public View3d.Light Light
		{
			get => m_light;
			set
			{
				if (m_light == value) return;
				m_light = value ?? new View3d.Light();
				DataContext = m_light;
			}
		}
		private View3d.Light m_light = null!;

		/// <summary>Support pinning this window</summary>
		private PinData? PinState
		{
			get => m_pin_state;
			set
			{
				if (m_pin_state == value) return;
				Util.Dispose(ref m_pin_state);
				m_pin_state = value;
			}
		}
		private PinData? m_pin_state;

		/// <summary>Handle the close button</summary>
		private void HandleClose(object sender, RoutedEventArgs args)
		{
			if (this.IsModal())
				DialogResult = true;
			else
				Close();
		}
	}
}
