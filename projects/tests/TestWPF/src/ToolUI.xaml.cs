using System.Windows;
using Rylogic.Gui.WPF;

namespace TestWPF
{
	/// <summary>
	/// Interaction logic for TestToolWindow.xaml
	/// </summary>
	public partial class ToolUI : Window, IPinnable
	{
		public ToolUI()
		{
			InitializeComponent();
			PinState = new PinData(this, EPin.TopLeft, true);
			DataContext = this;
		}

		public PinData PinState { get; }
	}
}
