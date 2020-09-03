using System.Windows;
using System.Windows.Media;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class TextStyle :Style
	{
		public static readonly TextStyle Default = new TextStyle();

		/// <summary>Foreground colour</summary>
		public Brush ForeColor { get; set; } = Brushes.Black;

		/// <summary>Background colour</summary>
		public Brush BackColor { get; set; } = Brushes.Transparent;

		/// <summary>Font style</summary>
		public Typeface Typeface { get; set; } = new Typeface(new FontFamily("Tahoma"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

		/// <summary>Font size</summary>
		public double EmSize { get; set; } = 12.0;

		/// <summary>Text flow direction</summary>
		public FlowDirection Flow { get; set; } = FlowDirection.LeftToRight;
		
		/// <summary>DIP scaling</summary>
		public double PixelsPerDIP { get; set; } = 1.0;
	}
}