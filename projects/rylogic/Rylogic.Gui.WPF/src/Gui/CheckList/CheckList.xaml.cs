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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Rylogic.Gui.WPF
{
	public partial class CheckList :UserControl
	{
		public CheckList()
		{
			InitializeComponent();
		}

		/// <summary>Orientation of the check list</summary>
		public Orientation Orientation
		{
			get => (Orientation)GetValue(Property);
			set => SetValue(Property, value);
		}
		public static readonly DependencyProperty Property = Gui_.DPRegister(typeof(CheckList), nameof(Orientation), Orientation.Horizontal, Gui_.EDPFlags.None);

		/// <summary>The number of check boxes in the list</summary>
		public int Length
		{
			get => (int)GetValue(LengthProperty);
			set => SetValue(LengthProperty, value);
		}

		// Using a DependencyProperty as the backing store for Length.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LengthProperty =
			DependencyProperty.Register("Length", typeof(int), typeof(CheckList), new PropertyMetadata(0));


	}
}
