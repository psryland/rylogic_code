using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public partial class PromptUI : Window, INotifyPropertyChanged
	{
		// Notes:
		//  - This is used to get simple data from the user.
		//  - Use 'MsgBox' if you just want to display some info.

		/// <summary>Display a model prompt dialog</summary>
		public static bool? Show(Window? owner, ref string value, string? title = null, string? prompt = null)
		{
			title ??= string.Empty;
			prompt ??= string.Empty;
			var dlg = new PromptUI(owner)
			{
				Title = title,
				Prompt = prompt,
				Value = value,
			};
			var r = dlg.ShowDialog();
			if (r == true)
			{
				value = dlg.Value;
			}
			return r;
		}

		public PromptUI(Window? owner = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = Owner?.Icon;
			ShowWrapCheckbox = true;
			DataContext = this;
			m_field.Focus();

			m_btn_ok.Click += (s, a) =>
			{
				DialogResult = true;
				Close();
			};
		}

		/// <summary>Image source for the prompt icon</summary>
		public ImageSource Image
		{
			get { return (ImageSource)GetValue(ImageProperty); }
			set { SetValue(ImageProperty, value); }
		}
		public static readonly DependencyProperty ImageProperty = Gui_.DPRegister<PromptUI>(nameof(Image));

		/// <summary>The prompt text</summary>
		public string Prompt
		{
			get { return (string)GetValue(PromptProperty); }
			set { SetValue(PromptProperty, value); }
		}
		public static readonly DependencyProperty PromptProperty = Gui_.DPRegister<PromptUI>(nameof(Prompt));

		/// <summary>The value of the user text field</summary>
		public string Value
		{
			get { return (string)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}
		private void Value_Changed()
		{
			if (Validate != null)
			{
				var res = Validate(Value);
				IsValid = res.IsValid;
				m_field.ToolTip = res.ErrorContent;
			}
			else
			{
				IsValid = true;
				m_field.ToolTip = null;
			}
		}
		public static readonly DependencyProperty ValueProperty = Gui_.DPRegister<PromptUI>(nameof(Value));

		/// <summary>A units string to display after the value text box</summary>
		public string Units
		{
			get { return (string)GetValue(UnitsProperty); }
			set { SetValue(UnitsProperty, value); }
		}
		public static readonly DependencyProperty UnitsProperty = Gui_.DPRegister<PromptUI>(nameof(Units));

		/// <summary>True if the value text should wrap</summary>
		public bool Wrap
		{
			get { return (bool)GetValue(WrapProperty); }
			set { SetValue(WrapProperty, value); }
		}
		public static readonly DependencyProperty WrapProperty = Gui_.DPRegister<PromptUI>(nameof(Wrap));

		/// <summary>True if the prompt is just displaying text</summary>
		public bool ReadOnly
		{
			get { return (bool)GetValue(ReadOnlyProperty); }
			set { SetValue(ReadOnlyProperty, value); }
		}
		public static readonly DependencyProperty ReadOnlyProperty = Gui_.DPRegister<PromptUI>(nameof(ReadOnly));

		/// <summary>True if the value can contain multiple lines</summary>
		public bool MultiLine
		{
			get { return (bool)GetValue(MultiLineProperty); }
			set { SetValue(MultiLineProperty, value); }
		}
		public static readonly DependencyProperty MultiLineProperty = Gui_.DPRegister<PromptUI>(nameof(MultiLine));

		/// <summary>How text in the value box should be aligned</summary>
		public HorizontalAlignment ValueAlignment
		{
			get { return (HorizontalAlignment)GetValue(ValueAlignmentProperty); }
			set { SetValue(ValueAlignmentProperty, value); }
		}
		public static readonly DependencyProperty ValueAlignmentProperty = Gui_.DPRegister<PromptUI>(nameof(ValueAlignment));

		/// <summary>Validation function for allowed value text</summary>
		public Func<string, ValidationResult>? Validate
		{
			get => m_validate;
			set
			{
				m_validate = value;
				Value_Changed();
			}
		}
		private Func<string, ValidationResult>? m_validate;

		/// <summary>True if the user input is valid</summary>
		public bool IsValid
		{
			get => m_is_valid;
			private set
			{
				if (m_is_valid == value) return;
				m_is_valid = value;
				NotifyPropertyChanged(nameof(IsValid));
			}
		}
		private bool m_is_valid;

		/// <summary>Make the "wrap" checkbox vislble</summary>
		public bool ShowWrapCheckbox
		{
			get => m_show_wrap_checkbox;
			set
			{
				if (m_show_wrap_checkbox == value) return;
				m_show_wrap_checkbox = value;
				NotifyPropertyChanged(nameof(ShowWrapCheckbox));
			}
		}
		private bool m_show_wrap_checkbox;

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
