using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RyLogViewer
{
	public class LogGrid :DataGrid
	{
		public LogGrid()
		{
			EnableRowVirtualization = true;
			EnableColumnVirtualization = true;
			HeadersVisibility = DataGridHeadersVisibility.None;
		}
		protected override void OnLoadingRow(DataGridRowEventArgs e)
		{
			base.OnLoadingRow(e);



			//#region IValueConverter
			//public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			//{
			//	var index = (int)parameter;
			//	return Source[index];
			//}
			//public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			//{
			//	throw new NotImplementedException();
			//}
			//#endregion

		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
		}

		/// <summary>The data source for the grid</summary>
		public IReadOnlyList<ILine> Source
		{
			get { return ItemsSource as IReadOnlyList<ILine>; }
			set
			{
				if (Source == value) return;
				ItemsSource = value;
			}
		}

		/// <summary>Get/Set the number of displayed columns</summary>
		public int ColumnCount
		{
			get { return Columns.Count; }
			set
			{
				Columns.Clear();
				for (var i = 0; i != value; ++i)
				{
					Columns.Add(new DataGridTextColumn
					{
						Header = $"Column {i}",
						Binding = new Binding()
						{
							Source = Source,
							//Converter = this,
							//ConverterParameter = i,
							Mode = BindingMode.OneWay,
						},
					});
				}
				HeadersVisibility = Columns.Count <= 1
					? DataGridHeadersVisibility.Column
					: DataGridHeadersVisibility.None;
			}
		}
	}
}
