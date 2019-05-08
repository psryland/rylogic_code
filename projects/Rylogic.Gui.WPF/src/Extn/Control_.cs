using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public class Control_
	{
		/// <summary>Foreground Attached property</summary>
		public const int Foreground = 0;
		public static readonly DependencyProperty ForegroundProperty = Gui_.DPRegisterAttached<Control_>(nameof(Foreground));
		public static SolidColorBrush GetForeground(DependencyObject obj) => (SolidColorBrush)obj.GetValue(ForegroundProperty);
		public static void SetForeground(DependencyObject obj, SolidColorBrush value) => obj.SetValue(ForegroundProperty, value);
		private static void Foreground_Changed(DependencyObject obj)
		{
			foreach (var child in Gui_.AllVisualChildren(obj).OfType<Control>())
				child.Foreground = GetForeground(obj);
		}

		/// <summary>HorizontalContentAlignment Attached Property</summary>
		public const int HorizontalContentAlignment = 0;
		public static readonly DependencyProperty HorizontalContentAlignmentProperty = Gui_.DPRegisterAttached<Control_>(nameof(HorizontalContentAlignment));
		public static HorizontalAlignment GetHorizontalContentAlignment(DependencyObject obj) => (HorizontalAlignment)obj.GetValue(HorizontalContentAlignmentProperty);
		public static void SetHorizontalContentAlignment(DependencyObject obj, SolidColorBrush value) => obj.SetValue(HorizontalContentAlignmentProperty, value);
		private static void HorizontalContentAlignment_Changed(DependencyObject obj)
		{
			foreach (var child in Gui_.AllVisualChildren(obj).OfType<Control>())
				child.HorizontalContentAlignment = GetHorizontalContentAlignment(obj);
		}

		/// <summary>VerticalContentAlignment Attached Property</summary>
		public const int VerticalContentAlignment = 0;
		public static readonly DependencyProperty VerticalContentAlignmentProperty = Gui_.DPRegisterAttached<Control_>(nameof(VerticalContentAlignment));
		public static VerticalAlignment GetVerticalContentAlignment(DependencyObject obj) => (VerticalAlignment)obj.GetValue(VerticalContentAlignmentProperty);
		public static void SetVerticalContentAlignment(DependencyObject obj, SolidColorBrush value) => obj.SetValue(VerticalContentAlignmentProperty, value);
		private static void VerticalContentAlignment_Changed(DependencyObject obj)
		{
			foreach (var child in Gui_.AllVisualChildren(obj).OfType<Control>())
				child.VerticalContentAlignment = GetVerticalContentAlignment(obj);
		}
	}
}
