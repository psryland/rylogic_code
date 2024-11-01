using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Rylogic.DirectShow;

namespace Rylogic.Gfx
{
	public class Video :IDisposable
	{
		private readonly object  m_shutdown_lock;
		private readonly object  m_controls_lock;
		private volatile bool    m_shutdown;
		private EPlayState       m_play_state;
		#if DEBUG
		private DsROTEntry? m_ds_rot; // Allow you to "Connect to remote graph" from GraphEdit
		#endif

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
		public void Dispose()
		{
			MonitorMediaEvents = false;
			CloseInterfaces();
		}

		/// <summary>DirectX Filter graph</summary>
		private IFilterGraph2? FilterGraph { get; set; }

		/// <summary>Media control</summary>
		private IMediaControl? MediaCtrl { get; set; }

		/// <summary></summary>
		private IMediaEvent? MediaEvent { get; set; }

		/// <summary></summary>
		private IMediaPosition? MediaPosition { get; set; }

		/// <summary></summary>
		private IVideoWindow? VideoWindow { get; set; }

		/// <summary></summary>
		private IBasicVideo? BasicVideo { get; set; }

		/// <summary></summary>
		private IBasicAudio? BasicAudio { get; set; }

		/// <summary></summary>
		private ISampleGrabber? SampGrabber { get; set; }

		/// <summary>Called when a video file is loaded successfully</summary>
		public event Action<Video, string>? FileLoaded;

		/// <summary>Called when the video starts/stops/resets</summary>
		public event Action<Video, EPlayState>? PlayStateChanged;

		/// <summary>Load a video/audio file and prepare to play it</summary>
		public void LoadFile(string file)
		{
			try
			{
				if (FilterGraph != null)
					throw new Exception("Reusing this Video object is not allowed");

				FileName = file;
				FilterGraph = new FilterGraph() as IFilterGraph2;
				if (FilterGraph == null)
					throw new Exception("failed to create direct show filter graph");

				// Have the filter graph construct the appropriate graph automatically
				DsError.ThrowExceptionForHR(FilterGraph.RenderFile(file, string.Empty));

				// Allows you to view the graph with GraphEdit File/Connect
				#if DEBUG
				m_ds_rot = new DsROTEntry(FilterGraph);
				#endif

				// Grab some other interfaces
				MediaEvent    = FilterGraph as IMediaEvent;
				MediaCtrl     = FilterGraph as IMediaControl;
				MediaPosition = FilterGraph as IMediaPosition;
				if (MediaEvent    == null) throw new Exception("failed to obtain a direct show IMediaEvent interface");
				if (MediaCtrl     == null) throw new Exception("failed to obtain a direct show IMediaControl interface");
				if (MediaPosition == null) throw new Exception("failed to obtain a direct show IMediaPosition interface");

				// Grab optional interfaces
				VideoWindow   = FilterGraph as IVideoWindow;
				BasicVideo    = FilterGraph as IBasicVideo;
				BasicAudio    = FilterGraph as IBasicAudio;

				// If this is an audio-only clip, get_Visible() won't work. Also, if this
				// video is encoded with an unsupported codec, we won't see any video,
				// although the audio will work if it is of a supported format.
				if (VideoWindow != null)
				{
					const int E_NOINTERFACE = unchecked((int)0x80004002);

					// Use put Visible since we want to hide the video window
					// till we're ready to show it anyway
					int hr = VideoWindow.put_Visible(OABool.False);
					if (hr == E_NOINTERFACE) VideoWindow = null;
					else DsError.ThrowExceptionForHR(hr);
				}

				// Create a new thread to wait for events
				MonitorMediaEvents = true;

				// Notify
				FileLoaded?.Invoke(this, file);
			}
			catch { Dispose(); throw; }
			#if DEBUG
			GC.Collect(); // Double check to make sure we aren't releasing something important.
			GC.WaitForPendingFinalizers();
			#endif
		}

		/// <summary>Start/Stop a background thread that monitors for media events</summary>
		private bool MonitorMediaEvents
		{
			get => m_media_event_thread != null;
			set
			{
				if (MonitorMediaEvents == value) return;
				if (m_media_event_thread != null)
				{
					// Release the thread
					if (m_event != null)
						m_event.Set();

					// Wait for the thread to end
					m_media_event_thread.Join();
				}
				m_media_event_thread = value ? new Thread(MediaEventWait) { Name = "Media Event Thread" } : null;
				if (m_media_event_thread != null)
				{
					// Get the event handle the graph will use to signal when events occur, wrap it in a ManualResetEvent
					DsError.ThrowExceptionForHR(MediaEvent?.GetEventHandle(out var media_event) ?? throw new Exception("MediaEvent interface missing"));
					m_event = new ManualResetEvent(false) { SafeWaitHandle = new SafeWaitHandle(media_event, false) };

					m_media_event_thread.Start();
				}

				// Handler
				void MediaEventWait()
				{
					// Wait for direct show events to happen. This approach uses waiting on an event handle.
					// The nice thing about doing it this way is that you aren't in the windows message
					// loop, and don't have to worry about reentrancy or taking too long. Plus, being
					// in a class as we are, we don't have access to the message loop.
					// Alternately, you can receive your events as windows messages.  See IMediaEventEx.SetNotifyWindow.

					// Returned when GetEvent is called but there are no events
					const int E_ABORT = unchecked((int)0x80004004);

					// Read the events
					for (; ; )
					{
						lock (m_shutdown_lock)
							if (m_shutdown)
								break; // shutdown the thread when flagged

						int hr = MediaEvent?.GetEvent(out var ec, out var p1, out var p2, 0) ?? throw new Exception("No MediaEvent interface");
						if (hr == E_ABORT)
						{
							// Wait for an event
							if (m_event == null) throw new Exception("m_event is gone?!");
							m_event.WaitOne(-1, true);
							continue;
						}
						else if (hr < 0)
						{
							// If the error wasn't due to running out of events
							DsError.ThrowExceptionForHR(hr);
						}

						//Debug.WriteLine("Video Event: " + ec); // Write the event name to the debug window

						// If the clip is finished playing
						if (ec == EventCode.Complete)
							Stop(); // Call Stop() to set state

						// Release any resources the message allocated
						DsError.ThrowExceptionForHR(MediaEvent.FreeEventParams(ec, p1, p2));
					}
				}
			}
		}
		private Thread? m_media_event_thread; // The thread that processes media events
		private ManualResetEvent? m_event; // Event signalled by direct show when things happen


		/// <summary>Attach this video to a window</summary>
		public void AttachToWindow(IntPtr handle)
		{
			if (VideoWindow == null) return;

			if (handle != IntPtr.Zero)
			{
				DsError.ThrowExceptionForHR(VideoWindow.put_Owner(handle));        // Parent the window
				DsError.ThrowExceptionForHR(VideoWindow.put_WindowStyle(WindowStyle.Child|WindowStyle.ClipChildren|WindowStyle.ClipSiblings)); // Set the window style
				DsError.ThrowExceptionForHR(VideoWindow.put_MessageDrain(handle));// Set the destination for messages
				DsError.ThrowExceptionForHR(VideoWindow.put_Visible(OABool.True)); // Make the window visible
			}
			else
			{
				DsError.ThrowExceptionForHR(VideoWindow.put_Visible(OABool.False)); // Make the window invisible
				DsError.ThrowExceptionForHR(VideoWindow.put_MessageDrain(handle));// Set the destination for messages
				DsError.ThrowExceptionForHR(VideoWindow.put_Owner(handle));         // Unparent it
			}
		}

		/// <summary>
		/// Returns true if this an audio-only file (no video component)
		/// Audio-only files have no video interfaces.  This might also be a file
		/// whose video component uses an unknown video codec.</summary>
		public bool AudioOnly => VideoWindow == null;

		/// <summary>Gets the native window size of the loaded video, or 0,0</summary>
		public Size NativeSize
		{
			get
			{
				if (BasicVideo == null) return Size.Empty;
				BasicVideo.GetVideoSize(out var w, out var h);
				return new Size(w,h);
			}
		}

		/// <summary>Get/Set the area within the window to display the video</summary>
		public Rectangle DisplayArea
		{
			get
			{
				if (VideoWindow == null) return Rectangle.Empty;
				DsError.ThrowExceptionForHR(VideoWindow.GetWindowPosition(out var l, out var t, out var w, out var h));
				return new Rectangle(l,t,w,h);
			}
			set
			{
				lock (m_controls_lock)
				{
					if (VideoWindow == null) return;
					DsError.ThrowExceptionForHR(VideoWindow.SetWindowPosition(value.Left, value.Top, value.Width, value.Height));
				}
			}
		}

		/// <summary>Get/Set full screen mode</summary>
		public bool FullScreen
		{
			get
			{
				if (VideoWindow == null) return false;
				DsError.ThrowExceptionForHR(VideoWindow.get_FullScreenMode(out var fullscreen_mode));
				return fullscreen_mode == OABool.True;
			}
			set
			{
				if (VideoWindow == null) return;
				if (value)
				{
					DsError.ThrowExceptionForHR(VideoWindow.put_FullScreenMode(OABool.True));       // Switch to full-screen mode
				}
				else
				{
					DsError.ThrowExceptionForHR(VideoWindow.put_FullScreenMode(OABool.False));      // Switch to windowed mode
					DsError.ThrowExceptionForHR(VideoWindow.SetWindowForeground(OABool.True));      // Reset video window
				}
			}
		}

		/// <summary>Return the currently playing file name</summary>
		public string FileName { get; private set; } = string.Empty;

		/// <summary>Return the current play state of the video</summary>
		public EPlayState PlayState
		{
			get => m_play_state;
			private set
			{
				if (m_play_state == value) return;
				m_play_state = value;
				PlayStateChanged?.Invoke(this, m_play_state);
			}
		}

		/// <summary>Starting playing the video/audio file</summary>
		public void Play()
		{
			lock (m_controls_lock)
			{
				if (PlayState != EPlayState.Paused && PlayState != EPlayState.Stopped) return;
				if (MediaCtrl != null) DsError.ThrowExceptionForHR(MediaCtrl.Run());
				PlayState = EPlayState.Running;
			}
		}

		/// <summary>Pause the video/audio file</summary>
		public void Pause()
		{
			lock (m_controls_lock)
			{
				if (PlayState != EPlayState.Running && PlayState != EPlayState.Stopped) return;
				if (MediaCtrl != null) DsError.ThrowExceptionForHR(MediaCtrl.Pause());
				PlayState = EPlayState.Paused;
			}
		}

		/// <summary>Stop the video/audio file</summary>
		public void Stop()
		{
			lock (m_controls_lock)
			{
				if (PlayState != EPlayState.Running && PlayState != EPlayState.Paused && PlayState != EPlayState.Init) return;
				if (MediaCtrl != null) DsError.ThrowExceptionForHR(MediaCtrl.Stop());
				PlayState = EPlayState.Stopped;
			}
		}

		/// <summary>Reset the clip back to the beginning</summary>
		public void Rewind()
		{
			lock (m_controls_lock)
			{
				if (MediaPosition != null)
					DsError.ThrowExceptionForHR(MediaPosition.put_CurrentPosition(0));
			}
		}

		/// <summary>Return the length of time for the video/audio file</summary>
		public double Duration
		{
			get { double duration = 0.0; if (MediaPosition != null) DsError.ThrowExceptionForHR(MediaPosition.get_Duration(out duration)); return duration; }
		}

		/// <summary>Set the position in the video</summary>
		public double Position
		{
			get { double current = 0.0; if (MediaPosition != null) DsError.ThrowExceptionForHR(MediaPosition.get_CurrentPosition(out current)); return current; }
			set
			{
				lock (m_controls_lock)
				{
					value = Math.Max(0, Math.Min(Duration, value));
					if (MediaPosition != null) DsError.ThrowExceptionForHR(MediaPosition.put_CurrentPosition(value));
					Pause();
				}
			}
		}

		/// <summary>Get/Set the playback rate of the video</summary>
		public double Rate
		{
			get
			{
				if (MediaPosition == null) return 0.0;
				double rate; DsError.ThrowExceptionForHR(MediaPosition.get_Rate(out rate));
				return rate;
			}
			set
			{
				lock (m_controls_lock)
				{
					if (MediaPosition == null) return;
					DsError.ThrowExceptionForHR(MediaPosition.put_Rate(value));
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
				if (BasicAudio == null) return 0;
				int vol; if (BasicAudio.get_Volume(out vol) == -1) return 0; // Fail quietly if this is a video-only media file
				return vol == 0 ? 100 : (int)(100.0 * (4.0 - Math.Log10(-vol)) / 4.0);
			}
			set
			{
				lock (m_controls_lock)
				{
					if (BasicAudio == null) return;
					BasicAudio.put_Volume((int)-Math.Pow(10, 4.0 - 4.0 * value / 100.0));
				}
			}
		}

		/// <summary>Set up the filter graph for grabbing snapshots</summary>
		public void EnableGrabbing()
		{
			var icgb2 = (ICaptureGraphBuilder2?)null;
			try
			{
				// Get a ICaptureGraphBuilder2 to help build the graph
				// Link the ICaptureGraphBuilder2 to the IFilterGraph2
				icgb2 = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
				DsError.ThrowExceptionForHR(icgb2.SetFiltergraph(FilterGraph ?? throw new Exception("No filter graph has been created")));

				// Get the SampleGrabber interface
				SampGrabber = (ISampleGrabber)new SampleGrabber();

				{// Set the media type to Video/RBG24
					AMMediaType media = new AMMediaType { majorType = MediaType.Video, subType = MediaSubType.RGB24, formatType = FormatType.VideoInfo };
					try { DsError.ThrowExceptionForHR(SampGrabber.SetMediaType(media)); }
					finally { DsUtils.FreeAMMediaType(media); }
				}

				// Configure the sample grabber
				DsError.ThrowExceptionForHR(SampGrabber.SetBufferSamples(true));

				// Add the sample graber to the filter graph
				IBaseFilter grab_filter = (IBaseFilter)SampGrabber;
				DsError.ThrowExceptionForHR(FilterGraph.AddFilter(grab_filter, "DS.NET Grabber"));
			}
			finally
			{
				if (icgb2 != null)
					Marshal.ReleaseComObject(icgb2);
			}
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
				var samp = SampGrabber ?? throw new Exception("No SampleGrabber has been created");
				DsError.ThrowExceptionForHR(samp.GetCurrentBuffer(ref bufsize, IntPtr.Zero));

				// Allocate the buffer and read it
				ip = Marshal.AllocCoTaskMem(bufsize);
				DsError.ThrowExceptionForHR(SampGrabber.GetCurrentBuffer(ref bufsize, ip));

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
			finally
			{
				if (ip != IntPtr.Zero)
					Marshal.FreeCoTaskMem(ip);
			}
		}

		/// <summary>Clean up the video resources</summary>
		private void CloseInterfaces()
		{
			Stop();
			AttachToWindow(IntPtr.Zero);
			PlayState = EPlayState.Cleanup;

			lock (m_shutdown_lock)
				m_shutdown = true;

			if (SampGrabber != null)
				Marshal.ReleaseComObject(SampGrabber);

			MediaCtrl = null;
			MediaPosition = null;
			SampGrabber = null;
			VideoWindow = null;
			BasicVideo = null;
			BasicAudio = null;

			#if DEBUG
			if (m_ds_rot != null) m_ds_rot.Dispose();
			m_ds_rot = null;
			#endif

			if (FilterGraph != null) Marshal.ReleaseComObject(FilterGraph);
			FilterGraph = null;

			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		/// <summary></summary>
		public enum EPlayState
		{
			Init,
			Running,
			Paused,
			Stopped,
			Cleanup,
		}

	}
}
