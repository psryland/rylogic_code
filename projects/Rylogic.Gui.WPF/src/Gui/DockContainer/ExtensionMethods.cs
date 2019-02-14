namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>Extension method helpers</summary>
	public static class Extn
	{
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
