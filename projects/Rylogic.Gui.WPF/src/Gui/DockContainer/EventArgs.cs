using System;
using System.Xml.Linq;

namespace Rylogic.Gui.WPF
{
	using DockContainerDetail;

	/// <summary>Args for when the active content on the dock container or dock pane changes</summary>
	public class ActiveContentChangedEventArgs : EventArgs
	{
		public ActiveContentChangedEventArgs(IDockable old, IDockable nue)
		{
			ContentOld = old;
			ContentNew = nue;
		}

		/// <summary>The content that was active</summary>
		public IDockable ContentOld { get; }

		/// <summary>The content that is becoming active</summary>
		public IDockable ContentNew { get; }
	}

	/// <summary>Args for when the active content on the dock container or dock pane changes</summary>
	public class ActivePaneChangedEventArgs : EventArgs
	{
		public ActivePaneChangedEventArgs(DockPane old, DockPane nue)
		{
			PaneOld = old;
			PaneNew = nue;
		}

		/// <summary>The pane that was active</summary>
		public DockPane PaneOld { get; }

		/// <summary>The pane that is becoming active</summary>
		public DockPane PaneNew { get; }
	}

	/// <summary>Args for when dockables are moved within the dock container</summary>
	public class DockableMovedEventArgs : EventArgs
	{
		public DockableMovedEventArgs(EAction action, IDockable who)
		{
			Action = action;
			Dockable = who;
		}

		/// <summary>What happened to the dockable</summary>
		public EAction Action { get; }

		/// <summary>The dockable that is being added or removed</summary>
		public IDockable Dockable { get; }

		/// <summary>Actions that can happen to a dockable</summary>
		public enum EAction
		{
			Added = TreeChangedEventArgs.EAction.Added,
			Removed = TreeChangedEventArgs.EAction.Removed,
			AddressChanged,
		}
	}

	/// <summary>Args for when the DockContainer is changed on a DockControl</summary>
	public class DockContainerChangedEventArgs : EventArgs
	{
		public DockContainerChangedEventArgs(DockContainer old, DockContainer nue)
		{
			Previous = old;
			Current = nue;
		}

		/// <summary>The old dock container</summary>
		public DockContainer Previous { get; }

		/// <summary>The new dock container</summary>
		public DockContainer Current { get; }
	}

	/// <summary>Args for when layout is being saved</summary>
	public class DockContainerSavingLayoutEventArgs : EventArgs
	{
		public DockContainerSavingLayoutEventArgs(XElement node)
		{
			Node = node;
		}

		/// <summary>The XML element to add data to</summary>
		public XElement Node { get; }
	}

	/// <summary>Args for when panes or branches are added to a tree</summary>
	internal class TreeChangedEventArgs : EventArgs
	{
		public enum EAction
		{
			/// <summary>The non-null Dockable, DockPane, or Branch was added to the tree</summary>
			Added,

			/// <summary>The non-null Dockable, DockPane, or Branch was removed from the tree</summary>
			Removed,

			/// <summary>The Dockable that has became active within DockPane in the tree</summary>
			ActiveContent,
		}

		public TreeChangedEventArgs(EAction action, DockControl dockcontrol = null, DockPane pane = null, Branch branch = null)
		{
			Action = action;
			DockControl = dockcontrol;
			DockPane = pane;
			Branch = branch;
		}

		/// <summary>The type of change that occurred</summary>
		public EAction Action { get; }

		/// <summary>Non-null if it was a dockable that was added or removed</summary>
		public DockControl DockControl { get; }

		/// <summary>Non-null if it was a dock pane that was added or removed</summary>
		public DockPane DockPane { get; }

		/// <summary>Non-null if it was a branch that was added or removed</summary>
		public Branch Branch { get; }
	}
}
