//**********************************************************************
// DockContainer
//  Copyright (c) Rylogic Limited 2015
//**********************************************************************
// Use:
//   Add a dock container to a form.
//   Create controls that implement 'IDockable'
//   Add the Dockables to the dock container
//   Use DockState on the IDockable to control where the item is docked
//
// Design:
//   A DockPane is a container of IDockable items.
//   When the DockState of an IDockable is set, a pane is created dynamically in order to
//   contain that dockable at the given dock location. Panes and be floated, or sunk (docked
//   into the control). DockContainers can nest. When more than one Pane is docked to the
//   same location within a dock container, a new child DockContainer is created at that location
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Permissions;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;

namespace pr.gui
{
	/// <summary>A control that can be docked within a DockContainer</summary>
	public interface IDockable
	{
		/// <summary>The docking implementation object that provides the docking functionality</summary>
		DockControl DockControl { get; }
	}

	/// <summary>
	/// The locations of where an IDockable can be docked.
	/// Actually, it is the DockPane that has a dock state, not the IDockable.
	/// DockPanes are created on demand to host the IDockable objects when they are docked.</summary>
	public enum EDockState
	{
		// Note: these values are also used as an index
		// Order effects DockStyle, smaller values dock w.r.t the area remaining after larger values have been docked
		// AutoHide locations are effectively unrelated to the non-auto hide locations. Each can exist independently to the other.

		/// <summary>The content is docked in the 'hidden' pane</summary>
		None,

		/// <summary>Docked to the main central area of the window</summary>
		Centre,

		/// <summary>Docked to the top</summary>
		Top,

		/// <summary>Docked to the bottom</summary>
		Bottom,

		/// <summary>Docked to the left side</summary>
		Left,

		/// <summary>Docked to the right side</summary>
		Right,

		/// <summary>A window that pops out from the left</summary>
		LeftAutoHide,

		/// <summary>A window that pops out from the left</summary>
		TopAutoHide,

		/// <summary>A window that pops out from the left</summary>
		RightAutoHide,

		/// <summary>A window that pops out from the left</summary>
		BottomAutoHide,

		/// <summary>A separate window that floats above the dock container and is owned by the dock container's parent form</summary>
		Float,
	}

	#region DockContainer

	/// <summary>A dock container is the parent control that manages docking of controls that implement IDockable.</summary>
	public partial class DockContainer :Panel
	{
		/// <summary>Create a new top level dock container</summary>
		public DockContainer()
			: this(null)
		{ }

		/// <summary>Create a dock container optionally as a child of another dock container</summary>
		private DockContainer(DockContainer parent)
		{
			// Create the array of pointers to panes
			m_panes = new DockPane[Enum<EDockState>.Count];

			// Create default options
			Options = new OptionData();

			// This container will have a parent if it is nested within another dock container
			ParentContainer = parent;
		}

		/// <summary>Options for the dock container</summary>
		public OptionData Options { get; private set; }

		/// <summary>Get/Set the active pane</summary>
		public DockPane ActivePane
		{
			get { return m_impl_active_pane; }
			set
			{
				// Careful, need to handle 'DockPane.Activated' or 'DockContainer.ActivePane' being assigned to
				if (m_impl_active_pane == value) return;

				var old = m_impl_active_pane;
				if (m_impl_active_pane != null)
				{
				}
				m_impl_active_pane = value;
				if (m_impl_active_pane != null)
				{
				}

				// Notify observers of each pane about activation changed
				old?.OnActivatedChanged();
				value?.OnActivatedChanged();

				// Notify that the active pane has changed
				OnActivePaneChanged(new ActivePaneChangedEventArgs(old, value));
			}
		}
		private DockPane m_impl_active_pane;

		/// <summary>
		/// Get/Set the active content. This will cause the pane that the content is on to also become
		/// active. To change the active content within a pane without making it active, assign to the pane's
		/// ActiveContent property</summary>
		public IDockable ActiveContent
		{
			get { return ActivePane?.ActiveContent; }
			set
			{
				if (ActiveContent == value) return;

				// Save the old content
				var old = ActiveContent;

				// Switch panes
				ActivePane = value?.DockControl.DockPane;

				// Ensure 'value' is that active content on its pane
				if (ActivePane != null)
					ActivePane.ActiveContent = value;

				// Raise ActiveContentChanged
				OnActiveContentChanged(new ActiveContentChangedEventArgs(old, value));
			}
		}

		/// <summary>The dockable objects managed by this container</summary>
		public IEnumerable<IDockable> Contents
		{
			get
			{
				foreach (var pane in m_panes)
					foreach (var item in pane.Content)
						yield return item;
			}
		}

		/// <summary>
		/// Get/Set the Pane at a dock location.
		/// Returns null if there is no pane for the given location.
		/// If setting a pane in a location that already exists, the contents of the inbound
		/// pane (value) are merged with the existing pane and the inbound pane is destroyed.</summary>
		public DockPane this[EDockState location]
		{
			get { return m_panes[(int)location]; }
			set
			{
				var index = (int)location;
				if (m_panes[index] == value) return;

				using (this.SuspendLayout(true))
				{
					// Assigning null to a location removes (but doesn't dispose) the pane at that location
					if (value == null)
					{
						var existing = m_panes[index];
						if (existing != null)
						{
							// Notify that a pane is being removed
							OnPanesChanged(new PanesChangedEventArgs(PanesChangedEventArgs.EChg.Removing, existing));

							// Assign 'm_panes[index]' first so that setting the dock state doesn't recursively call this method
							m_panes[index] = null;
							existing.DockState = EDockState.None;

							// Remove the pane control from our children
							Controls.Remove(existing);

							// Notify that a pane was removed
							OnPanesChanged(new PanesChangedEventArgs(PanesChangedEventArgs.EChg.Removed, existing));

							// Raise DockStateChanged for each dockable that was removed
							var args = new DockStateChangedEventArgs(location, EDockState.None);
							existing.Content.ForEach(x => x.DockControl.OnDockStateChanged(args));
						}
					}
					else
					{
						// If 'value' belongs to a different container
						if (value.DockContainer != this)
							throw new Exception("Panes can only be docked within their owning DockContainer");

						// If 'value' is already docked within this container, then remove it from that location
						// since we are moving the entire pane to a difference location.
						var idx = m_panes.IndexOf(value);
						if (idx != -1)
							this[(EDockState)idx] = null;

						// Make a copy of the content within 'value' so we can notify of the dock state changing for just those items
						var moving_contents = value.Content.ToArray();
						var old_dock_state = value.DockState;

						// No pane currently at the requested location? Add and dock 'value' as is
						if (m_panes[index] == null)
						{
							// Notify that a pane is being added
							OnPanesChanged(new PanesChangedEventArgs(PanesChangedEventArgs.EChg.Adding, value));

							// Assign 'value' here so that setting the DockState later doesn't recursively call this method
							m_panes[index] = value;
							value.DockState = location;

							// Re-add all panes so that the sibling order is correct for docking to work
							Controls.Clear();
							Controls.AddRange(m_panes.Where(x => x != null).ToArray());

							// Use standard control docking to position the pane
							// Note: DockStyle.Fill only considers other docked sibling that are earlier in the
							// sibling order (sibling order seems to be from last to first in Controls).
							switch (location)
							{
							case EDockState.Centre: value.Dock = DockStyle.Fill; break;
							case EDockState.Left: value.Dock = DockStyle.Left; break;
							case EDockState.Top: value.Dock = DockStyle.Top; break;
							case EDockState.Right: value.Dock = DockStyle.Right; break;
							case EDockState.Bottom: value.Dock = DockStyle.Bottom; break;
							}

							// Notify that a pane was added
							OnPanesChanged(new PanesChangedEventArgs(PanesChangedEventArgs.EChg.Added, value));
						}
						// Otherwise move the content and event handlers from 'value' into the existing pane
						else
						{
							m_panes[index].Cannibolise(ref value);
						}

						var args = new DockStateChangedEventArgs(old_dock_state, location);

						// Raise DockStateChanged for the pane that moved
						m_panes[index].OnDockStateChanged(args);

						// Raise DockStateChanged for each dockable that was moved
						moving_contents?.ForEach(x => x.DockControl.OnDockStateChanged(args));
					}
				}
			}
		}
		private DockPane[] m_panes;

		/// <summary>Non null is this is a nested dock container</summary>
		private DockContainer ParentContainer { get; set; }

		/// <summary>Raised when panes are added or removed from the dock container</summary>
		public event EventHandler<PanesChangedEventArgs> PanesChanged;
		protected void OnPanesChanged(PanesChangedEventArgs args)
		{
			PanesChanged.Raise(this, args);
		}

		/// <summary>Raised when content is added or removed from the dock container</summary>
		public event EventHandler<ContentChangedEventArgs> ContentChanged;
		protected void OnContentChanged(ContentChangedEventArgs args)
		{
			ContentChanged.Raise(this, args);
		}

		/// <summary>Raised whenever the active pane changes in the dock container</summary>
		public event EventHandler<ActivePaneChangedEventArgs> ActivePaneChanged;
		internal void OnActivePaneChanged(ActivePaneChangedEventArgs args)
		{
			ActivePaneChanged.Raise(this, args);
		}

		/// <summary>Raised whenever the active content for the dock container changes</summary>
		public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged;
		internal void OnActiveContentChanged(ActiveContentChangedEventArgs args)
		{
			ActiveContentChanged.Raise(this, args);
		}

		/// <summary>Add a dockable instance to this dock container</summary>
		public void Add(IDockable dockable, EDockState? dock_state = null)
		{
			if (dockable == null)
				throw new ArgumentNullException(nameof(dockable), "Cannot add 'null' content to a dock container");

			// If already on a pane, remove first
			dockable.DockControl.DockPane?.Remove(dockable);

			// Notify of a dockable about to be added
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Adding, dockable));

			// Create a dock pane to host the dockable
			var pane = new DockPane(this);
			pane.Add(dockable);

			// Dock the pane at the default location for 'dockable'
			// If a pane exists at that location they will be merged.
			pane.DockState = dock_state ?? dockable.DockControl.DefaultDockState;

			// Notify that a dockable has been added
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Added, dockable));
		}

		/// <summary>Remove a dockable instance from this dock container</summary>
		public void Remove(IDockable dockable)
		{
			if (dockable == null)
				return;
			if (dockable.DockControl.DockPane == null)
				return;
			if (dockable.DockControl.DockPane.DockContainer != this)
				throw new Exception("'dockable' is not a member of this dock container and so can't be removed");

			// Notify of a dockable about to be removed
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Removing, dockable));

			// Get the pane that 'dockable' belongs to
			var pane = dockable.DockControl.DockPane;
			pane.Remove(dockable);

			// If we just removed the last item from the pane, remove the pane as well
			if (pane.Content.Count == 0)
				this[pane.DockState] = null;

			// Notify that a dockable has been removed
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Removed, dockable));
		}

		/// <summary>Layout the control</summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			using (this.SuspendLayout(true))
			{
				var opts = Options;

				// Set the padding for docked panes
				// If there are auto hide panes, then leave room for them
				DockPadding.Left   = this[EDockState.LeftAutoHide] != null ? 80 : 0;
				DockPadding.Right  = this[EDockState.RightAutoHide] != null ? 80 : 0;
				DockPadding.Top    = this[EDockState.TopAutoHide] != null ? 80 : 0;
				DockPadding.Bottom = this[EDockState.BottomAutoHide] != null ? 80 : 0;

				// Get the available space (excluding dock padding)
				var rect = DisplayRectangle;

				// Calculate the pane sizes to fit the client area
				var l = opts.DockArea.Left   >= 1f ? (int)opts.DockArea.Left   : (int)(rect.Width  * opts.DockArea.Left  );
				var r = opts.DockArea.Right  >= 1f ? (int)opts.DockArea.Right  : (int)(rect.Width  * opts.DockArea.Right );
				var t = opts.DockArea.Top    >= 1f ? (int)opts.DockArea.Top    : (int)(rect.Height * opts.DockArea.Top   );
				var b = opts.DockArea.Bottom >= 1f ? (int)opts.DockArea.Bottom : (int)(rect.Height * opts.DockArea.Bottom);

				// If any pane exceeds the client area, reduce it to fit
				if (l > rect.Width) l = rect.Width;
				if (r > rect.Width) r = rect.Width;
				if (t > rect.Height) t = rect.Height;
				if (b > rect.Height) b = rect.Height;

				// If opposite panes overlap, reduce the sizes so that they meet
				var over_w = (rect.Width  - l - r - 1) / 2;
				var over_h = (rect.Height - t - b - 1) / 2;
				if (over_w < 0) { l += over_w; r += over_w; }
				if (over_h < 0) { t += over_h; b += over_h; }

				// Set the sizes of the dock panes
				if (this[EDockState.Left] != null) this[EDockState.Left].Size = new Size(l, rect.Height);
				if (this[EDockState.Right] != null) this[EDockState.Right].Size = new Size(r, rect.Height);
				if (this[EDockState.Top] != null) this[EDockState.Top].Size = new Size(rect.Width, t);
				if (this[EDockState.Bottom] != null) this[EDockState.Bottom].Size = new Size(rect.Width, b);

				base.OnLayout(levent);
			}
		}

		/// <summary>Initiate dragging of a pane or content</summary>
		internal void DragBegin(object item)
		{
			// Create a form for displaying the dock site locations and handling the drop of a pane or content
			using (var drop_handler = new DropHandler(this, item))
				drop_handler.ShowDialog(this);
		}
	}

	#endregion

	#region DockPane

	/// <summary>
	/// A pane groups a set of IDockable items together. Only one IDockable is displayed at a time in the pane,
	/// but tabs for all dockable items are displayed along the top or bottom.</summary>
	public class DockPane :UserControl
	{
		public DockPane(DockContainer owner)
		{
			if (owner == null)
				throw new ArgumentNullException("The owning dock container cannot be null");

			using (this.SuspendLayout(true))
			{
				DockContainer = owner;
				m_content     = new List<IDockable>();
				TabStripCtrl  = new DockContainer.TabStripControl(this) { Dock = DockStyle.Bottom };  // order of controls here is important
				TitleCtrl     = new DockContainer.PaneTitleControl(this) { Dock = DockStyle.Top }; // we want the 'TitleCtrl' to dock first
				SplitterCtrl  = new DockContainer.Splitter(this);

				DockState        = EDockState.None;
				//BorderStyle      = BorderStyle.FixedSingle;
			}
		}
		protected override void Dispose(bool disposing)
		{
			// Note: we don't own any of the content
			ActiveContent = null;
			TitleCtrl = null;
			TabStripCtrl = null;
			base.Dispose(disposing);
		}
		public override string ToString()
		{
			return CaptionText;
		}

		/// <summary>The dock container that this pane belongs too</summary>
		public DockContainer DockContainer { get; private set; }

		/// <summary>The content hosted by this pane</summary>
		public IReadOnlyList<IDockable> Content { get { return m_content; } }
		private List<IDockable> m_content;

		/// <summary>
		/// The content in this pane that was last active (Not necessarily the active content for the dock container)
		/// There should always be active content while 'Content' is not empty. Empty panes are destroyed.
		/// If this pane is not the active pane, then setting the active content only raises events for this pane.
		/// If this is the active pane, then the dock container events are also raised.</summary>
		public IDockable ActiveContent
		{
			get { return m_impl_active_content; }
			set
			{
				if (m_impl_active_content == value) return;

				// Only content that is in this pane can be made active for this pane
				if (value != null && !m_content.Contains(value))
					throw new Exception("The dockable '{0}' item has not been added to this pane so can not be made that active content.".Fmt(value.DockControl.PersistName));

				var old0 = m_impl_active_content;
				var old1 = DockContainer.ActiveContent;
				using (this.SuspendLayout(true))
				{
					if (m_impl_active_content != null)
					{
						// Remove from the child controls collection
						Controls.Remove(m_impl_active_content.DockControl.Owner);
					}
					m_impl_active_content = value;
					if (m_impl_active_content != null)
					{
						// When content becomes active, add it as a child control of the pane
						Controls.Add(m_impl_active_content.DockControl.Owner);
					}
				}

				// Raise the content changed event on this pane first, then on the dock container
				// if this pane is also the active pane for the container.
				OnActiveContentChanged(new ActiveContentChangedEventArgs(old0, value));
				if (DockContainer.ActivePane == this)
					DockContainer.OnActiveContentChanged(new ActiveContentChangedEventArgs(old1, value));
			}
		}
		private IDockable m_impl_active_content;

		/// <summary>Raised whenever the active content for this dock pane changes</summary>
		public event EventHandler<ActiveContentChangedEventArgs> ActiveContentChanged;
		protected void OnActiveContentChanged(ActiveContentChangedEventArgs args)
		{
			ActiveContentChanged.Raise(this, args);
		}

		/// <summary>A control that draws the caption title bar for the pane</summary>
		public DockContainer.PaneTitleControl TitleCtrl
		{
			get { return m_impl_caption_ctrl; }
			set
			{
				if (m_impl_caption_ctrl == value) return;
				using (this.SuspendLayout(true))
				{
					if (m_impl_caption_ctrl != null)
					{
						Controls.Remove(m_impl_caption_ctrl);
						Util.Dispose(ref m_impl_caption_ctrl);
					}
					m_impl_caption_ctrl = value;
					if (m_impl_caption_ctrl != null)
					{
						Controls.Add(m_impl_caption_ctrl);
					}
				}
			}
		}
		private DockContainer.PaneTitleControl m_impl_caption_ctrl;

		/// <summary>A control that draws the tabs for this pane</summary>
		public DockContainer.TabStripControl TabStripCtrl
		{
			get { return m_impl_tab_strip_ctrl; }
			set
			{
				if (m_impl_tab_strip_ctrl == value) return;
				using (this.SuspendLayout(true))
				{
					if (m_impl_tab_strip_ctrl != null)
					{
						Controls.Remove(m_impl_tab_strip_ctrl);
						Util.Dispose(ref m_impl_tab_strip_ctrl);
					}
					m_impl_tab_strip_ctrl = value;
					if (m_impl_tab_strip_ctrl != null)
					{
						Controls.Add(m_impl_tab_strip_ctrl);
					}
				}
			}
		}
		private DockContainer.TabStripControl m_impl_tab_strip_ctrl;

		/// <summary>A control used to resize this pane</summary>
		internal DockContainer.Splitter SplitterCtrl
		{
			get { return m_impl_splitter_ctrl; }
			set
			{
				if (m_impl_splitter_ctrl == value) return;
				using (this.SuspendLayout(true))
				{
					if (m_impl_splitter_ctrl != null)
					{
						Controls.Remove(m_impl_splitter_ctrl);
						Util.Dispose(ref m_impl_splitter_ctrl);
					}
					m_impl_splitter_ctrl = value;
					if (m_impl_splitter_ctrl != null)
					{
						Controls.Add(m_impl_splitter_ctrl);
					}
				}
			}
		}
		private DockContainer.Splitter m_impl_splitter_ctrl;

		/// <summary>
		/// Set the dock state for this pane. This means move all content in this
		/// pane to the given dock location. If a pane already exists at that location
		/// the content to merged with that pane and this pane is destructed.</summary>
		public EDockState DockState
		{
			get { return m_impl_dock_state; }
			set
			{
				if (m_impl_dock_state == value) return;

				// Add this pane to the given dock site. Update 'm_impl_dock_state' after this so that
				// the dock container knows which dock location we came from.
				if (value != EDockState.None)
					DockContainer[value] = this;

				m_impl_dock_state = value;
			}
		}
		private EDockState m_impl_dock_state;

		/// <summary>
		/// Raised whenever this dock pane is docked in a new location or floated
		/// 'sender' is the owner of the DockPane whose state changed.</summary>
		public event EventHandler<DockStateChangedEventArgs> DockStateChanged;
		internal void OnDockStateChanged(DockStateChangedEventArgs args)
		{
			DockStateChanged.Raise(this, args);
		}

		/// <summary>Get the text to display in the title bar for this pane</summary>
		public string CaptionText
		{
			get { return ActiveContent != null ? ActiveContent.DockControl.TabText : string.Empty; }
		}

		/// <summary>Add a dockable to this pane</summary>
		internal void Add(IDockable dockable)
		{
			if (dockable == null)
				throw new ArgumentNullException(nameof(dockable), "Cannot add 'null' content to a pane");

			// Ensure the dockable is not currently added
			dockable.DockControl.DockPane?.Remove(dockable);

			// Notify of a dockable about to be added
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Adding, dockable));

			// Add the dockable to our collection of dockables
			// Don't add the dockable as a child control yet,
			// that happens when the active content changes
			m_content.Add(dockable);
			dockable.DockControl.DockPane = this;

			// If this is the first dockable added, make it the active content
			if (m_content.Count == 1)
				ActiveContent = m_content.First();

			// Notify that a dockable has been added
			OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Added, dockable));
		}

		/// <summary>Remove a dockable from this pane</summary>
		internal void Remove(IDockable dockable)
		{
			if (dockable == null)
				return;
			if (dockable.DockControl.DockPane == null)
				return;
			if (dockable.DockControl.DockPane != this)
				throw new ArgumentException("'dockable' is not a member of this pane and so can't be removed");

			// Remove the dockable from our collection. If not in the collection, ignore it
			var idx = m_content.IndexOf(dockable);
			if (idx != -1)
			{
				// Notify of a dockable about to be removed
				OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Removing, dockable));

				// If we're about to remove the active content, make the next content the active content
				if (dockable == ActiveContent)
				{
					if (idx + 1 != m_content.Count) ActiveContent = m_content[idx + 1];
					else if (idx - 1 != -1) ActiveContent = m_content[idx - 1];
					else ActiveContent = null;
				}

				// Remove 'dockable' from the content collection
				m_content.RemoveAt(idx);
				dockable.DockControl.DockPane = null;

				// Notify that a dockable has been removed
				OnContentChanged(new ContentChangedEventArgs(ContentChangedEventArgs.EChg.Removed, dockable));
			}
		}

		/// <summary>Raised whenever the 'Content' collection is changed</summary>
		public event EventHandler<ContentChangedEventArgs> ContentChanged;
		protected void OnContentChanged(ContentChangedEventArgs args)
		{
			ContentChanged.Raise(this, args);
		}

		/// <summary>Move all content and event handlers from 'pane to this pane</summary>
		internal void Cannibolise(ref DockPane pane)
		{
			using (this.SuspendLayout(true))
			{
				// If this pane is currently empty, then we'll make the first item the active content
				var set_active = m_content.Count == 0;

				// Remove the content from 'pane.Controls'
				pane.Controls.RemoveIf<Control>(x => x is IDockable);

				// Move the content of 'pane' to this pane
				while (pane.m_content.Count != 0)
					Add(pane.m_content.First());

				// Move the event handlers
				ActiveContentChanged += pane.ActiveContentChanged;
				ContentChanged       += pane.ContentChanged;
				ActivatedChanged     += pane.ActivatedChanged;
				DockStateChanged     += pane.DockStateChanged;
				pane.ActiveContentChanged = null;
				pane.ContentChanged       = null;
				pane.ActivatedChanged     = null;
				pane.DockStateChanged     = null;

				// Set the active content in 'pane'
				if (set_active)
					ActiveContent = m_content.FirstOrDefault();

				// Dispose of the given pane
				Util.Dispose(ref pane);
			}
		}

		/// <summary>Get/Set this pane as activated. Activation causes the active content in this pane to be activated</summary>
		public bool Activated
		{
			get { return DockContainer.ActivePane == this; }
			set
			{
				// Assign this pane as the active one. This will cause the
				// previously active pane to have Activated = false called.
				// Careful, need to handle 'Activated' or 'DockContainer.ActivePane' being assigned to
				// The ActivePane handler will call OnActivatedChanged.
				DockContainer.ActivePane = value ? this : null;
			}
		}

		/// <summary>Raised when the pane is activated</summary>
		public event EventHandler ActivatedChanged;
		internal void OnActivatedChanged()
		{
			ActivatedChanged.Raise(this, EventArgs.Empty);
		}

		/// <summary>Layout the pane</summary>
		protected override void OnLayout(LayoutEventArgs e)
		{
			// Measure the remaining space and use that for the active content
			var content_rect = DisplayRectangle;

			// Position the title bar
			if (TitleCtrl != null)
			{
				var bounds = TitleCtrl.CalcBounds(DisplayRectangle);
				content_rect = content_rect.Subtract(bounds);
				TitleCtrl.Bounds = bounds;
			}

			// Position the tab strip
			if (TabStripCtrl != null)
			{
				var bounds = TabStripCtrl.CalcBounds(DisplayRectangle);
				content_rect = content_rect.Subtract(bounds);
				TabStripCtrl.Bounds = bounds;
			}

			// Use the remaining area for content
			if (ActiveContent != null)
			{
				ActiveContent.DockControl.Owner.Bounds = content_rect;
			}

			base.OnLayout(e);
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected override void WndProc(ref Message m)
		{
			// Watch for mouse activate. WM_MOUSEACTIVATE is sent to the inactive window under the
			// mouse. If that window passes the message to DefWindowProc then the parent window gets it.
			if (m.Msg == (int)Win32.WM_MOUSEACTIVATE)
				Activated = true;

			base.WndProc(ref m);
		}
	}

	#endregion

	#region DockControl

	/// <summary>Provides the implementation of the docking functionality</summary>
	public class DockControl
	{
		/// <summary>Create the docking functionality helper.</summary>
		/// <param name="owner">The control that docking is being provided for</param>
		/// <param name="persist_name">The name to use for this instance when saving the layout to XML</param>
		public DockControl(Control owner, string persist_name, EDockState default_dock_state = EDockState.Centre)
		{
			if (!(owner is IDockable))
				throw new Exception("The owning control must implement IDockable");

			Owner            = owner;
			PersistName      = persist_name;
			DefaultDockState = default_dock_state;
			TabText          = null;
			TabIcon          = null;
		}

		/// <summary>Get the control we're providing docking functionality for</summary>
		public Control Owner
		{
			get { return m_impl_owner; }
			private set
			{
				if (m_impl_owner == value) return;
				if (m_impl_owner != null)
				{
					// Focus only works on the child control that gets focus
					//m_impl_owner.GotFocus -= HandleGotFocus;
				}
				m_impl_owner = value;
				if (m_impl_owner != null)
				{
					//m_impl_owner.GotFocus += HandleGotFocus;
				}
			}
		}
		private Control m_impl_owner;

		/// <summary>Get/Set the pane that this content resides within.</summary>
		public DockPane DockPane
		{
			get { return m_impl_pane; }
			set
			{
				if (m_impl_pane == value) return;
				if (m_impl_pane != null)
				{
					if (m_impl_pane.Content.Contains((IDockable)Owner))
						m_impl_pane.Remove((IDockable)Owner);
				}
				m_impl_pane = value;
				if (m_impl_pane != null)
				{
					if (!m_impl_pane.Content.Contains((IDockable)Owner))
						m_impl_pane.Add((IDockable)Owner);
				}
			}
		}
		private DockPane m_impl_pane;

		/// <summary>Get/Set the pane that this content resides within.</summary>
		public DockContainer DockContainer
		{
			get { return DockPane?.DockContainer; }
			set
			{
				if (DockContainer == value) return;
				DockContainer?.Remove((IDockable)Owner);
				value?.Add((IDockable)Owner);
			}
		}

		/// <summary>The name to use for this instance when saving layout to XML</summary>
		public string PersistName { get; private set; }

		/// <summary>The dock state to use if not otherwise given</summary>
		public EDockState DefaultDockState { get; private set; }

		/// <summary>Dock this item at a new location within the DockContainer</summary>
		public EDockState DockState
		{
			get { return DockPane != null ? DockPane.DockState : EDockState.None; }
			set
			{
				if (DockState == value) return;

				// Must have an existing pane
				if (DockPane == null)
					throw new Exception("Can only change the dock state of this item after it has been added to a dock container");

				// Save the previous dock state
				var old = DockState;

				// Get the owning dock container
				var dc = DockPane.DockContainer;

				// Get or create the pane for the requested dock location
				var pane = dc[value] ?? new DockPane(dc);

				// Add this item to the pane
				pane.Add((IDockable)Owner);

				// Assign the pane to the dock container.
				// If a pane already exists in that location then they will merge.
				dc[value] = pane;

				// Save a reference to the pane we're in
				DockPane = dc[value];

				// Notify observers
				OnDockStateChanged(new DockStateChangedEventArgs(old, value));
			}
		}

		/// <summary>
		/// Raised whenever this dockable item is docked in a new location or floated
		/// 'sender' is the owner of the DockControl whose state changed.</summary>
		public event EventHandler<DockStateChangedEventArgs> DockStateChanged;
		internal void OnDockStateChanged(DockStateChangedEventArgs args)
		{
			DockStateChanged.Raise(Owner, args);
		}

		/// <summary>The text to display on the tab. Defaults to 'Owner.Text'</summary>
		public string TabText
		{
			get { return m_impl_tab_text ?? Owner.Text; }
			set { m_impl_tab_text = value; }
		}
		private string m_impl_tab_text;

		/// <summary>The icon to display on the tab. Defaults to '(Owner as Form)?.Icon'</summary>
		public Icon TabIcon
		{
			get { return m_impl_icon ?? (Owner as Form)?.Icon; }
			set { m_impl_icon = value; }
		}
		private Icon m_impl_icon;

		/// <summary></summary>
		private void HandleGotFocus(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}
	}

	#endregion

	#region Event Args

	/// <summary>Args for the DockStateChanged event</summary>
	public class DockStateChangedEventArgs :EventArgs
	{
		public DockStateChangedEventArgs(EDockState old, EDockState nue)
		{
			StateOld = old;
			StateNew = nue;
		}

		/// <summary>The old dock state</summary>
		public EDockState StateOld { get; private set; }

		/// <summary>The new dock state</summary>
		public EDockState StateNew { get; private set; }
	}

	/// <summary>Args for when the active content on the dock container or dock pane changes</summary>
	public class ActiveContentChangedEventArgs :EventArgs
	{
		public ActiveContentChangedEventArgs(IDockable old, IDockable nue)
		{
			ContentOld = old;
			ContentNew = nue;
		}

		/// <summary>The content that was active</summary>
		public IDockable ContentOld { get; private set; }

		/// <summary>The content that is becoming active</summary>
		public IDockable ContentNew { get; private set; }
	}

	/// <summary>Args for when the active content on the dock container or dock pane changes</summary>
	public class ActivePaneChangedEventArgs :EventArgs
	{
		public ActivePaneChangedEventArgs(DockPane old, DockPane nue)
		{
			PaneOld = old;
			PaneNew = nue;
		}

		/// <summary>The pane that was active</summary>
		public DockPane PaneOld { get; private set; }

		/// <summary>The pane that is becoming active</summary>
		public DockPane PaneNew { get; private set; }
	}

	/// <summary>Args for when IDockables are added or removed from a dock container</summary>
	public class ContentChangedEventArgs :EventArgs
	{
		public ContentChangedEventArgs(EChg what, IDockable who)
		{
			Change = what;
			Content = who;
		}

		/// <summary>The type of change that occurred</summary>
		public EChg Change { get; private set; }
		public enum EChg { Adding, Added, Removing, Removed }

		/// <summary>The content item involved in the change</summary>
		public IDockable Content { get; private set; }
	}

	/// <summary>Args for when DockPanes are added or removed from a dock container</summary>
	public class PanesChangedEventArgs :EventArgs
	{
		public PanesChangedEventArgs(EChg what, DockPane who)
		{
			Change = what;
			Pane = who;
		}

		/// <summary>The type of change that occurred</summary>
		public EChg Change { get; private set; }
		public enum EChg { Adding, Added, Removing, Removed }

		/// <summary>The content item involved in the change</summary>
		public DockPane Pane { get; private set; }
	}
	#endregion

	#region Custom Controls

	public partial class DockContainer
	{
		/// <summary>General options for the dock container</summary>
		public class OptionData
		{
			public OptionData()
			{
				AllowUserDocking = true;
				DockArea        = new DockAreaData(0.25f, 0.25f, 0.25f, 0.25f);
				TitleBar        = new TitleBarData();
				TabStrip        = new TabStripData();
			}

			/// <summary>Get/Set whether the user is allowed to drag and drop panes</summary>
			public bool AllowUserDocking { get; set; }

			/// <summary>The size of the Left, Top, Right, Bottom docking proportions</summary>
			public DockAreaData DockArea { get; private set; }
			public class DockAreaData
			{
				public DockAreaData(float left, float top, float right, float bottom)
				{
					Left   = left;
					Top    = top;
					Right  = right;
					Bottom = bottom;
				}

				/// <summary>
				/// The size of the left, top, right, bottom panes. If >= 1, then the value is interpreted
				/// as pixels, if less than 1 then interpreted as a fraction of the ClientRectangle width/height</summary>
				public float Left { get; set; }
				public float Top { get; set; }
				public float Right { get; set; }
				public float Bottom { get; set; }
			}

			/// <summary>Options for the dock pane title bars</summary>
			public TitleBarData TitleBar { get; private set; }
			public class TitleBarData
			{
				public TitleBarData()
				{
					ActiveGrad= new ColourSet(
						text: SystemColors.ActiveCaptionText,
						beg: SystemColors.GradientActiveCaption,
						end: SystemColors.InactiveCaption);

					InactiveGrad = new ColourSet(
						text: SystemColors.InactiveCaptionText,
						beg: SystemColors.GradientInactiveCaption,
						end: SystemColors.InactiveCaption,
						mode: LinearGradientMode.Vertical);

					Padding  = new Padding(2, 1, 2, 1);
					TextFont = SystemFonts.MenuFont;
				}

				///<summary>Colour gradient for the caption title bar in an active DockPane</summary>
				public ColourSet ActiveGrad { get; private set; }

				///<summary>Colour gradient for the caption title bar in an inactive DockPane</summary>
				public ColourSet InactiveGrad { get; private set; }

				/// <summary>Margin for the caption title bar text and buttons</summary>
				public Padding Padding { get; set; }

				/// <summary>The font to use for caption title bar text</summary>
				public Font TextFont { get; set; }
			}

			/// <summary>Options for the dock pane tab strips</summary>
			public TabStripData TabStrip { get; private set; }
			public class TabStripData
			{
				public TabStripData()
				{
					ActiveStrip = new ColourSet(
						text: SystemColors.ActiveCaptionText,
						beg: SystemColors.ControlDark,
						end: SystemColors.ControlDark);

					InactiveStrip = new ColourSet(
						text: SystemColors.InactiveCaptionText,
						beg: SystemColors.ControlDark,
						end: SystemColors.ControlDark);

					ActiveTab = new ColourSet(
						text: SystemColors.WindowText,
						beg: SystemColors.Window,
						end: SystemColors.Window,
						border: Color.Black);

					InactiveTab = new ColourSet(
						text: SystemColors.WindowText,
						beg: SystemColors.Control,
						end: SystemColors.Control);

					ActiveFont        = SystemFonts.MenuFont.Dup(FontStyle.Bold);
					InactiveFont      = SystemFonts.MenuFont;
					MinWidth          = 20;
					MaxWidth          = 200;
					TabSpacing        = 2;
					IconSize          = new Size(16, 16);
					IconToTextSpacing = 3;
					StripPadding      = new Padding(2, 2, 2, 0);
					TabPadding        = new Padding(2, 2, 2, 2);
				}

				///<summary>Colour gradient for the tab strip background in an active DockPane</summary>
				public ColourSet ActiveStrip { get; private set; }

				///<summary>Colour gradient for the tab strip background in an inactive DockPane</summary>
				public ColourSet InactiveStrip { get; private set; }

				///<summary>Colour gradient for the tab strip background in an active DockPane</summary>
				public ColourSet ActiveTab { get; private set; }

				///<summary>Colour gradient for the tab strip background in an inactive DockPane</summary>
				public ColourSet InactiveTab { get; private set; }

				/// <summary>The font to use for the active tab</summary>
				public Font ActiveFont { get; set; }

				/// <summary>The font used on the inactive tabs</summary>
				public Font InactiveFont { get; set; }

				/// <summary>The minimum width that a tab can have</summary>
				public int MinWidth { get; set; }

				/// <summary>The maximum width that a tab can have</summary>
				public int MaxWidth { get; set; }

				/// <summary>The gap size between tabs</summary>
				public int TabSpacing { get; set; }

				/// <summary>The size of icons displayed on the tabs</summary>
				public Size IconSize { get; set; }

				/// <summary>The space between the icon and text tab text</summary>
				public int IconToTextSpacing { get; set; }

				/// <summary>The space between the edge of the tab strip background edge and the tabs</summary>
				public Padding StripPadding { get; set; }

				/// <summary>The space between the tab edge and the tab text,icon,etc</summary>
				public Padding TabPadding { get; set; }
			}

			/// <summary>Colours for drawing title bars and tab strips</summary>
			public class ColourSet
			{
				public ColourSet(Color? text = null, Color? beg = null, Color? end = null, Color? border = null, LinearGradientMode mode = LinearGradientMode.Horizontal, Blend blend = null)
				{
					Text   = text ?? SystemColors.WindowText;
					Beg    = beg ?? SystemColors.Control;
					End    = end ?? SystemColors.Control;
					Border = border ?? Color.Transparent;
					Mode   = mode;
					Blend  = blend ?? new Blend(2)
					{
						Factors = new float[] { 0.0f, 1.0f },
						Positions = new float[] { 0.0f, 1.0f },
					};
				}
				public override string ToString()
				{
					return "{0} -> {1}".Fmt(Beg.Name, End.Name);
				}

				/// <summary>The start colour for the gradient</summary>
				public Color Beg { get; set; }

				/// <summary>The end colour for the gradient</summary>
				public Color End { get; set; }

				/// <summary>The gradient direction</summary>
				public LinearGradientMode Mode { get; set; }

				/// <summary>The border colour</summary>
				public Color Border { get; set; }

				/// <summary>The colour of associated text</summary>
				public Color Text { get; set; }

				/// <summary>The blend mode</summary>
				public Blend Blend { get; set; }
			}
		}

		/// <summary>A custom control for drawing the title bar, including text, close button and pin button</summary>
		public class PaneTitleControl :Control
		{
			public PaneTitleControl(DockPane owner)
			{
				// Double buffer and does not receive input focus
				SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
				SetStyle(ControlStyles.Selectable, false);

				DockPane = owner;
				using (this.SuspendLayout(true))
				{
					ButtonClose = new CaptionButton(Resources.dock_close);
					ButtonAutoHide = new CaptionButton(owner.DockState.IsAutoHide() ? Resources.dock_unpinned : Resources.dock_pinned);
				}
			}
			protected override void Dispose(bool disposing)
			{
				ButtonClose = null;
				ButtonAutoHide = null;
				DockPane = null;
				base.Dispose(disposing);
			}

			/// <summary>The dock pane that hosts this control</summary>
			public DockPane DockPane
			{
				get { return m_impl_dock_pane; }
				set
				{
					if (m_impl_dock_pane == value) return;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.ActivatedChanged -= Invalidate;
					}
					m_impl_dock_pane = value;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.ActivatedChanged += Invalidate;
					}
				}
			}
			private DockPane m_impl_dock_pane;

			/// <summary>The close button</summary>
			public CaptionButton ButtonClose
			{
				get { return m_impl_btn_close; }
				private set
				{
					if (m_impl_btn_close == value) return;
					if (m_impl_btn_close != null)
					{
						m_impl_btn_close.Click -= HandleClose;
						Controls.Remove(m_impl_btn_close);
						Util.Dispose(ref m_impl_btn_close);
					}
					m_impl_btn_close = value;
					if (m_impl_btn_close != null)
					{
						Controls.Add(m_impl_btn_close);
						m_impl_btn_close.Click += HandleClose;
					}
				}
			}
			private CaptionButton m_impl_btn_close;

			/// <summary>The auto hide button</summary>
			public CaptionButton ButtonAutoHide
			{
				get { return m_impl_btn_auto_hide; }
				private set
				{
					if (m_impl_btn_auto_hide == value) return;
					if (m_impl_btn_auto_hide != null)
					{
						m_impl_btn_auto_hide.Click -= HandleAutoHide;
						Controls.Remove(m_impl_btn_auto_hide);
						Util.Dispose(ref m_impl_btn_auto_hide);
					}
					m_impl_btn_auto_hide = value;
					if (m_impl_btn_auto_hide != null)
					{
						Controls.Add(m_impl_btn_auto_hide);
						m_impl_btn_auto_hide.Click += HandleAutoHide;
					}
				}
			}
			private CaptionButton m_impl_btn_auto_hide;

			/// <summary>Measure the height required by this control based on the content in 'DockPane'</summary>
			public int MeasureHeight()
			{
				// No title bar for DockState.Centre
				if (DockPane.DockState == EDockState.Centre)
					return 0;

				var opts = DockPane.DockContainer.Options.TitleBar;
				return opts.TextFont.Height + opts.Padding.Top + opts.Padding.Bottom;
			}

			/// <summary>Returns the size of the pane title control given the available 'display_area'</summary>
			public Rectangle CalcBounds(Rectangle display_area)
			{
				// No title bar for DockState.Centre
				if (DockPane.DockState == EDockState.Centre)
					return Rectangle.Empty;

				// Title bars are always at the top
				var r = new RectangleRef(display_area);
				r.Bottom = r.Top + MeasureHeight();
				return r;
			}

			/// <summary>Perform a hit test on the title bar</summary>
			public EHitItem HitTest(Point pt)
			{
				if (ButtonClose.Visible && ButtonClose.Bounds.Contains(pt)) return EHitItem.CloseBtn;
				if (ButtonAutoHide.Visible && ButtonAutoHide.Bounds.Contains(pt)) return EHitItem.AutoHideBtn;
				return EHitItem.Caption;
			}
			public enum EHitItem { Caption, CloseBtn, AutoHideBtn }

			/// <summary>Paint the control</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				// No area to draw in...
				if (ClientRectangle.Size.IsEmpty)
					return;

				// Draw the caption control
				var gfx = e.Graphics;
				var opts = DockPane.DockContainer.Options.TitleBar;

				// If the owner pane is activated, draw the 'active' title bar
				// Otherwise draw the inactive title bar
				var grad = DockPane.Activated ? opts.ActiveGrad : opts.InactiveGrad;
				using (var brush = new LinearGradientBrush(ClientRectangle, grad.Beg, grad.End, grad.Mode) { Blend=grad.Blend })
					gfx.FillRectangle(brush, e.ClipRectangle);

				// Calculate the area available for the caption text
				var r = ClientRectangle;
				r.X      += opts.Padding.Left;
				r.Y      += opts.Padding.Top;
				r.Width  -= opts.Padding.Left + opts.Padding.Right;
				r.Height -= opts.Padding.Top + opts.Padding.Bottom;
				if (ButtonClose.Visible)
					r.Width -= ButtonClose.Width + opts.Padding.Right;
				if (ButtonAutoHide.Visible)
					r.Width -= ButtonAutoHide.Width + opts.Padding.Right;

				// Text rendering format
				var fmt = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
				if (RightToLeft == RightToLeft.Yes) fmt |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;

				// Draw the caption text
				TextRenderer.DrawText(gfx, DockPane.CaptionText, opts.TextFont, DockContainerUtil.RtlTransform(this, r), grad.Text, fmt);
			}

			/// <summary>Layout the pane title bar</summary>
			protected override void OnLayout(LayoutEventArgs e)
			{
				var opts = DockPane.DockContainer.Options.TitleBar;
				var r = ClientRectangle;
				var x = r.Right - 1;
				var y = r.Y + opts.Padding.Top;
				var h = r.Height - opts.Padding.Bottom - opts.Padding.Top;

				// Position the close button
				if (ButtonClose != null)
				{
					x -= opts.Padding.Right + ButtonClose.Width;
					var w = h * ButtonClose.Image.Width / ButtonClose.Image.Height;
					ButtonClose.Bounds = DockContainerUtil.RtlTransform(this, new Rectangle(x, y, w, h));
				}

				// Position the auto hide button
				if (ButtonAutoHide != null)
				{
					// Update the image based on dock state
					ButtonAutoHide.Image = DockPane.DockState.IsAutoHide() ? Resources.dock_unpinned : Resources.dock_pinned;

					x -= opts.Padding.Right + ButtonAutoHide.Width;
					var w = h * ButtonAutoHide.Image.Width / ButtonAutoHide.Image.Height;
					ButtonAutoHide.Bounds = DockContainerUtil.RtlTransform(this, new Rectangle(x, y, w, h));
				}

				base.OnLayout(e);
			}

			/// <summary>Redo layout when RTL changes</summary>
			protected override void OnRightToLeftChanged(EventArgs e)
			{
				base.OnRightToLeftChanged(e);
				PerformLayout();
			}

			/// <summary>Start dragging the pane on mouse-down over the title</summary>
			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);

				// If left click on the caption, start a drag operation
				if (e.Button == MouseButtons.Left && HitTest(e.Location) == EHitItem.Caption)
				{
					// If user docking is allowed
					var opts = DockPane.DockContainer.Options;
					if (opts.AllowUserDocking)
					{
						// Begin dragging the pane
						DockPane.DockContainer.DragBegin(DockPane);
					}
				}
			}

			/// <summary>Helper overload of invalidate</summary>
			private void Invalidate(object sender, EventArgs e)
			{
				Invalidate();
			}

			/// <summary>Handle the close button being clicked</summary>
			private void HandleClose(object sender, EventArgs e)
			{
				if (DockPane.ActiveContent != null)
					DockPane.Remove(DockPane.ActiveContent);
			}

			/// <summary>Handle the auto hide button being clicked</summary>
			private void HandleAutoHide(object sender, EventArgs e)
			{
				DockPane.DockState = DockContainerUtil.ToggleAutoHide(DockPane.DockState);
				//if (DockHelper.IsDockStateAutoHide(DockPane.DockState))
				//{
				//	DockPane.DockPanel.ActiveAutoHideContent = null;
				//	DockPane.NestedDockingStatus.NestedPanes.SwitchPaneWithFirstChild(DockPane);
				//}
			}
		}

		/// <summary>Base class for a custom control containing a strip of tabs</summary>
		public class TabStripControl :Control
		{
			public TabStripControl(DockPane owner)
			{
				DockPane = owner;

				// Double buffer and does not receive input focus
				SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
				SetStyle(ControlStyles.Selectable, false);

				AllowDrop = true;
			}

			/// <summary>Override the window creation parameters</summary>
			protected override CreateParams CreateParams
			{
				get
				{
					var p = base.CreateParams;
					p.ClassStyle |= Win32.CS_DBLCLKS;
					return p;
				}
			}

			/// <summary>The dock pane that hosts this control</summary>
			public DockPane DockPane
			{
				get { return m_impl_dock_pane; }
				set
				{
					if (m_impl_dock_pane == value) return;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.ActivatedChanged -= Invalidate;
						m_impl_dock_pane.ContentChanged -= HandlePaneContentListChanged;
					}
					m_impl_dock_pane = value;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.ContentChanged += HandlePaneContentListChanged;
						m_impl_dock_pane.ActivatedChanged += Invalidate;
					}
				}
			}
			private DockPane m_impl_dock_pane;

			/// <summary>Measure the height required by this control based on the content in 'DockPane'</summary>
			public int MeasureHeight()
			{
				// No tab strip if there is only one item in the pane
				if (Tabs.Length <= 1)
					return 0;

				var opts = DockPane.DockContainer.Options.TabStrip;
				return Tabs[0].MeasureHeight() + opts.StripPadding.Bottom + opts.StripPadding.Top;
			}

			/// <summary>Returns the size of the tab strip control given the available 'display_area' and strip location</summary>
			public Rectangle CalcBounds(Rectangle display_area)
			{
				// No tab strip if there is only one item in the pane
				if (DockPane.Content.Count <= 1)
					return Rectangle.Empty;

				var h = MeasureHeight();
				var r = new RectangleRef(display_area);
				switch (Dock)
				{
				default: throw new Exception("Tab strip location is not valid");
				case DockStyle.Top: r.Bottom = r.Top    + h; r.Height = h; break;
				case DockStyle.Bottom: r.Top    = r.Bottom - h; r.Height = h; break;
				case DockStyle.Left: r.Right  = r.Left   + h; r.Width  = h; break;
				case DockStyle.Right: r.Left   = r.Right  - h; r.Width  = h; break;
				}
				return r;
			}

			/// <summary>A collection of tab instances for each item in the owning pane</summary>
			public Tab[] Tabs
			{
				get { return m_cache_tabs ?? (m_cache_tabs = DockPane.Content.Select(x => new Tab(this, x)).ToArray()); }
			}
			private Tab[] m_cache_tabs;

			/// <summary>The index of the first tab to display</summary>
			public int FirstTabIndex { get; set; }

			/// <summary>Paint the TabStrip control</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				var gfx = e.Graphics;
				var opts = DockPane.DockContainer.Options.TabStrip;

				// Draw each tab
				var x = opts.TabSpacing;
				foreach (var tab in Tabs.Skip(FirstTabIndex))
				{
					var rect = tab.CalcBounds().Shifted(x,0);
					tab.Paint(gfx, rect);
					gfx.ExcludeClip(rect);
					x += rect.Width + opts.TabSpacing;
					if (x > Width) break;
				}

				// Paint the background
				// If the owner pane is activated, draw the 'active' strip background otherwise draw the inactive one
				var grad = DockPane.Activated ? opts.ActiveStrip : opts.InactiveStrip;
				using (var brush = new LinearGradientBrush(ClientRectangle, grad.Beg, grad.End, grad.Mode) { Blend=grad.Blend })
					gfx.FillRectangle(brush, e.ClipRectangle);
			}

			/// <summary>Helper overload of invalidate</summary>
			private void Invalidate(object sender, EventArgs e)
			{
				Invalidate();
			}

			/// <summary>Handle the content collection in the owning pane changing</summary>
			private void HandlePaneContentListChanged(object sender, EventArgs e)
			{
				// Clear the collection of cached tab instances
				m_cache_tabs = null;
			}
		}

		/// <summary>A custom button drawn on the title bar of a pane</summary>
		public class CaptionButton :Control
		{
			public CaptionButton(Bitmap image)
			{
				SetStyle(ControlStyles.SupportsTransparentBackColor, true);
				BackColor = Color.Transparent;
				Image = image;
			}

			/// <summary>The image to display on the button</summary>
			public Bitmap Image { get; set; }

			/// <summary>Get/Set when the mouse is over the button</summary>
			private bool IsMouseOver
			{
				get { return m_mouse_over; }
				set
				{
					if (m_mouse_over == value) return;
					m_mouse_over = value;
					Invalidate();
				}
			}
			private bool m_mouse_over;

			/// <summary>The default size of the control</summary>
			protected override Size DefaultSize
			{
				get { return Resources.dock_close.Size; }
			}

			/// <summary>Detect mouse over</summary>
			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				IsMouseOver = ClientRectangle.Contains(e.X, e.Y);
			}
			protected override void OnMouseEnter(EventArgs e)
			{
				base.OnMouseEnter(e);
				IsMouseOver = true;
			}
			protected override void OnMouseLeave(EventArgs e)
			{
				base.OnMouseLeave(e);
				IsMouseOver = false;
			}

			/// <summary>Paint the button</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				if (IsMouseOver && Enabled)
				{
					using (var pen = new Pen(ForeColor))
						e.Graphics.DrawRectangle(pen, ClientRectangle.Inflated(-1, -1));// Rectangle.Inflate(ClientRectangle, -1, -1));
				}

				// Paint the button bitmap
				using (var attr = new ImageAttributes())
				{
					attr.SetRemapTable(new[]{
						new ColorMap{OldColor = Color.FromArgb(0, 0, 0), NewColor = ForeColor },
						new ColorMap{OldColor = Image.GetPixel(0, 0), NewColor = Color.Transparent }});

					e.Graphics.DrawImage(Image, new Rectangle(0, 0, Image.Width, Image.Height), 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, attr);
				}

				base.OnPaint(e);
			}
		}

		/// <summary>A splitter control used for resizing panes</summary>
		internal class Splitter :Control
		{
			public const int DefaultWidth = 4;
			private Control m_bar;

			public Splitter(DockPane owner)
			{
				m_bar = new Control { BackColor = SystemColors.ControlDarkDark };
				DockPane = owner;
				BarWidth = DefaultWidth;
				AlignFor(DockPane.DockState);
				SetStyle(ControlStyles.Selectable, false);
			}

			/// <summary>The dock pane that hosts this control</summary>
			public DockPane DockPane
			{
				get { return m_impl_dock_pane; }
				set
				{
					if (m_impl_dock_pane == value) return;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.DockStateChanged -= HandleDockStateChanged;
					}
					m_impl_dock_pane = value;
					if (m_impl_dock_pane != null)
					{
						m_impl_dock_pane.DockStateChanged += HandleDockStateChanged;
					}
				}
			}
			private DockPane m_impl_dock_pane;

			/// <summary>The width of the splitter</summary>
			public int BarWidth { get; set; }

			/// <summary>The orientation of the splitter</summary>
			public Orientation Orientation
			{
				get { return Dock == DockStyle.Left || Dock == DockStyle.Right ? Orientation.Vertical : Orientation.Horizontal; }
			}

			/// <summary>Position this control within 'DockPane' based on 'ds'</summary>
			public void AlignFor(EDockState ds)
			{
				var rc = new RectangleRef(DockPane.ClientRectangle);
				switch (ds)
				{
				default: throw new Exception("Unknown dock state: {0}".Fmt(ds));
				case EDockState.None:
				case EDockState.Centre:
					{
						break;
					}
				case EDockState.Left:
				case EDockState.LeftAutoHide:
					{
						rc.Left = rc.Right - BarWidth;
						rc.Width = BarWidth;
						Dock = DockStyle.Right;
						Cursor = Cursors.VSplit;
						break;
					}
				case EDockState.Right:
				case EDockState.RightAutoHide:
					{
						rc.Right = rc.Left + BarWidth;
						rc.Width = BarWidth;
						Dock = DockStyle.Left;
						Cursor = Cursors.VSplit;
						break;
					}
				case EDockState.Top:
				case EDockState.TopAutoHide:
					{
						rc.Top = rc.Bottom - BarWidth;
						rc.Height = BarWidth;
						Dock = DockStyle.Bottom;
						Cursor = Cursors.HSplit;
						break;
					}
				case EDockState.Bottom:
				case EDockState.BottomAutoHide:
					{
						rc.Bottom = rc.Top + BarWidth;
						rc.Height = BarWidth;
						Dock = DockStyle.Top;
						Cursor = Cursors.HSplit;
						break;
					}
				}
				Bounds = rc;
				Visible = ds != EDockState.Centre && ds != EDockState.None;
			}

			/// <summary>Paint the splitter</summary>
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				var gfx = e.Graphics;
				var rect = ClientRectangle;
				switch (Dock)
				{
				case DockStyle.Left:
				case DockStyle.Right:
					{
						gfx.DrawLine(SystemPens.ControlDarkDark, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom);
						break;
					}
				case DockStyle.Top:
				case DockStyle.Bottom:
					{
						gfx.DrawLine(SystemPens.ControlDark, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
						break;
					}
				}
			}

			/// <summary>Handling dragging</summary>
			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				if (e.Button != MouseButtons.Left) return;
				Capture = true;

				// Position the bar control
				var pt = DockPane.DockContainer.PointToClient(PointToScreen(e.Location));
				var rc = DockPane.DockContainer.RectangleToClient(RectangleToScreen(Bounds));
				m_bar.Location = Orientation == Orientation.Vertical
					? new Point(pt.X - BarWidth/2, rc.Top)
					: new Point(rc.Left, pt.Y - BarWidth/2);
				m_bar.Size = Orientation == Orientation.Vertical
					? new Size(BarWidth, Height)
					: new Size(Width, BarWidth);

				// Add the bar control to the DockContainer as the top level control
				using (DockPane.DockContainer.SuspendLayout(false))
				{
					DockPane.DockContainer.Controls.Add(m_bar);
					DockPane.DockContainer.Controls.SetChildIndex(m_bar, 0);
					DockPane.DockContainer.Invalidate(m_bar.Bounds);
					DockPane.DockContainer.Update();
				}
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				if (Capture)
				{
					var pt = DockPane.DockContainer.PointToClient(PointToScreen(e.Location));
					var rc = DockPane.DockContainer.RectangleToClient(RectangleToScreen(Bounds));

					using (DockPane.DockContainer.SuspendLayout(false))
					{
						DockPane.DockContainer.Invalidate(m_bar.Bounds);
						m_bar.Location = Orientation == Orientation.Vertical
							? new Point(pt.X - BarWidth/2, rc.Top)
							: new Point(rc.Left, pt.Y - BarWidth/2);
						DockPane.DockContainer.Invalidate(m_bar.Bounds);
						DockPane.DockContainer.Update();
					}
				}
			}
			protected override void OnMouseUp(MouseEventArgs e)
			{
				base.OnMouseUp(e);
				Capture = false;

				using (DockPane.DockContainer.SuspendLayout(true))
				{
					DockPane.DockContainer.Controls.Remove(m_bar);
					DockPane.DockContainer.Invalidate(m_bar.Bounds);
					DockPane.DockContainer.Update();

					// Get the mouse position and dock pane location in dock container space
					var pt = DockPane.DockContainer.PointToClient(PointToScreen(e.Location));
					var rc = DockPane.Bounds;
					var dc_bounds = DockPane.DockContainer.Bounds;

					// Change the size of the owning dock pane. Preserve whether the dock area is using fractions or pixels
					var opts = DockPane.DockContainer.Options;
					switch (DockPane.DockState)
					{
					case EDockState.Left:
					case EDockState.LeftAutoHide:
						{
							opts.DockArea.Left = (pt.X - rc.Left + BarWidth/2) / (opts.DockArea.Left >= 1f ? 1f : dc_bounds.Width);
							break;
						}
					case EDockState.Right:
					case EDockState.RightAutoHide:
						{
							opts.DockArea.Right = (rc.Right - pt.X + BarWidth/2) / (opts.DockArea.Right >= 1f ? 1f : dc_bounds.Width);
							break;
						}
					case EDockState.Top:
					case EDockState.TopAutoHide:
						{
							opts.DockArea.Top = (pt.Y - rc.Top + BarWidth/2) / (opts.DockArea.Top >= 1f ? 1f : dc_bounds.Height);
							break;
						}
					case EDockState.Bottom:
					case EDockState.BottomAutoHide:
						{
							opts.DockArea.Bottom = (rc.Bottom - pt.Y + BarWidth/2) / (opts.DockArea.Bottom >= 1f ? 1f : dc_bounds.Height);
							break;
						}
					}
				}
			}

			/// <summary>When the dock state changes, update the orientation and location of the splitter</summary>
			private void HandleDockStateChanged(object sender, DockStateChangedEventArgs e)
			{
				AlignFor(e.StateNew);
			}
		}

		/// <summary>A form that acts as the drop target when a pane or content is being dragged</summary>
		internal class DropHandler :Form
		{
			/// <summary>The dock container that created this drop handler</summary>
			private DockContainer m_owner;

			/// <summary>A form used as graphics to show dragged items</summary>
			private DockableOutline m_ghost;

			public DropHandler(DockContainer owner, object item)
			{
				Debug.Assert(item is DockPane || item is IDockable, "Only panes and content should be being dragged");
				m_owner = owner;

				// Create a form with a null region so that we have an invisible modal dialog
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false;
				Region = new Region(Rectangle.Empty);

				// Create the semi-transparent non-modal form for the dragged item
				m_ghost = new DockableOutline() { Size = new Size(150, 150), Location = MousePosition - new Size(50, 30) };
				m_ghost.Show(this);

				// Create the dock site indicators
				Cross = new Indicator(this, Resources.dock_site_cross);
			}
			protected override void Dispose(bool disposing)
			{
				m_owner.Focus();
				Cross = null;
				Util.Dispose(ref m_ghost);
				base.Dispose(disposing);
			}
			protected override void OnShown(EventArgs e)
			{
				base.OnShown(e);
				Capture = true;
				UpdateIndicators();
			}
			protected override void OnFormClosed(FormClosedEventArgs e)
			{
				base.OnFormClosed(e);
				Capture = false;
			}
			protected override void OnMouseMove(MouseEventArgs e)
			{
				base.OnMouseMove(e);
				HandleMouseMove(this, e);
			}
			protected override void OnMouseUp(MouseEventArgs e)
			{
				base.OnMouseUp(e);
				HandleMouseUp(this, e);
			}

			/// <summary>The cross of dock site locations displayed within the centre of a pane</summary>
			private Indicator Cross
			{
				get { return m_impl_cross; }
				set
				{
					if (m_impl_cross == value) return;
					if (m_impl_cross != null)
					{
						Controls.Remove(m_impl_cross);
						Util.Dispose(ref m_impl_cross);
					}
					m_impl_cross = value;
					if (m_impl_cross != null)
					{
						Controls.Add(m_impl_cross);
					}
				}
			}
			private Indicator m_impl_cross;

			/// <summary>Position the indicators based on what the mouse is hovering over</summary>
			private void UpdateIndicators()
			{
				// Find the pane under the mouse

				// Update the position of the dock site indicators
				Cross.Location = new Point(50,50);
			}

			/// <summary>Update the ghost as the mouse moves and handle snapping to specific dock sites when hovering the indicators</summary>
			private void HandleMouseMove(object sender, MouseEventArgs e)
			{
				// Update the position of the outline form
				m_ghost.Location = MousePosition - new Size(50, 30);
			}

			/// <summary>Handle mouse up as 'drop'</summary>
			private void HandleMouseUp(object sender, MouseEventArgs e)
			{
				Close();
			}

			/// <summary>A control for the docking site indicators</summary>
			private class Indicator :PictureBox
			{
				private DropHandler m_owner;
				public Indicator(DropHandler owner, Bitmap image)
				{
					m_owner = owner;
					SizeMode = PictureBoxSizeMode.AutoSize;
					Image = image;
					//Region = new Region(DockContainerUtil.BitmapToGraphicsPath(image));
				}
				protected override CreateParams CreateParams
				{
					get
					{
						var cp = base.CreateParams;
						cp.ClassStyle |= Win32.CS_DROPSHADOW;
						return cp;
					}
				}
				protected override void OnMouseMove(MouseEventArgs e)
				{
					base.OnMouseMove(e);
					m_owner.HandleMouseMove(this, e);
				}
				protected override void OnMouseUp(MouseEventArgs e)
				{
					base.OnMouseUp(e);
					m_owner.HandleMouseUp(this, e);
				}

				/// <summary>Returns a new region instance for this control in parent space</summary>
				public Region RegionOnParent
				{
					get
					{
						var r = Region.Clone();
						r.Translate(Location.X, Location.Y);
						return r;
					}
				}
			}
		}

		/// <summary>A form that acts as a graphic while a pane or content is being dragged</summary>
		public class DockableOutline :Form
		{
			public DockableOutline()
			{
				// Use a form because the outline needs to be dragged outside the parent window
				StartPosition = FormStartPosition.Manual;
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false;
				Opacity = 0.5f;
				BackColor = SystemColors.ActiveCaption;
				SetStyle(ControlStyles.Selectable, false);
			}
			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					cp.ExStyle |= Win32.WS_EX_NOACTIVATE | Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT; // Transparent and click-through
					return cp;
				}
			}
		}

		/// <summary></summary>
		public class GhostTab :Control
		{
		}

		/// <summary>A tab that displays the name of the content and it's icon</summary>
		public class Tab
		{
			internal Tab(TabStripControl strip, IDockable content)
			{
				Strip = strip;
				Content = content;
			}

			/// <summary>The tab strip that owns this tab</summary>
			public TabStripControl Strip { get; private set; }

			/// <summary>The content that this tab is associated with</summary>
			public IDockable Content { get; private set; }

			/// <summary>Text to display on the tab</summary>
			public string Text
			{
				get { return Content.DockControl.TabText; }
			}

			/// <summary>The icon to display on the tab</summary>
			public Icon Icon
			{
				get { return Content.DockControl.TabIcon; }
			}

			/// <summary>Gets the height of the tab</summary>
			public int MeasureHeight()
			{
				var opts = Strip.DockPane.DockContainer.Options.TabStrip;
				return Maths.Max(opts.InactiveFont.Height, opts.ActiveFont.Height, opts.IconSize.Height) + opts.TabPadding.Bottom + opts.TabPadding.Top;
			}

			/// <summary>Get the text format flags to use when rendering text for this tab</summary>
			private TextFormatFlags FmtFlags
			{
				get { return TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | (Strip.RightToLeft == RightToLeft.Yes ? TextFormatFlags.RightToLeft : 0); }
			}

			/// <summary>Returns the rectangle that would contain this Tab</summary>
			public Rectangle CalcBounds()
			{
				var opts = Strip.DockPane.DockContainer.Options.TabStrip;
				var h = MeasureHeight();
				var sz = TextRenderer.MeasureText(Text, opts.ActiveFont, new Size(opts.MaxWidth, h), FmtFlags);
				return new Rectangle(0, 0, opts.TabPadding.Left + (Icon != null ? Icon.Width + opts.IconToTextSpacing : 0) + sz.Width + opts.TabPadding.Right, h);
			}

			/// <summary>Draw the tab within 'rect'</summary>
			public void Paint(Graphics gfx, Rectangle rect)
			{
				// Fill the background
				var opts = Strip.DockPane.DockContainer.Options.TabStrip;
				var active = Content == Strip.DockPane.DockContainer.ActiveContent;
				var grad = active ? opts.ActiveTab : opts.InactiveTab;
				using (var brush = new LinearGradientBrush(rect, grad.Beg, grad.End, grad.Mode) { Blend=grad.Blend })
					gfx.FillRectangle(brush, rect);
				if (grad.Border != Color.Transparent)
					using (var pen = new Pen(grad.Border))
						gfx.DrawRectangle(pen, rect);

				var x = rect.X + opts.TabPadding.Left;

				// Draw the icon
				if (Icon != null)
				{
					var r = new Rectangle(x, (rect.Height - opts.IconSize.Height)/2, opts.IconSize.Width, opts.IconSize.Height);
					gfx.DrawIcon(Icon, r);
					x += r.Width + opts.IconToTextSpacing;
				}

				// Draw the text
				if (Text.HasValue())
				{
					var font = active ? opts.ActiveFont : opts.InactiveFont;
					var r = new Rectangle(x, (rect.Height - font.Height)/2, rect.Right - x - opts.TabPadding.Right, font.Height);
					TextRenderer.DrawText(gfx, Text, font, r, grad.Text, FmtFlags);
				}
			}
		}
	}

	#endregion

	#region Utility

	/// <summary>Global functions for the dock container</summary>
	public static class DockContainerUtil
	{
		/// <summary>True if this dock state is an auto hide state</summary>
		public static bool IsAutoHide(this EDockState ds)
		{
			return ds == EDockState.LeftAutoHide || ds == EDockState.TopAutoHide || ds == EDockState.RightAutoHide || ds == EDockState.BottomAutoHide;
		}

		/// <summary>Return auto hide off or on, whether is the inverse of the current value</summary>
		public static EDockState ToggleAutoHide(this EDockState ds)
		{
			switch (ds)
			{
			default: return ds;
			case EDockState.Left: return EDockState.LeftAutoHide;
			case EDockState.LeftAutoHide: return EDockState.Left;
			case EDockState.Top: return EDockState.TopAutoHide;
			case EDockState.TopAutoHide: return EDockState.Top;
			case EDockState.Right: return EDockState.RightAutoHide;
			case EDockState.RightAutoHide: return EDockState.Right;
			case EDockState.Bottom: return EDockState.BottomAutoHide;
			case EDockState.BottomAutoHide: return EDockState.Bottom;
			}
		}

		/// <summary>Return a pane belonging to 'dc' at the given point (or null)</summary>
		public static DockPane PaneAtPoint(Point pt, DockContainer dc)
		{
			var ctrl = Control.FromChildHandle(Win32.WindowFromPoint(pt));
			for (; ctrl != null; ctrl = ctrl.Parent)
			{
				var content = ctrl as IDockable;
				if (content != null && content.DockControl.DockContainer == dc)
					return content.DockControl.DockPane;

				var pane = ctrl as DockPane;
				if (pane != null && pane.DockContainer == dc)
					return pane;
			}
			return null;
		}

		/// <summary>Convert 'point' to Right To Left layout if 'control' is in RTL mode</summary>
		internal static Point RtlTransform(Control control, Point point)
		{
			return control.RightToLeft == RightToLeft.Yes ? new Point(control.Right - point.X, point.Y) : point;
		}

		/// <summary>Convert 'rect' to Right To Left layout if 'control' is in RTL mode</summary>
		internal static Rectangle RtlTransform(Control control, Rectangle rect)
		{
			return control.RightToLeft == RightToLeft.Yes ? new Rectangle(control.ClientRectangle.Right - rect.Right, rect.Y, rect.Width, rect.Height) : rect;
		}

		/// <summary>Generate a GraphicsPath from a bitmap</summary>
		internal static GraphicsPath BitmapToGraphicsPath(Bitmap bitmap)
		{
			GraphicsPath gp;
			if (!m_bm_to_gfx_path.TryGetValue(bitmap, out gp))
			{
				// Generates a graphics path by rastering a bitmap into rectangles
				gp = new GraphicsPath();
				var bkcol = bitmap.GetPixel(0, 0);
				for (int y = 0; y != bitmap.Height; ++y)
				{
					// Find the first non-background colour pixel
					int x0; for (x0 = 0; x0 != bitmap.Width && bitmap.GetPixel(x0, y) == bkcol; ++x0) {}

					// Find the last non-background colour pixel
					int x1; for (x1 = bitmap.Width; x1-- != 0 && bitmap.GetPixel(x1, y) == bkcol; ) {}

					// Add a rectangle for the raster line
					gp.AddRectangle(new Rectangle(x0, y, x1-x0+1, 1));
				}
				m_bm_to_gfx_path.Add(bitmap, gp);
			}
			return gp;
		}
		private static Dictionary<Bitmap, GraphicsPath> m_bm_to_gfx_path = new Dictionary<Bitmap, GraphicsPath>();
	}

	#endregion
}
