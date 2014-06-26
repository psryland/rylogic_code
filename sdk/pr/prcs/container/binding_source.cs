using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace pr.container
{
	/// <summary>Type-safe version of BindingSource</summary>
	public class BindingSource<TItem> :BindingSource ,IEnumerable<TItem>
	{
		private int m_impl_previous_position;

		public BindingSource() :base() { Init(); }
		public BindingSource(IContainer container) :base(container) { Init(); }
		public BindingSource(object dataSource, string dataMember) :base(dataSource, dataMember) { Init(); }
		private void Init()
		{
			m_impl_previous_position = -1;
		}

		/// <summary>The current item</summary>
		public new TItem Current
		{
			get { return (TItem)base.Current; }
			set
			{
				var idx = List.IndexOf(value);
				if (idx < 0 || idx >= List.Count) throw new IndexOutOfRangeException("Cannot set Current to a value that isn't in this collection");
				CurrencyManager.Position = idx;
			}
		}

		/// <summary>Access an item by index</summary>
		public new TItem this[int index]
		{
			get { return (TItem)base[index]; }
			set { base[index] = value; }
		}

		/// <summary>Get/Set the data source</summary>
		public new object DataSource
		{
			get { return base.DataSource; }
			set
			{
				if (value == base.DataSource) return;

				RaiseListChanging(this, new ListChgEventArgs<TItem>(ListChg.PreReset, -1, default(TItem)));

				// Unhook
				var bl = base.DataSource as BindingListEx<TItem>;
				if (bl != null)
					bl.ListChanging -= RaiseListChanging;

				// Set new data source
				base.DataSource = null;
				base.DataSource = value;

				// Hookup
				bl = base.DataSource as BindingListEx<TItem>;
				if (bl != null)
					bl.ListChanging += RaiseListChanging;

				RaiseListChanging(this, new ListChgEventArgs<TItem>(ListChg.Reset, -1, default(TItem)));
			}
		}

		/// <summary>Raised *only* if 'DataSource' is a BindingListEx</summary>
		public event EventHandler<ListChgEventArgs<TItem>> ListChanging;
		private void RaiseListChanging(object sender = null, ListChgEventArgs<TItem> args = null)
		{
			// I don't know why, but signing this up directly, as in:
			// bl.ListChanging += ListChanging.Raise;
			// compiles, but doesn't work.
			ListChanging.Raise(sender, args);
		}

		/// <summary>Raised just before the current list position changes</summary>
		public event EventHandler<PositionChgEventArgs> PositionChanging;

		/// <summary>Position changed handler</summary>
		protected override void OnPositionChanged(EventArgs e)
		{
			PositionChanging.Raise(this, new PositionChgEventArgs(m_impl_previous_position, Position));
			m_impl_previous_position = Position;
			base.OnPositionChanged(e);
		}

		// Note:
		// Reset methods invoked on this object do not cause the equivalent
		// reset method on the underlying object to be called. Calling a 
		// reset method on the underlying list however does cause the reset
		// method on the binding source to be called, however, it calls it
		// on BindingSource, not BindingSource<>. When a BindingListEx is
		// bound to DataSource its ListChanging events after forwarded on,
		// These 'new' methods raise the list changing events only when reset
		// methods are called on this object.

		/// <summary>Notify observers of a complete data reset</summary>
		public new void ResetBindings(bool metadata_changed)
		{
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<TItem>(ListChg.PreReset, -1, default(TItem)));

			base.ResetBindings(metadata_changed);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<TItem>(ListChg.Reset, -1, default(TItem)));
		}

		/// <summary>Causes a control bound to this BindingSource to reread the currently selected item and refresh its displayed value.</summary>
		public new void ResetCurrentItem()
		{
			var item = Current;
			var index = Position;

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<TItem>(ListChg.ItemPreReset, index, item));

			base.ResetCurrentItem();

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<TItem>(ListChg.ItemReset, index, item));
		}

		/// <summary>Causes a control bound to this BindingSource to reread the item at the specified index, and refresh its displayed value.</summary>
		public new void ResetItem(int itemIndex)
		{
			var item = this[itemIndex];

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<TItem>(ListChg.ItemPreReset, itemIndex, item));

			base.ResetItem(itemIndex);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<TItem>(ListChg.ItemReset, itemIndex, item));
		}

		/// <summary>Enumerate over data source elements</summary>
		IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
		{
			foreach (var item in List)
				yield return (TItem)item;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using System.Collections.Generic;
	using System.Linq;
	using NUnit.Framework;
	using container;
	using maths;
	using util;

	[TestFixture] internal static partial class UnitTests
	{
		public static bool Contains<T>(this List<T> list, params T[] items)
		{
			if (list.Count != items.Length) return false;
			for (int i = 0; i != list.Count; ++i)
				if (!Equals(list[i], items[i]))
					return false;
			return true;
		}
		internal static class TestBindingSource
		{
			[Test] public static void BindingSource()
			{
				var bl_evts = new List<ListChg>();
				var bs_evts = new List<ListChg>();
				
				var bl = new BindingListEx<int>();
				bl.ListChanging += (s,a) =>
					{
						bl_evts.Add(a.ChangeType);
					};
				var bs = new BindingSource<int>{DataSource = bl};
				bs.ListChanging += (s,a) =>
					{
						bs_evts.Add(a.ChangeType);
					};

				Action clear = () =>
					{
						bl_evts.Clear();
						bs_evts.Clear();
					};

				int i;
				{// List Tests
					bl.Add(42);
					Assert.True(bl_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded));
					Assert.True(bs_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded));
					clear();

					bl.Remove(42);
					Assert.True(bl_evts.Contains(ListChg.ItemPreRemove, ListChg.ItemRemoved));
					Assert.True(bs_evts.Contains(ListChg.ItemPreRemove, ListChg.ItemRemoved));
					clear();

					bl.Insert(0, 42);
					bl.Insert(0, 21);
					Assert.True(bl_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreAdd, ListChg.ItemAdded));
					Assert.True(bs_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreAdd, ListChg.ItemAdded));
					clear();

					bl.RemoveAt(0);
					bl.RemoveAt(0);
					Assert.True(bl_evts.Contains(ListChg.ItemPreRemove, ListChg.ItemRemoved, ListChg.ItemPreRemove, ListChg.ItemRemoved));
					Assert.True(bs_evts.Contains(ListChg.ItemPreRemove, ListChg.ItemRemoved, ListChg.ItemPreRemove, ListChg.ItemRemoved));
					clear();

					i = bl.AddNew();
					bl.EndNew(i);
					Assert.True(bl_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded));
					Assert.True(bs_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded));
					clear();

					i = bl.AddNew();
					bl.CancelNew(i);
					Assert.True(bl_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreRemove, ListChg.ItemRemoved));
					Assert.True(bs_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreRemove, ListChg.ItemRemoved));
					clear();
				
					bl.Clear();
					Assert.True(bl_evts.Contains(ListChg.PreClear, ListChg.PreReset, ListChg.Reset, ListChg.Clear));
					Assert.True(bs_evts.Contains(ListChg.PreClear, ListChg.PreReset, ListChg.Reset, ListChg.Clear));
					clear();

					bl.Add(1);
					bl.Add(2);
					bl.Add(3);
					bl.ResetBindings();
					bl.ResetItem(1);
					Assert.True(bl_evts.Contains
						(ListChg.ItemPreAdd   ,ListChg.ItemAdded
						,ListChg.ItemPreAdd   ,ListChg.ItemAdded
						,ListChg.ItemPreAdd   ,ListChg.ItemAdded
						,ListChg.PreReset     ,ListChg.Reset
						,ListChg.ItemPreReset ,ListChg.ItemReset));
					Assert.True(bs_evts.Contains
						(ListChg.ItemPreAdd   ,ListChg.ItemAdded
						,ListChg.ItemPreAdd   ,ListChg.ItemAdded
						,ListChg.ItemPreAdd   ,ListChg.ItemAdded
						,ListChg.PreReset     ,ListChg.Reset
						,ListChg.ItemPreReset ,ListChg.ItemReset));
					clear();
				}


				{// Binding Source Tests
					bs.Add(42);
					Assert.True(bl_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded));
					Assert.True(bs_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded));
					clear();

					bs.Remove(42);
					Assert.True(bl_evts.Contains(ListChg.ItemPreRemove, ListChg.ItemRemoved));
					Assert.True(bs_evts.Contains(ListChg.ItemPreRemove, ListChg.ItemRemoved));
					clear();

					bs.Insert(0, 42);
					bs.Insert(0, 21);
					Assert.True(bl_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreAdd, ListChg.ItemAdded));
					Assert.True(bs_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreAdd, ListChg.ItemAdded));
					clear();

					bs.RemoveAt(0);
					bs.RemoveAt(0);
					Assert.True(bl_evts.Contains(ListChg.ItemPreRemove, ListChg.ItemRemoved, ListChg.ItemPreRemove, ListChg.ItemRemoved));
					Assert.True(bs_evts.Contains(ListChg.ItemPreRemove, ListChg.ItemRemoved, ListChg.ItemPreRemove, ListChg.ItemRemoved));
					clear();

					bs.AddNew();
					Assert.True(bl_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded));
					Assert.True(bs_evts.Contains(ListChg.ItemPreAdd, ListChg.ItemAdded));
					clear();

					bs.Clear();
					Assert.True(bl_evts.Contains(ListChg.PreClear, ListChg.PreReset, ListChg.Reset, ListChg.Clear));
					Assert.True(bs_evts.Contains(ListChg.PreClear, ListChg.PreReset, ListChg.Reset, ListChg.Clear));
					clear();
					
					bs.Add(1);
					bs.Add(2);
					bs.Add(3);
					bs.RemoveCurrent();
					Assert.True(bl_evts.Contains
						(ListChg.ItemPreAdd    ,ListChg.ItemAdded
						,ListChg.ItemPreAdd    ,ListChg.ItemAdded
						,ListChg.ItemPreAdd    ,ListChg.ItemAdded
						,ListChg.ItemPreRemove ,ListChg.ItemRemoved
						));
					Assert.True(bs_evts.Contains
						(ListChg.ItemPreAdd    ,ListChg.ItemAdded
						,ListChg.ItemPreAdd    ,ListChg.ItemAdded
						,ListChg.ItemPreAdd    ,ListChg.ItemAdded
						,ListChg.ItemPreRemove ,ListChg.ItemRemoved
						));
					clear();

					bs.ResetCurrentItem();
					bs.ResetItem(1);
					bs.ResetBindings(false);
					Assert.True(bl_evts.Contains());
					Assert.True(bs_evts.Contains
						(ListChg.ItemPreReset ,ListChg.ItemReset
						,ListChg.ItemPreReset ,ListChg.ItemReset
						,ListChg.PreReset     ,ListChg.Reset
						));
					clear();
				}
			}
			[Test] public static void BindingSourceCurrency()
			{
				var bl = new BindingListEx<int>();
				bl.AddRange(new[]{1,2,3,4,5});

				var positions = new List<int>();
				var bs = new BindingSource<int>{DataSource = bl};
				bs.PositionChanging += (s,a) =>
					{
						positions.Add(a.OldIndex);
						positions.Add(a.NewIndex);
					};

				bs.Position = 3;
				bs.Position = 1;
				Assert.True(positions.SequenceEqual(new[]{0,3,3,1}));

				bs.CurrencyManager.Position = 4;
				Assert.True(positions.SequenceEqual(new[]{0,3,3,1,1,4}));

				bs.DataSource = null;
				Assert.True(positions.SequenceEqual(new[]{0,3,3,1,1,4,4,-1,-1,-1}));
			}
			[Test] public static void BindingSourceEnumeration()
			{
				var arr = new[]{1,2,3,4,5};
				var bs = new BindingSource<int>{DataSource = arr};
				var res = bs.ToArray();
				Assert.True(arr.SequenceEqual(res));
			}
		}
	}
}
#endif