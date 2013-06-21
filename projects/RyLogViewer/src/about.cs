using System;
using System.Reflection;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.gui;
using pr.util;

namespace RyLogViewer
{
	public class About :Form
	{
		private Button m_btn_ok;
		private TextBox m_edit_version;
		private PictureBox pictureBox1;
		private Label m_lbl_licence;
		private Button m_btn_version_history;
		private RichTextBox m_edit_licence;
		private Label m_lbl_info;
		
		public About(Licence licence)
		{
			InitializeComponent();
			
			// Version info
			m_edit_version.Text = string.Format(
				"{0} {1}"+
				"Version: {2}"+
				"Built: {3}"+
				"All Rights Reserved"
				,Util.GetAssemblyAttribute<AssemblyCompanyAttribute>().Company
				,Util.GetAssemblyAttribute<AssemblyCopyrightAttribute>().Copyright + Environment.NewLine
				,Util.AssemblyVersion() + Environment.NewLine
				,Util.AssemblyTimestamp() + Environment.NewLine
				);
			m_edit_version.Select(0,0);
			
			// Licence info
			m_edit_licence.Rtf = licence.InfoStringRtf();
			
			// Version history
			m_btn_version_history.Click += (s,a)=>
				{
					HelpUI.ShowHtml(this, Resources.version_history, "Version History");
				};
		}

		#region Windows Form Designer generated code
		
		/// <summary>Required designer variable.</summary>
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_edit_version = new System.Windows.Forms.TextBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.m_lbl_info = new System.Windows.Forms.Label();
			this.m_lbl_licence = new System.Windows.Forms.Label();
			this.m_btn_version_history = new System.Windows.Forms.Button();
			this.m_edit_licence = new System.Windows.Forms.RichTextBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(255, 241);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(71, 21);
			this.m_btn_ok.TabIndex = 11;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_edit_version
			// 
			this.m_edit_version.AcceptsReturn = true;
			this.m_edit_version.AcceptsTab = true;
			this.m_edit_version.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_version.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_edit_version.Location = new System.Drawing.Point(12, 64);
			this.m_edit_version.Multiline = true;
			this.m_edit_version.Name = "m_edit_version";
			this.m_edit_version.ReadOnly = true;
			this.m_edit_version.Size = new System.Drawing.Size(314, 74);
			this.m_edit_version.TabIndex = 10;
			this.m_edit_version.WordWrap = false;
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::RyLogViewer.Properties.Resources.book;
			this.pictureBox1.Location = new System.Drawing.Point(12, 11);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(48, 48);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pictureBox1.TabIndex = 9;
			this.pictureBox1.TabStop = false;
			// 
			// m_lbl_info
			// 
			this.m_lbl_info.AutoSize = true;
			this.m_lbl_info.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_info.Location = new System.Drawing.Point(66, 23);
			this.m_lbl_info.Name = "m_lbl_info";
			this.m_lbl_info.Size = new System.Drawing.Size(128, 24);
			this.m_lbl_info.TabIndex = 8;
			this.m_lbl_info.Text = "RyLog Viewer";
			// 
			// m_lbl_licence
			// 
			this.m_lbl_licence.AutoSize = true;
			this.m_lbl_licence.Location = new System.Drawing.Point(12, 141);
			this.m_lbl_licence.Name = "m_lbl_licence";
			this.m_lbl_licence.Size = new System.Drawing.Size(69, 13);
			this.m_lbl_licence.TabIndex = 14;
			this.m_lbl_licence.Text = "Licence Info:";
			// 
			// m_btn_version_history
			// 
			this.m_btn_version_history.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_version_history.Location = new System.Drawing.Point(12, 241);
			this.m_btn_version_history.Name = "m_btn_version_history";
			this.m_btn_version_history.Size = new System.Drawing.Size(92, 21);
			this.m_btn_version_history.TabIndex = 15;
			this.m_btn_version_history.Text = "Version History";
			this.m_btn_version_history.UseVisualStyleBackColor = true;
			// 
			// m_edit_licence
			// 
			this.m_edit_licence.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_licence.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_edit_licence.Location = new System.Drawing.Point(15, 157);
			this.m_edit_licence.Name = "m_edit_licence";
			this.m_edit_licence.Size = new System.Drawing.Size(311, 78);
			this.m_edit_licence.TabIndex = 16;
			this.m_edit_licence.Text = "";
			// 
			// About
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(338, 274);
			this.Controls.Add(this.m_edit_licence);
			this.Controls.Add(this.m_btn_version_history);
			this.Controls.Add(this.m_lbl_licence);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_edit_version);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.m_lbl_info);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(229, 234);
			this.Name = "About";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "RyLog Viewer";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

	}
}
