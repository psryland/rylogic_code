using Rylogic.Common;

namespace Rylogic.TextAligner
{
	/// <summary></summary>
	internal struct AlignPos
	{
		public AlignPos(int column, RangeI span)
		{
			Column = column;
			Span = span;
		}

		/// <summary>The column index to align to</summary>
		public readonly int Column;

		/// <summary>The range of characters around the align column</summary>
		public readonly RangeI Span;

		/// <summary></summary>
		public override string ToString() => $"Col {Column} {Span}";
	}
}
