using System.Windows;
using System.Windows.Media;
using Rylogic.Gui.WPF;

namespace RyLogViewer
{
	/// <summary>Interaction logic for ColourPickerUI.xaml</summary>
	public partial class ColourPickerUI : Window
	{
		public ColourPickerUI()
		{
			InitializeComponent();

			m_root.DataContext = this;
			m_btn_ok.Click += (s, a) =>
			{
				DialogResult = true;
				Close();
			};
		}

		/// <summary></summary>
		public Color TextColour
		{
			get => (Color)GetValue(TextColourProperty);
			set => SetValue(TextColourProperty, value);
		}
		public static readonly DependencyProperty TextColourProperty = Gui_.DPRegister<ColourPickerUI>(nameof(TextColour), Colors.Black, Gui_.EDPFlags.TwoWay);

		/// <summary></summary>
		public Color BackColour
		{
			get => (Color)GetValue(BackColourProperty);
			set => SetValue(BackColourProperty, value);
		}
		public static readonly DependencyProperty BackColourProperty = Gui_.DPRegister<ColourPickerUI>(nameof(BackColour), Colors.White, Gui_.EDPFlags.TwoWay);
	}
}
