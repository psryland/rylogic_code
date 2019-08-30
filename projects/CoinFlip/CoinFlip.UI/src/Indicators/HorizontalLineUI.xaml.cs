using System.Windows;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Indicators
{
	public partial class HorizontalLineUI :Window
	{
		public HorizontalLineUI(Window owner, HorizontalLine data)
		{
			InitializeComponent();
			Owner = owner;
			Data = data;

			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>Indicator data</summary>
		public HorizontalLine Data { get; }

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
