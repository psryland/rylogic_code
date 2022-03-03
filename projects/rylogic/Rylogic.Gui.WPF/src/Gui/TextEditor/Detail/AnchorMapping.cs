namespace Rylogic.Gui.WPF.TextEditor
{
	public class AnchorMapping
	{
		public enum EType
		{
			/// <summary>
			/// Normal replace.
			/// Anchors in front of the replaced region will stay in front, anchors after the replaced region will stay after.
			/// Anchors in the middle of the removed region will be deleted. If they survive deletion, they move depending on their AnchorMovementType.
			/// </summary>
			/// <remarks>
			/// This is the default implementation of DocumentChangeEventArgs when OffsetChangeMap is null,
			/// so using this option usually works without creating an OffsetChangeMap instance.
			/// This is equivalent to an OffsetChangeMap with a single entry describing the replace operation.
			/// </remarks>
			Normal,

			/// <summary>
			/// First the old text is removed, then the new text is inserted.
			/// Anchors immediately in front (or after) the replaced region may move to the other side of the insertion,
			/// depending on the AnchorMovementType.
			/// </summary>
			/// <remarks>
			/// This is implemented as an OffsetChangeMap with two entries: the removal, and the insertion.
			/// </remarks>
			RemoveAndInsert,

			/// <summary>
			/// The text is replaced character-by-character.
			/// Anchors keep their position inside the replaced text.
			/// Anchors after the replaced region will move accordingly if the replacement text has a different length than the replaced text.
			/// If the new text is shorter than the old text, anchors inside the old text that would end up behind the replacement text
			/// will be moved so that they point to the end of the replacement text.
			/// </summary>
			/// <remarks>
			/// On the OffsetChangeMap level, growing text is implemented by replacing the last character in the replaced text
			/// with itself and the additional text segment. A simple insertion of the additional text would have the undesired
			/// effect of moving anchors immediately after the replaced text into the replacement text if they used
			/// AnchorMovementStyle.BeforeInsertion.
			/// Shrinking text is implemented by removing the text segment that's too long; but in a special mode that
			/// causes anchors to always survive irrespective of their <see cref="TextAnchor.SurviveDeletion"/> setting.
			/// If the text keeps its old size, this is implemented as OffsetChangeMap.Empty.
			/// </remarks>
			CharacterReplace,

			/// <summary>
			/// Like 'Normal', but anchors with <see cref="TextAnchor.Movement"/> = Default will stay in front of the
			/// insertion instead of being moved behind it.
			/// </summary>
			KeepAnchorBeforeInsertion,
		}

		/// <summary>Defines how a text anchor moves.</summary>
		public enum EMove
		{
			/// <summary>
			/// When text is inserted at the anchor position, the type of the insertion
			/// determines where the caret moves to. For normal insertions, the anchor will stay
			/// behind the inserted text.</summary>
			Default,

			/// <summary>When text is inserted at the anchor position, the anchor will stay before the inserted text.</summary>
			BeforeInsertion,

			/// <summary>When text is insered at the anchor position, the anchor will move after the inserted text.</summary>
			AfterInsertion,
		}
	}
}
