using System.Windows;
using SolarHotWater.Common;

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
