using System.Windows;

namespace CoinFlip.UI.Dialogs
{
	public partial class BackTestingOptionsUI :Window
	{
		public BackTestingOptionsUI(Window owner, SimulationView sim)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;
			DataContext = sim;
		}
	}
}
