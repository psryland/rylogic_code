using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RyLogViewer
{
	public class HelpUI :Form
	{
		private const string HelpNotFound = @"<p>Help Reference Guide not found</p>";
		private Panel      m_panel;
		private WebBrowser m_html;
		private Button     m_btn_ok;
		
		public static DialogResult Show(IWin32Window owner, string resource_name, string title) { return Show(owner, resource_name, title, Point.Empty, Size.Empty); }
		public static DialogResult Show(IWin32Window owner, string resource_name, string title, Point loc, Size size)
		{
			Assembly ass = Assembly.GetExecutingAssembly();
			Stream stream = ass.GetManifestResourceStream(resource_name);
			
			var ui = new HelpUI{Text = title};
			if (loc != Point.Empty) ui.Location = loc;
			if (size != Size.Empty) ui.Size = size;
			
			Debug.Assert(ui.m_html.Document != null, "ui.m_html.Document != null");
			ui.m_html.Document.OpenNew(true);
			
			if (stream == null)
			{
				ui.m_html.Document.Write(HelpNotFound);
			}
			else
			{
				using (TextReader r = new StreamReader(stream))
					ui.m_html.Document.Write(r.ReadToEnd());
			}
			return ui.ShowDialog(owner);
		}

		private HelpUI()
		{
			InitializeComponent();
			m_html.DocumentText = "<html/>";
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelpUI));
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_html = new System.Windows.Forms.WebBrowser();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_panel
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel.Controls.Add(this.m_html);
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(624, 411);
			this.m_panel.TabIndex = 6;
			// 
			// m_html
			// 
			this.m_html.AllowWebBrowserDrop = false;
			this.m_html.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_html.IsWebBrowserContextMenuEnabled = false;
			this.m_html.Location = new System.Drawing.Point(0, 0);
			this.m_html.MinimumSize = new System.Drawing.Size(20, 20);
			this.m_html.Name = "m_html";
			this.m_html.ScriptErrorsSuppressed = true;
			this.m_html.Size = new System.Drawing.Size(622, 409);
			this.m_html.TabIndex = 2;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(275, 417);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 7;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// HelpUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 446);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_panel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "HelpUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Help";
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
