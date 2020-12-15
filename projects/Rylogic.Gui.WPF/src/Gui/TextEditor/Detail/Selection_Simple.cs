using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.TextEditor
{
	internal sealed class SimpleSelection : Selection
	{
		public SimpleSelection(TextArea text_area)
			: base(text_area)
		{ }
		public SimpleSelection(TextArea text_area, TextViewPosition beg, TextViewPosition end)
			: base(text_area)
		{
			Beg = beg;
			End = end;
			BegOffset = text_area.Document.Offset(beg.Location);
			EndOffset = text_area.Document.Offset(end.Location);
		}

		/// <summary>Selection start position</summary>
		private TextViewPosition Beg { get; }

		/// <summary>Selection end position</summary>
		private TextViewPosition End { get; }

		/// <summary>Selection beg offset</summary>
		private int BegOffset { get; }

		/// <summary>Selection end offset</summary>
		private int EndOffset { get; }

		/// <inheritdoc/>
		public override TextViewPosition BegPosition => Beg;

		/// <inheritdoc/>
		public override TextViewPosition EndPosition => End;

		/// <inheritdoc/>
		public override bool IsEmpty => BegOffset == EndOffset;

		/// <inheritdoc/>
		public override int Length => Math.Abs(EndOffset - BegOffset);

		/// <inheritdoc/>
		public override IEnumerable<SelectionSegment> Segments => Enumerable_.Sequence(new SelectionSegment(BegOffset, Beg.VisualColumn ?? -1, EndOffset, End.VisualColumn ?? -1));

		/// <inheritdoc/>
		public override ISegment SurroundingSegment => new SelectionSegment(BegOffset, EndOffset);

		/// <inheritdoc/>
		public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e)
		{
			return Create(TextArea,
				new TextViewPosition(TextArea.Document.Location(e.NewOffset(BegOffset, AnchorMapping.EMove.Default)), Beg.VisualColumn),
				new TextViewPosition(TextArea.Document.Location(e.NewOffset(EndOffset, AnchorMapping.EMove.Default)), End.VisualColumn));
		}

		/// <inheritdoc/>
		public override void ReplaceSelectionWithText(string new_text)
		{
			//using (TextArea.Document.RunUpdate())
			//{
			//	ISegment[] segmentsToDelete = TextArea.GetDeletableSegments(this.SurroundingSegment);
			//	for (int i = segmentsToDelete.Length - 1; i >= 0; i--)
			//	{
			//		if (i == segmentsToDelete.Length - 1)
			//		{
			//			if (segmentsToDelete[i].Offset == SurroundingSegment.Offset && segmentsToDelete[i].Length == SurroundingSegment.Length)
			//			{
			//				new_text = AddSpacesIfRequired(new_text, Beg, End);
			//			}
			//			int vc = TextArea.Caret.VisualColumn;
			//			TextArea.Caret.Offset = segmentsToDelete[i].EndOffset;
			//			if (string.IsNullOrEmpty(new_text))
			//				TextArea.Caret.VisualColumn = vc;
			//			TextArea.Document.Replace(segmentsToDelete[i], new_text);
			//		}
			//		else
			//		{
			//			TextArea.Document.Remove(segmentsToDelete[i]);
			//		}
			//	}
			//	if (segmentsToDelete.Length != 0)
			//	{
			//		TextArea.ClearSelection();
			//	}
			//}
		}


#if false
		/// <inheritdoc/>
		public override Selection SetEndpoint(TextViewPosition endPosition)
		{
			return Create(TextArea, Beg, endPosition);
		}

		public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
		{
			var document = TextArea.Document;
			if (document == null)
				throw ThrowUtil.NoDocumentAssigned();
			return Create(TextArea, Beg, endPosition);
		}
#endif
		/// <inheritdoc/>
		public override string ToString() => $"[SimpleSelection Beg={Beg} End={End}]";

		#region Equals

		/// <inheritdoc/>
		public override bool Equals(object? obj)
		{
			return
				obj is SimpleSelection rhs &&
				Beg.Equals(rhs.Beg) &&
				End.Equals(rhs.End) &&
				BegOffset == rhs.BegOffset &&
				EndOffset == rhs.EndOffset &&
				TextArea == rhs.TextArea;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				return BegOffset * 27811 + EndOffset + TextArea.GetHashCode();
			}
		}

		#endregion
	}
}
