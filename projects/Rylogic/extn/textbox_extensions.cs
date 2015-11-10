using System.Drawing;
using System.Windows.Forms;
using pr.common;
using pr.util;
using pr.win32;

namespace pr.extn
{
	public static class TextBoxExtensions
	{
		/// <summary>Returns a disposable object that preserves the current selection</summary>
		public static Scope<Range> SelectionScope(this TextBoxBase edit)
		{
			return Scope.Create(
				() => Range.FromStartLength(edit.SelectionStart, edit.SelectionLength),
				rn => edit.Select(rn.Begini, rn.Sizei));
		}

		/// <summary>Returns a disposable object that preserves the current selection</summary>
		public static Scope<Range> SelectionScope(this ComboBox edit)
		{
			return Scope.Create(
				() => Range.FromStartLength(edit.SelectionStart, edit.SelectionLength),
				pt => edit.Select(pt.Begini, pt.Sizei));
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

		/// <summary>Show or hide the caret for this text box. Returns true if successful. Successful Show/Hide calls must be matched</summary>
		public static bool ShowCaret(this TextBoxBase tb, bool show)
		{
			return show
				? Win32.ShowCaret(tb.Handle) != 0
				: Win32.HideCaret(tb.Handle) != 0;
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public static void AppendText(this TextBoxBase tb, string text)
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
		public static void AddTextToDropDownList(this ComboBox cb, int max_history = 10, bool only_if_has_value = true)
		{
			// Need to take a copy of the text, because Remove() will delete the text
			// if the current text is actually a selected item.
			var text = cb.Text ?? string.Empty;
			var selection = new Range(cb.SelectionStart, cb.SelectionStart + cb.SelectionLength);

			if (text.HasValue() || !only_if_has_value)
			{
				cb.Items.Remove(text);
				cb.Items.Insert(0, text);

				while (cb.Items.Count > max_history)
					cb.Items.RemoveAt(cb.Items.Count - 1);

				cb.SelectedIndex = 0;
			}

			cb.Select(selection.Begini, selection.Sizei);
		}

		/// <summary>Set the text box into a state indicating error or success.</summary>
		public static void HintState(this TextBoxBase tb, bool success
			,Color? success_col = null ,Color? error_col = null
			,ToolTip tt = null ,string success_tt = null ,string error_tt = null)
		{
			if (success)
			{
				tb.BackColor = success_col ?? Color.LightGreen;
				if (tt != null)
					tb.ToolTip(tt, success_tt ?? string.Empty);
			}
			else
			{
				tb.BackColor = error_col ?? Color.LightSalmon;
				if (tt != null)
					tb.ToolTip(tt, error_tt ?? string.Empty);
			}
		}
	}
}
