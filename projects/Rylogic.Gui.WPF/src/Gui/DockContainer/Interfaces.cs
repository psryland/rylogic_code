using System;

namespace Rylogic.Gui.WPF
{
	using DockContainerDetail;

	/// <summary>A control that can be docked within a DockContainer</summary>
	public interface IDockable
	{
		/// <summary>The docking implementation object that provides the docking functionality</summary>
		DockControl DockControl { get; }
	}

	/// <summary>Interface for classes that have a Root branch and active content</summary>
	internal interface ITreeHost
	{
		/// <summary>The dock container that owns this instance</summary>
		DockContainer DockContainer { get; }

		/// <summary>The root of the tree of dock panes</summary>
		Branch Root { get; }

		/// <summary>Add a dockable instance to this branch at the position described by 'location'. 'index' is the index within the destination dock pane</summary>
		DockPane Add(IDockable dockable, int index, params EDockSite[] location);
	}

	/// <summary>Marker interface for DockPanes and Branches</summary>
	internal interface IPaneOrBranch : IDisposable
	{
		/// <summary>The parent of this dock pane or branch</summary>
		Branch ParentBranch { get; }
	}

	/// <summary>Locations in the dock container where dock panes or trees of dock panes, can be docked.</summary>
	public enum EDockSite
	{
		// Note: Order here is important.
		// These values are also used as indices into arrays.

		/// <summary>Docked to the main central area of the window</summary>
		Centre = 0,

		/// <summary>Docked to the left side</summary>
		Left = 1,

		/// <summary>Docked to the right side</summary>
		Right = 2,

		/// <summary>Docked to the top</summary>
		Top = 3,

		/// <summary>Docked to the bottom</summary>
		Bottom = 4,

		/// <summary>Not docked</summary>
		None = 5,
	}

	/// <summary>Dock site mask value</summary>
	[Flags]
	internal enum EDockMask
	{
		None = 0,
		Centre = 1 << EDockSite.Centre,
		Top = 1 << EDockSite.Top,
		Bottom = 1 << EDockSite.Bottom,
		Left = 1 << EDockSite.Left,
		Right = 1 << EDockSite.Right,
	}

	/// <summary>States for a tab button</summary>
	public enum ETabState
	{
		Inactive,
		Active,
		Flashing,
	}
}