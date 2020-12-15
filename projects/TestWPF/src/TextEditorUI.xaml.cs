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

			Doc = new TextDocument();
			Doc.TextStyles[1] = new TextStyle(fore: Colour32.DarkRed.ToMediaBrush());
			Doc.TextStyles[2] = new TextStyle(fore: Colour32.DarkGreen.ToMediaBrush());

			Doc.Text = new CellString()
				.Append("Line0: This is a long test string\n")
				.Append("Line1: that uses different styles\n", 1)
				.Append("Line2: and spans multiple lines\n", 2)
				.Append("Line3: ", 0);

			DataContext = this;
		}

		/// <summary>Text doc</summary>
		public TextDocument Doc { get; }
	}
}
