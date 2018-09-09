using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// <summary>Interaction logic for MainWindow.xaml</summary>
	public partial class MainWindow :Window
	{
		public MainWindow()
		{
			InitializeComponent();

			m_recent_files.Add("One", false);
			m_recent_files.Add("Two", false);
			m_recent_files.Add("Three");
			m_recent_files.RecentFileSelected += (s) =>
			{
				Debug.WriteLine(s);
			};

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

			m_btn_msgbox.Click += (s, a) =>
			{
				new MsgBox(this, "The message what goes in my message box", "My Message Box", MsgBox.EButtons.OKCancel, MsgBox.EIcon.Information).ShowDialog();
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
