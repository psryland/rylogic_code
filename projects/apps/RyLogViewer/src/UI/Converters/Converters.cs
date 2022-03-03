using System;
using System.Globalization;
using System.Windows.Data;

namespace RyLogViewer
{
	public class ByteToKByte : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not int kbytes) return 0;
			return (kbytes + Constants.OneKB - 1) / Constants.OneKB;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not int kbytes) return 0;
			return kbytes * Constants.OneKB;
		}
	}
	public class ByteToMByte : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not int bytes) return 0;
			return (bytes + Constants.OneMB - 1) / Constants.OneMB;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not int mbytes) return 0;
			return mbytes * Constants.OneMB;
		}
	}
	public class TabPageToInt : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not EOptionsPage opts) return 0;
			return (int)opts;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not int opts) return EOptionsPage.General;
			return (EOptionsPage)opts;
		}
	}
}