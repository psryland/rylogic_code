using System.Windows;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace TestWPF
{
	/// <summary>Interaction logic for view3d_ui.xaml</summary>
	public partial class View3dUI :Window
	{
		public View3dUI()
		{
			InitializeComponent();

			m_view0.Window.CreateDemoScene();
			//m_view1.Window.CreateDemoScene();

			Closed += Gui_.DisposeChildren;
		}
	}
}
