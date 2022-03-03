using System;
using System.Windows.Media.TextFormatting;

namespace Rylogic.Gui.WPF.TextEditor
{
	internal sealed class SimpleTextSource :TextSource
	{
		public SimpleTextSource(string text, TextRunProperties properties)
		{
			Text = text;
			Properties = properties;
		}

		/// <summary></summary>
		private string Text { get; }

		/// <summary></summary>
		private TextRunProperties Properties { get; }

		/// <inheritdoc/>
		public override TextRun GetTextRun(int char_index)
		{
			if (char_index < Text.Length)
				return new TextCharacters(Text, char_index, Text.Length - char_index, Properties);
			else
				return new TextEndOfParagraph(1);
		}

		/// <inheritdoc/>
		public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int char_index) => throw new NotImplementedException();

		/// <inheritdoc/>
		public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int char_index) => throw new NotImplementedException();
	}
}

