using System.Windows;
using Rylogic.Gfx;

namespace LDraw.Dialogs
{
	public partial class ExampleScriptUI :Window
	{
		public ExampleScriptUI()
		{
			InitializeComponent();
			m_scintilla_control.Text = View3d.ExampleScript;
		}
	}
}
