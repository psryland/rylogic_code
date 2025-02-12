﻿using System;
using System.Collections.Generic;
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
	/// <summary>
	/// A vertical scroll bar replacement that shows a sub range within a larger range.
	/// This class cannot inherit from VScrollBar because the OS does fancy stuff rendering it</summary>
	public sealed class SubRangeScroll :Control
	{
		private const float m_corner_radius = 4f;
		private bool m_dragging = false;

		public interface ISubRange
		{
			RangeI Range { get; }
			Color Color { get; }
		}
		private class SubRange :ISubRange
		{
			public Rectangle m_rect;

			public RangeI Range { get; set; }
			public Color Color { get; set; }

			public SubRange(RangeI range, Color color) { Range = range; Color = color; }
		}

		/// <summary>The total size of the represented range</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The total size of the represented range")]
		public RangeI TotalRange
		{
			get { return m_total_range; }
			set
			{
				if (Equals(m_total_range, value)) return;
				if (value.Size <= 0) value.End = value.Beg + 1;
				m_total_range = value;
				m_thumb_range = RangeI.Constrain(m_thumb_range, m_total_range);
				foreach (var r in m_indicator_ranges) r.Range = RangeI.Constrain(r.Range, m_total_range);
				Invalidate();
			}
		}
		private RangeI m_total_range;

		/// <summary>The range of the thumb relative to TotalRange</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The size of the thumb within TotalRange")]
		public RangeI ThumbRange
		{
			get { return m_thumb_range; }
			set
			{
				if (Equals(m_thumb_range, value)) return;
				if (value.Size <= 0) value.End = value.Beg + 1;
				m_thumb_range = RangeI.Constrain(value, m_total_range);
				RaiseScrollEvent();
				Invalidate();
			}
		}
		private RangeI m_thumb_range;

		/// <summary>The minimum size to let the thumb get</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The minimum size to let the thumb get")]
		public int MinThumbSize { get; set; }

		/// <summary>The colour of the track</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The colour of the track")]
		public Color TrackColor { get; set; }

		/// <summary>The colour of the thumb</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The colour of the thumb")]
		public Color ThumbColor { get; set; }

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

		/// <summary>The ranges to draw on the scroll bar</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), Category("Behaviour"), Description("The indicator ranges to draw on the scroll bar")]
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
			TotalRange   = new RangeI(0, 100);
			ThumbRange   = new RangeI(35, 50);
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
			Region = new Region(Gdi.RoundedRectanglePath(ClientRectangle, m_corner_radius)); // Set the clip boundary
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.PageUp  )          { RangeI thm = ThumbRange; thm = thm.Shift(-m_large_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.PageDown)          { RangeI thm = ThumbRange; thm = thm.Shift(+m_large_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Up   && e.Control) { RangeI thm = ThumbRange; thm = thm.Shift(-m_small_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Down && e.Control) { RangeI thm = ThumbRange; thm = thm.Shift(+m_small_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
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
				int sy = (int)(Math_.Frac(total.Beg, r.Range.Beg, total.End) * bounds.Height);
				int ey = (int)(Math_.Frac(total.Beg, r.Range.End, total.End) * bounds.Height);
				r.m_rect = new Rectangle(bounds.X, bounds.Y + sy, bounds.Width, Math.Max(ey - sy, 1));
			}

			// Background
			c0 = Gfx_.Lerp(Color.Black, TrackColor, 0.8);
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
				c1 = Gfx_.Lerp(c0, Color.FromArgb(c0.A, Color.White), 0.2);
				using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
					gfx.FillRectangle(bsh, r.m_rect);
			}

			// Thumb
			{
				var r = MakeThumbRect(bounds);
				c0 = Gfx_.Lerp(Color.White, Color.FromArgb(0x80, ThumbColor), 0.8);
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
		}

		/// <summary>Reset the collection of indicator ranges</summary>
		public void ClearIndicatorRanges()
		{
			m_indicator_ranges.Clear();
		}

		/// <summary>Add an indicator range</summary>
		public void AddIndicatorRange(RangeI range, Color colour)
		{
			System.Diagnostics.Debug.Assert(TotalRange.Contains(range), "Indicator range outside total range");
			range = RangeI.Constrain(range, TotalRange);
			m_indicator_ranges.Add(new SubRange(range, colour));
		}

		/// <summary>Set the thumb position given a control space Y value</summary>
		private void ScrollThumbPos(int y)
		{
			RangeI thm = ThumbRange;
			thm.Mid = TotalRange.Beg + (long)(Math_.Frac(0, y, Height) * TotalRange.Size);
			ThumbRange = thm;
		}

		/// <summary>Create the rectangle describing the thumb position</summary>
		private Rectangle MakeThumbRect(Rectangle bounds)
		{
			var height = bounds.Height;
			var thm    = ThumbRange;
			var total  = TotalRange;

			int sy = (int)(Math_.Frac(total.Beg, thm.Beg, total.End) * bounds.Height);
			int ey = (int)(Math_.Frac(total.Beg, thm.End, total.End) * bounds.Height);
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
