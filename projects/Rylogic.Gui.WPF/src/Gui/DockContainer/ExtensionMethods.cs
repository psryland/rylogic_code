using System;
using System.Windows.Controls;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>Extension method helpers</summary>
	public static class Extn
	{
		/// <summary>Convert a DockPanel dock location to an 'EDockSite'</summary>
		public static EDockSite ToDockSite(this Dock dock)
		{
			switch (dock)
			{
				case Dock.Left: return EDockSite.Left;
				case Dock.Top: return EDockSite.Top;
				case Dock.Right: return EDockSite.Right;
				case Dock.Bottom: return EDockSite.Bottom;
				default: throw new Exception($"Unknown dock value {dock}");
			}
		}

		/// <summary>Convert an 'EDockSite' to a DockPanel dock location</summary>
		public static Dock ToDock(this EDockSite docksite)
		{
			switch (docksite)
			{
				case EDockSite.Centre:
				case EDockSite.Left: return Dock.Left;
				case EDockSite.Top: return Dock.Top;
				case EDockSite.Right: return Dock.Right;
				case EDockSite.Bottom: return Dock.Bottom;
				case EDockSite.None:
					throw new Exception($"No equivalent of {docksite} for DockPanel.Dock values");
				default:
					throw new Exception($"Unknown dock site value {docksite}");
			}
		}

		/// <summary>True if this dock site is a edge</summary>
		public static bool IsEdge(this EDockSite ds)
		{
			return ds == EDockSite.Left || ds == EDockSite.Top || ds == EDockSite.Right || ds == EDockSite.Bottom;
		}

		/// <summary>True if this dock site is Left or Right</summary>
		public static bool IsVertical(this EDockSite ds)
		{
			return ds == EDockSite.Left || ds == EDockSite.Right;
		}

		/// <summary>True if this dock site is Top or Bottom</summary>
		public static bool IsHorizontal(this EDockSite ds)
		{
			return ds == EDockSite.Top || ds == EDockSite.Bottom;
		}

		/// <summary>Convert a dock site address to a string description of the location</summary>
		public static string Description(this EDockSite[] address)
		{
			return string.Join(",", address);
		}
	}
}
