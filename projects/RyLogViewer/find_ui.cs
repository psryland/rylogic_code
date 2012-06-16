using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace RyLogViewer
{
	public partial class FindUI :Form
	{
		private readonly Pattern m_pattern;
		private readonly BindingList<string> m_history;
		
		/// <summary>An event called whenever the dialog gets a FindNext command</summary>
		public event Action<Pattern> FindNext;
		private void RaiseFindNext()
		{
			if (!UseFindExpr) return;
			m_btn_find_next.Enabled = false;
			if (FindNext != null) FindNext(m_pattern);
			m_btn_find_next.Enabled = true;
		}
		
		/// <summary>An event called whenever the dialog gets a FindPrev command</summary>
		public event Action<Pattern> FindPrev;
		private void RaiseFindPrev()
		{
			if (!UseFindExpr) return;
			m_btn_find_prev.Enabled = false;
			if (FindPrev != null) FindPrev(m_pattern);
			m_btn_find_prev.Enabled = true;
		}
		
		public FindUI(BindingList<string> find_history)
		{
			InitializeComponent();
			m_pattern = new Pattern();
			m_history = find_history;
			m_radio_substring.Checked = m_pattern.PatnType == EPattern.Substring;
			m_radio_wildcard .Checked = m_pattern.PatnType == EPattern.Wildcard;
			m_radio_regex    .Checked = m_pattern.PatnType == EPattern.RegularExpression;
			
			foreach (var h in m_history) m_combo_pattern.Items.Add(h);
			m_history.ListChanged += (s,a)=>
				{
					if (a.ListChangedType != ListChangedType.ItemAdded) return;
					m_combo_pattern.Items.Clear();
					foreach (var h in m_history) m_combo_pattern.Items.Add(h);
				};

			m_btn_find_prev.Click += (s,a) => RaiseFindPrev();
			m_btn_find_next.Click += (s,a) => RaiseFindNext();

			m_radio_substring.Click += (s,a)=>
				{
					if (m_radio_substring.Checked) m_pattern.PatnType = EPattern.Substring;
				};
			m_radio_wildcard.Click += (s,a)=>
				{
					if (m_radio_wildcard.Checked) m_pattern.PatnType = EPattern.Wildcard;
				};
			m_radio_regex.Click += (s,a)=>
				{
					if (m_radio_regex.Checked) m_pattern.PatnType = EPattern.RegularExpression;
				};
			m_check_ignore_case.CheckedChanged += (s,a)=>
				{
					m_pattern.IgnoreCase = m_check_ignore_case.Checked;
				};
			m_check_invert.CheckedChanged += (s,a)=>
				{
					m_pattern.Invert = m_check_invert.Checked;
				};
			
			// Shown
			Shown += (s,a)=>
				{
					m_combo_pattern.Focus();
				};
			FormClosing += (s,a)=>
				{
					if (a.CloseReason != CloseReason.UserClosing) return;
					Hide();
					a.Cancel = true;
					Owner.Focus();
				};
		}
		
		/// <summary>Handle key presses</summary>
		protected override void  OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			if (e.KeyCode == Keys.Escape)
			{
				Close();
				return;
			}
			if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.F3)
			{
				if (!e.Shift) RaiseFindNext();
				else          RaiseFindPrev();
				return;
			}
 			
			e.Handled = false;
			base.OnKeyDown(e);
		}
		
		/// <summary>Returns true if the find expression can be used</summary>
		private bool UseFindExpr
		{
			get
			{
				if (m_combo_pattern.Text.Length == 0) return false;
				m_pattern.Expr = m_combo_pattern.Text;
				AddToFindHistory(m_pattern.Expr);
				return true;
			}
		}
		
		/// <summary>Update the find history to include 'pattern'</summary>
		private void AddToFindHistory(string pattern)
		{
			m_history.Remove(pattern);
			m_history.Insert(0, pattern);
		}
	}
}
