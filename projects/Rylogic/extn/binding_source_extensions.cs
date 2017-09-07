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
			// Use 'Current' for BindingSource<T>
			return bs.Position >= 0 && bs.Position < bs.Count ? bs.Current : null;
		}
		public static T CurrentOrDefault<T>(this BindingSource bs)
		{
			return (T)bs.CurrentOrDefault();
		}

		/// <summary>Temporarily detaches the DataSource from this binding source</summary>
		public static Scope PauseBinding(this BindingSource bs)
		{
			var src = bs.DataSource;
			return Scope.Create(
				() => bs.DataSource = null,
				() => bs.DataSource = src);
		}

		/// <summary>Returns an RAII object that suspends raising event</summary>
		public static Scope SuspendEvents(this BindingSource bs, bool reset_bindings_on_resume)
		{
			return Scope.Create(
				() =>
				{
					var r = bs.RaiseListChangedEvents;
					bs.RaiseListChangedEvents = false;
					return r;
				},
				r =>
				{
					bs.RaiseListChangedEvents = r;
					if (reset_bindings_on_resume)
						bs.ResetBindings(false);
				});
		}
		public static Scope SuspendEvents<T>(this BindingSource<T> bs, bool reset_bindings_on_resume, bool preserve_position)
		{
			var pos = bs.Position;
			return Scope.Create(
				() =>
				{
					// Notify pre-reset before any changes are made
					if (reset_bindings_on_resume)
						bs.PreResetBindings();

					// Save the raise list changed events state
					var r = bs.RaiseListChangedEvents;
					bs.RaiseListChangedEvents = false;
					return r;
				},
				r =>
				{
					// Restore the raise events state
					bs.RaiseListChangedEvents = r;

					// Notify reset
					if (reset_bindings_on_resume)
						bs.ResetBindings(false, preserve_position:false, include_pre_reset:false);

					// Restore position
					if (preserve_position)
						bs.Position = pos;
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
