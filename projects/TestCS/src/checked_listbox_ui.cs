using System;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.gui;

namespace TestCS
{
	public class CheckedListBoxUI :Form
	{
		[Flags] public enum Things
		{
			One   = 1 << 0 ,
			Two   = 1 << 1 ,
			Three = 1 << 2 ,
			Four  = 1 << 3 ,
			Five  = 1 << 4 ,
			Six   = 1 << 5 ,
		}
		public CheckedListBoxUI()
		{
			InitializeComponent();

			m_chklist.EnumValue = Things.One | Things.Three;
			m_chklist.DrawItem += (s,a) =>
				{
					var checkSize = CheckBoxRenderer.GetGlyphSize(a.Graphics, System.Windows.Forms.VisualStyles.CheckBoxState.MixedNormal);
					var dx = (a.Bounds.Height - checkSize.Width)/2;
					a.DrawBackground();
					var isChecked = m_chklist.GetItemChecked(a.Index);//For some reason e.State doesn't work so we have to do this instead.
					
					CheckBoxRenderer.DrawCheckBox(a.Graphics, new Point(dx, a.Bounds.Top + dx),
						isChecked
							? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
							: System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);

					using (var sf = new StringFormat{ LineAlignment = StringAlignment.Center })
					{
						using (Brush brush = new SolidBrush(isChecked ? Color.Green : ForeColor))
						{
							a.Graphics.DrawString(m_chklist.Items[a.Index].ToString(), Font, brush, new Rectangle(a.Bounds.Height, a.Bounds.Top, a.Bounds.Width - a.Bounds.Height, a.Bounds.Height), sf);
						}
					}
				};
		}

		private FlagCheckedListBox m_chklist;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_chklist = new pr.gui.FlagCheckedListBox();
			this.SuspendLayout();
			//
			// m_chklist
			//
			this.m_chklist.CheckOnClick = true;
			this.m_chklist.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_chklist.FormattingEnabled = true;
			this.m_chklist.Location = new System.Drawing.Point(0, 0);
			this.m_chklist.Name = "m_chklist";
			this.m_chklist.Size = new System.Drawing.Size(284, 262);
			this.m_chklist.TabIndex = 0;
			//
			// CheckedListBoxUI
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.m_chklist);
			this.Name = "CheckedListBoxUI";
			this.Text = "tree_grid_ui";
			this.ResumeLayout(false);
		}

		#endregion
	}
}
