using System.Windows;

namespace Rylogic.Gui.WPF
{
	public partial class PatternEditorUI : Window
	{
		public PatternEditorUI()
		{
			InitializeComponent();
		}
		public PatternEditor Editor => m_pattern_editor;
	}
}
