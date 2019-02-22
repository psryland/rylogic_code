using System.Windows;
using CoinFlip.Settings;

namespace CoinFlip.UI
{
	public partial class App : Application
	{
		public App()
		{
		}

		/// <summary>Singleton access</summary>
		public static new App Current => (App)Application.Current;

		/// <summary>Selected app skin</summary>
		public static ESkin Skin
		{
			get { return SettingsData.Settings.Skin; }
			set
			{
				SettingsData.Settings.Skin = value;
				// A restart is needed to reload StaticResources
			}
		}
	}
}
