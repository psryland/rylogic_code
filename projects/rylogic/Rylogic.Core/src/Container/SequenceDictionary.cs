using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rylogic.Extn;

namespace Rylogic.Common
{
	public class SequenceDictionary<TKey, TValue> : IDictionary<TKey[], TValue>
	{
		// Notes:
		//  - This structure is an associative map where the key length is a variable length array.
		//  - A use would be 
		//
		// Example:
		//  - Given arrays of bytes: [1,2,4,6], [1,2,3,5,7], [1,2,5], [1,6,7], etc...
		//    Constructs a tree where each node contains the common leading values, and the descendants are the divergent parts.
		//    Root
		//      +-[1]
		//         +-[2]
		//         |  + [4,6]
		//         |  + [3,5,7]
		//         |  + [5]
		//         + [6,7]

		/// <summary>Base of the tree</summary>
		private Node Root = Node.Root;

		/// <summary>The number of nodes containing values</summary>
		public int Count { get; private set; }

		/// <summary>Reset the collection</summary>
		public void Clear()
		{
			Root.Children.Clear();
			Count = 0;
		}

		/// <summary>True if 'key' is in the map</summary>
		public bool ContainsKey(Span<TKey> key)
		{
			// 'key' is in the tree if all of 'key' is matched with a node in the map
			var (_, len, _) = FindNode(key);
			return len == key.Length;
		}

		/// <summary>Add an item to the map</summary>
		public void Add(Span<TKey> key, TValue value) => Add(key, value, false);
		private void Add(Span<TKey> key, TValue value, bool overwrite)
		{
			// Find the best match for 'key'
			var (node, len, partial) = FindNode(key);

			// If 'partial' is 0, 'node' is either a match for 'key' or it should be the parent of 'key'.
			// 'node' does not need splitting. None of 'node's existing children match 'key', even partially.
			if (partial == 0)
			{
				// 'node' is a match for 'key', set 'value' as its value
				if (len == key.Length)
				{
					Count += node.HasValue ? 0 : 1; // Add to 'Count' if node didn't previously have a value
					node.SetValue(value, overwrite);
				}
				else // Otherwise, add the remainder of 'key' as a child of 'node'.
				{
					++Count;
					node.Children.Add(new Node(key.Slice(len), value)); // Remainder of 'key'
				}
			}

			// Otherwise, 'node' is a partial match for 'key'. The node needs splitting.
			else
			{
				// Split 'node' at 'partial'
				node.Split(partial);

				// If 'node' is now a match for 'key', set the value
				if (len + partial == key.Length)
				{
					++Count;
					node.SetValue(value, false); // 'node' shouldn't already have a value, it's just been split
				}
				else // Otherwise, add the remainder of 'key' as a child of 'node'.
				{
					++Count;
					node.Children.Add(new Node(key.Slice(len + partial), value)); // Remainder of 'key'
				}
			}
		}

		/// <summary>Remove 'key' from the map</summary>
		public bool Remove(Span<TKey> key)
		{
			// Recursively find 'key', remove it, then on the way back up, compress nodes where possible
			bool DoRemove(Span<TKey> key, int match_length, Node parent)
			{
				// Find a child that matches (probably partially).
				// Only one child should match one or more bytes otherwise the tree is corrupt.
				var child = (Node?)null; int len = 0;
				foreach (var c in parent.Children)
				{
					len = MatchLength(key, match_length, c.Key, 0);
					if (len == 0) continue;
					child = c;
					break;
				}

				// If no child matches 'key' then there's nothing to remove
				if (child == null)
					return false;

				// If key matches 'child', remove it
				if (match_length + len == key.Length)
				{
					child.ClearValue();
					DoPrune(child, parent);
					return true;
				}

				// Otherwise, if all of key is not yet matched, recurse
				var res = DoRemove(key, match_length + len, child);

				// If a node was removed, fix up the tree on the way back up
				if (res)
					DoPrune(child, parent);

				return res;
			}
			static void DoPrune(Node node, Node parent)
			{
				// If 'node' has no children and no value it can be removed.
				if (node.Children.Count == 0 && !node.HasValue)
				{
					parent.Children.Remove(node);
				}

				// If 'node' only has one child and no value, merge it up into 'child'
				if (node.Children.Count == 1 && !node.HasValue)
				{
					var brat = node.Children[0]; // node's only child

					// Merge 'brat' with 'node'
					node.Key = Join(node.Key, brat.Key);
					node.Children.Assign(brat.Children);
					if (brat.HasValue)
						node.SetValue(brat.Value, false);
				}
			}

			if (!DoRemove(key, 0, Root))
				return false;

			--Count;
			return true;
		}

		/// <summary>Look for 'key' in the map</summary>
		public bool TryGetValue(Span<TKey> key, out TValue value)
		{
			value = default!;
			var (node, len, _) = FindNode(key);
			if (len != key.Length)
				return false;

			value = node.Value;
			return true;
		}

		/// <summary>Indexer access</summary>
		public TValue this[Span<TKey> key]
		{
			get
			{
				var (node, len, _) = FindNode(key);
				if (len != key.Length) throw new KeyNotFoundException($"Key is not in the map");
				return node.Value;
			}
			set
			{
				Add(key, value, true);
			}
		}

		/// <summary>Return all the keys for nodes with values</summary>
		public IEnumerable<TKey[]> Keys
		{
			get
			{
				var keys = new List<TKey[]>();
				void DoKeys(Node parent)
				{
					if (parent.HasValue)
						keys.Add(parent.Key);

					foreach (var child in parent.Children)
						DoKeys(child);
				}
				DoKeys(Root);
				return keys;
			}
		}
		public IEnumerable<TValue> Values
		{
			get
			{
				var values = new List<TValue>();
				void DoValues(Node parent)
				{
					if (parent.HasValue)
						values.Add(parent.Value);

					foreach (var child in parent.Children)
						DoValues(child);
				}
				DoValues(Root);
				return values;
			}
		}

		/// <summary>Enumerable key/value pairs</summary>
		public IEnumerator<KeyValuePair<TKey[], TValue>> GetEnumerator()
		{
			var queue = new Queue<Node>(new[] { Root });
			for (; queue.Count != 0;)
			{
				var node = queue.Dequeue();
				foreach (var child in node.Children)
					queue.Enqueue(child);
				if (node.HasValue)
					yield return new KeyValuePair<TKey[], TValue>(node.Key.ToArray(), node.Value);
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>Return the (key,value) pair that is the nearest match to 'key'. Returns true if 'pair.Value' is valid</summary>
		public bool Nearest(Span<TKey> key, out KeyValuePair<TKey[], TValue> pair)
		{
			var (node, len, _) = FindNode(key);
			pair = new KeyValuePair<TKey[], TValue>(key.Slice(0, len).ToArray(), node.Value);
			return len == key.Length;
		}

		/// <summary>
		/// Finds the lowest node that partially matches 'key'.
		/// Returns the lowest node, the match length in 'key', and the match length in the returned node.</summary>
		private (Node, int, int) FindNode(Span<TKey> key)
		{
			// Search for a matching Node, or the node that 'key' would be added to
			int match_length = 0;
			for (var node = Root; ;)
			{
				// Find a child that (partially) matches. Only one node should match one or more bytes.
				var child = (Node?)null; int len = 0;
				foreach (var c in node.Children)
				{
					len = MatchLength(key, match_length, c.Key, 0);
					if (len == 0) continue;
					child = c;
					break;
				}

				// If no child matches, 'key' would be added to 'node'
				if (child == null)
					return (node, match_length, 0);

				// If 'child' is a partial match, 'child' would be split at 'len',
				// and the non-common parts would be children of 'child'
				if (len != child.Key.Length)
					return (child, match_length, len);

				// Otherwise, 'key' completely matches 'child.Key', so carry on down the tree
				node = child;
				match_length += len;
			}
		}

		/// <summary>Return 'lhs' and 'rhs' concatenated</summary>
		private static TKey[] Join(TKey[] lhs, TKey[] rhs)
		{
			var key = new TKey[lhs.Length + rhs.Length];
			lhs.CopyTo(key, 0);
			rhs.CopyTo(key, lhs.Length);
			return key;
		}

		/// <summary>Return the number of bytes in common starting from lhs[i] and rhs[j]</summary>
		private static int MatchLength(Span<TKey> lhs, int i, Span<TKey> rhs, int j)
		{
			var count = 0;
			for (; i != lhs.Length && j != rhs.Length && Equals(lhs[i], rhs[j]); ++i, ++j, ++count) { }
			return count;
		}

		/// <summary>Debugging helper to check for an invalid tree</summary>
		public Exception? SanityCheck()
		{
			Exception? DoSanityCheck(Node node)
			{
				// No node should have no value and no children
				if (!node.HasValue && node.Children.Count == 0)
					return new Exception("Leaf node with no value found");

				// No node should have no value and only one child
				if (!node.HasValue && node.Children.Count == 1)
					return new Exception("Valueless node with one child found. Should be merged");

				return null;
			}
			return DoSanityCheck(Root);
		}

		/// <summary>Debugging helper</summary>
		public string DumpTree()
		{
			var str = new StringBuilder();
			void Print(Node node, int lvl)
			{
				var indent = new string(' ', 4 * lvl);
				var key = node == Root ? $"ROOT ({Count})" : string.Join("", node.Key.Select(x => x!.ToString()));
				var value = node.HasValue ? $"-[{node.Value}]" : string.Empty;
				
				str.AppendLine($"{indent}{key}{value}");
					
				foreach (var c in node.Children)
					Print(c, lvl + 1);
			}
			Print(Root, 0);
			str.AppendLine();
			return str.ToString();
		}

		/// <summary></summary>
		[DebuggerDisplay("{Desc,nq}")]
		private class Node
		{
			private Node() { }
			public Node(Span<TKey> key)
				: this()
			{
				Key = key.ToArray();
			}
			public Node(Span<TKey> key, TValue value)
				: this()
			{
				Key = key.ToArray();
				SetValue(value, false);
			}

			/// <summary>The unique remainder of the key. The full key is found by concatenating all keys down to this node</summary>
			public TKey[] Key
			{
				get;
				set
				{
					// Except for the root node
					if (value.Length == 0)
						throw new ArgumentException("Key length must have at least one element");

					field = value.ToArray();
				}
			} = Array.Empty<TKey>();

			/// <summary>The value associated with this node. Only valid if 'Children' is empty</summary>
			public TValue Value { get; private set; } = default!;

			/// <summary>True if 'Value' is valid</summary>
			public bool HasValue { get; private set; }

			/// <summary>Set/Clear the value of this node</summary>
			public void SetValue(TValue value, bool overwrite)
			{
				if (!overwrite && HasValue)
					throw new Exception("Key already exists");

				Value = value;
				HasValue = true;
			}
			public void ClearValue()
			{
				Value = default!;
				HasValue = false;
			}

			/// <summary>Child nodes</summary>
			public List<Node> Children { get; } = new List<Node>();

			/// <summary>Split this node at 'len', adding the remainder as a child</summary>
			public void Split(int len)
			{
				if (len == 0) throw new Exception("Can't split a node at index 0");
				var child = new Node(Key.Slice(len));
				if (HasValue) child.SetValue(Value, false);
				child.Children.AddRange(Children);

				Key = Key.Slice(0, len).ToArray();
				ClearValue();
				Children.Clear();
				Children.Add(child);
			}

			/// <summary>Debugging description of this node</summary>
			public string Desc => $"{string.Join("", Key)} = {(HasValue ? Value : "<no value>")}";

			/// <summary>Special case root node</summary>
			public static readonly Node Root = new();
		}

		#region IDictionary
		bool ICollection<KeyValuePair<TKey[], TValue>>.IsReadOnly => false;
		bool ICollection<KeyValuePair<TKey[], TValue>>.Contains(KeyValuePair<TKey[], TValue> item)
		{
			return ContainsKey(item.Key);
		}
		void ICollection<KeyValuePair<TKey[], TValue>>.Add(KeyValuePair<TKey[], TValue> item)
		{
			Add(item.Key, item.Value);
		}
		bool ICollection<KeyValuePair<TKey[], TValue>>.Remove(KeyValuePair<TKey[], TValue> item)
		{
			return Remove(item.Key);
		}
		void ICollection<KeyValuePair<TKey[], TValue>>.CopyTo(KeyValuePair<TKey[], TValue>[] array, int arrayIndex)
		{
			foreach (var kv in this)
				array[arrayIndex++] = kv;
		}
		bool IDictionary<TKey[], TValue>.ContainsKey(TKey[] key)
		{
			return ContainsKey(key);
		}
		bool IDictionary<TKey[], TValue>.TryGetValue(TKey[] key, out TValue value)
		{
			return TryGetValue(key, out value);
		}
		void IDictionary<TKey[], TValue>.Add(TKey[] key, TValue value)
		{
			Add(key, value);
		}
		bool IDictionary<TKey[], TValue>.Remove(TKey[] key)
		{
			return Remove(key);
		}
		TValue IDictionary<TKey[], TValue>.this[TKey[] key]
		{
			get => this[key];
			set => this[key] = value;
		}
		ICollection<TKey[]> IDictionary<TKey[], TValue>.Keys
		{
			get => Keys.ToArray();
		}
		ICollection<TValue> IDictionary<TKey[], TValue>.Values
		{
			get => Values.ToArray();
		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Diagnostics;
	using Common;

	[TestFixture]
	public class TestStringDictionary
	{
		public class Stuff
		{
			[DebuggerStepThrough] public Stuff(string val) => Smell = val;
			public string Smell { get; }
			public override string ToString() => $"{Smell}";
		}

		[Test]
		public void StandardUse()
		{
			var strings = new[] { "12345", "2345", "12678", "12", "234567", "23678", "234" };
			var dick = new SequenceDictionary<char, Stuff>();

			// Add data
			{
				foreach (var s in strings)
					dick.Add(s.ToCharArray(), new Stuff(s));

				//Console.Write(dick.DumpTree());
				Assert.Equal(strings.Length, dick.Count);
				foreach (var s in strings)
					Assert.True(dick.ContainsKey(s.ToCharArray()));
			}

			// Remove data
			{
				dick.Remove("2345".ToCharArray());

				//Console.Write(dick.DumpTree());
				Assert.Equal(strings.Length - 1, dick.Count);
				Assert.False(dick.ContainsKey("2345".ToCharArray()));

				dick.Remove("234567".ToCharArray());

				//Console.Write(dick.DumpTree());
				Assert.Equal(strings.Length - 2, dick.Count);
				Assert.False(dick.ContainsKey("234567".ToCharArray()));
			}

			// Search tests
			{
				Assert.True(dick.Nearest("12678".ToCharArray(), out var p0));
				Assert.Equal("12678", p0.Value.Smell);

				Assert.False(dick.Nearest("12678;".ToCharArray(), out var p1));
				Assert.Equal("12678", new string(p1.Key));
				Assert.Equal("12678", p1.Value.Smell);

				Assert.False(dick.Nearest("2345".ToCharArray(), out var p2));
				Assert.Equal("234", new string(p2.Key));
				Assert.Equal("234", p2.Value.Smell);
			}
		}
		[Test]
		public void Thrash()
		{
			var dick = new SequenceDictionary<char, Stuff>();
			var rng = new Random(42);
			for (int i = 0; i != 1000; ++i)
			{
				var chars = Enumerable.Range(0, rng.Next(1, 10)).Select(i => (char)rng.Next('0', '9')).ToArray();
				if (!dick.ContainsKey(chars))
					dick.Add(chars, new Stuff(new string(chars)));
				else
					dick.Remove(chars);
			}

			//Console.Write(dick.DumpTree());
			Assert.True(dick.SanityCheck() == null);
		}
	}
}
#endif