using System.Windows;
using CoinFlip.Settings;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Dialogs
{
	public partial class MainSettingsUI :Window
	{
		public MainSettingsUI(Window owner)
		{
			InitializeComponent();
			Owner = owner;
			Icon = Owner?.Icon;

			// Commands
			Accept = Command.Create(this, AcceptInternal);

			DataContext = this;
		}

		/// <summary></summary>
		public SettingsData Settings => SettingsData.Settings;

		/// <summary>Close the dialog</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			Close();
		}
	}
}
