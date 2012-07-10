using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using pr.common;
using pr.gfx;
using pr.maths;

namespace RyLogViewer
{
	/// <summary>
	/// A vertical scroll bar replacement that shows position within the current file.
	/// This class cannot inherit from VScrollBar because the OS does fancy stuff rendering it</summary>
	public sealed class SubRangeScroll :Control
	{
		public class SubRange
		{
			private Range m_range;
			private Color m_color;
			internal Rectangle m_rect;
			
			public Range Range
			{
				get { return m_range; }
				set { Debug.Assert(value.Count >= 0); m_range = value; }
			}
			public Color Color
			{
				get { return m_color; }
				set { m_color = value; }
			}
			
			public SubRange() {}
			public SubRange(Range range, Color color) { Range = range; Color = color; }
		}
		private readonly List<SubRange> m_ranges;         // Sub ranges to draw within the scroll bar
		private long                    m_large_change;   // Large change amount
		private long                    m_small_change;   // Small change amount
		private Range                   m_total_range;    // The total range size
		private Range                   m_thumb_range;    // The range of the thumb relative to total range
		private bool                    m_dragging;       // True while dragging

		public int MinThumbSize { get; set; }
			
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("TrackColor")]
		public Color TrackColor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("ThumbColor")]
		public Color ThumbColor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("LargeChange")]
		public long LargeChange
		{
			get { return m_large_change; }
			set { m_large_change = Maths.Clamp(value, 1, TotalRange.Count); Invalidate(); }
		}

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("SmallChange")]
		public long SmallChange
		{
			get { return m_small_change; }
			set { m_small_change = Maths.Clamp(value, 1, TotalRange.Count); Invalidate(); }
		}

		/// <summary>The ranges to draw on the scroll bar</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The indicator ranges to draw on the scroll bar")]
		public List<SubRange> Ranges
		{
			get { return m_ranges; }
		}

		/// <summary>The total size of the represented range</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The total size of the represented range")]
		public Range TotalRange
		{
			get { return m_total_range; }
			set
			{
				m_total_range = value;
				ThumbRange = TotalRange.Intersect(ThumbRange);
				foreach (var r in m_ranges) r.Range = TotalRange.Intersect(r.Range);
				Invalidate();
			}
		}

		/// <summary>The range of the thumb relative to TotalRange</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The size of the thumb bar within TotalRange")]
		public Range ThumbRange
		{
			get { return m_thumb_range; }
			set
			{
				m_thumb_range = value;
				var total = TotalRange;
				if (m_thumb_range.Count > total.Count) m_thumb_range = total;
				if (m_thumb_range.End   > total.End  ) m_thumb_range.Last  = total.Last;
				if (m_thumb_range.Begin < total.Begin) m_thumb_range.First = total.First;
				RaiseValueChanged();
				Invalidate();
			}
		}

		/// <summary>Raised whenever 'ThumbRange' is changed.</summary>
		public event EventHandler ValueChanged;
		private void RaiseValueChanged()
		{
			if (ValueChanged != null)
				ValueChanged(this, EventArgs.Empty);
		}

		/// <summary>Raised whenever the scroll box is moved by either a mouse or keyboard action immediately after the value is updated</summary>
		public event EventHandler Scroll;
		private void RaiseScrollEvent()
		{
			if (Scroll != null)
				Scroll(this, EventArgs.Empty);
		}
		
		/// <summary>Raised whenever a scroll finishes, immediately after the 'Scroll' event which is after the ValueChanged event</summary>
		public event EventHandler ScrollEnd;
		private void RaiseScrollEndEvent()
		{
			if (ScrollEnd != null)
				ScrollEnd(this, EventArgs.Empty);
		}

		public SubRangeScroll()
		{
			m_ranges     = new List<SubRange>();
			m_dragging   = false;
			MinThumbSize = 20;
			TrackColor   = SystemColors.ControlDark;
			ThumbColor   = SystemColors.Window;
			TotalRange   = new Range(0, 100);
			ThumbRange   = new Range(35, 50);
			SetStyle(
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint|
				ControlStyles.ResizeRedraw |
				ControlStyles.Selectable |
				ControlStyles.UserPaint, true);
		}
		
		private Rectangle MakeThumbRect(Rectangle bounds)
		{
			var height = bounds.Height;
			var thm   = ThumbRange;
			var total = TotalRange;
			
			int top  = (int)(Maths.Ratio(0, thm.Begin, total.Count) * Height);
			int hite = (int)(Maths.Ratio(0, thm.Count  , total.Count) * Height);
			var thumb_rect = new Rectangle(bounds.X, bounds.Y + top, bounds.Width, Math.Max(1,hite));
			
			// Fix up thumbs that are too small
			if (thumb_rect.Height < MinThumbSize)
			{
				int diff = MinThumbSize - thumb_rect.Height;
				thumb_rect.Height += diff;
				thumb_rect.Y      -= diff / 2;
				if (thumb_rect.Top    <      0) thumb_rect.Y = 0;
				if (thumb_rect.Bottom > height) thumb_rect.Y = height - thumb_rect.Height;
			}
			return thumb_rect;
		}

		/// <summary>Paint the scrollbar</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			const float rad = 4f;
			var gfx = e.Graphics;
			var bounds = e.ClipRectangle; bounds.Inflate(-1,0);
			var height = bounds.Height;
			var total = TotalRange;
			
			// Rectum?
			var back_rect = bounds;
			var thumb_rect = MakeThumbRect(bounds);
			foreach (var r in Ranges)
			{
				int top  = (int)(Maths.Ratio(0, r.Range.Begin, total.Count) * height);
				int hite = (int)(Math.Max(1, Maths.Ratio(0, r.Range.Count  , total.Count) * height));
				r.m_rect = new Rectangle(bounds.X, bounds.Y + top, bounds.Width, Math.Max(1,hite));
				r.m_rect .Inflate(-2,0);
			}
			
			Color c0,c1;
			Point pt0 = new Point(bounds.Left, 0);
			Point pt1 = new Point(bounds.Right, 0);
			
			gfx.SmoothingMode = SmoothingMode.AntiAlias;
			
			// Background
			c0 = TrackColor;
			c1 = Gfx.Blend(TrackColor, Color.White, 0.2f);
			using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
				gfx.FillRectangleRounded(bsh, back_rect, rad);
			
			// Thumb background
			c0 = ThumbColor;
			c1 = Gfx.Blend(ThumbColor, Color.Black, 0.2f);
			using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
				gfx.FillRectangleRounded(bsh, thumb_rect, rad);
			
			foreach (var r in Ranges)
			{
				c0 = r.Color;
				c1 = Gfx.Blend(c0, Color.FromArgb(c0.A, Color.White), 0.2f);
				using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
					gfx.FillRectangle(bsh, r.m_rect);
			}
			
			// Borders
			gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, back_rect, rad);
			//using (var pen = new Pen(Color.FromArgb(255, VisibleRangeColor)))
			//    gfx.DrawRectangle(pen, vis_rect);
			//using (var pen = new Pen(Color.FromArgb(255, SelectedRangeColor)))
			//    gfx.DrawRectangle(pen, sel_rect);
			gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, thumb_rect, rad);
		}

		/// <summary>Set the thumb position given a control space Y value</summary>
		private void ScrollThumbPos(int y)
		{
			Range thm = ThumbRange;
			thm.Mid = (long)(Maths.Ratio(0, y, Height) * TotalRange.Count);
			ThumbRange = thm;
			RaiseScrollEvent();
		}

		/// <summary>Mouse down, tries to centre the thumb at the mouse location</summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			ScrollThumbPos(e.Y);
			Capture = true;
			m_dragging = true;
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (m_dragging) ScrollThumbPos(e.Y);
			
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			ScrollThumbPos(e.Y);
			RaiseScrollEndEvent();
			Capture = false;
			m_dragging = false;
		}
		
		/// <summary>Keydown scrolling</summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.PageUp  )          { Range thm = ThumbRange; thm.Shift(-m_large_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.PageDown)          { Range thm = ThumbRange; thm.Shift(+m_large_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Up   && e.Control) { Range thm = ThumbRange; thm.Shift(-m_small_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Down && e.Control) { Range thm = ThumbRange; thm.Shift(+m_small_change); ThumbRange = thm; RaiseScrollEvent(); RaiseScrollEndEvent(); }
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
		//[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The normalised position of the thumb")]
		//public double Fraction
		//{
		//    get { return m_frac; }
		//    set { m_frac = Maths.Clamp(value, 0.0, 1.0); RaiseValueChanged(); Invalidate(); }
		//}
		
		//var thumb_rect = new Rectangle(bounds.X, bounds.Y + thm_centre - thm_hheight, bounds.Width, 2 * thm_hheight); thumb_rect.Inflate(-2,0);


		
		//[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Minimum")]
		//public int Minimum
		//{
		//    get { return m_minimum; }
		//    set { m_minimum = value; Value = Value; Invalidate(); }
		//}

		//[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Maximum")]
		//public int Maximum
		//{
		//    get { return m_maximum; }
		//    set { m_maximum = value; Value = Value; Invalidate(); }
		//}

		//[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Value")]
		//public int Value
		//{
		//    get { return (int)Maths.Lerp(Minimum, Maximum, (float)Fraction); }
		//    set { Fraction = Maths.Ratio(Minimum, value, Maximum); RaiseValueChanged(); Invalidate(); }
		//}

			