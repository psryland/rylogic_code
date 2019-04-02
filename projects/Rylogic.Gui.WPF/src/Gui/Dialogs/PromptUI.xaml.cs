using System;
using System.Windows;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public partial class PromptUI : Window
	{
		static PromptUI()
		{
			ImageProperty = Gui_.DPRegister<PromptUI>(nameof(Image));
			PromptProperty = Gui_.DPRegister<PromptUI>(nameof(Prompt));
			ValueProperty = Gui_.DPRegister<PromptUI>(nameof(Value));
			WrapProperty = Gui_.DPRegister<PromptUI>(nameof(Wrap));
			ReadOnlyProperty = Gui_.DPRegister<PromptUI>(nameof(ReadOnly));
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
		public static readonly DependencyProperty ValueProperty;

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
	}
}
