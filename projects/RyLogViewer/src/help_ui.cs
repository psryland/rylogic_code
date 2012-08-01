using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	public sealed class HelpUI :Form
	{
		private const string HelpNotFound = @"<p>Help Reference Guide not found</p>";
		private Panel      m_panel;
		private WebBrowser m_html;
		private Button     m_btn_ok;
		private Size       m_parent_ofs;
		
		// Create modal instances
		public static DialogResult ShowText(Form owner, string text, string title)
		{
			return ShowHtml(owner, Util.TextToHtml(text), title, Point.Empty, Size.Empty);
		}
		public static DialogResult ShowText(Form owner, string text, string title, Point loc, Size size)
		{
			return ShowHtml(owner, Util.TextToHtml(text), title, loc, size);
		}
		public static DialogResult ShowResource(Form owner, string resource_name, string title)
		{
			return ShowHtml(owner, Misc.TextResource(resource_name), title, Point.Empty, Size.Empty);
		}
		public static DialogResult ShowResource(Form owner, string resource_name, string title, Point loc, Size size)
		{
			return ShowHtml(owner, Misc.TextResource(resource_name), title, loc, size);
		}
		public static DialogResult ShowHtml(Form owner, string html, string title)
		{
			return ShowHtml(owner, html, title, Point.Empty, Size.Empty);
		}
		public static DialogResult ShowHtml(Form owner, string html, string title, Point loc, Size size)
		{
			return new HelpUI(owner, html, title, loc, size, true).ShowDialog(owner);
		}
		
		// Create non-modal instances
		public static HelpUI FromText(Form parent, string text, string title)
		{
			return FromHtml(parent, Util.TextToHtml(text), title, Point.Empty, Size.Empty);
		}
		public static HelpUI FromText(Form parent, string text, string title, Point loc, Size size)
		{
			return FromHtml(parent, Util.TextToHtml(text), title, loc, size);
		}
		public static HelpUI FromResource(Form parent, string resource_name, string title)
		{
			return FromHtml(parent, Misc.TextResource(resource_name), title, Point.Empty, Size.Empty);
		}
		public static HelpUI FromResource(Form parent, string resource_name, string title, Point loc, Size size)
		{
			return FromHtml(parent, Misc.TextResource(resource_name), title, loc, size);
		}
		public static HelpUI FromHtml(Form parent, string html, string title)
		{
			return FromHtml(parent, html, title, Point.Empty, Size.Empty);
		}
		public static HelpUI FromHtml(Form parent, string html, string title, Point loc, Size size)
		{
			return new HelpUI(parent, html, title, loc, size, false);
		}
		
		/// <summary>Construct from html. Private constructor so we can create overloads for resources, plain text, and html</summary>
		private HelpUI(Form parent, string html, string title, Point loc, Size size, bool modal)
		{
			InitializeComponent();
			Text = title;
			Owner = parent;
			StartPosition = FormStartPosition.CenterParent;
			if (loc != Point.Empty) { StartPosition = FormStartPosition.Manual; Location = loc; }
			if (size != Size.Empty) Size = size;
			
			m_html.DocumentText = "<html/>";
			Debug.Assert(m_html.Document != null);
			m_html.Document.OpenNew(true);
			m_html.Document.Write(html ?? HelpNotFound);
			
			EventHandler parent_moved = (s,a)=>
				{
					if (Owner != null)
						Location = Owner.Location + m_parent_ofs;
				};
			EventHandler record_offset = (s,a)=>
				{
					m_parent_ofs = Owner != null
						? new Size(Location.X - Owner.Location.X, Location.Y - Owner.Location.Y)
						: Size.Empty;
				};
			
			m_btn_ok.Click += (s,a)=>
				{
					Close();
				};
			
			// Whenever the window moves, save it's offset from the owner
			Move += record_offset;
			
			// Whenever the owner moves, move this form as well
			if (Owner != null)
				Owner.Move += parent_moved;
			
			// On closing, if non-modal just hide, otherwise remove the Move handler from the owner
			FormClosing += (s,a)=>
				{
					if (!modal && a.CloseReason == CloseReason.UserClosing)
					{
						Hide();
						a.Cancel = true;
						if (Owner != null)
							Owner.Focus();
					}
					else
					{
						if (Owner != null)
							Owner.Move -= parent_moved;
					}
				};
			
			record_offset(null,null);
		}
		
		/// <summary>Display the UI</summary>
		public void Display()
		{
			if (Owner != null)
				Location = Owner.Location + m_parent_ofs;
			if (!Visible)
				Show(Owner);
			else
				Focus();
		}

		/// <summary>Handle key presses</summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			if (e.KeyCode == Keys.Escape)
			{
				Close();
				return;
			}
			e.Handled = false;
			base.OnKeyDown(e);
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
