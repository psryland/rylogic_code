using System.Linq;
using System.Windows.Forms;
using Rylogic.Utility;

namespace TestCS
{
	public class WebBrowserUI :Form
	{
		public WebBrowserUI()
		{
			InitializeComponent();

			m_web.CanGoBackChanged    += (s,a) => m_btn_back   .Enabled = m_web.CanGoBack;
			m_web.CanGoForwardChanged += (s,a) => m_btn_forward.Enabled = m_web.CanGoForward;

			m_web.ResolveContent += (s,a) =>
				{
					switch (a.Url.AbsolutePath.ToLowerInvariant())
					{
					case "page1":
						a.Content = "<html>This is page1 <a href='about:Page2'>Go To Page2</a> or <a href='about:Page3'>show history</a></html>";
						break;
					case "page2":
						a.Content = "<html>This is page2 <a href='about:Page1'>Go To Page1</a> or to <a href='http://www.google.com'>Google</a></html>";
						break;
					case "page3":
						a.Content = "<html>" + string.Join("<br/>", m_web.UrlHistory.Select(x => x.Url.ToString())) + "</html>";
						break;
					}
				};

			m_web.Navigate("about:Page1");

			m_btn_forward.Enabled = m_web.CanGoForward;
			m_btn_back   .Enabled = m_web.CanGoBack;

			m_btn_forward.Click += (s,a) => m_web.GoForward();
			m_btn_back   .Click += (s,a) => m_web.GoBack();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		private Rylogic.Gui.WinForms.WebBrowser m_web;
		private Button m_btn_forward;
		private Button m_btn_back;

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_web = new Rylogic.Gui.WinForms.WebBrowser();
			this.m_btn_forward = new System.Windows.Forms.Button();
			this.m_btn_back = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_web
			// 
			this.m_web.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_web.Location = new System.Drawing.Point(0, 0);
			this.m_web.MinimumSize = new System.Drawing.Size(20, 20);
			this.m_web.Name = "m_web";
			this.m_web.Size = new System.Drawing.Size(295, 218);
			this.m_web.TabIndex = 0;
			// 
			// m_btn_forward
			// 
			this.m_btn_forward.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_forward.Location = new System.Drawing.Point(208, 229);
			this.m_btn_forward.Name = "m_btn_forward";
			this.m_btn_forward.Size = new System.Drawing.Size(75, 23);
			this.m_btn_forward.TabIndex = 1;
			this.m_btn_forward.Text = "Forward";
			this.m_btn_forward.UseVisualStyleBackColor = true;
			// 
			// m_btn_back
			// 
			this.m_btn_back.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_back.Location = new System.Drawing.Point(12, 229);
			this.m_btn_back.Name = "m_btn_back";
			this.m_btn_back.Size = new System.Drawing.Size(75, 23);
			this.m_btn_back.TabIndex = 2;
			this.m_btn_back.Text = "Back";
			this.m_btn_back.UseVisualStyleBackColor = true;
			// 
			// WebBrowserUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(295, 264);
			this.Controls.Add(this.m_btn_back);
			this.Controls.Add(this.m_btn_forward);
			this.Controls.Add(this.m_web);
			this.Name = "WebBrowserUI";
			this.Text = "web_browser_ui";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
