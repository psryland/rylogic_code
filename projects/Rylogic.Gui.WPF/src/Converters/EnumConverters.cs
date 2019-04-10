using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Rylogic.Attrib;

namespace Rylogic.Gui.WPF.Converters
{
	public class EnumToDesc : MarkupExtension, IValueConverter
	{
		// Notes:
		//  - Convert an enum to its description attribute
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is IEnumerable collection)
				return collection.Cast<object>().OfType<Enum>().Select(x => x.Desc());
			if (value is Enum en)
				return en.Desc();
			return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
	public class EnumValues : MarkupExtension, IValueConverter
	{
		// Use:
		//  <ComboBox
		//     ItemsSource="{Binding MyProp, Converter={conv:EnumValues}, Mode=OneTime}"
		//     ItemTemplate= "{conv:EnumValues+ToDesc}"
		//     SelectedItem="{Binding MyProp}"
		//     />
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				throw new ArgumentNullException();
			if (!value.GetType().IsEnum)
				throw new ArgumentException("Expected an enum property");

			var values = Enum.GetValues(value.GetType());
			return values;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

		/// <summary>Helper for mapping enum values to their description</summary>
		public class ToDesc : MarkupExtension
		{
			public override object ProvideValue(IServiceProvider serviceProvider)
			{
				var dt = new DataTemplate();
				dt.VisualTree = new FrameworkElementFactory(typeof(TextBlock));
				dt.VisualTree.SetValue(TextBlock.TextProperty, new Binding(".") { Converter = new EnumToDesc() });
				return dt;
			}
		}
	}
}
