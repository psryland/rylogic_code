using System;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary>Represents a selected segment.</summary>
	public class SelectionSegment :ISegment
	{
		/// <summary>Creates a SelectionSegment from two offsets.</summary>
		public SelectionSegment(int beg_offset, int end_offset)
		{
			BegOffset = Math.Min(beg_offset, end_offset);
			EndOffset = Math.Max(beg_offset, end_offset);
			BegVisualColumn = this.EndVisualColumn = -1;
		}

		/// <summary>Creates a SelectionSegment from two offsets and visual columns.</summary>
		public SelectionSegment(int beg_offset, int beg_visual_column, int end_offset, int end_visual_column)
		{
			if (beg_offset < end_offset || (beg_offset == end_offset && beg_visual_column <= end_visual_column))
			{
				BegOffset = beg_offset;
				EndOffset = end_offset;
				BegVisualColumn = beg_visual_column;
				EndVisualColumn = end_visual_column;
			}
			else
			{
				BegOffset = end_offset;
				EndOffset = beg_offset;
				BegVisualColumn = end_visual_column;
				EndVisualColumn = beg_visual_column;
			}
		}

		/// <inheritdoc/>
		public int Length => EndOffset - BegOffset;

		/// <inheritdoc/>
		public int BegOffset { get; }

		/// <inheritdoc/>
		public int EndOffset { get; }

		/// <summary>Gets the beg visual column.</summary>
		public int BegVisualColumn { get; }

		/// <summary>Gets the end visual column.</summary>
		public int EndVisualColumn { get; }

		/// <inheritdoc/>
		public override string ToString() => $"[SelectionSegment BegOffset={BegOffset}, EndOffset={EndOffset}, BegVC={BegVisualColumn}, EndVC={EndVisualColumn}]";
	}

}
