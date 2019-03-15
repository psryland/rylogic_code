using System;
using System.Windows;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
    public partial class ColourPickerUI : Window
	{
		public ColourPickerUI()
		{
			InitializeComponent();
		}

		/// <summary>The colour selected in the dialog</summary>
		public Color Color
		{
			get { return m_colour_wheel.Colour; }
			set { m_colour_wheel.Colour = value; }
		}

		/// <summary>Raised when the colour is changed</summary>
		public event EventHandler<ColourWheel.ColourEventArgs> ColorChanged
		{
			add { m_colour_wheel.ColourChanged += value; }
			remove { m_colour_wheel.ColourChanged -= value; }
		}

		/// <summary>Handlers</summary>
		private void HandleOkButton(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
