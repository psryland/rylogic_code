using System;
using System.Windows;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public partial class ColourPickerUI : Window
	{
		static ColourPickerUI()
		{
			ColourProperty = Gui_.DPRegister<ColourPickerUI>(nameof(Colour));
		}
		public ColourPickerUI()
		{
			InitializeComponent();
			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>The colour selected in the dialog</summary>
		public Colour32 Colour
		{
			get { return (Colour32)GetValue(ColourProperty); }
			set { SetValue(ColourProperty, value); }
		}
		private void Colour_Changed()
		{
			ColorChanged?.Invoke(this, new ColourWheel.ColourEventArgs(Colour));
		}
		public static readonly DependencyProperty ColourProperty;

		/// <summary>Raised when the colour is changed</summary>
		public event EventHandler<ColourWheel.ColourEventArgs> ColorChanged;

		/// <summary>Accept button</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			if (this.IsModal())
				DialogResult = true;

			Close();
		}
	}
}
