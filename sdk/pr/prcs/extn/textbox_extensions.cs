using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pr.common;
using pr.maths;
using pr.util;

namespace pr.extn
{
	public static class TextBoxExtensions
	{
		/// <summary>Returns a disposable object that preserves the current selection</summary>
		public static Scope SelectionScope(this TextBoxBase edit)
		{
			int start = 0, length = 0;
			return Scope.Create(
				() =>
				{
					start  = edit.SelectionStart;
					length = edit.SelectionLength;
				},
				() =>
				{
					edit.Select(start, length);
				});
		}

		/// <summary>Returns a disposable object that preserves the current selection</summary>
		public static Scope SelectionScope(this ComboBox edit)
		{
			int start = 0, length = 0;
			return Scope.Create(
				() =>
				{
					start  = edit.SelectionStart;
					length = edit.SelectionLength;
				},
				() =>
				{
					edit.Select(start, length);
				});
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public static void SetText(this TextBoxBase tb, string text)
		{
			var idx = tb.SelectionStart;
			tb.SelectedText = text;
			tb.SelectionStart = idx + text.Length;
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public static void SetText(this ComboBox cb, string text)
		{
			var idx = cb.SelectionStart;
			cb.SelectedText = text;
			cb.SelectionStart = idx + text.Length;
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public static void AddTextPreservingSelection(this TextBoxBase tb, string text)
		{
			var carot_at_end = tb.SelectionStart == tb.TextLength && tb.SelectionLength == 0;
			if (carot_at_end)
			{
				tb.SelectedText = text;
				tb.SelectionStart = tb.TextLength;
			}
			else
			{
				using (tb.SelectionScope())
					tb.AppendText(text);
			}
		}

		/// <summary>Add the current combo box text to the drop down list</summary>
		public static void AddTextToDropDownList(this ComboBox cb, int max_history = 10)
		{
			// Need to take a copy of the text, because Remove() will delete the text
			// if the current text is actually a selected item.
			var text = cb.Text;
			var selection = new Range(cb.SelectionStart, cb.SelectionStart + cb.SelectionLength);

			cb.Items.Remove(text);
			cb.Items.Insert(0, text);

			while (cb.Items.Count > max_history)
				cb.Items.RemoveAt(cb.Items.Count - 1);

			cb.SelectedIndex = 0;
			cb.Select(selection.Begini, selection.Sizei);
		}
	}
}
