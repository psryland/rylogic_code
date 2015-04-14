using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace pr.extn
{
	public static class TreeViewExtensions
	{
		/// <summary>Enumerate through all nodes in a collection</summary>
		private static IEnumerable<TreeNode> AllNodes(TreeNodeCollection nodes, ERecursionOrder order)
		{
			switch (order)
			{
			case ERecursionOrder.BreadthFirst:
				{
					var queue = new Queue<TreeNode>(nodes.Cast<TreeNode>());
					while (queue.Count != 0)
					{
						var node = queue.Dequeue();
						for (int i = 0; i != node.Nodes.Count; ++i) queue.Enqueue(node.Nodes[i]);
						yield return node;
					}
					break;
				}
			case ERecursionOrder.DepthFirst:
				{
					var stack = new Stack<TreeNode>(nodes.Cast<TreeNode>());
					while (stack.Count != 0)
					{
						var node = stack.Pop();
						for (int i = node.Nodes.Count; i-- != 0; ) stack.Push(node.Nodes[i]);
						yield return node;
					}
					break;
				}
			}
		}
		public enum ERecursionOrder { DepthFirst, BreadthFirst }

		/// <summary>Enumerate through all nodes in a the tree</summary>
		public static IEnumerable<TreeNode> AllNodes(this TreeView tree, ERecursionOrder order)
		{
			return AllNodes(tree.Nodes, order);
		}

		/// <summary>Enumerate through all child nodes</summary>
		public static IEnumerable<TreeNode> AllNodes(this TreeNode node, ERecursionOrder order)
		{
			return AllNodes(node.Nodes, order);
		}

		/// <summary>
		/// Return the node corresponding to the given path. Uses the tree's path separater as the delimiter.
		/// If 'add_node' is given, child nodes are added where necessary.</summary>
		public static TreeNode GetNode(this TreeView tree, string fullpath, Func<string,TreeNode> add_node = null)
		{
			var parts = fullpath.Split(new[]{tree.PathSeparator}, StringSplitOptions.RemoveEmptyEntries);

			var node = (TreeNode)null;
			var nodes = tree.Nodes;
			foreach (var part in parts)
			{
				if      (part == ".") continue;
				else if (part == ".." && node != null) node = node.Parent;
				else node = nodes.Cast<TreeNode>().FirstOrDefault(x => string.Compare(x.Name, part) == 0);
				if (node == null && add_node != null)
					node = add_node(part);
				if (node == null)
					throw new Exception("Node path {0} does not exist".Fmt(fullpath));

				nodes = node.Nodes;
			}
			return node;
		}

		/// <summary>
		/// Expands branches of the tree down to 'path'. Uses the tree's path separater as the delimiter.
		/// 'ignore_case' ignores case differences between the parts of 'fullpath' and the node.Name's.</summary>
		public static void Expand(this TreeView tree, string fullpath)
		{
			tree.GetNode(fullpath).EnsureVisible();
		}

		/// <summary>Ensure the parents of this node are expanded</summary>
		public static void ExpandParents(this TreeNode node)
		{
			if (node.Parent == null) return;
			if (node.Parent.IsExpanded) return;
			node.Parent.Expand();
			node.Parent.ExpandParents();
		}
	}
}
