using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Indicators
{
	public partial class TrendLineUI :Window
	{
		public TrendLineUI(Window owner, TrendLine data)
		{
			InitializeComponent();
			Owner = owner;
			Data = data;

			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>Indicator data</summary>
		public TrendLine Data { get; }

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
