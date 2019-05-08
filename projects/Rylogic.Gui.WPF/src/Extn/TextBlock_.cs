using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public class TextBlock_
	{
		/// <summary>Foreground Attached property</summary>
		public const int Foreground = 0;
		public static readonly DependencyProperty ForegroundProperty = Gui_.DPRegisterAttached<TextBlock_>(nameof(Foreground));
		public static SolidColorBrush GetForeground(DependencyObject obj) => (SolidColorBrush)obj.GetValue(ForegroundProperty);
		public static void SetForeground(DependencyObject obj, SolidColorBrush value) => obj.SetValue(ForegroundProperty, value);
		private static void Foreground_Changed(DependencyObject obj)
		{
			foreach (var child in Gui_.AllVisualChildren(obj).OfType<TextBlock>())
				child.Foreground = GetForeground(obj);
		}
	}
}
