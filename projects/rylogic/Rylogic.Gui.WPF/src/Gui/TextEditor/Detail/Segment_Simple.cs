using System;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary>A simple segment (Offset, Length) pair that is not automatically updated on document changes.</summary>
	internal struct SimpleSegment :IEquatable<SimpleSegment>, ISegment
	{
		public SimpleSegment(int offset, int length)
		{
			Length = length;
			BegOffset = offset;
		}
		public SimpleSegment(ISegment segment)
		{
			Length = segment.Length;
			BegOffset = segment.BegOffset;
		}

		/// <inheritdoc/>
		public int Length { get; }

		/// <inheritdoc/>
		public int BegOffset { get; }

		/// <inheritdoc/>
		public int EndOffset => BegOffset + Length;

		/// <inheritdoc/>
		public override string ToString() => $"Segment [Offset={BegOffset}, Length={Length}]";

		#region Equals 
		public static bool operator ==(SimpleSegment lhs, SimpleSegment rhs)
		{
			return lhs.Equals(rhs);
		}
		public static bool operator !=(SimpleSegment lhs, SimpleSegment rhs)
		{
			return !(lhs == rhs);
		}
		public bool Equals(SimpleSegment rhs)
		{
			return BegOffset == rhs.BegOffset && Length == rhs.Length;
		}
		public override bool Equals(object? obj)
		{
			return obj is SimpleSegment segment && Equals(segment);
		}
		public override int GetHashCode()
		{
			unchecked { return BegOffset + 10301 * Length; }
		}
		#endregion
	}
}
