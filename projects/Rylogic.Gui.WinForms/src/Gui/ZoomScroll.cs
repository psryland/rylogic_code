//***************************************************
// Copyright (c) Rylogic Ltd 2013
//***************************************************
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WinForms
{
	public class ZoomScroll :Control
	{
		private const float m_corner_radius = 4f;
		private readonly HoverScroll m_hoverscroll;
		private bool m_dragging = false;

		public ZoomScroll()
		{
			m_hoverscroll = new HoverScroll();
			DoubleBuffered = true;
			TotalRange = new Range(0, 1000);
			ZoomedRange = TotalRange;
			VisibleRange = new Range(35, 50);
			MinimumVisibleRangeSize = 20;
			VisibleRangeColor = Color.FromArgb(unchecked((int)0x80008000));
			VisibleRangeBorderColor = Color.FromArgb(unchecked((int)0xFF008000));
			TrackColor = SystemColors.ControlDark;
			SetStyle(
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.ResizeRedraw |
				ControlStyles.Selectable |
				ControlStyles.UserPaint, true);

			m_hoverscroll.WindowHandles.Add(Handle);
			Application.AddMessageFilter(m_hoverscroll);
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Application.RemoveMessageFilter(m_hoverscroll);
		}
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Region = new Region(Gdi.RoundedRectanglePath(ClientRectangle, m_corner_radius)); // Set the clip boundary
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.PageUp) { VisibleRange = VisibleRange.Shift(-m_large_change); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.PageDown) { VisibleRange = VisibleRange.Shift(+m_large_change); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Up && e.Control) { VisibleRange = VisibleRange.Shift(-m_small_change); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Down && e.Control) { VisibleRange = VisibleRange.Shift(+m_small_change); RaiseScrollEndEvent(); }
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Capture = true;
			m_dragging = true;
			CentreVisible(e.Y);
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (m_dragging) CentreVisible(e.Y);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			Capture = false;
			m_dragging = false;
			CentreVisible(e.Y);
			RaiseScrollEndEvent();
		}
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			if (e.Delta > 0) Zoom = Zoom * 1.05f;
			if (e.Delta < 0) Zoom = Zoom * 0.95f;
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Find the bounding area of the control
			var bounds = ClientRectangle;
			bounds.Inflate(-1, -1);
			if (bounds.Width <= 0 || bounds.Height <= 0)
				return;

			var gfx = e.Graphics;
			var total = ZoomedRange;
			gfx.SmoothingMode = SmoothingMode.AntiAlias;

			Color c0, c1;
			Point pt0 = new Point(bounds.Left, 0);
			Point pt1 = new Point(bounds.Right, 0);

			// Background
			c0 = Gfx_.Blend(Color.Black, TrackColor, 0.7f);
			c1 = TrackColor;
			using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1)) // This throws OutOfMemoryException if pt0 == pt1.. ffs MS..
				gfx.FillRectangle(bsh, bounds);

			// Draw the overlay image
			if (Overlay != null)
			{
				var r = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
				var s = new Rectangle(0, 0, Overlay.Width, Overlay.Height);
				if (OverlayAttributes == null)
					gfx.DrawImage(Overlay, r, s, GraphicsUnit.Pixel);
				else
					gfx.DrawImage(Overlay, r, s, GraphicsUnit.Pixel, OverlayAttributes);
			}

			// Draw the visible range
			if (bounds.Width > 2)
			{
				int sy = (int)(Math_.Frac(total.Beg, VisibleRange.Beg, total.End) * bounds.Height);
				int ey = (int)(Math_.Frac(total.Beg, VisibleRange.End, total.End) * bounds.Height);
				var r = new Rectangle(bounds.X + 1, bounds.Y + sy, bounds.Width - 2, ey - sy);
				using (var bsh = new SolidBrush(VisibleRangeColor))
				{
					gfx.FillRectangle(bsh, r);
				}
				if (bounds.Width > 4)
					using (var pen = new Pen(VisibleRangeBorderColor))
					{
						gfx.DrawLine(pen, r.Left + 1, r.Top, r.Right - 1, r.Top);
						gfx.DrawLine(pen, r.Left + 1, r.Bottom, r.Right - 1, r.Bottom);
					}
			}

			// Borders
			gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, bounds, m_corner_radius);
		}

		/// <summary>The total un-zoomed size of the represented range</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The total un-zoomed size of the represented range")]
		public Range TotalRange
		{
			get { return m_total_range; }
			set
			{
				if (Equals(m_total_range, value)) return;
				if (value.Size <= 0) value.End = value.Beg + 1;
				m_total_range   = value;
				m_zoomed_range  = value;
				m_visible_range = Range.Constrain(m_visible_range, m_total_range);
				Invalidate();
			}
		}
		private Range m_total_range;

		/// <summary>The size of the total range after zooming</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The size of the total range after zooming")]
		public Range ZoomedRange
		{
			get { return m_zoomed_range; }
			set
			{
				if (Equals(m_zoomed_range, value)) return;
				if (value.Size <= 0) value.End = value.Beg + 1;
				m_zoomed_range  = Range.Constrain(value, m_total_range);
				m_visible_range = Range.Constrain(m_visible_range, m_zoomed_range);
				Invalidate();
			}
		}
		private Range m_zoomed_range;

		/// <summary>The size of the visible region of the represented range</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The size of the visible region of the represented range")]
		public Range VisibleRange
		{
			get { return m_visible_range; }
			set
			{
				if (Equals(m_visible_range, value)) return;
				if (value.Size <= 0) value.End = value.Beg + 1;
				m_visible_range = Range.Constrain(value, m_zoomed_range);
				RaiseScrollEvent();
				Invalidate();
			}
		}
		private Range m_visible_range;

		/// <summary>Effectively the maximum zoom allowed = TotalRange.Size / MinimumVisibleRangeSize</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("Effectively the maximum zoom allowed = TotalRange.Size / MinimumVisibleRangeSize")]
		public int MinimumVisibleRangeSize { get; set; }

		/// <summary>The number of steps to move on page down/up</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The number of steps to move on page down/up")]
		public long LargeChange
		{
			get { return m_large_change; }
			set { m_large_change = Math_.Clamp(value, 1, TotalRange.Size); Invalidate(); }
		}
		private long m_large_change;

		/// <summary>The number of steps to move on arrow down/up</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The number of steps to move on arrow down/up")]
		public long SmallChange
		{
			get { return m_small_change; }
			set { m_small_change = Math_.Clamp(value, 1, TotalRange.Size); Invalidate(); }
		}
		private long m_small_change;

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The colour of the highlight showing the visible region")]
		public Color VisibleRangeColor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The border colour that delimits the visible region")]
		public Color VisibleRangeBorderColor { get; set; }

		/// <summary>The background colour of the scroll control</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("TrackColor")]
		public Color TrackColor { get; set; }

		/// <summary>The zoom factor</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The zoom factor")]
		public float Zoom
		{
			get { return m_total_range.Size / (float)m_zoomed_range.Size; }
			set
			{
				if (Math.Abs(value - Zoom) < float.Epsilon) return;
				value = Math_.Clamp(value, 1.0f, 1000000.0f);
				var sz = Math.Max((int)(m_total_range.Size / value), MinimumVisibleRangeSize);
				var r = new Range(0, sz){Mid = ZoomedRange.Mid};
				if (r.Beg > VisibleRange.Beg) r = r.Shift(VisibleRange.Beg - r.Beg);
				if (r.End < VisibleRange.End) r = r.Shift(VisibleRange.End - r.End);
				ZoomedRange = r;
			}
		}

		/// <summary>Raised whenever the visible range is moved by either a mouse or keyboard action immediately after the value is updated</summary>
		public event EventHandler Scroll;
		private void RaiseScrollEvent()
		{
			if (Scroll == null) return;
			Scroll(this, EventArgs.Empty);
		}

		/// <summary>Raised whenever a scroll finishes, immediately after the 'Scroll' event</summary>
		public event EventHandler ScrollEnd;
		private void RaiseScrollEndEvent()
		{
			if (ScrollEnd == null) return;
			ScrollEnd(this, EventArgs.Empty);
		}

		// This is used to represent an overview of the range represented.
		// To use transparency, 
		/// <summary>An overlay to draw in the background of the scrollbar</summary>
		public Image Overlay { get; set; }
		public ImageAttributes OverlayAttributes { get; set; }

		/// <summary>Set the centre of the visible range to client rect relative coord 'y'</summary>
		private void CentreVisible(int y)
		{
			var vis = VisibleRange;
			vis.Mid = ZoomedRange.Beg + (long)(Math_.Frac(0, y, Height) * ZoomedRange.Size);
			VisibleRange = vis;
		}
	}
}
