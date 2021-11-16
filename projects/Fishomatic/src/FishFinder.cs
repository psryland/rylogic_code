using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Extn.Windows;
using Rylogic.Interop.Win32;
using Rylogic.Maths;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Fishomatic
{
	public class FishFinder :IDisposable, INotifyPropertyChanged
	{
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
			get => m_state_curr;
			set
			{
				if (State == value) return;
				m_state_curr = value;
				m_state_curr_when = Environment.TickCount;
				m_state_next_when = m_state_curr_when;
				NotifyPropertyChanged(nameof(State));
				NotifyPropertyChanged(nameof(Run));
			}
		}
		public EState StateNext
		{
			get => m_state_next;
		}
		private EState m_state_curr;
		private EState m_state_next;
		private int m_state_curr_when;
		private int m_state_next_when;

		/// <summary>Request a state machine state change after X seconds</summary>
		private void StateChange(EState next_state)
		{
			StateChange(next_state, TimeSpan.Zero);
		}
		private void StateChange(EState next_state, TimeSpan delay)
		{
			m_state_next = next_state;
			m_state_next_when = Environment.TickCount + (int)delay.TotalMilliseconds;
		}

		/// <summary>The string describing what's going on right now</summary>
		public string Status
		{
			get => m_status;
			private set
			{
				if (Status == value) return;
				m_status = value;
				NotifyPropertyChanged(nameof(Status));
			}
		}
		private string m_status = string.Empty;

		/// <summary>Time until the next state change</summary>
		public TimeSpan Remaining => TimeSpan.FromMilliseconds(Math.Max(0, m_state_next_when - Environment.TickCount));

		/// <summary>Fractional time until state change</summary>
		public double ProgressFrac => m_state_next_when > m_state_curr_when ? Math_.Frac(m_state_curr_when, Environment.TickCount, m_state_next_when) : 0.0;

		/// <summary>WoW main window</summary>
		public CWindow? TargetWnd
		{
			get
			{
				// The WoW Window can appear or disappear at any time
				if (m_wow != null)
				{
					if (!Win32.IsWindow(m_wow) || m_wow.WindowRectangle.Width < 50 || m_wow.WindowRectangle.Height < 50)
						m_wow = null;
				}
				if (m_wow == null)
				{
					// Find the window by name, if there are multiple windows, choose the largest
					foreach (var win in Windows.GetWindowsByName(Settings.TargetWindowName, true))
					{
						// Find the largest window with WindowName as a title
						if (m_wow != null)
						{
							var r0 = m_wow.WindowRectangle;
							var r1 = win.WindowRectangle;
							if (r1.Width * r1.Height > r0.Width * r0.Height && r1.Width > 50 && r1.Height > 50)
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
				StateChange(value ? EState.Start : EState.Idle);
			}
		}

		/// <summary>Run the fishing state machine</summary>
		private void GoFish()
		{
			// Handle the WoW window appearing/disappearing
			if (TargetWnd is not CWindow wow)
				return;

			// If a state change is requested, wait for the delay time
			if (State != StateNext)
			{
				NotifyPropertyChanged(nameof(Remaining));
				NotifyPropertyChanged(nameof(ProgressFrac));
				if (Remaining != TimeSpan.Zero)
					return;

				State = StateNext;
			}

			// Handle the state
			switch (State)
			{
				case EState.Idle:
				{
					// While idle (paused), allow the sample colour to be set
					if (Win32.KeyDown(EKeyCodes.LControlKey))
					{
						// Read the colour of the pixel under the mouse
						var mouse_pt = Win32.GetCursorPos().ToPoint().ToPointD();
						var search_area = Settings.SmallSearchArea(mouse_pt).ToRectI();
						CaptureScreen(ref m_small_grab, search_area);
						m_small_grab = m_small_grab ?? throw new Exception("Failed to capture screen");
						Settings.TargetColour = m_small_grab.GetPixel(search_area.Width / 2, search_area.Height / 2);
						Status = "Sampling Target Colour";
					}
					else
					{
						Status = "Idle";
					}
					NotifyBobberUpdate(false);
					break;
				}
				case EState.Start:
				{
					m_start_time = Environment.TickCount;
					NotifyBobberUpdate(false);
					StateChange(EState.Cast);
					break;
				}
				case EState.Cast:
				{
					// See if it's time to apply baubles
					if (Environment.TickCount > m_start_time + (int)Settings.BaublesTime.TotalMilliseconds)
					{
						StateChange(EState.ApplyBaubles);
						break;
					}

					// Otherwise, cast for another fish
					Status = "Casting";
					PressKey(wow, Settings.CastKey);
					m_cast_time = Environment.TickCount;
					StateChange(EState.LookForBobber, Settings.AfterCastWait);
					break;
				}
				case EState.LookForBobber:
				{
					NotifyBobberUpdate(true);
					break;
				}
				default:
				{
					throw new Exception($"Unhandled state: {State}");
				}
			}
		}
		private Bitmap? m_small_grab;
		private int m_start_time;
		private int m_cast_time;

		/// <summary>Capture an area of the screen</summary>
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

			//bm.Save(@"P:/dump/screen_grab.bmp");
		}

		/// <summary>Send a keypress to the wow window</summary>
		private void PressKey(CWindow win, int key)
		{
			win.SendMessage(Win32.WM_KEYDOWN, key, 0x00080001);
			win.SendMessage(Win32.WM_KEYUP, key, 0x00080001);
		}

		/// <summary>Send a mouse click to the wow window</summary>
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

		/// <summary></summary>
		private void NotifyBobberUpdate(bool visible)
		{
			NotifyBobberUpdate(visible, new Point(0, 0));
		}
		private void NotifyBobberUpdate(bool visible, Point position)
		{
			BobberUpdate?.Invoke(this, new BobberUpdateEventArgs(visible, position));
		}

		/// <summary></summary>
		public event EventHandler<BobberUpdateEventArgs>? BobberUpdate;
		public class BobberUpdateEventArgs :EventArgs
		{
			public BobberUpdateEventArgs(bool visible, Point position)
			{
				Visible = visible;
				Position = position;
			}

			/// <summary>True if the bobber is visible</summary>
			public bool Visible { get; }

			/// <summary>The window-relative position of the bobber</summary>
			public Point Position { get; }
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
