using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	public class CrippleUI :Form
	{
		private Panel m_panel;
		private WebBrowser m_html;
		private Button m_btn_enable;
		private Button m_btn_close;

		public CrippleUI(string html)
		{
			InitializeComponent();

			m_html.DocumentText = "<html/>";
			Debug.Assert(m_html.Document != null);
			m_html.Document.OpenNew(true);
			m_html.Document.Write(html ?? string.Empty);
			m_html.Navigating += (s,a) =>
				{
					a.Cancel = true;

					// Handle clicks on the purchase link
					if (a.Url.AbsolutePath == Constants.Purchase)
					{
						Process.Start(Constants.StoreLink);
						return;
					}

					// Otherwise, handle links to the documentation
					var exe_dir = Util.ResolveAppPath();
					var uri = new UriBuilder();
					uri.Scheme = Uri.UriSchemeFile;
					uri.Host = string.Empty;
					uri.Path = exe_dir + "/" + a.Url.LocalPath;
					Process.Start(uri.Uri.ToString());
				};
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CrippleUI));
			this.m_btn_enable = new System.Windows.Forms.Button();
			this.m_btn_close = new System.Windows.Forms.Button();
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_html = new System.Windows.Forms.WebBrowser();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btn_enable
			// 
			this.m_btn_enable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_enable.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_enable.Location = new System.Drawing.Point(13, 239);
			this.m_btn_enable.Name = "m_btn_enable";
			this.m_btn_enable.Size = new System.Drawing.Size(215, 23);
			this.m_btn_enable.TabIndex = 0;
			this.m_btn_enable.Text = "Enable this Feature for a Limited Time";
			this.m_btn_enable.UseVisualStyleBackColor = true;
			// 
			// m_btn_close
			// 
			this.m_btn_close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_close.Location = new System.Drawing.Point(356, 239);
			this.m_btn_close.Name = "m_btn_close";
			this.m_btn_close.Size = new System.Drawing.Size(79, 23);
			this.m_btn_close.TabIndex = 1;
			this.m_btn_close.Text = "Close";
			this.m_btn_close.UseVisualStyleBackColor = true;
			// 
			// m_panel
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel.Controls.Add(this.m_html);
			this.m_panel.Location = new System.Drawing.Point(12, 12);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(423, 221);
			this.m_panel.TabIndex = 4;
			// 
			// m_html
			// 
			this.m_html.AllowNavigation = false;
			this.m_html.AllowWebBrowserDrop = false;
			this.m_html.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_html.IsWebBrowserContextMenuEnabled = false;
			this.m_html.Location = new System.Drawing.Point(0, 0);
			this.m_html.MinimumSize = new System.Drawing.Size(20, 20);
			this.m_html.Name = "m_html";
			this.m_html.ScriptErrorsSuppressed = true;
			this.m_html.Size = new System.Drawing.Size(421, 219);
			this.m_html.TabIndex = 0;
			this.m_html.WebBrowserShortcutsEnabled = false;
			// 
			// CrippleUI
			// 
			this.AcceptButton = this.m_btn_enable;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_close;
			this.ClientSize = new System.Drawing.Size(447, 274);
			this.Controls.Add(this.m_panel);
			this.Controls.Add(this.m_btn_close);
			this.Controls.Add(this.m_btn_enable);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(340, 151);
			this.Name = "CrippleUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Free Edition Limited Feature";
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
