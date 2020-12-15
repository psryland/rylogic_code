using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Rylogic.Gui.WPF.TextEditor
{
	[DebuggerDisplay("{Description,nq}")]
	public sealed class VisualLine :IDisposable
	{
		// Notes:
		//  - Represents a visual line in a 'TextView' (actually in the TextLayer).
		//  - VisualLine instances are owned by the TextView.
		//  - Multiple VisualLine instances can refer to the same Document line
		//    if there are multiple views sharing a document.
		//  - Typically corresponds to one 'Line', but can span multiple lines if folding, etc
		//  - 'pixels_per_dip' is DIP/96.0, i.e. 1.0 if DPI is 96.0, 1.25 if DIP is 120.0, etc

		internal VisualLine(TextView text_view, Line first_line, Line last_line)
		{
			TextView = text_view;
			FirstLine = first_line;
			LastLine = last_line;
			TextLines = Array.Empty<TextLine>();
		}
		public void Dispose()
		{
			Graphics = null;
		}

		/// <summary>The view this line is rendered within</summary>
		public TextView TextView { get; }

		/// <summary>The document that the 'Line's refer to</summary>
		public TextDocument Document => TextView.Document ?? throw new Exception("The document cannot be null");

		/// <summary>The first document line displayed by this visual line.</summary>
		public Line FirstLine { get; }

		/// <summary>The last document line displayed by this visual line. (inclusive)</summary>
		public Line LastLine { get; }

		/// <summary>Enumerate the document lines spanned by this visual line</summary>
		public IEnumerable<Line> DocumentLines
		{
			get
			{
				for (var line = FirstLine; line != null; line = FirstLine.NextLine)
				{
					yield return line;
					if (line == LastLine)
						break;
				}
			}
		}

		/// <summary>The length of this line in visual columns</summary>
		public int Length => DocumentLines.Max(x => x.LengthWithLineEnd);

		/// <summary>The width (in DIP) of this line</summary>
		public double LineWidth => m_line_width ?? 0.0;
		private double? m_line_width;

		/// <summary>The height (in DIP) of this line</summary>
		public double LineHeight => m_line_height ?? TextView.DefaultLineHeight;
		private double? m_line_height;

		/// <summary>The Y position (in DIP) of this line in the document</summary>
		public double YPos { get; internal set; }

		/// <summary>The visual position of this line at the given column.</summary>
		public Point VisualPosition(int column, VisualYPosition mode)
		{
			// Clamp the column to within the range of the line
			var col = Math.Max(0, Math.Min(column, FirstLine.LengthWithLineEnd));

			var text_line = TextLine(col) ?? throw new NotSupportedException("Need to handle columns off the end");
			var xpos = text_line.GetDistanceFromCharacterHit(new CharacterHit(col, 0)) + Math.Max(0, column - col) * TextView.WideSpaceWidth;
			var ypos = YPos + YOffset(TextView, text_line, mode);
			return new Point(xpos, ypos);
		}

		/// <summary>The formatted text lines for this visual line</summary>
		private IReadOnlyCollection<TextLine> TextLines { get; set; }
		private Size m_text_lines_area; // The area used to generate the text lines

		/// <summary>Return the formatted text line at the given column</summary>
		private TextLine? TextLine(int column)
		{
			foreach (var line in TextLines)
			{
				//	line.
			}
			return null;
		}

		/// <summary>Generate the TextLine's for this visual line</summary>
		internal void EnsureTextLines(Size area)
		{
			// Already generated and not invalidated
			if (TextLines.Count != 0 && m_text_lines_area == area)
				 return;

			// If there is no formatter, then there's no lines
			if (!(TextView.Formatter is TextFormatter formatter))
			{
				TextLines = Array.Empty<TextLine>();
				m_text_lines_area = Size.Empty;
				m_line_width = null;
				m_line_height = null;
				return;
			}

			// Create a text source 
			var src = new VisualLineTextSource(this, Document.TextStyles);
			var prev_line_break = (TextLineBreak?)null;

			m_line_width = 0.0;
			m_line_height = 0.0;

			// Generate the TextLines
			var text_lines = new List<TextLine>();
			for (int i = 0; i < src.Length;)
			{
				var text_line = formatter.FormatLine(src, i, area.Width, TextView.ParaStyle, prev_line_break);
				text_lines.Add(text_line);
				i += text_line.Length;

				m_line_height += text_line.Height;
				m_line_width = Math.Max(m_line_width.Value, text_line.WidthIncludingTrailingWhitespace);
			}

			// Cache the result
			TextLines = text_lines.AsReadOnly();
			m_text_lines_area = area;
			Graphics = null;

			// Update the document total height
			Document.UpdateLine(FirstLine, recursive: true);
		}

		/// <summary>Invalidates the graphics for this visual line</summary>
		public void Invalidate()
		{
			TextLines = Array.Empty<TextLine>();
			m_text_lines_area = Size.Empty;
			m_line_width = null;
			m_line_height = null;
			Graphics = null;
		}

		/// <summary>Return the WPF DrawingVisual for this line</summary>
		internal DrawingVisual? Graphics
		{
			get => m_gfx ?? (Graphics = new Gfx(this));
			set
			{
				if (m_gfx == value) return;
				if (m_gfx != null)
				{
					// Remove the line from the text layer
					TextView.TextLayer.Visuals.Remove(m_gfx);
				}
				m_gfx = value;
			}
		}
		private DrawingVisual? m_gfx;
		internal class Gfx :DrawingVisual
		{
			private readonly VisualLine m_vis_line;
			public Gfx(VisualLine vis_line)
			{
				m_vis_line = vis_line;
				
				using var scope = this.RenderScope();
				var dc = scope.Value;
				
				var ypos = 0.0;
				foreach (var line in m_vis_line.TextLines)
				{
					line.Draw(dc, new Point(0, ypos), InvertAxes.None);
					ypos += line.Height;
				}

				Height = ypos;
			}

			/// <summary>The height of this visual</summary>
			public double Height { get; }
		}

		/// <summary>True if 'doc_line' is drawn by this visual line</summary>
		private bool Contains(Line doc_line)
		{
			foreach (var line in DocumentLines)
				if (line == doc_line)
					return true;

			return false;
		}

		/// <summary></summary>
		private static double YOffset(TextView tv, TextLine tl, VisualYPosition mode)
		{
			return mode switch
			{
				VisualYPosition.LineTop => 0.0,
				VisualYPosition.LineMiddle => 0.0 + tl.Height / 2,
				VisualYPosition.LineBottom => 0.0 + tl.Height,
				VisualYPosition.TextTop => 0.0 + tl.Baseline - tv.DefaultBaseline,
				VisualYPosition.TextBottom => 0.0 + tl.Baseline - tv.DefaultBaseline + tv.DefaultLineHeight,
				VisualYPosition.TextMiddle => 0.0 + tl.Baseline - tv.DefaultBaseline + tv.DefaultLineHeight / 2,
				VisualYPosition.Baseline => 0.0 + tl.Baseline,
				_ => throw new ArgumentException($"Unknown VisualYPosition mode: {mode}"),
			};
		}

		#region Height

		#endregion

		#region TextSource

		private class VisualLineTextSource :TextSource
		{
			//todo: handle more than just FirstLine...

			private readonly VisualLine m_line;
			private readonly TextStyleMap m_styles;

			public VisualLineTextSource(VisualLine line, TextStyleMap styles)
			{
				m_line = line;
				m_styles = styles;
			}

			/// <summary>An empty text span</summary>
			private static readonly TextSpan<CultureSpecificCharacterBufferRange> EmptyTextSpan =
				new TextSpan<CultureSpecificCharacterBufferRange>(CharacterBufferRange.Empty.Length,
					new CultureSpecificCharacterBufferRange(null, CharacterBufferRange.Empty));

			/// <summary>The length of the text in this source</summary>
			public int Length => m_line.FirstLine.Length;

			/// <inheritdoc/>
			public override TextRun GetTextRun(int idx)
			{
				if (idx >= Length)
					return new TextEndOfParagraph(1);

				// Might have to handle 'idx' in the LineEnd range...
				var range = m_line.FirstLine.Text.SpanAt(idx);
				var style = m_styles[m_line.FirstLine.Text[range.Begi].sty];
				return new TextCharacters(m_line.FirstLine.Text.ToCharArray(), range.Begi, range.Sizei, style);
			}

			/// <inheritdoc/>
			public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int idx)
			{
				if (idx <= 0) return EmptyTextSpan;
				idx = Math.Min(idx, m_line.FirstLine.Length);
				var text = (string)m_line.FirstLine.Text;
				var range = m_line.FirstLine.Text.SpanAt(idx - 1);
				var style = m_styles[m_line.FirstLine.Text[range.Begi].sty];

				var char_range = new CharacterBufferRange(text.ToCharArray(), range.Begi, range.Sizei);
				return new TextSpan<CultureSpecificCharacterBufferRange>(char_range.Length, new CultureSpecificCharacterBufferRange(style.CultureInfo, char_range));
			}

			/// <inheritdoc/>
			public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		#region Diagnostics

		/// <summary>Debug description</summary>
		private string Description => $"[VisLine] \"{FirstLine.Text}\"";

		#endregion
	}
}

			///// <summary>Text ready to render</summary>
			//public IEnumerable<FormattedText> FormattedText(StyleMap styles)
			//{
			//	// No text on this line
			//	if (Text.Length == 0)
			//		yield break;

//	// If the formatted text isn't cached, recreate it
//	if (m_formatted.Count == 0)
//	{
//		m_formatted.Clear();
//		Height = 0.0;

//		var sb = new StringBuilder(256);
//		for (int s = 0, e = 0; s != Length; s = e)
//		{
//			sb.Length = 0;

//			// Find the span of characters with the same style
//			sb.Append(Text[s].ch);
//			for (e = s + 1; e != Length && Text[e].sty == Text[s].sty; ++e)
//				sb.Append(Text[e].ch);

//			// Get the style for the span
//			var style = styles.TryGetValue(Text[s].sty, out var sty) && sty is TextStyle ts ? ts : TextStyle.Default;

//			// Create formatted text for the span
//			var formatted = new FormattedText(sb.ToString(), CultureInfo.CurrentCulture, style.Flow, style.Typeface, style.EmSize, style.ForeColor, style.PixelsPerDIP);
//			m_formatted.Add(formatted);

//			// Record the line height
//			Height = Math.Max(Height, formatted.Height);
//		}
//	}

//	// Return the formatted text
//	foreach (var ft in m_formatted)
//		yield return ft;
//}
//private List<FormattedText> m_formatted = new List<FormattedText>();

