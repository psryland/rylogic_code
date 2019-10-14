using System.Windows;
using Rylogic.Gfx;

namespace LDraw.Dialogs
{
	public partial class ExampleScriptUI :Window
	{
		public ExampleScriptUI(Window owner)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;
			m_scintilla_control.Text = View3d.ExampleScript;
		}
	}
}
