using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class TreeViewMultiSelect :TreeView
	{
		public TreeViewMultiSelect()
		{
			InitializeComponent();
			ExpandAll = Command.Create(this, ExpandAllInternal);
			CollapseAll = Command.Create(this, CollapseAllInternal);
		}
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Down:
				case Key.Up:
				{
					NavigateBy(e.Key == Key.Down ? +1 : -1);
					e.Handled = true;
					break;
				}
				case Key.Home:
				case Key.End:
				{
					NavigateBy(e.Key == Key.End ? +int.MaxValue : -int.MaxValue);
					e.Handled = true;
					break;
				}
				case Key.PageDown:
				case Key.PageUp:
				{
					NavigateBy(e.Key == Key.PageDown ? +10 : -10);
					e.Handled = true;
					break;
				}
				case Key.Left:
				{
					if (IsCtrlPressed)
					{
						CollapseCurrentItem();
						e.Handled = true;
					}
					break;
				}
				case Key.Right:
				{
					if (IsCtrlPressed)
					{
						ExpandCurrentItem();
						e.Handled = true;
					}
					break;
				}
				case Key.A:
				{
					if (IsCtrlPressed)
					{
						SelectAll();
						e.Handled = true;
					}
					break;
				}
			}
		}
		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDown(e);

			// If clicking on a tree branch expander...
			if (e.OriginalSource is Shape || e.OriginalSource is Grid || e.OriginalSource is Border)
				return;

			var item = GetTreeViewItemClicked((FrameworkElement)e.OriginalSource);
			if (item != null)
				SelectedItemChangedInternal(item);
		}
		private static TreeViewItem? GetTreeViewItemClicked(DependencyObject sender)
		{
			for (;sender is not null and not TreeViewItem;)
				sender = VisualTreeHelper.GetParent(sender);

			return sender as TreeViewItem;
		}

		/// <summary>The selected tree view items</summary>
		public IList SelectedItems
		{
			get
			{
				var selected_tree_view_items = GetTreeViewItems(this, true).Where(GetIsItemSelected);
				var selected_model_items = selected_tree_view_items.Select(x => x.Header);
				return selected_model_items.ToList();
			}
		}

		/// <summary>'SelectedItems' has changed</summary>
		public event RoutedEventHandler SelectionChanged
		{
			add { AddHandler(SelectionChangedEvent, value); }
			remove { RemoveHandler(SelectionChangedEvent, value); }
		}
		public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewMultiSelect));

		/// <summary></summary>
		private void SelectedItemChangedInternal(TreeViewItem selected_item)
		{
			var added = new ArrayList();
			var removed = new ArrayList();

			// Clear all previous selected item states if ctrl is NOT being held down
			if (!IsCtrlPressed)
			{
				var items = GetTreeViewItems(this, true);
				foreach (var item in items)
				{
					SetIsItemSelected(item, false);
					removed.Add(item.DataContext);
				}
			}

			// Is this an item range selection?
			if (IsShiftPressed && m_last_selected != null)
			{
				var items = GetTreeViewItemRange(m_last_selected, selected_item);
				if (items.Count > 0)
				{
					foreach (var item in items)
					{
						SetIsItemSelected(item, true);
						added.Add(item.DataContext);
					}
					m_last_selected = items.Last();
				}
			}
			// Otherwise, individual selection
			else
			{
				SetIsItemSelected(selected_item, true);
				added.Add(selected_item.DataContext);
				m_last_selected = selected_item;
			}

			// Notify selection changed
			var args = new SelectionChangedEventArgs(SelectionChangedEvent, removed, added);
			RaiseEvent(args);
		}
		private TreeViewItem? m_last_selected;

		/// <summary>Navigate to the next visible item in the tree</summary>
		private void NavigateBy(int direction)
		{
			var all_visible_items = GetTreeViewItems(this, false); // Only visible items
			if (all_visible_items.Count == 0)
				return;

			var currently_selected = all_visible_items.FirstOrDefault(GetIsItemSelected);
			if (currently_selected == null)
			{
				if (direction > 0)
					SelectSingleItem(all_visible_items.First());
				else if (direction < 0)
					SelectSingleItem(all_visible_items.Last());
				return;
			}

			var current_index = all_visible_items.IndexOf(currently_selected);
			var new_index = (int)Math_.Clamp((long)current_index + direction, 0, all_visible_items.Count - 1);
			SelectSingleItem(all_visible_items[new_index]);
		}

		/// <summary>Select a single item, clearing other selections unless Ctrl is pressed</summary>
		private void SelectSingleItem(TreeViewItem item)
		{
			ArrayList removed = [];
			ArrayList added = [];

			if (!IsCtrlPressed)
			{
				// Clear all previous selections
				var all_items = GetTreeViewItems(this, true);
				foreach (var tree_item in all_items)
				{
					SetIsItemSelected(tree_item, false);
					removed.Add(tree_item.Header);
				}
			}

			// Select the new item
			SetIsItemSelected(item, true);
			m_last_selected = item;
			added.Add(item.Header);

			// Ensure the item is visible
			item.BringIntoView();

			// Focus the item
			item.Focus();

			// Notify selection changed
			RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, removed, added));
		}

		/// <summary>Select all visible items</summary>
		private void SelectAll()
		{
			var all_items = GetTreeViewItems(this, false);

			var added = new ArrayList();
			var removed = new ArrayList();

			// Select all visible items
			foreach (var item in all_items)
			{
				if (!GetIsItemSelected(item))
				{
					SetIsItemSelected(item, true);
					added.Add(item.Header);
				}
			}

			// Update the last selected item to the last item in the list
			if (all_items.Count > 0)
				m_last_selected = all_items.Last();

			// Notify selection changed if any items were added
			if (added.Count > 0)
			{
				var args = new SelectionChangedEventArgs(SelectionChangedEvent, removed, added);
				RaiseEvent(args);
			}
		}

		/// <summary>Expand the currently selected item</summary>
		private void ExpandCurrentItem()
		{
			var currently_selected = GetTreeViewItems(this, false).FirstOrDefault(GetIsItemSelected);
			if (currently_selected != null && currently_selected.HasItems && !currently_selected.IsExpanded)
			{
				currently_selected.IsExpanded = true;
			}
		}

		/// <summary>Collapse the currently selected item</summary>
		private void CollapseCurrentItem()
		{
			var currently_selected = GetTreeViewItems(this, false).FirstOrDefault(GetIsItemSelected);
			if (currently_selected == null)
				return;

			if (currently_selected.IsExpanded)
			{
				currently_selected.IsExpanded = false;
				return;
			}

			if (GetParentTreeViewItem(currently_selected) is TreeViewItem parent)
			{
				// Collapse the parent if it's expanded
				if (parent.IsExpanded)
					parent.IsExpanded = false;

				// Move selection to the parent
				SelectSingleItem(parent);
			}
		}

		/// <summary>Get the parent TreeViewItem of the 'item'</summary>
		private TreeViewItem? GetParentTreeViewItem(TreeViewItem item)
		{
			// Walk up the visual tree to find the parent TreeViewItem
			for (var parent = VisualTreeHelper.GetParent(item); parent != null; parent = VisualTreeHelper.GetParent(parent))
			{
				if (parent is TreeViewItem parent_item)
					return parent_item;
			}
			return null;
		}

		/// <summary>Expand all nodes</summary>
		public ICommand ExpandAll { get; }
		private void ExpandAllInternal()
		{
			static void DoExpand(ItemsControl ctrl)
			{
				foreach (var item in ctrl.Items)
				{
					if (ctrl.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem tvitem) continue;
					tvitem.IsExpanded = true;
					if (tvitem.Items.Count == 0) continue;
					DoExpand(tvitem);
				}
			}
			DoExpand(this);
		}

		/// <summary>Collapse all nodes</summary>
		public ICommand CollapseAll { get; }
		private void CollapseAllInternal()
		{
			static void DoCollapse(ItemsControl ctrl)
			{
				foreach (var item in ctrl.Items)
				{
					if (ctrl.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem tvitem) continue;
					tvitem.IsExpanded = false;
					if (tvitem.Items.Count == 0) continue;
					DoCollapse(tvitem);
				}
			}
			DoCollapse(this);
		}

		/// <summary>Return all tree items between two tree times</summary>
		private List<TreeViewItem> GetTreeViewItemRange(TreeViewItem beg, TreeViewItem end)
		{
			var items = GetTreeViewItems(this, false);
			var ibeg = items.IndexOf(beg);
			var iend = items.IndexOf(end);
			var range_start = ibeg > iend || ibeg == -1 ? iend : ibeg;
			var range_count = ibeg > iend ? ibeg - iend + 1 : iend - ibeg + 1;

			if (ibeg == -1 && iend == -1)
				range_count = 0;
			else if (ibeg == -1 || iend == -1)
				range_count = 1;

			return range_count != 0
				? items.GetRange(range_start, range_count)
				: new List<TreeViewItem>();
		}

		/// <summary>Recursive function for returning all items below a parent item</summary>
		private static List<TreeViewItem> GetTreeViewItems(ItemsControl parent, bool include_collapsed_items, List<TreeViewItem>? item_list = null)
		{
			item_list ??= new List<TreeViewItem>();
			for (var i = 0; i < parent.Items.Count; ++i)
			{
				if (parent.ItemContainerGenerator.ContainerFromIndex(i) is not TreeViewItem tv_item)
					continue;

				item_list.Add(tv_item);
				if (include_collapsed_items || tv_item.IsExpanded)
					GetTreeViewItems(tv_item, include_collapsed_items, item_list);
			}
			return item_list;
		}

		/// <summary>Attached property for 'IsItemSelected'</summary>
		private const int IsItemSelected = 0;
		public static readonly DependencyProperty IsItemSelectedProperty = Gui_.DPRegisterAttached<TreeViewMultiSelect>(nameof(IsItemSelected), Boxed.False, Gui_.EDPFlags.None);
		public static void SetIsItemSelected(UIElement element, bool value) => element.SetValue(IsItemSelectedProperty, value);
		public static bool GetIsItemSelected(UIElement element) => (bool)element.GetValue(IsItemSelectedProperty);

		/// <summary>Key press helpers</summary>
		private static bool IsCtrlPressed => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
		private static bool IsShiftPressed => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
	}
}
