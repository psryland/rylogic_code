using System.Windows;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public partial class View3dLightingUI : Window
	{
		public View3dLightingUI()
		{
			InitializeComponent();
			Light = new View3d.Light();
		}

		/// <summary>The Light updated by this dialog</summary>
		public View3d.Light Light
		{
			get { return m_light; }
			set
			{
				if (m_light == value) return;
				m_light = value ?? new View3d.Light();
				DataContext = m_light;
			}
		}
		private View3d.Light m_light;

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
