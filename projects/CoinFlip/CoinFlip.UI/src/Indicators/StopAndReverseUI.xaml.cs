using System.Windows;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Indicators
{
	/// <summary>
	public partial class StopAndReverseUI :Window
	{
		public StopAndReverseUI(Window owner, StopAndReverse data)
		{
			InitializeComponent();
			Owner = owner;
			Data = data;

			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>Indicator data</summary>
		public StopAndReverse Data { get; }

		/// <summary>Close the dialog</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			if (this.IsModal())
				DialogResult = true;

			Close();
		}
	}
}