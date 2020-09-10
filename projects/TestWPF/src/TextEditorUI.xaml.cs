using System.Windows;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.TextEditor;

namespace TestWPF
{
	public partial class TextEditorUI :Window
	{
		public TextEditorUI()
		{
			InitializeComponent();
			DataContext = this;

			m_editor.Document.Styles[1] = new TextStyle(fore: Colour32.Red.ToMediaBrush());
			m_editor.Document.Styles[2] = new TextStyle(fore: Colour32.Green.ToMediaBrush());

			m_editor.Document.Lines.Add(new Line("Hello"));
			m_editor.Document.Lines.Add(new Line("World", 1));
			m_editor.Document.Lines.Add(new Line("One", 2));
			m_editor.Document.Lines.Add(new Line("Two"));
			m_editor.Document.Lines.Add(new Line("Three"));
			m_editor.Document.Lines.Add(new Line("Four"));
			m_editor.Document.Lines.Add(new Line("Five"));
			m_editor.Document.Lines.Add(new Line("Six"));
			m_editor.Document.Lines.Add(new Line("Seven"));
			m_editor.Document.Lines.Add(new Line("Eight"));
			m_editor.Document.Lines.Add(new Line("Nine"));
			m_editor.Document.Lines.Add(new Line("Ten"));
			m_editor.Document.Lines.Add(new Line("Yi"));
			m_editor.Document.Lines.Add(new Line("Er"));
			m_editor.Document.Lines.Add(new Line("San"));
			m_editor.Document.Lines.Add(new Line("Si"));
			m_editor.Document.Lines.Add(new Line("Wu"));
			m_editor.Document.Lines.Add(new Line("Liu"));
			m_editor.Document.Lines.Add(new Line("Qi"));
			m_editor.Document.Lines.Add(new Line("Ba"));
			m_editor.Document.Lines.Add(new Line("Jiu"));
			m_editor.Document.Lines.Add(new Line("Shi"));
		}
	}
}
