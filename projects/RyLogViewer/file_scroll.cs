using System;
using System.ComponentModel;
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
		private int    m_minimum;        // Integer range that the
		private int    m_maximum;        //  normalised values apply to.
		private float  m_large_change;   // Normalised large change amount
		private float  m_small_change;   // Normalised small change amount
		private Range  m_visible_range;  // The portion of the bar to indicate as visible
		private Range  m_selected_range; // The portion of the bar to indicate as selected
		private long   m_sub_range;      // The sub range size
		private long   m_total_range;    // The total range size
		private double m_frac;           // The normalised position of the thumb
		private bool   m_dragging;       // True while dragging

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("TrackColor")]
		public Color TrackColor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("ThumbColor")]
		public Color ThumbColor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("VisibleRangeColor")]
		public Color VisibleRangeColor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("SelectedRangeColor")]
		public Color SelectedRangeColor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("LargeChange")]
		public int LargeChange
		{
			get { return (int)Maths.Lerp(Minimum, Maximum, m_large_change); }
			set { m_large_change = Maths.Clamp(Maths.Ratio(Minimum, value, Maximum), 0f, 1f); Invalidate(); }
		}

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("SmallChange")]
		public int SmallChange
		{
			get { return (int)Maths.Lerp(Minimum, Maximum, m_small_change); }
			set { m_small_change = Maths.Clamp(Maths.Ratio(Minimum, value, Maximum), 0f, 1f); Invalidate(); }
		}
		
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Minimum")]
		public int Minimum
		{
			get { return m_minimum; }
			set { m_minimum = value; Value = Value; Invalidate(); }
		}

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Maximum")]
		public int Maximum
		{
			get { return m_maximum; }
			set { m_maximum = value; Value = Value; Invalidate(); }
		}

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("Value")]
		public int Value
		{
			get { return (int)Maths.Lerp(Minimum, Maximum, (float)Fraction); }
			set { Fraction = Maths.Ratio(Minimum, value, Maximum); RaiseValueChanged(); Invalidate(); }
		}

		/// <summary>Return the current slider position in reference to TotalRange</summary>
		public long RangePos
		{
			get { return (long)(TotalRange * Fraction); }
		}

		/// <summary>The visible portion of the SubRange</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The size of the visible portion of the range")]
		public Range VisibleRange
		{
			get { return m_visible_range; }
			set { m_visible_range = new Range(0, TotalRange).Intersect(value); Invalidate(); }
		}
		
		/// <summary>The selected portion of the SubRange</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The size of the selected portion of the range")]
		public Range SelectedRange
		{
			get { return m_selected_range; }
			set { m_selected_range = new Range(0, TotalRange).Intersect(value); Invalidate(); }
		}

		/// <summary>The size of the visible portion of the range</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The size of the visible portion of the range")]
		public long SubRange
		{
			get { return m_sub_range; }
			set { m_sub_range = Maths.Clamp(value, 0, TotalRange); Invalidate(); }
		}
		
		/// <summary>The total size of the represented range</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The total size of the represented range")]
		public long TotalRange
		{
			get { return m_total_range; }
			set
			{
				m_total_range = value;
				SubRange = SubRange;
				VisibleRange = VisibleRange;
				SelectedRange = SelectedRange;
				Invalidate();
			}
		}
		
		/// <summary>Returns the fractional size of SubRange to TotalRange</summary>
		public float RangeRatio
		{
			get { return 1f * SubRange / TotalRange; }
		}

		/// <summary>The normalised position of the thumb</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The normalised position of the thumb")]
		public double Fraction
		{
			get { return m_frac; }
			set { m_frac = Maths.Clamp(value, 0.0, 1.0); RaiseValueChanged(); Invalidate(); }
		}
		
		/// <summary>Raised whenever 'Fraction' is changed.</summary>
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
			m_dragging = false;
			TrackColor = SystemColors.ControlDark;
			ThumbColor = SystemColors.Window;
			VisibleRangeColor = Color.FromArgb(128, Color.SteelBlue);
			SelectedRangeColor = Color.FromArgb(128, Color.DarkBlue);

			Fraction   = 0.5f;
			TotalRange = 100;
			SubRange   = 25;
			VisibleRange = new Range(10,50);
			SelectedRange = new Range(8,15);
			SetStyle(
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint|
				ControlStyles.ResizeRedraw |
				ControlStyles.Selectable |
				ControlStyles.UserPaint, true);
		}
		
		/// <summary>Paint the scrollbar</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			var gfx = e.Graphics;
			var bounds = e.ClipRectangle;
			
			const float rad = 4f;
			int thm_hheight = ThumbSize / 2;
			int thm_centre  = (int)Maths.Lerp(thm_hheight, Height - thm_hheight, (float)Fraction);
			int vis_top     = (int)(Maths.Ratio(0, m_visible_range.m_begin, TotalRange) * Height);
			int vis_height  = (int)(Maths.Ratio(0, m_visible_range.Count, TotalRange) * Height);
			int sel_top     = (int)(Maths.Ratio(0, m_selected_range.m_begin, TotalRange) * Height);
			int sel_height  = (int)(Maths.Ratio(0, m_selected_range.Count, TotalRange) * Height);
			
			// Rectum?
			var back_rect  = bounds; back_rect.Inflate(-1,-1);
			var thumb_rect = new Rectangle(bounds.X, bounds.Y + thm_centre - thm_hheight, bounds.Width, 2 * thm_hheight); thumb_rect.Inflate(-2,0);
			var vis_rect   = new Rectangle(bounds.X, bounds.Y + vis_top, bounds.Width, Math.Max(1,vis_height)); vis_rect.Inflate(-2,0);
			var sel_rect   = new Rectangle(bounds.X, bounds.Y + sel_top, bounds.Width, Math.Max(1,sel_height)); sel_rect.Inflate(-2,0);
			
			Color c0,c1;
			Point pt0 = new Point(bounds.Left, 0);
			Point pt1 = new Point(bounds.Right, 0);
			
			gfx.SmoothingMode = SmoothingMode.AntiAlias;
			
			// Background
			using (var bsh = new LinearGradientBrush(pt0, pt1, TrackColor, Gfx.Blend(TrackColor, Color.White, 0.2f)))
				gfx.FillRectangleRounded(bsh, back_rect, rad);
			
			// Thumb background
			using (var bsh = new LinearGradientBrush(pt0, pt1, ThumbColor, Gfx.Blend(ThumbColor, Color.Black, 0.2f)))
				gfx.FillRectangleRounded(bsh, thumb_rect, rad);
			
			// Visible range
			c0 = VisibleRangeColor;
			c1 = Gfx.Blend(c0, Color.FromArgb(c0.A, Color.White), 0.2f);
			using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
				gfx.FillRectangle(bsh, vis_rect);
			
			// Selected range
			c0 = SelectedRangeColor;
			c1 = Gfx.Blend(c0, Color.FromArgb(c0.A, Color.White), 0.2f);
			using (var bsh = new LinearGradientBrush(pt0, pt1, c0, c1))
				gfx.FillRectangle(bsh, sel_rect);

			// Borders
			gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, back_rect, rad);
			using (var pen = new Pen(Color.FromArgb(255, VisibleRangeColor)))
				gfx.DrawRectangle(pen, vis_rect);
			using (var pen = new Pen(Color.FromArgb(255, SelectedRangeColor)))
				gfx.DrawRectangle(pen, sel_rect);
			gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, thumb_rect, rad);
		}

		/// <summary>The size of the thumb in control space</summary>
		public int ThumbSize
		{
			get { return (int)(RangeRatio * Height); }
		}

		/// <summary>Set the thumb position given a control space Y value</summary>
		private void ScrollThumbPos(int y)
		{
			int h = ThumbSize / 2;
			y = Maths.Clamp(y, h, Height - h);
			Fraction = Maths.Ratio(h, y, Height - h);
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
			if (e.KeyCode == Keys.PageUp  )          { Fraction -= m_large_change; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.PageDown)          { Fraction += m_large_change; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Up   && e.Control) { Fraction -= m_small_change; RaiseScrollEvent(); RaiseScrollEndEvent(); }
			if (e.KeyCode == Keys.Down && e.Control) { Fraction += m_small_change; RaiseScrollEvent(); RaiseScrollEndEvent(); }
		}
	}
}
