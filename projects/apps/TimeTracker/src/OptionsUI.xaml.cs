using System.Windows;
using Rylogic.Gui.WPF;

namespace TimeTracker
{
	public partial class OptionsUI : Window
	{
		public OptionsUI(Window owner, Settings settings)
		{
			InitializeComponent();
			Owner = owner;
			Settings = settings;
			Accept = Command.Create(this, Close);
			DataContext = this;
		}
		public Settings Settings { get; }
		public Command Accept { get; }
	}
}
