//***************************************************
// Binding Source Extensions
//  Copyright (c) Rylogic Ltd 2010
//***************************************************

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using pr.container;
using pr.util;

namespace pr.extn
{
	public static class BindingSourceExtensions
	{
		/// <summary>Add a range of items to the source</summary>
		public static void AddRange(this BindingSource bs, IEnumerable items)
		{
			foreach (var item in items)
				bs.Add(item);
		}
		public static void AddRange<T>(this BindingSource<T> bs, IEnumerable<T> items)
		{
			foreach (var item in items)
				bs.Add(item);
		}

		/// <summary>Returns the 'current' item in the binding source or null (rather than throwing an exception)</summary>
		public static object CurrentOrDefault(this BindingSource bs)
		{
			return bs.Position >= 0 && bs.Position < bs.Count ? bs.Current : null;
		}
		public static T CurrentOrDefault<T>(this BindingSource bs)
		{
			return (T)bs.CurrentOrDefault();
		}
		public static T CurrentOrDefault<T>(this BindingSource<T> bs)
		{
			return bs.Position >= 0 && bs.Position < bs.Count ? bs.Current : default(T);
		}

		/// <summary>Temporarily detaches the DataSource from this binding source</summary>
		public static Scope PauseBinding(this BindingSource bs)
		{
			var sess_src = bs.DataSource;
			return Scope.Create(() => bs.DataSource = null, () => bs.DataSource = sess_src);
		}

		/// <summary>Returns an RAII object that suspends raising event</summary>
		public static Scope SuspendEvents(this BindingSource bs, bool reset_bindings_on_resume)
		{
			return Scope.Create(
				() =>
					{
						bs.RaiseListChangedEvents = false;
					},
				() =>
					{
						bs.RaiseListChangedEvents = true;
						if (reset_bindings_on_resume)
							bs.ResetBindings(false);
					});
		}

		/// <summary>True if the list changed event is probably something you care about</summary>
		public static bool WorthWorryingAbout(this ListChangedType lct)
		{
			return
				lct == ListChangedType.ItemAdded   ||
				lct == ListChangedType.ItemChanged ||
				lct == ListChangedType.ItemMoved   ||
				lct == ListChangedType.ItemDeleted ||
				lct == ListChangedType.Reset;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestBindingSourceExtns
	{
		[Test] public static void BindingSourceExtns()
		{}
	}
}
#endif
