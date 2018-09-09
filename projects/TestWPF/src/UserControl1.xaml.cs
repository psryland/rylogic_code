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
using Rylogic.Gui.WPF;

namespace TestWPF
{
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class UserControl1 : UserControl
	{
		public UserControl1()
		{
			InitializeComponent();
			Cheese = "WHAT THE ACTUAL FUCK";
			m_root.DataContext = this;
			Cheese = "WHAT THE ACTUAL FUCK!!!!";
		}



		public string Cheese
		{
			get { return (string)GetValue(CheeseProperty); }
			set { SetValue(CheeseProperty, value); }
		}
		private void Cheese_Changed(string value)
		{
			Cheese = value;
		}

		// Using a DependencyProperty as the backing store for Cheese.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CheeseProperty = Gui_.DPRegister<UserControl1>(nameof(Cheese));
//			DependencyProperty.Register("Cheese", typeof(string), typeof(UserControl1), new PropertyMetadata(string.Empty));


	}
}
