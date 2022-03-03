using System;
using Rylogic.Common;

namespace Rylogic.Gui.WPF.TextEditor
{
	public sealed class TextAnchor
	{
		// Notes:
		//  - TextAnchor references an offset (a position between two characters). It automatically updates 
		//    when text is inserted/removed in the document.
		//  - TextAnchor only adds a weak reference to document.Change so unsubscribing isn't needed and
		//    anchors wont keep a Document from being collected.
		//  - To track a segment, use AnchorSegment which implements ISegment using two text anchors.
		//  - Use TextDocument.CreateAnchor to create an anchor from an offset.
		//  - Anchor movement is ambiguous if text is inserted exactly at the anchor's location. Does the anchor
		//    stay before the inserted text, or does it move after it? The property MovementType is used to determine
		//    which of these options the anchor will choose.
		//
		// Example:
		//   auto anchor = document.CreateAnchor(offset);
		//   ChangeMyDocument();
		//   int newOffset = anchor.Offset;

		internal TextAnchor(TextDocument document, int offset, AnchorMapping.EMove move)
		{
			Offset = offset;
			Movement = move;
			document.ChangeInternal += WeakRef.MakeWeak<DocumentChangeEventArgs>(HandleDocChanged, h => document.ChangeInternal -= h);
			void HandleDocChanged(object? sender, DocumentChangeEventArgs e)
			{
				if (!(sender is TextDocument doc))
					throw new Exception("sender should be a TextDocument");

				if (e.After && !IsDeleted)
				{
					// If the anchor is before the change, ignore
					if (Offset < e.Offset)
					{ }

					// If the anchor is after the removed text, adjust by the difference in inserted - removed
					else if (Offset >= e.Offset + e.TextRemoved.Length)
					{
						Offset += e.TextInserted.Length - e.TextRemoved.Length;
					}

					// Otherwise, the anchor is within the removed text
					else
					{
						switch (Movement)
						{
							case AnchorMapping.EMove.Default:
							{
								// If the anchor is at the insertion offset, move to after the inserted text
								if (Offset == e.Offset)
									Offset += e.TextInserted.Length - e.TextRemoved.Length;

								// Otherwise, delete the anchor
								else
									Delete();

								break;
							}
							case AnchorMapping.EMove.BeforeInsertion:
							{
								// Move the anchor to the insertion offset
								Offset += e.Offset;
								break;
							}
							case AnchorMapping.EMove.AfterInsertion:
							{
								// Move the anchor to after the insertion
								Offset += e.TextInserted.Length - e.TextRemoved.Length;
								break;
							}
							default:
							{
								throw new Exception("Unknown anchor movement type");
							}
						}
					}
				}
			}
		}

		/// <summary>Gets the offset of the text anchor.</summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		public int Offset
		{
			get => !IsDeleted ? m_offset : throw new InvalidOperationException("Offset undefined. Anchor has been deleted");
			private set
			{
				m_offset = value;
			}
		}
		private int m_offset;

		/// <summary>Controls how the anchor moves.</summary>
		public AnchorMapping.EMove Movement { get; set; }

		/// <summary>True if the anchor has been deleted.</summary>
		public bool IsDeleted { get; internal set; }

		/// <summary>Occurs after the anchor was deleted.</summary>
		public event EventHandler? Deleted;

		/// <summary>Flag this anchor as deleted</summary>
		private void Delete()
		{
			if (IsDeleted) return;
			Deleted?.Invoke(this, EventArgs.Empty);
			IsDeleted = true;
		}

		/// <inheritdoc/>
		public override string ToString() => $"TextAnchor Offset={Offset}";
	}
}
