using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Rylogic.Windows.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Interop.Win32;
using Rylogic.Maths;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace MeasureSchmitt
{
	public partial class MainWindow :Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();

			MeasureMode = EMeasureMode.None;
			View = new BitmapImage();
			TargetGfx = new Target();
			Zoom = 1.0;

			// Update the view
			View = CaptureScreen(ref m_bmp!, new Rectangle(0, 0, 500, 500));

			SetMeasurementMode = Command.Create(this, SetMeasurementModeInternal);
			MovePosition0 = Command.Create(this, MovePosition0Internal);

			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			PeriodicRefresh = false;
			TargetGfx = null!;
			base.OnClosed(e);
		}

		/// <summary>Measurement mode</summary>
		public EMeasureMode MeasureMode
		{
			get => m_measure_mode;
			set
			{
				if (m_measure_mode == value) return;
				m_measure_mode = value;
				
				// Set defaults
				PeriodicRefresh = false;
				TargetGfx.Visibility = Visibility.Collapsed;
				switch (MeasureMode)
				{
					case EMeasureMode.None:
					{
						break;
					}
					case EMeasureMode.MousePosition:
					{
						PeriodicRefresh = true;
						break;
					}
					case EMeasureMode.FixedPosition:
					{
						TargetGfx.Visibility = Visibility.Visible;
						break;
					}
					case EMeasureMode.Line:
					{
						break;
					}
					case EMeasureMode.Box:
					{
						break;
					}
					default:
					{
						throw new Exception("Unknown measurement mode");
					}
				}

				SignalMeasure();
				NotifyPropertyChanged(nameof(MeasureMode));
			}
		}
		private EMeasureMode m_measure_mode;

		/// <summary>True if the measured data is updating</summary>
		private bool PeriodicRefresh
		{
			get => m_timer != null;
			set
			{
				if (PeriodicRefresh == value) return;
				m_timer?.Stop();
				m_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Background, HandleTick, Dispatcher.CurrentDispatcher) : null;
				m_timer?.Start();

				// Handler
				void HandleTick(object? sender, EventArgs e)
				{
					SignalMeasure();
				}
			}
		}
		private DispatcherTimer? m_timer;

		/// <summary>The area of interest</summary>
		public BitmapImage View
		{
			get => m_view;
			private set
			{
				if (m_view == value) return;
				m_view = value;
				NotifyPropertyChanged(nameof(View));
			}
		}
		private BitmapImage m_view = null!;
		private Bitmap m_bmp = null!;

		/// <summary>First point of interest</summary>
		public Vector Position0
		{
			get => m_position0;
			set
			{
				if (m_position0 == value) return;
				m_position0 = value;
				NotifyPropertyChanged(nameof(Position0));
			}
		}
		private Vector m_position0;

		/// <summary>The area to capture</summary>
		public Rectangle Area
		{
			get
			{
				var sx = User32.GetSystemMetrics(Win32.ESystemMetrics.SM_XVIRTUALSCREEN);
				var sy = User32.GetSystemMetrics(Win32.ESystemMetrics.SM_YVIRTUALSCREEN);
				var sw = User32.GetSystemMetrics(Win32.ESystemMetrics.SM_CXVIRTUALSCREEN);
				var sh = User32.GetSystemMetrics(Win32.ESystemMetrics.SM_CYVIRTUALSCREEN);

				// Clamp the capture area to the virtual screen area
				var size = m_image_view.DesiredSize;
				var w = Math_.Max(size.Width / Zoom, 10);
				var h = Math_.Max(size.Height / Zoom, 10);
				var r = Math_.Div(size.Width, size.Height, 1.0);
				if (w > sw) { w = sw; h = Math.Min(h, sw / r); }
				if (h > sh) { h = sh; w = Math.Min(w, sh * r); }

				Vector ClampPoint(Vector pt)
				{
					pt.X = Math_.Clamp(pt.X, sx + w / 2, sx + sw - w / 2);
					pt.Y = Math_.Clamp(pt.Y, sy + h / 2, sy + sh - h / 2);
					return pt;
				}

				Rect rect;
				switch (MeasureMode)
				{
					case EMeasureMode.None:
					{
						throw new Exception("Area undefinited when there is no measurement mode");
					}
					case EMeasureMode.MousePosition:
					{
						// An area centred on the mouse
						var p  = User32.GetCursorPos();
						var pt = ClampPoint(new Vector(p.X, p.Y));
						rect = new Rect(pt.X - w / 2, pt.Y - h / 2, w, h);
						break;
					}
					case EMeasureMode.FixedPosition:
					{
						// An area centred on the fixed point
						var pt = ClampPoint(TargetGfx.CentrePoint);
						rect = new Rect(pt.X - w / 2, pt.Y - h / 2, w, h);
						break;
					}
					case EMeasureMode.Line:
					case EMeasureMode.Box:
					default:
					{
						throw new Exception("Unknown measurement mode");
					}
				}
				rect.X = (int)rect.X;
				rect.Y = (int)rect.Y;
				rect.Width = (int)rect.Width;
				rect.Height = (int)rect.Height;
				return rect.ToRectI();
			}
		}

		/// <summary>Zoom for the captured screen area</summary>
		public double Zoom
		{
			get => m_zoom;
			set
			{
				if (m_zoom == value) return;
				m_zoom = Math_.Clamp(value, 0.01, 100.0);
				SignalMeasure();
				NotifyPropertyChanged(nameof(Zoom));
			}
		}
		private double m_zoom;

		/// <summary>Whether to smooth the displayed screen capture</summary>
		public bool Smooth
		{
			get => m_smooth;
			set
			{
				if (m_smooth == value) return;
				m_smooth = value;
				SignalMeasure();
				NotifyPropertyChanged(nameof(Smooth));
			}
		}
		private bool m_smooth;

		/// <summary>Include the mouse in the screen capture</summary>
		public bool MouseVisible
		{
			get => m_mouse_visible;
			set
			{
				if (m_mouse_visible == value) return;
				m_mouse_visible = value;
				SignalMeasure();
				NotifyPropertyChanged(nameof(MouseVisible));
			}
		}
		private bool m_mouse_visible;

		/// <summary>Perform a capture and screen position measurement</summary>
		private void DoMeasure()
		{
			m_measure_pending = false;
			if (MeasureMode != EMeasureMode.None)
				View = CaptureScreen(ref m_bmp!, Area);
		}
		private void SignalMeasure()
		{
			if (m_measure_pending) return;
			Dispatcher.BeginInvoke(new Action(DoMeasure), null);
			m_measure_pending = true;
		}
		private bool m_measure_pending;

		/// <summary>Capture an area of the screen. Return 'bm'</summary>
		private BitmapImage CaptureScreen(ref Bitmap? bm, Rectangle area)
		{
			// Make sure the bitmap exists and is the right size
			if (bm == null || bm.Size != area.Size)
			{
				//Debug.Assert( .PrimaryScreen.BitsPerPixel == 32);
				bm = new Bitmap(area.Width, area.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
			}
			try
			{
				using var g = Graphics.FromImage(bm);
				g.Clear(System.Drawing.Color.Black);
				g.CopyFromScreen(area.Location, new Point(), area.Size);

				// Add the mouse pointer to the graphics
				const int CURSOR_SHOWING = 1;
				var ci = User32.GetCursorInfo();
				if (MouseVisible && ci.flags == CURSOR_SHOWING && area.Contains(new Rectangle(ci.ptScreenPos.X, ci.ptScreenPos.Y, (int)ci.cbSize, (int)ci.cbSize)))
				{
					User32.DrawIcon(g.GetHdc(), ci.ptScreenPos.X - area.X, ci.ptScreenPos.Y - area.Y, ci.hCursor);
					g.ReleaseHdc();
				}

				//bm.Save(@"P:/dump/screen_grab.bmp");
			}
			catch { }
			return bm.ToBitmapSource();
		}

		/// <summary>Set the measurement mode</summary>
		public ICommand SetMeasurementMode { get; }
		private void SetMeasurementModeInternal(object? value)
		{
			if (value is EMeasureMode mode)
				MeasureMode = mode;
		}

		/// <summary>Adjust Position0 by a direction</summary>
		public ICommand MovePosition0 { get; }
		private void MovePosition0Internal(object? value)
		{
			if (value is Vector dir)
				Position0 += dir;
		}

		/// <summary>The target graphic</summary>
		public Target TargetGfx
		{
			get => m_target_gfx;
			set
			{
				if (m_target_gfx == value) return;
				if (m_target_gfx != null)
				{
					m_target_gfx.LocationChanged -= HandleLocationChanged;
					m_target_gfx.Close();
				}
				m_target_gfx = value;
				if (m_target_gfx != null)
				{
					m_target_gfx.Show();
					m_target_gfx.LocationChanged += HandleLocationChanged;
				}

				// Handlers
				void HandleLocationChanged(object? sender, EventArgs e)
				{
					Position0 = TargetGfx.CentrePoint;
					SignalMeasure();
				}
			}
		}
		private Target m_target_gfx = null!;

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
