using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF
{
	public partial class MsgBox : Window, INotifyPropertyChanged
	{
		// Notes:
		//  - All buttons except 'No' and 'Cancel' set dialog result to 'true'
		//  - Use 'Result' when using non-trivial dialogs

		[Flags]
		public enum EButtons
		{
			None      = 0,
			OK        = 1 << 1,
			Cancel    = 1 << 2,
			Overwrite = 1 << 3,
			Discard   = 1 << 4,
			Abort     = 1 << 5,
			Retry     = 1 << 6,
			Ignore    = 1 << 7,
			Yes       = 1 << 8,
			No        = 1 << 9,
			Reload    = 1 << 10,

			YesNo                  = Yes | No,
			OKCancel               = OK | Cancel,
			RetryCancel            = Retry | Cancel,
			YesNoCancel            = Yes | No | Cancel,
			AbortRetryIgnore       = Abort | Retry | Ignore,
			OverwriteDiscardIgnore = Overwrite | Discard | Ignore,
			ReloadIgnore           = Reload | Ignore,
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
			None,
			OK,
			Cancel,
			Overwrite,
			Discard,
			Reload,
			Abort,
			Retry,
			Ignore,
			Yes,
			No,
		}

		/// <summary>Display a modal message box</summary>
		public static bool? Show(Window owner, string message, string? title = null, EButtons btns = EButtons.OK, EIcon icon = EIcon.None, EButtons default_button = EButtons.OK)
		{
			title ??= string.Empty;
			message ??= string.Empty;
			return new MsgBox(owner, message, title, btns, icon, default_button).ShowDialog();
		}

		public MsgBox()
			: this(null, string.Empty, string.Empty, EButtons.OK, EIcon.None, EButtons.OK)
		{ }
		public MsgBox(string message, string title)
			: this(null, message, title, EButtons.OK, EIcon.None, EButtons.OK)
		{ }
		public MsgBox(string message, string title, EButtons btns)
			: this(null, message, title, btns, EIcon.None, EButtons.OK)
		{ }
		public MsgBox(string message, string title, EButtons btns, EIcon icon)
			: this(null, message, title, btns, icon, EButtons.OK)
		{ }
		public MsgBox(Window? owner, string message, string title)
			: this(owner, message, title, EButtons.OK, EIcon.None, EButtons.OK)
		{ }
		public MsgBox(Window? owner, string message, string title, EButtons btns)
			: this(owner, message, title, btns, EIcon.None, EButtons.OK)
		{ }
		public MsgBox(Window? owner, string message, string title, EButtons btns, EIcon icon)
			: this(owner, message, title, btns, icon, EButtons.OK)
		{ }
		public MsgBox(Window? owner, string message, string title, EButtons btns, EIcon icon, EButtons default_button)
		{
			InitializeComponent();
			Owner = owner;

			Title = title;
			Icon = Owner?.Icon;
			Buttons = btns;
			DefaultButton = default_button;
			Message = message.LineEnding("\r\n");
			ShowAlwaysCheckbox = false;
			Result = null;
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

			Accept = Command.Create(this, AcceptInternal);
			Cancel = Command.Create(this, CancelInternal);

			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			if (Result == null && DialogResult != null)
				Result = DialogResult == true ? EResult.OK : EResult.Cancel;

			base.OnClosed(e);
		}

		/// <summary>The result set by the selected button</summary>
		public EResult? Result { get; set; }

		/// <summary>The buttons to display on this message box</summary>
		public EButtons Buttons
		{
			get { return (EButtons)GetValue(ButtonsProperty); }
			set { SetValue(ButtonsProperty, value); }
		}
		public static readonly DependencyProperty ButtonsProperty = Gui_.DPRegister<MsgBox>(nameof(Buttons));

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

		/// <summary>True if the always options is selected</summary>
		public bool Always
		{
			get { return (bool)GetValue(AlwaysProperty); }
			set { SetValue(AlwaysProperty, value); }
		}
		public static readonly DependencyProperty AlwaysProperty = Gui_.DPRegister<MsgBox>(nameof(Always));

		/// <summary>Access to the dialog buttons</summary>
		public StackPanel ButtonPanel => m_btn_panel;

		/// <summary>Access the panel that contains the message</summary>
		public Grid ContentPanel => m_content;

		/// <summary>The default button</summary>
		public EButtons DefaultButton
		{
			get => m_default_button;
			set
			{
				if (m_default_button == value) return;
				m_default_button = value;
				NotifyPropertyChanged(nameof(DefaultButton));
			}
		}
		private EButtons m_default_button;

		/// <summary>Make the "always" checkbox vislble</summary>
		public bool ShowAlwaysCheckbox
		{
			get => m_show_always_checkbox;
			set
			{
				if (m_show_always_checkbox == value) return;
				m_show_always_checkbox = value;
				NotifyPropertyChanged(nameof(ShowAlwaysCheckbox));
			}
		}
		private bool m_show_always_checkbox;

		/// <summary>Positive result</summary>
		public Command Accept { get; }
		private void AcceptInternal(object? x)
		{
			if (x is EResult result)
				Result = result;

			DialogResult = true;
			Close();
		}

		/// <summary>Negative result</summary>
		public Command Cancel { get; }
		private void CancelInternal(object? x)
		{
			if (x is EResult result)
				Result = result;

			DialogResult = false;
			Close();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
