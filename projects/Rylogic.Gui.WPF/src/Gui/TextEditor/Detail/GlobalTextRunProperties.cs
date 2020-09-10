using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Rylogic.Extn.Windows;

namespace Rylogic.Gui.WPF.TextEditor
{
	public sealed class GlobalTextRunProperties :TextRunProperties
	{
		public GlobalTextRunProperties(FrameworkElement fe)
		{
			m_typeface = fe.CreateTypeface();
			m_font_rendering_emsize = fe.FontSize();
			m_foreground = (Brush)fe.GetValue(Control.ForegroundProperty);
			m_background = (Brush)fe.GetValue(Control.BackgroundProperty);
			m_culture_info = System.Globalization.CultureInfo.CurrentCulture;
			Drawing_.CheckIsFrozen(m_foreground);
		}

		/// <inheritdoc/>
		public override Typeface Typeface => m_typeface;
		private Typeface m_typeface;

		/// <inheritdoc/>
		public override double FontRenderingEmSize => m_font_rendering_emsize;

		/// <inheritdoc/>
		public override double FontHintingEmSize => m_font_rendering_emsize;
		private double m_font_rendering_emsize;

		/// <inheritdoc/>
		public override Brush ForegroundBrush => m_foreground;
		private Brush m_foreground;

		/// <inheritdoc/>
		public override Brush BackgroundBrush => m_background;
		private Brush m_background;

		/// <inheritdoc/>
		public override System.Globalization.CultureInfo CultureInfo => m_culture_info;
		private System.Globalization.CultureInfo m_culture_info;

		/// <inheritdoc/>
		public override TextEffectCollection? TextEffects => null;

		/// <inheritdoc/>
		public override TextDecorationCollection? TextDecorations => null;
	}
}
