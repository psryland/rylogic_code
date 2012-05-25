using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using pr.maths;
using pr.util;

namespace Fishomatic2
{
	public partial class MainForm :Form
	{
		private enum EState
		{
			Cast,
			LookForBobber,
			LookingForBobber,
			WatchBobber,
			ClickBobber,
			CastEnd,
			ApplyBaubles,
			ApplyBaublesToPole,
			ApplyBaublesWaiting,
			ApplyBaublesDone,
			FoundBobber
		}

		private struct RGB
		{
			public byte b,g,r,a;
			public RGB(Color col) { b = col.B; g = col.G; r = col.R; a = col.A; }
			public static int DistanceSq(RGB lhs, RGB rhs)
			{
				int r = lhs.r - rhs.r;
				int g = lhs.g - rhs.g;
				int b = lhs.b - rhs.b;
				return r*r + g*g + b*b;
			}
		}

		private readonly string FishPropsPath = Application.StartupPath + @"\Settings.xml";
		private readonly Timer m_tick = new Timer();
		private Window m_wow = null;
		private FishProperties m_props = null;
		private Bitmap m_large_grab = null;				// Screen capture of the whole search area
		private Bitmap m_small_grab = null;				// Screen capture of just the area around the bobber
		private EState m_state = EState.Cast;
		private EState m_next_state = EState.Cast;
		private Point m_bobber_position = new Point(0,0);
		private int m_state_time = 0;					// The time that the state was started
		private int m_next_state_time = 0;				// The time to move to the next state
		private int m_cast_time = 0;					// The time when the cast started
		private int m_minutes_elapsed = 0;
		private int m_start_time_baubles = 0;
		private int m_max_delta = 0;
		private bool m_paused = true;

		public MainForm()
		{
			InitializeComponent();
			m_props = FishProperties.Load(FishPropsPath);

			if (!FindWowWindow())
				throw new Exception("Unable to find the World of Warcraft window.");
	
			// Set the area to search in
			SetSearchBounds();

			// Set up the timer
			m_tick.Interval = 100;
			m_tick.Tick += delegate { Fish(); };
			m_tick.Start();
			
			// Log
			m_list_output.Columns.Add("Log Entry", m_list_output.Width-4);
		
			// Progress bar
			m_progress.Minimum = 0;
			m_progress.Maximum = 100;

			// The move threshold slider
			m_scroll_move_threshold.Minimum = 1;
			m_scroll_move_threshold.Maximum = 20;
			m_scroll_move_threshold.SmallChange = 1;
			m_scroll_move_threshold.LargeChange = 1;
			m_scroll_move_threshold.Value = Maths.Clamp(m_props.MoveThreshold, m_scroll_move_threshold.Minimum, m_scroll_move_threshold.Maximum);
			m_scroll_move_threshold.ValueChanged += delegate
			{
				m_props.MoveThreshold = m_scroll_move_threshold.Value;
				m_props.Save(FishPropsPath);
				m_edit_move_threshold.Text = m_props.MoveThreshold.ToString();
			};
			m_edit_move_threshold.Text = m_props.MoveThreshold.ToString();
			m_edit_move_threshold.TextChanged += delegate
			{
				int thres; int.TryParse(m_edit_move_threshold.Text, out thres);
				if (thres != m_props.MoveThreshold)
				{
					m_props.MoveThreshold = Maths.Clamp(thres, m_scroll_move_threshold.Minimum, m_scroll_move_threshold.Maximum);
					m_props.Save(FishPropsPath);
					m_scroll_move_threshold.Value = thres;
					m_edit_move_threshold.Text = m_props.MoveThreshold.ToString();
				}
			};

			// Display options
			m_btn_options.Click += delegate
			{
				Form form = new Form{Text="Options" ,FormBorderStyle=FormBorderStyle.SizableToolWindow ,Size=new Size(300, 400)};
				PropertyGrid grid = new PropertyGrid{SelectedObject=m_props ,Dock=DockStyle.Fill};
				form.Controls.Add(grid);
				form.ShowDialog(this);
				m_props.Save(FishPropsPath);
			};

			// The go fish button
			m_btn_gofish.Click += delegate
			{
				if (m_btn_gofish.CheckState == CheckState.Checked)	{ StartFishing(); }
				else												{ StopFishing(); }
			};

			// The search area button
			m_btn_search_bounds.Click += delegate
			{
				Rectangle desk = Screen.PrimaryScreen.WorkingArea;
				Rectangle rect = m_props.SearchArea;
				rect.Offset(-desk.Left, -desk.Top);
                
				Form form = new Form
				{
					Text="Bobber search area"
					,StartPosition=FormStartPosition.Manual
					,DesktopBounds=rect
					,FormBorderStyle=FormBorderStyle.SizableToolWindow
					,Opacity=0.5
				};
				form.KeyDown += delegate (object i, KeyEventArgs e) { if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Escape) form.Close(); };
				form.ShowDialog();

				rect = form.DesktopBounds;
				rect.Offset(desk.Left, desk.Top);
				m_props.SearchArea = rect;
				m_props.Save(FishPropsPath);
				
				//CaptureScreen(ref m_small_grab, m_props.SearchArea);
			};


			// Menu
			m_menu_file_reset_options.Click += delegate { m_props = new FishProperties(); };
			m_menu_file_always_on_top.Click += delegate { m_menu_file_always_on_top.Checked=!m_menu_file_always_on_top.Checked; TopMost=m_menu_file_always_on_top.Checked; };
			m_menu_file_exit.Click += delegate { Close(); };

			m_menu_file_always_on_top.Checked = m_props.AlwaysOnTop;
			TopMost = m_props.AlwaysOnTop;

			FormClosing += delegate { m_props.Save(FishPropsPath); };
			StopFishing();
		}

		// Start the fishing state machine
		private void StartFishing()
		{
			Text = "Fishomatic - Running";
			Msg(Text);
			m_btn_gofish.Text = "Stop Fishing";
			m_btn_gofish.Checked = true;
			m_state = EState.Cast;
			m_state_time = Environment.TickCount;
			m_next_state = m_state;
			m_next_state_time = m_state_time;
			m_start_time_baubles = m_state_time;
			m_minutes_elapsed = 0;
			m_paused = false;
			m_wow.Activate();
		}

		// Stop the fishing state machine
		private void StopFishing()
		{
			Text = "Fishomatic - Paused";
			Msg(Text);
			m_btn_gofish.Text = "Go Fish!"; 
			m_btn_gofish.Checked = false;
			m_paused = true;
		}

		// Add a message to the log
		private void Msg(string msg) { Msg(msg, false); }
		private void Msg(string msg, bool update_last)
		{
			if (update_last && m_list_output.Items.Count != 0 && m_list_output.Items[0].Text.StartsWith(msg.Substring(0, 4)))
				m_list_output.Items[0].Text = msg;
			else
				m_list_output.Items.Insert(0, new ListViewItem(msg));
			m_list_output.Refresh();
		}

		// Search for the wow window
		bool FindWowWindow()
		{
			foreach (Window win in Windows.GetWindowsByName("World of Warcraft", true))
			{
				if (m_wow == null) m_wow = win;
				else
				{
					// Find the largest window with "world of warcraft" as a title
					Rectangle r0 = m_wow.WindowRectangle;
					Rectangle r1 = win.WindowRectangle;
					if (r1.Width * r1.Height > r0.Width * r0.Height)
						m_wow = win;
				}
			}
			return m_wow != null;
		}

		// Send a keypress to the wow window
		private void PressKey(int key)
		{
			m_wow.SendMessage(Win32.WM_KEYDOWN, key, 0x00080001);
			m_wow.SendMessage(Win32.WM_KEYUP, key, 0x00080001);
		}

		// Send a mouse click to the wow window
		private void ClickBobber(Point pos, MouseButtons btn)
		{
			uint down_msg, up_msg; int wparam;
			switch (btn)
			{
			default:return;
			case MouseButtons.Left:
				wparam = Win32.MK_LBUTTON;
				down_msg = Win32.WM_LBUTTONDOWN;
				up_msg = Win32.WM_LBUTTONUP;
				break;
			case MouseButtons.Right:
				wparam = Win32.MK_RBUTTON;
				down_msg = Win32.WM_RBUTTONDOWN;
				up_msg = Win32.WM_RBUTTONUP;
				break;
			}

			Cursor.Position = pos;
			m_wow.SendMessage(down_msg ,wparam, Win32.MakeLParam(pos));
			m_wow.SendMessage(up_msg   ,wparam, Win32.MakeLParam(pos));
		}

		// Fishing main loop
		private void Fish()
		{
			// Toggle pause mode
			if (Win32.KeyPress(Keys.Scroll))
			{
				if (m_paused)	StartFishing();
				else			StopFishing();
			}

			// Main state machine
			if (m_paused) WhilePaused();
			else
			{
				// Check if it's time to apply baubles
				if (Environment.TickCount - m_start_time_baubles > m_minutes_elapsed * 60000)
				{
					if (++m_minutes_elapsed == m_props.BaublesTime)
						m_state = EState.ApplyBaubles;
					else
						Msg("Baubles in " + (m_props.BaublesTime - m_minutes_elapsed) + " minutes");
				}

				// Update the progress bar
				if (m_state != m_next_state && Environment.TickCount >= m_next_state_time)
				{
					m_state = m_next_state;
					m_state_time = Environment.TickCount;
					m_progress.Value = m_progress.Maximum;
				}
				else if (Environment.TickCount < m_next_state_time)
				{
					if (m_next_state_time == m_state_time) m_progress.Value = m_progress.Maximum;
                    else m_progress.Value = (Environment.TickCount - m_state_time) / (m_next_state_time - m_state_time);
					return;
				}

				// Get the time that's elapsed since the cast started
				int time_since_cast = Environment.TickCount - m_cast_time;

				switch (m_state)
				{
				default:break;
				
				case EState.Cast:
					Msg("Casting");
					m_status.Text = "Casting";
					PressKey(m_props.CastKey);
					m_cast_time = Environment.TickCount;
					m_next_state = EState.LookForBobber;
					m_next_state_time = Environment.TickCount + m_props.AfterCastWait;
					break;

				case EState.LookForBobber:
					Msg("Looking for bobber");
					m_next_state = EState.LookingForBobber;
					m_next_state_time = Environment.TickCount;
					break;

				case EState.LookingForBobber:
		            if (FindBobberPosition(ref m_bobber_position, new RGB(m_props.TargetColour), false))
		            {
						Msg("Bobber found at ("+m_bobber_position.X+","+m_bobber_position.Y+")");
		                m_next_state = EState.FoundBobber;
						m_next_state_time = Environment.TickCount + 100;
		            }
					else if (time_since_cast > m_props.AbortTime)
					{
						Msg("Bobber not found");
		                m_next_state = EState.CastEnd;
						m_next_state_time = Environment.TickCount;
		            }
					else
					{
						m_status.Text = "Looking for bobber";
					}
					break;
				case EState.FoundBobber:
		            if (FindBobberPosition(ref m_bobber_position, new RGB(m_props.TargetColour), true))
					{
						Msg("Bobber found at ("+m_bobber_position.X+","+m_bobber_position.Y+")", true);
						m_next_state = EState.WatchBobber;
						m_next_state_time = Environment.TickCount;
						m_max_delta = 0;
					}
					else if (time_since_cast > m_props.AbortTime)
					{
						Msg("Bobber not found");
		                m_next_state = EState.CastEnd;
						m_next_state_time = Environment.TickCount;
		            }
					else
					{
						m_status.Text = "Refining";
					}
					break;
				case EState.WatchBobber:
					if (BobberMoved(m_bobber_position, new RGB(m_props.TargetColour)) )
					{
						Msg("Bobber movement detected");
						m_next_state = EState.ClickBobber;
						m_next_state_time = Environment.TickCount + m_props.ClickDelay;
					}
					else if (time_since_cast > m_props.MaxFishCycle)
					{
						Msg("No bobber movement detected");
						m_next_state = EState.CastEnd;
						m_next_state_time = Environment.TickCount;
					}
					else
					{
						m_status.Text = "Catching...";
						m_progress.Value = Maths.Clamp(m_progress.Minimum + (m_progress.Maximum - m_progress.Minimum) * time_since_cast / m_props.MaxFishCycle, m_progress.Minimum, m_progress.Maximum);
					}
					break;

				case EState.ClickBobber:
		            ClickBobber(m_bobber_position, MouseButtons.Right);
		            Msg("Caught a fish!! (hopefully)");
		            m_next_state = EState.CastEnd;
					m_next_state_time = Environment.TickCount + m_props.AfterCatchWait;
		            break;

				case EState.CastEnd:
		            m_next_state = (m_minutes_elapsed < m_props.BaublesTime) ? EState.Cast : EState.ApplyBaubles;
					m_next_state_time = Environment.TickCount;
		            break;
					
				case EState.ApplyBaubles:
					Msg("Applying baubles...");
					PressKey(m_props.BaublesKey);
					m_next_state = EState.ApplyBaublesToPole;
					m_next_state_time = Environment.TickCount + 500;
					break;
				case EState.ApplyBaublesToPole:
					Msg("Applying baubles to the pole...", true);
					PressKey(m_props.FishingPoleKey);
					m_next_state = EState.ApplyBaublesWaiting;
					m_next_state_time = Environment.TickCount + 500;
					break;
				case EState.ApplyBaublesWaiting:
					Msg("Applying baubles to the pole, waiting...", true);
					m_next_state = EState.ApplyBaublesDone;
					m_next_state_time = Environment.TickCount + m_props.BaublesApplyWait;
					break;
				case EState.ApplyBaublesDone:
					Msg("Applying baubles to the pole, waiting... Done!", true);
					m_start_time_baubles = Environment.TickCount;
					m_minutes_elapsed = 0;
					m_next_state = EState.Cast;
					m_next_state_time = Environment.TickCount;
					break;
				}
			}

			// Ensure the history list doesn't get too big
			while (m_list_output.Items.Count > 500)
				m_list_output.Items.RemoveAt(m_list_output.Items.Count-1);
		}

		// Set the fishing search area
		private void SetSearchBounds()
		{
			Rectangle rect = m_wow.WindowRectangle;
			m_props.SearchArea = Rectangle.FromLTRB(
				rect.Left  + rect.Width  * 90   / 1280,
				rect.Top   + rect.Height * 90   / 1024,
				rect.Left  + rect.Width  * 1080 / 1280,
				rect.Top   + rect.Height * 850  / 1024);

			//CaptureScreen(ref m_small_grab, m_props.SearchArea);
		}

		// Capture a screen shot
		private void CaptureScreen(ref Bitmap bm, Rectangle area)
		{
			// Make sure the bitmap exists and is the right size
			if (bm == null || bm.Size != area.Size)
			{
				Debug.Assert(Screen.PrimaryScreen.BitsPerPixel == 32);
				bm = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppRgb);
			}
            using (Graphics g = Graphics.FromImage(bm))
		         g.CopyFromScreen(area.Location, new Point(0,0), area.Size);

			//bm.Save(@"D:/deleteme/screen_grab.bmp");
		}

		// Read the colour of the pixel under the mouse
		private Color ReadPixelColour()
		{
			Rectangle search_area = m_props.SmallSearchArea(MousePosition);
			CaptureScreen(ref m_small_grab, search_area);
			return m_small_grab.GetPixel(search_area.Width/2, search_area.Height/2);
		}

		// Return the point within a bitmap that matches 'target' the closest
		private Point FindTargetColour(Bitmap bitmap, RGB target, out int distance)
		{
			Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			BitmapData bm = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
			Point pt = new Point(0,0);
			distance = int.MaxValue;
			unsafe
			{
				//Bitmap test_bm = new Bitmap(rect.Width, rect.Height, bm.PixelFormat);
				RGB* px = (RGB*)bm.Scan0.ToPointer();
				for (int y = 0; y != bitmap.Height; ++y)
				{
					RGB* col = px + y * bitmap.Width;
					for (int x = 0; x != bitmap.Width; ++x, ++col)
					{
						//test_bm.SetPixel(x,y,Color.FromArgb(0xff, col->r, col->g, col->b));
						int distsq = RGB.DistanceSq(target, *col);
						if (distsq < distance)
						{
							pt.X = x;
							pt.Y = y;
							distance = distsq;
						}
					}
				}
				//test_bm.Save("D:/deleteme/screen_grap2.bmp");
			}
            bitmap.UnlockBits(bm);
			distance = (int)Math.Sqrt(distance);
			return pt;
		}

		// Search the search area for the bobber
		private bool FindBobberPosition(ref Point position, RGB target, bool refine)
		{
			Rectangle search_rect = (refine) ? m_props.SmallSearchArea(position) : m_props.SearchArea;
			Bitmap    bitmap      = (refine) ? m_small_grab : m_large_grab;
			CaptureScreen(ref bitmap, search_rect);
			if (refine) { m_small_grab = bitmap; } else { m_large_grab = bitmap; }
			
			int distance;
			Point pt = FindTargetColour(bitmap, target, out distance);
			position = new Point(pt.X + search_rect.X, pt.Y + search_rect.Y);
			Cursor.Position = position;
			return distance < m_props.ColourTolerence;
		}

		// Look for bobber movement
		private bool BobberMoved(Point position, RGB target)
		{
			// Grab a small area around the bobber
			Rectangle search_rect = m_props.SmallSearchArea(position);
			CaptureScreen(ref m_small_grab, search_rect);

			int distance;
			Point pt = FindTargetColour(m_small_grab, target, out distance);
			pt.X += search_rect.X;
			pt.Y += search_rect.Y;
			int dist = (int)Util.Distance(position, pt);
			if (dist > m_max_delta) m_max_delta = dist;

			Cursor.Position = pt;
			Msg("Position delta: "+dist+" (max: "+m_max_delta+")\nColour delta: "+distance+"\nRemaining time: "+((m_cast_time + m_props.MaxFishCycle - Environment.TickCount)/1000), true);
			return dist > m_props.MoveThreshold;
		}

		// Do stuff while paused
		private void WhilePaused()
		{
			if (Win32.KeyDown(Keys.ControlKey))
			{
				m_props.TargetColour = ReadPixelColour();
				Msg("Target colour: "+m_props.TargetColour.R+","+m_props.TargetColour.G+","+m_props.TargetColour.B, true);
			}
		}
	}
}
