﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.extn;
using pr.maths;
using pr.gfx;
using pr.ldr;

namespace pr.gui
{
	/// <summary>A control for drawing box and line diagrams</summary>
	public class DiagramControl :UserControl ,ISupportInitialize
	{
		/// <summary>Anything drawn on the diagram needs a position and bounding rectangle</summary>
		public interface IElement
		{
			/// <summary>The element to diagram transform</summary>
			m4x4 Position { get; }

			/// <summary>An ldr string for the graphics of the element (not including any text) in element space.</summary>
			string Graphics();
		}
		public interface INode :IElement
		{
			/// <summary>Text associated with the node</summary>
			string Text { get; }
		}
		public interface ILabel :IElement {}
		public interface IConnector :IElement {}

		#region Nodes

		/// <summary>Style attributes for nodes</summary>
		public class NodeStyle
		{
			public Color Border { get; set; }
			public Color Fill   { get; set; }
			public Color Text   { get; set; }
			public Font Font    { get; set; }

			public NodeStyle()
			{
				Border = Color.Black;
				Fill   = Color.WhiteSmoke;
				Text   = Color.Black;
				Font   = SystemFonts.DefaultFont;
			}
		}

		/// <summary>Simple rectangular box node</summary>
		public class BoxNode :DiagramControl.INode
		{
			/// <summary>Text to display in this box</summary>
			public string Text { get; set; }

			/// <summary>Style attributes for the node</summary>
			public NodeStyle Style { get; set; }

			/// <summary>The element to diagram transform</summary>
			public m4x4 Position { get; set; }

			public BoxNode()
			{
				Text  = string.Empty;
				Style = new NodeStyle();
				Position   = m4x4.Identity;
			}

			/// <summary>Render the element (draw in diagram space, not screen space)</summary>
			public string Graphics()
			{
				var ldr = new LdrBuilder();
				using (ldr.Group())
				{
					ldr.Box("test", 0xFF00FF00);
					//ldr.Quad(0xFF)
					//gfx.SetClip(bounds.Inflated(5,5));

					//using (var path = Gfx.RoundedRectanglePath(bounds, 5f))
					//using (var brush = new PathGradientBrush(path))
					//{
					//	ColorBlend blend = new ColorBlend();
					//	blend.Colors = new Color[] { Color.Transparent, Color.Black, Color.Black };
					//	blend.Positions = new float[] { 0.0f, 0.1f, 1.0f };
					//	brush.InterpolationColors = blend;
					//	gfx.FillPath(brush, path);
					//}

					//using (var shd = new PathGradientBrush(, WrapMode.Clamp){CenterPoint})
					//	gfx.FillRectangleRounded(shd, bounds.Inflated(5,5), 3f);     // drop shadow

					//gfx.FillRectangleRounded(Style.Fill, bounds, 3f);
					//gfx.DrawRectangleRounded(Style.Border, bounds.Inflated(-1,-1), 3f);

					//v2 sz = gfx.MeasureString(Text, Style.Font);
					//gfx.DrawString(Text, Style.Font, Style.Text, Bounds.Centre - sz/2);
				}
				return ldr.ToString();
			}
		}

		/// <summary>Simple contector between nodes</summary>
		public class Connector :DiagramControl.IConnector
		{
			/// <summary>The element to diagram transform</summary>
			public m4x4 Position { get; set; }

			/// <summary>Render the element (draw in diagram space, not screen space)</summary>
			public string Graphics()
			{
				return string.Empty;
			}
		}

		#endregion

		/// <summary>Wraps a diagram element</summary>
		private class Element
		{
			public IElement m_element;
			public int m_sort_axis;

			/// <summary>The centre position of this element</summary>
			//public v2 m_centre { get { return m_element.Bounds.Centre; } }
		}

		/// <summary>Rendering options</summary>
		public class RdrOptions
		{
			// Colours for graph elements
			public Color m_bg_colour      = SystemColors.ControlDark;      // The fill colour of the background
			public Color m_title_colour   = Color.Black;                   // The colour of the title text
			public Color m_grid_colour    = Color.FromArgb(230, 230, 230); // The colour of the grid lines

			// Graph margins and constants
			public float m_left_margin    = 0.0f; // Fractional distance from the left edge
			public float m_right_margin   = 0.0f;
			public float m_top_margin     = 0.0f;
			public float m_bottom_margin  = 0.0f;
			public float m_title_top      = 0.01f; // Fractional distance  down from the top of the client area to the top of the title text
			public Font  m_title_font     = new Font("tahoma", 12, FontStyle.Bold);    // Font to use for the title text
			public Font  m_note_font      = new Font("tahoma",  8, FontStyle.Regular); // Font to use for graph notes
			public RdrOptions Clone() { return (RdrOptions)MemberwiseClone(); }
		}

		// Members
		private readonly View3d       m_view3d;         // Renderer
		private readonly RdrOptions   m_rdr_options;    // Rendering options
		private readonly EventBatcher m_eb_update_diag; // Event batcher for updating the diagram graphics
		private View3d.CameraControls m_camera;         // The virtual window over the diagram
		private PointF                m_grab_location;  // The location in diagram space of where the diagram was "grabbed"
		private Rectangle             m_selection;      // Area selection, has width, height of zero when the user isn't selecting
		private RangeF                m_zoom_range;     // The limits for zooming
		private float                 m_zoom;           // The zoom value

		public DiagramControl() :this(new RdrOptions()) {}
		private DiagramControl(RdrOptions rdr_options)
		{
			m_view3d         = new View3d(Handle);
			m_rdr_options    = rdr_options;
			m_eb_update_diag = new EventBatcher(UpdateDiagram);
			m_camera         = new View3d.CameraControls(m_view3d.Drawset);
			m_zoom_range     = new RangeF(0.001, 1000.0);
			m_zoom           = 1f;

			InitializeComponent();

			m_view3d.Drawset.FocusPointVisible = false;
			m_view3d.Drawset.OriginVisible = false;
			m_view3d.Drawset.Orthographic = true;
			m_camera.SetPosition(new v4(0,0,10,1), v4.Origin, v4.YAxis);

			Elements = new ListEvt<IElement>();
			Elements.ListChanged += (s,a) =>
				{
					if ((a.ChgType & ListChg.ItemAddedOrRemoved) != 0)
						m_eb_update_diag.Signal();
				};

			Load += (s,a) => ResetView();
			MouseNavigation = true;
		}
		protected override void Dispose(bool disposing)
		{
			m_view3d.Dispose();
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>Diagram objects</summary>
		public ListEvt<IElement> Elements { get; private set; }

		/// <summary>Update the graphics in the diagram</summary>
		private void UpdateDiagram()
		{
			m_view3d.Drawset.RemoveAllObjects();
			foreach (var node in Elements)
			{
				var ldr = node.Graphics();
				var obj = new View3d.Object(ldr){Tag = node};
				m_view3d.Drawset.AddObject(obj);
			}
			m_view3d.SignalRefresh();
		}

		/// <summary>Controls for how the diagram is rendered</summary>
		public RdrOptions RenderOptions
		{
			get { return m_rdr_options; }
		}

		/// <summary>Enable/Disable default mouse control</summary>
		[Browsable(false)] public bool MouseNavigation
		{
			set
			{
				MouseDown  -= OnMouseDown;
				MouseUp    -= OnMouseUp;
				MouseWheel -= OnMouseWheel;
				if (value)
				{
					MouseDown  += OnMouseDown;
					MouseUp    += OnMouseUp;
					MouseWheel += OnMouseWheel;
				}
			}
		}

		/// <summary>Mouse navigation - public to allow users to forward mouse calls to us.	/// </summary>
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			m_selection.Location = e.Location;
			m_selection.Width = 0;
			m_selection.Height = 0;
			switch (e.Button)
			{
			case MouseButtons.Left:
				m_grab_location = PointToDiagram(e.Location);
				Cursor = Cursors.SizeAll;
				MouseMove -= OnMouseDrag;
				MouseMove += OnMouseDrag;
				break;
			}
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			switch (e.Button)
			{
			case MouseButtons.Left:
				Cursor = Cursors.Default;
				MouseMove -= OnMouseDrag;
				break;
			}
			Refresh();
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			//if (!DiagramRegion(ClientSize).Contains(e.Location)) return;
			//var point = PointToDiagram(e.Location);
			//var delta = e.Delta < -999 ? -999 : e.Delta > 999 ? 999 : e.Delta;
			//Zoom *= (1.0f + delta * 0.001f);
			//PositionDiagram(e.Location, point);
			//Refresh();
		}

		/// <summary>Handle mouse dragging the graph around</summary>
		private void OnMouseDrag(object sender, MouseEventArgs e)
		{
			var grab_loc = DiagramToPoint(m_grab_location);
			var dx = e.Location.X - grab_loc.X;
			var dy = e.Location.Y - grab_loc.Y;
			if (dx*dx + dy*dy < 25) return; // must drag at least 5 pixels
			PositionDiagram(e.Location, m_grab_location);
			Refresh();
		}

		/// <summary>Reset the view of the diagram back to the default</summary>
		public void ResetView(bool reset_position = true, bool reset_size = true)
		{
			if (reset_position)
			{
				m_camera.SetPosition(new v4(0,0,10,1), v4.Origin, v4.YAxis);
			}
			//if (reset_size && Math.Min(ClientSize.Width, ClientSize.Height) != 0)
			//{
			//	m_window.SizeX = (float)(ClientSize.Width  * m_zoom / PixelsPerUnit);
			//	m_window.SizeY = (float)(ClientSize.Height * m_zoom / PixelsPerUnit);
			//}
		}

		///// <summary>
		///// Zoom in/out on the diagram. Remember to call refresh.
		///// Zoom is a floating point value where 1f = no zoom, 2f = 2x magnification</summary>
		//public double Zoom
		//{
		//	get { return m_zoom; }
		//	set
		//	{
		//		TakeSnapshot();
		//		m_zoom = (float)Maths.Clamp(value, m_zoom_range.Begin, m_zoom_range.End);
		//		ResizeViewToClientArea();
		//		RegenBitmap = true;
		//	}
		//}

		///// <summary>Zoom limits</summary>
		//public double ZoomMin
		//{
		//	get { return m_zoom_range.Begin; }
		//	set { Debug.Assert(value > 0f); m_zoom_range.Begin = value; }
		//}
		//public double ZoomMax
		//{
		//	get { return m_zoom_range.End; }
		//	set { Debug.Assert(value > 0f); m_zoom_range.End   = value; }
		//}

		/// <summary>
		/// Returns a point in diagram space from a point in client space
		/// Use to convert mouse (client-space) locations to diagram coordinates</summary>
		public PointF PointToDiagram(Point point)
		{
			var ws = m_camera.WSPointFromSSPoint(point);
			return new PointF(ws.x, ws.y);
			//var wind = m_window.Lower();
			//var rect = DiagramRegion(ClientSize);
			//return new PointF(
			//	wind.x + (point.X - rect.Left) * m_window.SizeX / rect.Width ,
			//	wind.y + (point.Y - rect.Top ) * m_window.SizeY / rect.Height);
		}

		/// <summary>Returns a point in client space from a point in diagram space. Inverse of PointToDiagram</summary>
		public Point DiagramToPoint(PointF point)
		{
			var ws = new v4(point.X, point.Y, 0.0f, 1.0f);
			return m_camera.SSPointFromWSPoint(ws);
			//var wind = m_window.Lower();
			//var rect = DiagramRegion(ClientSize);
			//return new Point(
			//	(int)(rect.Left + (point.X - wind.x) * rect.Width  / m_window.SizeX),
			//	(int)(rect.Top  + (point.Y - wind.y) * rect.Height / m_window.SizeY));
		}

		/// <summary>Shifts the camera so that diagram space position 'ds' is at client space position 'cs'</summary>
		public void PositionDiagram(Point cs, PointF ds)
		{
			// Dragging the diagram is the same as shifting the camera in the opposite direction
			var dst = PointToDiagram(cs);
			var o2w = m_camera.O2W;
			o2w.p.x += ds.X - dst.X;
			o2w.p.y += ds.Y - dst.Y;
			m_camera.O2W = o2w;
			Refresh();
		}

		/// <summary>Resize the control</summary>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			m_view3d.RenderTargetSize = new Size(Width,Height);
		}

		// Absorb this event
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (m_view3d.Drawset == null)
				base.OnPaintBackground(e);
		}

		// Paint the control
		protected override void OnPaint(PaintEventArgs e)
		{
			if (m_view3d.Drawset == null)
			{
				base.OnPaint(e);
			}
			else
			{
				m_view3d.Drawset.Render();
			}
		}

		/// <summary>Get the rectangular area of the diagram for a given client area.</summary>
		public Rectangle DiagramRegion(Size target_size)
		{
			var marginL = (int)(target_size.Width  * RenderOptions.m_left_margin  );
			var marginT = (int)(target_size.Height * RenderOptions.m_top_margin   );
			var marginR = (int)(target_size.Width  * RenderOptions.m_right_margin );
			var marginB = (int)(target_size.Height * RenderOptions.m_bottom_margin);

			//if (m_title.Length != 0)       { marginT += TextRenderer.MeasureText(m_title      ,       RenderOptions.m_title_font).Height; }
			//if (m_yaxis.Label.Length != 0) { marginL += TextRenderer.MeasureText(m_yaxis.Label, YAxis.RenderOptions.m_label_font).Height; }
			//if (m_xaxis.Label.Length != 0) { marginB += TextRenderer.MeasureText(m_xaxis.Label, XAxis.RenderOptions.m_label_font).Height; }

			//var sx = TextRenderer.MeasureText(YAxis.TickText(AestheticTickValue(9999999.9)), YAxis.RenderOptions.m_tick_font);
			//var sy = TextRenderer.MeasureText(XAxis.TickText(AestheticTickValue(9999999.9)), XAxis.RenderOptions.m_tick_font);
			//marginL += YAxis.RenderOptions.m_tick_length + sx.Width;
			//marginB += XAxis.RenderOptions.m_tick_length + sy.Height;

			var x      = Math.Max(0, marginL);
			var y      = Math.Max(0, marginT);
			var width  = Math.Max(0, target_size.Width  - (marginL + marginR));
			var height = Math.Max(0, target_size.Height - (marginT + marginB));
			return new Rectangle(x, y, width, height);
		}

		///// <summary>
		///// Begins rendering of the diagram into a bitmap.
		///// If 'async' is true, then this method returns immediately with a placeholder bitmap otherwise the finished bitmap is returned.
		///// 'render_complete' is called (if not null) once the finished bitmap is complete.
		///// Note: 'render_complete' is always called in the UI thread context</summary>
		//public Bitmap GetDiagramBitmap(Size size, bool async, Action<DiagramControl, Bitmap> render_complete, Bitmap bm)
		//{
		//	Debug.Assert(!size.IsEmpty);

		//	// Signal the start of a new render. This should cause existing renders to cancel
		//	++m_rdr_id;
		//	m_rdr_idle.WaitOne(); // Wait for existing threads to exit
		//	m_rdr_idle.Reset();

		//	// Create a bitmap of the correct size
		//	if (bm == null || bm.Size != size)
		//		bm = new Bitmap(size.Width, size.Height);

		//	if (!async)
		//	{
		//		// Render 'bm' synchronously
		//		GenerateDiagramBitmap(bm, m_viewport, () => false);
		//		if (render_complete != null) render_complete(this, bm);
		//		m_rdr_idle.Set();
		//	}
		//	else
		//	{
		//		// If rendering async, then create a placeholder bitmap
		//		using (var gfx = Graphics.FromImage(bm))
		//		{
		//			var region = DiagramRegion(size);

		//			// Add a note to indicate the diagram is still rendering
		//			const string rdring_msg = "rendering...";
		//			var sz    = gfx.MeasureString(rdring_msg, m_rdr_options.m_title_font);
		//			var brush = new SolidBrush(Color.FromArgb(0x80, Color.Black));
		//			var pt    = new PointF(region.X + (region.Width - sz.Width)*0.5f, region.Y + (region.Height - sz.Height)*0.5f);
		//			gfx.DrawString(rdring_msg, m_rdr_options.m_title_font, brush, pt);
		//		}

		//		// Spawn a thread to render the diagram
		//		var viewport = m_viewport;
		//		ThreadPool.QueueUserWorkItem(rdr_id =>
		//			{
		//				try
		//				{
		//					var tmp_bm = new Bitmap(size.Width, size.Height);
		//					var cancelled = GenerateDiagramBitmap(tmp_bm, viewport, () => m_rdr_id != (int)rdr_id);
		//					if (!cancelled && render_complete != null) BeginInvoke(render_complete, this, tmp_bm);
		//					m_rdr_idle.Set();
		//				}
		//				catch (Exception ex)
		//				{
		//					Debug.Assert(false, "Exception in render worker thread: " + ex.Message);
		//				}
		//			}, m_rdr_id);
		//	}
		//	return bm;
		//}
		//public Bitmap GetDiagramBitmap(Size size)
		//{
		//	return GetDiagramBitmap(size, false, null, null);
		//}

		///// <summary>
		///// Returns a transform that can be used to draw in unscaled diagram space.
		///// 'diag' is the viewport window rectangle in diagram space
		///// 'client' is the client area rectangle in client space</summary>
		//private Matrix Diag2Client(RectangleF diag, Rectangle client)
		//{
		//	var region = DiagramRegion(client.Size); // The area in 'client' that the diagram will be drawn
		//	var scale_x = region.Width  / (Maths.FEql(diag.Width , 0f) ? 1f : diag.Width );
		//	var scale_y = region.Height / (Maths.FEql(diag.Height, 0f) ? 1f : diag.Height);
		//	var scale = Math.Min(scale_x, scale_y);
		//	return new Matrix(scale, 0f, 0f, scale, region.Left - diag.X * scale, region.Top - diag.Y * scale);
		//}

		///// <summary>Synchronously render the diagram into the bitmap 'bm'</summary>
		//private bool GenerateDiagramBitmap(Bitmap bm, BRect viewport, Func<bool> cancel_pending)
		//{
		//	var region = DiagramRegion(bm.Size);
		//	var d2s = Diag2Client(viewport, region);
		//	try
		//	{
		//		using (var gfx = Graphics.FromImage(bm))
		//		{
		//			gfx.InterpolationMode  = InterpolationMode.Bilinear;
		//			gfx.SmoothingMode      = SmoothingMode.HighQuality;
		//			gfx.CompositingQuality = CompositingQuality.HighQuality;
		//			gfx.SetClip(region);

		//			// Switch to diagram space
		//			gfx.Transform = d2s;

		//			// Render the elements onto the diagram
		//			foreach (var elem in VisibleElements)
		//			{
		//				elem.Render(gfx);
		//				if (cancel_pending())
		//					break;
		//			}

		//			//// Can't use inverted y scale here because the text comes out upside down
		//			//gfx.Transform = text_xfrm;
		//			//scale_y = -scale_y;

		//			//// Allow clients to draw in graph space
		//			//if (AddOverlaysOnRender != null && !cancel_pending())
		//			//	AddOverlaysOnRender(this, gfx, scale_x, scale_y);

		//			//// Add notes to the graph
		//			//foreach (var note in m_notes)
		//			//{
		//			//	gfx.DrawString(note.m_msg, m_rdr_options.m_note_font, new SolidBrush(note.m_colour), new PointF(note.m_loc.X*scale_x, note.m_loc.Y*scale_y));
		//			//	if (cancel_pending()) break;
		//			//}

		//			//gfx.ResetTransform();
		//			//gfx.ResetClip();
		//		}
		//	}
		//	catch (Exception)
		//	{
		//		// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
		//	//	gfx.Transform = text_xfrm;
		//	//	gfx.DrawString("Rendering error occured: "+ex.Message, m_rdr_options.m_title_font, new SolidBrush(Color.FromArgb(0x80, Color.Black)), new PointF());
		//	}
		//	return cancel_pending();
		//}

		///// <summary>Render the background of the diagram</summary>
		//private void RenderBkgd(Graphics gfx, BRect view)
		//{
		//	using (gfx.SaveState())
		//	{
		//		gfx.CompositingQuality = CompositingQuality.HighQuality;
		//		gfx.SmoothingMode      = SmoothingMode.None;
		//		gfx.InterpolationMode  = InterpolationMode.NearestNeighbor;
		//		gfx.PixelOffsetMode    = PixelOffsetMode.Half;

		//		// Clear the bitmap to the background colour
		//		gfx.Clear(m_rdr_options.m_bg_colour);

		//		// Draw the diagram background
		//		using (var b = new SolidBrush(Color.FromArgb(0x10, Color.Black)))
		//		{
		//			var xstart = (int)(view.Lower(0) < 0 ? view.Lower(0) - 1f : view.Lower(0));
		//			var xend   = (int)(view.Upper(0) > 0 ? view.Upper(0) + 1f : view.Upper(0));
		//			var ystart = (int)(view.Lower(1) < 0 ? view.Lower(1) - 1f : view.Lower(1));
		//			var yend   = (int)(view.Upper(1) > 0 ? view.Upper(1) + 1f : view.Upper(1));
		//			for (int x = xstart; x <= xend; x += 2)
		//				gfx.FillRectangle(b, Rectangle.FromLTRB(x, ystart, x + 1, yend));
		//			for (int y = ystart; y <= yend; y += 2)
		//				gfx.FillRectangle(b, Rectangle.FromLTRB(xstart, y, xend, y + 1));
		//		}
		//	}
		//}

		///// <summary>Returns all diagram elements that are visible</summary>
		//public IEnumerable<IElement> VisibleElements
		//{
		//	get
		//	{
		//		foreach (var elem in Nodes)
		//			yield return elem;
		//	}
		//}

		//private class KdTreeSorter :IKdTree<Element>
		//{
		//	/// <summary>The number of dimensions to sort on</summary>
		//	int IKdTree<Element>.Dimensions { get { return 2; } }

		//	/// <summary>Return the value of 'elem' on 'axis'</summary>
		//	float IKdTree<Element>.GetAxisValue(Element elem, int axis) { return elem.m_centre[axis]; }

		//	/// <summary>Save the sort axis generated during BuildTree for 'elem'</summary>
		//	void IKdTree<Element>.SortAxis(Element elem, int axis) { elem.m_sort_axis = axis; }

		//	/// <summary>Return the sort axis for 'elem'. Used during a search of the kdtree</summary>
		//	int IKdTree<Element>.SortAxis(Element elem) { return elem.m_sort_axis; }
		//}

		#region Component Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// DiagramControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "DiagramControl";
			this.Size = new System.Drawing.Size(373, 368);
			this.ResumeLayout(false);
		}

		public void BeginInit(){}
		public void EndInit(){}

		#endregion
	}
}