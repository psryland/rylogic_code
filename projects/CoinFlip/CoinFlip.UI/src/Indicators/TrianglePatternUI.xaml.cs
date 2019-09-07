using System.Windows;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Indicators
{
	public partial class TrianglePatternUI :Window
	{
		public TrianglePatternUI(Window owner, TrianglePattern data)
		{
			InitializeComponent();
			Owner = owner;
			Data = data;

			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>Indicator data</summary>
		public TrianglePattern Data { get; }

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
