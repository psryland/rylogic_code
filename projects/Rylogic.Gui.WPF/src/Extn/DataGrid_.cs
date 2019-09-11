using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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
		public static DataGridCell GetCell(DependencyObject dp)
		{
			return Gui_.FindVisualParent<DataGridCell>(dp);
		}

		/// <summary>Returns the row that contains this cell</summary>
		public static DataGridRow GetRow(this DataGridCell cell)
		{
			return Gui_.FindVisualParent<DataGridRow>(cell);
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

		#region Column Visibility

		/// <summary>Display a context menu for showing/hiding columns in the grid (at 'location' relative to the grid).</summary>
		public static void ColumnVisibilityCMenu(this DataGrid grid, Action<DataGridColumn> on_vis_changed = null)
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
		//  You can then handle reordered with custom code

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
			void HandleDrop(object sender, DragEventArgs e)
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
				var row = Gui_.FindVisualParent<DataGridRow>(row_header, root: data.Grid);
				if (row_header == null || row == null)
					return;

				// Get the index of the insert position
				var row_pt = e.GetPosition(row_header);
				var grab_index = data.Row.GetIndex();
				var drop_index = row.GetIndex() + (2 * row_pt.Y < row_header.ActualHeight ? 0 : 1);

				// Nothing to do if not reordering
				if (grab_index != drop_index)
				{
					// Reorder the items source.
					// Call the event to see if the user code handles it
					var args = new ReorderRowDropEventArgs(data.Grid, data.Row, drop_index);
					data.Grid.RaiseEvent(args);
					if (!args.Handled)
					{
						// Guess at the item source and reorder:
						// If the source is an unsorted/unfiltered/ungrouped ICollectionView then the source collection
						// order should match the view order. Modify the source collection and refresh the view.
						if (data.Grid.ItemsSource is ICollectionView view && view.SortDescriptions.Count == 0 && view.Filter == null && view.GroupDescriptions.Count == 0)
						{
							if (view.SourceCollection is IList list)
							{
								// Move to before the grab position
								if (drop_index < grab_index)
								{
									var tmp = list[grab_index];
									for (int i = grab_index; i != drop_index; --i)
										list[i] = list[i - 1];

									list[drop_index] = tmp;
								}
								// Move to after the grab position
								else
								{
									var tmp = list[grab_index];
									for (int i = grab_index; i != drop_index - 1; ++i)
										list[i] = list[i + 1];

									list[drop_index - 1] = tmp;
								}
								view.Refresh();
								e.Handled = true;
							}
						}
					}
				}
				e.Handled = true;
			}
			void HandleDragging(object sender, DragEventArgs e)
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
			void HandleMouseDown(object sender, MouseButtonEventArgs e)
			{
				if (!(e.OriginalSource is DependencyObject src)) return;

				var grid = (DataGrid)sender;
				var row_header = Gui_.FindVisualParent<DataGridRowHeader>(src, root: grid);
				if (row_header != null && grid.Cursor != Cursors.SizeNS)
				{
					grid.Dispatcher.BeginInvoke(new Action(() =>
					{
						using (var data = new ReorderRowsWithDragDropData(grid, e.GetPosition(grid)))
							DragDrop.DoDragDrop(grid, data, DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link);
					}));
					e.Handled = true;
				}
			}
		}
		private class ReorderRowsWithDragDropData :IDisposable
		{
			public ReorderRowsWithDragDropData(DataGrid grid, Point grab)
			{
				Grid = grid;
				Grab = grab;
				Row = Gui_.FindVisualParent<DataGridRow>(grid.InputHitTest(grab) as DependencyObject, root: grid);
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
				InsertIndex = insert_index;
			}

			/// <summary>The row being dragged</summary>
			public DataGridRow Row { get; }

			/// <summary>The index of where to insert the row</summary>
			public int InsertIndex { get; }
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

		#endregion
	}
}
