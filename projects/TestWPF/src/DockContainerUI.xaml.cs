using Rylogic.Gui.WPF;
using Rylogic.Utility;
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
using System.Windows.Shapes;

namespace TestWPF
{
	/// <summary>
	/// Interaction logic for DockContainerUI.xaml
	/// </summary>
	public partial class DockContainerUI : Window
	{
		public DockContainerUI()
		{
			InitializeComponent();

			var d0 = new Dockable("Dockable 0") { Background = Brushes.LightCoral };
			var d1 = new Dockable("Dockable 1") { Background = Brushes.LightGreen };
			var d2 = new Dockable("Dockable 2") { Background = Brushes.White };
			var d3 = new Dockable("Dockable 3") { Background = Brushes.White };
			var d4 = new Dockable("Dockable 4") { Background = Brushes.White };
			var d5 = new Dockable("Dockable 5") { Background = Brushes.White };
			var d6 = new Dockable("Dockable 6") { Background = Brushes.White };
			var d7 = new Dockable("Dockable 7") { Background = Brushes.White };
			var d8 = new Dockable("Dockable 8") { Background = Brushes.White };
			var d9 = new Dockable("Dockable 9") { Background = Brushes.White };

			m_dc.Add(d0, EDockSite.Centre);
		//	m_dc.Add(d1, EDockSite.Centre);
			m_dc.Add(d4, EDockSite.Bottom);
		//	m_dc.Add(d5, EDockSite.Bottom);
		//	m_dc.Add(d2, EDockSite.Left);
		//	m_dc.Add(d3, EDockSite.Left);
		//	m_dc.Add(d6, EDockSite.Right);
		//	m_dc.Add(d7, EDockSite.Right);
		//	m_dc.Add(d8, EDockSite.Top);
		//	m_dc.Add(d9, EDockSite.Top);
		}
	}

	/// <summary>Example dockable UI element</summary>
	public class Dockable : StackPanel, IDockable
	{
		public Dockable(string text)
		{
			//BorderStyle = BorderStyle.None;
			//Text = text;
			//BackgroundImage = SystemIcons.Error.ToBitmap();
			//BackgroundImageLayout = ImageLayout.Stretch;
			DockControl = new DockControl(this, text)
			{
				TabText = text,
				//TabIcon = SystemIcons.Exclamation.ToBitmap()
			};

			Children.Add(new TextBox { Width = 140, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
			Children.Add(new TextBox { Width = 140, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
			Children.Add(new TextBox { Width = 140, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
		}

		/// <summary>Implements docking functionality</summary>
		public DockControl DockControl
		{
			[DebuggerStepThrough]
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

		/// <summary>Human friendly name</summary>
		public override string ToString()
		{
			return DockControl.TabText;
		}
	}
	//private class TextDockable : TextBox, IDockable
	//{
	//	public TextDockable(string text)
	//	{
	//		Multiline = true;
	//		DockControl = new DockControl(this, text) { TabText = text, TabIcon = SystemIcons.Question.ToBitmap() };
	//	}

	//	/// <summary>Implements docking functionality</summary>
	//	public DockControl DockControl { [DebuggerStepThrough] get; private set; }
	//}
}
