using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	public class About :Form
	{
		private Label m_lbl_version_history;
		private TextBox m_edit_version_history;
		private Button m_btn_ok;
		private TextBox m_edit_version;
		private PictureBox pictureBox1;
		private Label m_lbl_info;
		
		public About()
		{
			InitializeComponent();
			
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

			Stream stream = Assembly.GetEntryAssembly().GetManifestResourceStream("RyLogViewer.docs.VersionHistory.txt");
			if (stream != null)
			{
				using (TextReader r = new StreamReader(stream))
					m_edit_version_history.Text = r.ReadToEnd();
				m_edit_version_history.Select(0,0);
			}
		}
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
			this.m_lbl_version_history = new System.Windows.Forms.Label();
			this.m_edit_version_history = new System.Windows.Forms.TextBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_edit_version = new System.Windows.Forms.TextBox();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.m_lbl_info = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// m_lbl_version_history
			// 
			this.m_lbl_version_history.AutoSize = true;
			this.m_lbl_version_history.Location = new System.Drawing.Point(9, 141);
			this.m_lbl_version_history.Name = "m_lbl_version_history";
			this.m_lbl_version_history.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_version_history.TabIndex = 13;
			this.m_lbl_version_history.Text = "Version History:";
			// 
			// m_edit_version_history
			// 
			this.m_edit_version_history.AcceptsReturn = true;
			this.m_edit_version_history.AcceptsTab = true;
			this.m_edit_version_history.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_version_history.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_edit_version_history.Location = new System.Drawing.Point(12, 157);
			this.m_edit_version_history.Multiline = true;
			this.m_edit_version_history.Name = "m_edit_version_history";
			this.m_edit_version_history.ReadOnly = true;
			this.m_edit_version_history.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.m_edit_version_history.Size = new System.Drawing.Size(287, 90);
			this.m_edit_version_history.TabIndex = 12;
			this.m_edit_version_history.WordWrap = false;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(228, 253);
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
			this.m_edit_version.Size = new System.Drawing.Size(287, 74);
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
			// About
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(311, 286);
			this.Controls.Add(this.m_lbl_version_history);
			this.Controls.Add(this.m_edit_version_history);
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
