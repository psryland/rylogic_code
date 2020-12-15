using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.TextEditor
{
	[DebuggerDisplay("{Description,nq}")]
	public class Line :IList<Cell>, IReadOnlyList<Cell>
	{
		// Notes:
		//  - An IList<Cell> is basically a StringBuilder
		//  - 'Text' should not include new-line characters.
		//    There is an implicit new line at the end of each line.
		//  - There isn't a reference to 'TextDocument' because Line's
		//    shouldn't know about Documents, and it's wasteful of memory.
		//  - The new-line is not included in the returned text because
		//    that would require a dependency on the 'Document' for 'LineEnd'.
		//  - A document (and therefore these Lines) can be shared by multiple
		//    view controls. Don't store WPF visuals in this class.

		internal Line()
		{
			Text = new CellString();
		}
		internal Line(CellString text)
			: this()
		{
			SetText(text);
		}
		internal Line(CellString text, int start, int length)
			: this()
		{
			SetText(text, start, length);
		}

		/// <summary>The line index (0-based) of this line in the document</summary>
		public int LineIndex => TextDocument.IndexByLine(this);

		/// <summary>The beg offset of the line in the document's text.</summary>
		public int BegOffset => TextDocument.OffsetByLine(this);

		/// <summary>The end offset of the line in the document's text (excludes the newline).</summary>
		public int EndOffset => BegOffset + Length;

		/// <summary>Line length (excludes the new line)</summary>
		public int Length => Text.Length;

		/// <summary>Line lenth including the LineEnd delimiter</summary>
		public int LengthWithLineEnd => SubTreeTextLength - (m_lhs?.SubTreeTextLength ?? 0 + m_rhs?.SubTreeTextLength ?? 0);

		/// <summary>Get the text of this line</summary>
		public CellString Text { get; }

		/// <summary>Set the whole line to the given text and style</summary>
		public void SetText(CellString text) => SetText(text, 0, text.Length);
		public void SetText(CellString text, int start, int length)
		{
			Debug.Assert(Validate(text, start, length));

			Text.Length = 0;
			Text.Append(text, start, length);
			Invalidate();
		}

		/// <summary>Access to each cell</summary>
		public Cell this[int index]
		{
			get => Text[index];
			set
			{
				if (Text[index] == value) return;
				Validate(value);
				Text[index] = value;
				Invalidate();
			}
		}

		/// <inheritdoc/>
		public int IndexOf(Cell item)
		{
			return Text.IndexOf(item);
		}

		/// <summary>Reset the line to empty</summary>
		public void Clear()
		{
			Text.Length = 0;
		}

		/// <summary>Append a cell to the line</summary>
		public void Add(Cell item)
		{
			Debug.Assert(Validate(item));
			Text.Add(item);
			Invalidate();
		}

		/// <summary>Insert a cell into the line</summary>
		public void Insert(int index, Cell item)
		{
			Debug.Assert(Validate(item));
			Text.Insert(index, item);
			Invalidate();
		}

		/// <summary>Insert a string into the line</summary>
		public void Insert(int index, CellString str)
		{
			Debug.Assert(Validate(str));
			Text.Insert(index, str);
			Invalidate();
		}

		/// <summary>Insert a string into the line</summary>
		public void Insert(int index, CellString str, int start, int length)
		{
			Debug.Assert(Validate(str));
			Text.Insert(index, str, start, length);
			Invalidate();
		}

		/// <summary>Delete a cell from the line</summary>
		public bool Remove(Cell item)
		{
			Invalidate();
			return Text.Remove(item);
		}

		/// <summary>Delete a cell at the given index position</summary>
		public void RemoveAt(int index)
		{
			Text.RemoveAt(index);
			Invalidate();
		}

		/// <summary>Remove a span of cells from the line</summary>
		public void Remove(int index, int count)
		{
			Text.Remove(index, count);
			Invalidate();
		}

		/// <inheritdoc/>
		public bool Contains(Cell item)
		{
			return Text.Contains(item);
		}

		/// <inheritdoc/>
		int ICollection<Cell>.Count => Length;
		int IReadOnlyCollection<Cell>.Count => Length;

		/// <inheritdoc/>
		void ICollection<Cell>.CopyTo(Cell[] array, int arrayIndex)
		{
			((ICollection<Cell>)Text).CopyTo(array, arrayIndex);
		}

		/// <summary></summary>
		bool ICollection<Cell>.IsReadOnly => false;

		/// <inheritdoc/>
		public IEnumerator<Cell> GetEnumerator()
		{
			return Text.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>Cause the cached formatted text to be recreated</summary>
		public void Invalidate() => ++IssueNumber;

		/// <summary>Invalidation counter</summary>
		internal int IssueNumber { get; private set; }

		/// <summary>Gets the next line in the document (or null if the last line)</summary>
		public Line? NextLine
		{
			get
			{
				//DebugVerifyAccess();
				if (m_rhs != null)
					return m_rhs.LeftMost;

				Line? p = m_parent;
				for (var c = this; p != null && p.m_rhs == c; c = p, p = p.m_parent) { }
				return p;
			}
		}

		/// <summary>Gets the previous line in the document (or null if the first line)</summary>
		public Line? PrevLine
		{
			get
			{
				//DebugVerifyAccess();
				if (m_lhs != null)
					return m_lhs.RightMost;

				Line? p = m_parent;
				for (var c = this; p != null && p.m_lhs == c; c = p, p = p.m_parent) { }
				return p;
			}
		}

		/// <summary>Meta data attached to each line</summary>
		internal UserData UserData { get; } = new UserData();

		#region Red/Black Tree

		internal Line? m_parent;
		internal Line? m_lhs;
		internal Line? m_rhs;
		internal bool m_black;
		
		/// <summary>The number of nodes in the sub tree that has this line as the root</summary>
		internal int SubTreeLineCount = 1;

		/// <summary>The total length of text in the sub tree that has this line as the root (includes newlines)</summary>
		internal int SubTreeTextLength = 0;

		/// <summary>Return the line that is the root of the tree containing this line</summary>
		internal Line TreeRoot
		{
			get
			{
				var line = this;
				for (; line.m_parent != null; line = line.m_parent) { }
				return line;
			}
		}

		/// <summary>The left-most child of the subtree</summary>
		internal Line LeftMost
		{
			get
			{
				var line = this;
				for (; line.m_lhs != null; line = line.m_lhs) { }
				return line;
			}
		}

		/// <summary>The right-most child of the subtree</summary>
		internal Line RightMost
		{
			get
			{
				var line = this;
				for (; line.m_rhs != null; line = line.m_rhs) { }
				return line;
			}
		}

		#endregion

		#region Diagnostics

		/// <summary></summary>
		private bool Validate(Cell cell)
		{
			if (cell != '\n' && cell != '\r') return true;
			throw new Exception("Line text should not include new-line characters");
		}

		/// <summary>Check 'cells' is a valid collection of cells to add to the line</summary>
		private bool Validate(CellString str) => Validate(str, 0, str.Length);
		private bool Validate(CellString str, int start, int length)
		{
			for (; length-- != 0; ++start)
				Validate(str[start]);

			return true;
		}

		/// <summary>Debug description</summary>
		private string Description => $"[DocLine] \"{Text}\"";

		#endregion
	}

	/// <summary>Line helper extensions</summary>
	internal static class Line_
	{
		/// <summary>True if 'str[idx]' is the location of a new line delimiter</summary>
		internal static bool IsNewLine(CellString str, int idx, string lineend)
		{
			return string.Compare((string)str, idx, lineend, 0, lineend.Length) == 0;
		}

		/// <summary>Search from 'idx' for the next new line delimiter, returning it's index. Returns 'str.Length' if not found</summary>
		internal static int FindNewLine(CellString str, int idx, string lineend)
		{
			for (; idx != str.Length && !IsNewLine(str, idx, lineend); ++idx) { }
			return idx;
		}

	//	/// <summary>Return the span in 'str' containing the first new line after 'offset'</summary>
	//	internal static SimpleSegment NextNewLine(CellString str, int offset)
	//	{
	//		Debug.Assert(offset >= 0 && offset < str.Length);
	//
	//		int i = offset;
	//		for (; i != str.Length && str[i] != '\r' && str[i] != '\n'; ++i) {}
	//		if (i == str.Length) return new SimpleSegment(i, 0);
	//		if (i + 1 == str.Length) return new SimpleSegment(i, 1);
	//		if (str[i] == '\r' && str[i + 1] == '\n') return new SimpleSegment(i, 2);
	//		return new SimpleSegment(i, 1);
	//	}
	//
	//	/// <summary>Return the span in 'str' containing the first new line after 'offset'</summary>
	//	internal static SimpleSegment NextNewLine(string str, int offset)
	//	{
	//		Debug.Assert(offset >= 0 && offset < str.Length);
	//
	//		int i = offset;
	//		for (; i != str.Length && str[i] != '\r' && str[i] != '\n'; ++i) { }
	//		if (i == str.Length) return new SimpleSegment(i, 0);
	//		if (i+1 == str.Length) return new SimpleSegment(i, 1);
	//		if (str[i] == '\r' && str[i+1] == '\n') return new SimpleSegment(i, 2);
	//		return new SimpleSegment(i, 1);
	//	}
	}
}