using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF
{
	/// <summary>
	/// Interaction logic for MsgBox.xaml
	/// </summary>
	public partial class MsgBox : Window
	{
		[Flags]
		public enum EButtons
		{
			None             = 0,
			OK               = (1 << EButton.OK),
			OKCancel         = (1 << EButton.OK) | (1 << EButton.Cancel),
			AbortRetryIgnore = (1 << EButton.Abort) | (1 << EButton.Retry) | (1 << EButton.Ignore),
			YesNoCancel      = (1 << EButton.Yes) | (1 << EButton.No) | (1 << EButton.Cancel),
			YesNo            = (1 << EButton.Yes) | (1 << EButton.No),
			RetryCancel      = (1 << EButton.Retry) | (1 << EButton.Cancel),
		}
		public enum EButton
		{
			None   = 0,
			OK     = 1,
			Cancel = 2,
			Abort  = 3,
			Retry  = 4,
			Ignore = 5,
			Yes    = 6,
			No     = 7,
		}
		public enum EIcon
		{
			None,
			Error,
			Question,
			Exclamation,
			Asterisk,
			Information,
		}
		public enum EResult
		{
			None   = 0,
			OK     = 1,
			Cancel = 2,
			Abort  = 3,
			Retry  = 4,
			Ignore = 5,
			Yes    = 6,
			No     = 7,
		}

		/// <summary>Display a modal message box</summary>
		public static bool? Show(Window owner, string message, string? title = null, EButtons btns = EButtons.OK, EIcon icon = EIcon.None, EButton default_button = EButton.OK)
		{
			title ??= string.Empty;
			message ??= string.Empty;
			return new MsgBox(owner, message, title, btns, icon, default_button).ShowDialog();
		}

		public MsgBox()
			: this(null, string.Empty, string.Empty, EButtons.OK, EIcon.None, EButton.OK)
		{ }
		public MsgBox(string message, string title)
			: this(null, message, title, EButtons.OK, EIcon.None, EButton.OK)
		{ }
		public MsgBox(string message, string title, EButtons btns)
			: this(null, message, title, btns, EIcon.None, EButton.OK)
		{ }
		public MsgBox(string message, string title, EButtons btns, EIcon icon)
			: this(null, message, title, btns, icon, EButton.OK)
		{ }
		public MsgBox(Window? owner, string message, string title)
			: this(owner, message, title, EButtons.OK, EIcon.None, EButton.OK)
		{ }
		public MsgBox(Window? owner, string message, string title, EButtons btns)
			: this(owner, message, title, btns, EIcon.None, EButton.OK)
		{ }
		public MsgBox(Window? owner, string message, string title, EButtons btns, EIcon icon)
			: this(owner, message, title, btns, icon, EButton.OK)
		{ }
		public MsgBox(Window? owner, string message, string title, EButtons btns, EIcon icon, EButton default_button)
		{
			InitializeComponent();
			Owner = owner;

			Title = title;
			Icon = Owner?.Icon;
			Message = message.LineEnding("\r\n");

			m_btn_positive.Click += on_clicked;
			m_btn_neutral.Click += on_clicked;
			m_btn_negative.Click += on_clicked;
			void on_clicked(object s, EventArgs a)
			{
				DialogResult =
					ReferenceEquals(s, m_btn_positive) ? true :
					ReferenceEquals(s, m_btn_negative) ? false :
					(bool?)null;
				Close();
			}

			switch (btns)
			{
			default: throw new ArgumentOutOfRangeException(nameof(btns));
			case EButtons.OK:
				m_btn_positive.Content = "_OK";
				m_btn_positive.IsDefault = default_button == EButton.OK;
				m_btn_positive.Visibility = Visibility.Visible;
				m_btn_neutral.Visibility = Visibility.Collapsed;
				m_btn_negative.Visibility = Visibility.Collapsed;
				break;
			case EButtons.OKCancel:
				m_btn_positive.Content = "_OK";
				m_btn_positive.IsDefault = default_button == EButton.OK;
				m_btn_negative.Content = "_Cancel";
				m_btn_negative.IsDefault = default_button == EButton.Cancel;
				m_btn_positive.Visibility = Visibility.Visible;
				m_btn_neutral.Visibility = Visibility.Collapsed;
				m_btn_negative.Visibility = Visibility.Visible;
				break;
			case EButtons.AbortRetryIgnore:
				m_btn_positive.Content = "_Abort";
				m_btn_positive.IsDefault = default_button == EButton.Abort;
				m_btn_neutral.Content = "_Retry";
				m_btn_neutral.IsDefault = default_button == EButton.Retry;
				m_btn_negative.Content = "_Ignore";
				m_btn_negative.IsDefault = default_button == EButton.Ignore;
				m_btn_positive.Visibility = Visibility.Visible;
				m_btn_neutral.Visibility = Visibility.Visible;
				m_btn_negative.Visibility = Visibility.Visible;
				break;
			case EButtons.YesNoCancel:
				m_btn_positive.Content = "_Yes";
				m_btn_positive.IsDefault = default_button == EButton.Yes;
				m_btn_neutral.Content = "_No";
				m_btn_neutral.IsDefault = default_button == EButton.No;
				m_btn_negative.Content = "_Cancel";
				m_btn_negative.IsDefault = default_button == EButton.Cancel;
				m_btn_positive.Visibility = Visibility.Visible;
				m_btn_neutral.Visibility = Visibility.Visible;
				m_btn_negative.Visibility = Visibility.Visible;
				break;
			case EButtons.YesNo:
				m_btn_positive.Content = "_Yes";
				m_btn_positive.IsDefault = default_button == EButton.Yes;
				m_btn_neutral.Content = "_No";
				m_btn_neutral.IsDefault = default_button == EButton.No;
				m_btn_positive.Visibility = Visibility.Visible;
				m_btn_neutral.Visibility = Visibility.Visible;
				m_btn_negative.Visibility = Visibility.Collapsed;
				break;
			case EButtons.RetryCancel:
				m_btn_positive.Content = "_Retry";
				m_btn_positive.IsDefault = default_button == EButton.Retry;
				m_btn_negative.Content = "_Cancel";
				m_btn_negative.IsDefault = default_button == EButton.Cancel;
				m_btn_positive.Visibility = Visibility.Visible;
				m_btn_neutral.Visibility = Visibility.Collapsed;
				m_btn_negative.Visibility = Visibility.Visible;
				break;
			}
			Image = icon switch
			{
				EIcon.None => null,
				EIcon.Error => System.Drawing.SystemIcons.Error.ToBitmapSource(),
				EIcon.Question => System.Drawing.SystemIcons.Question.ToBitmapSource(),
				EIcon.Exclamation => System.Drawing.SystemIcons.Exclamation.ToBitmapSource(),
				EIcon.Asterisk => System.Drawing.SystemIcons.Asterisk.ToBitmapSource(),
				EIcon.Information => System.Drawing.SystemIcons.Information.ToBitmapSource(),
				_ => throw new ArgumentOutOfRangeException(nameof(icon)),
			};
			DataContext = this;
		}

		/// <summary>The image to show to the left of the message</summary>
		public ImageSource? Image
		{
			get { return (ImageSource?)GetValue(ImageProperty); }
			set { SetValue(ImageProperty, value); }
		}
		public static readonly DependencyProperty ImageProperty = Gui_.DPRegister<MsgBox>(nameof(Image));

		/// <summary>The message to display</summary>
		public string Message
		{
			get { return (string)GetValue(MessageProperty); }
			set { SetValue(MessageProperty, value); }
		}
		public static readonly DependencyProperty MessageProperty = Gui_.DPRegister<MsgBox>(nameof(Message));

		/// <summary>The positive button control</summary>
		public Button PositiveBtn => m_btn_positive;

		/// <summary>The neutral button control</summary>
		public Button NeutralBtn => m_btn_neutral;

		/// <summary>The negative button control</summary>
		public Button NegativeBtn => m_btn_negative;

		/// <summary>Get/Set the text on the positive button</summary>
		public string PositiveBtnText
		{
			get { return (string)GetValue(PositiveBtnTextProperty); }
			set { SetValue(PositiveBtnTextProperty, value); }
		}
		public static readonly DependencyProperty PositiveBtnTextProperty = Gui_.DPRegister<MsgBox>(nameof(PositiveBtnText));

		/// <summary>Get/Set the text on the neutral button.</summary>
		public string NeutralBtnText
		{
			get { return (string)GetValue(NeutralBtnTextProperty); }
			set { SetValue(NeutralBtnTextProperty, value); }
		}
		public static readonly DependencyProperty NeutralBtnTextProperty = Gui_.DPRegister<MsgBox>(nameof(NeutralBtnText));

		/// <summary>Get/Set the text on the negative button.</summary>
		public string NegativeBtnText
		{
			get { return (string)GetValue(NegativeBtnTextProperty); }
			set { SetValue(NegativeBtnTextProperty, value); }
		}
		public static readonly DependencyProperty NegativeBtnTextProperty = Gui_.DPRegister<MsgBox>(nameof(NegativeBtnText));
	}
}

///// <summary>Position the controls within the dialog</summary>
//public void InitialLayout()
//{
//	using (this.SuspendLayout(true))
//	using (var gfx = CreateGraphics())
//	{
//		// Get the scaling due to DPI
//		var scale_x = gfx.DpiX / 96f;
//		var scale_y = gfx.DpiY / 96f;
//		var x_10px = (int)(10 * scale_x);
//		var y_10px = (int)(10 * scale_y);
//		var x_30px = (int)(30 * scale_x);
//		var y_30px = (int)(30 * scale_y);
//		var dlg_size = MinimumSize;

//		// Position, show, hide, resize the buttons
//		{
//			var btns = new[] { m_btn_positive, m_btn_neutral, m_btn_negative };
//			var vis_btns = btns.Where(b => b.Text.HasValue());
//			var num_btns = btns.Count(b => b.Text.HasValue());

//			// Set button sizes
//			var btn_h = vis_btns.Max(b => b.PreferredSize.Height);
//			foreach (var b in vis_btns)
//				b.Size = new Size(Math.Max(b.Width, b.PreferredSize.Width), btn_h);

//			// Resize the buttons panel
//			m_panel_btns.Height = btn_h * 2;

//			// Position the buttons
//			var x = m_panel_btns.ClientRectangle.Right - btn_h / 2;
//			var y = m_panel_btns.ClientRectangle.Top + btn_h / 2;
//			foreach (var btn in btns.Reversed())
//			{
//				btn.Visible = btn.Text.HasValue();
//				if (!btn.Visible) continue;
//				btn.Location = new Point(x - btn.Width, y);
//				x -= btn.Width + btn_h / 2;
//			}

//			// Measure the distance from the buttons panel to the dialog edges
//			var btns_srect = m_panel_btns.RectangleToScreen(m_panel_btns.ClientRectangle);
//			var dist = new int[]
//			{
//				btns_srect.Left   - Bounds.Left,
//				btns_srect.Top    - Bounds.Top,
//				btns_srect.Right  - Bounds.Right,
//				btns_srect.Bottom - Bounds.Bottom,
//			};

//			// Set the minimum size for the dialog required by the buttons panel
//			dlg_size.Width = Math.Max(dlg_size.Width, m_panel_btns.ClientRectangle.Right - x + dist[0] - dist[2]);
//			dlg_size.Height = Math.Max(dlg_size.Height, m_panel_btns.Height);
//		}

//		// Show the image if set
//		{
//			var image_visible = m_image.Image != null;
//			m_image.Visible = image_visible;
//			m_image.Location = new Point(x_30px, y_30px);

//			// Position the message adjacent to the image
//			m_message.Left += image_visible ? m_image.Width + x_10px : 0;
//			m_message.Width -= image_visible ? m_image.Width + x_10px : 0;
//		}

//		// Position, resize the text box and set the window size
//		{
//			// Measure the text to be displayed
//			var text_area = gfx.MeasureString(m_message.Text, m_message.Font);

//			// Re-flow the text if the aspect ratio is too large
//			if (Reflow && text_area.Area() != 0f && text_area.Aspect() > ReflowAspectRatio)
//			{
//				// Binary search for an aspect ratio ~= ReflowAspectRatio
//				var initial_width = text_area.Width;
//				for (float scale0 = 0.0f, scale1 = 1.0f; ;)
//				{
//					var scale = (scale0 + scale1) / 2f;
//					text_area = gfx.MeasureString(m_message.Text, m_message.Font, (int)(initial_width * scale));
//					var aspect = text_area.Aspect();
//					if (aspect < ReflowAspectRatio) scale0 = scale;
//					else if (aspect > ReflowAspectRatio) scale1 = scale;
//					if (Math.Abs(scale1 - scale0) < 0.05f)
//					{
//						text_area = gfx.MeasureString(m_message.Text, m_message.Font, (int)(initial_width * scale1));
//						break;
//					}
//				}
//			}

//			// If it's larger than the screen area, limit the size
//			// Find the screen area to limit how big we go
//			var screen_area = (Owner != null ? Screen.FromControl(Owner) : Screen.PrimaryScreen).WorkingArea;
//			screen_area = screen_area.Inflated(-screen_area.Width / 4, -screen_area.Height / 4);
//			text_area.Width = Math.Min(text_area.Width, screen_area.Width);
//			text_area.Height = Math.Min(text_area.Height, screen_area.Height);

//			// Measure the distance from the message to the dialog edges
//			var msg_srect = m_message.RectangleToScreen(m_message.ClientRectangle);
//			var dist = new int[]
//			{
//				msg_srect.Left   - Bounds.Left,
//				msg_srect.Top    - Bounds.Top,
//				msg_srect.Right  - Bounds.Right,
//				msg_srect.Bottom - Bounds.Bottom - m_message.Font.Height,
//			};

//			// Set the minimum size for the dialog required by the text area
//			dlg_size.Width = Math.Max(dlg_size.Width, (int)(text_area.Width + dist[0] - dist[2]));
//			dlg_size.Height = Math.Max(dlg_size.Height, (int)(text_area.Height + dist[1] - dist[3]));
//		}

//		// Set the size of the dialog
//		Size = dlg_size;
//	}
//}
///// <summary>Set to true to have the dialog automatically line wrap text. False to honour message new lines</summary>
//public bool Reflow
//{
//	get { return m_reflow; }
//	set { m_reflow = value; }
//}
//private bool m_reflow;

///// <summary>The ratio of width to height used to decide where to wrap text</summary>
//public float ReflowAspectRatio
//{
//	get { return m_reflow_aspect; }
//	set { m_reflow_aspect = value; }
//}
//private float m_reflow_aspect;

///// <summary>The result returned when the positive button is clicked, given the MessageBoxButtons value given at construction</summary>
//public DialogResult PositiveBtnResult
//{
//	get { return m_btn_positive.DialogResult; }
//}

///// <summary>The result returned when the neutral button is clicked, given the MessageBoxButtons value given at construction</summary>
//public DialogResult NeutralBtnResult
//{
//	get { return m_btn_neutral.DialogResult; }
//}

///// <summary>The result returned when the negative button is clicked, given the MessageBoxButtons value given at construction</summary>
//public DialogResult NegativeBtnResult
//{
//	get { return m_btn_negative.DialogResult; }
//}
///// <summary>The message body in RTF</summary>
//public string MessageRtf
//{
//	get { return m_message.Rtf; }
//	set { m_message.Rtf = value; }
//}

//			Reflow = true;
//			ReflowAspectRatio = DefaultReflowAspect;

//Owner = owner != null ? owner.TopLevelControl as Form : (Form)null;
//if (Owner != null)
//{
//	Icon = Owner.Icon;
//	ShowIcon =
//		Owner.ShowIcon &&
//		Owner.Icon != null &&
//		Owner.FormBorderStyle != FormBorderStyle.FixedToolWindow &&
//		Owner.FormBorderStyle != FormBorderStyle.SizableToolWindow;
//}
//else
//{
//	Icon = null;
//	ShowIcon = false;
//}

//Message.LinkClicked += (s, a) =>
//{
//	try { System.Diagnostics.Process.Start("explorer.exe", a.LinkText); }
//	catch (Exception ex)
//	{
//		MsgBox.Show(Owner, "Failed to navigate to link\r\nReason: " + ex.Message, "Link Failed", MessageBoxButtons.OK);
//	}
//};