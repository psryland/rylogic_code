using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.util;

namespace TestCS
{
	public class CheckedGroupBoxUI :Form
	{
		public CheckedGroupBoxUI()
		{
			InitializeComponent();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		private pr.gui.CheckedGroupBox m_chkgrp;
		private TextBox textBox1;
		private Button button1;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_chkgrp = new pr.gui.CheckedGroupBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.m_chkgrp.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_chkgrp
			// 
			this.m_chkgrp.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chkgrp.Checked = false;
			this.m_chkgrp.CheckState = System.Windows.Forms.CheckState.Unchecked;
			this.m_chkgrp.Controls.Add(this.textBox1);
			this.m_chkgrp.Controls.Add(this.button1);
			this.m_chkgrp.Enabled = false;
			this.m_chkgrp.Location = new System.Drawing.Point(12, 12);
			this.m_chkgrp.Name = "m_chkgrp";
			this.m_chkgrp.Size = new System.Drawing.Size(260, 237);
			this.m_chkgrp.TabIndex = 0;
			this.m_chkgrp.TabStop = false;
			this.m_chkgrp.Text = "Pauls Checked GroupBox";
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(6, 48);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(100, 20);
			this.textBox1.TabIndex = 1;
			this.textBox1.Text = "Some Text";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(6, 19);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 0;
			this.button1.Text = "button1";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// CheckedGroupBoxUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.m_chkgrp);
			this.Name = "CheckedGroupBoxUI";
			this.Text = "checked_groupbox_ui";
			this.m_chkgrp.ResumeLayout(false);
			this.m_chkgrp.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
