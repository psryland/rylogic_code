//***************************************************
// Binding Source Extensions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System.ComponentModel;
using System.Windows.Forms;
using pr.util;

namespace pr.extn
{
	public static class BindingSourceExtensions
	{
		public static object CurrentOrDefault(this BindingSource bs)
		{
			return bs.Position >= 0 && bs.Position < bs.Count ? bs.Current : null;
		}

		/// <summary>Temporarily detaches the DataSource from this binding source</summary>
		public static Scope PauseBinding(this BindingSource bs)
		{
			var sess_src = bs.DataSource;
			return Scope.Create(() => bs.DataSource = null, () => bs.DataSource = sess_src);
		}

		/// <summary>Returns an RAII object that suspends raising event</summary>
		public static Scope BlockEvents(this BindingSource bs, bool refresh_on_resume)
		{
			return Scope.Create(
				() =>
					{
						bs.RaiseListChangedEvents = false;
					},
				() =>
					{
						bs.RaiseListChangedEvents = true;
						if (refresh_on_resume)
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

namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal partial class UnitTests
	{
		internal static partial class TestExtensions
		{
			[Test] public static void BindingSourceExtns()
			{}
		}
	}
}
#endif
