using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Base class for anything on a chart</summary>
		[DebuggerDisplay("{Name} {Id} {GetType().Name}")]
		public abstract class Element : IDisposable, INotifyPropertyChanged
		{
			/// <summary>Buffers for creating the chart graphics</summary>
			protected List<View3d.Vertex> m_vbuf;
			protected List<View3d.Nugget> m_nbuf;
			protected List<ushort> m_ibuf;

			protected Element(Guid id, m4x4 position, string name)
			{
				View3d = View3d.Create();
				m_vbuf = new List<View3d.Vertex>();
				m_nbuf = new List<View3d.Nugget>();
				m_ibuf = new List<ushort>();

				Id = id;
				Name = name;
				m_chart = null;
				m_impl_position = position;
				m_impl_bounds = new BBox(position.pos, v4.Zero);
				m_impl_selected = false;
				m_impl_hovered = false;
				m_impl_visible = true;
				m_impl_visible_to_find_range = true;
				m_impl_screen_space = false;
				m_impl_enabled = true;
				IsInvalidated = true;
				UserData = new Dictionary<Guid, object>();
			}
			protected Element(XElement node)
				: this(Guid.Empty, m4x4.Identity, string.Empty)
			{
				// Note: Bounds, size, etc are not stored, the implementation
				// of the element provides those (typically in UpdateGfx)
				Id = node.Element(nameof(Id)).As(Id);
				Position = node.Element(nameof(Position)).As(Position);
				Name = node.Element(nameof(Name)).As(Name);
			}
			public virtual void Dispose()
			{
				Chart = null;
				Invalidated = null;
				PositionChanged = null;
				DataChanged = null;
				SelectedChanged = null;
				View3d = null;
			}

			/// <summary>View3d context reference (needed because Elements can out-live the chart)</summary>
			private View3d View3d
			{
				get { return m_view3d; }
				set
				{
					if (m_view3d == value) return;
					Util.Dispose(ref m_view3d);
					m_view3d = value;
				}
			}
			private View3d m_view3d;

			/// <summary>Non-null when the element has been added to a chart. Not virtual, override 'SetChartCore' instead</summary>
			public ChartControl Chart
			{
				[DebuggerStepThrough]
				get { return m_chart; }
				set { SetChartInternal(value, true); }
			}
			private ChartControl m_chart;

			/// <summary>Assign the chart for this element</summary>
			internal void SetChartInternal(ChartControl chart, bool update)
			{
				// An Element can be added to a chart by assigning to the Chart property
				// or by adding it to the Elements collection. It can be removed by setting
				// this property to null, by removing it from the Elements collection, or
				// by Disposing the element. Note: the chart does not own the elements, 
				// elements should only be disposed by the caller.
				if (m_chart == chart) return;

				// Detach from the old chart
				if (m_chart != null && update)
				{
					InvalidateChart();
					m_chart.Elements.Remove(this);
				}

				// Assign to the new chart
				SetChartCore(chart);

				// Attach to the new chart
				if (m_chart != null && update)
				{
					Debug.Assert(!m_chart.Elements.Contains(this), "Element already in the Chart's Elements collection");
					m_chart.Elements.Add(this);
					InvalidateChart();
				}

				Debug.Assert(CheckConsistency());
			}

			/// <summary>Add or remove this element from 'chart'</summary>
			protected virtual void SetChartCore(ChartControl chart)
			{
				// Note: don't suspend events on Chart.Elements.
				// User code maybe watching for ListChanging events.

				// Remove this element from any selected collection on the outgoing chart
				// This also clears the 'selected' state for the element
				Selected = false;

				// Remove any graphics from the previous chart
				if (m_chart != null)
					m_chart.Window.RemoveObjects(new[] { Id }, 1, 0);

				// Set the new chart
				m_chart = chart;
			}

			/// <summary>Unique id for this element</summary>
			public Guid Id { get; private set; }

			/// <summary>Debugging name for the element</summary>
			public string Name { get; set; }

			/// <summary>Export to XML</summary>
			public virtual XElement ToXml(XElement node)
			{
				node.Add2(nameof(Id), Id, false);
				node.Add2(nameof(Position), Position, false);
				node.Add2(nameof(Name), Name, false);
				return node;
			}

			/// <summary>Import from XML. Used to update the state of this element without having to delete/recreate it</summary>
			protected virtual void FromXml(XElement node)
			{
				Position = node.Element(nameof(Position)).As(Position);
				Name = node.Element(nameof(Name)).As(Name);
			}

			/// <summary>Replace the contents of this element with data from 'node'</summary>
			internal void Update(XElement node)
			{
				// Don't validate the TypeAttribute as users may have sub-classed the element
				using (SuspendEvents())
					FromXml(node);

				Invalidate();
			}

			/// <summary>RAII object for suspending events on this element</summary>
			public Scope SuspendEvents()
			{
				return Scope.Create(
					() =>
					{
						return new IDisposable[]
						{
							//Invalidated.Suspend(),
							//DataChanged.Suspend(),
							//SelectedChanged.Suspend(),
							//PositionChanged.Suspend(),
							//SizeChanged.Suspend(),
						};
					},
					arr =>
					{
						Util.DisposeAll(arr);
					});
			}

			/// <summary>Raised whenever a property of this Element changes</summary>
			public event PropertyChangedEventHandler PropertyChanged;
			protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
			{
				PropertyChanged?.Invoke(this, args);
			}
			protected void SetProp<T>(ref T prop, T value, string name, bool invalidate_graphics, bool invalidate_chart)
			{
				if (Equals(prop, value)) return;
				prop = value;
				OnPropertyChanged(new PropertyChangedEventArgs(name));
				if (invalidate_graphics) Invalidate();
				if (invalidate_chart) InvalidateChart();
			}

			/// <summary>Raised whenever the element needs to be redrawn</summary>
			public event EventHandler Invalidated;
			protected virtual void OnInvalidated()
			{
				IsInvalidated = true;
				InvalidateChart();
				Invalidated?.Invoke(this, EventArgs.Empty);
			}

			/// <summary>Raised whenever data associated with the element changes</summary>
			public event EventHandler DataChanged;
			protected virtual void OnDataChanged()
			{
				// Raise data changed on this element, and propagate
				// the event to the containing chart as well.
				DataChanged?.Invoke(this, EventArgs.Empty);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element is moved</summary>
			public event EventHandler PositionChanged;
			protected void OnPositionChanged()
			{
				PositionChanged?.Invoke(this, EventArgs.Empty);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element changes size</summary>
			public event EventHandler SizeChanged;
			protected void OnSizeChanged()
			{
				SizeChanged?.Invoke(this, EventArgs.Empty);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element is selected or deselected</summary>
			public event EventHandler SelectedChanged;
			protected virtual void OnSelectedChanged()
			{
				SelectedChanged?.Invoke(this, EventArgs.Empty);
			}

			/// <summary>Raised whenever the element is hovered over with the mouse</summary>
			public event EventHandler HoveredChanged;
			protected virtual void OnHoveredChanged()
			{
				HoveredChanged?.Invoke(this, EventArgs.Empty);
			}

			/// <summary>Signal that the chart needs laying out</summary>
			protected virtual void RaiseChartChanged()
			{
				if (Chart == null) return;
				Chart.OnChartChanged(new ChartChangedEventArgs(EChangeType.Edited));
			}

			/// <summary>Call 'Invalidate' on the containing chart</summary>
			protected virtual void InvalidateChart()
			{
				if (Chart == null) return;
				Chart.Scene.Invalidate();
			}

			/// <summary>Indicate that the graphics for this element needs to be recreated or modified</summary>
			public void Invalidate(object sender = null, EventArgs args = null)
			{
				if (IsInvalidated) return;
				OnInvalidated();
			}

			/// <summary>True if this element has been invalidated. Call 'UpdateGfx' to clear the invalidated state</summary>
			public bool IsInvalidated { get; private set; }

			/// <summary>Get/Set the selected state</summary>
			public virtual bool Selected
			{
				get { return m_impl_selected; }
				set { SetSelectedInternal(value, true); }
			}
			private bool m_impl_selected;

			/// <summary>Set the selected state of this element</summary>
			internal void SetSelectedInternal(bool selected, bool update_selected_collection)
			{
				// This allows the chart to set the selected state without
				// adding/removing from the chart's 'Selected' collection.
				if (m_impl_selected == selected) return;
				if (m_impl_selected && update_selected_collection)
				{
					Chart?.Selected.Remove(this);
				}

				SetProp(ref m_impl_selected, selected, nameof(Selected), true, true);

				if (m_impl_selected && update_selected_collection)
				{
					// Selection state is changed by assigning to this property or by
					// addition/removal from the chart's 'Selected' collection.
					Chart?.Selected.Add(this);
				}

				// Notify observers about the selection change
				OnSelectedChanged();
				Invalidate();
			}

			/// <summary>Get/Set the hovered state</summary>
			public virtual bool Hovered
			{
				get { return m_impl_hovered; }
				internal set { SetHoveredInternal(value, true); }
			}
			private bool m_impl_hovered;

			/// <summary>Set the selected state of this element</summary>
			internal void SetHoveredInternal(bool hovered, bool update_hovered_collection)
			{
				// This allows the chart to set the hovered state without
				// adding/removing from the chart's 'Hovered' collection.
				if (m_impl_hovered == hovered) return;
				if (m_impl_hovered && update_hovered_collection)
				{
					Chart?.Hovered.Remove(this);
				}

				SetProp(ref m_impl_hovered, hovered, nameof(Hovered), true, true);

				if (m_impl_hovered && update_hovered_collection)
				{
					// Hovered state is changed by assigning to this property or by
					// addition/removal from the chart's 'Hovered' collection.
					Chart?.Hovered.Add(this);
				}

				// Notify observers about the hovered change
				OnHoveredChanged();
				Invalidate();
			}

			/// <summary>Get/Set whether this element is visible in the chart</summary>
			public bool Visible
			{
				get { return m_impl_visible; }
				set { SetProp(ref m_impl_visible, value, nameof(Visible), false, true); } // Make an element visible/invisible, doesn't invalidate it, only the chart that is displaying it.
			}
			private bool m_impl_visible;

			/// <summary>Get/Set whether this element is enabled</summary>
			public bool Enabled
			{
				get { return m_impl_enabled; }
				set { SetProp(ref m_impl_enabled, value, nameof(Enabled), true, true); } // Changing the enabled data of an element should result in the graphics changing in some way that implies 'Enabled/Disabled'
			}
			private bool m_impl_enabled;

			/// <summary>True if this element is included when finding the range of data in the chart</summary>
			public bool VisibleToFindRange
			{
				get { return m_impl_visible_to_find_range; }
				set { SetProp(ref m_impl_visible_to_find_range, value, nameof(VisibleToFindRange), false, true); } // Make an element visible/invisible, doesn't invalidate it, only the chart that is displaying it.
			}
			private bool m_impl_visible_to_find_range;

			/// <summary>True if this is a screen-space element (i.e. Position is in normalised screen space, not chart space)</summary>
			public bool ScreenSpace
			{
				get { return m_impl_screen_space; }
				protected set { SetProp(ref m_impl_screen_space, value, nameof(ScreenSpace), false, true); }
			}
			private bool m_impl_screen_space;

			/// <summary>Allow users to bind arbitrary data to the chart element</summary>
			public IDictionary<Guid, object> UserData
			{
				get;
				private set;
			}

			/// <summary>Send this element to the bottom of the Z-order</summary>
			public void SendToBack()
			{
				// Z order is determined by position in the Elements collection
				if (Chart == null)
					return;

				// Save the chart pointer because removing the element will remove
				// it from the chart causing 'Chart' to become null before Insert is called.
				var chart = Chart;
				using (chart.Elements.SuspendEvents())
				{
					chart.Elements.Remove(this);
					chart.Elements.Insert(0, this);
				}
				chart.UpdateElementZOrder();
				InvalidateChart();
			}

			/// <summary>Bring this element to top of the stack.</summary>
			public void BringToFront()
			{
				// Z order is determined by position in the Elements collection
				if (Chart == null)
					return;

				// Save the chart pointer because removing the element will remove
				// it from the Chart causing 'Chart' to become null before Insert is called
				var chart = Chart;
				using (chart.Elements.SuspendEvents())
				{
					chart.Elements.Remove(this);
					chart.Elements.Add(this);
				}
				chart.UpdateElementZOrder();
				InvalidateChart();
			}

			/// <summary>The element to chart transform</summary>
			public m4x4 Position
			{
				get { return m_impl_position; }
				set
				{
					if (Math_.FEql(m_impl_position, value)) return;
					SetPosition(value);
				}
			}
			private m4x4 m_impl_position;

			/// <summary>Internal set position and raise event</summary>
			protected virtual void SetPosition(m4x4 pos)
			{
				m_impl_position = pos;
				OnPositionChanged();
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(Position)));
			}

			/// <summary>Get/Set the XY position of the element</summary>
			public v2 PositionXY
			{
				get { return Position.pos.xy; }
				set
				{
					var o2p = Position;
					Position = new m4x4(o2p.rot, new v4(value, o2p.pos.z, o2p.pos.w));
				}
			}

			/// <summary>Get/Set the z position of the element</summary>
			public float PositionZ
			{
				get { return Position.pos.z; }
				set
				{
					var o2p = Position;
					o2p.pos.z = value;
					Position = o2p;
				}
			}

			/// <summary>BBox for the element in chart space</summary>
			public virtual BBox Bounds
			{
				get { return m_impl_bounds; }
				protected set { SetProp(ref m_impl_bounds, value, nameof(Bounds), false, true); } // Doesn't invalidate graphics because bounds is usually set after graphics have been created
			}
			private BBox m_impl_bounds;

			/// <summary>Get/Set the centre point of the element (in chart space)</summary>
			public v4 Centre
			{
				get { return Bounds.Centre; }
				set { Position = new m4x4(Position.rot, value + (Position.pos - Centre)); }
			}

			/// <summary>True if this element can be resized</summary>
			public virtual bool Resizeable
			{
				get { return false; }
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in client space because typically hit testing uses pixel tolerances</summary>
			public virtual HitTestResult.Hit HitTest(Point chart_point, Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
			{
				return null;
			}

			/// <summary>Handle a click event on this element</summary>
			public virtual void HandleClicked(ChartClickedEventArgs args)
			{ }

			/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
			public virtual void Drag(Vector delta, bool commit)
			{
				var p = DragStartPosition;
				p.pos.x += (float)delta.X;
				p.pos.y += (float)delta.Y;
				Position = p;
				if (commit)
					DragStartPosition = Position;
			}

			/// <summary>Position recorded at the time dragging starts</summary>
			internal m4x4 DragStartPosition { get; set; }

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			public void UpdateGfx(object sender = null, EventArgs args = null)
			{
				if (m_impl_updating_gfx != 0) return; // Protect against reentrancy
				using (Scope.Create(() => ++m_impl_updating_gfx, () => --m_impl_updating_gfx))
				{
					UpdateGfxCore();
					IsInvalidated = false;
				}
			}
			protected virtual void UpdateGfxCore() { }
			private int m_impl_updating_gfx;

			/// <summary>Add/Remove the graphics associated with this element to the window</summary>
			internal void UpdateScene(View3d.Window window)
			{
				if (IsInvalidated) UpdateGfx();
				UpdateSceneCore(window);
			}
			protected virtual void UpdateSceneCore(View3d.Window window)
			{
				// Add or remove associated graphics
				// if (Visible)
				// 	window.AddObject();
				// else
				// 	window.RemoveObject();
			}

			/// <summary>Check the self consistency of this element</summary>
			public virtual bool CheckConsistency()
			{
				return true;
			}
		}
	}
}
