using System;
using System.Diagnostics;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary></summary>
	[DebuggerDisplay("{ch},nq}")]
	public struct Cell :IEquatable<Cell>
	{
		public Cell(char c)
		{
			ch = c;
			sty = 0;
		}
		public Cell(char c, ushort s)
		{
			ch = c;
			sty = s;
		}

		/// <summary>The character in the cell</summary>
		public char ch;

		/// <summary>The style bitmask</summary>
		public ushort sty;

		/// <summary></summary>
		public static implicit operator char(Cell c) => c.ch;

		#region Equals
		public static bool operator ==(Cell left, Cell right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(Cell left, Cell right)
		{
			return !(left == right);
		}
		public override bool Equals(object obj)
		{
			return obj is Cell cell && Equals(cell);
		}
		public bool Equals(Cell rhs)
		{
			return ch == rhs.ch && sty == rhs.sty;
		}
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 130108511;
				hash = hash * -1521134295 + ch.GetHashCode();
				hash = hash * -1521134295 + sty.GetHashCode();
				return hash;
			}
		}
		public override string ToString()
		{
			return new string(ch, 1);
		}
		#endregion
	}
}