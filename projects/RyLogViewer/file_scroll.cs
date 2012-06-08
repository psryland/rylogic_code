using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using pr.gfx;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	/// <summary>
	/// A vertical scroll bar replacement that shows position within the current file.
	/// This class cannot inherit from VScrollBar because the OS does fancy stuff rendering it</summary>
	public sealed class SubRangeScroll :Control
	{
		private int    m_minimum;       // Integer range that the
		private int    m_maximum;       //  normalised values apply to.
		private float  m_large_change;  // Normalised large change amount
		private float  m_small_change;  // Normalised small change amount
		private long   m_sub_range;     // The sub range size
		private long   m_total_range;   // The total range size
		private float  m_frac;          // The normalised position of the thumb
		private bool   m_dragging;      // True while dragging

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("TrackColor")]
		public Color TrackColor { get; set; }

		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("ThumbColor")]
		public Color ThumbColor { get; set; }

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
			get { return (int)Maths.Lerp(Minimum, Maximum, Fraction); }
			set { Fraction = Maths.Ratio(Minimum, value, Maximum); RaiseValueChanged(); Invalidate(); }
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
			set { m_total_range = value; SubRange = SubRange; Invalidate(); }
		}
		
		/// <summary>Returns the fractional size of SubRange to TotalRange</summary>
		public float RangeRatio
		{
			get { return 1f * SubRange / TotalRange; }
		}

		/// <summary>The normalised position of the thumb</summary>
		[EditorBrowsable(EditorBrowsableState.Always), Browsable(true), DefaultValue(false), Category("Behavior"), Description("The normalised position of the thumb")]
		public float Fraction
		{
			get { return m_frac; }
			set { m_frac = Maths.Clamp(value, 0f, 1f); RaiseScrollEvent(); Invalidate(); }
		}
		
		/// <summary>Raised whenever the scroll box is moved by either a mouse or keyboard action and before the value is updated</summary>
		public event EventHandler Scroll;
		private void RaiseScrollEvent()
		{
			if (Scroll != null)
				Scroll(this, EventArgs.Empty);
		}
		
		public event EventHandler ValueChanged;
		private void RaiseValueChanged()
		{
			if (ValueChanged != null)
				ValueChanged(this, EventArgs.Empty);
		}

		public SubRangeScroll()
		{
			m_dragging = false;
			TrackColor = SystemColors.ControlDark;
			ThumbColor = SystemColors.Window;
			TotalRange = 100;
			SubRange   = 25;
			Fraction   = 0.5f;
			
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
			int thm_centre  = (int)Maths.Lerp(thm_hheight, Height - thm_hheight, Fraction);
			
			gfx.SmoothingMode = SmoothingMode.AntiAlias;
			
			// Background
			var back_rect = bounds;
			back_rect.Inflate(-1,-1);
			using (var bsh = new LinearGradientBrush(new Point(bounds.Left, 0), new Point(bounds.Right, 0), TrackColor, Gfx.Blend(TrackColor, Color.White, 0.2f)))
				gfx.FillRectangleRounded(bsh, back_rect, rad);
			gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, back_rect, rad);
			
			// Thumb
			var thumb_rect = new Rectangle(bounds.X, bounds.Y + thm_centre - thm_hheight, bounds.Width, 2 * thm_hheight);
			thumb_rect.Inflate(-2,-2);
			using (var bsh = new LinearGradientBrush(new Point(bounds.Left, 0), new Point(bounds.Right, 0), ThumbColor, Gfx.Blend(ThumbColor, Color.Black, 0.2f)))
				gfx.FillRectangleRounded(bsh, thumb_rect, rad);
			gfx.DrawRectangleRounded(SystemPens.ControlDarkDark, thumb_rect, rad);
		}

		/// <summary>The size of the thumb in control space</summary>
		public int ThumbSize
		{
			get { return (int)(RangeRatio * Height); }
		}

		/// <summary>Set the thumb position given a control space Y value</summary>
		private void SetThumbPos(int y)
		{
			int h = ThumbSize / 2;
			y = Maths.Clamp(y, h, Height - h);
			Fraction = Maths.Ratio(h, y, Height - h);
		}

		/// <summary>Mouse down, tries to centre the thumb at the mouse location</summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			SetThumbPos(e.Y);
			Capture = true;
			m_dragging = true;
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (m_dragging) SetThumbPos(e.Y);
			
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			SetThumbPos(e.Y);
			Capture = false;
			m_dragging = false;
		}
		
		/// <summary>Keydown scrolling</summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.PageUp  ) Fraction -= m_large_change;
			if (e.KeyCode == Keys.PageDown) Fraction += m_large_change;
			if (e.KeyCode == Keys.Up   && e.Control) Fraction -= m_small_change;
			if (e.KeyCode == Keys.Down && e.Control) Fraction += m_small_change;
		}
	}
}
