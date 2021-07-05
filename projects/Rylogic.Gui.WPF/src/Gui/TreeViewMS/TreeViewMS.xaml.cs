using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Rylogic.Gui.WPF
{
	public partial class TreeViewMS :TreeView
	{
		public TreeViewMS()
		{
			InitializeComponent();
			ExpandAll = Command.Create(this, ExpandAllInternal);
			CollapseAll = Command.Create(this, CollapseAllInternal);
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
		public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewMS));

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
		public static readonly DependencyProperty IsItemSelectedProperty = Gui_.DPRegisterAttached<TreeViewMS>(nameof(IsItemSelected));
		public static void SetIsItemSelected(UIElement element, bool value) => element.SetValue(IsItemSelectedProperty, value);
		public static bool GetIsItemSelected(UIElement element) => (bool)element.GetValue(IsItemSelectedProperty);

		/// <summary>Key press helpers</summary>
		private static bool IsCtrlPressed => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
		private static bool IsShiftPressed => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
	}
}
