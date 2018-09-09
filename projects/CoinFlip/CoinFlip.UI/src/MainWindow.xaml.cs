using Rylogic.Gui.WPF;
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

namespace CoinFlip.UI
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			SetupUI();
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Menu
			m_menu_file_skin_toggle.Click += (s, a) =>
			{
				//ThemeManager.ChangeTheme(Application.Current, "Dark");
			};
			m_menu_file_close.Click += (s, a) =>
			{
				base.Close();
			};
			#endregion
		}
	}
}
