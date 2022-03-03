using System.Windows;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Dialogs
{
	public partial class BackTestingOptionsUI :Window
	{
		public BackTestingOptionsUI(Window owner, SimulationView sim)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;
			PinState = new PinData(this);
			DataContext = sim;
		}

		/// <summary>Pin window support</summary>
		private PinData PinState { get; }
	}
}
