using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using pr.common;
using pr.extn;

namespace pr.gui
{
	/// <summary>A replacement for ToolStripComboBox that preserves the text selection across focus lost/gained</summary>
	public class ToolStripComboBox :System.Windows.Forms.ToolStripComboBox
	{
		/// <summary>Used to preserve the selection while the control doesn't have focus</summary>
		private Range m_selection;

		public ToolStripComboBox() :this(string.Empty) {}
		public ToolStripComboBox(string name) :base(name)
		{
			m_selection = new Range(0,0);
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public new string SelectedText
		{
			get { return base.SelectedText; }
			set
			{
				//System.Diagnostics.Trace.Write("SelectedText: ");
				RestoreSelection();
				base.SelectedText = value;
			}
		}

		/// <summary>Restore the selection on focus gained</summary>
		protected override void OnGotFocus(EventArgs e)
		{
			//System.Diagnostics.Trace.Write("OnGotFocus: ");
			RestoreSelection();
			base.OnGotFocus(e);
			// Note, don't save on lost focus, the selection has already been reset to 0,0 by then
		}

		/// <summary>Update the selection whenever the text changes</summary>
		protected override void OnTextUpdate(EventArgs e)
		{
			base.OnTextUpdate(e);
			//System.Diagnostics.Trace.Write("OnTextUpdate: ");
			SaveSelection();
		}

		/// <summary>Update the selection whenever the text changes</summary>
		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			//System.Diagnostics.Trace.Write("OnTextChanged: ");
			SaveSelection();
		}

		/// <summary>Save the selection after it has been changed by mouse selection</summary>
		protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e)
		{
			base.OnMouseUp(e);
			//System.Diagnostics.Trace.Write("OnMouseUp: ");
			SaveSelection();
		}

		/// <summary>Save the selection after it has been changed by key presses</summary>
		protected override void OnKeyUp(System.Windows.Forms.KeyEventArgs e)
		{
			base.OnKeyUp(e);
			//System.Diagnostics.Trace.Write("OnKeyUp: ");
			SaveSelection();
		}

		/// <summary>Restore the selection</summary>
		private void RestoreSelection()
		{
			// Note, this order is important, also base.Select() uses the wrong order
			base.SelectionLength = m_selection.Sizei;
			base.SelectionStart  = m_selection.Begini;
			//System.Diagnostics.Trace.WriteLine("Selection Restored: [{0},{1}] -> [{2},{3}]".Fmt(m_selection.Begini, m_selection.Sizei, SelectionStart, SelectionLength));
		}

		/// <summary>Save the current selection</summary>
		private void SaveSelection()
		{
			// Only allow selection setting for editable combo box styles
			if (DropDownStyle != System.Windows.Forms.ComboBoxStyle.DropDownList)
			{
				int ss = base.SelectionStart;
				int sl = base.SelectionLength;
				m_selection = new Range(ss,ss+sl);
				//System.Diagnostics.Trace.WriteLine("Selection Saved: [{0},{1}]\n\t{2}".Fmt(m_selection.Begini, m_selection.Sizei, string.Join("\n\t", new StackTrace().GetFrames().Take(5).Select(x => x.GetMethod()))));
			}
		}
	}
}
