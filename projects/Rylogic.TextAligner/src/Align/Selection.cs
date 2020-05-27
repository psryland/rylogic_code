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

		/// <summary>The buffer position range of the selected characters [First,Last)</summary>
		public readonly RangeI Pos;

		/// <summary>The lines contained in the selection [First,Last]</summary>
		public readonly RangeI Lines;

		/// <summary>The line containing the first selected character</summary>
		public readonly ITextSnapshotLine SLine;

		/// <summary>The line containing the last selected character</summary>
		public readonly ITextSnapshotLine ELine;

		/// <summary>The buffer position of the caret</summary>
		public readonly int CaretPos;

		/// <summary>The line that the caret is on</summary>
		public readonly int CaretLineNumber;

		/// <summary>True if there is no selected text, just a single caret position</summary>
		public bool IsEmpty => Pos.Empty;

		/// <summary>True if the selection is entirely on a single line</summary>
		public bool IsSingleLine => Lines.Empty;

		/// <summary>True if the selection spans whole lines</summary>
		public bool IsWholeLines => Pos.Begi == SLine.Start.Position && Pos.Endi >= ELine.End.Position; // >= because ELine.End.Position doesn't include the newline
	}
}
