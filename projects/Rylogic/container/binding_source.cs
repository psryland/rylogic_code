using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace pr.container
{
	/// <summary>Type-safe version of BindingSource</summary>
	public class BindingSource<TItem> :BindingSource ,IEnumerable<TItem> ,IList<TItem>
	{
		private int m_impl_previous_position;
		private FieldInfo m_impl_listposition;

		public BindingSource() :base() { Init(); }
		public BindingSource(IContainer container) :base(container) { Init(); }
		public BindingSource(object dataSource, string dataMember) :base(dataSource, dataMember) { Init(); }
		private void Init()
		{
			m_impl_previous_position = -1;
			m_impl_listposition = typeof(CurrencyManager).GetField("listposition", BindingFlags.NonPublic|BindingFlags.Instance);
		}

		/// <summary>Get/Set whether the underlying list can be sorted by through this binding</summary>
		[Browsable(false)]
		public virtual bool AllowSort { get; set; }
		public override bool SupportsSorting
		{
			get { return base.SupportsSorting && AllowSort; }
		}

		/// <summary>Get/Set whether the Clear() method results in individual events for each item or just the PreReset/Reset events</summary>
		[Browsable(false)]
		public virtual bool PerItemClear
		{
			get { return DataSource is BindingListEx<TItem> && DataSource.As<BindingListEx<TItem>>().PerItemClear; }
			set
			{
				var bl = DataSource as BindingListEx<TItem>;
				if (bl == null) throw new Exception("PerItemClear only works for BindingListEx<> data sources");
				bl.PerItemClear = value;
			}
		}

		/// <summary>Set the current position (-1 means no current item)</summary>
		public new int Position
		{
			get { return base.Position; }
			set
			{
				// Allow setting the position to -1, meaning no current
				if (value == -1 && Position != -1)
				{
					m_impl_listposition.SetValue(CurrencyManager, -1);
					OnPositionChanged(EventArgs.Empty);
				}
				else if (value != -1 && Position == -1 && Count != 0)
				{
					m_impl_listposition.SetValue(CurrencyManager, -2);
					base.Position = value;
				}
				else
				{
					base.Position = value;
				}
			}
		}

		/// <summary>The current item</summary>
		public new TItem Current
		{
			get
			{
				return Position >= 0 && Position < Count ? (TItem)base.Current : default(TItem);
			}
			set
			{
				var idx = List.IndexOf(value);
				if ((idx < 0 || idx >= List.Count) && !Equals(value, default(TItem)))
					throw new IndexOutOfRangeException("Cannot set Current to a value that isn't in this collection");

				Position = idx;
			}
		}

		/// <summary>Access an item by index</summary>
		public new TItem this[int index]
		{
			get { return (TItem)base[index]; }
			set { base[index] = value; }
		}
		public TItem this[uint index]
		{
			get { return this[(int)index]; }
			set { this[(int)index] = value; }
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
				{
					bl.ListChanging -= RaiseListChanging;
					bl.ItemChanged  -= RaiseItemChanged;
				}
				var bs = base.DataSource as BindingSource<TItem>;
				if (bs != null)
				{
					bs.ListChanging -= RaiseListChanging;
					bs.ItemChanged  -= RaiseItemChanged;
				}

				// Set new data source
				// Note: this can cause a first chance exception in some controls
				// that are bound to this source (e.g. ListBox, ComboBox, etc).
				// They are handled internally, just continue.
				base.DataSource = null;
				base.DataSource = value;

				// Hookup
				bl = base.DataSource as BindingListEx<TItem>;
				if (bl != null)
				{
					bl.ListChanging += RaiseListChanging;
					bl.ItemChanged  += RaiseItemChanged;
				}
				bs = base.DataSource as BindingSource<TItem>;
				if (bs != null)
				{
					bs.ListChanging += RaiseListChanging;
					bs.ItemChanged  += RaiseItemChanged;
				}

				RaiseListChanging(this, new ListChgEventArgs<TItem>(ListChg.Reset, -1, default(TItem)));
			}
		}

		/// <summary>Raised *only* if 'DataSource' is a BindingListEx</summary>
		public event EventHandler<ListChgEventArgs<TItem>> ListChanging;
		private void RaiseListChanging(object sender, ListChgEventArgs<TItem> args)
		{
			// I don't know why, but signing this up directly, as in:
			// bl.ListChanging += ListChanging.Raise;
			// compiles, but doesn't work.
			ListChanging.Raise(sender, args);

			// If we're about to delete the current item, invalidate the previous position
			// since the 'OnPositionChanged','OnPositionChanging' events occur after the delete.
			if (args.ChangeType == ListChg.ItemPreRemove && args.Index == m_impl_previous_position)
				m_impl_previous_position = -1;
		}

		/// <summary>Raised *only* if 'DataSource' is a BindingListEx</summary>
		public event EventHandler<ItemChgEventArgs<TItem>> ItemChanged;
		private void RaiseItemChanged(object sender, ItemChgEventArgs<TItem> args)
		{
			ItemChanged.Raise(sender, args);
		}

		/// <summary>Raised after the current list position changes (includes the previous position)</summary>
		public event EventHandler<PositionChgEventArgs> PositionChanging;

		/// <summary>Position changed handler</summary>
		protected override void OnPositionChanged(EventArgs e)
		{
			// Note: PositionChanging with OldIndex = -1, and NewIndex = -1 happens when the current
			// position is deleted. Since this event occurs after the delete 'm_impl_previous_position'
			// has been set to -1.
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

		/// <summary>Reset bindings for 'item'</summary>
		public void ResetItem(TItem item)
		{
			var idx = List.IndexOf(item);
			if ((idx < 0 || idx >= List.Count) && !Equals(item, default(TItem)))
				throw new IndexOutOfRangeException("Cannot reset a value that isn't in this collection");

			ResetItem(idx);
		}

		/// <summary>Enumerate over data source elements</summary>
		public new IEnumerator<TItem> GetEnumerator()
		{
			foreach (var item in List)
				yield return (TItem)item;
		}

		#region IList<TItem>
		int IList<TItem>.IndexOf(TItem item)
		{
			return IndexOf(item);
		}
		void IList<TItem>.Insert(int index, TItem item)
		{
			Insert(index, item);
		}
		void ICollection<TItem>.Add(TItem item)
		{
			Add(item);
		}
		bool ICollection<TItem>.Contains(TItem item)
		{
			return Contains(item);
		}
		void ICollection<TItem>.CopyTo(TItem[] arr, int index)
		{
			CopyTo(arr, index);
		}
		bool ICollection<TItem>.Remove(TItem item)
		{
			if (!Contains(item)) return false;
			Remove(item);
			return true;
		}
		#endregion

		public override string ToString() { return "{0} Current: {1}".Fmt(Count, Current); }
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Collections.Generic;
	using System.Linq;
	using container;
	using maths;
	using util;

	[TestFixture] public class TestBindingSource
	{
		private static bool Contains<T>(List<T> list, params T[] items)
		{
			if (list.Count != items.Length) return false;
			for (int i = 0; i != list.Count; ++i)
				if (!Equals(list[i], items[i]))
					return false;
			return true;
		}

		[Test] public void BindingSource()
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
			    Assert.True(Contains(bl_evts, ListChg.ItemPreAdd, ListChg.ItemAdded));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreAdd, ListChg.ItemAdded));
				clear();

				bl.Remove(42);
			    Assert.True(Contains(bl_evts, ListChg.ItemPreRemove, ListChg.ItemRemoved));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreRemove, ListChg.ItemRemoved));
				clear();

				bl.Insert(0, 42);
				bl.Insert(0, 21);
			    Assert.True(Contains(bl_evts, ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreAdd, ListChg.ItemAdded));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreAdd, ListChg.ItemAdded));
				clear();

				bl.RemoveAt(0);
				bl.RemoveAt(0);
			    Assert.True(Contains(bl_evts, ListChg.ItemPreRemove, ListChg.ItemRemoved, ListChg.ItemPreRemove, ListChg.ItemRemoved));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreRemove, ListChg.ItemRemoved, ListChg.ItemPreRemove, ListChg.ItemRemoved));
				clear();

				i = bl.AddNew();
				bl.EndNew(i);
			    Assert.True(Contains(bl_evts, ListChg.ItemPreAdd, ListChg.ItemAdded));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreAdd, ListChg.ItemAdded));
				clear();

				i = bl.AddNew();
				bl.CancelNew(i);
			    Assert.True(Contains(bl_evts, ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreRemove, ListChg.ItemRemoved));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreRemove, ListChg.ItemRemoved));
				clear();

				bl.Add(1);
				bl.Add(2);
				bl.Add(3);
				bl.ResetBindings();
				bl.ResetItem(1);
				Assert.True(Contains(bl_evts
					,ListChg.ItemPreAdd   ,ListChg.ItemAdded
					,ListChg.ItemPreAdd   ,ListChg.ItemAdded
					,ListChg.ItemPreAdd   ,ListChg.ItemAdded
					,ListChg.PreReset     ,ListChg.Reset
					,ListChg.ItemPreReset ,ListChg.ItemReset));
				Assert.True(Contains(bs_evts
					,ListChg.ItemPreAdd   ,ListChg.ItemAdded
					,ListChg.ItemPreAdd   ,ListChg.ItemAdded
					,ListChg.ItemPreAdd   ,ListChg.ItemAdded
					,ListChg.PreReset     ,ListChg.Reset
					,ListChg.ItemPreReset ,ListChg.ItemReset));
				clear();
			}


			{// Binding Source Tests
				bs.Add(42);
				Assert.True(Contains(bl_evts, ListChg.ItemPreAdd, ListChg.ItemAdded));
				Assert.True(Contains(bs_evts, ListChg.ItemPreAdd, ListChg.ItemAdded));
				clear();

				bs.Remove(42);
			    Assert.True(Contains(bl_evts, ListChg.ItemPreRemove, ListChg.ItemRemoved));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreRemove, ListChg.ItemRemoved));
				clear();

				bs.Insert(0, 42);
				bs.Insert(0, 21);
			    Assert.True(Contains(bl_evts, ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreAdd, ListChg.ItemAdded));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreAdd, ListChg.ItemAdded, ListChg.ItemPreAdd, ListChg.ItemAdded));
				clear();

				bs.RemoveAt(0);
				bs.RemoveAt(0);
			    Assert.True(Contains(bl_evts, ListChg.ItemPreRemove, ListChg.ItemRemoved, ListChg.ItemPreRemove, ListChg.ItemRemoved));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreRemove, ListChg.ItemRemoved, ListChg.ItemPreRemove, ListChg.ItemRemoved));
				clear();

				bs.AddNew();
			    Assert.True(Contains(bl_evts, ListChg.ItemPreAdd, ListChg.ItemAdded));
			    Assert.True(Contains(bs_evts, ListChg.ItemPreAdd, ListChg.ItemAdded));
				clear();

				bs.Add(1);
				bs.Add(2);
				bs.Add(3);
				bs.RemoveCurrent();
			    Assert.True(Contains(bl_evts
				    ,ListChg.ItemPreAdd    ,ListChg.ItemAdded
					,ListChg.ItemPreAdd    ,ListChg.ItemAdded
					,ListChg.ItemPreAdd    ,ListChg.ItemAdded
					,ListChg.ItemPreRemove ,ListChg.ItemRemoved
					));
			    Assert.True(Contains(bs_evts
				    ,ListChg.ItemPreAdd    ,ListChg.ItemAdded
					,ListChg.ItemPreAdd    ,ListChg.ItemAdded
					,ListChg.ItemPreAdd    ,ListChg.ItemAdded
					,ListChg.ItemPreRemove ,ListChg.ItemRemoved
					));
				clear();

				bs.ResetCurrentItem();
				bs.ResetItem(1);
				bs.ResetBindings(false);
			    Assert.True(Contains(bl_evts));
			    Assert.True(Contains(bs_evts
					,ListChg.ItemPreReset ,ListChg.ItemReset
					,ListChg.ItemPreReset ,ListChg.ItemReset
					,ListChg.PreReset     ,ListChg.Reset
					));
				clear();

				bs.PerItemClear = true;
				bs.Clear();
				Assert.True(Contains(bl_evts
					,ListChg.PreReset
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.Reset
					));
				Assert.True(Contains(bs_evts
					,ListChg.PreReset
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.ItemPreRemove    ,ListChg.ItemRemoved
					,ListChg.Reset
					));
				clear();
			}
		}
		[Test] public void BindingSourceCurrency()
		{
			var positions = new List<int>();

			var bl = new BindingListEx<int>();
			bl.AddRange(new[]{1,2,3,4,5});

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

			bs.Current = default(int);
			Assert.True(bs.Position == -1);
			Assert.True(positions.SequenceEqual(new[]{0,3,3,1,1,4,4,-1}));

			bs.DataSource = null;
			Assert.True(positions.SequenceEqual(new[]{0,3,3,1,1,4,4,-1}));
		}
		[Test] public void BindingSourceEnumeration()
		{
			var arr = new[]{1,2,3,4,5};
			var bs = new BindingSource<int>{DataSource = arr};
			var res = bs.ToArray();
			Assert.True(arr.SequenceEqual(res));

			foreach (var i in bs)
				Assert.True(i != 0); // Checking type inference
		}
		[Test] public void BindingSourceChaining()
		{
			var bl = new BindingListEx<int>();
			var bs1 = new BindingSource<int>{DataSource = bl};
			var bs2 = new BindingSource<int>{DataSource = bs1};
			var bs3 = new BindingSource<int>{DataSource = bs2};
			var bs4 = new BindingSource<int>{DataSource = bs3};
			var bs5 = new BindingSource<int>{DataSource = bs4};

			var bl_evts = new List<ListChg>();
			var bs_evts = new List<ListChg>();
			bl .ListChanging += (s,a) => bl_evts.Add(a.ChangeType);
			bs5.ListChanging += (s,a) => bs_evts.Add(a.ChangeType);

			bl.Add(1);
			bl.Add(2);
			bl.Add(3);
			bl.Clear();

			Assert.True(bl_evts.Count != 0);
			Assert.True(bl_evts.SequenceEqual(bs_evts));
		}
	}
}
#endif
