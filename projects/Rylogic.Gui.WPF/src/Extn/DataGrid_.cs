using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF
{
	public static class DataGrid_
	{
		public struct GridAddressData
		{
			public int CellIndex;
			public int RowIndex;
		}

		/// <summary>Returns the cell that contains 'dp' (or null)</summary>
		public static DataGridCell? FindCell(DependencyObject dp)
		{
			return Gui_.FindVisualParent<DataGridCell>(dp);
		}

		/// <summary>Returns the row that contains this cell</summary>
		public static DataGridRow GetRow(this DataGridCell cell)
		{
			return Gui_.FindVisualParent<DataGridRow>(cell) ?? throw new Exception("Cell does not have an associated row");
		}

		/// <summary>Returns the cell and column index of this cell</summary>
		public static GridAddressData GridAddress(this DataGridCell cell)
		{
			return new GridAddressData
			{
				CellIndex = cell.Column.DisplayIndex,
				RowIndex = cell.GetRow().GetIndex(),
			};
		}

		/// <summary>Parse a string representation of a DataGridLength</summary>
		public static DataGridLength ParseDataGridLength(string length)
		{
			if (length.ToLower() == "auto")
				return new DataGridLength(1.0, DataGridLengthUnitType.Auto);
			if (length.ToLower() == "sizetocells")
				return new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
			if (length.ToLower() == "sizetoheader")
				return new DataGridLength(1.0, DataGridLengthUnitType.SizeToHeader);
			if (length.ToLower() == "*")
				return new DataGridLength(1.0, DataGridLengthUnitType.Star);
			if (length.EndsWith("*"))
				return new DataGridLength(double.Parse(length.TrimEnd('*')), DataGridLengthUnitType.Star);
			if (double.TryParse(length, out var pixels))
				return new DataGridLength(pixels, DataGridLengthUnitType.Pixel);
			throw new Exception($"Failed to parse DataGridLength string: {length}");
		}

		#region Auto Column Size

		// Usage:
		//   In your xaml set 'DataGrid_.ColumnResizeMode' and 'NotifyOnTargetUpdated=True'
		//   in the column text binding on all columns that can change.
		//    e.g.
		//      <DataGrid
		//          local:DataGrid_.ColumnResizeMode="FitToDisplayWidth"
		//          ... />
		//          <DataGridTextColumn
		//              Header = "MyColumn"
		//              Binding="{Binding TextToShow, NotifyOnTargetUpdated=True}"
		//              />

		/// <summary>Column resizing schemes</summary>
		public enum EColumnResizeMode
		{
			None,
			FitToDisplayWidth,
		}

		/// <summary>Foreground Attached property</summary>
		public const int ColumnResizeMode = 0;
		public static readonly DependencyProperty ColumnResizeModeProperty = Gui_.DPRegisterAttached(typeof(DataGrid_), nameof(ColumnResizeMode));
		public static EColumnResizeMode GetColumnResizeMode(DependencyObject obj) => (EColumnResizeMode)obj.GetValue(ColumnResizeModeProperty);
		public static void SetColumnResizeMode(DependencyObject obj, EColumnResizeMode value) => obj.SetValue(ColumnResizeModeProperty, value);
		private static void ColumnResizeMode_Changed(DependencyObject obj)
		{
			if (!(obj is DataGrid grid))
				return;

			grid.SizeChanged -= ResizeColumns;
			grid.TargetUpdated -= ResizeColumns;
			if (GetColumnResizeMode(obj) != EColumnResizeMode.None)
			{
				grid.SizeChanged += ResizeColumns;
				grid.TargetUpdated += ResizeColumns;
			}

			// Handler
			static void ResizeColumns(object sender, EventArgs e)
			{
				var grid = (DataGrid)sender;
				var widths = grid.Columns.Select(x => x.Width).ToList();

				var total_width = grid.ActualWidth;
				if (double.IsNaN(total_width))
					return;

				foreach (var (col, i) in grid.Columns.WithIndex())
				{
					if (widths[i].UnitType == DataGridLengthUnitType.Pixel)
						col.Width = widths[i];
					else
						col.Width = new DataGridLength(widths[i].Value, DataGridLengthUnitType.Star);
				}

				grid.UpdateLayout();
			}
		}

		#endregion

		#region Column Visibility

		/// <summary>Display a context menu for showing/hiding columns in the grid (at 'location' relative to the grid).</summary>
		public static void ColumnVisibilityCMenu(this DataGrid grid, Action<DataGridColumn>? on_vis_changed = null)
		{
			var cmenu = new ContextMenu();
			foreach (var col in grid.Columns.Cast<DataGridColumn>())
			{
				var item = cmenu.Items.Add2(new MenuItem
				{
					Header = col.Header.ToString(),
					IsChecked = col.Visibility == Visibility.Visible,
					Tag = col
				});
				item.Click += (s, a) =>
				{
					var c = (DataGridColumn)item.Tag;
					c.Visibility = item.IsChecked ? Visibility.Collapsed : Visibility.Visible;
					on_vis_changed?.Invoke(c);
				};
			}
			cmenu.PlacementTarget = grid;
			cmenu.StaysOpen = true;
			cmenu.IsOpen = true;
		}

		/// <summary>Display a context menu for showing/hiding columns in the grid. Attach to 'MouseRightButtonUp'</summary>
		public static void ColumnVisibility(object sender, MouseButtonEventArgs args)
		{
			// Unfortunately, binding like this 'MouseRightButtonUp="{Binding DataGrid_.ColumnVisibility}"'
			// doesn't work. You'd need to provide an instance method that forwards to this call, so you
			// might as well just sign up the event handler in the constructor:
			//  m_grid.MouseRightButtonUp += DataGrid_.ColumnVisibility;
			if (args.ChangedButton != MouseButton.Right || args.RightButton != MouseButtonState.Released)
				return;

			// If this throws, check you've actually signed up to a DataGrid (i.e. not a Grid)
			var grid = (DataGrid)sender;
			var hit = (DependencyObject)args.OriginalSource;

			// Right mouse on a column header displays a context menu for hiding/showing columns
			var hit_header = Gui_.FindVisualParent<DataGridColumnHeader>(hit, root: grid) != null;
			if (!hit_header)
			{
				// The 'filler' header is not clickable (IsHitTestVisible is false)
				var filler = Gui_.FindVisualChild<DataGridColumnHeader>(grid, x => x.Name == "PART_FillerColumnHeader");
				hit_header = filler?.RenderArea().Contains(args.GetPosition(filler)) ?? false;
			}
			if (hit_header)
			{
				grid.ColumnVisibilityCMenu();
				args.Handled = true;
			}
		}

		#endregion

		#region Row Drag Drop

		// Usage:
		//  Add 'gui:DataGrid_.ReorderRowsWithDragDrop="True"' to your DataGrid.
		//   You may want 'CanUserSortColumns="False"' as well because sorting prevents
		//   the default behaviour of reordering the underlying collection.
		//  Alternatively, you can add a handler:
		//   gui:DataGrid_.ReorderRowDrop="HandleReorderRowDrop"
		//   private void HandleReorderRowDrop(object sender, DataGrid_.ReorderRowDropEventArgs e)
		//  You can then handle reordered with custom code.
		//  Use the 'RowsReordered' to get notification after a reorder has happened

		/// <summary>Foreground Attached property</summary>
		public const int ReorderRowsWithDragDrop = 0;
		public static readonly DependencyProperty ReorderRowsWithDragDropProperty = Gui_.DPRegisterAttached(typeof(DataGrid_), nameof(ReorderRowsWithDragDrop));
		public static bool GetReorderRowsWithDragDrop(DependencyObject obj) => (bool)obj.GetValue(ReorderRowsWithDragDropProperty);
		public static void SetReorderRowsWithDragDrop(DependencyObject obj, bool value) => obj.SetValue(ReorderRowsWithDragDropProperty, value);
		private static void ReorderRowsWithDragDrop_Changed(DependencyObject obj)
		{
			if (!(obj is DataGrid data_grid))
				return;

			var enable = GetReorderRowsWithDragDrop(obj);
			if (enable)
			{
				data_grid.AllowDrop = true;
				data_grid.PreviewMouseLeftButtonDown += HandleMouseDown;
				data_grid.DragEnter += HandleDragging;
				data_grid.DragOver += HandleDragging;
				data_grid.Drop += HandleDrop;
			}
			else
			{
				data_grid.Drop -= HandleDrop;
				data_grid.DragOver += HandleDragging;
				data_grid.DragEnter += HandleDragging;
				data_grid.PreviewMouseLeftButtonDown -= HandleMouseDown;
			}

			// Handlers
			static void HandleDrop(object? sender, DragEventArgs e)
			{
				// Must allow move
				if (!e.AllowedEffects.HasFlag(DragDropEffects.Move))
					return;

				// Ignore if not dropping a row
				var data = (ReorderRowsWithDragDropData)e.Data.GetData(typeof(ReorderRowsWithDragDropData));
				if (data == null)
					return;

				// Ignore if the mouse hasn't moved enough to start dragging
				if (!data.Dragging)
					return;

				// Don't allow drop if not over a row header
				var row_header = e.OriginalSource is DependencyObject src ? Gui_.FindVisualParent<DataGridRowHeader>(src, root: data.Grid) : null;
				if (row_header == null)
					return;

				// Must drop over a row
				var row = Gui_.FindVisualParent<DataGridRow>(row_header, root: data.Grid);
				if (row == null)
					return;

				// Get the index of the insert position
				var row_pt = e.GetPosition(row_header);
				var grab_index = data.Row.GetIndex();
				var drop_index = row.GetIndex() + (2 * row_pt.Y < row_header.ActualHeight ? 0 : 1);

				// Nothing to do if not reordering
				if (grab_index != drop_index)
				{
					// Reorder the items source.
					var args = new ReorderRowDropEventArgs(data.Grid, data.Row, drop_index);
					
					// Call the event to allow user code to handle it
					data.Grid.RaiseEvent(args);

					// Fallback to the default handling
					if (!args.Handled)
						ReorderRowsDropDefaultHandler(data.Grid, args);

					// Notify if reordering happened
					if (args.Handled)
						data.Grid.RaiseEvent(new RoutedEventArgs(RowsReorderedEvent, data.Grid));
				}

				e.Handled = true;
			}
			static void HandleDragging(object? sender, DragEventArgs e)
			{
				e.Effects = DragDropEffects.None;
				e.Handled = true;

				// Must allow move
				if (!e.AllowedEffects.HasFlag(DragDropEffects.Move))
					return;

				// Ignore if not dropping a row
				var data = (ReorderRowsWithDragDropData)e.Data.GetData(typeof(ReorderRowsWithDragDropData));
				if (data == null)
					return;

				// Don't allow drop if not over a row header
				var row_header = e.OriginalSource is DependencyObject src ? Gui_.FindVisualParent<DataGridRowHeader>(src, root: data.Grid) : null;
				if (row_header == null)
					return;

				// The current position in grid space
				var pt = e.GetPosition(data.Grid);

				// Check the mouse has moved enough to start dragging
				if (!data.Dragging)
				{
					// Not moved enough
					if ((pt - data.Grab).LengthSquared < 25) return;
					data.Dragging = true;
				}

				// Set 'Effect' to indicate whether drop is allowed
				e.Effects = DragDropEffects.Move;

				// Update the indicator position
				var row_pt = e.GetPosition(row_header);
				var scn_pt = row_header.PointToScreen(2 * row_pt.Y < row_header.ActualHeight ? new Point(0,0) : new Point(0,row_header.ActualHeight));
				data.PositionIndicator(scn_pt);
			}
			static void HandleMouseDown(object? sender, MouseButtonEventArgs e)
			{
				if (!(e.OriginalSource is DependencyObject src)) return;
				if (!(sender is DataGrid grid)) return;

				var row_header = Gui_.FindVisualParent<DataGridRowHeader>(src, root: grid);
				if (row_header != null && grid.Cursor != Cursors.SizeNS)
				{
					grid.Dispatcher.BeginInvoke(new Action(() =>
					{
						using var data = new ReorderRowsWithDragDropData(grid, e.GetPosition(grid));
						DragDrop.DoDragDrop(grid, data, DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link);
					}));
					e.Handled = true;
				}
			}
		}
		private sealed class ReorderRowsWithDragDropData :IDisposable
		{
			public ReorderRowsWithDragDropData(DataGrid grid, Point grab)
			{
				Grid = grid;
				Grab = grab;
				Row = (grid.InputHitTest(grab) as DependencyObject)?.FindVisualParent<DataGridRow>(root: grid) ?? throw new Exception("Dragged row not found");
				Indicator = new Popup
				{
					Placement = PlacementMode.AbsolutePoint,
					IsHitTestVisible = false,
					AllowsTransparency = true,
					StaysOpen = true,
				};
				Indicator.Child = new Path
				{
					Data = Geometry_.MakePolygon(true, new Point(0, 0), new Point(10, 4), new Point(0, 8)),
					Stroke = Brushes.DeepSkyBlue,
					Fill = Brushes.SkyBlue,
					StrokeThickness = 1,
					IsHitTestVisible = false,
				};
			}
			public void Dispose()
			{
				Indicator.IsOpen = false;
			}

			/// <summary>The source of the drag</summary>
			public DataGrid Grid { get; }

			/// <summary>The row being dragged</summary>
			public DataGridRow Row { get; }

			/// <summary>Dragging only starts if the mouse moves by more than a minimum distance</summary>
			public bool Dragging
			{
				get => Indicator.IsOpen;
				set => Indicator.IsOpen = value;
			}

			/// <summary>Grid relative mouse location at the start of the drag</summary>
			public Point Grab { get; }

			/// <summary>Graphics to show where the row will be inserted</summary>
			private Popup Indicator { get; }

			/// <summary>Position the insert indicator. 'pt' is in screen space</summary>
			public void PositionIndicator(Point pt)
			{
				var sz = Indicator.Child.DesiredSize;
				Indicator.HorizontalOffset = pt.X - sz.Width;
				Indicator.VerticalOffset = pt.Y - sz.Height / 2;
			}
		}
		public class ReorderRowDropEventArgs :RoutedEventArgs
		{
			public ReorderRowDropEventArgs(object sender, DataGridRow row, int insert_index)
				:base(ReorderRowDropEvent, sender)
			{
				Row = row;
				DropIndex = insert_index;
			}

			/// <summary>The row being dragged</summary>
			public DataGridRow Row { get; }

			/// <summary>The index of the row that was grabbed</summary>
			public int GrabIndex => Row.GetIndex();

			/// <summary>The index of where to insert the row</summary>
			public int DropIndex { get; }
		}

		/// <summary>Default implementation for the 'ReorderRowDrop' event when not handled</summary>
		public static void ReorderRowsDropDefaultHandler(object sender, ReorderRowDropEventArgs args)
		{
			// Guess at the item source and reorder:
			// If the source is an unsorted/unfiltered/ungrouped ICollectionView then the source collection
			// order should match the view order. Modify the source collection and refresh the view.
			var grid = (DataGrid)sender;
			if (grid.ItemsSource is ICollectionView view && view.SortDescriptions.Count == 0 && view.Filter == null && view.GroupDescriptions.Count == 0)
			{
				if (view.SourceCollection is IList list)
				{
					// Move to before the grab position
					if (args.DropIndex < args.GrabIndex)
					{
						var tmp = list[args.GrabIndex];
						for (int i = args.GrabIndex; i != args.DropIndex; --i)
							list[i] = list[i - 1];

						list[args.DropIndex] = tmp;
					}
					// Move to after the grab position
					else
					{
						var tmp = list[args.GrabIndex];
						for (int i = args.GrabIndex; i != args.DropIndex - 1; ++i)
							list[i] = list[i + 1];

						list[args.DropIndex - 1] = tmp;
					}
					view.Refresh();
					args.Handled = true;
				}
			}
		}

		/// <summary>Event for reordering the underlying collection on row drop</summary>
		public const int ReorderRowDrop = 0;
		public delegate void ReorderRowDropEventHandler(object sender, ReorderRowDropEventArgs args);
		public static readonly RoutedEvent ReorderRowDropEvent = EventManager.RegisterRoutedEvent(nameof(ReorderRowDrop), RoutingStrategy.Bubble, typeof(ReorderRowDropEventHandler), typeof(DataGrid_));
		public static void AddReorderRowDropHandler(DependencyObject d, ReorderRowDropEventHandler handler)
		{
			if (d is UIElement uie)
				uie.AddHandler(ReorderRowDropEvent, handler);
		}
		public static void RemoveReorderRowDropHandler(DependencyObject d, ReorderRowDropEventHandler handler)
		{
			if (d is UIElement uie)
				uie.RemoveHandler(ReorderRowDropEvent, handler);
		}

		/// <summary>Event to signal that rows have been reordered</summary>
		public const int RowsReordered = 0;
		public delegate void RowsReorderedEventHandler(object sender, RoutedEventArgs args);
		public static readonly RoutedEvent RowsReorderedEvent = EventManager.RegisterRoutedEvent(nameof(RowsReordered), RoutingStrategy.Bubble, typeof(RowsReorderedEventHandler), typeof(DataGrid_));
		public static void AddRowsReorderedHandler(DependencyObject d, RowsReorderedEventHandler handler)
		{
			if (d is UIElement uie)
				uie.AddHandler(RowsReorderedEvent, handler);
		}
		public static void RemoveRowsReorderedHandler(DependencyObject d, RowsReorderedEventHandler handler)
		{
			if (d is UIElement uie)
				uie.RemoveHandler(RowsReorderedEvent, handler);
		}

		#endregion
	}
}
