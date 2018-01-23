using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Graphix;
using Rylogic.Maths;
using Timer = System.Windows.Forms.Timer;

namespace Rylogic.Gui
{
	public class ViewVideoControl :UserControl
	{
		private class RemoteRef
		{
			public Video m_video;
			public Timer m_timer;
		}

		private readonly Form m_remote;
		private Video     m_video;
		private ImageList m_image_list;
		private Point     m_mouse_loc;
		private bool      m_fit_to_window;

		/// <summary>Called when the volume for the currently loaded video is changed</summary>
		public event Action<ViewVideoControl, int> VolumeChanged;//(this, new_volume)

		/// <summary>Called when the remote changes visibility</summary>
		public event Action<ViewVideoControl, bool> RemoteVisibleChanged;//(this, visible)

		public ViewVideoControl()
		{
			InitializeComponent();
			
			m_remote = RemoteCreate();
			RemoteAutoHidePeriod = 1500;
			RemoteAutoHideSpeed = 400;
			FitToWindow = true;

			MouseMove += (s,e)=>
			{
				if (ActivateRemote() && !m_remote.Visible) m_remote.Show(this);
			};
			
			VisibleChanged += (s,e)=>
			{
				if (!Visible) m_remote.Hide();
			};

			Load += (s,e)=>
			{
				// If this fires you probably need an "if (ParentForm == null) return" somewhere
				Debug.Assert(ParentForm != null, "The control is being used before it has a parent");
				ParentForm.Move += (o,a)=>{ PositionRemote(); };
				PositionRemote();
				//m_remote.Show(ParentForm);
			};
			Disposed += (s,e)=>
			{
				m_remote.Close();
			};
		}

		/// <summary>Get/Set the video displayed in this control</summary>
		public Video Video
		{
			get { return m_video; }
			set
			{
				if (m_video == value) return;
				Debug.Assert(m_video == null || m_video.PlayState != Video.EPlayState.Cleanup, "Do not dispose the old video before it is removed from the control");
				RemoteAttachVideo(value);

				if (m_video != null)
				{
					m_video.Stop();
					m_video.AttachToWindow(IntPtr.Zero); // detach from our window
					// Don't dispose here, since we don't own the video
				}
				m_video = value;
				if (m_video != null)
				{
					m_video.AttachToWindow(Handle);
					SetDisplayArea();
					m_video.Position = 0;
					m_video.Stop();
				}
			}
		}

		/// <summary>Get/Set the visibility of the remote</summary>
		public bool RemoteVisible
		{
			get { return m_remote.Visible; }
			set { if (RemoteVisible == value) return; if (value) {m_remote.Show(this);} else {m_remote.Hide(); BringToFront();} }
		}

		/// <summary>Zoom the image so that it maximises its size in the client area</summary>
		public bool FitToWindow
		{
			get { return m_fit_to_window; }
			set
			{
				m_fit_to_window = value;
				SetDisplayArea();
			}
		}

		/// <summary>Get/Set the auto hide period for the remote (in ms)</summary>
		public int RemoteAutoHidePeriod {get;set;}

		/// <summary>Get/Set the rate at which the remote fades out (in ms)</summary>
		public int RemoteAutoHideSpeed {get;set;}

		/// <summary>Create the remote form</summary>
		private Form RemoteCreate()
		{
			Rectangle rect = ClientRectangle;
			const int height = 24, btn_width = 26, lbl_width = 100;

			Timer timer = new Timer {Interval = 10, Tag = Environment.TickCount};
			timer.Tick += (s,e)=>{ RemoteStep(); };

			// use tag to reference the last video the remote was configured with and the timer used for fading
			Form remote = new Form
			{
				StartPosition = FormStartPosition.Manual,
				FormBorderStyle = FormBorderStyle.None,
				Size = new Size(rect.Width - 2, height + 4),
				ShowInTaskbar = false,
				MinimumSize = new Size(1, 1),
				BackColor = Color.Black,
				Tag = new RemoteRef {m_video = null, m_timer = timer}
			};
			remote.SuspendLayout();

			Button btn_play = new Button{Name="btn_play", ImageList=m_image_list, ImageKey="Play", AutoSize=false, Location=new Point(0*(2+btn_width),2), Size=new Size(btn_width,height), TabIndex=0, UseVisualStyleBackColor=true, Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom};
			btn_play.Click += (s,e)=>{ if (btn_play.ImageKey == "Play") Video.Play(); else Video.Stop(); };
			remote.Controls.Add(btn_play);

			Button btn_volume = new Button{Name="btn_volume", ImageList=m_image_list, ImageKey="Speaker", AutoSize=false, Location=new Point(1*(2+btn_width),2), Size=new Size(26,height), TabIndex=1, UseVisualStyleBackColor=true, Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom};
			btn_volume.Click += (s,e)=>
			{
				if (((RemoteRef)remote.Tag).m_video == null) return;
				Video video = ((RemoteRef)remote.Tag).m_video;
				int volume = video.Volume;
				TrackBar vol_track = new TrackBar{Dock=DockStyle.Fill, Minimum=0, Maximum=100, Value=volume, Orientation=Orientation.Vertical};
				Form     vol       = new Form{StartPosition=FormStartPosition.Manual, FormBorderStyle=FormBorderStyle.None, Location=remote.Location+(Size)btn_volume.Location+new Size(0,-(height + vol_track.Height)), Size=new Size(25,70), ShowInTaskbar=false, MinimumSize=new Size(1,1)};
				vol.Controls.Add(vol_track);
				vol_track.MouseLeave += (o,a)=>{ vol.Close(); };
				vol_track.ValueChanged += (o,a)=>{ video.Volume = vol_track.Value; };
				vol.ShowDialog(remote);
				int new_volume = video.Volume;
				if (new_volume != volume) VolumeChanged?.Invoke(this, new_volume);
			};
			remote.Controls.Add(btn_volume);

			TrackBar track = new TrackBar
			{
				Name = "track",
				AutoSize = false,
				Location = new Point(2*(2 + btn_width), 2),
				Size = new Size(remote.ClientSize.Width - 2*(2 + btn_width) - lbl_width - 4, height),
				TabIndex = 2,
				TickStyle = TickStyle.None,
				Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
				LargeChange = 100,
				SmallChange = 1,
				Tag = true
			};
			track.ValueChanged += (s,e)=>{ if ((bool)track.Tag) Video.Position = track.Value / 100f; };
			remote.Controls.Add(track);

			Label lbl_time = new Label
			{
				Name = "lbl_time",
				AutoSize = true,
				Location = new Point(remote.ClientSize.Width - lbl_width - 2, (remote.ClientSize.Height - 14)/2),
				Size = new Size(lbl_width, 14),
				TabIndex = 3,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Anchor = AnchorStyles.Right | AnchorStyles.Top,
				Text = "00:00.00/00:00.00",
			};
			remote.Controls.Add(lbl_time);

			remote.ResumeLayout(false);
			remote.VisibleChanged += (s,e)=>{ timer.Enabled = remote.Visible; if (remote.Visible) remote.BringToFront(); RemoteVisibleChanged?.Invoke(this, remote.Visible); };
			remote.FormClosing    += (s,e)=>{ if (e.CloseReason == CloseReason.UserClosing) {m_remote.Hide(); e.Cancel = true;} };
			return remote;
		}

		/// <summary>Configure the remote using 'video'</summary>
		private void RemoteAttachVideo(Video video)
		{
			// Function that changes the play button as the play state is changed
			Action<Video, Video.EPlayState> update_play_button = (v,s)=>
			{
				Button btn_play = (Button)m_remote.Controls[0];
				switch (s){
				case Video.EPlayState.Init:    btn_play.ImageKey = "Play";  btn_play.Enabled = false; break;
				case Video.EPlayState.Running: btn_play.ImageKey = "Stop";  btn_play.Enabled = true;  break;
				case Video.EPlayState.Paused:  btn_play.ImageKey = "Play";  btn_play.Enabled = true;  break;
				case Video.EPlayState.Stopped: btn_play.ImageKey = "Play";  btn_play.Enabled = true;  break;
				case Video.EPlayState.Cleanup: btn_play.ImageKey = "Play";  btn_play.Enabled = false; break;
				default: throw new ArgumentOutOfRangeException("s");
				}
			};

			// If the video has changed, remove old handlers and attach new ones
			Video old_video = ((RemoteRef)m_remote.Tag).m_video;
			if (old_video != null && old_video != video)
				old_video.PlayStateChanged -= update_play_button;

			((RemoteRef)m_remote.Tag).m_video = video;

			if (video != null)
			{
				foreach (Control c in m_remote.Controls) c.Enabled = true;

				video.PlayStateChanged += update_play_button;

				Label lbl_time = (Label)m_remote.Controls[3];
				DateTime dur = new DateTime((long)(video.Duration * 10000000));
				if (dur.Hour != 0) lbl_time.Text = "00:00:00" + "/" + dur.ToString("HH:mm:ss");
				else               lbl_time.Text = "00:00.00" + "/" + dur.ToString("mm:ss.ff");
				
				TrackBar track = (TrackBar)m_remote.Controls[2];
				track.SetRange(0, (int)(video.Duration * 100));
				
				RemoteStep();
			}
			else
			{
				foreach (Control c in m_remote.Controls) c.Enabled = false;
			}
		}
		
		/// <summary>Called whenever the remote timer ticks</summary>
		private void RemoteStep()
		{
			Video video = ((RemoteRef)m_remote.Tag).m_video;
			Timer timer = ((RemoteRef)m_remote.Tag).m_timer;
			if (video == null || timer == null) return;
			
			// Update the progress track position
			TrackBar track = (TrackBar)m_remote.Controls[2];
			track.Tag = false;
			track.Value = (int)(video.Position * 100f);
			track.Tag = true;

			// Update the time label
			DateTime pos = new DateTime((long)(video.Position * 10000000));
			DateTime dur = new DateTime((long)(video.Duration * 10000000));
			Label lbl_time = (Label)m_remote.Controls[3];
			if (dur.Hour != 0) lbl_time.Text = pos.ToString("HH:mm:ss") + "/" + dur.ToString("HH:mm:ss");
			else               lbl_time.Text = pos.ToString("mm:ss.ff") + "/" + dur.ToString("mm:ss.ff");
			
			// If the mouse is within the remote bounds and moving push out the hide time
			if (ActivateRemote())
			{
				timer.Tag = Environment.TickCount + RemoteAutoHidePeriod;
				m_mouse_loc = MousePosition;
			}
			
			// Fade out, then disable
			float dt = unchecked((int)timer.Tag - Environment.TickCount);
			m_remote.Opacity = Math_.Clamp(dt/RemoteAutoHideSpeed, 0f, 1f);
			
			if (dt <= 0) m_remote.Hide();
		}

		/// <summary>Position the remote relative to the control</summary>
		private void PositionRemote()
		{
			// Don't position the remote until it has a parent form, doing so causes it to be created
			// before it has a parent which means the Load event fires with ParentForm == null
			if (ParentForm == null) return;
			Rectangle rect = ClientRectangle;
			m_remote.Location = PointToScreen(new Point(rect.Left + 1, rect.Bottom - m_remote.Height - 1));
			m_remote.Size     = new Size(rect.Width - 2, m_remote.Height);
		}

		/// <summary>Returns true if the mouse has moved from 'm_mouse_loc' within the bounds of the remote</summary>
		private bool ActivateRemote()
		{
			Point pt = m_remote.PointToClient(MousePosition);
			return m_remote.ClientRectangle.Contains(pt) &&
			       (Math.Abs(MousePosition.X - m_mouse_loc.X) > SystemInformation.DragSize.Width ||
			        Math.Abs(MousePosition.Y - m_mouse_loc.Y) > SystemInformation.DragSize.Height);
		}

		/// <summary>Set the display area for the video</summary>
		private void SetDisplayArea()
		{
			if (Video == null) return;

			Rectangle client_rect = ClientRectangle;
			Rectangle native_rect = new Rectangle(Point.Empty, m_video.NativeSize);
			Rectangle rect = native_rect;

			if (FitToWindow)
			{
				float client_aspect = (float)client_rect.Width / client_rect.Height;
				float native_aspect = (float)native_rect.Width / native_rect.Height;
				if (client_aspect > native_aspect) { rect.Height = client_rect.Height; rect.Width  = (int)(client_rect.Height * native_aspect); }
				else                               { rect.Width  = client_rect.Width ; rect.Height = (int)(client_rect.Width  / native_aspect); }
			}

			rect.Offset((client_rect.Width  - rect.Width)/2, (client_rect.Height - rect.Height)/2);
			rect.Intersect(client_rect);
			m_video.DisplayArea = rect;
		}

		/// <summary>Get/Set additional keys that should be considered input keys. e.g. Keys.Left, Keys.Right, etc</summary>
		public Keys[] AdditionalInputKeys {get;set;}

		/// <summary>Include client input keys</summary>
		protected override bool IsInputKey(Keys key)
		{
			if (AdditionalInputKeys != null) foreach (Keys k in AdditionalInputKeys) if (k == key) return true;
			return base.IsInputKey(key);
		}

		/// <summary>Update the video window when the size changes</summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (Video != null) SetDisplayArea();
			if (m_remote != null) PositionRemote();
		}

		///// <summary>Handle notification messages</summary>
		//protected override void WndProc(ref Message m)
		//{
		//    if (Video != null)
		//    {
		//        if (m.Msg == Video.WM_VideoNotify)
		//            Video.ProcessGraphEvents();

		//        // Pass this message to the video window for notification of system changes
		//        Video.WndProc(ref m);
		//    }
		//    base.WndProc(ref m);
		//}

		#region Component Designer generated code
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			// Don't dispose the attached video here, since we don't own it
			Debug.Assert(Video == null, "Should not be disposing this control with the video still attached, set Video to null before disposing. (You probably want to dispose the video too)");
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewVideoControl));
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Magenta;
			this.m_image_list.Images.SetKeyName(0, "Play");
			this.m_image_list.Images.SetKeyName(1, "Stop");
			this.m_image_list.Images.SetKeyName(2, "Speaker");
			// 
			// ViewVideoControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "ViewVideoControl";
			this.Size = new System.Drawing.Size(309, 232);
			this.ResumeLayout(false);
		}
		#endregion
	}
}
