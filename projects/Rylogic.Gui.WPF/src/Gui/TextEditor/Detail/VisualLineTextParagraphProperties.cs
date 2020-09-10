using System.Windows;
using System.Windows.Media.TextFormatting;

namespace Rylogic.Gui.WPF.TextEditor
{
	internal sealed class VisualLineTextParagraphProperties :TextParagraphProperties
	{
		public VisualLineTextParagraphProperties(TextRunProperties text_run_propertries, double tab_size = 0.0, bool first_line = false, TextWrapping text_wrapping = TextWrapping.NoWrap, double indent = 0.0)
		{
			m_default_text_run_properties = text_run_propertries;
			m_tab_size = tab_size;
			m_first_line_in_paragraph = first_line;
			m_text_wrapping = text_wrapping;
			m_indent = indent;
		}

		/// <inheritdoc/>
		public override double DefaultIncrementalTab => m_tab_size;
		private double m_tab_size;

		/// <inheritdoc/>
		public override TextRunProperties DefaultTextRunProperties => m_default_text_run_properties;
		private TextRunProperties m_default_text_run_properties;

		/// <inheritdoc/>
		public override FlowDirection FlowDirection => FlowDirection.LeftToRight;

		/// <inheritdoc/>
		public override TextAlignment TextAlignment => TextAlignment.Left;

		/// <inheritdoc/>
		public override double LineHeight => double.NaN;

		/// <inheritdoc/>
		public override bool FirstLineInParagraph => m_first_line_in_paragraph;
		private bool m_first_line_in_paragraph;

		/// <inheritdoc/>
		public override TextWrapping TextWrapping => m_text_wrapping;
		private TextWrapping m_text_wrapping;

		/// <inheritdoc/>
		public override TextMarkerProperties? TextMarkerProperties => null;

		/// <inheritdoc/>
		public override double Indent => m_indent;
		private double m_indent;
	}
}

