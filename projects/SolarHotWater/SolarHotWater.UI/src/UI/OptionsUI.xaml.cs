using System.Windows;

namespace SolarHotWater.UI
{
	public partial class OptionsUI :Window
	{
		public OptionsUI(Window owner, SettingsData settings)
		{
			InitializeComponent();
			Owner = owner;
			DataContext = settings;
		}
	}
}
