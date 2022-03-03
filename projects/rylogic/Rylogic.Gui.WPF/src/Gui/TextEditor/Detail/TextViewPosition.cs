using System;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary>Represents a text location with a visual column.</summary>
	public struct TextViewPosition :IEquatable<TextViewPosition>
	{
		// todo:
		// replace with TextLocation

		/// <summary>Creates a new TextViewPosition instance.</summary>
		public TextViewPosition(int line, int column, int? visual_column)
		{
			Line = line;
			Column = column;
			VisualColumn = visual_column;
		}

		/// <summary>Creates a new TextViewPosition instance.</summary>
		public TextViewPosition(int line, int column)
			: this(line, column, null)
		{
		}

		/// <summary>Creates a new TextViewPosition instance.</summary>
		public TextViewPosition(TextLocation location, int? visual_column)
		{
			Line = location.LineIndex;
			Column = location.Column;
			VisualColumn = visual_column;
		}

		/// <summary>Creates a new TextViewPosition instance.</summary>
		public TextViewPosition(TextLocation location)
			: this(location, null)
		{
		}

		/// <summary>Gets/Sets Location.</summary>
		public TextLocation Location
		{
			get => new TextLocation(Line, Column);
			set
			{
				Line = value.LineIndex;
				Column = value.Column;
			}
		}

		/// <summary>Gets/Sets the line index (0-based).</summary>
		public int Line { get; set; }

		/// <summary>Gets/Sets the (text) column index (0-based).</summary>
		public int Column { get; set; }

		/// <summary>Gets/Sets the visual column index. Null if unknown.</summary>
		public int? VisualColumn { get; set; }

		/// <inheritdoc/>
		public override string ToString() => $"[TextViewPosition Line={Line} Column={Column} VisualColumn={VisualColumn}]";

		#region Equals

		/// <inheritdoc/>
		public override bool Equals(object? obj) => obj is TextViewPosition position && Equals(position);
		public bool Equals(TextViewPosition rhs) => Line == rhs.Line && Column == rhs.Column && VisualColumn == rhs.VisualColumn;
		public static bool operator ==(TextViewPosition left, TextViewPosition right) => left.Equals(right);
		public static bool operator !=(TextViewPosition left, TextViewPosition right) => !left.Equals(right);

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked
			{
				hashCode += 1000000007 * Line.GetHashCode();
				hashCode += 1000000009 * Column.GetHashCode();
				hashCode += 1000000021 * VisualColumn.GetHashCode();
			}
			return hashCode;
		}

		#endregion
	}
}
