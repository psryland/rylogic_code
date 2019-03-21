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
			m_menu_tests_chart.Click += (s, a) =>
			{
				new ChartUI().Show();
			};
			m_menu_tests_diagram.Click += (s, a) =>
			{
				new DiagramUI().Show();
			};
			m_menu_tests_dock_container.Click += (s, a) =>
			{
				new DockContainerUI().Show();
			};
			m_menu_tests_message_box_ui.Click += (s, a) =>
			{
				MsgBox.Show(this, "Informative isn't it", "Massage Box", MsgBox.EButtons.YesNoCancel, MsgBox.EIcon.Exclamation);
			};
			m_menu_tests_pattern_editor.Click += (s, a) =>
			{
				new PatternEditorUI().Show();
			};
			m_menu_tests_prompt_ui.Click += (s, a) =>
			{
				var dlg = new PromptUI
				{
					Owner = this,
					Title = "Prompting isn't it...",
					Prompt = "I'll have what she's having. Really long message\r\nwith new lines in and \r\n other stuff\r\n\r\nType 'Hello' into the field",
					Value = "Really long value as well, that hopefully wraps around",
					Image = FindResource("pencil") as BitmapImage
				};
				if (dlg.ShowDialog() == true)
					Debug.Assert(dlg.Value == "Hello");
			};
			m_menu_tests_view3d.Click += (s, a) =>
			{
				new View3dUI().Show();
			};
			m_menu_tests_tool_ui.Click += (s, a) =>
			{
				new ToolUI { Owner = this }.Show();
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
