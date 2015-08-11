using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using pr.extn;
using pr.util;
using pr.win32;

namespace pr.gui
{
	using BrowserCtrl = pr.gui.WebBrowser;

	public class HelpUI :ToolForm
	{
		/// <summary>Create a help dialog from plain text, rtf, or html</summary>
		public static HelpUI From(Control parent, EContent type, string title, string content, Point? ofs = null, Size? size = null, EPin pin = EPin.TopRight)
		{
			return new HelpUI(parent, type, title, content, ofs, size, pin, false);
		}

		/// <summary>Show a modal help dialog from plain text, rtf, or html</summary>
		public static DialogResult ShowDialog(Control parent, EContent type, string title, string content, Point? ofs = null, Size? size = null, EPin pin = EPin.TopRight)
		{
			using (var ui = new HelpUI(parent, type, title, content, ofs, size, pin, true))
				return ui.ShowDialog(parent);
		}

		/// <summary>A special url used to mean 'the current help content'</summary>
		private static readonly Uri HelpUrl = new Uri("about:help");

		#region UI Elements
		private Panel m_panel;
		private Label m_lbl_status;
		private Button m_btn_forward;
		private Button m_btn_back;
		private Button m_btn_ok;
		#endregion

		public HelpUI(Control parent, EContent type, string title, string content, Point? ofs = null, Size? size = null, EPin pin = EPin.TopRight, bool modal = false)
			:base(parent, pin, ofs ?? Point.Empty, size ?? Size.Empty, modal)
		{
			InitializeComponent();
			Type = type;
			Text = title;
			Content = content;

			m_btn_back   .Enabled = false;
			m_btn_forward.Enabled = false;
			SetStatusText(null);

			switch (Type)
			{
			default: throw new Exception("Unknown content type");
			case EContent.Text:
				{
					var txt = new TextBox
					{
						Dock        = DockStyle.Fill,
						BorderStyle = BorderStyle.None,
						Multiline   = true,
						ScrollBars  = ScrollBars.Both,
						ReadOnly    = true,
					};
					txt.Text = Content;
					TextCtrl = txt;
					break;
				}
			case EContent.Rtf:
				{
					var rtb = new RichTextBox
					{
						Dock = DockStyle.Fill,
						BorderStyle = BorderStyle.None,
						DetectUrls = true,
						ReadOnly = true,
					};
					rtb.Rtf = Content;
					rtb.LinkClicked += OnLinkClicked;
					TextCtrl = rtb;
					break;
				}
			case EContent.Html:
				{
					var web = new BrowserCtrl{Dock = DockStyle.Fill, AllowNavigation = true};
					web.CanGoForwardChanged += (s,a) => m_btn_forward.Enabled = web.CanGoForward;
					web.CanGoBackChanged    += (s,a) => m_btn_back   .Enabled = web.CanGoBack;
					web.ResolveContent      += (s,a) => ResolveContent(a);
					web.StatusTextChanged   += (s,a) => SetStatusText(web.StatusText != "Done" ? web.StatusText : string.Empty);
					web.UrlHistory.Add(new BrowserCtrl.Visit(HelpUrl));
					TextCtrl = web;
					ShowNavigationButtons = true;
					break;
				}
			}
			TextCtrl.BackColor = SystemColors.Window;
			m_panel.Controls.Add(TextCtrl);

			m_btn_ok     .Click += Close;
			m_btn_forward.Click += OnForward;
			m_btn_back   .Click += OnBack;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			RenderContent();
		}

		/// <summary>The type of text content to display</summary>
		public EContent Type { get; private set; }
		public enum EContent { Text, Rtf, Html }

		/// <summary>The control that displays the content</summary>
		public Control TextCtrl { get; private set; }

		/// <summary>The text,rtf,html to display in the window</summary>
		public string Content { get; set; }

		/// <summary>Show/Hide the navigation buttons</summary>
		public bool ShowNavigationButtons
		{
			get { return m_btn_forward.Visible && m_btn_back.Visible; }
			set { m_btn_forward.Visible = m_btn_back.Visible = value; }
		}

		/// <summary>Update the control with the current content</summary>
		public void RenderContent()
		{
			switch (Type)
			{
			default: throw new Exception("Unknown content type");
			case EContent.Text:
				{
					var txt = TextCtrl.As<TextBox>();
					txt.Text = Content;
					txt.Select(0,0);
					Win32.HideCaret(txt.Handle);
					break;
				}
			case EContent.Rtf:
				{
					var rtf = TextCtrl.As<System.Windows.Forms.RichTextBox>();
					rtf.Rtf = Content;
					rtf.Select(0,0);
					Win32.HideCaret(rtf.Handle);
					break;
				}
			case EContent.Html:
				{
					var web = TextCtrl.As<WebBrowser>();
					web.Reload();
					break;
				}
			}
		}

		/// <summary>Resolve 'args.Url' into content for the web control</summary>
		private void ResolveContent(BrowserCtrl.ResolveContentEventArgs args)
		{
			if (args.Url == HelpUrl)
			{
				args.Content = Content;
			}
			else
			{
				OnResolveContent(args);
				Content = args.Content;
			}
		}

		/// <summary>Handle a link click in the displayed text (only applies to RTF)</summary>
		public event EventHandler<LinkClickedEventArgs> LinkClicked;
		protected virtual void OnLinkClicked(object sender, LinkClickedEventArgs args)
		{
			LinkClicked.Raise(sender, args);
		}

		/// <summary>Resolve a link into content (only applies to html)</summary>
		public event EventHandler<BrowserCtrl.ResolveContentEventArgs> ResolveContentEvent;
		protected virtual void OnResolveContent(BrowserCtrl.ResolveContentEventArgs args)
		{
			ResolveContentEvent.Raise(this, args);
		}

		/// <summary>Default handling for the back button click</summary>
		protected virtual void OnBack(object sender, EventArgs args)
		{
			// Default implementation just resets back to the current content
			// for text,rtf views. Html views support navigation by default
			switch (Type)
			{
			default: throw new Exception("Unknown content type");
			case EContent.Text:
				{
					RenderContent();
					break;
				}
			case EContent.Rtf:
				{
					RenderContent();
					break;
				}
			case EContent.Html:
				{
					var web = TextCtrl.As<BrowserCtrl>();
					if (!web.GoBack())
						RenderContent();
					break;
				}
			}
		}

		/// <summary>Handles the forward button click</summary>
		protected virtual void OnForward(object sender, EventArgs args)
		{
			// Default implementation just resets back to the current content
			// for text,rtf views. Html views support navigation by default
			switch (Type)
			{
			default: throw new Exception("Unknown content type");
			case EContent.Text:
				{
					RenderContent();
					break;
				}
			case EContent.Rtf:
				{
					RenderContent();
					break;
				}
			case EContent.Html:
				{
					var web = TextCtrl.As<BrowserCtrl>();
					if (!web.GoForward())
						RenderContent();
					break;
				}
			}
		}

		/// <summary>Set the text of the status text. Clear to hide</summary>
		public virtual void SetStatusText(string text)
		{
			m_lbl_status.Text = text ?? string.Empty;
			m_lbl_status.Visible = m_lbl_status.Text.HasValue();
		}

		/// <summary>Use the old RichEdit control rather than RICHEDIT50W</summary>
		public void UseRichEdit2()
		{
			m_panel.Controls.Clear();
			TextCtrl = new System.Windows.Forms.RichTextBox
				{
					Dock = DockStyle.Fill,
					BorderStyle = BorderStyle.None,
					DetectUrls = true,
					ReadOnly = true,
				};
			m_panel.Controls.Add(TextCtrl);
			RenderContent();
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelpUI));
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_btn_forward = new System.Windows.Forms.Button();
			this.m_btn_back = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_status = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// m_panel_rtf
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel.Name = "m_panel_rtf";
			this.m_panel.Size = new System.Drawing.Size(656, 625);
			this.m_panel.TabIndex = 1;
			// 
			// m_btn_forward
			// 
			this.m_btn_forward.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_forward.Font = new System.Drawing.Font("Segoe UI Symbol", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_forward.Location = new System.Drawing.Point(128, 643);
			this.m_btn_forward.Margin = new System.Windows.Forms.Padding(4);
			this.m_btn_forward.Name = "m_btn_forward";
			this.m_btn_forward.Size = new System.Drawing.Size(100, 28);
			this.m_btn_forward.TabIndex = 12;
			this.m_btn_forward.Text = "Forward ▶\r\n";
			this.m_btn_forward.UseVisualStyleBackColor = true;
			this.m_btn_forward.Visible = false;
			// 
			// m_btn_back
			// 
			this.m_btn_back.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_back.Font = new System.Drawing.Font("Segoe UI Symbol", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_back.Location = new System.Drawing.Point(20, 643);
			this.m_btn_back.Margin = new System.Windows.Forms.Padding(4);
			this.m_btn_back.Name = "m_btn_back";
			this.m_btn_back.Size = new System.Drawing.Size(100, 28);
			this.m_btn_back.TabIndex = 11;
			this.m_btn_back.Text = "◀ Back\r\n";
			this.m_btn_back.UseVisualStyleBackColor = true;
			this.m_btn_back.Visible = false;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(539, 644);
			this.m_btn_ok.Margin = new System.Windows.Forms.Padding(4);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(100, 28);
			this.m_btn_ok.TabIndex = 10;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_status
			// 
			this.m_lbl_status.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_status.AutoSize = true;
			this.m_lbl_status.BackColor = System.Drawing.Color.LemonChiffon;
			this.m_lbl_status.Location = new System.Drawing.Point(0, 625);
			this.m_lbl_status.Margin = new System.Windows.Forms.Padding(0);
			this.m_lbl_status.Name = "m_lbl_status";
			this.m_lbl_status.Size = new System.Drawing.Size(30, 17);
			this.m_lbl_status.TabIndex = 4;
			this.m_lbl_status.Text = "Idle";
			this.m_lbl_status.Visible = false;
			// 
			// HelpUI2
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(652, 685);
			this.Controls.Add(this.m_lbl_status);
			this.Controls.Add(this.m_btn_forward);
			this.Controls.Add(this.m_btn_back);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_panel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "HelpUI2";
			this.Text = "Help";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
