using System;
using System.Windows;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ColourPickerUI : Window
	{
		public ColourPickerUI(Window? owner = null, Colour32? initial_colour = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;
			Colour = InitialColour = initial_colour ?? Colour32.White;
			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>The initial colour (if provided)</summary>
		public Colour32 InitialColour { get; }

		/// <summary>The colour selected in the dialog</summary>
		public Colour32 Colour
		{
			get => (Colour32)GetValue(ColourProperty);
			set => SetValue(ColourProperty, value);
		}
		private void Colour_Changed()
		{
			Suppress.Unused(Colour_Changed);
			ColorChanged?.Invoke(this, new ColourWheel.ColourEventArgs(Colour));
		}
		public static readonly DependencyProperty ColourProperty = Gui_.DPRegister<ColourPickerUI>(nameof(Colour), Colour32.White, Gui_.EDPFlags.TwoWay);

		/// <summary>Raised when the colour is changed</summary>
		public event EventHandler<ColourWheel.ColourEventArgs>? ColorChanged;

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
