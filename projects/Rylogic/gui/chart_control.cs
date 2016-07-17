using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.util;
using pr.gfx;
using pr.maths;
using pr.extn;
using pr.container;
using System.Xml.Linq;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using pr.common;

namespace pr.gui
{
	/// <summary>A view 3d based chart control</summary>
	public class ChartControl :UserControl
	{
		#region Constants
		public static readonly Guid GridId = new Guid("1783F2BF-2782-4C39-98AF-C5935B1003E9");
		#endregion

		/// <summary>The types of elements that can be on a chart</summary>
		public enum Entity
		{
			/// <summary>User created chart elements</summary>
			User,

			/// <summary>Chart elements created by the chart control</summary>
			ChartInternal,
		}

		#region Elements

		/// <summary>Base class for anything on a chart</summary>
		public abstract class Element :IDisposable
		{
			protected Element(Entity entity, Guid id, m4x4 position)
			{
				Entity          = entity;
				Id              = id;
				m_impl_chart     = null;
				m_impl_position = position;
				m_impl_selected = false;
				m_impl_visible  = true;
				m_impl_enabled  = true;
				UserData        = new Dictionary<Guid, object>();
			}
			protected Element(Entity entity, XElement node)
			{
				Chart     = null;
				Entity      = entity;
				Id          = node.Element(XmlTag.Id).As<Guid>();
				Position    = node.Element(XmlTag.Position).As<m4x4>();
				Selected    = false;
				Visible     = true;
				Enabled     = true;
				UserData    = new Dictionary<Guid, object>();
			}
			public void Dispose()
			{
				Dispose(true); // Mimic the WinForms Control disposing pattern
			}
			protected bool Disposed { get; private set; }
			protected virtual void Dispose(bool disposing)
			{
				Util.BreakIf(!Disposed && Util.IsGCFinalizerThread);
				Chart = null;
				Invalidated = null;
				PositionChanged = null;
				DataChanged = null;
				SelectedChanged = null;
				Disposed = true;
			}

			/// <summary>Get the entity type for this element</summary>
			public Entity Entity { get; private set; }

			/// <summary>Non-null when the element has been added to a chart. Not virtual, override 'SetChartInternal' instead</summary>
			public ChartControl Chart
			{
				get { return m_impl_chart; }
				set { SetChartInternal(value, true); }
			}
			private ChartControl m_impl_chart;

			/// <summary>Assign the chart for this element</summary>
			internal void SetChartInternal(ChartControl chart, bool update)
			{
				// An Element can be added to a chart by assigning to the Chart property
				// or by adding it to the Elements collection. It can be removed by setting
				// this property to null, by removing it from the Elements collection, or
				// by Disposing the element. Note: the chart does not own the elements, 
				// elements should only be disposed by the caller.
				if (m_impl_chart == chart) return;

				// Detach from the old chart
				if (m_impl_chart != null && update)
				{
					m_impl_chart.Elements.Remove(this);
				}

				// Assign to the new chart
				SetChartCore(chart);

				// Attach to the new chart
				if (m_impl_chart != null && update)
				{
					m_impl_chart.Elements.Add(this);
				}

				Debug.Assert(CheckConsistency());
				Invalidate();
			}

			/// <summary>Add or remove this element from 'chart'</summary>
			public virtual void SetChartCore(ChartControl chart)
			{
				// Note: don't suspend events on Chart.Elements.
				// User code maybe watching for ListChanging events.
				Debug.Assert(!Disposed);

				// Remove this element from any selected collection on the outgoing chart
				// This also clears the 'selected' state for the element
				Selected = false;

				// Ensure this element isn't in the dirty list on the out-going chart
				m_impl_chart?.m_dirty.Remove(this);

				// Set the new chart
				m_impl_chart = chart;
			}

			/// <summary>Unique id for this element</summary>
			public Guid Id { get; private set; }

			/// <summary>Export to XML</summary>
			public virtual XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.Id       ,Id       ,false);
				node.Add2(XmlTag.Position ,Position ,false);
				return node;
			}

			/// <summary>Import from XML. Used to update the state of this element without having to delete/recreate it</summary>
			protected virtual void FromXml(XElement node)
			{
				Position = node.Element(XmlTag.Position).As(Position);
			}

			/// <summary>Replace the contents of this element with data from 'node'</summary>
			internal void Update(XElement node)
			{
				// Don't validate the TypeAttribute as users may have sub-classed the element
				using (SuspendEvents())
					FromXml(node);

				Invalidate();
			}

			/// <summary>Indicate a render is needed. Note: doesn't call Render</summary>
			public void Invalidate(object sender = null, EventArgs args = null)
			{
				if (Chart == null || Chart.m_dirty.Contains(this)) return;
				Chart.m_dirty.Add(this);
				Invalidated.Raise(this);
			}

			/// <summary>RAII object for suspending events on this element</summary>
			public Scope SuspendEvents()
			{
				return Scope.Create(
					() =>
					{
						return new[]
						{
							Invalidated.Suspend(),
							DataChanged.Suspend(),
							SelectedChanged.Suspend(),
							PositionChanged.Suspend(),
							SizeChanged.Suspend(),
						};
					},
					arr =>
					{
						Util.DisposeAll(arr);
					});
			}

			/// <summary>Raised whenever the element needs to be redrawn</summary>
			public event EventHandler Invalidated;

			/// <summary>Raised whenever data associated with the element changes</summary>
			public event EventHandler DataChanged;
			protected virtual void OnDataChanged()
			{
				DataChanged.Raise(this, EventArgs.Empty);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element is moved</summary>
			public event EventHandler PositionChanged;
			protected void OnPositionChanged()
			{
				PositionChanged.Raise(this);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element changes size</summary>
			public event EventHandler SizeChanged;
			protected void OnSizeChanged()
			{
				SizeChanged.Raise(this, EventArgs.Empty);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element is selected or deselected</summary>
			public event EventHandler SelectedChanged;
			protected virtual void OnSelectedChanged()
			{
				SelectedChanged.Raise(this);
			}

			/// <summary>Signal that the chart needs laying out</summary>
			protected virtual void RaiseChartChanged()
			{
				if (Chart == null) return;
				Chart.RaiseChartChanged(new ChartChangedEventArgs(EChangeType.Edited));
			}

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

				m_impl_selected = selected;

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

			/// <summary>Get/Set whether this element is visible in the chart</summary>
			public bool Visible
			{
				get { return m_impl_visible; }
				set
				{
					if (m_impl_visible == value) return;
					m_impl_visible = value;
					Invalidate();
				}
			}
			private bool m_impl_visible;

			/// <summary>Get/Set whether this element is enabled</summary>
			public bool Enabled
			{
				get { return m_impl_enabled; }
				set
				{
					if (m_impl_enabled == value) return;
					m_impl_enabled = value;
					Invalidate();
				}
			}
			private bool m_impl_enabled;

			/// <summary>Allow users to bind arbitrary data to the chart element</summary>
			public IDictionary<Guid, object> UserData { get; private set; }

			/// <summary>Send this element to the bottom of the stack.</summary>
			public void SendToBack()
			{
				// Z order is determined by position in the Elements collection
				if (Chart == null)
					return;

				// Save the chart pointer because removing the element will remove
				// it from the chart causing 'Chart' to become null before Insert is called
				var chart = Chart;
				using (chart.Elements.SuspendEvents())
				{
					chart.Elements.Remove(this);
					chart.Elements.Insert(0, this);
				}
				chart.UpdateElementZOrder();
				chart.Invalidate();
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
				chart.Invalidate();
			}

			/// <summary>The element to chart transform</summary>
			public m4x4 Position
			{
				get { return m_impl_position; }
				set
				{
					if (m4x4.FEql(m_impl_position, value)) return;
					SetPosition(value);
				}
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

			/// <summary>Position recorded at the time dragging starts</summary>
			internal m4x4 DragStartPosition { get; set; }

			/// <summary>Internal set position and raise event</summary>
			protected virtual void SetPosition(m4x4 pos)
			{
				m_impl_position = pos;
				OnPositionChanged();
			}
			private m4x4 m_impl_position;

			/// <summary>Get/Set the centre point of the element (in chart space)</summary>
			public v2 Centre
			{
				get { return Bounds.Centre; }
				set { PositionXY = value + (PositionXY - Centre); }
			}

			/// <summary>AABB for the element in chart space</summary>
			public virtual BRect Bounds
			{
				get { return new BRect(PositionXY, v2.Zero); }
			}

			/// <summary>True if this element can be resized</summary>
			public virtual bool Resizeable
			{
				get { return false; }
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in chart space</summary>
			public virtual HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam)
			{
				return null;
			}

			/// <summary>Handle a click event on this element</summary>
			internal virtual bool HandleClicked(HitTestResult.Hit hit, View3d.CameraControls cam)
			{
				return false;
			}

			/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
			public virtual void Drag(v2 delta, bool commit)
			{
				var p = DragStartPosition;
				p.pos.x += delta.x;
				p.pos.y += delta.y;
				Position = p;
				if (commit)
					DragStartPosition = Position;
			}

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			public void UpdateGfx()
			{
				if (m_impl_updating_gfx != 0) return; // Protect against reentrancy
				using (Scope.Create(() => ++m_impl_updating_gfx, () => --m_impl_updating_gfx))
					UpdateGfxCore();
			}
			protected virtual void UpdateGfxCore() {}
			private int m_impl_updating_gfx;

			/// <summary>Add the graphics associated with this element to the window</summary>
			internal void AddToScene(View3d.Window window)
			{
				AddToSceneCore(window);
			}
			protected virtual void AddToSceneCore(View3d.Window window)
			{
			}

			/// <summary>Check the self consistency of this element</summary>
			public virtual bool CheckConsistency()
			{
				return true;
			}
		}

		#region Grid
		public class Grid :Element
		{
			public Grid() :base(Entity.ChartInternal, GridId, m4x4.Identity)
			{ }
		}
		#endregion

		#endregion

		private Control                m_panel;  // The panel containing the view3d part of the chart
		private View3d                 m_view3d; // Renderer
		private View3d.Window          m_window; // A view3d window for this control instance
		private View3d.CameraControls  m_camera; // The virtual window over the diagram
		private readonly DirtyElements m_dirty;  // Elements that need refreshing

		public ChartControl()
			:this(new RdrOptions())
		{ }
		public ChartControl(RdrOptions options)
		{
			try
			{
				if (this.IsInDesignMode())
					return;

				Options = options;

				using (this.SuspendLayout(false))
				{
					InitializeComponent();

					// Set up a panel to host the view3d window
					m_panel = Controls.Add2(new Control { Name = "chart" });
				}

				m_view3d = new View3d();
				m_window = new View3d.Window(m_view3d, m_panel.Handle, new View3d.WindowOptions(false, null, IntPtr.Zero) { DbgName = "Chart" });
				m_window.LightProperties = View3d.Light.Directional(-v4.ZAxis, Colour32.Zero, Colour32.Gray, Colour32.Zero, 0f, 0f);
				m_window.FocusPointVisible = false;
				m_window.OriginVisible = false;
				m_window.Orthographic = false;
				m_camera = m_window.Camera;
				m_camera.SetClipPlanes(0.5f, 1.1f, true);
				m_dirty = new DirtyElements(this);

				Elements = new BindingListEx<Element> { PerItemClear = true, UseHashSet = true };
				Selected = new BindingListEx<Element> { PerItemClear = false };
				ElemIds  = new Dictionary<Guid, Element>();
				Range = new RangeData(this);

				m_panel.Resize += (s,a) => m_window.RenderTargetSize = m_panel.ClientSize;
				m_panel.Invalidated += (s,a) => m_window.Invalidate();
				m_panel.Paint += (s,a) => m_window.Present();
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		protected override void Dispose(bool disposing)
		{
			Range = null;
			Util.Dispose(ref m_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnLayout(LayoutEventArgs e)
		{
			if (Range == null) return;
			var dims = ChartDimensions;
			m_panel.Bounds = dims.ChartArea;
			base.OnLayout(e);
		}
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			// Swallow
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			DoPaint(e.Graphics, ClientRectangle);
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Invalidate(true);
		}

		/// <summary>Rendering options for the chart</summary>
		public RdrOptions Options { get; private set; }
		public class RdrOptions
		{
			public RdrOptions()
			{
				BkColour        = SystemColors.Control;
				ChartBkColour   = Color.White;
				TitleColour     = Color.Black;
				TitleFont       = new Font("tahoma", 12, FontStyle.Bold);
				TitleTransform  = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
				Margin          = new Padding(3);
				NoteFont        = new Font("tahoma",  8, FontStyle.Regular);
				SelectionColour = Color.DarkGray;
			}

			/// <summary>The fill colour of the background of the graph</summary>
			public Color BkColour { get; set; }

			/// <summary>The fill colour of the chart background</summary>
			public Color ChartBkColour { get; set; }

			/// <summary>The colour of the title text</summary>
			public Color TitleColour { get; set; }

			/// <summary>Transform for position the graph title, offset from top centre</summary>
			public Matrix TitleTransform { get; set; }

			/// <summary>The distances from the edge of the control to the chart area</summary>
			public Padding Margin { get; set; }

			/// <summary>Font to use for the title text</summary>
			public Font TitleFont { get; set; }

			/// <summary>Font to use for graph notes</summary>
			public Font NoteFont { get; set; }

			/// <summary>Area selection colour</summary>
			public Color SelectionColour { get; set; }
		}

		/// <summary>The title of the chart</summary>
		public string Title
		{
			get { return m_title ?? string.Empty; }
			set
			{
				if (m_title == value) return;
				m_title = value;
				Invalidate();
			}
		}
		private string m_title;

		/// <summary>The X/Y axis range of the chart</summary>
		public RangeData Range
		{
			get { return m_range; }
			private set
			{
				if (value == m_range) return;
				Util.Dispose(ref m_range);
				m_range = value;
			}
		}
		private RangeData m_range;

		/// <summary>Accessor to the current X axis</summary>
		public RangeData.Axis XAxis
		{
			get { return Range.XAxis; }
		}

		/// <summary>Accessor to the current Y axis</summary>
		public RangeData.Axis YAxis
		{
			get { return Range.YAxis; }
		}

		/// <summary>All chart objects</summary>
		public BindingListEx<Element> Elements { get; private set; }

		/// <summary>The set of selected chart elements</summary>
		public BindingListEx<Element> Selected { get; private set; }

		/// <summary>Associative array from Element Ids to Elements</summary>
		public Dictionary<Guid, Element> ElemIds { get; private set; }

		/// <summary>Raised whenever elements in the chart have been edited or moved</summary>
		public event EventHandler<ChartChangedEventArgs> ChartChaged;
		protected virtual void OnDiagramChanged(ChartChangedEventArgs args)
		{
			ChartChaged.Raise(this, args);
		}
		private ChartChangedEventArgs RaiseChartChanged(ChartChangedEventArgs args)
		{
			if (!ChartChaged.IsSuspended())
				OnDiagramChanged(args);

			return args;
		}
		public Scope SuspendChartChanged(bool raise_on_resume = true)
		{
			return ChartChaged.Suspend(raise_if_signalled:raise_on_resume, sender:this, args:new ChartChangedEventArgs(EChangeType.Edited));
		}

		/// <summary>Paint the control</summary>
		private void DoPaint(Graphics gfx, Rectangle area)
		{
			try
			{
				// Get the areas to draw in
				var dims = ChartDimensions;
				var show_chart = m_window != null && !this.IsInDesignMode();

				// Construct the view3d scene
				if (show_chart)
				{
					// Add all objects
					m_window.RemoveAllObjects();
					foreach (var elem in Elements)
					{
						if (!elem.Visible) continue;
						elem.AddToScene(m_window);
					}

					// Add axis graphics
					XAxis.AddToScene(m_window);
					YAxis.AddToScene(m_window);

					// Start the render
					m_window.BackgroundColour = Options.ChartBkColour;
					m_window.Render();
				}

				// Draw the chart frame
				DoPaintFrame(gfx, dims);

				// Render the diagram
				if (show_chart)
					m_window.Present();

				// Add user graphics over the chart
			}
			catch (OverflowException)
			{
				// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
				using (var bsh = new SolidBrush(Color.FromArgb(0x80, Color.Black)))
					gfx.DrawString("Rendering error within .NET", Options.TitleFont, bsh, PointF.Empty);
			}
		}

		/// <summary>Draw the titles, axis labels, ticks, etc around the chart</summary>
		private void DoPaintFrame(Graphics gfx, ChartDims dims)
		{
			// This is not enforced in the axis.Min/Max accessors because it's useful
			// to be able to change the min/max independently of each other, set them
			// to float max etc. It's only invalid to render a graph with a negative range
			Debug.Assert(XAxis.Span > 0, "Negative x range");
			Debug.Assert(YAxis.Span > 0, "Negative y range");

			// Clear to the background colour
			gfx.Clear(Options.BkColour);

			// Draw the graph title and labels
			if (Title.HasValue())
			{
				using (var bsh = new SolidBrush(Options.TitleColour))
				{
					var r = gfx.MeasureString(Title, Options.TitleFont);
					var x = (dims.Area.Width - r.Width) * 0.5f;
					var y = (dims.Area.Top + Options.Margin.Top) * 1f;
					gfx.TranslateTransform(x, y);
					gfx.MultiplyTransform(Options.TitleTransform);
					gfx.DrawString(Title, Options.TitleFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}
			if (XAxis.Label.HasValue())
			{
				using (var bsh = new SolidBrush(XAxis.Options.LabelColour))
				{
					var r = gfx.MeasureString(XAxis.Label, XAxis.Options.LabelFont);
					var x = (dims.Area.Width - r.Width) * 0.5f;
					var y = (dims.Area.Bottom - Options.Margin.Bottom - r.Height) * 1f;
					gfx.TranslateTransform(x, y);
					gfx.MultiplyTransform(XAxis.Options.LabelTransform);
					gfx.DrawString(XAxis.Label, XAxis.Options.LabelFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}
			if (YAxis.Label.HasValue())
			{
				using (var bsh = new SolidBrush(YAxis.Options.LabelColour))
				{
					var r = gfx.MeasureString(YAxis.Label, YAxis.Options.LabelFont);
					var x = (dims.Area.Left + Options.Margin.Left) * 1f;
					var y = (dims.Area.Height + r.Width) * 0.5f;
					gfx.TranslateTransform(x, y);
					gfx.RotateTransform(-90.0f);
					gfx.MultiplyTransform(YAxis.Options.LabelTransform);
					gfx.DrawString(YAxis.Label, YAxis.Options.LabelFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}

			// Tick marks and labels
			var lblx = (float)(dims.ChartArea.Left - YAxis.Options.TickLength - 1);
			var lbly = (float)(dims.ChartArea.Top + dims.ChartArea.Height + XAxis.Options.TickLength + 1);
			if (XAxis.Options.DrawTickLabels || XAxis.Options.DrawTickMarks)
			{
				using (var pen = new Pen(XAxis.Options.TickColour))
				using (var bsh = new SolidBrush(XAxis.Options.TickColour))
				{
					double min, max, step;
					XAxis.GridLines(out min, out max, out step);
					for (var x = min; x < max; x += step)
					{
						var X = (int)(dims.ChartArea.Left + x * dims.ChartArea.Width / XAxis.Span);
						var s = XAxis.TickText(x + XAxis.Min);
						var r = gfx.MeasureString(s, XAxis.Options.TickFont);
						if (XAxis.Options.DrawTickLabels)
							gfx.DrawString(s, XAxis.Options.TickFont, bsh, new PointF(X - r.Width*0.5f, lbly));
						if (XAxis.Options.DrawTickMarks)
							gfx.DrawLine(pen, X, dims.ChartArea.Top + dims.ChartArea.Height, X, dims.ChartArea.Top + dims.ChartArea.Height + XAxis.Options.TickLength);
					}
				}
			}
			if (YAxis.Options.DrawTickLabels || YAxis.Options.DrawTickMarks)
			{
				using (var pen = new Pen(YAxis.Options.TickColour))
				using (var bsh = new SolidBrush(YAxis.Options.TickColour))
				{
					double min, max, step;
					YAxis.GridLines(out min, out max, out step);
					for (var y = min; y < max; y += step)
					{
						var Y = (int)(dims.ChartArea.Top + dims.ChartArea.Height - y * dims.ChartArea.Height / YAxis.Span);
						var s = YAxis.TickText(y + YAxis.Min);
						var r = gfx.MeasureString(s, YAxis.Options.TickFont);
						if (YAxis.Options.DrawTickLabels)
							gfx.DrawString(s, YAxis.Options.TickFont, bsh, new PointF(lblx - r.Width, Y - r.Height*0.5f));
						if (YAxis.Options.DrawTickMarks)
							gfx.DrawLine(pen, dims.ChartArea.Left - YAxis.Options.TickLength, Y, dims.ChartArea.Left, Y);
					}
				}
			}

			// Axes
			using (var pen = new Pen(XAxis.Options.AxisColour, XAxis.Options.AxisThickness))
			{
				var y = dims.ChartArea.Bottom;
				gfx.DrawLine(pen, new Point(dims.ChartArea.Left, y), new Point(dims.ChartArea.Right, y));
			}
			using (var pen = new Pen(YAxis.Options.AxisColour, YAxis.Options.AxisThickness))
			{
				var x = (int)(dims.ChartArea.Left - 1 - XAxis.Options.AxisThickness*0.5f);
				gfx.DrawLine(pen, new Point(x, dims.ChartArea.Top), new Point(x, dims.ChartArea.Bottom));
			}
		}

		/// <summary>The Z value of the highest element in the diagram</summary>
		private float HighestZ { get; set; }

		/// <summary>The Z value of the lowest element in the diagram</summary>
		private float LowestZ { get; set; }

		/// <summary>Set the Z value for all elements.</summary>
		private void UpdateElementZOrder()
		{
			LowestZ = 0f;
			HighestZ = 0f;

			foreach (var elem in Elements.Where(x => x.Entity == Entity.User))
				elem.PositionZ = HighestZ += 0.001f;

			foreach (var elem in Elements.Where(x => x.Entity == Entity.ChartInternal))
				elem.PositionZ = HighestZ += 0.001f;
		}

		/// <summary>Return the size of the chart in the direction of 'axis'</summary>
		private ChartDims ChartDimensions
		{
			get { return new ChartDims(this); }
		}
		private struct ChartDims
		{
			public ChartDims(ChartControl chart)
			{
				Chart = chart;
				using (var gfx = chart.CreateGraphics())
				{
					RectangleF rect = chart.ClientRectangle;
					var r = SizeF.Empty;

					Area = rect.ToRect();

					// Add margins
					rect.X      += chart.Options.Margin.Left;
					rect.Y      += chart.Options.Margin.Top;
					rect.Width  -= chart.Options.Margin.Left + chart.Options.Margin.Right;
					rect.Height -= chart.Options.Margin.Top + chart.Options.Margin.Bottom;

					// Add space for tick marks
					if (chart.YAxis.Options.DrawTickMarks)
					{
						rect.X      += chart.YAxis.Options.TickLength;
						rect.Width  -= chart.YAxis.Options.TickLength;
					}
					if (chart.XAxis.Options.DrawTickMarks)
					{
						rect.Height -= chart.XAxis.Options.TickLength;
					}

					// Add space for the title and axis labels
					if (chart.Title.HasValue())
					{
						r = gfx.MeasureString(chart.Title, chart.Options.TitleFont);
						rect.Y      += r.Height;
						rect.Height -= r.Height;
					}
					if (chart.XAxis.Label.HasValue())
					{
						r = gfx.MeasureString(chart.XAxis.Label, chart.XAxis.Options.LabelFont);
						rect.Height -= r.Height;
					}
					if (chart.YAxis.Label.HasValue())
					{
						r = gfx.MeasureString(chart.Range.YAxis.Label, chart.YAxis.Options.LabelFont);
						rect.X     += r.Height; // will be rotated by 90deg
						rect.Width -= r.Height;
					}

					// Add space for the tick labels
					const string lbl = "9.999";
					if (chart.XAxis.Options.DrawTickLabels)
					{
						r = gfx.MeasureString(lbl, chart.XAxis.Options.TickFont);
						rect.Height -= r.Height;
					}
					if (chart.YAxis.Options.DrawTickLabels)
					{
						r = gfx.MeasureString(lbl, chart.YAxis.Options.TickFont);
						rect.X     += r.Width;
						rect.Width -= r.Width;
					}

					ChartArea = rect.ToRect();
				}
			}

			/// <summary>The chart that these dimensions were calculated from</summary>
			public ChartControl Chart { get; private set; }

			/// <summary>The size of the control</summary>
			public Rectangle Area { get; private set; }

			/// <summary>The area of the view3d part of the chart</summary>
			public Rectangle ChartArea { get; private set; }
		}

		#region Range

		/// <summary>The 2D size of the chart</summary>
		public class RangeData :IDisposable
		{
			public RangeData(ChartControl owner)
			{
				XAxis = new Axis(owner);
				YAxis = new Axis(owner);
			}
			public RangeData(RangeData rhs)
			{
				XAxis = new Axis(rhs.XAxis);
				YAxis = new Axis(rhs.YAxis);
			}
			public virtual void Dispose()
			{
				XAxis = null;
				YAxis = null;
			}

			/// <summary>The chart X axis</summary>
			public Axis XAxis
			{
				get { return m_xaxis; }
				private set
				{
					if (value == m_xaxis) return;
					Util.Dispose(ref m_xaxis);
					m_xaxis = value;
				}
			}
			private Axis m_xaxis;

			/// <summary>The chart Y axis</summary>
			public Axis YAxis
			{
				get { return m_yaxis; }
				private set
				{
					if (value == m_yaxis) return;
					Util.Dispose(ref m_yaxis);
					m_yaxis = value;
				}
			}
			private Axis m_yaxis;

			/// <summary>A axis on the chart (typically X or Y)</summary>
			public class Axis :IDisposable
			{
				public Axis(ChartControl owner)
					:this(owner, 0f, 1f)
				{}
				public Axis(ChartControl owner, double min, double max)
					:this(owner, min, max, new RdrOptions())
				{}
				public Axis(ChartControl owner, double min, double max, RdrOptions options)
				{
					Debug.Assert(owner != null);
					Options       = options;
					Owner         = owner;
					Label         = string.Empty;
					Min           = min;
					Max           = max;
					AllowScroll   = true;
					AllowZoom     = true;
					LockRange     = false;
					TickText      = x => Math.Round(x, 4, MidpointRounding.AwayFromZero).ToString("G4");
					ShowGridLines = false;
				}
				public Axis(Axis rhs)
				{
					Owner         = rhs.Owner;
					Label         = rhs.Label;
					Min           = rhs.Min;
					Max           = rhs.Max;
					AllowScroll   = rhs.AllowScroll;
					AllowZoom     = rhs.AllowZoom;
					LockRange     = rhs.LockRange;
					TickText      = rhs.TickText;
					ShowGridLines = rhs.ShowGridLines;
				}
				public virtual void Dispose()
				{
					ShowGridLines = false;
				}

				/// <summary>Render options for the axis</summary>
				public RdrOptions Options { get; private set; }
				public class RdrOptions
				{
					public RdrOptions()
					{
						AxisColour     = Color.Black;
						LabelColour    = Color.Black;
						GridColour     = Color.Gray;
						TickColour     = Color.Black;
						LabelFont      = new Font("tahoma", 10, FontStyle.Regular);
						TickFont       = new Font("tahoma", 8, FontStyle.Regular);
						DrawTickMarks  = true;
						DrawTickLabels = true;
						LabelTransform = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
						TickLength     = 5;
						AxisThickness  = 1f;
						PixelsPerTick  = 30.0;
					}

					/// <summary>The colour of the main axes</summary>
					public Color AxisColour { get; set; }

					/// <summary>The colour of the label text</summary>
					public Color LabelColour { get; set; }

					/// <summary>The colour of the grid lines</summary>
					public Color GridColour { get; set; }

					/// <summary>The colour of the tick text</summary>
					public Color TickColour { get; set; }

					/// <summary>The font to use for the axis label</summary>
					public Font LabelFont { get; set; }

					/// <summary>The font to use for tick labels</summary>
					public Font TickFont { get; set; }

					/// <summary>True if tick marks should be drawn</summary>
					public bool DrawTickMarks { get; set; }

					/// <summary>True if tick labels should be drawn</summary>
					public bool DrawTickLabels { get; set; }

					/// <summary>Offset transform from default label position</summary>
					public Matrix LabelTransform { get; set; }

					/// <summary>The length of the tick marks</summary>
					public int TickLength { get; set; }

					/// <summary>The thickness of the axis line</summary>
					public float AxisThickness { get; set; }

					/// <summary>The preferred number of pixels between each grid line</summary>
					public double PixelsPerTick { get; set; }
				}

				/// <summary>The chart that owns this axis</summary>
				public ChartControl Owner { get; private set; }

				/// <summary>The axis label</summary>
				public string Label
				{
					get { return m_label ?? string.Empty; }
					set
					{
						if (m_label == value) return;
						m_label = value;
					}
				}
				private string m_label;

				/// <summary>Show grid lines on the chart for this axis</summary>
				public bool ShowGridLines
				{
					get { return m_gridlines != null; }
					set
					{
						if (ShowGridLines == value) return;
						if (!value)
						{
							Util.Dispose(ref m_gridlines);
						}
						if (value)
						{
							//// Create a model for the grid lines
							//double min, max, step;
							//GridLines(out min, out max, out step);
							//var num_lines = (int)(1 + (max - min) / step);

							//var verts = new View3d.Vertex[num_lines * 2];
							//var indices = new ushort[num_lines * 2];




							//using (var pin_v = new PinnedObject<View3d.Vertex[]>(verts))
							//using (var pin_i = new PinnedObject<ushort[]>(indices))
							//{

							//	//double min, max, step;
							//	//GridLines(out min, out max, out step);
							//	//var num_lines = (int)(1 + (max - min) / step);

							//	//var is_x = Owner.XAxis == this;
							//	//var dims = Owner.ChartDimensions;
							//	//var v0 = new v4(is_x ? min : dims.C


							//	//var col = (uint)Options.GridColour.ToArgb();
							//	//for (int n = 0, v = 0, i = 0; n != num_lines; ++n)
							//	//{
							//	//	verts[v++] = new View3d.Vertex(new v4(), col);
							//	//	verts[v++] = new View3d.Vertex(new v4(), col);
							//	//	indices[i] = (ushort)i++;
							//	//}


							//	m_gridlines = new View3d.Object(
							//		"Axis - {0}".Fmt(Label),
							//		(uint)Options.GridColour.ToArgb(),
							//		num_lines * 2,
							//		num_lines * 2,
							//		pin_v.Pointer,
							//		pin_i.Pointer,
							//		View3d.EPrim.D3D_PRIMITIVE_TOPOLOGY_LINELIST,
							//		View3d.EGeom.Vert | View3d.EGeom.Colr);
							//}
						}
					}
				}
				private View3d.Object m_gridlines;

				/// <summary>The minimum axis value</summary>
				public double Min
				{
					get { return m_min; }
					set
					{
						if (m_min == value) return;
						Debug.Assert(value < Max, "Range must be positive and non-zero");
						m_min = value;
					}
				}
				private double m_min;

				/// <summary>The maximum axis value</summary>
				public double Max
				{
					get { return m_max; }
					set
					{
						if (m_max == value) return;
						Debug.Assert(Min < value, "Range must be positive and non-zero");
						m_max = value;
					}
				}
				private double m_max;

				/// <summary>The total range of this axis (max - min)</summary>
				public double Span
				{
					get { return Max - Min; }
					set
					{
						if (Span == value) return;
						Debug.Assert(value > 0, "Range must be positive and non-zero");
						var c = Centre;
						m_min = c - 0.5*value;
						m_max = c + 0.5*value;
					}
				}

				/// <summary>The centre value of the range</summary>
				public double Centre
				{
					get { return (Min + Max) * 0.5; }
					set
					{
						var d = value - Centre;
						m_min += d;
						m_max += d;
					}
				}

				/// <summary>Allow scrolling on this axis</summary>
				public bool AllowScroll { get; set; }

				/// <summary>Allow zooming on this axis</summary>
				public bool AllowZoom { get; set; }

				/// <summary>Get/Set whether the range can be changed by user input</summary>
				public bool LockRange { get; set; }

				/// <summary>Convert the axis value to a string</summary>
				public Func<double,string> TickText;

				/// <summary>Scroll the axis by 'delta'</summary>
				public void Shift(double delta)
				{
					if (!AllowScroll) return;
					Centre += delta;
				}

				/// <summary>Return the position of the grid lines for this axis</summary>
				public void GridLines(out double min, out double max, out double step)
				{
					var dims = Owner.ChartDimensions;
					var axis_length = Owner.XAxis == this ? dims.ChartArea.Width : Owner.YAxis == this ? dims.ChartArea.Height : 0.0;
					var max_ticks = axis_length / Options.PixelsPerTick;

					// Choose step sizes
					var span = Span;
					double step_base = Math.Pow(10.0, (int)Math.Log10(Span)); step = step_base;
					foreach (var s in new[]{0.05f, 0.1f, 0.2f, 0.25f, 0.5f, 1.0f, 2.0f, 5.0f, 10.0f, 20.0f, 50.0f})
					{
						if (s * span > max_ticks * step_base) continue;
						step = step_base / s;
					}

					min = (Min - Math.IEEERemainder(Min, step)) - Min;
					max = Span * 1.0001;
					if (min < 0.0) min += step;
							
					// Protect against increments smaller than can be represented
					if (min + step == min) step = (max - min) * 0.01f;

					// Protect against too many ticks along the axis
					if (max - min > step*100) step = (max - min) * 0.01f;
				}

				/// <summary>Add the graphics associated with this element to the window</summary>
				internal void AddToScene(View3d.Window window)
				{
					if (m_gridlines == null) return;
					window.AddObject(m_gridlines);
				}

				/// <summary>Friendly string view</summary>
				public override string ToString()
				{
					return "[{0}:{1}]".Fmt(Min, Max);
				}
			}
		}

		#endregion

		#region Nested Classes

		/// <summary>Chart change types</summary>
		public enum EChangeType
		{
			/// <summary>
			/// Raised after elements in the chart have been moved, resized, or had their content changed.
			/// This event will be raised in addition to the more detailed modification events below</summary>
			Edited,

			///// <summary>
			///// A new node is being created.
			///// Setting 'Cancel' for this event will about the add.</summary>
			//AddNodeBegin,
			//AddNodeEnd,
			//AddNodeCancelled,

			///// <summary>
			///// A new link is being created.
			///// Setting 'Cancel' for this event will abort the add.</summary>
			//AddLinkBegin,
			//AddLinkEnd,
			//AddLinkAbort,

			///// <summary>
			///// An end of a connector is being moved to another node.
			///// Setting 'Cancel' for this event will abort the move.</summary>
			//MoveLinkBegin,
			//MoveLinkEnd,
			//MoveLinkAbort,

			///// <summary>
			///// An end of a connector is being moved around on the diagram.
			///// Called whenever the end is above a node (even if !Enabled).
			///// Setting 'Cancel' for this event will abort the move/add</summary>
			//LinkMoving,

			/// <summary>
			/// Elements are about to be deleted from the chart by the user.
			/// Setting 'Cancel' for this event will abort the deletion.</summary>
			RemovingElements,
		}

		/// <summary>Event args for the diagram changed event</summary>
		public class ChartChangedEventArgs :EventArgs
		{
			// Note: many events are available by attaching to the Elements binding list

			/// <summary>The type of change that occurred</summary>
			public EChangeType ChgType { get; private set; }

			/// <summary>A cancel property for "about to change" events</summary>
			public bool Cancel { get; set; }

			public ChartChangedEventArgs(EChangeType ty)
			{
				ChgType  = ty;
				Cancel   = false;
			}
		}

		/// <summary>HashSet wrapper for dirty Elements</summary>
		public class DirtyElements :HashSet<Element>
		{
			private readonly ChartControl m_chart;
			public DirtyElements(ChartControl owner)
			{
				m_chart = owner;
			}
			public new void Add(Element item)
			{
				Debug.Assert(item.Chart == m_chart);
				base.Add(item);
			}
		}

		/// <summary>Results collection for a hit test</summary>
		public class HitTestResult
		{
			public class Hit
			{
				/// <summary>The type of element hit</summary>
				public Entity Entity { get { return Element.Entity; } }

				/// <summary>The element that was hit</summary>
				public Element Element { get; private set; }

				/// <summary>Where on the element it was hit (in element space)</summary>
				public PointF Point { get; private set; }

				/// <summary>The element's chart location at the time it was hit</summary>
				public m4x4 Location { get; private set; }

				public Hit(Element elem, PointF pt)
				{
					Element  = elem;
					Point    = pt;
					Location = elem.Position;
				}
				public override string ToString()
				{
					return Element.ToString();
				}
			}

			/// <summary>The collection of hit objects</summary>
			public List<Hit> Hits { get; private set; }

			/// <summary>The camera position when the hit test was performed (needed for chart to screen space conversion)</summary>
			public View3d.CameraControls Camera { get; private set; }

			public HitTestResult(View3d.CameraControls cam)
			{
				Hits = new List<Hit>();
				Camera = cam;
			}
		}

		/// <summary>String constants used in XML export/import</summary>
		public static class XmlTag
		{
			public const string Root              = "root"               ;
			public const string TypeAttribute     = "ty"                 ;
			public const string Element           = "elem"               ;
			public const string Id                = "id"                 ;
			public const string Position          = "pos"                ;
		}

		#endregion

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		}
		#endregion
	}
}
