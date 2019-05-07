using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public class Prop :DependencyObject
	{
		static Prop()
		{
			ForegroundProperty = Gui_.DPRegisterAttached<Prop>(nameof(Foreground), flags:FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);
		}

		/// <summary>Foreground</summary>
		public const int Foreground = 0;
		public static SolidColorBrush GetForeground(DependencyObject obj)
		{
			return (SolidColorBrush)obj.GetValue(ForegroundProperty);
		}
		public static void SetForeground(DependencyObject obj, SolidColorBrush value)
		{
			obj.SetValue(ForegroundProperty, value);
		}
		private static void Foreground_Changed(DependencyObject obj)
		{
			foreach (var child in Gui_.AllVisualChildren(obj))
			{
				switch (child)
				{
				case TextBlock block:
					block.Foreground = GetForeground(obj);
					break;
				case TextBoxBase tbb:
					tbb.Foreground = GetForeground(obj);
					break;
				}
			}
				
		}
		public static readonly DependencyProperty ForegroundProperty;
	}
}
