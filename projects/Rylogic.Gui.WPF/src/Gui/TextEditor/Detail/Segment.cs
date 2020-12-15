using System;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary>An (Offset,Length)-pair.</summary>
	public interface ISegment
	{
		// Notes:
		//  - This is an interface rather than an abstract base class because
		//    SimpleSegment is a struct.
		
		/// <summary>Gets the length of the segment.</summary>
		int Length { get; }

		/// <summary>Gets the start offset of the segment.</summary>
		int BegOffset { get; }

		/// <summary>Gets the end offset of the segment.</summary>
		int EndOffset { get; } // => BegOffset + Length;
	}
	public static class Segment_
	{
		/// <summary>Invalid segment</summary>
		public static readonly ISegment Invalid = new SimpleSegment(-1, -1);

		/// <summary>True, if 'offset' is between [BegOffset, EndOffset] (inclusive)</summary>
		public static bool Contains(this ISegment segment, int offset)
		{
			var beg = segment.BegOffset;
			var end = beg + segment.Length;
			return offset >= beg && offset <= end;
		}
		
		/// <summary>Gets the overlapping portion of the segments. Returns SimpleSegment.Invalid if the segments don't overlap.</summary>
		public static ISegment GetOverlap(this ISegment segment, ISegment other)
		{
			var beg = Math.Max(segment.BegOffset, other.BegOffset);
			var end = Math.Min(segment.EndOffset, other.EndOffset);
			return end >= beg ? new SimpleSegment(beg, end - beg) : Segment_.Invalid;
		}
	}
}
