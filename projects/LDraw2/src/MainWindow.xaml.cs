using LDraw.UI;
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

namespace LDraw
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			// Commands
			NewScene = Command.Create(this, NewSceneInternal);
			ShowAbout = Command.Create(this, ShowAboutInternal);

			m_dc.Options.AlwaysShowTabs = true;
			m_dc.Add(new SceneUI("Scene"), EDockSite.Centre);
			m_dc.Add(new SceneUI("Script"), EDockSite.Left);
			DataContext = this;
		}

		/// <summary></summary>
		public Command NewScene { get; }
		private void NewSceneInternal()
		{
			m_dc.Add(new SceneUI("Scene"), EDockSite.Centre);
		}

		/// <summary>Show the about box</summary>
		public Command ShowAbout { get; }
		private void ShowAboutInternal()
		{
		}
	}
}
