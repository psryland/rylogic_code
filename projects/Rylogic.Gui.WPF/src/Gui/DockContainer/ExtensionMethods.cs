using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>Extension method helpers</summary>
	internal static class Extn
	{
		/// <summary>True if this dock site is a edge</summary>
		public static bool IsEdge(this EDockSite ds)
		{
			return ds == EDockSite.Left || ds == EDockSite.Top || ds == EDockSite.Right || ds == EDockSite.Bottom;
		}
	}

}
