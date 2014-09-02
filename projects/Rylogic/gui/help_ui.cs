using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace pr.gui
{
	public sealed class HelpUI :ToolForm
	{
		private Panel      m_panel;
		private WebBrowser m_html;
		private Button m_btn_back;
		private Button m_btn_forward;
		private Label m_lbl_status;
		private Button     m_btn_ok;

		// Create modal instances
		public static DialogResult ShowText(Form owner, string text, string title)
		{
			return ShowHtml(owner, inet.Html.FromText(text), title, Point.Empty, Size.Empty, EPin.TopRight);
		}
		public static DialogResult ShowText(Form owner, string text, string title, Point ofs, Size size, EPin pin)
		{
			return ShowHtml(owner, inet.Html.FromText(text), title, ofs, size, pin);
		}
		public static DialogResult ShowResource(Form owner, string resource_name, Assembly ass, string title)
		{
			return ShowHtml(owner, Util.TextResource(resource_name, ass), title, Point.Empty, Size.Empty, EPin.TopRight);
		}
		public static DialogResult ShowResource(Form owner, string resource_name, Assembly ass, string title, Point ofs, Size size, EPin pin)
		{
			return ShowHtml(owner, Util.TextResource(resource_name, ass), title, ofs, size, pin);
		}
		public static DialogResult ShowHtml(Form owner, string html, string title)
		{
			return ShowHtml(owner, html, title, Point.Empty, Size.Empty, EPin.TopRight);
		}
		public static DialogResult ShowHtml(Form owner, string html, string title, Point ofs, Size size, EPin pin)
		{
			return new HelpUI(owner, html, title, ofs, size, pin, true).ShowDialog(owner);
		}

		// Create non-modal instances
		public static HelpUI FromText(Form parent, string text, string title)
		{
			return FromHtml(parent, inet.Html.FromText(text), title, Point.Empty, Size.Empty, EPin.TopRight);
		}
		public static HelpUI FromText(Form parent, string text, string title, Point ofs, Size size, EPin pin)
		{
			return FromHtml(parent, inet.Html.FromText(text), title, ofs, size, pin);
		}
		public static HelpUI FromResource(Form parent, string resource_name, Assembly ass, string title)
		{
			return FromHtml(parent, Util.TextResource(resource_name, ass), title, Point.Empty, Size.Empty, EPin.TopRight);
		}
		public static HelpUI FromResource(Form parent, string resource_name, Assembly ass, string title, Point ofs, Size size, EPin pin)
		{
			return FromHtml(parent, Util.TextResource(resource_name, ass), title, ofs, size, pin);
		}
		public static HelpUI FromHtml(Form parent, string html, string title)
		{
			return FromHtml(parent, html, title, Point.Empty, Size.Empty, EPin.TopRight);
		}
		public static HelpUI FromHtml(Form parent, string html, string title, Point ofs, Size size, EPin pin)
		{
			return new HelpUI(parent, html, title, ofs, size, pin, false);
		}

		private Uri m_url;
		private readonly Uri m_about_blank = new Uri("about:blank");

		/// <summary>Construct from html. Private constructor so we can create overloads for resources, plain text, and html</summary>
		private HelpUI(Form owner, string html, string title, Point ofs, Size size, EPin pin, bool modal)
		:base(owner, pin, ofs, size, modal)
		{
			InitializeComponent();
			Text = title;
			Html = html;
			m_url = m_about_blank;

			m_html.AllowNavigation = true;
			m_html.StatusTextChanged   += (s,a) => SetStatusText(m_html.StatusText != "Done" ? m_html.StatusText : string.Empty);
			m_html.CanGoForwardChanged += (s,a) => m_btn_forward.Enabled = m_html.CanGoForward;
			m_html.CanGoBackChanged    += (s,a) => m_btn_back.Enabled    = !m_url.AbsoluteUri.Equals(m_about_blank.AbsoluteUri);
			m_html.Navigated           += (s,a) => m_btn_back.Enabled    = !m_url.AbsoluteUri.Equals(m_about_blank.AbsoluteUri);
			m_html.Navigating          += (s,a) => m_url = a.Url;
			m_html.PreviewKeyDown      += (s,a) =>
				{
					// Blocks Refresh which causes rendered html to vanish
					if (a.KeyCode == Keys.F5)
						a.IsInputKey = true;
				};
			m_lbl_status.Visible = false;
			m_lbl_status.Text = m_html.StatusText;

			m_btn_back.Click += (s,a) =>
				{
					if (!m_html.GoBack())
						ResetView();
				};
			m_btn_forward.Click += (s,a) =>
				{
					if (!m_html.GoForward())
						ResetView();
				};
			m_btn_ok.Click += (s,a)=>
				{
					Close();
				};

			Shown += (s,a) => ResetView();

			m_btn_back.Enabled = false;
			m_btn_forward.Enabled = false;
		}

		/// <summary>Set the html for the dialog</summary>
		public string Html { get; set; }

		/// <summary>Restores the help UI to the Html view</summary>
		public void ResetView()
		{
			m_html.DocumentStream = new MemoryStream(Encoding.UTF8.GetBytes(Html));
			m_btn_back.Enabled = false;
			m_url = m_about_blank;
		}

		/// <summary>Set the text of the status text. Clear to hide</summary>
		public void SetStatusText(string text)
		{
			m_lbl_status.Text = text;
			m_lbl_status.Visible = m_lbl_status.Text.HasValue();
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
			this.m_lbl_status = new System.Windows.Forms.Label();
			this.m_html = new System.Windows.Forms.WebBrowser();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_back = new System.Windows.Forms.Button();
			this.m_btn_forward = new System.Windows.Forms.Button();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_panel
			//
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel.Controls.Add(this.m_lbl_status);
			this.m_panel.Controls.Add(this.m_html);
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(624, 411);
			this.m_panel.TabIndex = 6;
			//
			// m_lbl_status
			//
			this.m_lbl_status.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_status.AutoSize = true;
			this.m_lbl_status.BackColor = System.Drawing.Color.LemonChiffon;
			this.m_lbl_status.Location = new System.Drawing.Point(2, 394);
			this.m_lbl_status.Name = "m_lbl_status";
			this.m_lbl_status.Size = new System.Drawing.Size(24, 13);
			this.m_lbl_status.TabIndex = 3;
			this.m_lbl_status.Text = "Idle";
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
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(528, 416);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 7;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			//
			// m_btn_back
			//
			this.m_btn_back.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_back.Font = new System.Drawing.Font("Segoe UI Symbol", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_back.Location = new System.Drawing.Point(12, 416);
			this.m_btn_back.Name = "m_btn_back";
			this.m_btn_back.Size = new System.Drawing.Size(75, 23);
			this.m_btn_back.TabIndex = 8;
			this.m_btn_back.Text = "◀ Back\r\n";
			this.m_btn_back.UseVisualStyleBackColor = true;
			//
			// m_btn_forward
			//
			this.m_btn_forward.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_forward.Font = new System.Drawing.Font("Segoe UI Symbol", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_forward.Location = new System.Drawing.Point(93, 416);
			this.m_btn_forward.Name = "m_btn_forward";
			this.m_btn_forward.Size = new System.Drawing.Size(75, 23);
			this.m_btn_forward.TabIndex = 9;
			this.m_btn_forward.Text = "Forward ▶\r\n";
			this.m_btn_forward.UseVisualStyleBackColor = true;
			//
			// HelpUI
			//
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(624, 446);
			this.Controls.Add(this.m_btn_forward);
			this.Controls.Add(this.m_btn_back);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_panel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "HelpUI";
			this.Text = "Help";
			this.m_panel.ResumeLayout(false);
			this.m_panel.PerformLayout();
			this.ResumeLayout(false);
		}

		#endregion
	}
}
