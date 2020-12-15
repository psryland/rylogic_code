using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rylogic.Gui.WPF.TextEditor
{
	internal sealed class EmptySelection :Selection
	{
		public EmptySelection(TextArea text_area)
			: base(text_area)
		{}

		/// <inheritdoc/>
		public override int Length => 0;

		/// <inheritdoc/>
		public override CellString GetText() => CellString.Empty;

		/// <inheritdoc/>
		public override TextViewPosition BegPosition => new TextViewPosition(TextLocation.Invalid);

		/// <inheritdoc/>
		public override TextViewPosition EndPosition => new TextViewPosition(TextLocation.Invalid);

		/// <inheritdoc/>
		public override IEnumerable<SelectionSegment> Segments => Array.Empty<SelectionSegment>();

		/// <inheritdoc/>
		public override ISegment SurroundingSegment => Segment_.Invalid;

		/// <inheritdoc/>
		public override Selection UpdateOnDocumentChange(DocumentChangeEventArgs e) => this;

		/// <inheritdoc/>
		public override void ReplaceSelectionWithText(string new_text)
		{
			//new_text = AddSpacesIfRequired(new_text, TextArea.Caret.Position, TextArea.Caret.Position);
			//if (new_text.Length > 0)
			//{
			//	if (TextArea.ReadOnlySectionProvider.CanInsert(TextArea.Caret.Offset))
			//	{
			//		TextArea.Document.Insert(TextArea.Caret.Offset, new_text);
			//	}
			//}
			//TextArea.Caret.VisualColumn = -1;
		}

#if false
		public override Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition)
		{
			var document = TextArea.Document;
			if (document == null)
				throw ThrowUtil.NoDocumentAssigned();
			return Create(TextArea, startPosition, endPosition);
		}


		public override selection setendpoint(textviewposition endposition)
		{
			throw new notsupportedexception();
		}
#endif

		#region Equals

		public override bool Equals(object? obj)
		{
			// Use reference equality because there's only one EmptySelection per text area.
			return ReferenceEquals(this, obj);
		}
		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		#endregion
	}
}
