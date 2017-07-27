using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace pr.container
{
	/// <summary>Type-safe version of BindingSource</summary>
	public class BindingSource<TItem>
		:Component, IEnumerable<TItem>, IList<TItem>, IBindingListView, IBindingList, IList,
		ICollection, IEnumerable, ITypedList, ICancelAddNew, ISupportInitializeNotification,
		ISupportInitialize, ICurrencyManagerProvider, IListChanging<TItem>, IItemChanged<TItem>
	{
		// Notes:
		// Reset methods invoked on this object do not cause the equivalent
		// reset method on the underlying object to be called. Calling a 
		// reset method on the underlying list however does cause the reset
		// method on the binding source to be called, however, it calls it
		// on BindingSource, not BindingSource<>. When a BindingListEx is
		// bound to DataSource its ListChanging events after forwarded on,
		// These 'new' methods raise the list changing events only when reset
		// methods are called on this object.

		public BindingSource()
		{
			Init(new BSource(this));
		}
		public BindingSource(IContainer container)
		{
			Init(new BSource(this, container));
		}
		public BindingSource(object dataSource, string dataMember)
		{
			Init(new BSource(this, dataSource, dataMember));
		}
		protected override void Dispose(bool disposing)
		{
			m_bs.Dispose();
			base.Dispose(disposing);
		}
		private void Init(BSource bs)
		{
			m_bs = bs;
			AllowNoCurrent = false;
		}

		/// <summary>The BindingSource providing the implementation</summary>
		private BSource m_bs;
		private class BSource :BindingSource
		{
			private BindingSource<TItem> This;
			public int m_impl_previous_position;
			public FieldInfo m_impl_listposition;

			public BSource(BindingSource<TItem> @this) { Init(@this); }
			public BSource(BindingSource<TItem> @this, IContainer container) :base(container) { Init(@this); }
			public BSource(BindingSource<TItem> @this, object dataSource, string dataMember) :base(dataSource, dataMember) { Init(@this); }
			private void Init(BindingSource<TItem> @this)
			{
				This = @this; 
				m_impl_previous_position = -1;
				m_impl_listposition = typeof(CurrencyManager).GetField("listposition", BindingFlags.NonPublic|BindingFlags.Instance);

				// CurrencyManager.Current
				// I *REALLY* wanted to replace the CurrencyManager.Current property to return null instead of throwing
				// when Position == -1, however I haven't found a way to do this yet. This exception causes DataGridView
				// to fail when 'AllowNoCurrent' is true. However, it seems overriding 'SetCurrentCellAddressCore' is a
				// work around.
			}
			public new int Position
			{
				get { return base.Position; }
				set
				{
					// Allow setting the position to -1, meaning no current
					if (value == base.Position)
					{
					}
					else if (value == -1 && Position != -1 && This.AllowNoCurrent)
					{
						m_impl_listposition.SetValue(base.CurrencyManager, -1);
						OnPositionChanged(EventArgs.Empty);
					}
					else if (value != -1 && Position == -1 && Count != 0)
					{
						// Setting 'listposition' to 0 then setting 'Position' generates a single PositionChanged event
						// when value != 0. However, when value == 0 there is no event. If I set 'listposition' to -2, an
						// extra event gets generated (-1->0, 0->value). I'm opting to manual raise the event for the value == 0 case
						m_impl_listposition.SetValue(base.CurrencyManager, 0);
						base.Position = value;
						if (value == 0) OnPositionChanged(EventArgs.Empty);
					}
					else
					{
						base.Position = value;
					}
				}
			}
			public new object Current
			{
				get { return Position >= 0 && Position < Count ? (TItem)base.Current : default(TItem); }
			}
			protected override void OnPositionChanged(EventArgs e)
			{
				// Set the previous position before calling the event
				// because it may recursively cause another position change.
				var prev_position = m_impl_previous_position;
				m_impl_previous_position = Position;

				// Raise the improved version of PositionChanged
				This.RaisePositionChanged(this, new PositionChgEventArgs(prev_position, Position));
				base.OnPositionChanged(e);
			}
		}

		/// <summary>Gets/sets the list element at the specified index.</summary>
		/// <param name="index">The zero-based index of the element to retrieve.</param>
		/// <returns>The element at the specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">index is less than zero or is equal to or greater than System.Windows.Forms.BindingSource.Count.</exception>
		[Browsable(false)]
		public TItem this[int index]
		{
			get { return (TItem)m_bs[index]; }
			set { m_bs[index] = value; }
		}
		public TItem this[uint index]
		{
			get { return this[(int)index]; }
			set { this[(int)index] = value; }
		}
		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (TItem)value; }
		}

		/// <summary>Gets a value indicating whether items in the underlying list can be edited.</summary>
		/// <returns>true to indicate list items can be edited; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool AllowEdit
		{
			get { return m_bs.AllowEdit; }
		}

		/// <summary>Gets or sets a value indicating whether the System.Windows.Forms.BindingSource.AddNew method can be used to add items to the list.</summary>
		/// <returns>true if System.Windows.Forms.BindingSource.AddNew can be used to add items to the list; otherwise, false.</returns>
		/// <exception cref="InvalidOperationException">This property is set to true when the underlying list represented by the System.Windows.Forms.BindingSource.List property has a fixed size or is read-only.</exception>
		/// <exception cref="MissingMethodException">The property is set to true and the System.Windows.Forms.BindingSource.AddingNew event is not handled when the underlying list type does not have a default constructor.</exception>
		public virtual bool AllowNew
		{
			get { return m_bs.AllowNew; }
			set { m_bs.AllowNew = value; }
		}

		/// <summary>Gets a value indicating whether items can be removed from the underlying list.</summary>
		/// <returns>true to indicate list items can be removed from the list; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool AllowRemove
		{
			get { return m_bs.AllowRemove; }
		}

		/// <summary>Get/Set whether the underlying list can be sorted through this binding</summary>
		[Browsable(false)]
		public virtual bool AllowSort
		{
			get
			{
				var bl = m_bs.DataSource as BindingListEx<TItem>;
				if (bl != null) return bl.AllowSort;
				var bs = m_bs.DataSource as BindingSource<TItem>;
				if (bs != null) return bs.AllowSort;
				return m_impl_allow_sort;
			}
			set
			{
				m_impl_allow_sort = value;
				var bl = m_bs.DataSource as BindingListEx<TItem>;
				if (bl != null) { bl.AllowSort = value; return; }
				var bs = m_bs.DataSource as BindingSource<TItem>;
				if (bs != null) { bs.AllowSort = value; return; }
			}
		}
		private bool m_impl_allow_sort;

		/// <summary>
		/// Get/Set whether the Position can be set to -1 when the binding source contains items.
		/// Warning, this can causes exceptions in CurrencyManager when bound to a DGV.</summary>
		[Browsable(false)]
		public virtual bool AllowNoCurrent
		{
			get
			{
				var bs = m_bs.DataSource as BindingSource<TItem>;
				if (bs != null) return bs.AllowNoCurrent;
				return m_impl_allow_no_current;
			}
			set
			{
				m_impl_allow_no_current = value;
				var bs = m_bs.DataSource as BindingSource<TItem>;
				if (bs != null) { bs.AllowNoCurrent = value; return; }
			}
		}
		private bool m_impl_allow_no_current;

		/// <summary>Gets the total number of items in the underlying list, taking the current System.Windows.Forms.BindingSource.Filter value into consideration.</summary>
		/// <returns>The total number of filtered items in the underlying list.</returns>
		[Browsable(false)]
		public virtual int Count
		{
			get { return m_bs.Count; }
		}

		/// <summary>Gets the currency manager associated with this System.Windows.Forms.BindingSource.</summary>
		/// <returns>The System.Windows.Forms.CurrencyManager associated with this System.Windows.Forms.BindingSource.</returns>
		[Browsable(false)]
		public virtual CurrencyManager CurrencyManager
		{
			get { return m_bs.CurrencyManager; }
		}

		/// <summary>Get/Set the current item in the list.</summary>
		[Browsable(false)]
		public TItem Current
		{
			get { return (TItem)m_bs.Current; }
			set
			{
				var idx = List.IndexOf(value);
				if ((idx < 0 || idx >= List.Count) && !Equals(value, default(TItem)))
					throw new IndexOutOfRangeException("Cannot set Current to a value that isn't in this collection");

				Position = idx;
			}
		}

		/// <summary>Gets or sets the specific list in the data source to which the connector currently binds to.</summary>
		/// <returns>The name of a list (or row) in the System.Windows.Forms.BindingSource.DataSource. The default is an empty string ("").</returns>
		[DefaultValue("")]
		[Editor("System.Windows.Forms.Design.DataMemberListEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
		[RefreshProperties(RefreshProperties.Repaint)]
		public string DataMember
		{
			get { return m_bs.DataMember; }
			set { m_bs.DataMember = value; }
		}

		/// <summary>Get/Set the data source that the connector binds to.</summary>
		[AttributeProvider(typeof(IListSource))]
		[DefaultValue(null)]
		[RefreshProperties(RefreshProperties.Repaint)]
		public object DataSource
		{
			get { return m_bs.DataSource; }
			set
			{
				if (value == m_bs.DataSource) return;

				// Validation
				if (value != null)
				{
					// If the value isn't an IList the DataSource will create a
					// list internally and add 'value' as the first element.
					if (value is IList<TItem> && !(value is IList))
						throw new Exception("Binding to an IList<> that isn't also an IList has unexpected results");
				}

				RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.PreReset, -1, default(TItem)));

				// Unhook
				if (m_bs.DataSource is IListChanging<TItem> lc0)
					lc0.ListChanging -= RaiseListChanging;
				if (m_bs.DataSource is IItemChanged<TItem> ic0)
					ic0.ItemChanged  -= RaiseItemChanged;

				// Set new data source
				// Don't set to null because that causes controls with 'DisplayMember' properties to throw exceptions
				// complaining that the DisplayMember value does not exist in the data source.
				// Note: if Position == -1 && Count != 0 and this data source is bound to a DGV, it throws an unhandled
				// exception. You need to ensure Position is within [0,Count) before the grid gains focus.
				m_bs.DataSource = value ?? new TItem[0];
				// Note: this can cause a first chance exception in some controls
				// that are bound to this source (e.g. ListBox, ComboBox, etc.).
				// They are handled internally, just continue.
				//m_bs.DataSource = null;
				//m_bs.DataSource = value;

				// Hookup
				if (m_bs.DataSource is IItemChanged<TItem> ic1)
					ic1.ItemChanged  += RaiseItemChanged;
				if (m_bs.DataSource is IListChanging<TItem> lc1)
					lc1.ListChanging += RaiseListChanging;
				if (m_bs.DataSource is IListChanging<TItem> lc2)
					RaiseListChangedEvents = lc2.RaiseListChangedEvents;

				RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.Reset, -1, default(TItem)));
			}
		}

		/// <summary>Gets or sets the expression used to filter which rows are viewed.</summary>
		/// <returns>A string that specifies how rows are to be filtered. The default is null.</returns>
		[DefaultValue(null)]
		public virtual string Filter
		{
			get { return m_bs.Filter; }
			set { m_bs.Filter = value; }
		}

		/// <summary>Gets a value indicating whether the list binding is suspended.</summary>
		/// <returns>true to indicate the binding is suspended; otherwise, false.</returns>
		[Browsable(false)]
		public bool IsBindingSuspended
		{
			get { return m_bs.IsBindingSuspended; }
		}

		/// <summary>Gets a value indicating whether the underlying list has a fixed size.</summary>
		/// <returns>true if the underlying list has a fixed size; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool IsFixedSize
		{
			get { return m_bs.IsFixedSize; }
		}

		/// <summary>Gets a value indicating whether the underlying list is read-only.</summary>
		/// <returns>true if the list is read-only; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool IsReadOnly
		{
			get { return m_bs.IsReadOnly; }
		}

		/// <summary>Gets a value indicating whether the items in the underlying list are sorted.</summary>
		/// <returns>true if the list is an System.ComponentModel.IBindingList and is sorted; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool IsSorted
		{
			get { return m_bs.IsSorted; }
		}

		/// <summary>Gets a value indicating whether access to the collection is synchronized (thread safe).</summary>
		/// <returns>true to indicate the list is synchronized; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool IsSynchronized
		{
			get { return m_bs.IsSynchronized; }
		}

		/// <summary>Gets the list that the connector is bound to.</summary>
		/// <returns>An System.Collections.IList that represents the list, or null if there is no underlying list associated with this System.Windows.Forms.BindingSource.</returns>
		[Browsable(false)]
		public IList List
		{
			get { return m_bs.List; }
		}

		/// <summary>Get/Set the current position (-1 means no current item)</summary>
		[Browsable(false)]
		[DefaultValue(-1)]
		public int Position
		{
			get { return m_bs.Position; }
			set { m_bs.Position = value; }
		}

		/// <summary>Get/Set a value indicating whether ListChanging events should be raised.</summary>
		[Browsable(false)]
		[DefaultValue(true)]
		public bool RaiseListChangedEvents
		{
			get
			{
				if (m_bs.DataSource is IListChanging<TItem> lc) return lc.RaiseListChangedEvents;
				return m_bs.RaiseListChangedEvents;
			}
			set
			{
				if (m_bs.DataSource is IListChanging<TItem> lc) lc.RaiseListChangedEvents = value;
				m_bs.RaiseListChangedEvents = value;
			}
		}

		/// <summary>Gets or sets the column names used for sorting, and the sort order for viewing the rows in the data source.</summary>
		/// <returns>A case-sensitive string containing the column name followed by "ASC" (for ascending) or "DESC" (for descending). The default is null.</returns>
		[DefaultValue(null)]
		public string SortColumn
		{
			get { return m_bs.Sort; }
			set { m_bs.Sort = value; }
		}

		/// <summary>Gets the collection of sort descriptions applied to the data source.</summary>
		/// <returns>If the data source is an System.ComponentModel.IBindingListView, a System.ComponentModel.ListSortDescriptionCollection that contains the sort descriptions applied to the list; otherwise, null.</returns>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual ListSortDescriptionCollection SortDescriptions
		{
			get { return m_bs.SortDescriptions; }
		}

		/// <summary>Gets the direction the items in the list are sorted.</summary>
		/// <returns>One of the System.ComponentModel.ListSortDirection values indicating the direction the list is sorted.</returns>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual ListSortDirection SortDirection
		{
			get { return m_bs.SortDirection; }
		}

		/// <summary>Gets the System.ComponentModel.PropertyDescriptor that is being used for sorting the list.</summary>
		/// <returns>If the list is an System.ComponentModel.IBindingList, the System.ComponentModel.PropertyDescriptor that is being used for sorting; otherwise, null.</returns>
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual PropertyDescriptor SortProperty
		{
			get { return m_bs.SortProperty; }
		}

		/// <summary>Gets a value indicating whether the data source supports multi-column sorting.</summary>
		/// <returns>true if the list is an System.ComponentModel.IBindingListView and supports multi-column sorting; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool SupportsAdvancedSorting
		{
			get { return m_bs.SupportsAdvancedSorting; }
		}

		/// <summary>Gets a value indicating whether the data source supports change notification.</summary>
		/// <returns>true in all cases.</returns>
		[Browsable(false)]
		public virtual bool SupportsChangeNotification
		{
			get { return m_bs.SupportsChangeNotification; }
		}

		/// <summary>Gets a value indicating whether the data source supports filtering.</summary>
		/// <returns>true if the list is an System.ComponentModel.IBindingListView and supports filtering; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool SupportsFiltering
		{
			get { return m_bs.SupportsFiltering; }
		}

		/// <summary>Gets a value indicating whether the data source supports searching with the System.Windows.Forms.BindingSource.Find(System.ComponentModel.PropertyDescriptor,System.Object) method.</summary>
		/// <returns>true if the list is a System.ComponentModel.IBindingList and supports the searching with the Overload:System.Windows.Forms.BindingSource.Find method; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool SupportsSearching
		{
			get { return m_bs.SupportsSearching; }
		}

		/// <summary>Gets a value indicating whether the data source supports sorting.</summary>
		/// <returns>true if the data source is an System.ComponentModel.IBindingList and supports sorting; otherwise, false.</returns>
		[Browsable(false)]
		public virtual bool SupportsSorting
		{
			get { return m_bs.SupportsSorting && AllowSort; }
		}

		/// <summary>Gets an object that can be used to synchronize access to the underlying list.</summary>
		/// <returns>An object that can be used to synchronize access to the underlying list.</returns>
		[Browsable(false)]
		public virtual object SyncRoot
		{
			get { return m_bs.SyncRoot; }
		}

		/// <summary>Occurs before an item is added to the underlying list.</summary>
		/// <exception cref="InvalidOperationException">System.ComponentModel.AddingNewEventArgs.NewObject is not the same type as the type contained in the list.</exception>
		public event AddingNewEventHandler AddingNew
		{
			add { m_bs.AddingNew += value; }
			remove { m_bs.AddingNew -= value; }
		}

		/// <summary>Occurs when all the clients have been bound to this System.Windows.Forms.BindingSource.</summary>
		public event BindingCompleteEventHandler BindingComplete
		{
			add { m_bs.BindingComplete += value; }
			remove { m_bs.BindingComplete -= value; }
		}

		/// <summary>Occurs when the currently bound item changes.</summary>
		public event EventHandler CurrentChanged
		{
			add { m_bs.CurrentChanged += value; }
			remove { m_bs.CurrentChanged -= value; }
		}

		/// <summary>Occurs when a property value of the System.Windows.Forms.BindingSource.Current property has changed.</summary>
		public event EventHandler CurrentItemChanged
		{
			add { m_bs.CurrentItemChanged += value; }
			remove { m_bs.CurrentItemChanged -= value; }
		}

		/// <summary>Occurs when a currency-related exception is silently handled by the System.Windows.Forms.BindingSource.</summary>
		public event BindingManagerDataErrorEventHandler DataError
		{
			add { m_bs.DataError += value; }
			remove { m_bs.DataError -= value; }
		}

		/// <summary>Occurs when the System.Windows.Forms.BindingSource.DataMember property value has changed.</summary>
		public event EventHandler DataMemberChanged
		{
			add { m_bs.DataMemberChanged += value; }
			remove { m_bs.DataMemberChanged -= value; }
		}

		/// <summary>Occurs when the System.Windows.Forms.BindingSource.DataSource property value has changed.</summary>
		public event EventHandler DataSourceChanged
		{
			add { m_bs.DataSourceChanged += value; }
			remove { m_bs.DataSourceChanged -= value; }
		}

		/// <summary>Adds an existing item to the internal list.</summary>
		/// <param name="value">An System.Object to be added to the internal list.</param>
		/// <returns>The zero-based index at which value was added to the underlying list represented by the System.Windows.Forms.BindingSource.List property.</returns>
		/// <exception cref="InvalidOperationException">value differs in type from the existing items in the underlying list.</exception>
		public virtual int Add(TItem value)
		{
			return m_bs.Add(value);
		}

		/// <summary>Add a range of items</summary>
		public virtual void AddRange(IEnumerable<TItem> values)
		{
			foreach (var value in values)
				Add(value);
		}

		/// <summary>Adds a new item to the underlying list.</summary>
		/// <returns>The System.Object that was created and added to the list.</returns>
		/// <exception cref="InvalidOperationException">The System.Windows.Forms.BindingSource.AllowNew property is set to false. -or- A public default constructor could not be found for the current item type.</exception>
		public virtual TItem AddNew()
		{
			return (TItem)m_bs.AddNew();
		}

		/// <summary>Sort the underlying data source</summary>
		public void Sort(Cmp<TItem> comparer = null)
		{
			if (!AllowSort)
				throw new NotSupportedException("Sorting is not supported for this collection");

			// Suspend events, since this is just a reorder
			using (this.SuspendEvents(false))
				List_.Sort(this, comparer);

			RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.Reordered, -1, default(TItem)));
		}

		/// <summary>Sorts the data source with the specified sort descriptions.</summary>
		/// <param name="sorts">A System.ComponentModel.ListSortDescriptionCollection containing the sort descriptions to apply to the data source.</param>
		/// <exception cref="NotSupportedException">The data source is not an System.ComponentModel.IBindingListView.</exception>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ApplySort(ListSortDescriptionCollection sorts)
		{
			m_bs.ApplySort(sorts);
		}

		/// <summary>Sorts the data source using the specified property descriptor and sort direction.</summary>
		/// <param name="property">A System.ComponentModel.PropertyDescriptor that describes the property by which to sort the data source.</param>
		/// <param name="sort">A System.ComponentModel.ListSortDirection indicating how the list should be sorted.</param>
		/// <exception cref="NotSupportedException">The data source is not an System.ComponentModel.IBindingList.</exception>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ApplySort(PropertyDescriptor property, ListSortDirection sort)
		{
			m_bs.ApplySort(property, sort);
		}

		/// <summary>Cancels the current edit operation.</summary>
		public void CancelEdit()
		{
			m_bs.CancelEdit();
		}

		/// <summary>Removes all elements from the list.</summary>
		public virtual void Clear()
		{
			m_bs.Clear();
		}

		/// <summary>Determines whether an object is an item in the list.</summary>
		/// <param name="value">The System.Object to locate in the underlying list represented by the System.Windows.Forms.BindingSource.List property. The value can be null.</param>
		/// <returns>true if the value parameter is found in the System.Windows.Forms.BindingSource.List; otherwise, false.</returns>
		public virtual bool Contains(object value)
		{
			return m_bs.Contains(value);
		}

		/// <summary>Copies the contents of the System.Windows.Forms.BindingSource.List to the specified array, starting at the specified index value.</summary>
		/// <param name="arr">The destination array.</param>
		/// <param name="index">The index in the destination array at which to start the copy operation.</param>
		public virtual void CopyTo(Array arr, int index)
		{
			m_bs.CopyTo(arr, index);
		}

		/// <summary>Applies pending changes to the underlying data source.</summary>
		public void EndEdit()
		{
			m_bs.EndEdit();
		}

		/// <summary>Searches for the index of the item that has the given property descriptor.</summary>
		/// <param name="prop">The System.ComponentModel.PropertyDescriptor to search for.</param>
		/// <param name="key">The value of prop to match.</param>
		/// <returns>The zero-based index of the item that has the given value for System.ComponentModel.PropertyDescriptor.</returns>
		/// <exception cref="NotSupportedException">The underlying list is not of type System.ComponentModel.IBindingList.</exception>
		public virtual int Find(PropertyDescriptor prop, object key)
		{
			return m_bs.Find(prop, key);
		}

		/// <summary>Returns the index of the item in the list with the specified property name and value.</summary>
		/// <param name="propertyName">The name of the property to search for.</param>
		/// <param name="key">The value of the item with the specified propertyName to find.</param>
		/// <returns>The zero-based index of the item with the specified property name and value.</returns>
		/// <exception cref="InvalidOperationException">The underlying list is not a System.ComponentModel.IBindingList with searching functionality implemented.</exception>
		/// <exception cref="ArgumentException">propertyName does not match a property in the list.</exception>
		public int Find(string propertyName, object key)
		{
			return m_bs.Find(propertyName, key);
		}

		/// <summary>Retrieves an enumerator for the System.Windows.Forms.BindingSource.List.</summary>
		/// <returns>An System.Collections.IEnumerator for the System.Windows.Forms.BindingSource.List.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_bs.GetEnumerator();
		}

		/// <summary>Enumerate over data source elements</summary>
		public IEnumerator<TItem> GetEnumerator()
		{
			foreach (var item in List)
				yield return (TItem)item;
		}

		/// <summary>Retrieves an array of System.ComponentModel.PropertyDescriptor objects representing the bind-able properties of the data source list type.</summary>
		/// <param name="listAccessors">An array of System.ComponentModel.PropertyDescriptor objects to find in the list as bind-able.</param>
		/// <returns>An array of System.ComponentModel.PropertyDescriptor objects that represents the properties on this list type used to bind data.</returns>
		public virtual PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
		{
			return m_bs.GetItemProperties(listAccessors);
		}

		/// <summary>Gets the name of the list supplying data for the binding.</summary>
		/// <param name="listAccessors">An array of System.ComponentModel.PropertyDescriptor objects to find in the list as bindable.</param>
		/// <returns>The name of the list supplying the data for binding.</returns>
		public virtual string GetListName(PropertyDescriptor[] listAccessors)
		{
			return m_bs.GetListName(listAccessors);
		}

		/// <summary>Gets the related currency manager for the specified data member.</summary>
		/// <param name="dataMember">The name of column or list, within the data source to retrieve the currency manager for.</param>
		/// <returns>The related System.Windows.Forms.CurrencyManager for the specified data member.</returns>
		public virtual CurrencyManager GetRelatedCurrencyManager(string dataMember)
		{
			return m_bs.GetRelatedCurrencyManager(dataMember);
		}

		/// <summary>Searches for the specified object and returns the index of the first occurrence within the entire list.</summary>
		/// <param name="value">The System.Object to locate in the underlying list represented by the System.Windows.Forms.BindingSource.List property. The value can be null.</param>
		/// <returns>The zero-based index of the first occurrence of the value parameter; otherwise, -1 if value is not in the list.</returns>
		public virtual int IndexOf(TItem value)
		{
			return m_bs.IndexOf(value);
		}

		/// <summary>Inserts an item into the list at the specified index.</summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The System.Object to insert. The value can be null.</param>
		/// <exception cref="ArgumentOutOfRangeException">index is less than zero or greater than System.Windows.Forms.BindingSource.Count.</exception>
		/// <exception cref="NotSupportedException">The list is read-only or has a fixed size.</exception>
		public virtual void Insert(int index, TItem value)
		{
			m_bs.Insert(index, value);
		}

		/// <summary>Moves to the first item in the list.</summary>
		public void MoveFirst()
		{
			m_bs.MoveFirst();
		}

		/// <summary>Moves to the last item in the list.</summary>
		public void MoveLast()
		{
			m_bs.MoveLast();
		}

		/// <summary>Moves to the next item in the list.</summary>
		public void MoveNext()
		{
			m_bs.MoveNext();
		}

		/// <summary>Moves to the previous item in the list.</summary>
		public void MovePrevious()
		{
			m_bs.MovePrevious();
		}

		/// <summary>Removes the specified item from the list.</summary>
		/// <param name="value">The item to remove from the underlying list represented by the System.Windows.Forms.BindingSource.List property.</param>
		/// <exception cref="NotSupportedException">The underlying list has a fixed size or is read-only.</exception>
		public virtual void Remove(TItem value)
		{
			m_bs.Remove(value);
		}

		/// <summary>Removes the item at the specified index in the list.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException">index is less than zero or greater than the value of the System.Windows.Forms.BindingSource.Count property.</exception>
		/// <exception cref="NotSupportedException">The underlying list represented by the System.Windows.Forms.BindingSource.List property is read-only or has a fixed size.</exception>
		public virtual void RemoveAt(int index)
		{
			m_bs.RemoveAt(index);
		}

		/// <summary>Removes the current item from the list.</summary>
		/// <exception cref="InvalidOperationException">The System.Windows.Forms.BindingSource.AllowRemove property is false.-or-System.Windows.Forms.BindingSource.Position is less than zero or greater than System.Windows.Forms.BindingSource.Count.</exception>
		/// <exception cref="NotSupportedException">The underlying list represented by the System.Windows.Forms.BindingSource.List property is read-only or has a fixed size.</exception>
		public void RemoveCurrent()
		{
			m_bs.RemoveCurrent();
		}

		/// <summary>Removes the filter associated with the System.Windows.Forms.BindingSource.</summary>
		/// <exception cref="NotSupportedException">The underlying list does not support filtering.</exception>
		public virtual void RemoveFilter()
		{
			m_bs.RemoveFilter();
		}

		/// <summary>Removes the sort associated with the System.Windows.Forms.BindingSource.</summary>
		/// <exception cref="NotSupportedException">The underlying list does not support sorting.</exception>
		public virtual void RemoveSort()
		{
			m_bs.RemoveSort();
		}

		/// <summary>Reinitializes the System.Windows.Forms.BindingSource.AllowNew property.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public virtual void ResetAllowNew()
		{
			m_bs.ResetAllowNew();
		}

		/// <summary>Causes a control bound to the System.Windows.Forms.BindingSource to reread all the items in the list and refresh their displayed values.</summary>
		/// <param name="metadataChanged">true if the data schema has changed; false if only values have changed.</param>
		public void ResetBindings(bool metadataChanged)
		{
			var idx = m_bs.Position;

			if (RaiseListChangedEvents)
				RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.PreReset, -1, default(TItem)));

			m_bs.ResetBindings(metadataChanged);

			// Preserve the current position if possible
			if (idx >= 0 && idx < m_bs.Count)
				m_bs.Position = idx;

			if (RaiseListChangedEvents)
				RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.Reset, -1, default(TItem)));
		}

		/// <summary>Causes a control bound to the System.Windows.Forms.BindingSource to reread the currently selected item and refresh its displayed value.</summary>
		public void ResetCurrentItem()
		{
			var item = Current;
			var index = Position;

			if (RaiseListChangedEvents)
				RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.ItemPreReset, index, item));

			m_bs.ResetCurrentItem();

			if (RaiseListChangedEvents)
				RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.ItemReset, index, item));
		}

		/// <summary>Causes a control bound to this BindingSource to reread the item at the specified index, and refresh its displayed value.</summary>
		/// <param name="itemIndex">The zero-based index of the item that has changed.</param>
		public void ResetItem(int itemIndex)
		{
			var item = this[itemIndex];

			if (RaiseListChangedEvents)
				RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.ItemPreReset, itemIndex, item));

			m_bs.ResetItem(itemIndex);

			if (RaiseListChangedEvents)
				RaiseListChanging(this, new ListChgEventArgs<TItem>(this, ListChg.ItemReset, itemIndex, item));
		}

		/// <summary>Reset bindings for 'item'</summary>
		public void ResetItem(TItem item, bool ignore_missing = false)
		{
			var idx = List.IndexOf(item);
			if (idx < 0 || idx >= List.Count)
			{
				if (ignore_missing || Equals(item, default(TItem))) return;
				throw new IndexOutOfRangeException("Cannot reset a value that isn't in this collection");
			}
			ResetItem(idx);
		}
		
		/// <summary>Resumes data binding.</summary>
		private void ResumeBinding()
		{
			m_bs.ResumeBinding();
		}

		/// <summary>Suspends data binding to prevent changes from updating the bound data source.</summary>
		private void SuspendBinding()
		{
			m_bs.SuspendBinding();
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

		/// <summary>RAII object to restore the current position</summary>
		public Scope PreservePosition()
		{
			return Scope.Create(() => Position, p => Position = Count != 0 ? Maths.Clamp(p, 0, Count-1) : -1);
		}

		/// <summary>Occurs when the underlying list changes or an item in the list changes.</summary>
		private event ListChangedEventHandler ListChanged
		{
			add { m_bs.ListChanged += value; }
			remove { m_bs.ListChanged -= value; }
		}

		/// <summary>Raised *only* if 'DataSource' is 'IListChanging<TItem>'</summary>
		public event EventHandler<ListChgEventArgs<TItem>> ListChanging;
		protected virtual void OnListChanging(object sender, ListChgEventArgs<TItem> args)
		{
			ListChanging.Raise(sender, args);
		}
		private void RaiseListChanging(object sender, ListChgEventArgs<TItem> args)
		{
			OnListChanging(sender, args);

			// If we're about to remove all items, set Position to -1
			if (args.ChangeType == ListChg.PreReset)
				Position = -1;

			// If we're about to delete the current item, set Position to the next/prev item or -1 if there are no more
			if (args.ChangeType == ListChg.ItemPreRemove && args.Index == m_bs.m_impl_previous_position)
			{
				if      (args.Index < Count - 1) Position = args.Index + 1;
				else if (args.Index > 0)         Position = args.Index - 1;
				else                             Position = -1;
			}
		}

		/// <summary>Raised *only* if 'DataSource' is 'IListChanging<TItem></summary>
		public event EventHandler<ItemChgEventArgs<TItem>> ItemChanged;
		protected virtual void OnItemChanged(object sender, ItemChgEventArgs<TItem> args)
		{
			ItemChanged.Raise(sender, args);
		}
		private void RaiseItemChanged(object sender, ItemChgEventArgs<TItem> args)
		{
			OnItemChanged(sender, args);
		}

		/// <summary>Raised after the current list position changes (includes the previous position)</summary>
		public event EventHandler<PositionChgEventArgs> PositionChanged;
		protected virtual void OnPositionChanged(object sender, PositionChgEventArgs args)
		{
			PositionChanged.Raise(this, args);
		}
		private void RaisePositionChanged(object sender, PositionChgEventArgs args)
		{
			OnPositionChanged(sender, args);
		}

		public override string ToString()
		{
			return "{0} Current: {1}".Fmt(Count, Current);
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

		#region IList
		bool IList.IsReadOnly
		{
			get { return IsReadOnly; }
		}
		bool IList.IsFixedSize
		{
			get { return IsFixedSize; }
		}
		int IList.Add(object value)
		{
			return Add((TItem)value);
		}
		bool IList.Contains(object value)
		{
			return Contains((TItem)value);
		}
		void IList.Clear()
		{
			Clear();
		}
		int IList.IndexOf(object value)
		{
			return IndexOf((TItem)value);
		}
		void IList.Insert(int index, object value)
		{
			Insert(index, (TItem)value);
		}
		void IList.Remove(object value)
		{
			Remove((TItem)value);
		}
		void IList.RemoveAt(int index)
		{
			RemoveAt(index);
		}
		#endregion

		#region ICollection
		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo(array, index);
		}
		int ICollection.Count
		{
			get { return Count; }
		}
		object ICollection.SyncRoot
		{
			get { return SyncRoot; }
		}
		bool ICollection.IsSynchronized
		{
			get { return IsSynchronized; }
		}
		#endregion

		#region IBindingList
		object IBindingList.AddNew()
		{
			return AddNew();
		}
		void IBindingList.AddIndex(PropertyDescriptor property)
		{
			((IBindingList)m_bs).AddIndex(property);
		}
		void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
		{
			ApplySort(property, direction);
		}
		int IBindingList.Find(PropertyDescriptor property, object key)
		{
			return Find(property, key);
		}
		void IBindingList.RemoveIndex(PropertyDescriptor property)
		{
			((IBindingList)m_bs).RemoveIndex(property);
		}
		void IBindingList.RemoveSort()
		{
			RemoveSort();
		}
		bool IBindingList.AllowNew
		{
			get { return AllowNew; }
		}
		bool IBindingList.AllowEdit
		{
			get { return AllowEdit; }
		}
		bool IBindingList.AllowRemove
		{
			get { return AllowRemove; }
		}
		bool IBindingList.SupportsChangeNotification
		{
			get { return SupportsChangeNotification; }
		}
		bool IBindingList.SupportsSearching
		{
			get { return SupportsSearching; }
		}
		bool IBindingList.SupportsSorting
		{
			get { return SupportsSorting; }
		}
		bool IBindingList.IsSorted
		{
			get { return IsSorted; }
		}
		PropertyDescriptor IBindingList.SortProperty
		{
			get { return SortProperty; }
		}
		ListSortDirection IBindingList.SortDirection
		{
			get { return SortDirection; }
		}
		event ListChangedEventHandler IBindingList.ListChanged
		{
			add { ListChanged += value; }
			remove { ListChanged -= value; }
		}
		#endregion

		#region ICancelAddNew
		void ICancelAddNew.CancelNew(int itemIndex)
		{
			((ICancelAddNew)m_bs).CancelNew(itemIndex);
		}
		void ICancelAddNew.EndNew(int itemIndex)
		{
			((ICancelAddNew)m_bs).EndNew(itemIndex);
		}

		void ISupportInitialize.BeginInit()
		{
			throw new NotImplementedException();
		}

		void ISupportInitialize.EndInit()
		{
			throw new NotImplementedException();
		}
		#endregion

		#region ISupportInitializeNotification
		bool ISupportInitializeNotification.IsInitialized
		{
			get { return ((ISupportInitializeNotification)m_bs).IsInitialized; }
		}
		event EventHandler ISupportInitializeNotification.Initialized
		{
			add { ((ISupportInitializeNotification)m_bs).Initialized += value; }
			remove { ((ISupportInitializeNotification)m_bs).Initialized -= value; }
		}
		#endregion

		/// <summary>Create a new view of this binding source</summary>
		public BindingSource<TItem> CreateView(Func<TItem,bool> pred)
		{
			// Create a view of this binding source
			var view = new View(this, pred);
			var bs = new BindingSource<TItem>{DataSource = view, AllowNoCurrent = AllowNoCurrent };

			// When the underlying source position changes, if it corresponds to an item
			// in the view, update the Position in the returned view. Note, using view.PositionChanged
			// rather than view.BindingSource.PositionChanged because 'view.BindingSource' is actually
			// 'this' and we wouldn't be able to detach this handler.
			view.PositionChanged += (s,a) =>
			{
				// If 'a.NewIndex >= 0', this the current item in the underlying source is also in this view
				bs.Position = a.NewIndex >= 0 ? a.NewIndex : -1;
			};

			// When the view position changes, set the position in the underlying binding source
			bs.PositionChanged += (s,a) =>
			{
				view.Position = a.NewIndex;
			};

			// Clean up the view if the binding source is disposed
			bs.Disposed += (s,a) =>
			{
				if (bs != null) bs.DataSource = null;
				Util.Dispose(ref view);
			};

			// Clean up the binding source if the view gets disposed
			view.Disposed += (s,a) =>
			{
				if (bs != null) bs.DataSource = null;
				Util.Dispose(ref bs);
			};

			return bs;
		}

		/// <summary>
		/// A view of the binding source data that is filtered by a predicate.
		/// Updates when the underlying binding source changes.
		/// This class is intended to be the DataSource of another binding source instance</summary>
		public class View :IDisposable ,IBindingList, IList<TItem>, IList, ICollection, IEnumerable<TItem>, IEnumerable, ICancelAddNew
		{
			public View(BindingSource<TItem> bs, Func<TItem, bool> pred = null)
			{
				m_readonly = false;
				Index = new BindingListEx<int>();
				Predicate = pred ?? (x => true);
				BindingSource = bs;
			}
			public void Dispose()
			{
				Disposed.Raise(this);
				BindingSource = null;
				Predicate = null;
				Index = null;
			}

			/// <summary>Raised just before this view is disposed</summary>
			public event EventHandler Disposed;

			/// <summary>The indices of the elements visible in this view</summary>
			public BindingListEx<int> Index
			{
				get { return m_bl; }
				set
				{
					if (m_bl == value) return;
					if (m_bl != null)
					{
						m_bl.ListChanging -= HandleViewChanging;
						m_bl.ListChanged -= HandleViewChanged;
						m_bc = null;
					}
					m_bl = value;
					if (m_bl != null)
					{
						m_bc = new BindingContext();
						m_bl.ListChanging += HandleViewChanging;
						m_bl.ListChanged += HandleViewChanged;
						m_bl.ResetBindings();
						UpdateView();
					}
				}
			}
			private BindingListEx<int> m_bl;
			private BindingContext m_bc;

			/// <summary>The filter function</summary>
			public Func<TItem,bool> Predicate
			{
				get { return m_predicate; }
				set
				{
					if (m_predicate == value) return;
					m_predicate = value ?? (x => true);
					UpdateView();
				}
			}
			private Func<TItem,bool> m_predicate;

			/// <summary>The binding source that this is a view of</summary>
			public BindingSource<TItem> BindingSource
			{
				get { return m_bs; }
				set
				{
					if (m_bs == value) return;
					if (m_bs != null)
					{
						m_bs.ListChanging -= HandleSourceChanging;
						m_bs.PositionChanged -= HandleSourcePositionChanged;
						m_bs.Disposed -= HandleBindingSourceDisposed;
					}
					m_bs = value;
					if (m_bs != null)
					{
						m_bs.ListChanging += HandleSourceChanging;
						m_bs.PositionChanged += HandleSourcePositionChanged;
						m_bs.Disposed += HandleBindingSourceDisposed;
					}
					UpdateView();
				}
			}
			private BindingSource<TItem> m_bs;

			/// <summary>Raised when this view is changing</summary>
			public event EventHandler<ListChgEventArgs<TItem>> ListChanging;
			public event ListChangedEventHandler ListChanged;

			/// <summary>The Position in the underlying binding source as an index in this view</summary>
			/// <returns>Returns a twos-complement value if the underlying source position in not in this view</returns>
			public int Position
			{
				get { return SrcToViewIndex(BindingSource.Position); }
				set
				{
					// If 'value < 0' then ignore the set, we only want to set the position
					// on the underlying source when it corresponds to an item in this view.
					if (value < 0) return;
					BindingSource.Position = Index[value];
				}
			}

			/// <summary>Raised when the Position in the underlying binding source changes</summary>
			public event EventHandler<PositionChgEventArgs> PositionChanged;

			/// <summary>Convert an index in 'BindingSource' into an index in 'Index'.</summary>
			/// <returns>Returns a twos-complement index if 'src_index' is not an item in this view</returns>
			public int SrcToViewIndex(int src_index)
			{
				if (src_index == -1) return -1;
				return Index.BinarySearch(i => i.CompareTo(src_index));
			}
			public int ViewToSrcIndex(int view_index)
			{
				if (view_index == -1) return -1;
				return Index[view_index];
			}

			/// <summary>Update the list of items in this view</summary>
			public void UpdateView()
			{
				HandleSourceChanging(this, new ListChgEventArgs<TItem>(this, ListChg.Reset, -1, default(TItem)));
			}

			/// <summary>Handle the underlying binding source changing</summary>
			private void HandleSourceChanging(object sender, ListChgEventArgs<TItem> e)
			{
				// No source, no view
				if (BindingSource == null)
				{
					Index.Clear();
					return;
				}

				// See if the change to the binding source affects this view
				switch (e.ChangeType)
				{
				default:
					{
						Debug.Assert(SanityCheck());
						break;
					}
				case ListChg.PreReset:
				case ListChg.PreReordered:
					{
						// Invalidate 'Index' before a reset/position changed
						Index.Clear();
						break;
					}
				case ListChg.Reset:
				case ListChg.Reordered:
					{
						// Populate the Index collection with those that pass the predicate
						Index.Clear();
						for (int i = 0; i != BindingSource.Count; ++i)
						{
							if (!Predicate(BindingSource[i])) continue;
							Index.Add(i);
						}
						Debug.Assert(SanityCheck());
						break;
					}
				case ListChg.ItemPreReset:
					{
						// Before an item is reset, retest whether it belongs in the view
						var idx = SrcToViewIndex(e.Index);
						if (idx >= 0 && !Predicate(e.Item)) Index.RemoveAt(idx);
						if (idx <  0 &&  Predicate(e.Item)) Index.Insert(~idx, e.Index);
						break;
					}
				case ListChg.ItemReset:
					{
						Debug.Assert(SanityCheck());
						break;
					}
				case ListChg.ItemPreRemove:
					{
						// Before an item is removed from the source, remove it from the view
						var idx = SrcToViewIndex(e.Index);
						if (idx >= 0) Index.RemoveAt(idx);
						if (idx < 0) idx = ~idx;
						for (var i = idx; i < Index.Count; ++i) --Index[i];
						break;
					}
				case ListChg.ItemRemoved:
					{
						Debug.Assert(SanityCheck());
						break;
					}
				case ListChg.ItemPreAdd:
					{
						Debug.Assert(SanityCheck());
						break;
					}
				case ListChg.ItemAdded:
					{
						// After an item is added, see if it should be added to the view
						var idx = SrcToViewIndex(e.Index);
						if (idx < 0) idx = ~idx;
						if (Predicate(e.Item)) Index.Insert(idx, e.Index);
						for (var i = idx + 1; i < Index.Count; ++i) ++Index[i];
						Debug.Assert(SanityCheck());
						break;
					}
				}
			}

			/// <summary>Report position changed in the underlying binding source</summary>
			private void HandleSourcePositionChanged(object sender, PositionChgEventArgs e)
			{
				// Convert the binding source position into a position in this view
				var old = SrcToViewIndex(e.OldIndex);
				var neu = SrcToViewIndex(e.NewIndex);
				PositionChanged.Raise(this, new PositionChgEventArgs(old, neu));
			}

			/// <summary>Raise the ListChanging event on this view</summary>
			private void HandleViewChanging(object sender, ListChgEventArgs<int> e)
			{
				// Translate the changing Index collection to a changing item
				var item = e.Index != -1 ? BindingSource[e.Item] : default(TItem);
				ListChanging.Raise(this, new ListChgEventArgs<TItem>(this, e.ChangeType, e.Index, item));
			}

			/// <summary>Handle the IBindingList changed events</summary>
			private void HandleViewChanged(object sender, ListChangedEventArgs e)
			{
				if (ListChanged == null) return;
				ListChanged(this, e);
			}

			/// <summary>If the owning binding source is disposed, dispose this view as well</summary>
			private void HandleBindingSourceDisposed(object sender, EventArgs e)
			{
				Dispose();
			}

			#region IEnumerable
			public IEnumerator<TItem> GetEnumerator()
			{
				return Index.Select(i => BindingSource[i]).GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<TItem>)this).GetEnumerator();
			}
			#endregion

			#region IList

			/// <summary>The number of items in this view</summary>
			public int Count
			{
				get { return Index.Count; }
			}

			/// <summary>Get/Set whether the view is readonly</summary>
			public bool IsReadOnly
			{
				get { return m_readonly || BindingSource.IsReadOnly; }
				set { m_readonly = value; }
			}
			private bool m_readonly;
			private void ThrowIfReadOnly()
			{
				if (!IsReadOnly) return;
				throw new Exception("BindingSource View is readonly");
			}

			/// <summary>True if this view is a fixed size</summary>
			public bool IsFixedSize
			{
				get { return IsReadOnly; }
			}

			/// <summary>Synchronisation object</summary>
			public object SyncRoot
			{
				get { return BindingSource.SyncRoot; }
			}

			/// <summary>True if this collection is thread safe</summary>
			public bool IsSynchronized
			{
				get { return false; }
			}

			/// <summary>Access elements in the view by index</summary>
			public TItem this[int index]
			{
				get { return BindingSource[Index[index]]; }
				set
				{
					ThrowIfReadOnly();
					BindingSource[Index[index]] = value;
				}
			}
			public TItem this[uint index]
			{
				get { return this[(int)index]; }
				set { this[(int)index] = value; }
			}
			object IList.this[int index]
			{
				get { return this[index]; }
				set { this[index] = (TItem)value; }
			}

			/// <summary>The index of 'item' in this view</summary>
			public int IndexOf(TItem item)
			{
				var i = 0;
				foreach (var x in this)
				{
					if (!Equals(x, item)) ++i;
					else return i;
				}
				return -1;
			}
			int IList.IndexOf(object value)
			{
				return IndexOf((TItem)value);
			}

			/// <summary>Insert an item into the underlying binding source. Note, if the item does not pass the predicate it will not appear in this view</summary>
			public void Insert(int index, TItem item)
			{
				ThrowIfReadOnly();
				BindingSource.Insert(Index[index], item);
			}
			void IList.Insert(int index, object value)
			{
				Insert(index, (TItem)value);
			}

			/// <summary>Remove an item from the underlying binding source</summary>
			public void RemoveAt(int index)
			{
				ThrowIfReadOnly();
				BindingSource.RemoveAt(Index[index]);
			}

			/// <summary>Remove 'item' from the underlying binding source. Note: 'item' does not have to be in this view</summary>
			public void Remove(TItem item)
			{
				ThrowIfReadOnly();
				BindingSource.Remove(item);
			}
			void IList.Remove(object value)
			{
				Remove((TItem)value);
			}
			bool ICollection<TItem>.Remove(TItem item)
			{
				ThrowIfReadOnly();
				return ((ICollection<TItem>)BindingSource).Remove(item);
			}

			/// <summary>Add 'item' to the underlying data source. Note: 'item' won't be added to this view if Predicate(item) is false<</summary>
			public void Add(TItem item)
			{
				ThrowIfReadOnly();
				BindingSource.Add(item);
			}
			int IList.Add(object value)
			{
				Add((TItem)value);
				return Count - 1;
			}

			/// <summary>Clear the underlying data source.</summary>
			public void Clear()
			{
				ThrowIfReadOnly();
				BindingSource.Clear();
			}

			/// <summary>True if 'item' is in this view</summary>
			public bool Contains(TItem item)
			{
				foreach (var x in this)
				{
					if (!Equals(x, item)) continue;
					return true;
				}
				return false;
			}
			bool IList.Contains(object value)
			{
				return Contains((TItem)value);
			}

			/// <summary>Copy the items in this view to 'array'</summary>
			public void CopyTo(TItem[] array, int offset)
			{
				foreach (var item in this)
					array[offset++] = item;
			}
			void ICollection.CopyTo(Array array, int offset)
			{
				foreach (var item in this)
					array.SetValue(item, offset++);
			}

			#endregion

			#region IBindingList
			
			/// <summary>Get whether new elements can be added via this view</summary>
			public bool AllowNew
			{
				get { return !IsReadOnly && BindingSource.AllowNew; }
			}

			/// <summary>Get whether elements can be edited via this view</summary>
			public bool AllowEdit
			{
				get { return !IsReadOnly && BindingSource.AllowEdit; }
			}

			/// <summary>Get whether elements can be removed via this view</summary>
			public bool AllowRemove
			{
				get { return !IsReadOnly && BindingSource.AllowRemove; }
			}

			/// <summary>Get whether change notification is supported</summary>
			public bool SupportsChangeNotification
			{
				get { return BindingSource.SupportsChangeNotification; }
			}

			/// <summary>Get whether searching is supported</summary>
			public bool SupportsSearching
			{
				get { return BindingSource.SupportsSearching; }
			}

			/// <summary>Get whether sorting is supported</summary>
			public bool SupportsSorting
			{
				get { return BindingSource.SupportsSorting; }
			}

			/// <summary>True if the collection is sorted</summary>
			public bool IsSorted
			{
				get { return BindingSource.IsSorted; }
			}

			/// <summary>The property to sort on</summary>
			public PropertyDescriptor SortProperty
			{
				get { return BindingSource.SortProperty; }
			}

			/// <summary>The sort direction</summary>
			public ListSortDirection SortDirection
			{
				get { return BindingSource.SortDirection; }
			}

			/// <summary>Add a new item to the collection</summary>
			public object AddNew()
			{
				ThrowIfReadOnly();
				return BindingSource.AddNew();
			}

			/// <summary>Apply the sort to the underlying collection</summary>
			public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
			{
				BindingSource.ApplySort(property, direction);
			}

			/// <summary>Remove the sort</summary>
			public void RemoveSort()
			{
				BindingSource.RemoveSort();
			}

			/// <summary></summary>
			public int Find(PropertyDescriptor property, object key)
			{
				return BindingSource.Find(property, key);
			}

			/// <summary></summary>
			void IBindingList.AddIndex(PropertyDescriptor property)
			{
				((IBindingList)BindingSource).AddIndex(property);
			}
			void IBindingList.RemoveIndex(PropertyDescriptor property)
			{
				((IBindingList)BindingSource).RemoveIndex(property);
			}

			#endregion

			#region IBindingListView
			public string Filter
			{
				get { return BindingSource.Filter; }
				set { BindingSource.Filter = value; }
			}
			public ListSortDescriptionCollection SortDescriptions
			{
				get { return BindingSource.SortDescriptions; }
			}
			public bool SupportsAdvancedSorting
			{
				get { return BindingSource.SupportsAdvancedSorting; }
			}
			public bool SupportsFiltering
			{
				get { return BindingSource.SupportsFiltering; }
			}
			public void ApplySort(ListSortDescriptionCollection sorts)
			{
				BindingSource.ApplySort(sorts);
			}
			public void RemoveFilter()
			{
				BindingSource.RemoveFilter();
			}
			#endregion

			#region ICancelAddNew
			void ICancelAddNew.CancelNew(int itemIndex)
			{
				((ICancelAddNew)BindingSource).CancelNew(itemIndex);
			}
			void ICancelAddNew.EndNew(int itemIndex)
			{
				((ICancelAddNew)BindingSource).EndNew(itemIndex);
			}
			#endregion

			/// <summary>Check the view is correct</summary>
			private bool SanityCheck()
			{
				var should_be = BindingSource.Where(Predicate).ToList();
				if (!this.SequenceEqual(should_be))
					return false;

				return true;
			}
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Collections.Generic;
	using System.Linq;
	using container;

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

			var bs = new BindingSource<int>{DataSource = bl, AllowNoCurrent = true };
			bs.PositionChanged += (s,a) =>
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
		[Test] public void BindingSourceDeletingCurrent()
		{
			var positions = new List<int>();

			var bl = new BindingListEx<int>();
			bl.AddRange(new[]{1,2,3,4,5});

			var current = -1;
			var bs = new BindingSource<int>{DataSource = bl};
			bs.PositionChanged += (s,a) =>
			{
				positions.Add(a.OldIndex);
				positions.Add(a.NewIndex);
				current = bs.Current;
			};

			bs.Position = 2;
			Assert.True(current == 3);

			bl.RemoveAt(2);
			Assert.True(bs.Position == 2);
			Assert.True(current == 4);

			bs.Position = 2;
			Assert.True(current == 4);

			bl.RemoveAt(2);
			Assert.True(bs.Position == 2);
			Assert.True(current == 5);

			bl.RemoveAt(2);
			Assert.True(bs.Position == 1);
			Assert.True(current == 2);

			bs.Position = 0;
			Assert.True(current == 1);

			Assert.True(positions.SequenceEqual(new[]{0,2,2,3,3,2,2,3,3,2,2,1,1,0}));
		}
		[Test] public void BindingSourceView()
		{
			var bl = new BindingListEx<char>();
			var bs = new BindingSource<char> { DataSource = bl , AllowNoCurrent = true };
			var bv = bs.CreateView(i => (i % 2) == 0);

			bl.Add('a','b','c','d','e','f','g','h','i','j');

			Assert.True(bl.SequenceEqual(new[]{'a','b','c','d','e','f','g','h','i','j'}));
			Assert.True(bs.SequenceEqual(new[]{'a','b','c','d','e','f','g','h','i','j'}));
			Assert.True(bv.SequenceEqual(new[]{'b','d','f','h','j'                    }));

			bs.Remove('e');
			bs.Remove('f');

			Assert.True(bl.SequenceEqual(new[]{'a','b','c','d','g','h','i','j'}));
			Assert.True(bs.SequenceEqual(new[]{'a','b','c','d','g','h','i','j'}));
			Assert.True(bv.SequenceEqual(new[]{'b','d','h','j'                }));

			bv.Remove('a');
			bv.Remove('b');

			Assert.True(bl.SequenceEqual(new[]{'c','d','g','h','i','j'}));
			Assert.True(bs.SequenceEqual(new[]{'c','d','g','h','i','j'}));
			Assert.True(bv.SequenceEqual(new[]{'d','h','j'            }));

			bv.Add('k');
			bv.Add('l');

			Assert.True(bl.SequenceEqual(new[]{'c','d','g','h','i','j','k','l'}));
			Assert.True(bs.SequenceEqual(new[]{'c','d','g','h','i','j','k','l'}));
			Assert.True(bv.SequenceEqual(new[]{'d','h','j','l'                }));

			bs.Position = 2;
			Assert.True(bv.Position == -1);
			bs.Position = 3;
			Assert.True(bv.Position == 1);

			bv.Position = 2;
			Assert.True(bs.Position == 5);
			bv.Position = 3;
			Assert.True(bs.Position == 7);
		}
	}
}
#endif
