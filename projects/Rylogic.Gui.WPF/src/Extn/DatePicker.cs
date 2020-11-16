using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Rylogic.Gui.WPF
{
	public static class DatePicker_
	{
		/// <summary>Hide the textbox, just displaying the calender icon</summary>
		public const int HideTextBox = 0;
		public static readonly DependencyProperty HideTextBoxProperty = Gui_.DPRegisterAttached(typeof(DatePicker_), nameof(HideTextBox));
		public static bool GetHideTextBox(DependencyObject obj) => (bool)obj.GetValue(HideTextBoxProperty);
		public static void SetHideTextBox(DependencyObject obj, bool value) => obj.SetValue(HideTextBoxProperty, value);
		private static void HideTextBox_Changed(DependencyObject obj)
		{
			if (obj is DatePicker dp)
			{
				dp.Loaded -= OnLoaded;
				dp.Loaded += OnLoaded;
				void OnLoaded(object sender, RoutedEventArgs e)
				{
					var tb = Gui_.FindVisualChild<DatePickerTextBox>(obj);
					if (tb != null) tb.Visibility = Visibility.Collapsed;
				}
			}
		}

		/// <summary>Change the watermark content</summary>
		public const int Watermark = 0;
		public static readonly DependencyProperty WatermarkProperty = Gui_.DPRegisterAttached(typeof(DatePicker_), nameof(Watermark));
		public static string GetWatermark(DependencyObject obj) => (string)obj.GetValue(WatermarkProperty);
		public static void SetWatermark(DependencyObject obj, string value) => obj.SetValue(WatermarkProperty, value);
		private static void Watermark_Changed(DependencyObject obj)
		{
			// The water mark is the text displayed before a date has been selected
			if (obj is DatePicker dp)
			{
				dp.Loaded -= ChangeWatermark;
				dp.Loaded += ChangeWatermark;
				void ChangeWatermark(object sender, RoutedEventArgs e)
				{
					var tb = Gui_.FindVisualChild<DatePickerTextBox>(obj);
					if (tb != null)
						tb.Text = GetWatermark(obj);
				}
			}
		}
	}
}
