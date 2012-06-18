using System;
using System.Collections.Generic;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	public partial class FindUI :Form
	{
		private readonly Settings m_settings;
		private readonly Pattern m_pattern;
		private readonly List<string> m_history;
		private readonly ToolTip m_tt;
		
		/// <summary>The search pattern</summary>
		public string Pattern
		{
			get { return m_pattern.Expr; }
			set { m_pattern.Expr = value; }
		}

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
		
		public FindUI(Settings settings)
		{
			InitializeComponent();
			m_settings = settings;
			m_pattern  = new Pattern();
			m_history  = new List<string>(m_settings.FindHistory);
			m_tt       = new ToolTip();
			
			// Search buttons
			m_btn_find_prev.ToolTip(m_tt, "Search backward through the log.\r\nKeyboard shortcut: <Shift>+<Enter>");
			m_btn_find_prev.Click += (s,a) => RaiseFindPrev();
			
			m_btn_find_next.ToolTip(m_tt, "Search forward through the log.\r\nKeyboard shortcut: <Enter>");
			m_btn_find_next.Click += (s,a) => RaiseFindNext();
			
			// Pattern type
			m_radio_substring.Checked = m_pattern.PatnType == EPattern.Substring;
			m_radio_substring.Click += (s,a)=>
				{
					if (m_radio_substring.Checked) m_pattern.PatnType = EPattern.Substring;
				};
			m_radio_wildcard .Checked = m_pattern.PatnType == EPattern.Wildcard;
			m_radio_wildcard.Click += (s,a)=>
				{
					if (m_radio_wildcard.Checked) m_pattern.PatnType = EPattern.Wildcard;
				};
			m_radio_regex    .Checked = m_pattern.PatnType == EPattern.RegularExpression;
			m_radio_regex.Click += (s,a)=>
				{
					if (m_radio_regex.Checked) m_pattern.PatnType = EPattern.RegularExpression;
				};
			
			// Ignore case
			m_check_ignore_case.ToolTip(m_tt, "Check to make searches ignore differences in case");
			m_check_ignore_case.CheckedChanged += (s,a)=>
				{
					m_pattern.IgnoreCase = m_check_ignore_case.Checked;
				};
			
			// Invert
			m_check_invert.ToolTip(m_tt, "Check to find instances that do not match the search pattern");
			m_check_invert.CheckedChanged += (s,a)=>
				{
					m_pattern.Invert = m_check_invert.Checked;
				};
			
			// Shown
			VisibleChanged += (s,a)=>
				{
					if (Visible)
					{
						m_combo_pattern.Focus();
						m_combo_pattern.Text = Pattern;
						m_combo_pattern.SelectAll();
						
					}
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
				
				// Update the history
				Misc.AddToHistoryList(m_history, m_pattern.Expr, false, Constants.MaxFindHistory);
				m_settings.FindHistory = m_history.ToArray();
				
				// Repopulate the combo
				m_combo_pattern.Items.Clear();
				foreach (var h in m_history)
					m_combo_pattern.Items.Add(h);
				
				return true;
			}
		}
	}
}
