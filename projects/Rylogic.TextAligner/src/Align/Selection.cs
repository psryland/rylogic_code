using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Rylogic.Common;
using Rylogic.Maths;

namespace Rylogic.TextAligner
{
	/// <summary></summary>
	internal struct Selection
	{
		public readonly RangeI Pos;              // The buffer position range of the selected characters [First,Last)
		public readonly RangeI Lines;            // The lines contained in the selection [First,Last]
		public readonly ITextSnapshotLine SLine; // The line containing the first selected character
		public readonly ITextSnapshotLine ELine; // The line containing the last selected character
		public readonly int CaretPos;            // The buffer position of the caret
		public readonly int CaretLineNumber;     // The line that the caret is on

		public Selection(IWpfTextView view)
		{
			var snapshot = view.TextSnapshot;
			var selection = view.Selection;
			var caret = view.Caret;

			Pos = new RangeI(selection.Start.Position, selection.End.Position);
			Lines = new RangeI(
				snapshot.GetLineNumberFromPosition(Pos.Begi),
				snapshot.GetLineNumberFromPosition(Pos.Empty ? Pos.Begi : Pos.Endi - 1));

			SLine = snapshot.GetLineFromLineNumber(Lines.Begi);
			ELine = snapshot.GetLineFromLineNumber(Lines.Endi);

			CaretPos = Math_.Clamp(caret.Position.BufferPosition, SLine.Start.Position, ELine.End.Position);
			CaretLineNumber = snapshot.GetLineNumberFromPosition(CaretPos);

			Debug.Assert(Pos.Size >= 0);
			Debug.Assert(Lines.Size >= 0);
		}
		public bool IsEmpty => Pos.Empty;
		public bool IsSingleLine => Lines.Empty;
		public bool IsWholeLines => Pos.Begi == SLine.Start.Position && Pos.Endi >= ELine.End.Position; // >= because ELine.End.Position doesn't include the newline
	}
}
