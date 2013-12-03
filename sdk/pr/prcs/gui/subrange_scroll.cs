using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.maths;

namespace pr.gui
{
	/// <summary>
	/// A vertical scroll bar replacement that shows a subrange within a larger range.
	/// This class cannot inherit from VScrollBar because the OS does fancy stuff rendering it</summary>
	public sealed class SubRangeScroll :Control
	{
		private const float m_corner_radius = 4f;
		private bool m_dragging = false;

		public interface ISubRange
		{
			Range Range { get; }
			Color Color { get; }
		}
		private class SubRange :ISubRange
		{
			public Rectangle m_rect;

			public Range Range { get; set; }
			public Color Color { get; set; }

			public SubRange(Range range, Color color) { Range = range; Color = color; }
		}

		/// <summary>The total size of the represented range</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The total size of the represented range")]
		public Range TotalRange
		{
			get { return m_total_range; }
			set
			{
				if (Equals(m_total_range, value)) return;
				if (value.Count <= 0) value.End = value.Begin + 1;
				m_total_range = value;
				m_thumb_range = Range.Constrain(m_thumb_range, m_total_range);
				foreach (var r in m_indicator_ranges) r.Range = Range.Constrain(r.Range, m_total_range);
				Invalidate();
			}
		}
		private Range m_total_range;

		/// <summary>The range of the thumb relative to TotalRange</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The size of the thumb within TotalRange")]
		public Range ThumbRange
		{
			get { return m_thumb_range; }
			set
			{
				if (Equals(m_thumb_range, value)) return;
				if (value.Count <= 0) value.End = value.Begin + 1;
				m_thumb_range = Range.Constrain(value, m_total_range);
				RaiseScrollEvent();
				Invalidate();
			}
		}
		private Range m_thumb_range;

		/// <summary>The minimum size to let the thumb get</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The minimum size to let the thumb get")]
		public int MinThumbSize { get; set; }

		/// <summary>The colour of the track</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The colour of the track")]
		public Color TrackColor { get; set; }

		/// <summary>The colour of the thumb</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The colour of the thumb")]
		public Color ThumbColor { get; set; }

		/// <summary>The number of steps to move on page down/up</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The number of steps to move on page down/up")]
		public long LargeChange
		{
			get { return m_large_change; }
			set { m_large_change = Maths.Clamp(value, 1, TotalRange.Count); Invalidate(); }
		}
		private long m_large_change;

		/// <summary>The number of steps to move on arrow down/up</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The number of steps to move on arrow down/up")]
		public long SmallChange
		{
			get { return m_small_change; }
			set { m_small_change = Maths.Clamp(value, 1, TotalRange.Count); Invalidate(); }
		}
		private long m_small_change;

		/// <summary>The ranges to draw on the scroll bar</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The indicator ranges to draw on the scroll bar")]
		public IEnumerable<ISubRange> IndicatorRanges
		{
			// Not exposing the list as the ranges need to clamped within TotalRange
			get { return m_indicator_ranges; }
		}
		private readonly List<SubRange> m_indicator_ranges = new List<SubRange>();

		/// <summary>Raised whenever the scroll box is moved by either a mouse or keyboard action immediately after the value is updated</summary>
		public event EventHandler Scroll;
		private void RaiseScrollEvent()
		{
			if (Scroll == null) return;
			Scroll(this, EventArgs.Empty);
		}

		/// <summary>Raised whenever a scroll finishes, immediately after the 'Scroll' event which is after the ValueChanged event</summary>
		public event EventHandler ScrollEnd;
		private void RaiseScrollEndEvent()
		{
			if (ScrollEnd == null) return;
			ScrollEnd(this, EventArgs.Empty);
		}

		// This is used to represent an overview of the range represented.
		// To use transparency, ...
		/// <summary>An overlay to draw in the background of the scrollbar</summary>
		public Image Overlay { get; set; }
		public ImageAttributes OverlayAttributes { get; set; }

		public SubRangeScroll()
		{
			MinThumbSize = 20;
			ThumbColor   = Color.FromArgb(unchecked ((int)0xFF008000));
			TrackColor   = SystemColors.ControlDark;
			TotalRange   = new Range(0, 100);
			ThumbRange   = new Range(35, 50);
			SetStyle(
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint|
				ControlStyles.ResizeRedraw |
				ControlStyles.Selectable |
				ControlStyles.UserPaint, true);
		}
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Region = new Region(Gfx.RoundedRectanglePath(ClientRectangle, m_corner_radius)); // Set the clip boundary
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.PageUp  )          { Range thm = ThumbRange; thm = thm.Shift(-m_large_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.PageDown)          { Range thm = ThumbRange; thm = thm.Shift(+m_large_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Up   && e.Control) { Range thm = ThumbRange; thm = thm.Shift(-m_small_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Down && e.Control) { Range thm = ThumbRange; thm = thm.Shift(+m_small_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			// Mouse down, tries to centre the thumb at the mouse location
			base.OnMouseDown(e);
			Capture = true;
			m_dragging = true;
			ScrollThumbPos(e.Y);
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (m_dragging) ScrollThumbPos(e.Y);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			Capture = false;
			m_dragging = false;
			ScrollThumbPos(e.Y);
			RaiseScrollEndEvent();
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			var bounds = ClientRectangle;
			bounds.Inflate(-1,-1);
			if (bounds.Width <= 0 || bounds.Height <= 0)
				return;

			var gfx = e.Graphics;
			var total = TotalRange;
			gfx.SmoothingMode = SmoothingMode.AntiAlias;

			Color c0,c1;
			Point pt0 = new Point(bounds.Left, 0);
			Point pt1 = new Point(bounds.Right, 0);

			// Create rectangles for the highlight ranges
			foreach (var r in m_indicator_ranges)
			{
				int sy = (int)(Maths.Frac(total.Begin, r.Range.Begin, total.End) * bounds.Height);
				int ey = (int)(Maths.Frac(total.Begin, r.Range.End  , total.End) * bounds.Height);
				r.m_rect = new Rectangle(bounds.X, bounds.Y + sy, bounds.Width, Math.Max(ey - sy, 1));
			}

			// Background
			c0 = Gfx.Blend(Color.Black, TrackColor, 0.8f);
			c1 = TrackColor;
			using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
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

			// Indicator ranges
			foreach (var r in m_indicator_ranges)
			{
				c0 = r.Color;
				c1 = Gfx.Blend(c0, Color.FromArgb(c0.A, Color.White), 0.2f);
				using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
					gfx.FillRectangle(bsh, r.m_rect);
			}

			// Thumb
			{
				var r = MakeThumbRect(bounds);
				c0 = Gfx.Blend(Color.White, Color.FromArgb(0x80, ThumbColor), 0.8f);
				c1 = ThumbColor;
				using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
					gfx.FillRectangle(bsh, r);
				if (r.Width > 2)
					using (var pen = new Pen(ThumbColor))
					{
						gfx.DrawLine(pen, r.Left+1, r.Top   , r.Right-1, r.Top);
						gfx.DrawLine(pen, r.Left+1, r.Bottom, r.Right-1, r.Bottom);
					}
			}

			// Borders
			gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, bounds, m_corner_radius);
			//using (var pen = new Pen(Color.FromArgb(255, VisibleRangeColor)))
			//    gfx.DrawRectangle(pen, vis_rect);
			//using (var pen = new Pen(Color.FromArgb(255, SelectedRangeColor)))
			//    gfx.DrawRectangle(pen, sel_rect);
			//gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, thumb_rect, m_corner_radius);
		}

		/// <summary>Reset the collection of indicator ranges</summary>
		public void ClearIndicatorRanges()
		{
			m_indicator_ranges.Clear();
		}

		/// <summary>Add an indicator range</summary>
		public void AddIndicatorRange(Range range, Color colour)
		{
			System.Diagnostics.Debug.Assert(m_total_range.Contains(range), "Indicator range outside total range");
			range = Range.Constrain(range, m_total_range);
			m_indicator_ranges.Add(new SubRange(range, colour));
		}

		/// <summary>Set the thumb position given a control space Y value</summary>
		private void ScrollThumbPos(int y)
		{
			Range thm = ThumbRange;
			thm.Mid = TotalRange.Begin + (long)(Maths.Frac(0, y, Height) * TotalRange.Count);
			ThumbRange = thm;
		}

		/// <summary>Create the rectangle describing the thumb position</summary>
		private Rectangle MakeThumbRect(Rectangle bounds)
		{
			var height = bounds.Height;
			var thm    = ThumbRange;
			var total  = TotalRange;

			int sy = (int)(Maths.Frac(total.Begin, thm.Begin, total.End) * bounds.Height);
			int ey = (int)(Maths.Frac(total.Begin, thm.End  , total.End) * bounds.Height);
			var r = new Rectangle(bounds.X, bounds.Y + sy, bounds.Width, ey - sy);

			// Apply the minimum thumb size constraint
			if (r.Height < MinThumbSize)
			{
				int diff = MinThumbSize - r.Height;
				r.Height += diff;
				r.Y      -= diff / 2;
				if (r.Top    <      0) r.Y = 0;
				if (r.Bottom > height) r.Y = height - r.Height;
			}
			return r;
		}
	}
}

		///// <summary>Return the current centre position of the slider relative to TotalRange.</summary>
		//public long RangeCentrePos
		//{
		//    get { return (long)(TotalRange * Maths.Lerp(ThumbSize*0.5, 1.0 - ThumbSize*0.5, Fraction)); }
		//}
		///// <summary>The thumb size as a fraction of the total range</summary>
		//public double ThumbSize
		//{
		//    get { return m_thumbsize; }
		//    set { m_thumbsize = Maths.Clamp(value, 0.05, 1.0); Invalidate(); }
		//}

		///// <summary>The size of the thumb in control space</summary>
		//private int ThumbSizeCS
		//{
		//    get { return (int)(m_thumbsize * Height); }
		//}

		///// <summary>The normalised position of the thumb</summary>
		//[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("The normalised position of the thumb")]
		//public double Fraction
		//{
		//    get { return m_frac; }
		//    set { m_frac = Maths.Clamp(value, 0.0, 1.0); RaiseValueChanged(); Invalidate(); }
		//}

		//var thumb_rect = new Rectangle(bounds.X, bounds.Y + thm_centre - thm_hheight, bounds.Width, 2 * thm_hheight); thumb_rect.Inflate(-2,0);

		//[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("Minimum")]
		//public int Minimum
		//{
		//    get { return m_minimum; }
		//    set { m_minimum = value; Value = Value; Invalidate(); }
		//}

		//[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("Maximum")]
		//public int Maximum
		//{
		//    get { return m_maximum; }
		//    set { m_maximum = value; Value = Value; Invalidate(); }
		//}

		//[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behavior"), Description("Value")]
		//public int Value
		//{
		//    get { return (int)Maths.Lerp(Minimum, Maximum, (float)Fraction); }
		//    set { Fraction = Maths.Ratio(Minimum, value, Maximum); RaiseValueChanged(); Invalidate(); }
		//}
