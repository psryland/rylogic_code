using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using pr.extn;
using pr.util;

namespace pr.container
{
	/// <summary>Extension to BindingList that notifies *before* an item is removed</summary>
	[DataContract] public class BindingListEx<T> :System.ComponentModel.BindingList<T>
	{
		public BindingListEx() :base()
		{
			Init();
		}
		public BindingListEx(IList<T> list) :base(list)
		{
			Init();
		}
		public BindingListEx(int initial_count, Func<int,T> gen)
		{
			Init();
			for (int i = 0; initial_count-- != 0;)
				Add(gen(i++));
		}
		public BindingListEx(int initial_count, T value = default(T))
		{
			Init();
			for (;initial_count-- != 0;)
				Add(value);
		}
		private void Init()
		{
			// ResetBindings and ResetItem aren't overridable.
			// Attach handlers to ensure we always receive the Reset event.
			// Calling the 'new' method will cause the Pre events to be raised as well
			ListChanged += (s,a) =>
				{
					if (a.ListChangedType == ListChangedType.Reset)
						ListChanging.Raise(this, new ListChgEventArgs<T>(ListChg.Reset, -1, default(T)));
					if (a.ListChangedType == ListChangedType.ItemChanged)
						ListChanging.Raise(this, new ListChgEventArgs<T>(ListChg.ItemReset, a.NewIndex, this[a.NewIndex]));
				};
		}

		/// <summary>Get/Set readonly for this list</summary>
		public bool ReadOnly
		{
			get { return !AllowNew && !AllowEdit && !AllowRemove; }
			set { AllowNew = AllowEdit = AllowRemove = !value; }
		}

		/// <summary>Raised whenever items are added or about to be removed from the list</summary>
		public event EventHandler<ListChgEventArgs<T>> ListChanging;

		/// <summary>Raised whenever an element in the list is changed</summary>
		public event EventHandler<ItemChgEventArgs<T>> ItemChanged;

		/// <summary>Removes all items from the list</summary>
		protected override void ClearItems()
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.PreClear, -1, default(T));
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.PreReset, -1, default(T));
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}

			// Reset event is raised from attached handler
			base.ClearItems();

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<T>(ListChg.Clear, -1, default(T)));
		}

		/// <summary>Inserts the specified item in the list at the specified index.</summary>
		protected override void InsertItem(int index, T item)
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.ItemPreAdd, -1, item);
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}

			// Note: you can get first chance exceptions here when the list is bound to a BindingSource
			// that is bound to a combo box or list box. It happens when the list goes to/from empty
			// and is just shoddiness in the windows controls.
			base.InsertItem(index, item);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<T>(ListChg.ItemAdded, index, item));
		}

		/// <summary>Removes the item at the specified index.</summary>
		protected override void RemoveItem(int index)
		{
			var item = this[index];
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.ItemPreRemove, index, item);
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}
			
			base.RemoveItem(index);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<T>(ListChg.ItemRemoved, -1, item));
		}
		
		/// <summary>Replaces the item at the specified index with the specified item.</summary>
		protected override void SetItem(int index, T item)
		{
			var old = this[index];

			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.ItemPreRemove, index, old);
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.ItemPreAdd, index, item);
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}

			base.SetItem(index, item);

			if (RaiseListChangedEvents)
				ItemChanged.Raise(this, new ItemChgEventArgs<T>(index, old, item));

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<T>(ListChg.ItemRemoved, index, old));
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<T>(ListChg.ItemAdded, index, item));
		}

		/// <summary>Reorders the list</summary>
		protected override void ApplySortCore(System.ComponentModel.PropertyDescriptor prop, System.ComponentModel.ListSortDirection direction)
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.PreReordered, -1, default(T));
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}
			
			base.ApplySortCore(prop, direction);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs<T>(ListChg.Reordered, -1, default(T)));
		}

		/// <summary>Notify observers of the entire list changing</summary>
		public new void ResetBindings()
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.PreReset, -1, default(T));
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}

			// Reset event is raised from attached handler
			base.ResetBindings();
		}
		
		/// <summary>Notify observers of a specific item changing</summary>
		public new void ResetItem(int position)
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(ListChg.ItemPreReset, position, this[position]);
				ListChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}

			// Reset event is raised from attached handler
			base.ResetItem(position);
		}
	
		/// <summary>Notify observers of a specific item changing</summary>
		public void ResetItem(T item)
		{
			var idx = IndexOf(item);
			if (idx < 0 || idx >= Count) throw new Exception("Item is not within this container");
			ResetItem(idx);
		}

		/// <summary>RAII object for suspending list events</summary>
		public Scope SuspendEvents(bool reset_bindings_on_resume = false)
		{
			return Scope.Create(
				() =>
				{
					RaiseListChangedEvents = false;
				},
				() =>
				{
					RaiseListChangedEvents = true;
					if (reset_bindings_on_resume)
						ResetBindings();
				});
		}
	}
}


#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Collections.Generic;
	using System.Linq;
	using container;

	[TestFixture] public class TestBindingListEx
	{
		[Test] public void BindingList()
			{
				var a0 = new BindingListEx<double>(5, i => 2.0);
				var a1 = new BindingListEx<double>(5, i =>
					{
						return i + 1.0;
					});
			Assert.True(a0.SequenceEqual(new[]{2.0, 2.0, 2.0, 2.0, 2.0}));
			Assert.True(a1.SequenceEqual(new[]{1.0, 2.0, 3.0, 4.0, 5.0}));
		}
	}
}
#endif
