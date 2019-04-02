using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Rylogic.Common;

namespace Rylogic.Gui.WPF.Converters
{
	/// <summary>Return just the filename of a full path</summary>
	public class FileName : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string path)) return value;
			if (!Path_.IsValidFilepath(path, false)) return value;
			return Path_.FileName(path);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Return a relative path based on some base path</summary>
	public class RelativePath : MarkupExtension, IMultiValueConverter
	{
		// Example Use:
		//	<TextBlock>
		//		<TextBlock.Text>
		//			<MultiBinding Converter="{conv:RelativePath}">
		//				<Binding Path="FullPath" />
		//				<Binding Path="BasePath" />
		//			</MultiBinding>
		//		</TextBlock.Text>
		//	</TextBlock>
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(values[0] is string full_path)) return null;
			if (!(values[1] is string base_path)) return null;
			return Path_.RelativePath(full_path, base_path);
		}
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
