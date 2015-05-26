using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.util;

namespace pr.gui
{
	public class FindUI :ToolForm
	{
		/// <remarks>
		/// To use this class you need to hook up the 'Find' event so that
		/// when the event occurs you call 'DoFind' providing the appropriate
		/// text to search. 'LastMatch' is a convenience member to save where
		/// the last search was up to.
		/// Note: the group box in the UI is there to make it easier to add other
		/// controls underneath the main find controls
		/// </remarks>
		
		private Pattern m_pat;

		public FindUI(Control target = null, EPin pin = EPin.Centre) :base(target, pin)
		{
			InitializeComponent();
			KeyPreview = true;

			m_pat = new Pattern();
			m_pat.PatternChanged += UpdateUI;

			// Search pattern
			m_cb_pattern.TextChanged += (s,a) =>
				{
					m_pat.Expr = m_cb_pattern.Text;
				};

			// Radio buttons
			m_radio_substr.Click += (s,a) =>
				{
					if (m_radio_substr.Checked)
						m_pat.PatnType = EPattern.Substring;
				};
			m_radio_wildcard.Click += (s,a) =>
				{
					if (m_radio_wildcard.Checked)
						m_pat.PatnType = EPattern.Wildcard;
				};
			m_radio_regexp.Click += (s,a) =>
				{
					if (m_radio_regexp.Checked)
						m_pat.PatnType = EPattern.RegularExpression;
				};

			// Seach options
			m_chk_ignore_case.CheckedChanged += (s,a) =>
				{
					m_pat.IgnoreCase = m_chk_ignore_case.Checked;
				};
			m_chk_invert.CheckedChanged += (s,a) =>
				{
					m_pat.Invert = m_chk_invert.Checked;
				};

			// Find buttons
			m_btn_find_next.Click += (s,a) => OnFind(new FindEventArgs(+1));
			m_btn_find_prev.Click += (s,a) => OnFind(new FindEventArgs(-1));

			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			switch (e.KeyCode)
			{
			case Keys.Escape:
				Hide();
				break;
			case Keys.Enter:
				if (e.Shift) m_btn_find_prev.PerformClick();
				else         m_btn_find_next.PerformClick();
				break;
			}
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			m_cb_pattern.Focus();
		}
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			m_cb_pattern.Focus();
		}

		/// <summary>Raised when the find next/prev button is pressed</summary>
		public event EventHandler<FindEventArgs> Find;

		/// <summary>Event args for find events</summary>
		public class FindEventArgs :EventArgs
		{
			/// <summary>+1 to find the next instances, -1 for the previous instance</summary>
			public int Direction { get; set; }

			public FindEventArgs(int dir)
			{
				Direction = dir;
			}
		}

		/// <summary>Called when FindNext is required</summary>
		protected virtual void OnFind(FindEventArgs args)
		{
			Find.Raise(this, args);
		}

		/// <summary>Perform the find logic on a section of text</summary>
		/// <param name="text">This is the source of text to search</param>
		/// <param name="start">The index of the starting position to begin searching</param>
		/// <param name="length">The number of characters to search</param>
		/// <returns>The sub range within 'text' of a matching occurrence</returns>
		public Range DoFind(string text, int start = 0, int length = -1)
		{
			// Add the search pattern to the history
			PatternHistory = Util.AddToHistoryList(PatternHistory, m_pat.Expr, false, 10);

			// For RegExpr matches, we just want the whole match
			return LastMatch = m_pat.Match(text, start, length).FirstOrDefault();
		}

		/// <summary>Convenience member for saving the last match location</summary>
		public Range LastMatch { get; set; }

		/// <summary>The history of search patterns</summary>
		public string[] PatternHistory
		{
			get { return m_cb_pattern.Items.Cast<string>().ToArray(); }
			set { m_cb_pattern.Items.Clear(); m_cb_pattern.Items.AddRange(value); }
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_cb_pattern.BackColor = m_pat.IsValid ? Color.LightGreen : Color.LightSalmon;
			m_cb_pattern.ToolTip(m_tt, m_pat.SyntaxErrorDescription);

			m_radio_substr  .Checked = m_pat.PatnType == EPattern.Substring;
			m_radio_wildcard.Checked = m_pat.PatnType == EPattern.Wildcard;
			m_radio_regexp  .Checked = m_pat.PatnType == EPattern.RegularExpression;

			m_chk_ignore_case.Checked = m_pat.IgnoreCase;
			m_chk_invert.Checked = m_pat.Invert;
		}

		#region Windows Form Designer generated code

		private ComboBox m_cb_pattern;
		private Button m_btn_find_prev;
		private Button m_btn_find_next;
		private RadioButton m_radio_substr;
		private RadioButton m_radio_wildcard;
		private RadioButton m_radio_regexp;
		private CheckBox m_chk_ignore_case;
		private CheckBox m_chk_invert;
		private ImageList m_il;
		private ToolTip m_tt;
		private GroupBox m_grp_find;

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindUI));
			this.m_cb_pattern = new pr.gui.ComboBox();
			this.m_btn_find_prev = new System.Windows.Forms.Button();
			this.m_il = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_find_next = new System.Windows.Forms.Button();
			this.m_chk_ignore_case = new System.Windows.Forms.CheckBox();
			this.m_radio_substr = new System.Windows.Forms.RadioButton();
			this.m_radio_wildcard = new System.Windows.Forms.RadioButton();
			this.m_radio_regexp = new System.Windows.Forms.RadioButton();
			this.m_chk_invert = new System.Windows.Forms.CheckBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_grp_find = new System.Windows.Forms.GroupBox();
			this.m_grp_find.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_cb_pattern
			// 
			this.m_cb_pattern.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_pattern.FormattingEnabled = true;
			this.m_cb_pattern.Location = new System.Drawing.Point(6, 19);
			this.m_cb_pattern.Name = "m_cb_pattern";
			this.m_cb_pattern.Size = new System.Drawing.Size(206, 21);
			this.m_cb_pattern.TabIndex = 0;
			// 
			// m_btn_find_prev
			// 
			this.m_btn_find_prev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_find_prev.ImageKey = "green_left.png";
			this.m_btn_find_prev.ImageList = this.m_il;
			this.m_btn_find_prev.Location = new System.Drawing.Point(216, 13);
			this.m_btn_find_prev.Name = "m_btn_find_prev";
			this.m_btn_find_prev.Size = new System.Drawing.Size(29, 31);
			this.m_btn_find_prev.TabIndex = 1;
			this.m_btn_find_prev.UseVisualStyleBackColor = true;
			// 
			// m_il
			// 
			this.m_il.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il.ImageStream")));
			this.m_il.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il.Images.SetKeyName(0, "green_left.png");
			this.m_il.Images.SetKeyName(1, "green_right.png");
			// 
			// m_btn_find_next
			// 
			this.m_btn_find_next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_find_next.ImageKey = "green_right.png";
			this.m_btn_find_next.ImageList = this.m_il;
			this.m_btn_find_next.Location = new System.Drawing.Point(245, 13);
			this.m_btn_find_next.Name = "m_btn_find_next";
			this.m_btn_find_next.Size = new System.Drawing.Size(29, 31);
			this.m_btn_find_next.TabIndex = 2;
			this.m_btn_find_next.UseVisualStyleBackColor = true;
			// 
			// m_chk_ignore_case
			// 
			this.m_chk_ignore_case.AutoSize = true;
			this.m_chk_ignore_case.Location = new System.Drawing.Point(6, 69);
			this.m_chk_ignore_case.Name = "m_chk_ignore_case";
			this.m_chk_ignore_case.Size = new System.Drawing.Size(83, 17);
			this.m_chk_ignore_case.TabIndex = 3;
			this.m_chk_ignore_case.Text = "Ignore Case";
			this.m_chk_ignore_case.UseVisualStyleBackColor = true;
			// 
			// m_radio_substr
			// 
			this.m_radio_substr.AutoSize = true;
			this.m_radio_substr.Location = new System.Drawing.Point(4, 46);
			this.m_radio_substr.Name = "m_radio_substr";
			this.m_radio_substr.Size = new System.Drawing.Size(69, 17);
			this.m_radio_substr.TabIndex = 4;
			this.m_radio_substr.TabStop = true;
			this.m_radio_substr.Text = "Substring";
			this.m_radio_substr.UseVisualStyleBackColor = true;
			// 
			// m_radio_wildcard
			// 
			this.m_radio_wildcard.AutoSize = true;
			this.m_radio_wildcard.Location = new System.Drawing.Point(79, 46);
			this.m_radio_wildcard.Name = "m_radio_wildcard";
			this.m_radio_wildcard.Size = new System.Drawing.Size(67, 17);
			this.m_radio_wildcard.TabIndex = 5;
			this.m_radio_wildcard.TabStop = true;
			this.m_radio_wildcard.Text = "Wildcard";
			this.m_radio_wildcard.UseVisualStyleBackColor = true;
			// 
			// m_radio_regexp
			// 
			this.m_radio_regexp.AutoSize = true;
			this.m_radio_regexp.Location = new System.Drawing.Point(152, 46);
			this.m_radio_regexp.Name = "m_radio_regexp";
			this.m_radio_regexp.Size = new System.Drawing.Size(75, 17);
			this.m_radio_regexp.TabIndex = 6;
			this.m_radio_regexp.TabStop = true;
			this.m_radio_regexp.Text = "Reg. Expr.";
			this.m_radio_regexp.UseVisualStyleBackColor = true;
			// 
			// m_chk_invert
			// 
			this.m_chk_invert.AutoSize = true;
			this.m_chk_invert.Location = new System.Drawing.Point(95, 67);
			this.m_chk_invert.Name = "m_chk_invert";
			this.m_chk_invert.Size = new System.Drawing.Size(86, 17);
			this.m_chk_invert.TabIndex = 7;
			this.m_chk_invert.Text = "Invert Match";
			this.m_chk_invert.UseVisualStyleBackColor = true;
			// 
			// m_grp_find
			// 
			this.m_grp_find.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grp_find.Controls.Add(this.m_cb_pattern);
			this.m_grp_find.Controls.Add(this.m_chk_invert);
			this.m_grp_find.Controls.Add(this.m_btn_find_prev);
			this.m_grp_find.Controls.Add(this.m_radio_regexp);
			this.m_grp_find.Controls.Add(this.m_btn_find_next);
			this.m_grp_find.Controls.Add(this.m_radio_wildcard);
			this.m_grp_find.Controls.Add(this.m_chk_ignore_case);
			this.m_grp_find.Controls.Add(this.m_radio_substr);
			this.m_grp_find.Location = new System.Drawing.Point(0, -6);
			this.m_grp_find.Margin = new System.Windows.Forms.Padding(0);
			this.m_grp_find.Name = "m_grp_find";
			this.m_grp_find.Size = new System.Drawing.Size(277, 95);
			this.m_grp_find.TabIndex = 8;
			this.m_grp_find.TabStop = false;
			// 
			// FindUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(277, 89);
			this.Controls.Add(this.m_grp_find);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximumSize = new System.Drawing.Size(1000, 128);
			this.MinimumSize = new System.Drawing.Size(250, 128);
			this.Name = "FindUI";
			this.Text = "Find...";
			this.m_grp_find.ResumeLayout(false);
			this.m_grp_find.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
	}
}
