//***********************************************
//  Copyright (c) Rylogic Ltd 2008
//***********************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace Rylogic.Container
{
	/// <summary>
	/// Provides a generic collection that supports data binding and additionally supports sorting.
	/// See http://msdn.microsoft.com/en-us/library/ms993236.aspx
	/// If the elements are IComparable it uses that; otherwise compares the ToString()
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	[Obsolete("Just use normal BindingListEX")]public class SortableBindingList<T> : BindingList<T> where T : class
	{
		private PropertyDescriptor m_sort_property;
		private ListSortDirection  m_sort_direction = ListSortDirection.Ascending;
		private bool               m_sorted;

		public SortableBindingList() :base() {}
		public SortableBindingList(IList<T> list) :base(list) {}

		/// <summary>Gets a value indicating whether the list supports sorting.</summary>
		protected override bool SupportsSortingCore
		{
			get { return true; }
		}

		/// <summary>Gets a value indicating whether the list is sorted.</summary>
		protected override bool IsSortedCore
		{
			get { return m_sorted; }
		}
 
		/// <summary>Gets the direction the list is sorted.</summary>
		protected override ListSortDirection SortDirectionCore
		{
			get { return m_sort_direction; }
		}
 
		/// <summary>Gets the property descriptor that is used for sorting the list if sorting is implemented in a derived class; otherwise, returns null</summary>
		protected override PropertyDescriptor SortPropertyCore
		{
			get { return m_sort_property; }
		}
 
		/// <summary>Removes any sort applied with ApplySortCore if sorting is implemented</summary>
		protected override void RemoveSortCore()
		{
			m_sort_direction = ListSortDirection.Ascending;
			m_sort_property = null;
		}
 
		/// <summary>Sorts the items if overridden in a derived class</summary>
		protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
		{
			m_sort_property = prop;
			m_sort_direction = direction;
 
			List<T> list = Items as List<T>;
			if (list == null) return;
 
			list.Sort(Compare);
 
			m_sorted = true;

			//fire an event that the list has been changed.
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
		}
		private int Compare(T lhs, T rhs)
		{
			int result = OnComparison(lhs, rhs);
			if (m_sort_direction == ListSortDirection.Descending) result = -result; //invert if descending
			return result;
		}
 
		private int OnComparison(T lhs, T rhs)
		{
			object lhsValue = lhs == null ? null : m_sort_property.GetValue(lhs);
			object rhsValue = rhs == null ? null : m_sort_property.GetValue(rhs);
			if (lhsValue == null)
			{
				return (rhsValue == null) ? 0 : -1; //nulls are equal
			}
			if (rhsValue == null)
			{
				return 1; //first has value, second doesn't
			}
			if (lhsValue is IComparable)
			{
				return ((IComparable)lhsValue).CompareTo(rhsValue);
			}
			if (lhsValue.Equals(rhsValue))
			{
				return 0; //both are the same
			}

			//not comparable, compare ToString
			return lhsValue.ToString().CompareTo(rhsValue.ToString());
		}
	}
}



	//public class SortableBindingList<T> :BindingList<T>
	//{
	//    // a cache of functions that perform the sorting for a given type, property, and sort direction
	//    private static readonly Dictionary<string, Func<List<T>, IEnumerable<T>>> m_orderby_expression_cache = new Dictionary<string, Func<List<T>, IEnumerable<T>>>();

	//    //private List<T>            m_list;            // reference to the list provided at the time of instantiation
	//    private bool               m_is_sorted;
	//    private ListSortDirection  m_sort_direction;
	//    private PropertyDescriptor m_sort_property;

	//    //// function that refereshes the contents of the base classes collection of elements
	//    //private readonly Action<SortableBindingList<T>, List<T>> PopulateBaseList = (a, b) => a.ResetItems(b);

	//    //public SortableBindingList()
	//    //{
	//    //    m_list = new List<T>();
	//    //}

	//    //public SortableBindingList(IEnumerable<T> enumerable)
	//    //{
	//    //    m_list = enumerable.ToList();
	//    //    ResetItems(m_list);
	//    //    //PopulateBaseList(this, m_list);
	//    //}

	//    //public SortableBindingList(List<T> list)
	//    //{
	//    //    m_list = list;
	//    //    ResetItems(m_list);
	//    //    //PopulateBaseList(this, m_list);
	//    //}

	//    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
	//    {
	//        // Look for an appropriate sort method in the cache if not found .
	//        // Call CreateOrderByMethod to create one. 
	//        // Apply it to the original list.
	//        // Notify any bound controls that the sort has been applied.
	//        m_sort_property = prop;
	//        m_sort_direction = direction;
	//        m_is_sorted = true;

	//        var orderByMethodName = m_sort_direction == ListSortDirection.Ascending ? "OrderBy" : "OrderByDescending";

	//        var cacheKey = typeof(T).GUID + prop.Name + orderByMethodName;
	//        if (!m_orderby_expression_cache.ContainsKey(cacheKey))
	//            CreateOrderByMethod(prop, orderByMethodName, cacheKey);

	//        ResetItems(m_orderby_expression_cache[cacheKey](this.ToList()).ToList());
	//        //ResetItems(m_orderby_expression_cache[cacheKey](m_list).ToList());
	//        ResetBindings();
	//    }

	//    private void CreateOrderByMethod(PropertyDescriptor prop, string orderByMethodName, string cacheKey)
	//    {
	//        // Create a generic method implementation for IEnumerable<T>. Cache it.
	//        var property               = typeof(T).GetProperty(prop.Name);
	//        var src_param              = Expression.Parameter(typeof(List<T>), "source");
	//        var lambda_param           = Expression.Parameter(typeof(T), "lambdaParameter");
	//        var propertySelectorLambda = Expression.Lambda(Expression.MakeMemberAccess(lambda_param, property), lambda_param);

	//        var orderby_method = typeof(Enumerable).GetMethods()
	//                                .Where(a => a.Name == orderByMethodName && a.GetParameters().Length == 2)
	//                                .Single()
	//                                .MakeGenericMethod(typeof(T), prop.PropertyType);

	//        var orderby_expression = Expression.Lambda<Func<List<T>, IEnumerable<T>>>(
	//                                    Expression.Call(orderby_method, new Expression[] {src_param, propertySelectorLambda}),
	//                                    src_param);

	//        m_orderby_expression_cache.Add(cacheKey, orderby_expression.Compile());
	//    }

	//    protected override void RemoveSortCore()
	//    {
	//        m_sort_direction = ListSortDirection.Ascending;
	//        m_sort_property = null;
	//        base.RemoveSortCore();
	//        //ResetItems(m_list);
	//    }

	//    private void ResetItems(List<T> items)
	//    {
	//        base.ClearItems();
	//        for (int i = 0; i < items.Count; i++)
	//            base.InsertItem(i, items[i]);
	//    }

	//    /// <summary> Gets a value indicating whether the list supports sorting. </summary>
	//    protected override bool SupportsSortingCore
	//    {
	//        get { return true; }
	//    }

	//    /// <summary> Gets the direction the list is sorted. </summary>
	//    protected override ListSortDirection SortDirectionCore
	//    {
	//        get { return m_sort_direction; }
	//    }

	//    /// <summary> Gets the property descriptor that is used for sorting the list if sorting is implemented in a derived class; otherwise, returns null. </summary>
	//    protected override PropertyDescriptor SortPropertyCore
	//    {
	//        get { return m_sort_property; }
	//    }

	//    /// <summary> Gets a value indicating whether the list is sorted. </summary>
	//    protected override bool IsSortedCore
	//    {
	//        get { return m_is_sorted; }
	//    }

	//    //protected override void OnListChanged(ListChangedEventArgs e)
	//    //{
	//    //    //m_list = Items.ToList();
	//    //    base.OnListChanged(e);
	//    //}

	//}
