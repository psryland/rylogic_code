using System.Windows;
using Rylogic.Gui.WPF.TextEditor;

namespace TestWPF
{
	public partial class TextEditorUI :Window
	{
		public TextEditorUI()
		{
			InitializeComponent();
			DataContext = this;

			{
				var line = new Line();
				line.SetText("Hello", 0);
				m_editor.Document.Add(line);
			}
			{
				var line = new Line();
				line.SetText("World", 0);
				m_editor.Document.Add(line);
			}
		}
	}
}
