//***************************************************
// Undo/Redo functionally
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace pr.undo
{
	// Abandoned...
	//  It should be possible to write a differencing multiple undo history

	///// <summary>A simple undo history for text changes. For use with RichTextBox/TextBox</summary>
	//public class TextBoxUndo
	//{
	//	private enum ChangeType { Text, Select, Scroll, }
	//	private class Change
	//	{
	//		public ChangeType ChangeType { get; set; }

	//	}
	//	private class Info
	//	{
	//		/// <summary>Where the control is scrolled to</summary>
	//		public int ScrollPosition { get; private set; }

	//		/// <summary>Selection location</summary>
	//		public int SelectionStart { get; private set; }
	//		public int SelectionLength { get; private set; }

	//		public Info() {}
	//	}
	//	private readonly Stack<string> m_history;
	//	private readonly StringBuilder m_sb;
	//	private int m_max_length;
	//	private int m_current;
		
	//	public TextBoxUndo()
	//	{
	//		m_history = new Stack<string>(m_max_length);
	//		m_sb = new StringBuilder();
	//		m_max_length = 100;
	//		m_current = -1;
	//	}

	//	/// <summary>Attach this undo history to a text box control</summary>
	//	public void Attach(RichTextBox tb)
	//	{
	//		tb.KeyDown += OnKeyDown;
	//		tb.TextChanged += OnTextChanged;
	//		tb.SelectionChanged += OnSelectionChanged;

	//		// Record the start point
	//		m_history.Clear();
	//		m_history.Push(new Info(tb.SelectionStart, tb.SelectionLength, tb.Text));
	//	}

	//	/// <summary>Detact this undo history from a text box control</summary>
	//	public void Detach(TextBoxBase tb)
	//	{
	//		tb.KeyDown -= OnKeyDown;
	//		tb.TextChanged -= OnTextChanged;
	//	}

	//	/// <summary>The maximum number of entries in the undo buffer</summary>
	//	public int MaxLength
	//	{
	//		get { return m_max_length; }
	//		set { m_max_length = value; LimitHistory(m_max_length); }
	//	}

	//	private void Push(int selection_start, int selection_length)
	//	{
	//		var last = 
	//	}


	//	/// <summary>Watch for text changes and record an undo history</summary>
	//	private void OnTextChanged(object sender, EventArgs args)
	//	{
	//		var tb = (TextBoxBase)sender;
	//		Push(tb.SelectionStart, tb.SelectionLength, tb.Text);
	//	}

	//	private void OnSelectionChanged(object sender, EventArgs args)
	//	{
	//		var tb = (RichTextBox)sender;
	//		Push(tb.SelectionStart, tb.SelectionLength);
	//	}

	//	/// <summary>Ensure the history is no longer than 'length'</summary>
	//	public void LimitHistory(int length)
	//	{
	//		while (m_history.Count > length)
	//			m_history.Pop();
	//	}

	//	/// <summary>Handle Ctrl-Z, Ctrl-Y</summary>
	//	private void OnKeyDown(object sender, KeyEventArgs args)
	//	{
	//		args.Handled = false;
	//		if (args.Modifiers != Keys.Control) return;
	//		var tb = (RichTextBox)sender;

	//		if (args.KeyCode == Keys.Z)
	//		{
	//		}
	//		if (args.KeyCode == Keys.Y)
	//		{
	//		}
	//	}

	//	/// <summary>Add the current difference to the history</summary>
	//	private void AddToHistory()
	//	{
	//	}

	//}
}
