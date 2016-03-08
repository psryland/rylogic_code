using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using pr.extn;
using pr.maths;
using pr.win32;

namespace pr.gui
{
	/// <summary>A helper dialog for prompting for a single line of user input</summary>
	public class MsgBox :Form
	{
		// Notes:
		//  - To override the behaviour of a button, set its DialogResult to None. This will prevent
		//    it from closing the message box. You can then hook up whatever handler you like.

		#region UI Elements
		private Panel m_panel;
		private PictureBox m_image;
		private RichTextBox m_message;
		private Button m_btn_positive;
		private Button m_btn_neutral;
		private Button m_btn_negative;
		#endregion

		/// <summary>Display a modal message box</summary>
		public static DialogResult Show(Control owner, string message, string title, MessageBoxButtons btns = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxDefaultButton dflt_btn = MessageBoxDefaultButton.Button1, bool reflow = true, float reflow_aspect = 7f)
		{
			using (var dlg = new MsgBox(owner, message, title, btns, icon, dflt_btn))
			{
				dlg.Reflow = reflow;
				dlg.ReflowAspectRatio = reflow_aspect;
				return dlg.ShowDialog(owner);
			}
		}

		/// <summary>Display a modal message box</summary>
		public static DialogResult Show(string message, string title, MessageBoxButtons btns = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxDefaultButton dflt_btn = MessageBoxDefaultButton.Button1, bool reflow = true, float reflow_aspect = 7f)
		{
			return Show(null, message,title, btns, icon, dflt_btn, reflow, reflow_aspect);
		}

		public MsgBox()                                                                          :this(null, string.Empty, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1) {}
		public MsgBox(string message, string title)                                              :this(null, message, title, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1) {}
		public MsgBox(string message, string title, MessageBoxButtons btns)                      :this(null, message, title, btns, MessageBoxIcon.None, MessageBoxDefaultButton.Button1) {}
		public MsgBox(string message, string title, MessageBoxButtons btns, MessageBoxIcon icon) :this(null, message, title, btns, icon, MessageBoxDefaultButton.Button1) {}
		public MsgBox(Control owner, string message, string title)                                              :this(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1) {}
		public MsgBox(Control owner, string message, string title, MessageBoxButtons btns)                      :this(owner, message, title, btns, MessageBoxIcon.None, MessageBoxDefaultButton.Button1) {}
		public MsgBox(Control owner, string message, string title, MessageBoxButtons btns, MessageBoxIcon icon) :this(owner, message, title, btns, icon, MessageBoxDefaultButton.Button1) {}
		public MsgBox(Control owner, string message, string title, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton dflt_btn)
		{
			InitializeComponent();
			StartPosition = FormStartPosition.CenterParent;
			Title = title;
			Message = message.LineEnding("\r\n");
			Reflow = true;
			ReflowAspectRatio = 7f;

			Owner = owner != null ? owner.TopLevelControl as Form : (Form)null;
			if (Owner != null)
			{
				Icon = Owner.Icon;
				ShowIcon = Owner.ShowIcon &&
					Owner.Icon != null &&
					Owner.FormBorderStyle != FormBorderStyle.FixedToolWindow &&
					Owner.FormBorderStyle != FormBorderStyle.SizableToolWindow;
			}
			else
			{
				Icon = null;
				ShowIcon = false;
			}

			m_message.LinkClicked += (s,a) =>
			{
				try { System.Diagnostics.Process.Start("explorer.exe", a.LinkText); }
				catch (Exception ex)
				{
					MsgBox.Show(Owner, "Failed to navigate to link\r\nReason: " + ex.Message, "Link Failed", MessageBoxButtons.OK);
				}
			};

			AcceptButton = null;
			CancelButton = null;

			m_btn_positive.Text         = string.Empty;
			m_btn_neutral .Text         = string.Empty;
			m_btn_negative.Text         = string.Empty;
			m_btn_positive.DialogResult = DialogResult.None;
			m_btn_neutral .DialogResult = DialogResult.None;
			m_btn_negative.DialogResult = DialogResult.None;

			EventHandler on_clicked = (s,a) =>
			{
				DialogResult = ((Button)s).DialogResult;
				if (DialogResult != DialogResult.None)
					Close();
			};

			m_btn_positive.Click += on_clicked;
			m_btn_neutral .Click += on_clicked;
			m_btn_negative.Click += on_clicked;

			switch (btns)
			{
			default: throw new ArgumentOutOfRangeException("btns");
			case MessageBoxButtons.OK:
				m_btn_positive.Text         = "OK";
				m_btn_positive.DialogResult = DialogResult.OK;
				AcceptButton = m_btn_positive;
				CancelButton = m_btn_positive;
				break;
			case MessageBoxButtons.OKCancel:
				m_btn_positive.Text         = "OK";
				m_btn_positive.DialogResult = DialogResult.OK;
				m_btn_negative.Text         = "Cancel";
				m_btn_negative.DialogResult = DialogResult.Cancel;
				AcceptButton = m_btn_positive;
				CancelButton = m_btn_negative;
				break;
			case MessageBoxButtons.AbortRetryIgnore:
				m_btn_positive.Text         = "&Abort";
				m_btn_positive.DialogResult = DialogResult.Abort;
				m_btn_neutral .Text         = "&Retry";
				m_btn_neutral .DialogResult = DialogResult.Retry;
				m_btn_negative.Text         = "&Ignore";
				m_btn_negative.DialogResult = DialogResult.Ignore;
				break;
			case MessageBoxButtons.YesNoCancel:
				m_btn_positive.Text         = "&Yes";
				m_btn_positive.DialogResult = DialogResult.Yes;
				m_btn_neutral .Text         = "&No";
				m_btn_neutral .DialogResult = DialogResult.No;
				m_btn_negative.Text         = "Cancel";
				m_btn_negative.DialogResult = DialogResult.Cancel;
				AcceptButton = m_btn_positive;
				CancelButton = m_btn_negative;
				break;
			case MessageBoxButtons.YesNo:
				m_btn_positive.Text         = "&Yes";
				m_btn_positive.DialogResult = DialogResult.Yes;
				m_btn_neutral .Text         = "&No";
				m_btn_neutral .DialogResult = DialogResult.No;
				AcceptButton = m_btn_positive;
				break;
			case MessageBoxButtons.RetryCancel:
				m_btn_positive.Text         = "&Retry";
				m_btn_positive.DialogResult = DialogResult.Retry;
				m_btn_negative.Text         = "Cancel";
				m_btn_negative.DialogResult = DialogResult.Cancel;
				AcceptButton = m_btn_neutral;
				CancelButton = m_btn_negative;
				break;
			}

			switch (icon)
			{
			default: throw new ArgumentOutOfRangeException("icon");
			case MessageBoxIcon.None:        Image = null; break;
			case MessageBoxIcon.Error:       Image = SystemIcons.Error.ToBitmap(); break;
			case MessageBoxIcon.Question:    Image = SystemIcons.Question.ToBitmap(); break;
			case MessageBoxIcon.Exclamation: Image = SystemIcons.Exclamation.ToBitmap(); break;
			case MessageBoxIcon.Asterisk:    Image = SystemIcons.Asterisk.ToBitmap(); break;
			}

			switch (dflt_btn)
			{
			default: throw new ArgumentOutOfRangeException("dflt_btn");
			case MessageBoxDefaultButton.Button1: if (m_btn_positive.Text != string.Empty) AcceptButton = m_btn_positive; break;
			case MessageBoxDefaultButton.Button2: if (m_btn_neutral .Text != string.Empty) AcceptButton = m_btn_neutral ;  break;
			case MessageBoxDefaultButton.Button3: if (m_btn_negative.Text != string.Empty) AcceptButton = m_btn_negative; break;
			}
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			UpdateLayout();
			if (StartPosition == FormStartPosition.CenterParent)
			{
				if (ParentForm != null)
					CenterToParent();
				else
					CenterToScreen();
			}
		}

		/// <summary>The message box title</summary>
		public string Title
		{
			get { return Text; }
			set { Text = value; }
		}

		/// <summary>The message body</summary>
		public string Message
		{
			get { return m_message.Text; }
			set { m_message.Text = value; }
		}

		/// <summary>The message body in RTF</summary>
		public string MessageRtf
		{
			get { return m_message.Rtf; }
			set { m_message.Rtf = value; }
		}

		/// <summary>The panel containing the message + icon</summary>
		public Panel Panel
		{
			get { return m_panel; }
		}

		/// <summary>The control that displays the message text</summary>
		public RichTextBox TextBox
		{
			get { return m_message; }
		}

		/// <summary>The image to display next to the message body</summary>
		public Image Image
		{
			get { return m_image.Image; }
			set { m_image.Image = value; }
		}

		/// <summary>Allow access to the icon image control</summary>
		public PictureBox ImageCtrl
		{
			get { return m_image; }
		}

		/// <summary>The positive button control</summary>
		public Button PositiveBtn
		{
			get { return m_btn_positive; }
		}

		/// <summary>The neutral button control</summary>
		public Button NeutralBtn
		{
			get { return m_btn_neutral; }
		}

		/// <summary>The negative button control</summary>
		public Button NegativeBtn
		{
			get { return m_btn_negative; }
		}

		/// <summary>Set to true to have the dialog automatically line wrap text. False to honour message new lines</summary>
		public bool Reflow
		{
			get { return m_reflow; }
			set { m_reflow = value; }
		}
		private bool m_reflow;

		/// <summary>The ratio of width to height used to decide where to wrap text</summary>
		public float ReflowAspectRatio
		{
			get { return m_reflow_aspect; }
			set { m_reflow_aspect = value; }
		}
		private float m_reflow_aspect;

		/// <summary>
		/// Get/Set the text on the positive button.<para/>
		/// Positive button mapping for MessageBoxButtons:<para/>
		/// MessageBoxButtons.OK                ---&gt; DialogResult.OK;<para/>
		/// MessageBoxButtons.OKCancel          ---&gt; DialogResult.OK;<para/>
		/// MessageBoxButtons.AbortRetryIgnore  ---&gt; DialogResult.Abort;<para/>
		/// MessageBoxButtons.YesNoCancel       ---&gt; DialogResult.Yes;<para/>
		/// MessageBoxButtons.YesNo             ---&gt; DialogResult.Yes;<para/>
		/// MessageBoxButtons.RetryCancel       ---&gt; DialogResult.Retry;<para/></summary>
		public string PositiveBtnText
		{
			get { return m_btn_positive.Text; }
			set { m_btn_positive.Text = value; }
		}

		/// <summary>
		/// Get/Set the text on the neutral button.<para/>
		/// Neutral button mapping for MessageBoxButtons:<para/>
		/// MessageBoxButtons.OK               ---&gt; not visible<para/>
		/// MessageBoxButtons.OKCancel         ---&gt; not visible<para/>
		/// MessageBoxButtons.AbortRetryIgnore ---&gt; DialogResult.Retry;<para/>
		/// MessageBoxButtons.YesNoCancel      ---&gt; DialogResult.No;<para/>
		/// MessageBoxButtons.YesNo            ---&gt; DialogResult.No;<para/>
		/// MessageBoxButtons.RetryCancel      ---&gt; not visible<para/></summary>
		public string NeutralBtnText
		{
			get { return m_btn_neutral.Text; }
			set { m_btn_neutral.Text = value; }
		}

		/// <summary>
		/// Get/Set the text on the negative button.<para/>
		/// Negative button mapping for MessageBoxButtons:<para/>
		/// MessageBoxButtons.OK               ---&gt; not visible<para/>
		/// MessageBoxButtons.OKCancel         ---&gt; DialogResult.Cancel;<para/>
		/// MessageBoxButtons.AbortRetryIgnore ---&gt; DialogResult.Ignore;<para/>
		/// MessageBoxButtons.YesNoCancel      ---&gt; DialogResult.Cancel;<para/>
		/// MessageBoxButtons.YesNo            ---&gt; not visible<para/>
		/// MessageBoxButtons.RetryCancel      ---&gt; DialogResult.Cancel;<para/></summary>
		public string NegativeBtnText
		{
			get { return m_btn_negative.Text; }
			set { m_btn_negative.Text = value; }
		}

		/// <summary>The result returned when the positive button is clicked, given the MessageBoxButtons value given at construction</summary>
		public DialogResult PositiveBtnResult
		{
			get { return m_btn_positive.DialogResult; }
		}

		/// <summary>The result returned when the neutral button is clicked, given the MessageBoxButtons value given at construction</summary>
		public DialogResult NeutralBtnResult
		{
			get { return m_btn_neutral.DialogResult; }
		}

		/// <summary>The result returned when the negative button is clicked, given the MessageBoxButtons value given at construction</summary>
		public DialogResult NegativeBtnResult
		{
			get { return m_btn_negative.DialogResult; }
		}

		/// <summary>Sets an appropriate size for the message box and lays out the controls</summary>
		public void UpdateLayout()
		{
			using (this.SuspendLayout(true))
			{
				const int text_margin = 27;
				const int button_margin = 12;
				const int btn_sep = 10;

				// Find the screen area to bound the message text
				var screen_area = (Owner != null ? Screen.FromControl(Owner) : Screen.PrimaryScreen).WorkingArea;
				screen_area.Inflate(-screen_area.Width / 8, -screen_area.Height / 8);

				// If the image is visible, reduce the available screen area
				var image_area = Size.Empty;
				var show_image = m_image.Image != null;
				if (show_image) // don't use m_image.Visible because it's not true until the dialog is displayed
				{
					screen_area.Inflate(-m_image.Width, 0);
					image_area = m_image.Size;
				}

				// Measure the text to be displayed
				// If it's larger than the screen area, limit the size but enable scroll bars
				var text_area = m_message.PreferredSize;
				if (Reflow && text_area.Area() != 0f && text_area.Aspect() > ReflowAspectRatio)
				{
					var scale = Math.Sqrt(ReflowAspectRatio / text_area.Aspect());
					m_message.MaximumSize = new Size((int)(text_area.Width * scale), 0);
					text_area = m_message.PreferredSize;
					m_message.MaximumSize = Size.Empty;
				}
				text_area.Width  = Math.Min(text_area.Width, screen_area.Width * 7 / 8);
				text_area.Height = Math.Min(text_area.Height, screen_area.Height * 7 / 8);

				// Get the bound area of the content
				var content_area = new Size(
					image_area.Width + text_area.Width,
					Math.Max(image_area.Height, text_area.Height));

				var btns = new[]{m_btn_positive, m_btn_neutral, m_btn_negative};
				var vis_btns = btns.Where(b => b.Text.HasValue());
				var num_btns = btns.Count(b => b.Text.HasValue());
			
				// Set button sizes
				var btn_h = vis_btns.Max(b => b.PreferredSize.Height);
				foreach (var b in vis_btns)
					b.Size = new Size(Math.Max(b.Width, b.PreferredSize.Width), btn_h);

				// Set the form size
				var nc_size = new Size(Bounds.Width - ClientSize.Width, Bounds.Height - ClientSize.Height);
				MinimumSize = new Size(
					nc_size.Width  + button_margin + vis_btns.Sum(b => b.Size.Width) + (num_btns-1)*btn_sep + button_margin,
					nc_size.Height + text_margin + image_area.Height + text_margin + button_margin + btn_h + button_margin);
				ClientSize = new Size(
					Maths.Clamp(text_margin + content_area.Width  + text_margin                                         , MinimumSize.Width - nc_size.Width, screen_area.Width),
					Maths.Clamp(text_margin + content_area.Height + text_margin + button_margin + btn_h + button_margin , MinimumSize.Height - nc_size.Height, screen_area.Height));

				int x = text_margin, y = text_margin;

				// Layout the background panel
				m_panel.Location = Point.Empty;
				m_panel.Size = new Size(ClientSize.Width, text_margin + content_area.Height + text_margin);

				// Layout icon
				m_image.Visible = show_image;
				if (show_image)
				{
					m_image.Location = new Point(x, y);
					x += m_image.Width;
				}

				// Layout the message text
				m_message.Location = new Point(x, y + (content_area.Height - text_area.Height) / 2);
				m_message.Size = new Size(Math.Max(text_area.Width, ClientSize.Width - 2*text_margin - image_area.Width), text_area.Height);

				// Layout buttons (from right to left)
				x = ClientSize.Width  - button_margin;
				y = ClientSize.Height - button_margin - btn_h;
				foreach (var btn in btns.Reversed())
				{
					var show = btn.Text.HasValue();
					if ((btn.Visible = show) == false) continue;
					btn.Location = new Point(x - btn.Width, y);
					x -= btn.Width + btn_sep;
				}
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
			this.m_btn_negative = new System.Windows.Forms.Button();
			this.m_btn_neutral = new System.Windows.Forms.Button();
			this.m_btn_positive = new System.Windows.Forms.Button();
			this.m_message = new pr.gui.RichTextBox();
			this.m_image = new System.Windows.Forms.PictureBox();
			this.m_panel = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.m_image)).BeginInit();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btn_negative
			// 
			this.m_btn_negative.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_negative.Location = new System.Drawing.Point(379, 98);
			this.m_btn_negative.Name = "m_btn_negative";
			this.m_btn_negative.Size = new System.Drawing.Size(88, 26);
			this.m_btn_negative.TabIndex = 2;
			this.m_btn_negative.Text = "Negative(3)";
			this.m_btn_negative.UseVisualStyleBackColor = true;
			// 
			// m_btn_neutral
			// 
			this.m_btn_neutral.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_neutral.Location = new System.Drawing.Point(285, 98);
			this.m_btn_neutral.Name = "m_btn_neutral";
			this.m_btn_neutral.Size = new System.Drawing.Size(88, 26);
			this.m_btn_neutral.TabIndex = 1;
			this.m_btn_neutral.Text = "Neutral(2)";
			this.m_btn_neutral.UseVisualStyleBackColor = true;
			// 
			// m_btn_positive
			// 
			this.m_btn_positive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_positive.Location = new System.Drawing.Point(191, 98);
			this.m_btn_positive.Name = "m_btn_positive";
			this.m_btn_positive.Size = new System.Drawing.Size(88, 26);
			this.m_btn_positive.TabIndex = 0;
			this.m_btn_positive.Text = "Positive(1)";
			this.m_btn_positive.UseVisualStyleBackColor = true;
			// 
			// m_message
			// 
			this.m_message.AcceptsTab = true;
			this.m_message.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_message.BackColor = System.Drawing.SystemColors.Window;
			this.m_message.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_message.CaretLocation = new System.Drawing.Point(0, 0);
			this.m_message.CurrentLineIndex = 0;
			this.m_message.Cursor = System.Windows.Forms.Cursors.Default;
			this.m_message.FirstVisibleLineIndex = 0;
			this.m_message.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.m_message.LineCount = 2;
			this.m_message.Location = new System.Drawing.Point(84, 30);
			this.m_message.Margin = new System.Windows.Forms.Padding(0);
			this.m_message.Name = "m_message";
			this.m_message.ReadOnly = true;
			this.m_message.Size = new System.Drawing.Size(368, 36);
			this.m_message.TabIndex = 3;
			this.m_message.Text = "The text of the message box\nOn multiple lines";
			// 
			// m_image
			// 
			this.m_image.Location = new System.Drawing.Point(30, 30);
			this.m_image.Name = "m_image";
			this.m_image.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
			this.m_image.Size = new System.Drawing.Size(26, 26);
			this.m_image.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.m_image.TabIndex = 4;
			this.m_image.TabStop = false;
			// 
			// m_panel
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BackColor = System.Drawing.SystemColors.Window;
			this.m_panel.Controls.Add(this.m_image);
			this.m_panel.Controls.Add(this.m_message);
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(478, 86);
			this.m_panel.TabIndex = 1;
			// 
			// MsgBox
			// 
			this.ClientSize = new System.Drawing.Size(479, 136);
			this.Controls.Add(this.m_panel);
			this.Controls.Add(this.m_btn_positive);
			this.Controls.Add(this.m_btn_neutral);
			this.Controls.Add(this.m_btn_negative);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(320, 160);
			this.Name = "MsgBox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			((System.ComponentModel.ISupportInitialize)(this.m_image)).EndInit();
			this.m_panel.ResumeLayout(false);
			this.m_panel.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestMsgBox
	{
		[Test] public void Test()
		{
			//var btns = MessageBoxButtons.OKCancel;
			//var icon = MessageBoxIcon.Question;
			//var line = string.Join(" "  , Enumerable.Range(0, 30).Select(x => "123456789"));
			//var msg = string.Join("\r\n", Enumerable.Range(0, 30).Select(x => line));
			//new MsgBox(msg, "Paul's", btns, icon).ShowDialog();
		}
	}
}
#endif