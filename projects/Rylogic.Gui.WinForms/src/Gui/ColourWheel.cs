using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WinForms
{
	public class ColourWheel :UserControl
	{
		private const string ValueLabel = "V";
		private const string AlphaLabel = "A";

		public ColourWheel()
		{
			InitializeComponent();
			m_hsv_colour      = new HSV(1f, 0f, 0f, 1f);
			m_parts           = EParts.All;
			m_slider_width    = 20;
			m_vertical_layout = false;
			m_measurements    = null;

			SetStyle(
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.AllPaintingInWmPaint|
				ControlStyles.ResizeRedraw |
				ControlStyles.Selectable |
				ControlStyles.UserPaint, true);
		}

		/// <summary>The currently selected colour (RGB)</summary>
		public Color Colour
		{
			get { return HSVColour.ToColor(); }
			set { HSVColour = HSV.FromColor(value, HSVColour.H); }
		}

		/// <summary>The currently selected colour (HSV)</summary>
		public HSV HSVColour
		{
			get { return m_hsv_colour; }
			set
			{
				if (Equals(m_hsv_colour,value)) return;
				m_hsv_colour = value;
				Invalidate();
				RaiseColourChanged();
			}
		}
		private HSV m_hsv_colour;

		/// <summary>The parts of the control to draw</summary>
		[Editor(typeof(FlagEnumUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public EParts Parts
		{
			get { return m_parts; }
			set
			{
				if (Equals(m_parts, value)) return;
				m_parts = value;
				m_measurements = null;
				Invalidate();
			}
		}
		private EParts m_parts;
		[Flags] public enum EParts
		{
			None            = 0,
			Wheel           = 1 << 0,
			VSlider         = 1 << 1,
			ASlider         = 1 << 2,
			ColourSelection = 1 << 3,
			VSelection      = 1 << 4,
			ASelection      = 1 << 5,
			All             =  Wheel | VSlider | ASlider | ColourSelection | VSelection | ASelection,
		}

		/// <summary>The width of the slider bars</summary>
		public int SliderWidth
		{
			get { return m_slider_width; }
			set
			{
				if (Equals(m_slider_width, value)) return;
				m_slider_width = value;
				m_measurements = null;
				Invalidate();
			}
		}
		private int m_slider_width;

		/// <summary>True to draw the slider bars to the right, false to draw below</summary>
		public bool VerticalLayout
		{
			get { return m_vertical_layout; }
			set
			{
				if (Equals(m_vertical_layout, value)) return;
				m_vertical_layout = value;
				m_measurements = null;
				Invalidate();
			}
		}
		private bool m_vertical_layout;

		/// <summary>The layout of the control at the current size</summary>
		public Measurements LayoutDimensions
		{
			get { return m_measurements ?? (m_measurements =  Measure(ClientSize.Width, ClientSize.Height, Parts, SliderWidth, VerticalLayout)); }
		}
		private Measurements m_measurements;
		public class Measurements
		{
			public bool Empty { get { return Width == 0 || Height == 0; } }
			public int Width;   // The total width required for the control
			public int Height;  // The total height required for the control

			public float Radius;     // The radius of the colour wheel
			public Point Centre;     // The centre of the colour wheel (clamped to a pixel)
			public Rectangle Wheel;   // The square area required for the wheel

			public Rectangle VLabel;  // The area of the 'V' label
			public Rectangle VSlider; // The area covered by the VSlider

			public Rectangle ALabel;  // The area of the 'A' label
			public Rectangle ASlider; // The area covered by the ASlider
		}

		/// <summary>An event raised whenever the selected colour is changed</summary>
		public event EventHandler<ColourEventArgs> ColourChanged;
		public class ColourEventArgs :EventArgs
		{
			/// <summary>The selected colour</summary>
			public Color Colour { get; private set; }
			public ColourEventArgs(Color colour)
			{
				Colour = colour;
			}
		}
		private void RaiseColourChanged()
		{
			ColourChanged?.Invoke(this, new ColourEventArgs(Colour));
		}

		/// <summary>
		/// An event raised whenever the act of selecting a colour begins or ends.
		/// Use the button state to tell. Occurs before the first colour change, or after the last colour change</summary>
		public event MouseEventHandler ColourSelection;
		private void RaiseColourSelection(MouseEventArgs args)
		{
			if (ColourSelection == null) return;
			ColourSelection(this, args);
		}

		/// <summary>Returns the measurements of a ColourWheel that fits in the given width/height/orientation</summary>
		public static Measurements Measure(int width, int height, EParts parts, int slider_width, bool vertical)
		{
			bool drawv = (parts & EParts.VSlider) != 0;
			bool drawa = (parts & EParts.ASlider) != 0;
			int slider_space = 0 + (drawv ? slider_width * 3/2 : 0) + (drawa ? slider_width*3/2 : 0);

			int w = Math.Max(0, width  - (vertical ? 0 : slider_space));
			int h = Math.Max(0, height - (vertical ? slider_space : 0));
			int min = Math.Min(w, h);

			var meas = new Measurements();
			meas.Width  = min + (vertical ? 0 : slider_space);
			meas.Height = min + (vertical ? slider_space : 0);

			meas.Radius = min * 0.5f;
			meas.Centre = new Point((int)Math.Ceiling(meas.Radius), (int)Math.Ceiling(meas.Radius));
			meas.Wheel   = new Rectangle(0, 0, meas.Centre.X * 2, meas.Centre.Y * 2);

			using (var bm = new Bitmap(1,1))
			using (var gfx = Graphics.FromImage(bm))
			{
				gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
				Func<Graphics,int,int,string,Rectangle> LabelBounds = (g,x,y,label) =>
					{
						var sz = g.MeasureString(label, SystemFonts.DefaultFont, slider_width / 2);
						return new Rectangle(
							(int)(x + (vertical ? 0f : (slider_width - sz.Width) * 0.5f)),
							(int)(y + (vertical ? (slider_width - sz.Height) * 0.5f : 0f)),
							(int)Math.Ceiling(sz.Width),
							(int)Math.Ceiling(sz.Height));
					};
				Func<int,int,Rectangle,Rectangle> SliderBounds = (x,y,label_bounds) =>
					{
						return new Rectangle(
							x + (vertical ? label_bounds.Width + 2 : 0),
							y + (vertical ? 0 : label_bounds.Height + 2),
							vertical ? meas.Width - label_bounds.Width - 2 : slider_width,
							vertical ? slider_width : meas.Height - label_bounds.Height - 2);
					};

				var ofs = (vertical ? meas.Wheel.Width : meas.Wheel.Height) + slider_width / 5;
				if ((parts & EParts.VSlider) != 0)
				{
					meas.VLabel  = LabelBounds(gfx, vertical ? 0 : ofs, vertical ? ofs : 0, ValueLabel);
					meas.VSlider = SliderBounds(vertical ? 0 : ofs, vertical ? ofs : 0, meas.VLabel);
					ofs += slider_width * 3 / 2;
				}
				if ((parts & EParts.ASlider) != 0)
				{
					meas.ALabel  = LabelBounds(gfx, vertical ? 0 : ofs, vertical ? ofs : 0, AlphaLabel);
					meas.ASlider = SliderBounds(vertical ? 0 : ofs, vertical ? ofs : 0, meas.ALabel);
				}
				return meas;
			}
		}

		/// <summary>Wheel point based on the current dimensions</summary>
		private PointF WheelPoint(HSV colour)
		{
			var dim = LayoutDimensions;
			return new PointF(
				dim.Centre.X + (float)(dim.Radius * colour.S * Math.Cos(Math_.Tau * colour.H)),
				dim.Centre.Y + (float)(dim.Radius * colour.S * Math.Sin(Math_.Tau * colour.H)));
		}

		/// <summary>Set the hue and saturation based on point X,Y relative to the wheel centre</summary>
		private void SelectHueAndSaturation(int x, int y)
		{
			var dim = LayoutDimensions;
			if (dim.Empty) return;
			HSVColour = HSV.FromRadial((x - dim.Centre.X) / dim.Radius, (y - dim.Centre.Y) / dim.Radius, HSVColour.V, HSVColour.A);
		}

		/// <summary>Set the brightness based on point x,y relative to the VSlider</summary>
		private void SelectBrightness(int x, int y)
		{
			var dim = LayoutDimensions;
			if (dim.Empty || dim.VSlider.IsEmpty) return;
			
			var v = VerticalLayout
				? Math_.Clamp(Math_.Frac(dim.VSlider.Left, x, dim.VSlider.Right), 0.0, 1.0)
				: Math_.Clamp(Math_.Frac(dim.VSlider.Bottom, y, dim.VSlider.Top), 0.0, 1.0);

			HSVColour = HSV.FromAHSV(HSVColour.A, HSVColour.H, HSVColour.S, (float)v);
		}

		/// <summary>Set the alpha based on point x,y relative to the VSlider</summary>
		private void SelectAlpha(int x, int y)
		{
			var dim = LayoutDimensions;
			if (dim.Empty || dim.ASlider.IsEmpty) return;

			var a = VerticalLayout
				? Math_.Clamp(Math_.Frac(dim.ASlider.Left, x, dim.ASlider.Right), 0.0, 1.0)
				: Math_.Clamp(Math_.Frac(dim.ASlider.Bottom, y, dim.ASlider.Top), 0.0, 1.0);

			HSVColour = HSV.FromAHSV((float)a, HSVColour.H, HSVColour.S, HSVColour.V);
		}

		/// <summary>Get/Create the bitmap of the colour wheel</summary>
		private Bitmap WheelBitmap
		{
			get
			{
				var dim = LayoutDimensions;
				System.Diagnostics.Debug.Assert(!dim.Empty);

				// Cached version already the right size? return it
				if (m_wheel != null && m_wheel.Size == dim.Wheel.Size)
					return m_wheel;

				// Allocate the bitmap, if needed
				if (m_wheel != null) m_wheel.Dispose(); // Release the old bitmap
				m_wheel = new Bitmap(dim.Wheel.Width, dim.Wheel.Height, PixelFormat.Format32bppArgb);

				// Silly radius? ignore
				if (dim.Radius < 10f)
					return m_wheel;

				// Generate the points and colours for the wheel
				const int ColourCount = 60;
				var points = new PointF[ColourCount];
				var colours = new Color[ColourCount];
				for (int i = 0; i != ColourCount; ++i)
				{
					points[i] = new PointF(
						(float)(dim.Centre.X + dim.Radius * Math.Cos(i * Math_.Tau / ColourCount)),
						(float)(dim.Centre.Y + dim.Radius * Math.Sin(i * Math_.Tau / ColourCount)));
				
					colours[i] = HSV.ToColour32(1f, (float)i / ColourCount, 1f, 1f);
				}

				using (var pgb = new PathGradientBrush(points))
				{
					// Set the various properties. Note the SurroundColors property, which contains an array of points, 
					// in a one-to-one relationship with the points that created the gradient.
					pgb.CenterColor = Color.White;
					pgb.CenterPoint = dim.Centre;
					pgb.SurroundColors = colours;

					using (var gfx = Graphics.FromImage(m_wheel))
						gfx.FillEllipse(pgb, dim.Wheel);
				}
				return m_wheel;
			}
		}
		private Bitmap m_wheel; // Cached wheel bitmap

		/// <summary>Returns the part hit at location x,y</summary>
		private EParts PartHitTest(int x, int y)
		{
			var dim = LayoutDimensions;
			if (dim.Empty) return EParts.None;
			if (dim.Wheel.Contains(x,y)) return EParts.Wheel;
			if (dim.VSlider.Contains(x,y)) return EParts.VSlider;
			if (dim.ASlider.Contains(x,y)) return EParts.ASlider;
			return EParts.None;
		}

		/// <summary>The hit component during mouse down</summary>
		private EParts m_selected_part;
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if ((m_selected_part = PartHitTest(e.X, e.Y)) == EParts.None) return;
			RaiseColourSelection(e);
			switch (m_selected_part)
			{
			case EParts.Wheel:   SelectHueAndSaturation(e.X, e.Y); break;
			case EParts.VSlider: SelectBrightness(e.X, e.Y); break;
			case EParts.ASlider: SelectAlpha(e.X, e.Y); break;
			}
			Capture = true;
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			switch (m_selected_part)
			{
			case EParts.Wheel:   SelectHueAndSaturation(e.X, e.Y); break;
			case EParts.VSlider: SelectBrightness(e.X, e.Y); break;
			case EParts.ASlider: SelectAlpha(e.X, e.Y); break;
			}
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			Capture = false;
			switch (m_selected_part)
			{
			case EParts.Wheel:   SelectHueAndSaturation(e.X, e.Y); break;
			case EParts.VSlider: SelectBrightness(e.X, e.Y); break;
			case EParts.ASlider: SelectAlpha(e.X, e.Y); break;
			}
			RaiseColourSelection(e);
			m_selected_part = EParts.None;
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			m_measurements = null;
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			var gfx = e.Graphics;
			var dim = LayoutDimensions;
			if (dim.Empty)
			{
				base.OnPaint(e);
				return;
			}

			// Draw the wheel
			if ((Parts & EParts.Wheel) != 0)
			{
				var bm = WheelBitmap;
				gfx.DrawImageUnscaled(bm, 0, 0);

				// Draw the colour selection
				if ((Parts & EParts.ColourSelection) != 0)
				{
					var pt = WheelPoint(HSVColour);
					gfx.DrawEllipse(Pens.Black, pt.X - 2f, pt.Y - 2f, 4f, 4f);
				}
			}

			gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

			// Draw the VSlider
			if ((Parts & EParts.VSlider) != 0)
			{
				gfx.DrawString(ValueLabel, SystemFonts.DefaultFont, Brushes.Black, dim.VLabel);
				if (!dim.VSlider.IsEmpty)
				{
					using (var b = new LinearGradientBrush(
						VerticalLayout ? dim.VSlider.LeftCentre()  : dim.VSlider.BottomCentre(),
						VerticalLayout ? dim.VSlider.RightCentre() : dim.VSlider.TopCentre()   ,
						Color.Black, Color.White))
						gfx.FillRectangle(b, dim.VSlider);
					
					gfx.DrawRectangle(Pens.Black, dim.VSlider);
				}
				
				// Draw the brightness selection
				if ((Parts & EParts.VSelection) != 0)
				{
					var v = VerticalLayout
						? Math_.Lerp(dim.VSlider.Left, dim.VSlider.Right, (double)HSVColour.V)
						: Math_.Lerp(dim.VSlider.Bottom, dim.VSlider.Top, (double)HSVColour.V);
					var pts = SliderSelector(v, dim.VSlider, VerticalLayout);
					gfx.DrawLines(Pens.Black, pts);
				}
			}

			// Draw the ASlider
			if ((Parts & EParts.ASlider) != 0)
			{
				gfx.DrawString(AlphaLabel, SystemFonts.DefaultFont, Brushes.Black, dim.ALabel);
				if (!dim.ASlider.IsEmpty)
				{
					using (var b = new LinearGradientBrush(
						VerticalLayout ? dim.ASlider.LeftCentre()  : dim.ASlider.BottomCentre(),
						VerticalLayout ? dim.ASlider.RightCentre() : dim.ASlider.TopCentre()   ,
						Color.Black, Color.White))
						gfx.FillRectangle(b, dim.ASlider);

					gfx.DrawRectangle(Pens.Black, dim.ASlider);
				}

				// Draw the alpha selection
				if ((Parts & EParts.ASelection) != 0)
				{
					var v = VerticalLayout
						? Math_.Lerp(dim.ASlider.Left, dim.ASlider.Right, (double)HSVColour.A)
						: Math_.Lerp(dim.ASlider.Bottom, dim.ASlider.Top, (double)HSVColour.A);
					var pts = SliderSelector(v, dim.ASlider, VerticalLayout);
					gfx.DrawLines(Pens.Black, pts);
				}
			}
		}

		/// <summary>Return arrays of points for a given value along a slider</summary>
		private static PointF[] SliderSelector(float v, RectangleF rect, bool vertical)
		{
			return vertical
				? new[]
					{
						new PointF(v    , rect.Top       ),
						new PointF(v - 2, rect.Top    - 2),
						new PointF(v + 2, rect.Top    - 2),
						new PointF(v    , rect.Top       ),
						new PointF(v    , rect.Bottom    ),
						new PointF(v - 2, rect.Bottom + 2),
						new PointF(v + 2, rect.Bottom + 2),
						new PointF(v    , rect.Bottom    )
					}
				: new[]
					{
						new PointF(rect.Left     , v    ),
						new PointF(rect.Left  - 2, v - 2),
						new PointF(rect.Left  - 2, v + 2),
						new PointF(rect.Left     , v    ),
						new PointF(rect.Right    , v    ),
						new PointF(rect.Right + 2, v - 2),
						new PointF(rect.Right + 2, v + 2),
						new PointF(rect.Right    , v    )
					};
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (m_wheel != null) { m_wheel.Dispose(); m_wheel = null; }
			if (disposing && (components != null)) components.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// ColourWheel
			// 
			this.Name = "ColourWheel";
			this.Size = new System.Drawing.Size(144, 142);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
