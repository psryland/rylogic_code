using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using pr.directshow;
using Microsoft.Win32.SafeHandles;

namespace pr.gfx
{
	public class Video :IDisposable
	{
		public enum EPlayState { Init, Running, Paused, Stopped, Cleanup }

		private IFilterGraph2    m_filter_graph      ;
		private IMediaControl    m_media_ctrl        ;
		private IMediaEvent      m_media_event       ;
		private IMediaPosition   m_media_position    ;
		private IVideoWindow     m_video_window      ;
		private IBasicVideo      m_basic_video       ;
		private IBasicAudio      m_basic_audio       ;
		private ISampleGrabber   m_samp_grabber      ;       // Used to grab snapshots
		private Thread           m_media_event_thread;       // The thread that processes media events
		private ManualResetEvent m_event             ;       // Event signalled by direct show when things happen
		private readonly object  m_shutdown_lock     ;
		private readonly object  m_controls_lock     ;
		private volatile bool    m_shutdown          ;
		private string           m_file              ;       // The video/audio file currently loaded
		private EPlayState       m_play_state        ;
		#if DEBUG
		private DsROTEntry       m_ds_rot            ;       // Allow you to "Connect to remote graph" from GraphEdit
		#endif

		/// <summary>Called when a video file is loaded successfully</summary>
		public event Action<Video, string> FileLoaded;

		/// <summary>Called when the video starts/stops/resets</summary>
		public event Action<Video, EPlayState> PlayStateChanged;

		public Video()
		{
			m_shutdown_lock = new object();
			m_controls_lock = new object();
			m_shutdown = false;
			m_play_state = EPlayState.Init;
		}
		public Video(string file) :this()
		{
			LoadFile(file);
		}
		~Video()
		{
			CloseInterfaces();
		}
		public void Dispose()
		{
			CloseInterfaces();
		}

		/// <summary>Load a video/audio file and prepare to play it</summary>
		public void LoadFile(string file)
		{
			try
			{
				if (m_filter_graph != null) throw new Exception("Reusing this Video object is not allowed");

				m_file = file;
				m_filter_graph = new FilterGraph() as IFilterGraph2;
				if (m_filter_graph == null) throw new Exception("failed to create direct show filter graph");

				// Have the filter graph construct the appropriate graph automatically
				DsError.ThrowExceptionForHR(m_filter_graph.RenderFile(file, null));

				#if DEBUG
				// Allows you to view the graph with GraphEdit File/Connect
				m_ds_rot = new DsROTEntry(m_filter_graph);
				#endif

				// Grab some other interfaces
				m_media_event    = m_filter_graph as IMediaEvent;
				m_media_ctrl     = m_filter_graph as IMediaControl;
				m_media_position = m_filter_graph as IMediaPosition;
				if (m_media_event    == null) throw new Exception("failed to obtain a direct show IMediaEvent interface");
				if (m_media_ctrl     == null) throw new Exception("failed to obtain a direct show IMediaControl interface");
				if (m_media_position == null) throw new Exception("failed to obtain a direct show IMediaPosition interface");

				// Grab optional interfaces
				m_video_window   = m_filter_graph as IVideoWindow;
				m_basic_video    = m_filter_graph as IBasicVideo;
				m_basic_audio    = m_filter_graph as IBasicAudio;

				// If this is an audio-only clip, get_Visible() won't work. Also, if this
				// video is encoded with an unsupported codec, we won't see any video,
				// although the audio will work if it is of a supported format.
				if (m_video_window != null)
				{
					const int E_NOINTERFACE = unchecked((int)0x80004002);

					// Use put Visible since we want to hide the video window
					// till we're ready to show it anyway
					int hr = m_video_window.put_Visible(OABool.False);
					if (hr == E_NOINTERFACE) m_video_window = null;
					else DsError.ThrowExceptionForHR(hr);
				}

				// Get the event handle the graph will use to signal when events occur, wrap it in a ManualResetEvent
				IntPtr media_event;
				DsError.ThrowExceptionForHR(m_media_event.GetEventHandle(out media_event));
				m_event = new ManualResetEvent(false){SafeWaitHandle = new SafeWaitHandle(media_event, false)};

				// Create a new thread to wait for events
				m_media_event_thread = new Thread(MediaEventWait){Name = "Media Event Thread"};
				m_media_event_thread.Start();

				if (FileLoaded != null) FileLoaded(this, file);
			}
			catch { Dispose(); throw; }
			#if DEBUG
			GC.Collect(); // Double check to make sure we aren't releasing something important.
			GC.WaitForPendingFinalizers();
			#endif
		}

		// Wait for direct show events to happen. This approach uses waiting on an event handle.
		// The nice thing about doing it this way is that you aren't in the windows message
		// loop, and don't have to worry about re-entrency or taking too long.  Plus, being
		// in a class as we are, we don't have access to the message loop.
		// Alternately, you can receive your events as windows messages.  See IMediaEventEx.SetNotifyWindow.
		private void MediaEventWait()
		{
			// Returned when GetEvent is called but there are no events
			const int E_ABORT = unchecked((int)0x80004004);

			// Read the events
			for (;;)
			{
				lock (m_shutdown_lock) if (m_shutdown) break; // shutdown the thread when flagged

				IntPtr p1, p2; EventCode ec;
				int hr = m_media_event.GetEvent(out ec, out p1, out p2, 0);
				if (hr == E_ABORT) { m_event.WaitOne(-1, true); continue; }// Wait for an event
				else if (hr < 0) DsError.ThrowExceptionForHR(hr);// If the error wasn't due to running out of events

				//Debug.WriteLine("Video Event: " + ec); // Write the event name to the debug window

				// If the clip is finished playing
				if (ec == EventCode.Complete)
					Stop(); // Call Stop() to set state

				// Release any resources the message allocated
				DsError.ThrowExceptionForHR(m_media_event.FreeEventParams(ec, p1, p2));
			}
		}

		/// <summary>Attach this video to a window</summary>
		public void AttachToWindow(IntPtr handle)
		{
			if (m_video_window == null) return;

			if (handle != IntPtr.Zero)
			{
				DsError.ThrowExceptionForHR(m_video_window.put_Owner(handle));        // Parent the window
				DsError.ThrowExceptionForHR(m_video_window.put_WindowStyle(WindowStyle.Child|WindowStyle.ClipChildren|WindowStyle.ClipSiblings)); // Set the window style
				DsError.ThrowExceptionForHR(m_video_window.put_MessageDrain(handle));// Set the destination for messages
				DsError.ThrowExceptionForHR(m_video_window.put_Visible(OABool.True)); // Make the window visible
			}
			else
			{
				DsError.ThrowExceptionForHR(m_video_window.put_Visible(OABool.False)); // Make the window invisible
				DsError.ThrowExceptionForHR(m_video_window.put_MessageDrain(handle));// Set the destination for messages
				DsError.ThrowExceptionForHR(m_video_window.put_Owner(handle));         // Unparent it
			}
		}

		/// <summary>Returns true if this an audio-only file (no video component)
		/// Audio-only files have no video interfaces.  This might also be a file
		/// whose video component uses an unknown video codec.</summary>
		public bool AudioOnly
		{
			get { return m_video_window == null; }
		}

		/// <summary>Gets the native window size of the loaded video, or 0,0</summary>
		public Size NativeSize
		{
			get
			{
				if (m_basic_video == null) return Size.Empty;
				int w,h; m_basic_video.GetVideoSize(out w, out h);
				return new Size(w,h);
			}
		}

		/// <summary>Get/Set the area within the window to display the video</summary>
		public Rectangle DisplayArea
		{
			get
			{
				if (m_video_window == null) return Rectangle.Empty;
				int l,t,w,h; DsError.ThrowExceptionForHR(m_video_window.GetWindowPosition(out l, out t, out w, out h));
				return new Rectangle(l,t,w,h);
			}
			set
			{
				lock (m_controls_lock)
				{
					if (m_video_window == null) return;
					DsError.ThrowExceptionForHR(m_video_window.SetWindowPosition(value.Left, value.Top, value.Width, value.Height));
				}
			}
		}

		/// <summary>Get/Set full screen mode</summary>
		public bool FullScreen
		{
			get
			{
				if (m_video_window == null) return false;
				OABool fullscreen_mode;
				DsError.ThrowExceptionForHR(m_video_window.get_FullScreenMode(out fullscreen_mode));
				return fullscreen_mode == OABool.True;
			}
			set
			{
				if (m_video_window == null) return;
				if (value)
				{
					DsError.ThrowExceptionForHR(m_video_window.put_FullScreenMode(OABool.True));       // Switch to full-screen mode
				}
				else
				{
					DsError.ThrowExceptionForHR(m_video_window.put_FullScreenMode(OABool.False));      // Switch to windowed mode
					DsError.ThrowExceptionForHR(m_video_window.SetWindowForeground(OABool.True));      // Reset video window
				}
			}
		}

		/// <summary>Return the currently playing file name</summary>
		public string FileName
		{
			get { return m_file; }
		}

		/// <summary>Return the current play state of the video</summary>
		public EPlayState PlayState
		{
			get { return m_play_state; }
			private set
			{
				if (m_play_state == value) return;
				m_play_state = value;
				if (PlayStateChanged != null) PlayStateChanged(this, m_play_state);
			}
		}

		/// <summary>Starting playing the video/audio file</summary>
		public void Play()
		{
			lock (m_controls_lock)
			{
				if (PlayState != EPlayState.Paused && PlayState != EPlayState.Stopped) return;
				if (m_media_ctrl != null) DsError.ThrowExceptionForHR(m_media_ctrl.Run());
				PlayState = EPlayState.Running;
			}
		}

		/// <summary>Pause the video/audio file</summary>
		public void Pause()
		{
			lock (m_controls_lock)
			{
				if (PlayState != EPlayState.Running && PlayState != EPlayState.Stopped) return;
				if (m_media_ctrl != null) DsError.ThrowExceptionForHR(m_media_ctrl.Pause());
				PlayState = EPlayState.Paused;
			}
		}

		/// <summary>Stop the video/audio file</summary>
		public void Stop()
		{
			lock (m_controls_lock)
			{
				if (PlayState != EPlayState.Running && PlayState != EPlayState.Paused && PlayState != EPlayState.Init) return;
				if (m_media_ctrl != null) DsError.ThrowExceptionForHR(m_media_ctrl.Stop());
				PlayState = EPlayState.Stopped;
			}
		}

		/// <summary>Reset the clip back to the beginning</summary>
		public void Rewind()
		{
			lock (m_controls_lock)
			{
				if (m_media_position != null) DsError.ThrowExceptionForHR(m_media_position.put_CurrentPosition(0));
			}
		}

		/// <summary>Return the length of time for the video/audio file</summary>
		public double Duration
		{
			get { double duration = 0.0; if (m_media_position != null) DsError.ThrowExceptionForHR(m_media_position.get_Duration(out duration)); return duration; }
		}

		/// <summary>Set the position in the video</summary>
		public double Position
		{
			get { double current = 0.0; if (m_media_position != null) DsError.ThrowExceptionForHR(m_media_position.get_CurrentPosition(out current)); return current; }
			set
			{
				lock (m_controls_lock)
				{
					value = Math.Max(0, Math.Min(Duration, value));
					if (m_media_position != null) DsError.ThrowExceptionForHR(m_media_position.put_CurrentPosition(value));
					Pause();
				}
			}
		}

		/// <summary>Get/Set the playback rate of the video</summary>
		public double Rate
		{
			get
			{
				if (m_media_position == null) return 0.0;
				double rate; DsError.ThrowExceptionForHR(m_media_position.get_Rate(out rate));
				return rate;
			}
			set
			{
				lock (m_controls_lock)
				{
					if (m_media_position == null) return;
					DsError.ThrowExceptionForHR(m_media_position.put_Rate(value));
				}
			}
		}

		/// <summary>Get/Set the volume for the audio [0,100]</summary>
		public int Volume
		{
			// Volume is logarithmic, -10000 = -10^4 = silent, 0 ~= -10^0 = max
			// So convert 0->100 to 4->0, then use -10^value as the volume
			get
			{
				if (m_basic_audio == null) return 0;
				int vol; if (m_basic_audio.get_Volume(out vol) == -1) return 0; // Fail quietly if this is a video-only media file
				return vol == 0 ? 100 : (int)(100.0 * (4.0 - Math.Log10(-vol)) / 4.0);
			}
			set
			{
				lock (m_controls_lock)
				{
					if (m_basic_audio == null) return;
					m_basic_audio.put_Volume((int)-Math.Pow(10, 4.0 - 4.0 * value / 100.0));
				}
			}
		}

		/// <summary>Setup the filter graph for grabbing snapshots</summary>
		public void EnableGrabbing()
		{
			ICaptureGraphBuilder2 icgb2 = null;
			try
			{
				// Get a ICaptureGraphBuilder2 to help build the graph
				// Link the ICaptureGraphBuilder2 to the IFilterGraph2
				icgb2 = new CaptureGraphBuilder2() as ICaptureGraphBuilder2;
				if (icgb2 == null) throw new Exception("failed to create direct show CaptureGraphBuilder2");
				DsError.ThrowExceptionForHR(icgb2.SetFiltergraph(m_filter_graph));

				// Get the SampleGrabber interface
				m_samp_grabber = (ISampleGrabber)new SampleGrabber();

				{// Set the media type to Video/RBG24
					AMMediaType media = new AMMediaType{majorType = MediaType.Video, subType = MediaSubType.RGB24, formatType = FormatType.VideoInfo};
					try { DsError.ThrowExceptionForHR(m_samp_grabber.SetMediaType(media)); }
					finally { DsUtils.FreeAMMediaType(media); }
				}

				// Configure the sample grabber
				DsError.ThrowExceptionForHR(m_samp_grabber.SetBufferSamples(true));

				// Add the sample graber to the filter graph
				IBaseFilter grab_filter = (IBaseFilter)m_samp_grabber;
				DsError.ThrowExceptionForHR(m_filter_graph.AddFilter(grab_filter, "DS.NET Grabber"));
			}
			finally { if (icgb2 != null) { Marshal.ReleaseComObject(icgb2); } }
		}

		/// <summary>Screen grab the current image. Graph can be paused, playing, or stopped</summary>
		public Bitmap Snapshot()
		{
			// Grab a snapshot of the most recent image played.
			// Returns A pointer to the raw pixel data.
			// Caller must release this memory with Marshal.FreeCoTaskMem when it is no longer needed.
			IntPtr ip = IntPtr.Zero;
			try
			{
				// Read the buffer size
				int bufsize = 0;
				DsError.ThrowExceptionForHR(m_samp_grabber.GetCurrentBuffer(ref bufsize, IntPtr.Zero));

				// Allocate the buffer and read it
				ip = Marshal.AllocCoTaskMem(bufsize);
				DsError.ThrowExceptionForHR(m_samp_grabber.GetCurrentBuffer(ref bufsize, ip));

				// We know the Bits Per Pixel is 24 (3 bytes) because
				// we forced it to be with sampGrabber.SetMediaType()
				Size native_size = NativeSize;
				int stride = bufsize / native_size.Height;
				Debug.Assert((bufsize % native_size.Height) == 0);

				return new Bitmap(
					native_size.Width,
					native_size.Height,
					-stride,
					PixelFormat.Format24bppRgb,
					(IntPtr)(ip.ToInt32() + bufsize - stride));
			}
			finally { if (ip != IntPtr.Zero) Marshal.FreeCoTaskMem(ip); }
		}

		/// <summary>Cleanup the video resources</summary>
		private void CloseInterfaces()
		{
			Stop();
			AttachToWindow(IntPtr.Zero);
			PlayState = EPlayState.Cleanup;

			lock (m_shutdown_lock) m_shutdown = true;
			if (m_event != null) m_event.Set(); // Release the thread
			m_event = null;

			// Wait for the thread to end
			if (m_media_event_thread != null) m_media_event_thread.Join();
			m_media_event_thread = null;

			if (m_samp_grabber != null)
				Marshal.ReleaseComObject(m_samp_grabber);

			m_media_ctrl = null;
			m_media_position = null;
			m_samp_grabber = null;
			m_video_window = null;
			m_basic_video = null;
			m_basic_audio = null;

			#if DEBUG
			if (m_ds_rot != null) m_ds_rot.Dispose();
			m_ds_rot = null;
			#endif

			if (m_filter_graph != null) Marshal.ReleaseComObject(m_filter_graph);
			m_filter_graph = null;

			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}
}
