using System;

namespace Rylogic.Gui.WPF.TextEditor
{
	public sealed class AnchorSegment :ISegment
	{
		// Notes:
		//  - A segment using <see cref="TextAnchor"/>s as beg and end positions.
		//  - For new anchors, the beg anchor will use movement type 'AfterInsertion'
		//    and the end anchor will use movement type 'BeforeInsertion'.
		//  - Should the end position move before the start position, the segment will have length 0.

		public AnchorSegment(TextAnchor beg, TextAnchor end)
		{
			Beg = beg;
			End = end;
		}
		public AnchorSegment(TextDocument document, ISegment segment)
			: this(document, segment.BegOffset, segment.Length)
		{}
		public AnchorSegment(TextDocument document, int offset, int length)
			: this(
				 document.CreateAnchor(offset, AnchorMapping.EMove.AfterInsertion),
				 document.CreateAnchor(offset + length, AnchorMapping.EMove.BeforeInsertion))
		{ }

		/// <summary>Segment start anchor</summary>
		public TextAnchor Beg { get; }

		/// <summary>Segment end anchor</summary>
		public TextAnchor End { get; }

		/// <inheritdoc/>
		public int Length => Math.Max(0, EndOffset - BegOffset);

		/// <inheritdoc/>
		public int BegOffset => Beg.Offset;

		/// <inheritdoc/>
		public int EndOffset => Math.Max(Beg.Offset, End.Offset);

		/// <inheritdoc/>
		public override string ToString() => $"Segment=[beg={BegOffset}, end={EndOffset}] (len={Length})";
	}
}
