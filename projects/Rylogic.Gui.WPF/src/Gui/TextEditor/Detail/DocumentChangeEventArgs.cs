using System;
using Rylogic.Common;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class DocumentChangeEventArgs : EventArgs, IUndoRedo
	{
		// Notes:
		//  - Changes are always Remove first, then Insert.
		//    If the opposite change is needed, it will be two document changes.
		//  - DocumentChange also records text anchors that are moved or deleted.

		public DocumentChangeEventArgs(bool before, TextDocument doc, int offset, CellString? text_inserted = null, CellString? text_removed = null)
		{
			Before = before;
			Document = doc;
			Offset = offset;
			TextInserted = text_inserted ?? CellString.Empty;
			TextRemoved = text_removed ?? CellString.Empty;
		}

		/// <summary>True if the change is about to happen</summary>
		public bool Before { get; internal set; }
		public bool After => !Before;

		/// <summary>The document that this change applies to</summary>
		public TextDocument Document { get; }

		/// <summary>Where the text was inserted/removed</summary>
		public int Offset { get; }

		/// <summary>The text inserted</summary>
		public CellString TextInserted { get; }

		/// <summary>The text removed</summary>
		public CellString TextRemoved { get; }

		//public IList<OffsetChange> AnchorChanges { get; }

		/// <inheritdoc/>
		void IUndoRedo.Redo(object context) => Document.ApplyDocumentChange(this, undo:false);
		void IUndoRedo.Undo(object context) => Document.ApplyDocumentChange(this, undo:true);

		/// <summary>Gets the new anchor position as a consequence of this document change</summary>
		public int NewOffset(int offset, AnchorMapping.EMove move_type)
		{
			if (offset > Offset)
			{
				offset -= Math.Min(offset - Offset, TextRemoved.Length - Offset);
			}
			if (offset >= Offset)
			{
				offset += move_type switch
				{
					AnchorMapping.EMove.Default         => offset > Offset ? TextInserted.Length : 0,
					AnchorMapping.EMove.BeforeInsertion => offset > Offset ? TextInserted.Length : 0,
					AnchorMapping.EMove.AfterInsertion  => TextInserted.Length,
					_ => throw new Exception("Unknown anchor movement type")
				};
			}
			return offset;
		}
	}
}
