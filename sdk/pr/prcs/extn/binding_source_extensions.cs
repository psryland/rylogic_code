//***************************************************
// Binding Source Extensions
//  Copyright © Rylogic Ltd 2010
//***************************************************

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

		/// <summary>Returns an RAII object that suspends raising event</summary>
		public static Scope BlockEvents(this BindingSource bs)
		{
			return Scope.Create(() => bs.RaiseListChangedEvents = false, () => bs.RaiseListChangedEvents = true);
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
			[Test] public static void BindingSources()
			{}
		}
	}
}
#endif
