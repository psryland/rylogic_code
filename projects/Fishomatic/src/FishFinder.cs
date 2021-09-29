using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Extn.Windows;
using Rylogic.Interop.Win32;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;

namespace Fishomatic
{
	public class FishFinder :IDisposable, INotifyPropertyChanged
	{
		//private const string WindowName = "World of Warcraft";
		private const string WindowName = "Virtual Rex";

		public FishFinder(Settings settings)
		{
			Settings = settings;
			State = EState.Idle;
			Ticker = true;
		}
		public void Dispose()
		{
			Run = false;
			Ticker = false;
		}

		/// <summary></summary>
		private Settings Settings { get; }

		/// <summary>State machine state</summary>
		public EState State
		{
			get => m_state;
			set
			{
				if (State == value) return;
				m_state = value;
				NotifyPropertyChanged(nameof(State));
			}
		}
		private EState m_state;

		/// <summary>WoW main window</summary>
		public CWindow? TargetWnd
		{
			get
			{
				// The WoW Window can appear or disappear at any time
				if (m_wow != null)
				{
					if (!Win32.IsWindow(m_wow))
						m_wow = null;
				}
				if (m_wow == null)
				{
					// Find the window by name, if there are multiple windows, choose the largest
					foreach (var win in Windows.GetWindowsByName(WindowName, true))
					{
						// Find the largest window with WindowName as a title
						if (m_wow != null)
						{
							var r0 = m_wow.WindowRectangle;
							var r1 = win.WindowRectangle;
							if (r1.Width * r1.Height > r0.Width * r0.Height)
								m_wow = win;
						}
						m_wow = win;
					}
				}
				return m_wow;
			}
		}
		private CWindow? m_wow = null!;
	
		/// <summary>Run the fish finder</summary>
		private bool Ticker
		{
			get => m_ticker != null;
			set
			{
				if (Ticker == value) return;
				if (m_ticker != null)
				{
					m_ticker.Stop();
				}
				m_ticker = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (m_ticker != null)
				{
					m_ticker.Start();
				}

				// Handle the timer tick
				void HandleTick(object? sender, EventArgs e)
				{
					GoFish();
				}
			}
		}
		private DispatcherTimer? m_ticker;

		/// <summary>Run/Pause the fishing</summary>
		public bool Run
		{
			get => State != EState.Idle;
			set
			{
				if (Run == value) return;
				State = EState.Cast;
				NotifyPropertyChanged(nameof(Run));
			}
		}

		/// <summary></summary>
		private void GoFish()
		{
			// Handle the WoW window appearing/disappearing
			if (TargetWnd is not CWindow wow)
				return;

			switch (State)
			{
				case EState.Idle:
				{
					// While idle (paused), allow the sample colour to be set
					if (Win32.KeyDown(EKeyCodes.LControlKey))
					{
						// Read the colour of the pixel under the mouse
						var search_area = Settings.SmallSearchArea(Mouse.GetPosition(null)).ToRectI();
						CaptureScreen(ref m_small_grab, search_area);
						m_small_grab = m_small_grab ?? throw new Exception("Failed to capture screen");
						Settings.TargetColour = m_small_grab.GetPixel(search_area.Width / 2, search_area.Height / 2);
					}
					break;
				}
				case EState.Cast:
				{
					break;
				}
				default:
				{
					throw new Exception($"Unhandled state: {State}");
				}
			}
		}
		private Bitmap? m_small_grab;

		// Capture a screen shot
		private void CaptureScreen(ref Bitmap? bm, Rectangle area)
		{
			// Make sure the bitmap exists and is the right size
			if (bm == null || bm.Size != area.Size)
			{
				//Debug.Assert( .PrimaryScreen.BitsPerPixel == 32);
				bm = new Bitmap(area.Width, area.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
			}
			using var g = Graphics.FromImage(bm);
			g.CopyFromScreen(area.Location, new Point(0, 0), area.Size);

			bm.Save(@"P:/dump/screen_grab.bmp");
		}

		// Send a keypress to the wow window
		private void PressKey(CWindow win, int key)
		{
			win.SendMessage(Win32.WM_KEYDOWN, key, 0x00080001);
			win.SendMessage(Win32.WM_KEYUP, key, 0x00080001);
		}

		// Send a mouse click to the wow window
		private void ClickBobber(CWindow win, Point pos, MouseButton btn)
		{
			int wparam;
			uint down_msg, up_msg;

			switch (btn)
			{
				case MouseButton.Left:
				{
					wparam = Win32.MK_LBUTTON;
					down_msg = Win32.WM_LBUTTONDOWN;
					up_msg = Win32.WM_LBUTTONUP;
					break;
				}
				case MouseButton.Right:
				{
					wparam = Win32.MK_RBUTTON;
					down_msg = Win32.WM_RBUTTONDOWN;
					up_msg = Win32.WM_RBUTTONUP;
					break;
				}
				default:
				{
					return;
				}
			}

			//Cursor.Position = pos;
			var lparam = (int)Win32.PointToLParam(pos);
			win.SendMessage(down_msg, wparam, lparam);
			win.SendMessage(up_msg, wparam, lparam);
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
