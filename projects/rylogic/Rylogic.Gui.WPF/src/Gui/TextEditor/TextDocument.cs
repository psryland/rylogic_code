using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class TextDocument :INotifyPropertyChanged, RedBlack_.IAccessors<Line>
	{
		// Notes:
		//  - 'TextDocument' contains all line/text management (think fancy string builder)
		//  - Contains a Red/Black tree of Lines
		//  - The tree is never empty, there is always at least an empty line
		//  - 'SubTreeTextLength' is always 'LineEnd.Length' more that the total
		//    document length because it assumes the last line ends with a new line.
		//  - TextDocument does not know about TextView or TextArea and does not contain
		//    any width/height information because multiple TextView/TextArea controls
		//    may reference the same document.
		//  - The 'Line.UserData' allows TextView/TextArea at attach data to each line.

		public TextDocument()
			:this(new OptionsData())
		{ }
		public TextDocument(OptionsData options)
		{
			Root = new Line();
			Options = options;
			TextStyles = new TextStyleMap();
			LineEnd = DefaultLineEnd;
		}

		/// <summary>The root node in the Line tree</summary>
		internal Line Root;

		/// <summary>Text editor options</summary>
		public OptionsData Options { get; }

		/// <summary>The text styles</summary>
		public TextStyleMap TextStyles { get; }

		/// <summary>Get/Set the text and styles for the whole document</summary>
		public CellString Text
		{
			get
			{
				var cells = new CellString(TextLength);
				foreach (var line in Lines)
				{
					if (cells.Length != 0) cells.Append(LineEnd);
					cells.Append(line);
				}
				return cells;
			}
			set
			{
				var lines = value.Split<CellString, Cell>((s, i) => Line_.IsNewLine(s, i, LineEnd) ? LineEnd.Length : -1);
				var list = lines.Select(x => new Line(x.List, x.Start, x.Length)).ToList();
				Root = BuildTree(list);
			}
		}

		/// <summary>Gets the text for a portion of the document.</summary>
		public CellString GetText(ISegment segment) => GetText(segment.BegOffset, segment.Length);
		public CellString GetText(int offset, int length)
		{
			VerifyAccess();
			var cells = new CellString(length);

			// Get the line the contains 'offset'
			var line = LineByOffset(offset);
			var i = offset - line.BegOffset;
			for (; line != null; line = line.NextLine, i = 0)
			{
				// Insert newline delimiters between lines
				if (cells.Length != 0)
				{
					var len = Math.Min(length, LineEnd.Length);
					cells.Append(LineEnd, 0, len);
					length -= len;
				}

				// Insert text from 'line'
				{
					var len = Math.Min(length, line.Length);
					cells.Append(line, i, len);
					length -= len;
				}
			}

			return cells;
		}

		/// <summary>The total length of the document</summary>
		public int TextLength => Root.SubTreeTextLength - LineEnd.Length; // see notes

		/// <summary>The number of lines in this collection</summary>
		public int LineCount => Root.SubTreeLineCount;

		/// <summary>Implicit new line character(s) for the end of each line</summary>
		public string LineEnd
		{
			get;
			set
			{
				if (field == value)
					return;

				// Record an undo action for changing the line end
				UndoStack.Push(new LineEndChange(this, field, value));
				
				// If the length of the line ending changes, the text lengths
				// in the tree will need updating.
				var length_changed = field.Length != value.Length;

				// Set the new line ending
				field = value;

				// Refresh the text lengths if the length of 'LineEnd' has changed
				if (length_changed)
					UpdateTree();

				// Notify LineEnd changed.
				NotifyPropertyChanged(nameof(LineEnd));
			}
		} = DefaultLineEnd;

		/// <summary>Iterate through the lines of the document in order from first to last</summary>
		public IEnumerable<Line> Lines
		{
			get
			{
				for (var line = Root.LeftMost; line != null; line = line.NextLine)
					yield return line;
			}
		}

		/// <summary>Set the maximum length of the undo history. Use 0 to disable</summary>
		public int UndoHistoryLength
		{
			get => UndoStack.HistoryLength;
			set => UndoStack.HistoryLength = value;
		}

		/// <summary>Return the index of 'line'</summary>
		public static int IndexByLine(Line line)
		{
			//Document.VerifyAccess();
			var index = line.m_lhs?.SubTreeLineCount ?? 0;
			for (; line.m_parent != null; line = line.m_parent)
			{
				// If 'line' is the parent's right child, increase 'index' by the number
				// of nodes in the parent's left child, plus one for the parent itself.
				if (line == line.m_parent.m_rhs)
					index += 1 + line.m_parent.m_lhs?.SubTreeLineCount ?? 0;
			}
			return index;
		}

		/// <summary>Return the offset at the start of 'line'</summary>
		public static int OffsetByLine(Line line)
		{
			//Document.VerifyAccess();
			var offset = line.m_lhs?.SubTreeTextLength ?? 0;
			for (; line.m_parent != null; line = line.m_parent)
			{
				// If 'line' is the parent's right child, increase 'offset' by the length
				// of all nodes in the parent's left child, plus the length of the parent itself
				if (line == line.m_parent.m_rhs)
					offset += line.m_parent.SubTreeTextLength - line.m_parent.m_rhs?.SubTreeTextLength ?? 0;
			}
			return offset;
		}

		/// <summary>Gets a document line by character offset.</summary>
		public Line LineByOffset(int offset) => LineByOffset(Root, offset);
		public static Line LineByOffset(Line root, int offset)
		{
			Debug.Assert(offset >= 0 && offset <= root.SubTreeTextLength);
			if (offset == root.SubTreeTextLength)
				return root.RightMost;

			for (var line = root; ;)
			{
				var lhs_length = line.m_lhs?.SubTreeTextLength ?? 0;
				if (offset < lhs_length)
				{
					line = line.m_lhs ?? throw new Exception("Line tree is corrupt");
				}
				else if (offset > lhs_length + line.Length)
				{
					line = line.m_rhs ?? throw new Exception("Line tree is corrupt");
					offset -= lhs_length + line.Length;
				}
				else
				{
					return line;
				}
			}
		}

		/// <summary>Get the line at the given index (0-based)</summary>
		public Line LineByIndex(int index) => LineByIndex(Root, index);
		public static Line LineByIndex(Line root, int index)
		{
			//Document.VerifyAccess();
			Debug.Assert(index >= 0 && index < root.SubTreeLineCount);
			for (var line = root; ;)
			{
				var lhs_count = line.m_lhs?.SubTreeLineCount ?? 0;
				if (index < lhs_count)
				{
					line = line.m_lhs ?? throw new Exception("Line tree is corrupt");
				}
				else if (index > lhs_count)
				{
					line = line.m_rhs ?? throw new Exception("Line tree is corrupt");
					index -= lhs_count + 1;
				}
				else
				{
					return line;
				}
			}
		}

		/// <summary>Gets the offset from a text location.</summary>
		public int Offset(TextLocation location)
		{
			return Offset(location.LineIndex, location.Column);
		}
		public int Offset(int line, int column)
		{
			var doc_line = LineByIndex(line);
			return 
				column <= 0 ? doc_line.BegOffset :
				column > doc_line.Length ? doc_line.EndOffset :
				doc_line.BegOffset + column;
		}

		/// <summary>Gets the location from an offset.</summary>
		public TextLocation Location(int offset)
		{
			var line = LineByOffset(offset);
			return new TextLocation(line.LineIndex, offset - line.BegOffset);
		}

		/// <summary>Creates a new <see cref="TextAnchor"/> at the specified offset.</summary>
		public TextAnchor CreateAnchor(int offset, AnchorMapping.EMove move = AnchorMapping.EMove.Default)
		{
			VerifyAccess();
			ThrowIfRangeInvalid(offset, 0);
			return new TextAnchor(this, offset, move);
		}

		#region Begin/End Update, Insert, Remove, Replace

		// Notes:
		//  - The order of events:
		//    1) UpdateScope() Created
		//       - Start a change batch on the undo stack
		//       - UpdateStarted event raised
		//    2) Insert(), Remove(), or Replace() called
		//       - Change event raised, Before = true
		//       - The document is changed
		//       - TextAnchor.Deleted event raised if any anchors were in the deleted text portion.
		//       - Change event raised, After = true
		//    3) UpdateScope() disposed
		//       - TextChanged event raised
		//       - PropertyChanged event raised for the Text, TextLength, LineCount properties (in that order)
		//       - End of change batch on the undo stack
		//       - UpdateFinished event raised
		//
		//  - If the insert/remove/replace methods are called without a call to UpdateScope() they will call UpdateScope()
		//    to ensure no change happens outside of UpdateStarted/UpdateFinished.
		//  - There can be multiple document changes within an UpdateScope(). In this case, the events are only raised once
		//    for the outer most scope.

		/// <summary>Occurs before and after the document changes.</summary>
		public event EventHandler<DocumentChangeEventArgs>? Change;

		/// <summary>Occurs after Change(Before==true) and before Change(After==true).</summary>
		internal event EventHandler<DocumentChangeEventArgs>? ChangeInternal;

		/// <summary>Occurs when a document change starts.</summary>
		public event EventHandler? UpdateStarted;

		/// <summary>Occurs when a document change is finished.</summary>
		public event EventHandler? UpdateFinished;

		/// <summary>True if an update is running.</summary>
		public bool IsInUpdate
		{
			get
			{
				VerifyAccess();
				return m_run_update != 0;
			}
		}

		/// <summary>Creates a scope in which updates are made</summary>
		public IDisposable RunUpdate() => UpdateScope();
		public IDisposable UpdateScope()
		{
			VerifyAccess();

			// If updates are blocked, throw
			if (m_block_updates != 0)
				throw new InvalidOperationException("Cannot change document within another document change.");

			// Allow nested update calls (if not blocked)
			return Scope.Create(() =>
			{
				if (++m_run_update == 1)
				{
					UpdateStarted?.Invoke(this, EventArgs.Empty);
				}
				return UndoStack.Group();
			},
			g =>
			{
				// Push the undo/redo group
				g.Dispose();

				if (--m_run_update == 0)
				{
					UpdateFinished?.Invoke(this, EventArgs.Empty);
					//FireChangeEvents();
				}
			});
		}
		private int m_run_update;

		/// <summary>Disallow updates for a scoped period</summary>
		public IDisposable BlockUpdates()
		{
			return Scope.Create(
				() => ++m_block_updates,
				() => --m_block_updates);
		}
		private int m_block_updates;

		/// <summary>Validate a text range</summary>
		private void ThrowIfRangeInvalid(int offset, int length)
		{
			if (offset < 0 || offset > TextLength)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"offset must be within [0, {TextLength}]");

			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, $"length must be >= 0");

			if (offset + length > TextLength)
				throw new ArgumentOutOfRangeException(nameof(length), length, $"offset+length must be with [0, {TextLength}]");
		}

		/// <summary>Inserts text.</summary>
		public void Insert(int offset, CellString text) => Replace(offset, 0, text);

		/// <summary>Removes text.</summary>
		public void Remove(ISegment segment) => Replace(segment, CellString.Empty);
		public void Remove(int offset, int length) => Replace(offset, length, CellString.Empty);

		/// <summary>Replaces text.</summary>
		public void Replace(ISegment segment, CellString text) => Replace(segment.BegOffset, segment.Length, text, null);
		public void Replace(int offset, int length, CellString text) => Replace(offset, length, text, null);

		/// <summary>Replaces text.</summary>
		public void Replace(int offset, int length, CellString text, AnchorMapping? offset_change_map)
		{
			// Notes:
			//  The 'offset_change_map' determines how offsets inside the old text are mapped to the new text.
			//  This affects how the anchors and segments inside the replaced region behave.
			//  If you pass null (the default when using one of the other overloads), the offsets are changed as
			//  in OffsetChangeMappingType.Normal mode.
			//  If you pass OffsetChangeMap.Empty, then everything will stay in its old place (OffsetChangeMappingType.CharacterReplace mode).
			//  The offsetChangeMap must be a valid 'explanation' for the document change. See <see cref="OffsetChangeMap.IsValidForDocumentChange"/>.
			//  Passing an OffsetChangeMap to the Replace method will automatically freeze it to ensure the thread safety of the resulting
			//  DocumentChangeEventArgs instance.

			// Ensure that all changes take place inside an update group.
			// Will also take care of throwing an exception if inDocumentChanging is set.
			using var update = UpdateScope();

			// Protect document change against corruption by other changes inside the event handlers
			using var block = BlockUpdates();

			// The range verification must wait until after the UpdateScope() call because
			// the document might be modified inside the UpdateStarted event.
			ThrowIfRangeInvalid(offset, length);

			// Empty strings still raise events
			if (length == 0 && text.Length == 0)
				return;

			// Get the text to be removed
			var removed = GetText(offset, length);

			// Create a record of the document change
			var chg = new DocumentChangeEventArgs(true, this, offset, text_inserted:text, text_removed:removed);
			Change?.Invoke(this, chg);
			ChangeInternal?.Invoke(this, chg);

			// Push this change onto the undo stack. If the history length is 0, this will be a nop
			UndoStack.Push(chg);

			// Apply the changes
			ApplyDocumentChange(chg, undo:false);
			
			// Notify change complete
			chg.Before = false;
			ChangeInternal?.Invoke(this, chg);
			Change?.Invoke(this, chg);
		}

		/// <summary>Apply (or unapply) the changes described by 'chg'</summary>
		internal void ApplyDocumentChange(DocumentChangeEventArgs chg, bool undo)
		{
			// Notes:
			//  - This is called on first applying a change, and during undo/redo.
			//    It is assumed undo actions such has recording the text that is about to be deleted,
			//    the locations of anchors, etc has already been done.

			var text_removed = undo ? chg.TextInserted : chg.TextRemoved;
			var text_inserted = undo ? chg.TextRemoved : chg.TextInserted;

			// Remove the 'removed' text
			{
				// The text being removed should match 'chg.TextRemoved'
				var line = LineByOffset(chg.Offset);
				var i = chg.Offset - line.BegOffset;
				var remaining = text_removed.Length;

				// Delete lines
				for (; remaining != 0; i = 0)
				{
					var len = Math.Min(remaining, line.Length - i);
					if (i != 0 || len == remaining)
					{
						// Partial line delete
						line.Remove(i, len);
						remaining -= len;
					}
					else
					{
						// Whole line delete
						Root = RedBlack_.Delete(Root, line, this) ?? new Line();
						remaining -= len + LineEnd.Length;
					}
				}
			}

			// Insert the 'inserted' text
			{
				var line = LineByOffset(chg.Offset);
				var i = chg.Offset - line.BegOffset;
				var remaining = text_inserted.Length;

				// Insert lines
				for (; remaining != 0; i = 0)
				{
					var idx = text_inserted.Length - remaining;
					var len = Line_.FindNewLine(text_inserted, idx, LineEnd) - idx;
					if (i != 0 || len == remaining)
					{
						// Partial line insert (i.e. no new line at the end)
						line.Insert(i, text_inserted, idx, len);
						remaining -= len;
					}
					else
					{
						// Whole line insert
						Root = RedBlack_.Insert(Root, new Line(text_inserted, idx, len), this);
						remaining -= len + LineEnd.Length;
					}
				}
			}

			// Add/Move/Remove text anchors
			{
			}
		}

		#endregion

		#region Red/Black Tree

		/// <summary>Build a balanced tree from a list of lines</summary>
		private Line BuildTree(IList<Line> lines)
		{
			var root = BuildTree(lines, 0, lines.Count, TreeHeight(lines.Count)) ?? new Line();
			root.m_black = true;
			return root;

			// Recursive BuildTree
			Line? BuildTree(IList<Line> lines, int start, int end, int height)
			{
				Debug.Assert(start <= end);
				if (start == end)
					return null;

				var middle = (start + end) / 2;
				var line = lines[middle];

				line.m_lhs = BuildTree(lines, start, middle, height - 1);
				line.m_rhs = BuildTree(lines, middle + 1, end, height - 1);
				if (line.m_lhs != null) line.m_lhs.m_parent = line;
				if (line.m_rhs != null) line.m_rhs.m_parent = line;
				if (height == 1) line.m_black = false;

				UpdateLine(line, recursive: false);
				return line;
			}
			static int TreeHeight(int size) => size != 0 ? TreeHeight(size / 2) + 1 : 0;
		}

		/// <summary>Update the line count and total text length in the tree starting at 'line'</summary>
		internal void UpdateLine(Line line, bool recursive)
		{
			// The number of lines in the subtree with 'line' as the root
			var count = 1;

			// The total text length of the subtree with 'line' as the root
			var length = line.Length + LineEnd.Length;

			// Add contribution from left and right sub trees
			if (line.m_lhs != null)
			{
				count += line.m_lhs.SubTreeLineCount;
				length += line.m_lhs.SubTreeTextLength;
			}
			if (line.m_rhs != null)
			{
				count += line.m_rhs.SubTreeLineCount;
				length += line.m_rhs.SubTreeTextLength;
			}

			// Counts unchanged, quick out
			if (count == line.SubTreeLineCount &&
				length == line.SubTreeTextLength)
				return;

			// Update 'line's counts
			line.SubTreeLineCount = count;
			line.SubTreeTextLength = length;

			// Update the parent counts
			if (line.m_parent != null && recursive)
				UpdateLine(line.m_parent, recursive);
		}

		/// <summary>Update the line count and total text length for all lines in the tree</summary>
		internal void UpdateTree()
		{
			foreach (var line in RedBlack_.EnumerateDF(Root, +1, this))
				UpdateLine(line, recursive: false);
		}

		/// <inheritdoc/>
		int RedBlack_.IAccessors<Line>.Compare(Line lhs, Line rhs)
		{
			return lhs.SubTreeLineCount.CompareTo(rhs.SubTreeLineCount);
		}

		/// <inheritdoc/>
		RedBlack_.EColour RedBlack_.IAccessors<Line>.Colour(Line? elem)
		{
			return elem?.m_black == false ? RedBlack_.EColour.Red : RedBlack_.EColour.Black;
		}

		/// <inheritdoc/>
		void RedBlack_.IAccessors<Line>.Colour(Line elem, RedBlack_.EColour colour)
		{
			elem.m_black = colour == RedBlack_.EColour.Black;
		}

		/// <inheritdoc/>
		Line RedBlack_.IAccessors<Line>.Child(int side, Line? elem)
		{
			if (elem == null) return null!;
			return side < 0 ? elem.m_lhs! : elem.m_rhs!;
		}

		/// <inheritdoc/>
		void RedBlack_.IAccessors<Line>.Child(int side, Line? elem, Line? child)
		{
			if (elem == null)
			{
				//Root = child ?? new Line();
			}
			if (elem != null)
			{
				if (side < 0)
				{
					elem.m_lhs = child;
				}
				else
				{
					elem.m_rhs = child;
				}
			}
			if (child != null)
			{
				child.m_parent = elem;
				UpdateLine(child, recursive: true);
			}
		}

		#endregion

		#region Thread Ownership

		/// <summary>The thread that is allowed to interact with this instance</summary>
		private Thread m_owner_thread = Thread.CurrentThread;
		private readonly object m_lock = new();

		/// <summary>Assert that the current thread is the owner</summary>
		public bool VerifyAccess()
		{
			if (Thread.CurrentThread == m_owner_thread) return true;
			throw new InvalidOperationException("TextDocument can be accessed only from the thread that owns it.");
		}

		/// <summary>Transfer ownership to a different thread</summary>
		public void SetOwnerThread(Thread owner)
		{
			// This is handy for loading on a background thread then passing to the UI thread.
			lock (m_lock)
			{
				if (m_owner_thread != null) VerifyAccess();
				m_owner_thread = owner;
			}
		}

		#endregion

		#region Undo / Redo

		/// <summary>Get/Set the undo stack.</summary>
		public UndoStack UndoStack
		{
			get;
			set
			{
				// Notes:
				//  - Setting the undo stack allows multiple TextDocuments to share a common undo stack.
				//  - The outgoing undo stack is cleared to drop references to this TextDocument
				if (field == value) return;
				VerifyAccess();
				field.Clear();
				field = value;
				NotifyPropertyChanged(nameof(UndoStack));
			}
		} = new();

		#endregion

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

		/// <summary>Default value for LineEnd</summary>
		public const string DefaultLineEnd = "\n";
		private class LineEndChange :IUndoRedo
		{
			private readonly TextDocument m_doc;
			private readonly string m_old;
			private readonly string m_nue;
			public LineEndChange(TextDocument doc, string old, string nue)
			{
				m_doc = doc;
				m_old = old;
				m_nue = nue;
			}

			void IUndoRedo.Redo(object context) => m_doc.LineEnd = m_nue;
			void IUndoRedo.Undo(object context) => m_doc.LineEnd = m_old;
		}
	}
}
///// <summary>Enumerate each cell in the document</summary>
//public IEnumerable<Cell> Cells
//{
//	get
//	{
//		for (var line = Root.LeftMost; line != null; line = line.NextLine)
//		{
//			foreach (var cell in line)
//				yield return cell;
//			foreach (var ch in LineEnd)
//				yield return new Cell(ch, 0);
//		}
//	}
//}

/////// <summary>Enumerate each character in the document</summary>
////public IEnumerable<char> Characters => Cells.Select(x => x.ch);
///// <summary>Replaces text.</summary>
//public void Replace(int offset, int length, CellString text, AnchorMapping.EType anchor_mapping)
//{
//	// Please see OffsetChangeMappingType XML comments for details on how these modes work.
//	switch (anchor_mapping)
//	{
//		case AnchorMapping.EType.Normal:
//		{
//			Replace(offset, length, text, null);
//			break;
//		}
//		case AnchorMapping.EType.KeepAnchorBeforeInsertion:
//		{
//			Replace(offset, length, text, AnchorMapping.FromSingleElement(new OffsetChangeMapEntry(offset, length, text.Length, false, true)));
//			break;
//		}
//		case AnchorMapping.EType.RemoveAndInsert:
//		{
//			if (length == 0 || text.Length == 0)
//			{
//				// only insertion or only removal?
//				// OffsetChangeMappingType doesn't matter, just use Normal.
//				Replace(offset, length, text, null);
//			}
//			else
//			{
//				AnchorMapping map = new AnchorMapping(2);
//				map.Add(new OffsetChangeMapEntry(offset, length, 0));
//				map.Add(new OffsetChangeMapEntry(offset, 0, text.Length));
//				map.Freeze();
//				Replace(offset, length, text, map);
//			}
//			break;
//		}
//		case AnchorMapping.EType.CharacterReplace:
//		{
//			if (length == 0 || text.Length == 0)
//			{
//				// only insertion or only removal?
//				// OffsetChangeMappingType doesn't matter, just use Normal.
//				Replace(offset, length, text, null);
//			}
//			else if (text.Length > length)
//			{
//				// look at OffsetChangeMappingType.CharacterReplace XML comments on why we need to replace
//				// the last character
//				OffsetChangeMapEntry entry = new OffsetChangeMapEntry(offset + length - 1, 1, 1 + text.Length - length);
//				Replace(offset, length, text, AnchorMapping.FromSingleElement(entry));
//			}
//			else if (text.Length < length)
//			{
//				OffsetChangeMapEntry entry = new OffsetChangeMapEntry(offset + text.Length, length - text.Length, 0, true, false);
//				Replace(offset, length, text, AnchorMapping.FromSingleElement(entry));
//			}
//			else
//			{
//				Replace(offset, length, text, AnchorMapping.Empty);
//			}
//			break;
//		}
//		default:
//		{
//			throw new ArgumentOutOfRangeException("offsetChangeMappingType", anchor_mapping, "Invalid enum value");
//		}
//	}
//}