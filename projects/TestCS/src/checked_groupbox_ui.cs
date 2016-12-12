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

		private pr.gui.CheckedGroupBox m_chkgrp0;
		private TextBox textBox1;
		private Button button1;
		private pr.gui.CheckedGroupBox m_chkgrp1;
		private TextBox textBox2;
		private Button button2;
		private pr.gui.CheckedGroupBox m_chkgrp2;
		private TextBox textBox3;
		private Button button3;
		private pr.gui.CheckedGroupBox m_chkgrp3;
		private TextBox textBox4;
		private Button button4;

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
			this.m_chkgrp0 = new pr.gui.CheckedGroupBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.m_chkgrp1 = new pr.gui.CheckedGroupBox();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.button2 = new System.Windows.Forms.Button();
			this.m_chkgrp2 = new pr.gui.CheckedGroupBox();
			this.textBox3 = new System.Windows.Forms.TextBox();
			this.button3 = new System.Windows.Forms.Button();
			this.m_chkgrp3 = new pr.gui.CheckedGroupBox();
			this.textBox4 = new System.Windows.Forms.TextBox();
			this.button4 = new System.Windows.Forms.Button();
			this.m_chkgrp0.SuspendLayout();
			this.m_chkgrp1.SuspendLayout();
			this.m_chkgrp2.SuspendLayout();
			this.m_chkgrp3.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_chkgrp0
			// 
			this.m_chkgrp0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chkgrp0.Checked = false;
			this.m_chkgrp0.CheckState = System.Windows.Forms.CheckState.Unchecked;
			this.m_chkgrp0.Controls.Add(this.textBox1);
			this.m_chkgrp0.Controls.Add(this.button1);
			this.m_chkgrp0.Enabled = false;
			this.m_chkgrp0.Location = new System.Drawing.Point(12, 12);
			this.m_chkgrp0.Name = "m_chkgrp0";
			this.m_chkgrp0.Size = new System.Drawing.Size(260, 85);
			this.m_chkgrp0.Style = pr.gui.CheckedGroupBox.EStyle.Basic;
			this.m_chkgrp0.TabIndex = 0;
			this.m_chkgrp0.TabStop = false;
			this.m_chkgrp0.Text = "Pauls Checked GroupBox";
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
			// m_chkgrp1
			// 
			this.m_chkgrp1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chkgrp1.Checked = false;
			this.m_chkgrp1.CheckState = System.Windows.Forms.CheckState.Unchecked;
			this.m_chkgrp1.Controls.Add(this.textBox2);
			this.m_chkgrp1.Controls.Add(this.button2);
			this.m_chkgrp1.Enabled = false;
			this.m_chkgrp1.Location = new System.Drawing.Point(12, 103);
			this.m_chkgrp1.Name = "m_chkgrp1";
			this.m_chkgrp1.Size = new System.Drawing.Size(260, 83);
			this.m_chkgrp1.Style = pr.gui.CheckedGroupBox.EStyle.CheckBox;
			this.m_chkgrp1.TabIndex = 3;
			this.m_chkgrp1.TabStop = false;
			this.m_chkgrp1.Text = "Pauls Checked GroupBox";
			// 
			// textBox2
			// 
			this.textBox2.Location = new System.Drawing.Point(6, 48);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(100, 20);
			this.textBox2.TabIndex = 1;
			this.textBox2.Text = "Some Text";
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(6, 19);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 0;
			this.button2.Text = "button2";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// m_chkgrp2
			// 
			this.m_chkgrp2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chkgrp2.Checked = false;
			this.m_chkgrp2.CheckState = System.Windows.Forms.CheckState.Unchecked;
			this.m_chkgrp2.Controls.Add(this.textBox3);
			this.m_chkgrp2.Controls.Add(this.button3);
			this.m_chkgrp2.Enabled = false;
			this.m_chkgrp2.Location = new System.Drawing.Point(12, 192);
			this.m_chkgrp2.Name = "m_chkgrp2";
			this.m_chkgrp2.Size = new System.Drawing.Size(260, 83);
			this.m_chkgrp2.Style = pr.gui.CheckedGroupBox.EStyle.RadioBtn;
			this.m_chkgrp2.TabIndex = 5;
			this.m_chkgrp2.TabStop = false;
			this.m_chkgrp2.Text = "Pauls Checked GroupBox";
			// 
			// textBox3
			// 
			this.textBox3.Location = new System.Drawing.Point(6, 48);
			this.textBox3.Name = "textBox3";
			this.textBox3.Size = new System.Drawing.Size(100, 20);
			this.textBox3.TabIndex = 1;
			this.textBox3.Text = "Some Text";
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(6, 19);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(75, 23);
			this.button3.TabIndex = 0;
			this.button3.Text = "button3";
			this.button3.UseVisualStyleBackColor = true;
			// 
			// m_chkgrp3
			// 
			this.m_chkgrp3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chkgrp3.Checked = false;
			this.m_chkgrp3.CheckState = System.Windows.Forms.CheckState.Unchecked;
			this.m_chkgrp3.Controls.Add(this.textBox4);
			this.m_chkgrp3.Controls.Add(this.button4);
			this.m_chkgrp3.Enabled = false;
			this.m_chkgrp3.Location = new System.Drawing.Point(12, 281);
			this.m_chkgrp3.Name = "m_chkgrp3";
			this.m_chkgrp3.Size = new System.Drawing.Size(260, 83);
			this.m_chkgrp3.Style = pr.gui.CheckedGroupBox.EStyle.RadioBtn;
			this.m_chkgrp3.TabIndex = 5;
			this.m_chkgrp3.TabStop = false;
			this.m_chkgrp3.Text = "Pauls Checked GroupBox";
			// 
			// textBox4
			// 
			this.textBox4.Location = new System.Drawing.Point(6, 48);
			this.textBox4.Name = "textBox4";
			this.textBox4.Size = new System.Drawing.Size(100, 20);
			this.textBox4.TabIndex = 1;
			this.textBox4.Text = "Some Text";
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(6, 19);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(75, 23);
			this.button4.TabIndex = 0;
			this.button4.Text = "button4";
			this.button4.UseVisualStyleBackColor = true;
			// 
			// CheckedGroupBoxUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 463);
			this.Controls.Add(this.m_chkgrp3);
			this.Controls.Add(this.m_chkgrp2);
			this.Controls.Add(this.m_chkgrp1);
			this.Controls.Add(this.m_chkgrp0);
			this.Name = "CheckedGroupBoxUI";
			this.Text = "checked_groupbox_ui";
			this.m_chkgrp0.ResumeLayout(false);
			this.m_chkgrp0.PerformLayout();
			this.m_chkgrp1.ResumeLayout(false);
			this.m_chkgrp1.PerformLayout();
			this.m_chkgrp2.ResumeLayout(false);
			this.m_chkgrp2.PerformLayout();
			this.m_chkgrp3.ResumeLayout(false);
			this.m_chkgrp3.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
