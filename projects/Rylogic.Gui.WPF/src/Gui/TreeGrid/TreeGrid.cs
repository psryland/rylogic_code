﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public class TreeGrid : DataGrid
	{
		// Notes:
		//  - The TreeGrid needs to have a hierarchical collection of 'objects' ('TreeSource')
		//    and a flat collection ('FlatList') for binding to the grid.
		//  - The 'FlatList' is an IList of objects (not Nodes) so that other columns in the
		//    grid can bind directly to the objects.
		//  - For any object visibile in the grid, there is a Node object that wraps it and
		//    provides methods for accessing the children and parent of the object.
		//  - The 'TreeSource' can use INotifyCollectionChanged with Reset/Add/Remove/Replace but indices
		//    will be ignored because they are meaningless in a tree structure.



		//  This is needed because the bound collection contains only the root-level items.
		//    We need to have an internal flat-list of items to bind to the DataGrid.ItemsSource.
		//  - The FlatView must be a list of user items (i.e. not 'Node') because the other columns
		//    in the grid bind to that object. Only the TreeColumn binds to Nodes (which wrap the user item).
		public TreeGrid()
		{
			List = new FlatList(this);
			ListView = new ListCollectionView(List);

			IsSynchronizedWithCurrentItem = true;
			Columns.CollectionChanged += delegate { m_tree_column = null; };
		}
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (List.FindNode(ListView.CurrentItem) is not Node node)
				return;

			switch (e.Key)
			{
				case Key.Left:
				{
					if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) break;
					goto case Key.Subtract;
				}
				case Key.Right:
				{
					if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) break;
					goto case Key.Add;
				}
				case Key.Subtract:
				{
					if (node.IsExpanded)
					{
						node.IsExpanded = false;
						e.Handled = true;
					}
					else if (node.ParentNode is Node parent)
					{
						parent.IsExpanded = false;
						e.Handled = true;
					}
					break;
				}
				case Key.Add:
				{
					node.IsExpanded = true;
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
					{
						// Expand recursively
						var stack = new Stack<Node>(new[] { node });
						for (; stack.Count != 0;)
						{
							var n = stack.Pop();
							n.IsExpanded = true;
							foreach (var child in n.ChildNodes)
								stack.Push(child);
						}
					}
					e.Handled = true;
					break;
				}
			}
		}

		/// <summary>The tree column. There should be exactly one</summary>
		private TreeGridColumn TreeColumn => m_tree_column ??= Columns.OfType<TreeGridColumn>().SingleOrDefault() ?? throw new Exception("TreeGrids should have exactly one DataGridTreeColumn");
		private TreeGridColumn? m_tree_column;

		/// <summary>The collection of root level tree items</summary>
		public IEnumerable? TreeSource
		{
			get => (IEnumerable?)GetValue(TreeSourceProperty);
			set => SetValue(TreeSourceProperty, value);
		}
		private void TreeSource_Changed(IEnumerable? nue, IEnumerable? old)
		{
			using (var defer = List.DeferRefresh())
			{
				List.Clear();

				if (old is INotifyCollectionChanged old_ncc)
				{
					old_ncc.CollectionChanged -= HandleCollectionChanged;
				}
				if (nue is INotifyCollectionChanged nue_ncc)
				{
					nue_ncc.CollectionChanged += HandleCollectionChanged;
					HandleCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				}
				else if (nue is IEnumerable nue_list)
				{
					foreach (var item in nue_list)
						List.Add(item!, null);
				}
			}

			// Bind the grid to the flat list of 'objects'
			ItemsSource = ListView;

			// Handlers
			void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
			{
				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Reset:
					{
						// 'TreeSource' is reset, repopulate 'List' with just the root level objects
						using var defer = List.DeferRefresh();

						List.Clear();
						if (TreeSource is IEnumerable roots)
						{
							foreach (var item in roots)
								List.Add(item!, null);
						}
						break;
					}
					case NotifyCollectionChangedAction.Add:
					{
						// Items have been added to 'TreeSource'. Insert them into the flat list
						foreach (var item in e.NewItems)
						{
							var parent = List.FindNode(item!) is Node node ? node.ParentNode : null;
							List.Add(item!, parent);
						}
						break;
					}
					case NotifyCollectionChangedAction.Remove:
					{
						// Items have been removed from 'TreeSource'. Remove them from the flat list
						foreach (var item in e.OldItems)
						{
							List.Remove(item!);
						}
						break;
					}
					case NotifyCollectionChangedAction.Replace:
					case NotifyCollectionChangedAction.Move:
					{
						// Remove the old items and add the new ones
						foreach (var item in e.OldItems)
						{
							List.Remove(item!);
						}
						foreach (var item in e.NewItems)
						{
							var parent = List.FindNode(item!) is Node node ? node.ParentNode : null;
							List.Add(item!, parent);
						}
						break;
					}
					default:
					{
						throw new Exception($"Unsupported collection operation: {e.Action}");
					}
				}
			}
		}
		public static readonly DependencyProperty TreeSourceProperty = Gui_.DPRegister<TreeGrid>(nameof(TreeSource), null, Gui_.EDPFlags.None);

		/// <summary>The flat list of items for DataGrid binding</summary>
		internal ICollectionView ListView { get; }
		internal FlatList List { get; }

		/// <summary>Hide the DataGrid's ItemsSource property</summary>
		private new IEnumerable? ItemsSource
		{
			get => base.ItemsSource;
			set => base.ItemsSource = value;
		}

		/// <summary>Expand the item in the tree</summary>
		public void Expand(object item)
		{
			if (List.FindNode(item) is not Node node)
				throw new Exception("Item is not visible in the tree");
			
			node.IsExpanded = true;
		}

		/// <summary>Collapse the item in the tree</summary>
		public void Collapse(object item)
		{
			if (List.FindNode(item) is not Node node)
				throw new Exception("Item is not visible in the tree");

			node.IsExpanded = false;
		}

		/// <summary>A collection optimised for expanding/collapsing tree nodes</summary>
		internal class FlatList : IList, INotifyCollectionChanged
		{
			// Notes:
			//  - The flat list contains a hash map from objects to 'Node's
			//  - It contains an entry for each item that could be visible in
			//    the grid, i.e. not items that are children of collapsed nodes.
			//  - The elements of the IList are user items, not 'Node' because
			//    other grid columns are bound to the user items.
			//  - Presences in the 'Nodes' map indicates visibility. There are
			//    temporary states when only some of the children of a node are
			//    visible.

			private class NodeMap : Dictionary<object, Node> { }

			/// <summary></summary>
			public FlatList(TreeGrid owner)
			{
				Owner = owner;
				Nodes = new NodeMap();
			}

			/// <summary>The grid that owns this list</summary>
			private TreeGrid Owner { get; }

			/// <summary>The Node for each visible item</summary>
			private NodeMap Nodes { get; }

			/// <summary>Enumerate the root level objects (in order)</summary>
			private IEnumerable<object> RootItems => Owner.TreeSource?.Cast<object>() ?? Enumerable.Empty<object>();

			/// <summary>Enumerate the root level nodes (in order)</summary>
			private IEnumerable<Node> RootNodes => RootItems.Select(x => FindNode(x)).NotNull();

			/// <summary>Iterate over all visible items</summary>
			public IEnumerator GetEnumerator()
			{
				var stack = new Stack<IEnumerator<Node>>(new[] { RootNodes.GetEnumerator() });
				for (; stack.Count != 0;)
				{
					var nodes = stack.Peek();
					if (!nodes.MoveNext())
					{
						stack.Pop();
						continue;
					}

					var node = nodes.Current;
					yield return node.DataItem;

					if (node.IsExpanded)
						stack.Push(node.ChildNodes.GetEnumerator());
				}
			}

			/// <summary>The number of visible rows in the grid</summary>
			public int Count => Nodes.Count;

			/// <summary>Return an item by index</summary>
			public object this[int index]
			{
				get
				{
					var i = -1;
					foreach (var item in this)
					{
						if (++i != index) continue;
						return item!;
					}
					throw new IndexOutOfRangeException($"Index value {index} is out of range [0,{Count})");
				}
			}

			/// <summary>Return the node for an item. Returns null if 'item' is not visible</summary>
			public Node? FindNode(object item)
			{
				if (item == null) return null;
				return Nodes.TryGetValue(item, out var node) ? node : null;
			}

			/// <summary>True if 'item' is visible in the list</summary>
			public bool Contains(object item)
			{
				return Nodes.ContainsKey(item);
			}

			/// <summary>Return the index of 'item' in the flat list</summary>
			public int IndexOf(object item)
			{
				// Notes:
				//  - The index is based on the visible nodes only
				var index = 0;
				foreach (var i in this)
				{
					if (item == i) return index;
					++index;
				}
				return -1;
			}

			/// <summary>Drop all data from the list</summary>
			public void Clear()
			{
				Nodes.Clear();
				NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}

			/// <summary>Add an item to the list of expanded nodes</summary>
			public void Add(object item, Node? parent)
			{
				// Notes:
				//  - add/remove are not symmetric. Add only adds immediate children
				//    but remove has to remove all descendants recursively.
				//  - CollectionChanged doesn't support ranged adds/removes.
				//  - Remove is difficult because it creates temporary states where
				//    only some of the children of a node are in the list.
				if (item == null)
					throw new Exception("Cannot add null items");

				// Add the node
				var node = Node.Create(item, parent, Owner);
				Nodes[item] = node;
				var index = IndexOf(item);

				// Notify
				NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
			}

			/// <summary>Remove 'item' from the list of expanded nodes</summary>
			public void Remove(object item)
			{
				if (item == null)
					throw new Exception("Cannot remove 'null'");
				if (FindNode(item) is not Node node)
					return;

				// Remove its children
				if (node.IsExpanded)
					foreach (var child in node.ChildItems)
						Remove(child);

				// Remove the node
				var idx = IndexOf(item);
				Nodes.Remove(item);

				// If we're deleting the current item, make the previous one current
				if (Owner.ListView.CurrentItem == item)
					Owner.ListView.MoveCurrentToPrevious();
				if (Keyboard.FocusedElement is DataGridCell cell && cell.DataContext == item)
					cell.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));

				// Notify
				NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, idx));
			}

			/// <inheritdoc/>
			public event NotifyCollectionChangedEventHandler? CollectionChanged;
			private void NotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
			{
				if (m_defer_ncc_events != 0) return;
				CollectionChanged?.Invoke(this, args);
			}
			private int m_defer_ncc_events;

			/// <summary>Suspended NCC events and do a refresh when disposed</summary>
			public IDisposable DeferRefresh()
			{
				return Scope.Create(
					() => ++m_defer_ncc_events,
					() => { --m_defer_ncc_events; NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)); });
			}

			#region IList

			bool IList.IsReadOnly => true;
			bool IList.IsFixedSize => false;
			void IList.Clear() => Clear();
			int IList.Add(object? value) => throw new NotImplementedException();
			void IList.Remove(object? value) => throw new NotImplementedException();
			bool IList.Contains(object? value) => Contains(value ?? throw new Exception("List never contains nulls"));
			int IList.IndexOf(object? value) => IndexOf(value ?? throw new Exception("List never contains nulls"));
			void IList.Insert(int index, object? value) => throw new System.NotImplementedException();
			void IList.RemoveAt(int index)
			{
				throw new System.NotImplementedException();
			}
			object? IList.this[int index]
			{
				get => this[index];
				set
				{
					throw new System.NotImplementedException();
				}
			}

			int ICollection.Count => Count;
			bool ICollection.IsSynchronized
			{
				get
				{
					throw new System.NotImplementedException();
				}
			}
			object ICollection.SyncRoot
			{
				get
				{
					throw new System.NotImplementedException();
				}
			}
			void ICollection.CopyTo(System.Array array, int index)
			{
				throw new System.NotImplementedException();
			}
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			#endregion
		}

		/// <summary>A tree node</summary>
		[DebuggerDisplay("Node: {DataItem}")]
		internal class Node : FrameworkElement, INotifyPropertyChanged
		{
			/// <summary>Node factory that adds bindings from the DataGridTreeColumn</summary>
			public static Node Create(object item, Node? parent, TreeGrid owner)
			{
				var node = new Node(item, parent, owner);
				var tcol = owner.TreeColumn;
				if (tcol.Binding is BindingBase name) node.SetBinding(TextProperty, Binding_.Clone(name, source: node.DataItem));
				if (tcol.Image is BindingBase image) node.SetBinding(ImageProperty, Binding_.Clone(image, source: node.DataItem));
				if (tcol.Children is BindingBase children) node.SetBinding(ChildrenProperty, Binding_.Clone(children, source: node.DataItem));
				return node;
			}
			private Node(object data_item, Node? parent, TreeGrid owner)
			{
				Owner = owner;
				ParentNode = parent;
				DataItem = data_item;
			}

			/// <summary>The grid the contains this node</summary>
			private TreeGrid Owner { get; }

			/// <summary>The column that created this node</summary>
			private FlatList List => Owner.List;

			/// <summary>The tree column containing the cell for this node</summary>
			public TreeGridColumn Column => Owner.TreeColumn;

			/// <summary>The user object</summary>
			public object DataItem { get; }

			/// <summary>The parent of this node</summary>
			public Node? ParentNode { get; }

			/// <summary>Return the root-level node for this node</summary>
			public Node RootNode
			{
				get
				{
					var node = this;
					for (; node.ParentNode != null; node = node.ParentNode) { }
					return node;
				}
			}

			/// <summary>Children as 'objects'</summary>
			public IEnumerable<object> ChildItems => Children.Cast<object>();

			/// <summary>The visible children of this node</summary>
			public IEnumerable<Node> ChildNodes => ChildItems.Select(x => List.FindNode(x)).NotNull();

			/// <summary>True if this node is expanded</summary>
			public bool IsExpanded
			{
				get => m_is_expanded;
				set
				{
					if (IsExpanded == value) return;
					var expand = value && HasChildren;
					InvalidateCount();

					// Expand the node
					if (expand)
					{
						// Ensure the node knows it's expanded
						m_is_expanded = true;

						// Add each child to the flat list
						foreach (var child in ChildItems)
							List.Add(child, this);
					}
					else
					{
						// Remove each child from the flat list
						foreach (var child in ChildItems)
							List.Remove(child);

						// Ensure the node knows it's collapsed
						m_is_expanded = false;
					}
					NotifyPropertyChanged(nameof(IsExpanded));
				}
			}
			private bool m_is_expanded;

			/// <summary>True if the node has child nodes</summary>
			public bool HasChildren => ChildItems.Any();

			/// <summary>The indentation level of the node</summary>
			public int Level
			{
				get
				{
					var level = 0;
					for (var node = this; node.ParentNode is Node parent; node = parent, ++level) { }
					return level;
				}
			}

			/// <summary>The amount to indent, in DIP pixels</summary>
			public int Indent => Level * Column.IndentSize;

			/// <summary>Invalidate counts up to the root of this node</summary>
			private void InvalidateCount()
			{
				ParentNode?.InvalidateCount();
				NotifyPropertyChanged(nameof(HasChildren));
			}

			/// <summary>The text label for the node</summary>
			public string Text
			{
				get => (string)GetValue(TextProperty);
				set => SetValue(TextProperty, value);
			}
			public static readonly DependencyProperty TextProperty = Gui_.DPRegister<Node>(nameof(Text), string.Empty, Gui_.EDPFlags.TwoWay);

			/// <summary>Optional image for the node</summary>
			public ImageSource Image
			{
				get => (ImageSource)GetValue(ImageProperty);
				set => SetValue(ImageProperty, value);
			}
			public static readonly DependencyProperty ImageProperty = Gui_.DPRegister<Node>(nameof(Image), null, Gui_.EDPFlags.None);

			/// <summary>The child elements. Note, the children are not visible in the grid if IsExpanded is false</summary>
			public IEnumerable Children
			{
				get => (IEnumerable?)GetValue(ChildrenProperty) ?? Enumerable.Empty<object>();
				set => SetValue(ChildrenProperty, value);
			}
			public static readonly DependencyProperty ChildrenProperty = Gui_.DPRegister<Node>(nameof(Children), null, Gui_.EDPFlags.None);

			/// <inheritdoc/>
			public event PropertyChangedEventHandler? PropertyChanged;
			private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

#if false
			/// <summary>Return the count of all (visible) nodes below this one</summary>
			public int DescendantCount
			{
				get
				{
					//if (m_descendant_count == null)
					{
						static int DescCount(Node n) => 1 + (n.IsExpanded ? n.ChildNodes.Sum(x => 1 + x.DescendantCount) : 0);
						m_descendant_count = -1 + DescCount(this);
					}
					return m_descendant_count.Value;
				}
			}
			private int? m_descendant_count;

			/// <summary>Enumerate the expanded children of 'node'</summary>
			private IEnumerable<Node> VisibleChildren(Node node)
			{
				if (!node.IsExpanded) yield break;
				foreach (var child in node.Children!)
					yield return Nodes[child!];
			}


		/// <summary>Expand 'node' in the tree view</summary>
		internal bool Expand(Node node)
		{
			if (FlatView?.SourceCollection is not FlatList list)
				return false;
			if (!node.HasChildren)
				return false;

		//	// Find where 'node' is in the flat list (todo: optimise)
		//	var idx = list.Lookup(node.DataItem);
		//	if (idx == -1)
		//		return false;
		//
		//	// Insert the children of 'node'
		//	foreach (var child in children)
		//		list.Insert(++idx, child!);

			return true;
		}

		/// <summary>Collapse 'node' in the tree view</summary>
		internal bool Collapse(Node node)
		{
			if (FlatView?.SourceCollection is not ObservableCollection<object> list)
				return false;
			if (node.Children is not IEnumerable children)
				return false;

			// Find where 'node' is in the flat list (todo: optimise)
			var idx = list.IndexOf(node.DataItem);
			if (idx == -1)
				return false;

			// Remove the children of 'node'
			foreach (var child in children)
				list.RemoveAt(++idx);

			return true;
		}
#endif