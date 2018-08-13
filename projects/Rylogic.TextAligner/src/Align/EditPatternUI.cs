using System;
using System.Windows.Forms;

namespace Rylogic.TextAligner
{
	internal class EditPatternUI :Form
	{
		#region UI Elements
		private Rylogic.Gui.PatternUI m_pattern_ui;
		private SplitContainer m_split;
		private System.Windows.Forms.TextBox m_edit_comment;
		#endregion

		public EditPatternUI(AlignPattern pat)
		{
			InitializeComponent();

			if (pat != null)
			{
				m_pattern_ui.EditPattern(pat);
			}
			else
			{
				m_pattern_ui.NewPattern(new AlignPattern());
			}

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>The pattern being edited</summary>
		public AlignPattern Pattern
		{
			get { return (AlignPattern)m_pattern_ui.Original; }
		}

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Commit
		{
			add { m_pattern_ui.Commit += value; }
			remove { m_pattern_ui.Commit -= value; }
		}

		/// <summary></summary>
		private void SetupUI()
		{
			var pat = (AlignPattern)m_pattern_ui.Pattern;
			m_edit_comment.Text = !string.IsNullOrEmpty(pat.Comment) ? pat.Comment : "Enter a reminder comment here";
			m_edit_comment.TextChanged += (s, a) =>
			{
				pat.Comment = m_edit_comment.Text;
			};
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_pattern_ui = new Rylogic.Gui.PatternUI();
			this.m_edit_comment = new System.Windows.Forms.TextBox();
			this.m_split = new System.Windows.Forms.SplitContainer();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_pattern_ui
			// 
			this.m_pattern_ui.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pattern_ui.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pattern_ui.Location = new System.Drawing.Point(0, 0);
			this.m_pattern_ui.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_ui.MinimumSize = new System.Drawing.Size(357, 138);
			this.m_pattern_ui.Name = "m_pattern_ui";
			this.m_pattern_ui.Size = new System.Drawing.Size(444, 217);
			this.m_pattern_ui.TabIndex = 1;
			this.m_pattern_ui.TestText = "Enter text here to test your pattern";
			this.m_pattern_ui.Touched = false;
			// 
			// m_edit_comment
			// 
			this.m_edit_comment.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_edit_comment.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_edit_comment.Location = new System.Drawing.Point(0, 0);
			this.m_edit_comment.Multiline = true;
			this.m_edit_comment.Name = "m_edit_comment";
			this.m_edit_comment.Size = new System.Drawing.Size(444, 72);
			this.m_edit_comment.TabIndex = 2;
			// 
			// m_split
			// 
			this.m_split.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split.Location = new System.Drawing.Point(8, 8);
			this.m_split.Name = "m_split";
			this.m_split.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split.Panel1
			// 
			this.m_split.Panel1.Controls.Add(this.m_pattern_ui);
			// 
			// m_split.Panel2
			// 
			this.m_split.Panel2.Controls.Add(this.m_edit_comment);
			this.m_split.Size = new System.Drawing.Size(444, 293);
			this.m_split.SplitterDistance = 217;
			this.m_split.TabIndex = 3;
			// 
			// EditPatternUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(460, 309);
			this.Controls.Add(this.m_split);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "EditPatternUI";
			this.Padding = new System.Windows.Forms.Padding(8);
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Edit Alignment Pattern";
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel2.ResumeLayout(false);
			this.m_split.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
