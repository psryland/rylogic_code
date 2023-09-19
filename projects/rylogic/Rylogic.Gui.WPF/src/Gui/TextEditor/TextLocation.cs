using System;
using System.Diagnostics;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary>A line/column position.</summary>
	[DebuggerDisplay("{Description,nq}")]
	public struct TextLocation :IComparable<TextLocation>, IEquatable<TextLocation>
	{
		/// Notes:
		///  - The document provides the methods <see cref="TextDocument.Location"/> and
		///    <see cref="TextDocument.Offset(TextLocation)"/> to convert between offsets
		///    and TextLocations.

		/// <summary>Represents no text location (0, 0).</summary>
		public static readonly TextLocation Invalid = new(-1, -1);

		/// <summary>Creates a TextLocation instance.</summary>
		public TextLocation(int line, int column, int? visual_column = null)
		{
			LineIndex = line;
			Column = column;
			VisualColumn = visual_column;
		}

		/// <summary>The line index (0-based).</summary>
		public int LineIndex;

		/// <summary>The column index (0-based). -1 it unknown or unspecified</summary>
		public int Column;

		/// <summary>The visual column index. Null if unknown.</summary>
		public int? VisualColumn;

		/// <summary>Gets a string representation for debugging purposes.</summary>
		public string Description => $"Line={LineIndex}, Col={Column} (vis={VisualColumn})";

		#region Equals / Comparable

		public bool Equals(TextLocation rhs) => this == rhs;
		public override bool Equals(object? obj) => obj is TextLocation tl && tl == this;
		public override int GetHashCode() => unchecked(87 * Column.GetHashCode() ^ LineIndex.GetHashCode());
		public static bool operator ==(TextLocation lhs, TextLocation rhs) => lhs.Column == rhs.Column && lhs.LineIndex == rhs.LineIndex;
		public static bool operator !=(TextLocation lhs, TextLocation rhs) => lhs.Column != rhs.Column || lhs.LineIndex != rhs.LineIndex;
		public static bool operator <=(TextLocation lhs, TextLocation rhs)
		{
			return !(lhs > rhs);
		}
		public static bool operator >=(TextLocation lhs, TextLocation rhs)
		{
			return !(lhs < rhs);
		}
		public static bool operator <(TextLocation lhs, TextLocation rhs)
		{
			return
				lhs.LineIndex != rhs.LineIndex ? lhs.LineIndex < rhs.LineIndex :
				lhs.Column != rhs.Column ? lhs.Column < rhs.Column :
				false;
		}
		public static bool operator >(TextLocation lhs, TextLocation rhs)
		{
			return
				lhs.LineIndex != rhs.LineIndex ? lhs.LineIndex > rhs.LineIndex :
				lhs.Column != rhs.Column ? lhs.Column > rhs.Column :
				false;
		}
		public int CompareTo(TextLocation rhs)
		{
			return this == rhs ? 0 : this < rhs ? -1 : 1;
		}

		#endregion
	}
}
