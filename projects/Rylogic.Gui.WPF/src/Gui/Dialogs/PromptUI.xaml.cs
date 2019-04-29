using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public partial class PromptUI : Window, INotifyPropertyChanged
	{
		static PromptUI()
		{
			ImageProperty = Gui_.DPRegister<PromptUI>(nameof(Image));
			PromptProperty = Gui_.DPRegister<PromptUI>(nameof(Prompt));
			ValueProperty = Gui_.DPRegister<PromptUI>(nameof(Value));
			UnitsProperty = Gui_.DPRegister<PromptUI>(nameof(Units));
			WrapProperty = Gui_.DPRegister<PromptUI>(nameof(Wrap));
			ReadOnlyProperty = Gui_.DPRegister<PromptUI>(nameof(ReadOnly));
			MultiLineProperty = Gui_.DPRegister<PromptUI>(nameof(MultiLine));
			ValueAlignmentProperty = Gui_.DPRegister<PromptUI>(nameof(ValueAlignment));
		}
		public PromptUI(Window owner = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = Owner?.Icon;
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
		public static readonly DependencyProperty ImageProperty;

		/// <summary>The prompt text</summary>
		public string Prompt
		{
			get { return (string)GetValue(PromptProperty); }
			set { SetValue(PromptProperty, value); }
		}
		public static readonly DependencyProperty PromptProperty;

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
		public static readonly DependencyProperty ValueProperty;

		/// <summary>A units string to display after the value text box</summary>
		public string Units
		{
			get { return (string)GetValue(UnitsProperty); }
			set { SetValue(UnitsProperty, value); }
		}
		public static readonly DependencyProperty UnitsProperty;

		/// <summary>True if the value text should wrap</summary>
		public bool Wrap
		{
			get { return (bool)GetValue(WrapProperty); }
			set { SetValue(WrapProperty, value); }
		}
		public static readonly DependencyProperty WrapProperty;

		/// <summary>True if the prompt is just displaying text</summary>
		public bool ReadOnly
		{
			get { return (bool)GetValue(ReadOnlyProperty); }
			set { SetValue(ReadOnlyProperty, value); }
		}
		public static readonly DependencyProperty ReadOnlyProperty;

		/// <summary>True if the value can contain multiple lines</summary>
		public bool MultiLine
		{
			get { return (bool)GetValue(MultiLineProperty); }
			set { SetValue(MultiLineProperty, value); }
		}
		public static readonly DependencyProperty MultiLineProperty;

		/// <summary>How text in the value box should be aligned</summary>
		public HorizontalAlignment ValueAlignment
		{
			get { return (HorizontalAlignment)GetValue(ValueAlignmentProperty); }
			set { SetValue(ValueAlignmentProperty, value); }
		}
		public static readonly DependencyProperty ValueAlignmentProperty;

		/// <summary>Validation function for allowed value text</summary>
		public Func<string, ValidationResult> Validate
		{
			get { return m_validate; }
			set
			{
				m_validate = value;
				Value_Changed();
			}
		}
		private Func<string, ValidationResult> m_validate;

		/// <summary>True if the user input is valid</summary>
		public bool IsValid
		{
			get { return m_is_valid; }
			private set
			{
				if (m_is_valid == value) return;
				m_is_valid = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
			}
		}
		private bool m_is_valid;

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
