using System;
using System.Collections.Generic;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary>Base class for selections.</summary>
	public abstract class Selection
	{
		/// <summary>Creates a new simple selection that selects the text from beg_offset to end_offset.</summary>
		public static Selection Create(TextArea text_area, int beg_offset, int end_offset)
		{
			if (beg_offset == end_offset)
				return text_area.EmptySelection;

			return new SimpleSelection(text_area,
				new TextViewPosition(text_area.Document.Location(beg_offset)),
				new TextViewPosition(text_area.Document.Location(end_offset)));
		}
		public static Selection Create(TextArea text_area, TextViewPosition beg, TextViewPosition end)
		{
			var doc = text_area.Document;
			return doc.Offset(beg.Location) != doc.Offset(end.Location) || beg.VisualColumn != end.VisualColumn
				? new SimpleSelection(text_area, beg, end)
				: text_area.EmptySelection;
		}

		/// <summary>Creates a new simple selection that selects the text in the specified segment.</summary>
		public static Selection Create(TextArea text_area, ISegment segment)
		{
			return Create(text_area, segment.BegOffset, segment.EndOffset);
		}

		/// <summary>Constructor for Selection.</summary>
		protected Selection(TextArea text_area)
		{
			TextArea = text_area;
		}

		/// <summary>The TextArea that the selection is within</summary>
		internal TextArea TextArea { get; }

		/// <summary>Gets whether the selection is empty.</summary>
		public virtual bool IsEmpty => Length == 0;

		/// <summary>Gets the selection length.</summary>
		public abstract int Length { get; }

		/// <summary>Gets the selected text.</summary>
		public virtual CellString GetText()
		{
			var doc = TextArea.Document;
			if (doc == null)
				throw new Exception("No associated TextDocument");

			var iter = Segments.GetEnumerator();

			// No segments
			if (!iter.MoveNext())
				return CellString.Empty;

			// If there is only one segment, return the text directly
			var text = doc.GetText(iter.Current);
			if (!iter.MoveNext())
				return text;

			// Otherwise, combine text from the segments
			var cstr = new CellString(text) { Capacity = Length };
			for (var more = true; more; more = iter.MoveNext())
				cstr.Append(doc.GetText(iter.Current));

			return cstr;
		}

		/// <summary>Gets the start position of the selection.</summary>
		public abstract TextViewPosition BegPosition { get; }

		/// <summary>Gets the end position of the selection.</summary>
		public abstract TextViewPosition EndPosition { get; }

		/// <summary>Gets the selected text segments.</summary>
		public abstract IEnumerable<SelectionSegment> Segments { get; }

		/// <summary>Gets the smallest segment that contains all segments in this selection. May return an invalid segment if the selection contains no segments</summary>
		public abstract ISegment SurroundingSegment { get; }

		/// <summary>Replaces the selection with the specified text.</summary>
		public abstract void ReplaceSelectionWithText(string new_text);

		/// <summary>Updates the selection when the document changes.</summary>
		public abstract Selection UpdateOnDocumentChange(DocumentChangeEventArgs e);

#if false
		bool InsertVirtualSpaces(string newText, TextViewPosition start, TextViewPosition end)
		{
			return (!string.IsNullOrEmpty(newText) || !(IsInVirtualSpace(start) && IsInVirtualSpace(end)))
				&& newText != "\r\n"
				&& newText != "\n"
				&& newText != "\r";
		}

		bool IsInVirtualSpace(TextViewPosition pos)
		{
			return pos.VisualColumn > TextArea.TextView.GetOrConstructVisualLine(TextArea.Document.GetLineByNumber(pos.Line)).VisualLength;
		}

		/// <summary>
		/// Gets whether virtual space is enabled for this selection.
		/// </summary>
		public virtual bool EnableVirtualSpace
		{
			get { return TextArea.Options.EnableVirtualSpace; }
		}

		/// <summary>
		/// Returns a new selection with the changed end point.
		/// </summary>
		/// <exception cref="NotSupportedException">Cannot set endpoint for empty selection</exception>
		public abstract Selection SetEndpoint(TextViewPosition endPosition);

		/// <summary>
		/// If this selection is empty, starts a new selection from <paramref name="startPosition"/> to
		/// <paramref name="endPosition"/>, otherwise, changes the endpoint of this selection.
		/// </summary>
		public abstract Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition);

		/// <summary>
		/// Gets whether the selection is multi-line.
		/// </summary>
		public virtual bool IsMultiline
		{
			get
			{
				ISegment surroundingSegment = this.SurroundingSegment;
				if (surroundingSegment == null)
					return false;
				int start = surroundingSegment.Offset;
				int end = start + surroundingSegment.Length;
				var document = TextArea.Document;
				if (document == null)
					throw ThrowUtil.NoDocumentAssigned();
				return document.GetLineByOffset(start) != document.GetLineByOffset(end);
			}
		}


		/// <summary>
		/// Creates a HTML fragment for the selected text.
		/// </summary>
		public string CreateHtmlFragment(HtmlOptions options)
		{
			if (options == null)
				throw new ArgumentNullException("options");
			IHighlighter highlighter = TextArea.GetService(typeof(IHighlighter)) as IHighlighter;
			StringBuilder html = new StringBuilder();
			bool first = true;
			foreach (ISegment selectedSegment in this.Segments)
			{
				if (first)
					first = false;
				else
					html.AppendLine("<br>");
				html.Append(HtmlClipboard.CreateHtmlFragment(TextArea.Document, highlighter, selectedSegment, options));
			}
			return html.ToString();
		}

		/// <inheritdoc/>
		public abstract override bool Equals(object obj);

		/// <inheritdoc/>
		public abstract override int GetHashCode();

		/// <summary>
		/// Gets whether the specified offset is included in the selection.
		/// </summary>
		/// <returns>True, if the selection contains the offset (selection borders inclusive);
		/// otherwise, false.</returns>
		public virtual bool Contains(int offset)
		{
			if (this.IsEmpty)
				return false;
			if (this.SurroundingSegment.Contains(offset))
			{
				foreach (ISegment s in this.Segments)
				{
					if (s.Contains(offset))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Creates a data object containing the selection's text.
		/// </summary>
		public virtual DataObject CreateDataObject(TextArea textArea)
		{
			string text = GetText();
			// Ensure we use the appropriate newline sequence for the OS
			DataObject data = new DataObject(TextUtilities.NormalizeNewLines(text, Environment.NewLine));
			// we cannot use DataObject.SetText - then we cannot drag to SciTe
			// (but dragging to Word works in both cases)

			// Also copy text in HTML format to clipboard - good for pasting text into Word
			// or to the SharpDevelop forums.
			HtmlClipboard.SetHtml(data, CreateHtmlFragment(new HtmlOptions(textArea.Options)));
			return data;
		}

		internal string AddSpacesIfRequired(string new_text, TextViewPosition beg, TextViewPosition end)
		{
			if (EnableVirtualSpace && InsertVirtualSpaces(new_text, beg, end))
			{
				var line = TextArea.Document.LineByNumber(beg.Line);
				string lineText = TextArea.Document.GetText(line);
				var vLine = TextArea.TextView.GetOrConstructVisualLine(line);
				int colDiff = beg.VisualColumn - vLine.VisualLengthWithEndOfLineMarker;
				if (colDiff > 0)
				{
					string additionalSpaces = "";
					if (!TextArea.Options.ConvertTabsToSpaces && lineText.Trim('\t').Length == 0)
					{
						int tabCount = (int)(colDiff / TextArea.Options.IndentationSize);
						additionalSpaces = new string('\t', tabCount);
						colDiff -= tabCount * TextArea.Options.IndentationSize;
					}
					additionalSpaces += new string(' ', colDiff);
					return additionalSpaces + new_text;
				}
			}
			return new_text;
		}
#endif

		#region Equals

		public static bool operator ==(Selection? lhs, Selection? rhs)
		{
			if ((object?)lhs == null && (object?)rhs == null) return true;
			if ((object?)lhs == null || (object?)rhs == null) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(Selection? lhs, Selection? rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object? obj)
		{
			return base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		#endregion
	}
	}
