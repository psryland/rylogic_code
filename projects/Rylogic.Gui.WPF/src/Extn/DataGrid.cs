using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Rylogic.Gui.WPF
{
	public static class DataGrid_
	{
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
			// doesn't work. You'd need to provide an instance method that forwarded to this call, so you
			// might as well just sign up the event handler in the constructor:
			//  m_grid.MouseRightButtonUp += DataGrid_.ColumnVisibility;
			if (args.ChangedButton != MouseButton.Right || args.RightButton != MouseButtonState.Released)
				return;

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
	}
}
