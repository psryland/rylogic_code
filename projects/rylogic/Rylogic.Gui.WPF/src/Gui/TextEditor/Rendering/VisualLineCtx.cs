namespace Rylogic.Gui.WPF.TextEditor
{
	public sealed class VisualLineCtx
	{
		// Notes:
		//  - This class provides access to a VisualLine object,
		//    with a specific document line as the context.

		public VisualLineCtx(VisualLine visual_line, Line doc_line)
		{
			VisualLine = visual_line;
			DocumentLine = doc_line;
		}

		/// <summary>The visual line</summary>
		public VisualLine VisualLine { get; }
		
		/// <summary>The line from the TextDocument</summary>
		public Line DocumentLine { get; }

		/// <summary>Invalidates the graphics for this visual line</summary>
		public void Invalidate()
		{
			// If the visual line represents more than one document line
			// it may not be correct to invalidate the visual line unless
			// the document line 'IsFirstLine'.
			VisualLine.Invalidate();
		}

		/// <summary>The visual height (in DIP) of this line</summary>
		public double LineHeight => IsFirstLine ? VisualLine.LineHeight : 0.0;

		/// <summary>True if this is the first line represented in the VisualLine</summary>
		private bool IsFirstLine => DocumentLine == VisualLine.FirstLine;
	}
}

