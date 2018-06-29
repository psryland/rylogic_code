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

namespace TestWPF
{
	/// <summary>Interaction logic for MainWindow.xaml</summary>
	public partial class MainWindow :Window
	{
		public MainWindow()
		{
			InitializeComponent();

			m_menu_file_close.Click += (s, a) =>
			{
				Close();
			};
			m_menu_tests_diagram.Click += (s, a) =>
			{
				new DiagramUI().Show();
			};
			m_menu_tests_view3d.Click += (s, a) =>
			{
				new View3dUI().Show();
			};
			m_menu_tests_dock_container.Click += (s, a) =>
			{
				new DockContainerUI().Show();
			};
			m_menu_tests_pattern_editor.Click += (s, a) =>
			{
				new PatternEditorUI().Show();
			};
			//var grid = new Canvas();
			//var btn = new ImageButton
			//{
			//	Width = 180,
			//	Height=40,
			//	Content = "Push ME!",
			//	Template = (ControlTemplate)Resources["MyControl"]
			//};

			//grid.Children.Add(btn);
			//this.Content = grid;
		}
	}
}
