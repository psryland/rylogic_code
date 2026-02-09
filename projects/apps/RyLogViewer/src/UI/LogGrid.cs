using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Common;
using RyLogViewer.Options;

namespace RyLogViewer
{
	public class LogGrid :DataGrid
	{
		private readonly RowConverter m_conv;
		public LogGrid()
		{
			m_conv = new RowConverter(this);
			AutoGenerateColumns = false;
			EnableRowVirtualization = true;
			EnableColumnVirtualization = true;
			GridLinesVisibility = DataGridGridLinesVisibility.None;
			HeadersVisibility = DataGridHeadersVisibility.None;
		}
		protected override void OnLoadingRow(DataGridRowEventArgs e)
		{
			base.OnLoadingRow(e);
		}
		protected override void OnLoadingRowDetails(DataGridRowDetailsEventArgs e)
		{
			base.OnLoadingRowDetails(e);
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
		}

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.SettingChange -= HandleSettingChange;
				}
				field = value;
				if (field != null)
				{
					field.SettingChange += HandleSettingChange;

					// Apply settings
					ColumnCount = field.Format.ColumnCount;
				}

				// Handlers
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					if (e.Key == nameof(Format.ColumnCount) && e.Value is int column_count)
						ColumnCount = column_count;
				}
			}
		} = null!;

		/// <summary>The data source for the grid</summary>
		public IReadOnlyList<ILine> Source
		{
			get => (IReadOnlyList<ILine>)ItemsSource;
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
						Binding = new System.Windows.Data.Binding()
						{
							Source = Source,
							Converter = m_conv,
							ConverterParameter = i,
							Mode = BindingMode.OneWay,
						},
					});
				}
				HeadersVisibility = Columns.Count > 1
					? DataGridHeadersVisibility.Column
					: DataGridHeadersVisibility.None;
			}
		}

		/// <summary>Helper class for converting 'ILine' to cell values</summary>
		private class RowConverter : IValueConverter
		{
			private readonly LogGrid m_grid;
			public RowConverter(LogGrid grid)
			{
				m_grid = grid;
			}

			/// <summary>The data source for the grid</summary>
			public IReadOnlyList<ILine> Source => m_grid.Source;

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				var index = (int)parameter;
				return Source[0].Value(index);
				//	return Source[index];
				//return "Blah";
			}
			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}
		}
	}
}
