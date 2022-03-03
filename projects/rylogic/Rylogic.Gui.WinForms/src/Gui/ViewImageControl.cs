using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Rylogic.Gfx;

namespace Rylogic.Gui.WinForms
{
	public class ViewImageControl :UserControl
	{
		/// <summary>Quality modes for rendering images</summary>
		[TypeConverter(typeof(QualityModes))]
		public class QualityModes :TypeConverter
		{
			public CompositingQuality CompositingQuality  {get;set;}
			public InterpolationMode  InterpolationMode   {get;set;}
			public SmoothingMode      SmoothingMode       {get;set;}
			public PixelOffsetMode    PixelOffsetMode     {get;set;}

			public QualityModes()
			{
				CompositingQuality = CompositingQuality.HighQuality;
				InterpolationMode  = InterpolationMode.NearestNeighbor;
				SmoothingMode      = SmoothingMode.None;
				PixelOffsetMode    = PixelOffsetMode.Half;
			}

			// TypeConverter methods. This type is a type converter so that it can be displayed in a property grid
			public override string ToString()                                               { return "Quality Modes"; }
			public override bool GetPropertiesSupported    (ITypeDescriptorContext context) { return true; }
			public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) { return true; }
			public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) { return TypeDescriptor.GetProperties(value ,attributes); }
			public override object CreateInstance(ITypeDescriptorContext context, IDictionary values)
			{
				QualityModes t = new QualityModes();
				foreach (var prop in typeof(QualityModes).GetProperties()) {if (values[prop.Name] != null) prop.SetValue(t, values[prop.Name], null);}
				return t;
			}
		}

		private Image          m_error_bitmap;          // The bitmap to display when no bitmap is set
		private Image          m_bitmap;                // The image we're displaying
		private Point          m_centre;                // The position of the centre of the view
		private float          m_zoom;                  // The ratio of the client rect to the bitmap
		private RotateFlipType m_rotation;              // The amount to rotate the image by
		private float          m_min_zoom;              // The minimum amount to zoom
		private float          m_max_zoom;              // The maximum amount to zoom
		private bool           m_drag_enabled;          // True if mouse dragging is enabled
		private Point          m_drag_start_mouse_pos;  // The mouse position where dragging started
		private Point          m_drag_start_view_pos;   // The view position when dragging started
		private bool           m_draw_image;            // True if the image should be drawn (allows overlays only to be drawn)

		/// <summary>Occurs when the 'Image' property is set</summary>
		public event Action<ViewImageControl> ImageChanged;
		
		/// <summary>Occurs when the image is zoomed</summary>
		public event Action<ViewImageControl> ImageZoomed;
		
		/// <summary>Occurs when the centre of the view is set to a position in the image</summary>
		public event Action<ViewImageControl> ImageMoved;

		/// <summary>Allows users to add overlays to the viewed image. 'image_area' is the portion of the source image that is visible. Users should draw within this area</summary>
		public event AddOverlaysEventHandler AddOverlaysEvent;
		public delegate void AddOverlaysEventHandler(ViewImageControl sender, Graphics gfx, Rectangle image_area, Rectangle screen_area);
		
		/// <summary>Allows users to control how the image is drawn to the control</summary>
		public OwnerPaintHandler OwnerPaint;
		public delegate void OwnerPaintHandler(ViewImageControl sender, PaintEventArgs args, Rectangle src_rect, Rectangle dst_rect);

		/// <summary>A timer to allow users to cause the image to be redrawn periodically</summary>
		public Timer RedrawTimer = new Timer{Enabled = false};

		/// <summary>Controls for the render quality of images</summary>
		public QualityModes Quality {get;set;}

		// Constructor
		public ViewImageControl()
		{
			AutoScaleMode = AutoScaleMode.Font;
			
			// ReSharper disable DoNotCallOverridableMethodsInConstructor
			DoubleBuffered = true;
			// ReSharper restore DoNotCallOverridableMethodsInConstructor

			ErrorImage = null;
			Image = ErrorImage;
			ImageVisible = true;
			EnableDragging = true;
			Rotation = RotateFlipType.RotateNoneFlipNone;
			RedrawTimer.Tick += (s,e)=>{ Refresh(); };
			Quality = new QualityModes();
			
			// Handle mouse zoom and dragging
			MouseWheel += OnMouseWheel;
			MouseDown  += OnMouseDown;
			MouseUp    += OnMouseUp;
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		/// <summary>Enable/Disable double buffering for the control</summary>
		public bool DblBuffered
		{
			get { return DoubleBuffered; }
			set { DoubleBuffered = value; }
		}

		/// <summary>Get/Set additional keys that should be considered input keys. e.g. Keys.Left, Keys.Right, etc</summary>
		public Keys[] AdditionalInputKeys {get;set;}

		/// <summary>Include client input keys</summary>
		protected override bool IsInputKey(Keys key)
		{
			if (AdditionalInputKeys != null) foreach (Keys k in AdditionalInputKeys) if (k == key) return true;
			return base.IsInputKey(key);
		}

		/// <summary>Return a pixel from the image clamped to within the bounds of the image</summary>
		public Color Pixel(float x, float y)
		{
			int ix = (int)Math.Max(0.0f, Math.Min(Image.Width  - 1f, x));
			int iy = (int)Math.Max(0.0f, Math.Min(Image.Height - 1f, y));
			return ((Bitmap)Image).GetPixel(ix, iy);
		}

		/// <summary>Return a linearly interpolated pixel from the image</summary>
		public Colour128 LerpPixel(float x, float y)
		{
			// Return a linearly interpolated pixel value
			x -= 0.5f;
			y -= 0.5f;
			float dx = x - (int)x;
			float dy = y - (int)y;
			float w0 = (1.0f - dx) * (1.0f - dy);
			float w1 = (       dx) * (1.0f - dy);
			float w2 = (1.0f - dx) * (       dy);
			float w3 = (       dx) * (       dy);
			Color c0 = Pixel(x,               y);
			Color c1 = Pixel(x + 1.0f,        y);
			Color c2 = Pixel(x,        1.0f + y);
			Color c3 = Pixel(x + 1.0f, 1.0f + y);
			return new Colour128(
				(c0.A*w0 + c1.A*w1 + c2.A*w2 + c3.A*w3),
				(c0.R*w0 + c1.R*w1 + c2.R*w2 + c3.R*w3),
				(c0.G*w0 + c1.G*w1 + c2.G*w2 + c3.G*w3),
				(c0.B*w0 + c1.B*w1 + c2.B*w2 + c3.B*w3));
		}

		/// <summary>Get/Set the image to display in the control. Remember to call Refresh if needed afterwards</summary>
		public Image Image
		{
			get { return m_bitmap; }
			set
			{
				float zoom = m_bitmap != null ? Zoom : 1f; // preserve the zoom
				m_bitmap = value ?? m_error_bitmap;
				//m_bitmap.RotateFlip(m_rotation);
				SetZoomLimits();
				Zoom = zoom;
				if (ImageChanged != null) ImageChanged(this);
			}
		}
	
		/// <summary>Get/Set the error image to display</summary>
		public Image ErrorImage
		{
			get { return m_error_bitmap; }
			set
			{
				m_error_bitmap = value;
				if( m_error_bitmap == null )
				{
					Bitmap bm = new Bitmap(1,1);
					bm.SetPixel(0,0,BackColor);
					m_error_bitmap = bm;
				}
			}
		}

		/// <summary>Get/Set the amount to rotate the image by</summary>
		public RotateFlipType Rotation
		{
			get { return m_rotation; }
			set { m_rotation = value; }
		}

		/// <summary>Get/Set the level of zoom. Remember to call Refresh if needed afterwards. Zoom = view size / bitmap size</summary>
		public float Zoom
		{
			get { return m_zoom; }
			set
			{
				m_zoom = Math.Max(m_min_zoom, Math.Min(m_max_zoom, value));
				ClampCentreToBitmap();
				if (ImageZoomed != null) ImageZoomed(this);
			}
		}

		/// <summary>Get/Set the view centre. Remember to call Refresh if needed afterwards</summary>
		public Point Centre
		{
			get { return m_centre; }
			set
			{
				m_centre = value;
				ClampCentreToBitmap();
				if (ImageMoved != null) ImageMoved(this);
			}
		}

		/// <summary>Return the visible area of the image</summary>
		public Rectangle VisibleArea
		{
			get
			{
				// Project the current client area into image space
				Rectangle rect = ClientRectangle;
				rect.Width  = (int)(rect.Width  / Zoom);
				rect.Height = (int)(rect.Height / Zoom);
				rect.X = Centre.X - rect.Width  / 2;
				rect.Y = Centre.Y - rect.Height / 2;

				// If the view extends beyond the image clamp to the image
				if (rect.X < 0)                          rect.X = 0;
				if (rect.Y < 0)                          rect.Y = 0;
				if (rect.X + rect.Width  > Image.Width ) rect.Width  = Image.Width  - rect.X;
				if (rect.Y + rect.Height > Image.Height) rect.Height = Image.Height - rect.Y;
				
				return rect;
			}
		}

		/// <summary>Return the rectangle containing the area (in screen space) in which the image
		/// is displayed. Generally this will equal ClientRectangle when zoomed in but will be
		/// less than ClientRectangle when zoomed out (unless the image dimensions exactly match the window size)</summary>
		public Rectangle DisplayArea
		{
			get
			{
				Rectangle src_rect = VisibleArea;
				Rectangle dst_rect = ClientRectangle;
				dst_rect.Width  = (int)(src_rect.Width  * Zoom);
				dst_rect.Height = (int)(src_rect.Height * Zoom);
				dst_rect.X = (ClientRectangle.Width  - dst_rect.Width ) / 2;
				dst_rect.Y = (ClientRectangle.Height - dst_rect.Height) / 2;
				return dst_rect;
			}
		}

		/// <summary>Zoom the image so that it maximises its size in the client area</summary>
		public void FitToWindow()
		{
			Rectangle rect = ClientRectangle;
			Centre = new Point(m_bitmap.Width/2,m_bitmap.Height/2);
			Zoom = (m_bitmap.Width*rect.Height > m_bitmap.Height*rect.Width) ? (float)rect.Width/m_bitmap.Width : (float)rect.Height/m_bitmap.Height;
		}

		/// <summary>Get/Set whether the image is drawn in OnPaint handlers</summary>
		public bool ImageVisible
		{
			get { return m_draw_image; }
			set { m_draw_image = value; }
		}

		/// <summary>Get/Set whether image dragging is enabled</summary>
		public bool	EnableDragging
		{
			get { return m_drag_enabled; }
			set { m_drag_enabled = value; }
		}

		/// <summary>Return the scale factors between the visible portion of the image and the client rectangle</summary>
		public PointF ImageSpaceScale
		{
			get
			{
				Rectangle visiable_area = VisibleArea;
				Rectangle display_area = DisplayArea;
				return new PointF(
					visiable_area.Width  / (float)display_area.Width,
					visiable_area.Height / (float)display_area.Height);
			}
		}

		/// <summary>Convert a point in client space to a point in image space</summary>
		public PointF PointToImageSpace(Point pt)
		{
			Rectangle visiable_area = VisibleArea;
			Rectangle display_area = DisplayArea;
			PointF scale = ImageSpaceScale;
			return new PointF(
				(pt.X - display_area.Left) * scale.X + visiable_area.Left,
				(pt.Y - display_area.Top ) * scale.Y + visiable_area.Top);
		}

		// Handle mouse down events for dragging
		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (!m_drag_enabled || e.Button != MouseButtons.Left) return;
			Capture = true;
			Cursor = Cursors.SizeAll;
			m_drag_start_mouse_pos = e.Location;
			m_drag_start_view_pos = Centre;
			MouseMove += OnMouseDrag;
		}
		
		// Handle mouse up events to end dragging
		private void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (!m_drag_enabled || e.Button != MouseButtons.Left) return;
			Capture = false;
			Cursor = Cursors.Default;
			MouseMove -= OnMouseDrag;
		}

		// Handle mouse move events while dragging
		private void OnMouseDrag(object sender, MouseEventArgs e)
		{
			if (!m_drag_enabled || e.Button != MouseButtons.Left) { MouseMove -= OnMouseDrag; return; }
			Size diff = new Size(m_drag_start_mouse_pos.X - e.Location.X, m_drag_start_mouse_pos.Y - e.Location.Y);
			diff.Width  = (int)(diff.Width  / Zoom);
			diff.Height = (int)(diff.Height / Zoom);
			Centre = m_drag_start_view_pos + diff;
			Refresh();
		}

		// Handle mouse wheel zooming
		private void OnMouseWheel(object sender, MouseEventArgs e)
		{
			//PointF mouse_pos = PointToImageSpace(e.Location);
			//PointF to_centre = (PointF.FromPoint(Centre) - mouse_pos);
			Zoom *= (1.0f - e.Delta * 0.001f);
			//Centre = (mouse_pos + to_centre * Zoom).ToPoint();
			Refresh();
		}

		/// <summary>Resize</summary>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			SetZoomLimits();
			Refresh();
		}

		/// <summary>Absorb PaintBackground events</summary>
		protected override void OnPaintBackground(PaintEventArgs e)
		{}

		/// <summary>Paint the control</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			Rectangle src_rect = VisibleArea;
			Rectangle dst_rect = DisplayArea;

			e.Graphics.CompositingQuality = Quality.CompositingQuality;
			e.Graphics.InterpolationMode  = Quality.InterpolationMode;
			e.Graphics.SmoothingMode      = Quality.SmoothingMode;
			e.Graphics.PixelOffsetMode    = Quality.PixelOffsetMode;

			// Draw the image
			if (ImageVisible)
			{
				if (OwnerPaint != null)
					OwnerPaint(this, e, src_rect, dst_rect);
				else
					Render(e.Graphics, src_rect, dst_rect);
			}

			e.Graphics.ResetTransform();

			// Add overlays
			if (AddOverlaysEvent != null)
				AddOverlaysEvent(this, e.Graphics, src_rect, dst_rect);
		}

		/// <summary>Render 'Image' to gfx. Useful as a default paint method for OwnerPaint handlers</summary>
		public void Render(Graphics gfx, Rectangle src_rect, Rectangle dst_rect)
		{
			gfx.Clear(BackColor);
			gfx.DrawImage(Image, dst_rect, src_rect, GraphicsUnit.Pixel);
		}

		/// <summary>Ensure 'rect' is within the bounds of the bitmap rectangle and has the same aspect ratio as the bitmap</summary>
		private void ClampCentreToBitmap()
		{
			// Project the current client area into image space
			int half_width	= Math.Min(Image.Width,  (int)(ClientRectangle.Width  / Zoom)) / 2;
			int half_height	= Math.Min(Image.Height, (int)(ClientRectangle.Height / Zoom)) / 2;
			m_centre.X = Math.Max(half_width , Math.Min(Image.Width  - half_width , m_centre.X));
			m_centre.Y = Math.Max(half_height, Math.Min(Image.Height - half_height, m_centre.Y));
		}

		/// <summary>Set sensible zoom limits given the current bitmap size</summary>
		private void SetZoomLimits()
		{
			const float MinPixels = 5f;
			m_min_zoom = Math.Min(ClientRectangle.Width/(float)m_bitmap.Width, ClientRectangle.Height/(float)m_bitmap.Height);
			m_max_zoom = Math.Max(ClientRectangle.Width/MinPixels,             ClientRectangle.Height/MinPixels);
			m_min_zoom = Math.Min(m_min_zoom, 1f);
			m_max_zoom = Math.Max(m_max_zoom, 1f);
			Zoom = Zoom;
		}
	}
}