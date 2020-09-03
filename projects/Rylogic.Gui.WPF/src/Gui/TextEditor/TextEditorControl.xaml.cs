using System;
using System.Collections;
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
using Rylogic.Gui.WPF.TextEditor;

namespace Rylogic.Gui.WPF
{
	public partial class TextEditorControl :Control
	{
		// Notes:
		//  - This is a Scintilla-inspired pure WPF text editor control
		//  - 
		public TextEditorControl()
		{
			InitializeComponent();
			Document = new Document();
		}

		/// <summary>The styles</summary>
		public StyleMap Styles { get; } = new StyleMap();

		/// <summary>The text displayed in the control</summary>
		public Document Document
		{
			get => m_doc;
			set
			{
				if (m_doc == value) return;
				m_doc = value;
			}
		}
		private Document m_doc = null!;

		/// <summary></summary>
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			
			var line_origin = new Point();
			foreach (var line in Document.Lines)
			{
				foreach (var ft in line.FormattedText(Styles))
				{
					dc.DrawText(ft, line_origin);
					line_origin.X += ft.WidthIncludingTrailingWhitespace;
				}
				line_origin.Y += line.Height;
			}
		}
	}
}