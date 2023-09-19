using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Cache;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rylogic.Extn;
using Rylogic.PolyFill;
using Rylogic.Utility;

namespace Rylogic.Container
{
	public static class RedBlack_
	{
		// Notes:
		//  - Rules:
		//     + New nodes start as 'Red'
		//     + The root node is always 'Black'
		//     + The leaf null pointers are considered 'Black'
		//     + If a node is 'Red' then its children are 'Black'
		//     + All paths through the tree have equal number of black nodes
		//
		//  - This implementation does not require 'T' have a parent pointer
		//  - 'IAccessors<T>.Compare' is the same as the usual IComparible.
		//    This is turned into a boolean Left/Right side internally.
		// Handy Visualisation: https://www.cs.usfca.edu/~galles/visualization/RedBlack.html

		public enum EColour
		{
			Red = 0,
			Black = 1,
		}
		public interface IAccessors<T>
		{
			/// <summary>
			/// Compare 'lhs' and 'rhs'.
			/// Return -1 => lhs < rhs, 0 => lhs == rhs, +1 => lhs > rhs</summary>
			int Compare(T lhs, T rhs);

			/// <summary>Get/Set the colour of a node. Return black if 'elem' is null</summary>
			EColour Colour([AllowNull] T elem);
			void Colour([DisallowNull] T elem, EColour colour);

			/// <summary>
			/// Get the left/right child of 'elem'.
			/// 'side < 0' means left, 'side > 0' means right
			/// If 'elem' is null, assume it's children are null</summary>
			[return: MaybeNull] T Child(int side, [AllowNull] T elem);

			/// <summary>
			/// Set the left/right child of 'elem'.
			/// 'side < 0' means left, 'side > 0' means right.
			/// NOTE: 'elem' can be null which means set 'child' as the root of the tree</summary>
			void Child(int side, [AllowNull] T elem, [AllowNull] T child);
		}
		private const int Left = -1;
		private const int Right = +1;

		/// <summary>Initialise a Red/Black tree with a root node</summary>
		public static T Build<T>(T root) where T : IAccessors<T> => Build(root, root);
		public static T Build<T>(T root, IAccessors<T> ops) where T : notnull => Build(new[] { root }, ops);

		/// <summary>Construct a Red/Black tree from 'items'. Returns the root</summary>
		public static T Build<T>(IEnumerable<T> items) where T : IAccessors<T> => Build(items, items.First());
		public static T Build<T>(IEnumerable<T> items, IAccessors<T> ops) where T : notnull
		{
			var iter = items.GetEnumerator();
			if (!iter.MoveNext())
				throw new Exception("Cannot build a Red/Black tree from an empty collection");

			// Use the first item as the root
			var root = iter.Current;
			ops.Colour(root, EColour.Black);

			// Insert all items into the tree
			for (; iter.MoveNext();)
			{
				root = Insert(root, iter.Current, ops);
				Check(root, ops);
			}

			return root;
		}

		/// <summary>Insert a node into a red black tree. Returns the new root</summary>
		public static T Insert<T>([DisallowNull] T root, [DisallowNull] T item) where T:IAccessors<T> => Insert(root, item, root);
		public static T Insert<T>([DisallowNull] T root, [DisallowNull] T item, IAccessors<T> ops) where T : notnull
		{
			using var stack_inst = StackPool<T>.Instance.Alloc();
			var stack = stack_inst.Value;

			// Insert 'item'. Record the path down the tree in 'stack'
			stack.Push(root);
			for (; ; )
			{
				var side = ops.Compare(item, stack.Top!) < 0 ? Left : Right;
				if (ops.Child(side, stack.Top) is T child)
				{
					stack.Push(child);
				}
				else
				{
					ops.Child(side, stack.Top, item);
					stack.Push(item);
					break;
				}
			}

			// Climb back to the root node, fixing colours as we go
			for (; stack.Count > 2;)
			{
				// Get the leaf and its parent
				var node = stack.Pop();
				var parent = stack.Pop();

				// If either 'node' or 'parent' is black, no more changes are needed
				if (ops.Colour(node) == EColour.Black || ops.Colour(parent) == EColour.Black)
					break;

				var gparent = stack.Pop();
				var parent_side = Side(parent, gparent, ops);

				// Red Uncle
				if (ops.Child(-parent_side, gparent) is T uncle && ops.Colour(uncle) == EColour.Red)
				{
					// Recolour
					ops.Colour(gparent, EColour.Red);
					ops.Colour(parent, EColour.Black);
					ops.Colour(uncle, EColour.Black);
					stack.Push(gparent);
					continue;
				}

				// Black Uncle
				// If 'node_side' is not equal to 'parent_side', swap 'node' and 'parent' to transform case 2 -> case 3
				var node_side = Side(node, parent, ops);
				if (node_side != parent_side)
					Rotate(-node_side, parent, gparent, ops);

				// Case 3 -> swap 'parent' and 'gparent' and recolour
				ops.Colour(gparent, EColour.Red);
				gparent = Rotate<T>(-parent_side, gparent, stack.Top, ops);
				ops.Colour(gparent, EColour.Black);

				// If we rotate the root element, update 'root'
				if (stack.Count == 0)
					root = gparent;
			}

			// Return the root, ensuring it's black
			ops.Child(0, default!, root);
			ops.Colour(root, EColour.Black);
			return root;
		}

		/// <summary>Remove a node from the red black tree. Returns the new root</summary>
		[return: MaybeNull] public static T Delete<T>([DisallowNull] T root, T item) where T : IAccessors<T> => Delete(root, item, root);
		[return: MaybeNull] public static T Delete<T>([DisallowNull] T root, T item, IAccessors<T> ops) where T : notnull => Delete(root, x => ops.Compare(x, item), ops);

		/// <summary>Remove a node from the red black tree. Returns the new root</summary>
		public static T Delete<T>([DisallowNull] T root, Func<T, int> pred) where T : IAccessors<T> => Delete(root, pred, root);
		public static T Delete<T>([DisallowNull] T root, Func<T, int> pred, IAccessors<T> ops) where T : notnull
		{
			using var stack_inst = StackPool<T>.Instance.Alloc();
			var stack = stack_inst.Value;

			// Delete from an empty tree is idempotent
			// Even though the function says "doesn't return null", return null for the special
			// case that 'root' is null. A completely deleted tree is a rare case, but calls like:
			//  root = RedBlack_.Delete(root, ...) will be common. The return type needs to match
			// the type of 'root'.
			if (Equals(root, default))
				return root!;

			// Find the node to delete
			stack.Push(root);
			var node_side = 0;
			for (; ; )
			{
				var side = pred(stack.Top!);
				if (side == 0)
				{
					// Found it
					break;
				}
				else if (ops.Child(side, stack.Top) is T c)
				{
					stack.Push(c);
					node_side = side;
				}
				else
				{
					// Not found in tree
					return root;
				}
			}

			// Basically, if 'node' has two children, we substitute it with a node that has 0 or 1 children.
			// That reduces the cases to just deleting zero or single child nodes.
			if (ops.Child(Left, stack.Top) is T childL && ops.Child(Right, stack.Top) is T childR)
			{
				// The node (and its parent) that need to be substituted
				var subst = stack.Pop();
				var subst_p = stack.Top;

				// Find the in-order previous node to 'subst' and swap it with 'subst'.
				// The choice is symmetrical, using the right-most in the left sub-tree.
				// 'replacement' is the node that will be swapped into the position of 'subst'
				var repl_side = Left;
				var repl_parent = subst;
				var replacement = childL;
				for (; ops.Child(Right, replacement) is T c;)
				{
					repl_parent = replacement;
					replacement = c;
					repl_side = Right;
				}

				// Swap 'replacement' and 'subst'
				// First, unlink 'replacement' from the tree
				// Note: 'repl_childR' is necessarily null.
				var repl_childL = ops.Child(Left, replacement);
				ops.Child(repl_side, repl_parent, repl_childL);

				// Replace 'subst' with 'replacement' in the tree.
				ops.Child(Right, replacement, ops.Child(Right, subst)); // attach 'subst's right sub-tree to 'replacement'
				ops.Child(Left, replacement, ops.Child(Left, subst));   // attach 'subst's left sub-tree to 'replacement'
				ops.Child(node_side, subst_p, replacement);             // attach 'replacement' as the child of 'subst_p'
				if (stack.Count == 0) root = replacement;               // if 'subst' was the root, 'replacement' is now the root
				if (repl_side == Left) repl_parent = replacement;       // if 'repl_parent' is 'subst', 'replacement' is now 'repl_parent'

				// Put 'subst' where 'replacement' was
				ops.Child(Right, subst, default!);         // 'subst' is the right-most of the left sub-tree so childR is null
				ops.Child(Left, subst, repl_childL);      // attach 'replacement's left sub-tree to 'subst'
				ops.Child(repl_side, repl_parent, subst); // attach 'subst' as a child of 'repl_parent'

				// Swap colours
				var subs_colour = ops.Colour(subst);
				var repl_colour = ops.Colour(replacement);
				ops.Colour(replacement, subs_colour);
				ops.Colour(subst, repl_colour);

				// Push nodes onto 'stack' until we reach 'replacement's old position
				stack.Push(replacement);
				for (var side = Left; ops.Child(side, replacement) is T c; side = Right)
				{
					stack.Push(c);
					replacement = c;
					node_side = side;
				}
			}

			// Now, we're deleting 'node' and we know it has 0 or 1 child. Remember: don't assume 'node' is
			// the right-most of a left sub-tree. It could be any leaf node at this point.
			var node = stack.Pop();

			// Find the single child or 'node' (or null).
			var child_l = ops.Child(Left, node);
			var child_r = ops.Child(Right, node);
			var child =
				!Equals(child_l, default) ? child_l :
				!Equals(child_r, default) ? child_r :
				default;

			// In all cases we replace 'node' with its child, but the
			// colour fix-up depends on the colours of 'node' and 'child'.
			ops.Child(node_side, stack.Top, child!);
			if (stack.Count == 0)
			{
				// If 'node' was the root, then 'child' becomes the root.
				// If 'child' is null, then we just deleted the last node.
				root = child!;
				if (Equals(root, default))
					return root!;
			}

			// If 'node' is red, then the child cannot be red because no red node has red children.
			// This means the number of black nodes in all paths is unchanged for this case.
			// If 'node' is black, then some recolouring is needed.
			if (ops.Colour(node) == EColour.Black)
			{
				// If the child is red, then the number of black nodes
				// can be preserved by promoting the red child to black.
				if (ops.Colour(child!) == EColour.Red)
				{
					ops.Colour(child!, EColour.Black);
				}
				// Otherwise, 'child' is "double black"
				else
				{
					// Repeatedly apply cases until double black is resolved.
					// 'child' is assumed to be the node that is double black.
					// 'stack.Top' is assumed to be the parent of 'child'.
					for (; ; )
					{
						// Case 1: is 'child' now the root node?
						// If so, change double-black to black and job done
						if (stack.Count == 0)
							break;

						// Find the sibbling of 'child'
						// The sibbling must exist because 'child' exists and is double black. If sibbling was
						// null then there would be an imbalance of black nodes on 'child's sub tree.
						var parent = stack.Pop();
						var sibbling = ops.Child(-node_side, parent) ?? throw new Exception("Red/Black tree is invalid");

						// Case 2: is the sibbling red? (implying parent is black, and sibbling's children are black)
						// If so, swap parent and sibbling colours and rotate sibbling to parent. This moves the double
						// black one level down the tree, and sibbling becomes the double-black's gparent.
						if (ops.Colour(sibbling) == EColour.Red)
						{
							//     P(b)              S(b)
							//    /   \      =>     /    \
							//  C(bb) S(r)        P(r)    Y(b)
							//        /  \        /   \
							//      X(b) Y(b)  C(bb)  X(b)
							ops.Colour(parent, EColour.Red);
							ops.Colour(sibbling, EColour.Black);
							var new_parent = Rotate<T>(node_side, parent, stack.Top, ops);
							stack.Push(new_parent);

							// Find the new sibbling and possible new root after the rotation
							if (stack.Count == 0) root = new_parent;
							sibbling = ops.Child(-node_side, parent) ?? throw new Exception("Red/Black tree is invalid");
						}

						// From here we know the sibbling is black
						Debug.Assert(ops.Colour(sibbling) == EColour.Black);

						// Case 3: is the sibbling black, the parent black, and both nephews black?
						// If so, make the parent the double black node and sibbling red and recur.
						if (ops.Colour(parent) == EColour.Black &&
							ops.Colour(ops.Child(Left, sibbling)) == EColour.Black &&
							ops.Colour(ops.Child(Right, sibbling)) == EColour.Black)
						{
							// This transformation pushes the problem double black node
							// up to the parent. Start again with the new double black node.
							//      P(b)              P(bb)
							//     /   \              /  \
							// C(bb)   S(b)    =>  C(b)  S(r)
							//        /   \             /   \
							//       X(b) Y(b)        X(b)  Y(b)
							ops.Colour(sibbling, EColour.Red);
							node_side = Side<T>(parent, stack.Top, ops);
							child = parent;
							continue;
						}

						// Case 4: is the sibbling black, the parent red, and both nephews black?
						// If so, swap the colours of parent and sibbling then job done
						if (ops.Colour(parent) == EColour.Red &&
							ops.Colour(ops.Child(Left, sibbling)) == EColour.Black &&
							ops.Colour(ops.Child(Right, sibbling)) == EColour.Black)
						{
							//      P(r)              P(b)
							//     /   \              /  \
							// C(bb)   S(b)    =>  C(b)  S(r)
							//        /   \             /   \
							//       X(b) Y(b)        X(b)  Y(b)
							ops.Colour(parent, EColour.Black);
							ops.Colour(sibbling, EColour.Red);
							break;
						}

						// Case 5: is the sibbling black, and the same side nephew red?
						// If so, swap nephew and sibbling colours and rotate the red nephew to the sibbling.
						// This is a preparation step before executing case 6,
						var nephew = ops.Child(node_side, sibbling);
						if (ops.Colour(nephew) == EColour.Red)
						{
							//      P(b)               P(b)
							//     /   \               /  \
							// C(bb)   S(b)    =>  C(bb)  X(r)
							//        /   \                 \
							//       X(r) Y(b)              S(r)
							//                                \
							//                                Y(b)
							ops.Colour(sibbling, EColour.Red);
							ops.Colour(nephew!, EColour.Black);
							sibbling = Rotate(-node_side, sibbling, parent, ops);
						}

						// Case 6: is the sibbling black, and the opposite side nephew red?
						// If so, rotate sibbling to parent then recolour and job done.
						nephew = ops.Child(-node_side, sibbling);
						if (ops.Colour(nephew) == EColour.Red)
						{
							//      P(?)                S(b)
							//     /   \               /    \
							// C(bb)   S(b)    =>    P(b)    Y(r)
							//        /   \         /   \
							//       X(?) Y(r)    C(b)  X(?) 
							ops.Colour(sibbling, ops.Colour(parent));
							ops.Colour(parent, EColour.Black);
							ops.Colour(nephew!, EColour.Black);
							var new_parent = Rotate<T>(node_side, parent, stack.Top, ops);

							// if 'parent' was the root, update the root
							if (stack.Count == 0) root = new_parent;
							break;
						}

						throw new Exception("Unhandled case in Red/Black tree deletion");
					}
				}
			}

			// Return the root, ensuring it's black
			ops.Child(0, default!, root);
			ops.Colour(root, EColour.Black);
			return root;
		}

		/// <summary>Depth first traversal of the tree (order is ascending = +1 or descending = -1)</summary>
		public static IEnumerable<T> EnumerateDF<T>([DisallowNull] T root, int order) where T : IAccessors<T> => EnumerateDF(root, order, root);
		public static IEnumerable<T> EnumerateDF<T>([DisallowNull] T root, int order, IAccessors<T> ops) where T : notnull
		{
			using var stack_inst = StackPool<T>.Instance.Alloc();
			var stack = stack_inst.Value;

			stack.Push(root);
			for (; stack.Count != 0;)
			{
				// Find the left-most node of 'stack.Top'
				for (; ops.Child(-order, stack.Top) is T child0;)
					stack.Push(child0);

				for (; stack.Count != 0; )
				{
					// Yield a node
					var node = stack.Pop();
					yield return node;

					// Visit the right sub-tree
					if (ops.Child(+order, node) is T child1)
					{
						stack.Push(child1);
						break;
					}
				}
			}
		}

		/// <summary>Breadth first traversal of the tree (order is ascending = +1 or descending = -1)</summary>
		public static IEnumerable<T> EnumerateBF<T>([DisallowNull] T root, int order) where T : IAccessors<T> => EnumerateBF(root, order, root);
		public static IEnumerable<T> EnumerateBF<T>([DisallowNull] T root, int order, IAccessors<T> ops) where T : notnull
		{
			using var stack_inst = StackPool<T>.Instance.Alloc();
			var stack = stack_inst.Value;

			stack.Push(root);
			for (; stack.Count != 0;)
			{
				var node = stack.Pop();
				yield return node;

				// Push right first, because stack is LIFO
				if (ops.Child(+order, node) is T child1)
					stack.Push(child1);
				if (ops.Child(-order, node) is T child0)
					stack.Push(child0);
			}
		}

		/// <summary>Validate the rules of red/black trees</summary>
		public static Exception? Check<T>(T node) where T : IAccessors<T> => Check(node, node);
		public static Exception? Check<T>(T node, IAccessors<T> ops, int level = 0)
		{
			// The root node must be black
			if (level == 0)
			{
				if (ops.Colour(node) != EColour.Black)
					return new Exception($"The root node of a Red/Black tree should be black");
			}

			// No red node has red children
			if (ops.Colour(node) == EColour.Red)
			{
				if (ops.Child(Left, node) is T l && ops.Colour(l) == EColour.Red)
					return new Exception($"Red node {node} has a red left child {l}");
				if (ops.Child(Right, node) is T r && ops.Colour(r) == EColour.Red)
					return new Exception($"Red node {node} has a red right child {r}");
			}

			// The ordering is correct
			{
				if (ops.Child(Left, node) is T l && ops.Compare(l, node) > 0)
					return new Exception($"Left child {l} compares greater than its parent node {node}");
				if (ops.Child(Right, node) is T r && ops.Compare(r, node) < 0)
					return new Exception($"Right child {r} compares less than its parent node {node}");
			}

			// Recursively check the sub trees
			if (ops.Child(Left, node) is T left && Check(left, ops, level + 1) is Exception errL)
				return errL;
			if (ops.Child(Right, node) is T rite && Check(rite, ops, level + 1) is Exception errR)
				return errR;

			return null;
		}

		/// <summary>Left or right rotate. I.E swap 'node' with its left or right child</summary>
		private static T Rotate<T>(int dir, T node, [AllowNull] T parent, IAccessors<T> ops) where T : notnull
		{
			// 'c' will end up where 'node' currently is
			var c = ops.Child(-dir, node);
			if (Equals(c, default))
				throw new Exception("Can't rotate to a null child");

			// 'cc' will become a child of 'node'
			var cc = ops.Child(+dir, c);

			// Update the child of 'parent' from 'node' to 'c'
			if (!Equals(parent, default))
			{
				var side = Side(node, parent, ops);
				ops.Child(side, parent, c);
			}

			// Set 'node' as a child of 'c'
			ops.Child(+dir, c, node);

			// Set 'cc' as a child of 'node' 
			ops.Child(-dir, node, cc);
			return c;
		}

		/// <summary>Return the child side that 'node' is of 'parent'</summary>
		private static int Side<T>(T node, [AllowNull] T parent, IAccessors<T> ops) where T : notnull
		{
			return
				Equals(parent, default) ? 0 :
				Equals(ops.Child(Left, parent), node) ? Left :
				Equals(ops.Child(Right, parent), node) ? Right :
				throw new Exception("'node' is not a child of 'parent'");
		}

		/// <summary>Performance enhancing</summary>
		[DebuggerDisplay("{Description,nq}")]
		private class Stack<T>
		{
			private readonly List<T> m_list = new();
			[MaybeNull] public T Top  => m_list.Count != 0 ? m_list[m_list.Count + ~0] : default!;
			public int Count          => m_list.Count;
			public T Bottom           => m_list[0];
			public T Pop()            => m_list.PopBack();
			public void Clear()       => m_list.Clear();
			public void Push(T value) => m_list.Add(value);
			public string Description => $"Count = {Count} Top = {Top}";
		}
		private class StackPool<T>
		{
			public static StackPool<T> Instance { get; } = new StackPool<T>();
			private readonly ConcurrentBag<Stack<T>> m_pool = new();
			public Scope<Stack<T>> Alloc() => Scope.Create(() => Get(), x => Return(x));
			private Stack<T> Get() => m_pool.TryTake(out var s) ? s : new Stack<T>();
			private void Return(Stack<T> s) { s.Clear(); m_pool.Add(s); }
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Diagnostics;
	using System.Linq;
	using Container;
	using Extn;

	[TestFixture]
	public class TestRBTree
	{
		[DebuggerDisplay("{Description,nq}")]
		class Sorta :RedBlack_.IAccessors<Sorta>
		{
			public int m_value;
			private RedBlack_.EColour m_colour;
			private Sorta? m_left;
			private Sorta? m_right;
			private Sorta? m_parent;

			public Sorta(int i)
			{
				m_value = i;
				m_colour = RedBlack_.EColour.Red;
			}

			/// <inheritdoc/>
			int RedBlack_.IAccessors<Sorta>.Compare(Sorta lhs, Sorta rhs) => lhs.m_value.CompareTo(rhs.m_value);

			/// <inheritdoc/>
			RedBlack_.EColour RedBlack_.IAccessors<Sorta>.Colour(Sorta? elem)
			{
				return elem?.m_colour ?? RedBlack_.EColour.Black;
			}
			void RedBlack_.IAccessors<Sorta>.Colour(Sorta elem, RedBlack_.EColour colour)
			{
				elem.m_colour = colour;
			}

			/// <inheritdoc/>
			Sorta RedBlack_.IAccessors<Sorta>.Child(int side, Sorta? elem)
			{
				if (elem == null) return null!;
				return side < 0 ? elem.m_left! : elem.m_right!;
			}
			void RedBlack_.IAccessors<Sorta>.Child(int side, Sorta? elem, Sorta? child)
			{
				// This is how to maintain the parent relationship
				if (elem != null)
				{
					if (side < 0)
					{
						elem.m_left = child;
					}
					else
					{
						elem.m_right = child;
					}
				}
				if (child != null)
				{
					child.m_parent = elem;
				}
			}

			/// <summary></summary>
			public string Description
			{
				get
				{
					var s = string.Empty;
					s += m_left != null ? $"(L:{m_left.m_value} {m_left.m_colour})" : "(L:---)";
					s += $"  {m_value} {m_colour}  ";
					s += m_right  != null ? $"(R:{m_right.m_value} {m_right.m_colour})  "   : "(R:---)  ";
					return s;
				}
			}
			public static implicit operator Sorta(int i) { return new Sorta(i); }

			/// <summary></summary>
			public static void CheckTree(Sorta? node)
			{
				if (node == null) return;
				var ops = (RedBlack_.IAccessors<Sorta>)node;

				// Root node must be black
				if (node.m_parent == null)
				{
					Assert.Equal(node.m_colour, RedBlack_.EColour.Black);
				}

				// No red node has red children
				if (node.m_colour == RedBlack_.EColour.Red)
				{
					if (node.m_left is Sorta l)
						Assert.True(l.m_colour == RedBlack_.EColour.Black);
					if (node.m_right is Sorta r)
						Assert.True(r.m_colour == RedBlack_.EColour.Black);
				}

				// Check parent/child pointers
				if (node.m_parent != null)
				{
					var childL = ops.Child(-1, node.m_parent);
					var childR = ops.Child(+1, node.m_parent);
					for (; ; )
					{
						if (Equals(node, childL))
						{
							Assert.True(ops.Compare(node, node.m_parent) <= 0);
							break;
						}
						if (Equals(node, childR))
						{
							Assert.True(ops.Compare(node, node.m_parent) >= 0);
							break;
						}
						else
						{
							Assert.True(false);
						}
					}
				}
				if (node.m_left != null)
				{
					Assert.AreSame(ops.Child(-1, node), node.m_left);
					Assert.AreSame(node.m_left.m_parent, node);
					Assert.True(ops.Compare(node.m_left, node) <= 0);
				}
				if (node.m_right != null)
				{
					Assert.AreSame(ops.Child(+1, node), node.m_right);
					Assert.AreSame(node.m_right.m_parent, node);
					Assert.True(ops.Compare(node.m_right, node) >= 0);
				}

				// Recurse
				if (node.m_left != null)
				{
					CheckTree(node.m_left);
				}
				if (node.m_right != null)
				{
					CheckTree(node.m_right);
				}
			}
		}

		[Test]
		public void Insert()
		{
			var root = RedBlack_.Build(new Sorta[] { 3 });
			root = RedBlack_.Insert(root, 1); Sorta.CheckTree(root);
			root = RedBlack_.Insert(root, 5); Sorta.CheckTree(root);
			root = RedBlack_.Insert(root, 7); Sorta.CheckTree(root);
			root = RedBlack_.Insert(root, 6); Sorta.CheckTree(root);
			root = RedBlack_.Insert(root, 8); Sorta.CheckTree(root);
			root = RedBlack_.Insert(root, 9); Sorta.CheckTree(root);
			root = RedBlack_.Insert(root, 10); Sorta.CheckTree(root);

			var ordered0 = RedBlack_.EnumerateDF(root, +1).Select(x => x.m_value).ToList();
			Assert.True(ordered0.SequenceEqual(new[] { 1, 3, 5, 6, 7, 8, 9, 10 }));

			var ordered1 = RedBlack_.EnumerateDF(root, -1).Select(x => x.m_value).ToList();
			Assert.True(ordered1.SequenceEqual(new[] { 10, 9, 8, 7, 6, 5, 3, 1 }));
		}

		[Test]
		public void InsertWithDuplicates()
		{
			var root = RedBlack_.Build<Sorta>(0);
			var values = int_.Range(50).Select(i => ((i + 57) * 137) % 27).ToList();
			for (int i = 0; i != values.Count; ++i)
			{
				root = RedBlack_.Insert(root, values[i]);
				Sorta.CheckTree(root);
			}
			var ordered = RedBlack_.EnumerateDF(root, +1).Select(x => x.m_value).ToList();
			Assert.True(ordered.SequenceOrdered(ESequenceOrder.Increasing));
		}

		[Test]
		public void Delete()
		{
			{
				var root = RedBlack_.Build(new Sorta[] { 3, 1, 5, 7, 6, 8, 9, 10 });
				Sorta.CheckTree(root);
				root = RedBlack_.Delete(root, x => 8.CompareTo(x.m_value));
				Sorta.CheckTree(root);

			}
			{
				var root = RedBlack_.Build(new Sorta[] { 30, 20, 40, 10, 25});
				Sorta.CheckTree(root);
				root = RedBlack_.Delete(root, x => 30.CompareTo(x.m_value));
				Sorta.CheckTree(root);
			}
		}

		[Test]
		public void DeleteWithDuplicates()
		{
			var values = int_.Range(50).Select(i => ((i + 57) * 137) % 27).ToList();
			var root = RedBlack_.Build(values.Select(x => new Sorta(x)));
			Sorta.CheckTree(root);

			var ordered = RedBlack_.EnumerateDF(root, +1).Select(x => x.m_value).ToList();
			Assert.True(ordered.SequenceOrdered(ESequenceOrder.Increasing));

			for (int i = 0; values.Count != 0;)
			{
				i = ((i + 57) * 137) % values.Count;
				//if (i == 1) Debugger.Break();
				root = RedBlack_.Delete(root, x => values[i].CompareTo(x.m_value));
				Sorta.CheckTree(root);
				values.RemoveAt(i);
			}
			Assert.True(root == null);
		}

		/*
		var result = RedBlack_.Search(tree, 2, new[] { 5.0, 5.0 }, 1.1).Select(x => x.pos).ToList();
		result.Sort((l, r) =>
		{
			if (l.x != r.x) return l.x < r.x ? -1 : 1;
			return l.y < r.y ? -1 : 1;
		});

		Assert.Equal(result.Count, 5);
		Assert.Equal(result[0], new v2(4f, 5f));
		Assert.Equal(result[1], new v2(5f, 4f));
		Assert.Equal(result[2], new v2(5f, 5f));
		Assert.Equal(result[3], new v2(5f, 6f));
		Assert.Equal(result[4], new v2(6f, 5f));
		*/
	}
}
#endif