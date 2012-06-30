// Transitions
// Installer project
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.attrib;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.inet;
using pr.maths;
using pr.util;
using Timer = System.Windows.Forms.Timer;

namespace imager
{
	#if !PLATFORM_X64 && !PLATFORM_X86
	#warning "No platform defined"
	#endif

	public sealed partial class Imager :Form
	{
		private const string AppTitle = "Imager";

		// ReSharper disable UnusedMember.Local
		private class MediaFile
		{
			public readonly string m_file; // The full filepath
			public readonly long   m_timestamp;
			public override string ToString()                    { return m_file; }
			public MediaFile(string file, long timestamp)        { m_file = file; m_timestamp = timestamp; }
			public MediaFile(string file)                        { m_file = Path.GetFullPath(file).ToLower(); m_timestamp = File.GetCreationTime(file).ToFileTime(); }
			public string FileName                               { get { return Path.GetFileNameWithoutExtension(m_file); } }
			public string Directory                              { get { return Path.GetDirectoryName(m_file); } }
			public string Extn                                   { get { return Path.GetExtension(m_file); } }
			public static bool Eql(MediaFile lhs, MediaFile rhs)
			{
				if (lhs == rhs) return true;
				if (lhs == null || rhs == null) return false;
				return string.CompareOrdinal(lhs.m_file, rhs.m_file) == 0 && lhs.m_timestamp == rhs.m_timestamp;
			}
		}

		public  enum EMode       { Normal, ScreenSaver, Preview };
		private enum EBuildState { Idle, Finding, Sorting };
		private enum EViewType   { NotSet, Normal, ChildWindow, FullScreen };

		private readonly Random m_rng;
		private readonly Settings m_settings;
		private readonly RecentFiles m_recent_files;
		private readonly BindingSource m_media_list;
		private readonly ManualResetEvent m_build_done;
		private readonly ManualResetEvent m_sort_done;
		private readonly View3D m_view3d;
		private readonly View3D.Object m_photo_model;
		private View3D.Texture m_photo;
		private readonly ViewVideoControl m_video;
		//private readonly Transition[] m_transitions;
		private readonly Form[] m_blanks;
		private readonly Form m_controls;
		private readonly Form m_media_list_ui;
		private readonly DirectoryManager m_dir_manager;
		private readonly Timer m_slide_show;
		private readonly Timer m_hide_timer;
		private readonly IntPtr m_parent;
		private readonly object m_build_lock;
		private EBuildState m_build_state;
		private volatile int m_build_issue;
		private volatile int m_build_processes;
		private EViewType m_view_type;
		private Point m_mouse_location;
		private readonly EMode m_mode;
		private bool m_resizing;
		
		/// <summary>'mode' is the mode that the app will run in.
		/// 'paths' is an optional list of directories to search for media files.
		/// 'parent' is the parent window if 'mode" == Preview</summary>
		public Imager(EMode mode, string[] paths, IntPtr parent)
		{
			InitializeComponent();

			Log.Info(this, "Constructor - mode: "+mode+"\n");

			KeyPreview = true;
			BackColor = Color.Black;
		
			Application.AddMessageFilter(new HoverScroll());

			m_settings = Settings.Load();
			m_rng = new Random((int)DateTime.Now.Ticks);
			m_settings.SettingsChanged += OnSettingsChanged;
			m_recent_files = new RecentFiles(m_menu_recent, (f)=>{ if (File.Exists(f)) LoadFile(new MediaFile(f)); });
			m_recent_files.Import(m_settings.RecentFiles);
			m_media_list = new BindingSource{DataSource = new List<MediaFile>()};
			m_build_done = new ManualResetEvent(true);
			m_sort_done = new ManualResetEvent(true);
			m_build_state = EBuildState.Idle;
			m_build_lock = new object();
			m_build_issue = 0;
			m_build_processes = 0;
			m_view_type = EViewType.NotSet;
			m_mouse_location = MousePosition;
			m_parent = parent;
			m_mode = mode;
			m_resizing = false;
			m_photo = null;
			
			m_view3d         = CreateView3D();
			m_photo_model    = CreatePhotoModel(m_view3d);
			m_video          = CreateVideoControl();
			//m_transitions    = CreateTransitions();
			m_blanks         = CreateBlankForms();
			m_hide_timer     = CreateHideTimer();
			m_controls       = CreateMediaControls();
			m_media_list_ui  = CreateMediaListUI();
			m_dir_manager    = new DirectoryManager(m_settings, false);

			m_slide_show = new Timer{Interval = 100, Enabled = false, Tag = 0U};
			m_slide_show.Tick += (s,e)=>{ StepSlideShow(); };

			m_menu_open_file.Click            += (s,e)=>{ OpenMedia(true, false); };
			m_menu_directories.Click          += (s,e)=>{ if (!m_dir_manager.Visible) m_dir_manager.Show(this); };
			m_menu_options.Click              += (s,e)=>{ new Config(m_settings).ShowDialog(this); };
			m_menu_exit.Click                 += (s,e)=>{ Close(); };
			m_menu_full_screen.Click          += (s,e)=>{ ToggleFullScreen(); };
			m_menu_slide_show.Click           += (s,e)=>{ if (m_mode == EMode.Normal) SlideShow = !SlideShow; };
			m_menu_media_list.Click           += (s,e)=>{ if (!m_media_list_ui.Visible) m_media_list_ui.Show(this); else m_media_list_ui.Hide(); };
			m_menu_check_for_updates.Click    += (s,e)=>{ CheckForUpdates(true); };
			m_menu_help.Click                 += (s,e)=>{ Process.Start(m_settings.HelpURL); };
			m_menu_about.Click                += (s,e)=>{ new About().ShowDialog(this); };
			m_menu.VisibleChanged             += (s,e)=>{ MenuStrip menu = (MenuStrip)s; if (menu.Visible) menu.BringToFront(); };

			m_status.BackColor = SystemColors.Control;
			m_status.VisibleChanged           += (s,e)=>{ StatusStrip status = (StatusStrip)s; if (status.Visible) status.BringToFront(); };

			m_lbl_msg.BackColor = Color.Black;
			m_lbl_msg.ForeColor = Color.White;
			m_lbl_msg.MouseClick     += (s,e)=>{ if (e.Button == MouseButtons.Right) m_settings.ShowFilenames = false; };
			m_lbl_msg.VisibleChanged += (s,e)=>{ Label lbl = (Label)s; if (lbl.Visible) lbl.BringToFront(); };
			m_lbl_building_media_list.Click += (s,e)=>
			{
				ContextMenuStrip menu = new ContextMenuStrip();
				menu.Items.Add("Cancel?", null, (o,a)=> { m_lbl_building_media_list.Text = "Cancelling..."; CancelMediaListBuild(); });
				menu.Show(MousePosition);
			};

			// Configure the form as a child of another window if a parent is given
			if (m_mode == EMode.Preview && m_parent != IntPtr.Zero)
			{
				// Make this a child window, so when the parent closes. this will close also
				Win32.SetParent(Handle, m_parent);
				Win32.SetWindowLong(Handle, Win32.GWL_STYLE, Win32.GetWindowLong(Handle, Win32.GWL_STYLE)|Win32.WS_CHILDWINDOW); //|Win32.WS_MAXIMIZE
			}

			AllowDrop         = true;
			StartPosition     = FormStartPosition.Manual;
			DragOver         += (s,e)=>{ MainView_DragOver(e); };
			DragDrop         += (s,e)=>{ MainView_DragDrop(e); };
			MouseMove        += OnMouseMove;
			MouseDoubleClick += OnMouseDoubleClick;
			KeyDown          += HandleKeys;
			Shown            += (s,e)=>{ OnFormShown(paths); };
			FormClosing      += (s,e)=>{ OnFormClosing(); };

			ResizeBegin   += (s,e)=>{ m_resizing = true; };
			ResizeEnd     += (s,e)=>{ m_resizing = false; SaveWindowBounds(); UpdateUI(); ResetZoom(); if (m_view3d.Visible) m_view3d.Refresh(); };
			SizeChanged   += (s,e)=>{ if (!m_resizing) UpdateUI(); };

			if (m_mode == EMode.Normal)
			{
				MinimumSize = new Size(100,60);
				if (m_settings.WindowBounds == Rectangle.Empty) StartPosition = FormStartPosition.WindowsDefaultLocation;
				ViewType = EViewType.Normal;
			}
			else if (m_mode == EMode.ScreenSaver)
			{
				MinimumSize = new Size(100,60);
				ViewType = EViewType.FullScreen;
			}
			else if (m_mode == EMode.Preview)
			{
				MinimumSize = new Size(1,1);
				ViewType = EViewType.ChildWindow;
			}
		}

		/// <summary>Called when the app is displayed for the first time</summary>
		private void OnFormShown(ICollection<string> paths)
		{
			Status = "no file";
			PhotoLabel = "";
			ImageInfo = null;

			if (m_mode == EMode.Normal)
			{
				if (paths != null && paths.Count > 1)
				{
					BuildMediaListAsync(paths, true, false);
				}
				//else if (m_settings.CacheSSMediaList)
				//{
				//    BuildMediaListFromCacheAsync(m_settings.MediaPaths, true, false);
				//}
				//else if (m_settings.MediaPaths.Count != 0)
				//{
				//    BuildMediaListAsync(m_settings.MediaPaths, true, false);
				//}
				if (m_settings.StartupVersionCheck)
				{
					CheckForUpdates(false);
				}
				m_controls.Show(this);
			}
			if (m_mode == EMode.ScreenSaver || m_mode == EMode.Preview)
			{
				if (m_settings.CacheSSMediaList && File.Exists(MediaListCacheFilepath))
				{
					BuildMediaListFromCache();
				}
				else if (m_settings.SSMediaPaths.Count != 0)
				{
					BuildMediaListAsync(m_settings.SSMediaPaths, true, false);
				}
				else
				{
					BuildMediaListAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), true, false);
				}
				if (m_mode == EMode.ScreenSaver)
				{
					m_mouse_location = MousePosition;
					MouseMove          += MouseMoveCausesExit;
					m_view3d.MouseMove += MouseMoveCausesExit;
					m_video.MouseMove  += MouseMoveCausesExit;
					foreach (Form f in m_blanks) f.MouseMove += MouseMoveCausesExit;
					Util.ShowCursor = false;
				}
				SlideShow = true;
			}
		}

		/// <summary>Called as the app shuts down</summary>
		private void OnFormClosing()
		{
			Log.Info(this,"OnFormClosing:\n");
			SlideShow = false;
			SetMedia(EMediaType.All, null, null);
			m_media_list_ui.Close();
			m_controls.Close();
			m_settings.RecentFiles = m_recent_files.Export();
			m_settings.Save();
		}

		/// <summary>Access to the settings</summary>
		public Settings Settings
		{
			get { return m_settings; }
		}

		/// <summary>Record the current window bounds in the settings</summary>
		private void SaveWindowBounds()
		{
			// If the window is in normal mode, record it's on-screen position
			if (m_view_type == EViewType.Normal && WindowState == FormWindowState.Normal && m_settings.WindowBounds != Bounds)
			{
				m_settings.WindowBounds = Bounds;
				m_settings.Save();
			}
		}

		/// <summary>Stop the form using the arrow and tab keys for control navigation</summary>
		protected override bool ProcessDialogKey(Keys keyData)
		{
			return false;
		}

		/// <summary>Set 'media' as currently displayed</summary>
		private void SetMedia(EMediaType type, object media, MediaFile media_file)
		{
			// Clear the old media
			if (media != m_photo)
			{
				// It might be a different image object but referencing the same
				// texture in the renderer, only dispose if actually different textures
				if (m_photo != null && !m_photo.Equals(media)) m_photo.Dispose();
				m_view3d.Visible = false;
			}
			if (media != m_video.Video)
			{
				Video old_video = m_video.Video;
				m_video.Visible = false;
				m_video.Video = null;
				if (old_video != null) old_video.Dispose();
			}

			// If we're running as a screen saver, and the primary display is set to random, re-position the forms
			if (m_mode == EMode.ScreenSaver && m_settings.PrimaryDisplay == 0)
				UpdateUI();

			// Assign the new media
			if (media != null)
			{
				Debug.Assert(media_file != null);
				switch (type)
				{
				case EMediaType.Image:
					{
						Log.Info(this, "SetMedia: Image - " + media_file.m_file + "\n");
						m_photo = (View3D.Texture)media;
						m_photo_model.Edit(EditPhotoCB);
						if (m_settings.ResetZoomOnLoad) ResetZoom();
						m_view3d.Visible = true;
						m_view3d.Refresh();
					}break;
				case EMediaType.Audio:
				case EMediaType.Video:
					{
						Log.Info(this, "SetMedia: " + (type == EMediaType.Audio ? "Audio - " : "Video - ") + media_file.m_file + "\n");
						m_video.Video = (Video)media;
						m_video.Video.Volume = m_mode == EMode.Normal ? m_settings.Volume : m_mode == EMode.ScreenSaver ? m_settings.SSVolume : 0;
						if (m_settings.ResetZoomOnLoad) ResetZoom();
						m_video.Visible = true;
						m_video.Video.Rewind();
						m_video.Video.Play();
					}break;
				}

				Log.Info(this, "SetMedia: assignment done\n");
				Status = media_file.ToString();
				PhotoLabel = media_file.ToString();
				ImageInfo = media;
				Text = AppTitle + (string.IsNullOrEmpty(media_file.m_file) ? ("") : (" - " + media_file.m_file));
				FindInMediaList(media_file);
			}
			else
			{
				Log.Info(this, "SetMedia: null assigned\n");
				Status = "no file";
				PhotoLabel = "";
				ImageInfo = null;
				Text = AppTitle;
			}
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		/// <summary>Load a media file</summary>
		private void LoadFile(MediaFile media_file)
		{
			Action<MediaFile, string, string> fail = (mf, msg, detail)=>
			{
				Log.Info(this, "LoadFile: FAILED - " + mf.m_file + " - " + msg + " - " + detail + "\n");
				Status = msg;
				m_media_list.Remove(mf);
				m_media_list.ResetBindings(false);
				Action load_next = LoadCurrent;
				BeginInvoke(load_next, null); // fire off a call to load the next file in the list, use BeginInvoke to prevent recursion
			};

			Log.Info(this, "LoadFile: " + media_file.m_file + "\n");

			// Check the file exists first
			if (!File.Exists(media_file.m_file))
			{
				fail(media_file, "File not found: "+media_file.m_file, "File doesn't exist");
				return;
			}

			// Try to load it
			Status = "Loading: " + media_file.m_file;
			if (MatchExtn(media_file.m_file, m_settings.ImageExtensions))
			{
				try { 
				View3D.ImageInfo info = View3D.Texture.GetInfo(media_file.m_file);
				View3D.Texture img = new View3D.Texture(media_file.m_file, info.m_width, info.m_height, 1, View3D.EFilter.D3DX_FILTER_NONE, View3D.EFilter.D3DX_FILTER_NONE, 0);
				SetMedia(EMediaType.Image, img, media_file);
				} catch (Exception ex) { fail(media_file, "Failed to load "+media_file.m_file, ex.Message); }
			}
			else if (MatchExtn(media_file.m_file, m_settings.VideoExtensions))
			{
				try { SetMedia(EMediaType.Video, new Video(media_file.m_file){Volume = 0}, media_file); }
				catch (Exception ex) { fail(media_file, "Failed to load "+media_file.m_file, ex.Message); }
			}
			else if (MatchExtn(media_file.m_file, m_settings.AudioExtensions))
			{
				try { SetMedia(EMediaType.Audio, new Video(media_file.m_file){Volume = 0}, media_file); }
				catch (Exception ex) { fail(media_file, "Failed to load "+media_file.m_file, ex.Message); }
			}
			else
			{
				fail(media_file, "Unknown media format: "+media_file.m_file, "File extension not recognised");
				return;
			}

			// Save the file to the recent history
			m_recent_files.Add(media_file.m_file);
			m_settings.RecentFiles = m_recent_files.Export();
			m_settings.Save();
		}

		/// <summary>Load the current item in the media list</summary>
		private void LoadCurrent()
		{
			if (m_media_list.Current != null)
			{
				LoadFile((MediaFile)m_media_list.Current);
			}
			else
			{
				SetMedia(EMediaType.All, null, null);
				m_lbl_msg.Text = "No Media Files";
				if (m_mode == EMode.Normal && !SlideShow)
					m_lbl_msg.Location = new Point((ClientSize.Width - m_lbl_msg.Width)/2, (ClientSize.Height - m_lbl_msg.Height)/2);
				else
					m_lbl_msg.Location = new Point(m_rng.Next(ClientSize.Width - m_lbl_msg.Width), m_rng.Next(m_menu.Height, ClientSize.Height - m_status.Height - m_lbl_msg.Height));
				m_lbl_msg.Visible = true;
			}
		}

		/// <summary>Load the next media file in the media list</summary>
		private void Next()
		{
			if (m_media_list.Count == 0) return;
			m_media_list.Position = (m_media_list.Position + 1) % m_media_list.Count;
		}

		/// <summary>Load the previous media file in the media list</summary>
		private void Prev()
		{
			if (m_media_list.Count == 0) return;
			m_media_list.Position = (m_media_list.Position + m_media_list.Count - 1) % m_media_list.Count;
		}

		/// <summary>Set the app to normal, maximised, or fullscreen mode</summary>
		private EViewType ViewType
		{
			get { return m_view_type; }
			set { if (value != ViewType) { m_view_type = value; UpdateUI(); } }
		}

		/// <summary>Cancel any existing media list build/sort processes. Blocks until done</summary>
		public void CancelMediaListBuild()
		{
			Log.Info(this, "CancelMediaListBuild\n");
			lock (m_build_lock) { m_build_issue = unchecked(m_build_issue + 1); }
			m_build_done.WaitOne();
			m_sort_done.WaitOne();
		}

		/// <summary>A function that adds a cache of media files to 'm_media_list'. This should always run in the main UI thread</summary>
		private void AddToMediaList(List<MediaFile> cache, bool load_first_file, ref int r)
		{
			const string roll = "      ...      ";
			Debug.Assert(!InvokeRequired);
		
			// Add the found media files to the media list
			foreach (MediaFile mf in cache) m_media_list.Add(mf);
			cache.Clear();

			// If the first file should be loaded, spawn that now
			if (load_first_file && m_media_list.Count != 0)
			{
				m_media_list.Position = 0;
				LoadCurrent();
			}

			// Update UI elements
			m_lbl_building_media_list.Text = "Building Media List " + roll.Substring(r=(r+8)%9,6);
			m_media_list_ui.Text = "Media File List - " + m_media_list.Count + " files";
		}
		
		/// <summary>Build a media list from a collection of files/directories.
		/// This is an asynchronous method that updates 'm_media_list' when done.
		/// Calling this method again before it is finished is allowed.</summary>
		public void BuildMediaListAsync(IEnumerable<MediaPath> paths, bool load_first_file, bool accumulative)
		{
			// m_media_list should only be accessed by the main thread => is doesn't need to be locked
			// Each thread spawned by calls to this method creates a local cache of media files
			// which are periodically added to the main media list
			Log.Info(this, "BuildMediaListAsync\n");

			// Ensure existing builds/sorts are stopped if needed
			bool wait_for_build_done = false;
			lock (m_build_lock)
			{
				switch (m_build_state)
				{
				// In the finding state we only need to cancel finds if the build isn't accumulative
				case EBuildState.Finding:
					if (accumulative) break;
					m_build_issue = unchecked(m_build_issue + 1);
					wait_for_build_done = true;
					break;
				// In the sorting state we need to cancel the sort before adding more files
				case EBuildState.Sorting:
					m_build_issue = unchecked(m_build_issue + 1);
					break;
				}
			}
			if (wait_for_build_done) m_build_done.WaitOne();
			m_sort_done.WaitOne();

			// If this build is not accumulative, reset the media list.
			if (!accumulative) m_media_list.Clear();
			
			// Enter the "finding files" build state
			lock (m_build_lock)
			{
				m_build_state = EBuildState.Finding;
				m_build_processes++;
				m_build_done.Reset();
				m_lbl_building_media_list.Visible = true;
				m_lbl_building_media_list.Text = "Building Media List ";
			}

			// Make local copies of the paths and build issue
			List<MediaPath> media_paths = new List<MediaPath>(paths);
			int build_issue = m_build_issue;

			// Spawn a thread to search 'media_paths' for media files
			ThreadPool.QueueUserWorkItem((a)=>
			{
				try
				{
					//// A function that adds a cache of media files to 'm_media_list'
					//// This should always run in the main UI thread
					//#region AddToMediaList
					//const string roll = "      ...      "; int r = 0;
					//Action<List<MediaFile>> add_to_media_list = (cc)=>
					//{
					//    // Add the found media files to the media list
					//    foreach (MediaFile mf in cc) m_media_list.Add(mf);
					//    cc.Clear();

					//    // If the first file should be loaded, spawn that now
					//    if (load_first_file && m_media_list.Count != 0)
					//    {
					//        load_first_file = false;
					//        m_media_list.Position = 0;
					//        LoadCurrent();
					//    }

					//    // Update UI elements
					//    m_lbl_building_media_list.Text = "Building Media List " + roll.Substring(r=(r+8)%9,6);
					//    m_media_list_ui.Text = "Media File List - " + m_media_list.Count + " files";
					//};
					//#endregion

					// Add a cache of media files to the media list
					int r = 0;
					Action<List<MediaFile>> add_to_media_list = (cc)=>
					{
						AddToMediaList(cc, load_first_file, ref r);
						load_first_file = false;
					};

					// A recursive function that finds media files given a list of paths.
					// Throws if the issue number is changed
					// ReSharper disable AccessToModifiedClosure
					#region FindMediaFiles
					Action<IEnumerable<MediaPath>, List<MediaFile>, int> find_media_files = null;
					find_media_files = (mp, cc, issue)=>
					{
						foreach (MediaPath path in mp)
						{
							lock (m_build_lock) if (m_build_issue != issue) throw new OperationCanceledException();
							if (IsMediaFile(path))
							{
								cc.Add(new MediaFile(path));
								if (cc.Count == cc.Capacity) Invoke(add_to_media_list, cc);
							}
							else if (Directory.Exists(path.Pathname)) // If 'path' is a directory, add it recursively
							{
								if (path.Pathname == "." || path.Pathname == "..") continue;
						
								// Add the media files from 'path.Pathname'
								foreach (string file in Directory.GetFiles(path))
								{
									lock (m_build_lock) if (m_build_issue != issue) throw new OperationCanceledException();
									if (!IsMediaFile(file)) continue;
									cc.Add(new MediaFile(file));
									if (cc.Count == cc.Capacity) Invoke(add_to_media_list, cc);
								}

								// Look in the sub directories of 'path.Pathname'
								// ReSharper disable PossibleNullReferenceException
								if (path.SubFolders)
									find_media_files(Util.Conv(Directory.GetDirectories(path.Pathname), s=>new MediaPath(s)), cc, issue);
								// ReSharper restore PossibleNullReferenceException
							}
						}
					};
					// ReSharper restore AccessToModifiedClosure
					#endregion

					// Find the media files
					List<MediaFile> cache = new List<MediaFile>(100);
					find_media_files(media_paths, cache, build_issue);
					if (cache.Count != 0) Invoke(add_to_media_list, cache);
				
					// If this is the last build thread, spawn the sort thread
					lock (m_build_lock)
					{
						if (m_build_processes != 1) return;
						m_build_state = EBuildState.Sorting;
						m_sort_done.Reset();
						Action sort_media_list = SortMediaListAsync;
						BeginInvoke(sort_media_list);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Exception during build: " + ex.Message);
					Action clean_up = ()=>{ m_lbl_building_media_list.Text = ""; m_lbl_building_media_list.Visible = false; };
					BeginInvoke(clean_up);
				}
				finally
				{
					// If we are the last build process to exit, set the event
					lock (m_build_lock) { if (--m_build_processes == 0) m_build_done.Set(); }
				}
			});
		}
		public void BuildMediaListAsync(string path, bool load_first_file, bool accumulative)
		{
			BuildMediaListAsync(new[]{new MediaPath(path)}, load_first_file, accumulative);
		}
		public void BuildMediaListAsync(IEnumerable<string> paths, bool load_first_file, bool accumulative)
		{
			List<MediaPath> mp = new List<MediaPath>();
			foreach (string p in paths) { mp.Add(new MediaPath(p)); }
			BuildMediaListAsync(mp, load_first_file, accumulative);
		}

		/// <summary>Called to sort the current media list</summary>
		public void SortMediaListAsync()
		{
			Log.Info(this, "SortMediaListAsync\n");

			// Switch to the sorting state
			lock (m_build_lock)
			{
				// If the build state is "finding" then a sort will happen after that, just wait for it
				if (m_build_state == EBuildState.Finding) return;
				m_build_state = EBuildState.Sorting;
				m_sort_done.Reset();
				m_lbl_building_media_list.Visible = true;
				m_lbl_building_media_list.Text = "Sorting Media List ";
			}

			// Make local copies of variables
			List<MediaFile> media_list = new List<MediaFile>((List<MediaFile>)m_media_list.DataSource);
			ESortOrder folder_order = m_mode != EMode.Normal ? m_settings.SSFolderOrder : m_settings.FolderOrder;
			ESortOrder file_order   = m_mode != EMode.Normal ? m_settings.SSFilesOrder  : m_settings.FilesOrder;
			MediaFile current = m_media_list.Current as MediaFile;
			int issue = m_build_issue;
			bool allow_duplicates = m_settings.AllowDuplicates;
			bool cache_media_list = (m_mode == EMode.ScreenSaver && m_settings.CacheSSMediaList);
			string cache_file = MediaListCacheFilepath;
			
			// Spawn a thread to perform the sort
			ThreadPool.QueueUserWorkItem((a)=>
			{
				try
				{
					// Sort progress callback function
					#region Progress Callback
					const string roll = "      ...      "; int r = 0;
					Action progress_cb = ()=>
					{
						lock (m_build_lock) if (issue != m_build_issue) throw new OperationCanceledException("media list sort cancelled");
						Action update_ui = ()=> { m_lbl_building_media_list.Text = "Sorting Media List " + roll.Substring(r=(r+8)%9,6); };
						BeginInvoke(update_ui);
					};
					#endregion

					// Set the new sorted media list
					#region Set New Media List
					Action<List<MediaFile>,MediaFile> set_media_list = (list,curr)=>
						{
							m_media_list.DataSource = list;
							m_media_list_ui.Text = "Media File List - " + list.Count + " files";
							FindInMediaList(curr);
							m_lbl_building_media_list.Visible = false;
							m_lbl_building_media_list.Text = "";
						};
					#endregion

					// Do the sort
					SortMediaList(media_list, folder_order, file_order, allow_duplicates, progress_cb);

					// If caching is enabled, write the media list to a cache file
					if (cache_media_list)
					{
						using (StreamWriter of = new StreamWriter(cache_file))
							foreach (MediaFile mf in media_list)
								of.WriteLine(mf.m_file+","+mf.m_timestamp);
					}

					// Set the new sorted media list
					BeginInvoke(set_media_list, media_list, current);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Exception during sort: " + ex.Message);
					Action clean_up = ()=>{ m_lbl_building_media_list.Text = ""; m_lbl_building_media_list.Visible = false; };
					BeginInvoke(clean_up);
				}
				finally
				{
					lock (m_build_lock) { m_build_state = EBuildState.Idle; m_sort_done.Set(); }
				}
			});
		}

		/// <summary>Apply sorting to a media list.</summary>
		private static void SortMediaList(List<MediaFile> list, ESortOrder folder_order, ESortOrder file_order, bool allow_duplicates, Action progress_cb)
		{
			int[] tick = {0};
			Random rng = new Random(Environment.TickCount);

			// Ensure folders/files are in contiguous groups
			List<MediaFile> copy = new List<MediaFile>(list); list.Clear();
			copy.Sort((lhs,rhs)=>
				{
					if (++tick[0] == 100 && progress_cb != null) { progress_cb(); tick[0] = 0; }
					return string.CompareOrdinal(lhs.m_file, rhs.m_file);
				});
			
			// Remove adjascent duplicates
			if (!allow_duplicates)
				copy.Unique(MediaFile.Eql);
			
			// If we're sorting by folder timestamp, build a map from directory to minimum timestamp then sort the folders
			if (folder_order == ESortOrder.ChronologicalAscending || folder_order == ESortOrder.ChronologicalDecending)
			{
				Dictionary<string, long> ts = new Dictionary<string,long>();
				foreach (MediaFile mf in copy) { ts[mf.Directory] = ts.ContainsKey(mf.Directory) ? Math.Min(mf.m_timestamp, ts[mf.Directory]) : mf.m_timestamp; }
				copy.Sort((lhs,rhs)=>
					{
						if (++tick[0] == 100 && progress_cb != null) { progress_cb(); tick[0] = 0; }
						return Maths.Compare(ts[lhs.Directory], ts[rhs.Directory]);
					});
			}
			
			// Apply sorting
			while (copy.Count != 0)
			{
				if (++tick[0] == 100 && progress_cb != null) { progress_cb(); tick[0] = 0; }

				// Get the range of files in the directory 'copy[d].Directory'
				Func<int, Range> find_dir_range = (d)=>
					{
						string dir = copy[d].Directory;
						Range r = new Range(0, copy.Count);
						for (r.Begin = d - 1; r.Begin != -1         && string.CompareOrdinal(dir, copy[(int)r.Begin].Directory) == 0; --r.Begin) {}
						for (r.End   = d + 1; r.End   != copy.Count && string.CompareOrdinal(dir, copy[(int)r.End  ].Directory) == 0; ++r.End  ) {}
						++r.Begin;
						return r;
					};

				// Find a range of files to apply file ordering too
				Range rg = new Range(0, copy.Count);
				switch (folder_order)
				{
				// If folder order is none, apply file order to all files
				default:
					break;

				// If folder_order is not none, apply folder order, then file order within folders
				case ESortOrder.AlphabeticalAscending:
				case ESortOrder.ChronologicalAscending:
					rg = find_dir_range(0);
					break;

				case ESortOrder.AlphabeticalDecending:
				case ESortOrder.ChronologicalDecending:
					rg = find_dir_range(copy.Count - 1);
					break;

				case ESortOrder.Random:
					rg = find_dir_range(rng.Next(copy.Count));
					break;
				}

				// Transfer 'copy' back to 'list' in the desired order
				switch (file_order)
				{
				default: break;
				case ESortOrder.AlphabeticalAscending:  copy.Sort((int)rg.Begin, (int)rg.Count, (lhs,rhs)=>{ return string.CompareOrdinal(lhs.FileName, rhs.FileName); }); break;
				case ESortOrder.AlphabeticalDecending:  copy.Sort((int)rg.Begin, (int)rg.Count, (lhs,rhs)=>{ return string.CompareOrdinal(rhs.FileName, lhs.FileName); }); break;
				case ESortOrder.ChronologicalAscending: copy.Sort((int)rg.Begin, (int)rg.Count, (lhs,rhs)=>{ return Maths.Compare(lhs.m_timestamp, rhs.m_timestamp); }); break;
				case ESortOrder.ChronologicalDecending: copy.Sort((int)rg.Begin, (int)rg.Count, (lhs,rhs)=>{ return Maths.Compare(rhs.m_timestamp, lhs.m_timestamp); }); break;
				case ESortOrder.Random: for (long i = rg.End; i != rg.Begin; --i) copy.Swap((int)(rg.Begin + rng.Next((int)rg.Count)), (int)(i-1)); break;
				}

				// Move the range from 'copy' to 'list'
				for (long i = rg.Begin; i != rg.End; ++i) list.Add(copy[(int)i]);
				copy.RemoveRange((int)rg.Begin, (int)rg.Count);
			}
		}

		/// <summary>Populate the media list from a cache file of media file paths</summary>
		private void BuildMediaListFromCache()
		{
			// m_media_list should only be accessed by the main thread => is doesn't need to be locked
			// Each thread spawned by calls to this method creates a local cache of media files
			// which are periodically added to the main media list
			Log.Info(this, "BuildMediaListFromCache\n");

			// Cancel any existing builds
			CancelMediaListBuild();
			m_media_list.Clear();
			
			// Enter the "finding files" build state
			lock (m_build_lock)
			{
				m_build_state = EBuildState.Finding;
				m_build_processes++;
				m_build_done.Reset();
				m_lbl_building_media_list.Visible = true;
				m_lbl_building_media_list.Text = "Building Media List ";
			}

			// Make local copies of the paths and build issue
			int build_issue = m_build_issue;
			string cache_file = MediaListCacheFilepath;

			// If the cache is available generate the media list from the cache file
			ThreadPool.QueueUserWorkItem(a =>
			{
				try
				{
					// Add a cache of media files to the media list
					int r = 0; bool load_first_file = true;
					Action<List<MediaFile>> add_to_media_list = cc =>
					{
						AddToMediaList(cc, load_first_file, ref r);
						load_first_file = false;
					};

					// Read media files from the cache and add them to the media list
					List<MediaFile> cache = new List<MediaFile>(100);
					using (StreamReader f = new StreamReader(cache_file))
					{
						for (string line = f.ReadLine(); line != null; line = f.ReadLine())
						{
							lock (m_build_lock) if (m_build_issue != build_issue) throw new OperationCanceledException();
							string[] mf = line.Split(','); if (mf.Length != 2) continue;
							cache.Add(new MediaFile(mf[0], long.Parse(mf[1])));
							if (cache.Count == cache.Capacity) Invoke(add_to_media_list, cache);
						}
					}
					if (cache.Count != 0) Invoke(add_to_media_list, cache);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Exception during build from cache: " + ex.Message);
					Action clean_up = ()=>{ m_lbl_building_media_list.Text = ""; m_lbl_building_media_list.Visible = false; };
					BeginInvoke(clean_up);
				}
				finally
				{
					// If we are the last build process to exit, set the event
					lock (m_build_lock) { if (--m_build_processes == 0) m_build_done.Set(); }
				}
			});
		}

		/// <summary>Set the current position in the media list to the best match for 'file'</summary>
		private void FindInMediaList(MediaFile media_file)
		{
			// If the current position matches 'mf' then there's nothing to do
			if (m_media_list.Current == null || string.CompareOrdinal(media_file.m_file, ((MediaFile) m_media_list.Current).m_file) != 0)
			{
				// Otherwise search for 'media_file' in 'm_media_list'
				List<MediaFile> list = (List<MediaFile>) m_media_list.DataSource;
				//m_media_list_ui.Controls[0].Tag = false;
				m_media_list.Position = list.FindIndex(mf =>{ return string.CompareOrdinal(media_file.m_file, mf.m_file) == 0; });
				//m_media_list_ui.Controls[0].Tag = true;
				Log.Info(this, "FindInMediaList: linear search done\n");
			}
			//m_settings.CachedListPosition = m_media_list.Position;
			//Log.Write("FindInMediaList: position = " + m_settings.CachedListPosition + "\n");
		}

		/// <summary>Load a media file or directory</summary>
		private void OpenMedia(bool file, bool accumulate)
		{
			if (file)
			{
				StringBuilder filter = new StringBuilder(512);

				// Add an extns string to a dialog filter
				Action<string, char> add_to_filter = (extns,sep)=>
					{
						foreach (string s in extns.Split(';'))
							filter.Append("*.").Append(s.Substring(1)).Append(sep).Append(' ');
					};

				filter.Append("All Media Files (");
				add_to_filter(m_settings.ImageExtensions,',');
				add_to_filter(m_settings.VideoExtensions,',');
				add_to_filter(m_settings.AudioExtensions,',');
				filter.Remove(filter.Length-2,2).Append(")|");
				add_to_filter(m_settings.ImageExtensions,';');
				add_to_filter(m_settings.VideoExtensions,';');
				add_to_filter(m_settings.AudioExtensions,';');
				filter.Remove(filter.Length-2,2);

				filter.Append("|Image Files (");
				add_to_filter(m_settings.ImageExtensions,',');
				filter.Remove(filter.Length-2,2).Append(")|");
				add_to_filter(m_settings.ImageExtensions,';');
				filter.Remove(filter.Length-2,2);

				filter.Append("|Video Files (");
				add_to_filter(m_settings.VideoExtensions,',');
				filter.Remove(filter.Length-2,2).Append(")|");
				add_to_filter(m_settings.VideoExtensions,';');
				filter.Remove(filter.Length-2,2);

				filter.Append("|Audio Files (");
				add_to_filter(m_settings.AudioExtensions,',');
				filter.Remove(filter.Length-2,2).Append(")|");
				add_to_filter(m_settings.AudioExtensions,';');
				filter.Remove(filter.Length-2,2);

				filter.Append("|All Files (*.*)|*.*");

				OpenFileDialog fd = new OpenFileDialog{Filter = filter.ToString(), Multiselect = true, CheckFileExists = true};
				if (fd.ShowDialog(this) != DialogResult.OK) return;
				BuildMediaListAsync(fd.FileNames, true, accumulate);
			}
			else
			{
				FolderBrowserDialog fd = new FolderBrowserDialog();
				if (fd.ShowDialog(this) != DialogResult.OK) return;
				BuildMediaListAsync(fd.SelectedPath, true, accumulate);
			}
		}

		/// <summary>Called when something is dragged into the app</summary>
		private static void MainView_DragOver(DragEventArgs e)
		{
			bool shift_pressed = Bit.AllSet(e.KeyState, 4);
			if (!e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.None;
			else if (shift_pressed)                           e.Effect = DragDropEffects.Copy;
			else                                              e.Effect = DragDropEffects.Move;
		}

		/// <summary>Called for drag/drop events on the view</summary>
		private void MainView_DragDrop(DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
			
			bool shift_pressed = Bit.AllSet(e.KeyState, 4);
			e.Effect = shift_pressed ? DragDropEffects.Copy : DragDropEffects.Move;

			string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
			BuildMediaListAsync(paths, !shift_pressed || m_media_list.Count == 0, shift_pressed);
		}

		/// <summary>Returns true if 'file' is a media file we're interested in</summary>
		private bool IsMediaFile(string file)
		{
			return File.Exists(file) && (
				((m_settings.MediaType & EMediaType.Image) != 0 && MatchExtn(file, m_settings.ImageExtensions)) ||
				((m_settings.MediaType & EMediaType.Audio) != 0 && MatchExtn(file, m_settings.AudioExtensions)) ||
				((m_settings.MediaType & EMediaType.Video) != 0 && MatchExtn(file, m_settings.VideoExtensions)));
		}

		/// <summary>Toggle between full screen and normal</summary>
		private void ToggleFullScreen()
		{
			if (m_mode == EMode.Normal)
				ViewType = ViewType == EViewType.FullScreen ? EViewType.Normal : EViewType.FullScreen;
		}

		/// <summary>Start/Stop the slide show</summary>
		public bool SlideShow
		{
			get { return m_slide_show.Enabled; }
			set
			{
				if (value == SlideShow) return;
				m_slide_show.Tag = (uint)Environment.TickCount;
				m_slide_show.Enabled = value;
				m_menu_slide_show.Checked = value;
				Log.Info(this, "SlideShow:"+(value?"started":"stopped")+"\n");
			}
		}

		/// <summary>Drive the slide show</summary>
		private void StepSlideShow()
		{
			// Protect against timer events occuring after the slideshow has been stopped.
			if (!SlideShow) return;

			// Check if it's time to go to the next file
			int dt = (int)unchecked((uint)Environment.TickCount - (uint)m_slide_show.Tag);
			if (dt < m_settings.SlideShowRate * 1000) return;
			
			// Hacky way to shutdown when in preview mode
			if (m_mode == EMode.Preview && !Win32.IsWindowVisible(m_parent))
			{
				Log.Info(this, "Parent not visible in preview mode, calling Close()\n");
				Close();
				return;
			}

			// If a video is playing, wait for it to stop first
			if (m_video.Video != null && m_video.Video.PlayState == Video.EPlayState.Running)
				return;

			Log.Info(this, "StepSlideShow - Next\n");
			
			// Load the next file
			Next();
			LoadCurrent();

			// Save the timestamp
			m_slide_show.Tag = (uint)Environment.TickCount;
		}

		/// <summary>Create the 3d renderer</summary>
		private View3D CreateView3D()
		{
			View3D view3d = new View3D(Program.OnError, 0)
			{
				BackColor = Color.Black,
				RenderMode = View3D.ERenderMode.Solid,
				MouseNavigation = false,
				DefaultKeyboardShortcuts = false,
				FocusPointVisible = false,
				OriginVisible = false,
				CameraAspect = 1f,
				CameraFovY = (float)(Math.PI/4.0),
				Visible = false,
			};
			Controls.Add(view3d);
	
			MouseEventHandler mouse_move = (s,e)=>
			{
				if (m_photo == null) return;
				view3d.Navigate(e.Location, e.Button & ~MouseButtons.Right, false);
				ClampCameraPosition();
				view3d.Refresh();
			};
			view3d.MouseWheel += (s,e)=>
			{
				view3d.NavigateZ(e.Delta / 120f);
				ClampCameraPosition();
				view3d.Refresh();
			};
			view3d.MouseDown  += (s,e)=>
			{
				if (m_photo == null) return;
				Cursor = Cursors.SizeAll;
				view3d.MouseMove -= mouse_move;
				view3d.MouseMove += mouse_move;
				view3d.Navigate(e.Location, e.Button & ~MouseButtons.Right, true);
			};
			view3d.MouseUp  += (s,e)=>
			{
				if (m_photo == null) return;
				Cursor = Cursors.Default;
				MouseMove -= mouse_move;
				view3d.Navigate(e.Location, 0, true);
			};

			view3d.MouseMove        += OnMouseMove;
			view3d.MouseDoubleClick += OnMouseDoubleClick;
			view3d.DragOver         += (s,e)=>{ MainView_DragOver(e); };
			view3d.DragDrop         += (s,e)=>{ MainView_DragDrop(e); };
			view3d.PositionCamera(new v4(0,0,1f/(float)Math.Tan(view3d.CameraFovY/2f),1f), v4.Origin, v4.YAxis);
			view3d.LightProperties = View3D.View3DLight.Directional(-v4.ZAxis, Colour32.Zero, Colour32.Gray, Colour32.Zero, 0f, false);
			return view3d;
		}

		/// <summary>Create the model that will have the photos on it as a texture</summary>
		private View3D.Object CreatePhotoModel(View3D view3d)
		{
			View3D.EditObjectCB edit_cb = EditPhotoCB;
			View3D.Object obj = new View3D.Object("photo", 0xFFFFFFFF, 6, 4, edit_cb);
			view3d.DrawsetAddObject(obj);
			return obj;
		}

		/// <summary>Callback from view3d to update the photo model</summary>
		private void EditPhotoCB(int vcount, int icount, View3D.Vertex[] verts, ushort[] indices, ref int new_vcount, ref int new_icount, ref View3D.EPrimType prim_type, ref View3D.Material mat, IntPtr ctx)
		{
			float w = 0f, h = 0f;
			IntPtr tex = IntPtr.Zero;
			if (m_photo != null)
			{
				float aspect = m_photo.m_info.m_width / (float)m_photo.m_info.m_height;
				if (aspect >= 1f) { w = 1f ; h = 1f / aspect; } else { w = aspect; h = 1f; }
				tex = m_photo.m_handle;
			}
			verts[0] = new View3D.Vertex(new v4(-w,  h, 0.0f, 1.0f), v4.ZAxis, Colour32.White, new v2(0.001f, 0.001f));
			verts[1] = new View3D.Vertex(new v4(-w, -h, 0.0f, 1.0f), v4.ZAxis, Colour32.White, new v2(0.001f, 0.999f));
			verts[2] = new View3D.Vertex(new v4( w, -h, 0.0f, 1.0f), v4.ZAxis, Colour32.White, new v2(0.999f, 0.999f));
			verts[3] = new View3D.Vertex(new v4( w,  h, 0.0f, 1.0f), v4.ZAxis, Colour32.White, new v2(0.999f, 0.001f));
			indices[0] = 3; indices[1] = 0; indices[2] = 1;
			indices[3] = 1; indices[4] = 2; indices[5] = 3;
			prim_type = View3D.EPrimType.D3DPT_TRIANGLELIST;
			mat.m_diff_tex = tex;
		}

		/// <summary>Position the view3d camera so that the image is zoomed based on ZoomType</summary>
		private void ResetZoom()
		{
			if (m_photo != null)
			{
				bool zoom_in_to_fill_window = m_settings.ZoomType == EZoomType.FitToWindow;
				bool zoom_out_to_fit_window = m_settings.ZoomType != EZoomType.ActualSize;

				float cam_aspect = m_view3d.CameraAspect;
				float img_aspect = m_photo.m_info.m_aspect;
				bool x_bound = img_aspect > cam_aspect;

				// 'dist' is the distance to fit the photo's largest dimension within the camera field of view
				// The largest axes of the photo model has length = 1f
				float dist;
				if (x_bound)
				{
					float size = img_aspect >= 1f ? 1f : img_aspect;
					dist = size / (float)Math.Tan(m_view3d.CameraFovX/2f);
				}
				else
				{
					float size = img_aspect <= 1f ? 1f : 1f/img_aspect;
					dist = size / (float)Math.Tan(m_view3d.CameraFovY/2f);
				}

				// 'scale' is the amount the image would be scaled by in order to fit it to the window
				// Use this with the zoom type to decide how to actually scale the image.
				Rectangle rect = ClientRectangle;
				float scale = (x_bound) ? ((float)rect.Width / m_photo.m_info.m_width) : ((float)rect.Height / m_photo.m_info.m_height);
				if (zoom_in_to_fill_window && scale > 1f) scale = 1f;
				if (zoom_out_to_fit_window && scale < 1f) scale = 1f;
				
				m_view3d.PositionCamera(new v4(0f, 0f, scale * dist, 1f), v4.Origin, v4.YAxis);
			}
			if (m_video != null)
			{
				m_video.FitToWindow = m_settings.ZoomType == EZoomType.FitToWindow;
			}
			m_lbl_zoom_type.Text = m_settings.ZoomType.StrAttr();
		}

		/// <summary>Clamps the view3d camera to within the allowed position space</summary>
		private void ClampCameraPosition()
		{
			m4x4 c2w = m_view3d.CameraToWorld;
			
			if (m_photo != null)
			{
				c2w.p.z = Maths.Clamp(c2w.p.z, 0.01f, 100f);
				float img_aspect = m_photo.m_info.m_aspect;
				float xsize = img_aspect >= 1f ? 1f :      img_aspect;         // the normalised x dimension of the image
				float ysize = img_aspect <= 1f ? 1f : 1f / img_aspect;         // the normalised y dimension of the image
				float xmaxz = xsize / (float)Math.Tan(m_view3d.CameraFovX/2f); // the z distance that fits the image to screen in the x direction
				float ymaxz = ysize / (float)Math.Tan(m_view3d.CameraFovY/2f); // the z distance that fits the image to screen in the y direction
				float xlim = Math.Max(xsize * (xmaxz - c2w.p.z) / xmaxz, 0f);
				float ylim = Math.Max(ysize * (ymaxz - c2w.p.z) / ymaxz, 0f);
				c2w.p.x = Maths.Clamp(c2w.p.x, -xlim, xlim);
				c2w.p.y = Maths.Clamp(c2w.p.y, -ylim, ylim);
			}
			else
			{
				c2w.p.z = 1f / (float)Math.Tan(m_view3d.CameraFovY/2f);
			}

			m_view3d.CameraToWorld = c2w;
			m_view3d.CameraFocusDist = c2w.p.z;
		}

		/// <summary>Create a video control</summary>
		private ViewVideoControl CreateVideoControl()
		{
			ViewVideoControl video = new ViewVideoControl
			{
				AdditionalInputKeys = new[]{Keys.Left, Keys.Right},
				AllowDrop = true,
				Anchor = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,
				BackColor = Color.Black,
				BorderStyle = BorderStyle.None,
				FitToWindow = true,
				Name = "m_video",
				RemoteAutoHidePeriod = 2000,
				RemoteAutoHideSpeed = 400,
				RemoteVisible = false,
				Video = null,
				Visible = false,
			};

			video.MouseMove        += OnMouseMove;
			video.MouseDoubleClick += OnMouseDoubleClick;
			video.DragOver         += (s,e)=>{ MainView_DragOver(e); };
			video.DragDrop         += (s,e)=>{ MainView_DragDrop(e); };
			video.VolumeChanged += (o,v)=>{m_settings.Volume = v;};

			Controls.Add(video);
			return video;
		}

		///// <summary>Create the transition handlers</summary>
		//private static Transition[] CreateTransitions()
		//{
		//    Transition[] trans = new Transition[Util.Count(typeof(ETransitions))];
		//    foreach (ETransitions t in Enum.GetValues(typeof(ETransitions)))
		//        trans[(int)t] = Transition.Create(t);
		//    return trans;
		//}

		/// <summary>Create blank forms to display on the other monitors while in fullscreen mode</summary>
		private static Form[] CreateBlankForms()
		{
			// Create blanks for each extra monitor
			Screen[] scrns = Screen.AllScreens;
			Form[] blanks = new Form[Math.Max(0, scrns.Length-1)];
			for (int i = 0; i < blanks.Length; ++i)
			{
				blanks[i] = new Form
				{
					FormBorderStyle = FormBorderStyle.None,
					StartPosition = FormStartPosition.Manual,
					ShowInTaskbar = false,
					BackColor = Color.Black,
					KeyPreview = true,
					TopMost = true,
				};
			}
			return blanks;
		}

		/// <summary>Create a timer to handle hiding stuff</summary>
		private Timer CreateHideTimer()
		{
			Timer timer = new Timer {Interval = 10, Tag = Environment.TickCount};
			timer.Tick += (s,e)=>
			{
				// If the mouse is currently within the controls window area extend the visible period
				if (m_controls.ClientRectangle.Contains(m_controls.PointToClient(MousePosition)))
					ShowControls();
				
				// Fade out, then disable the controls
				const float hide_speed = 400;
				float dt = unchecked((int)timer.Tag - Environment.TickCount);
				m_controls.Opacity = Maths.Clamp(dt/hide_speed, 0f, 1f);
				if (dt <= 0)
				{
					//BringToFront();
					m_controls.Visible = false;
					timer.Enabled = false;
					if (ViewType == EViewType.FullScreen) Util.ShowCursor = false;
				}
			};

			return timer;
		}

		/// <summary>Create a window containing controls for skipping through the media list</summary>
		private Form CreateMediaControls()
		{
			const int btn_width = 26, height = 24;
			int btn = 0;

			Form controls = new Form{StartPosition=FormStartPosition.Manual, FormBorderStyle=FormBorderStyle.None, ShowInTaskbar=false, MinimumSize=new Size(1,1), BackColor=Color.Black};
			controls.SuspendLayout();
			controls.KeyPreview = true;
			controls.KeyDown += HandleKeys;

			ToolTip tt = new ToolTip{AutoPopDelay=10000, InitialDelay=500, ReshowDelay=500};
			
			// Position the controls window
			Action position = ()=>{ controls.Location = PointToScreen(new Point(ClientRectangle.Right - controls.Width - 2, 2+(m_menu.Visible ? m_menu.Height : 0))); };

			Button btn_prev = new Button{Name="btn_prev", ImageList=m_image_list, ImageKey="Left", AutoSize=false, Location=new Point(2+btn*(btn_width+2),2), Size=new Size(btn_width,height), TabIndex=btn, UseVisualStyleBackColor=true, Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom};
			btn_prev.Click += (s,e)=>{ Prev(); LoadCurrent(); };
			controls.Controls.Add(btn_prev); btn++;
			tt.SetToolTip(btn_prev, "Load previous media file");

			Button btn_ss = new Button{Name="btn_ss", ImageList=m_image_list, ImageKey="Play", AutoSize=false, Location=new Point(2+btn*(btn_width+2),2), Size=new Size(btn_width,height), TabIndex=btn, UseVisualStyleBackColor=true, Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom};
			btn_ss.Click += (s,e)=>{ SlideShow = !SlideShow; };
			controls.Controls.Add(btn_ss); btn++;
			tt.SetToolTip(btn_ss, "Start slide show");
			m_menu_slide_show.CheckedChanged += (s,e)=>
			{
				btn_ss.ImageKey = SlideShow ? "Stop" : "Play";
				tt.SetToolTip(btn_ss, SlideShow ? "Stop slide show" : "Start slide show");
			};
			
			Button btn_next = new Button{Name="btn_next", ImageList=m_image_list, ImageKey="Right", AutoSize=false, Location=new Point(2+btn*(btn_width+2),2), Size=new Size(btn_width,height), TabIndex=btn, UseVisualStyleBackColor=true, Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom};
			btn_next.Click += (s,e)=>{ Next(); LoadCurrent(); };
			controls.Controls.Add(btn_next); btn++;
			tt.SetToolTip(btn_next, "Load next media file");

			Button btn_fs = new Button{Name="btn_fs", ImageList=m_image_list, ImageKey="FullScreen", AutoSize=false, Location=new Point(2+btn*(btn_width+2),2), Size=new Size(btn_width,height), TabIndex=btn, UseVisualStyleBackColor=true, Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom};
			btn_fs.Click += (s,e)=>{ ViewType = ViewType == EViewType.FullScreen ? EViewType.Normal : EViewType.FullScreen; position(); };
			controls.Controls.Add(btn_fs); btn++;
			tt.SetToolTip(btn_fs, "Toggle fullscreen");

			Button btn_fit = new Button{Name="btn_fit", ImageList=m_image_list, ImageKey="Fit", AutoSize=false, Location=new Point(2+btn*(btn_width+2),2), Size=new Size(btn_width,height), TabIndex=btn, UseVisualStyleBackColor=true, Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom};
			Func<EZoomType,EZoomType,string> tt_str = (zt0,zt1)=>{ return "Current Zoom Mode: " + zt0.StrAttr() + "\n   " + zt0.DescAttr() + "\n\n" + "Next Zoom Mode: " + zt1.StrAttr() + "\n   " + zt1.DescAttr(); };
			btn_fit.Click += (s,e)=>
			{
				m_settings.ZoomType = (EZoomType)Util.Next(m_settings.ZoomType);
				tt.SetToolTip(btn_fit, tt_str(m_settings.ZoomType, (EZoomType)Util.Next(m_settings.ZoomType)));
			};
			controls.Controls.Add(btn_fit); btn++;
			tt.SetToolTip(btn_fit, tt_str(m_settings.ZoomType, (EZoomType)Util.Next(m_settings.ZoomType)));
			
			controls.Size = new Size(4+btn*(btn_width+2), height+4);
			controls.ResumeLayout(false);
			controls.FormClosing    += (s,e)=>{ if (e.CloseReason == CloseReason.UserClosing) {controls.Hide(); e.Cancel = true;} };
			controls.VisibleChanged += (s,e)=>
			{
				Log.Info(this, "m_controls now "+(controls.Visible ? "Visible" : "Hidden")+"\n");
				if (controls.Visible) { position(); controls.BringToFront(); ShowControls(); }
				else if (ViewType == EViewType.FullScreen) { Util.ShowCursor = false; }
			};

			Move      += (s,e)=>{ position(); };
			ResizeEnd += (s,e)=>{ position(); };
			position();
			return controls;
		}

		/// <summary>Create the media list window</summary>
		private Form CreateMediaListUI()
		{
			DataGridView dgv = new DataGridView
			{
				Dock = DockStyle.Fill,
				VirtualMode = true,
				AutoGenerateColumns = false,
				DataSource = m_media_list,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = true,
				AllowUserToResizeRows = false,
				AllowUserToOrderColumns = true,
				AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle{BackColor=Color.FromArgb(240,240,240)},
				RowHeadersVisible = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				AllowDrop = true,
				Tag = true,
			};
			dgv.DblBuffer();
			dgv.Columns.Add(new DataGridViewTextBoxColumn{Name="FileName"  ,HeaderText="File"      ,DataPropertyName="FileName"  ,FillWeight=8f  ,SortMode=DataGridViewColumnSortMode.Automatic});
			dgv.Columns.Add(new DataGridViewTextBoxColumn{Name="Extn"      ,HeaderText="Type"      ,DataPropertyName="Extn"      ,FillWeight=2f  ,SortMode=DataGridViewColumnSortMode.Automatic});
			dgv.Columns.Add(new DataGridViewTextBoxColumn{Name="Directory" ,HeaderText="Directory" ,DataPropertyName="Directory" ,FillWeight=12f ,SortMode=DataGridViewColumnSortMode.Automatic});
			dgv.DataError        += (s,e)=>{ e.Cancel = true; };
			dgv.KeyDown          += (s,e)=>{ if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Return || e.KeyCode == m_menu_media_list.ShortcutKeys) m_media_list_ui.Hide(); };
			dgv.KeyUp            += (s,e)=>{ };
			dgv.DragOver         += (s,e)=>{ MainView_DragOver(e); };
			dgv.DragDrop         += (s,e)=>{ MainView_DragDrop(e); };
			dgv.DoubleClick      += (s,e)=>{ LoadCurrent(); };
			
			Form form = new Form{Text = "Media File List", FormBorderStyle = FormBorderStyle.SizableToolWindow, Size = new Size(600,800)};
			form.FormClosing += (s,e)=>{ if (e.CloseReason == CloseReason.UserClosing) {m_media_list_ui.Hide(); e.Cancel = true; BringToFront(); } };
			form.Controls.Add(dgv);
			return form;
		}

		/// <summary>True if the mouse has moved from 'm_mouse_location'</summary>
		private bool MouseMoved
		{
			get { return Util.Moved(MousePosition, m_mouse_location); }
		}

		/// <summary>Ensures the controls form is visible and pushes out the time till it fades</summary>
		private void ShowControls()
		{
			const int show_period = 1500;
			
			m_controls.Visible = true;
			m_hide_timer.Tag = Environment.TickCount + show_period;
			m_hide_timer.Enabled = true;
		}

		/// <summary>Mouse double click handler</summary>
		private void OnMouseDoubleClick(object sender, MouseEventArgs e)
		{
			ToggleFullScreen();
		}

		/// <summary>Mouse move handler</summary>
		private void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (MouseMoved)
			{
				ShowControls();
				Util.ShowCursor = true;
				Action ResetMouseLocation = () => { m_mouse_location = MousePosition; };
				BeginInvoke(ResetMouseLocation, null);
			}
		}

		/// <summary>Called when the settings are changed</summary>
		private void OnSettingsChanged(Settings settings, string prop)
		{
			Log.Info(this, "SettingsChanged - "+prop+"\n");
			switch (prop)
			{
			default: break;
			case "MediaType":
			case "ImageExtensions":
			case "AudioExtensions":
			case "VideoExtensions":
				if (m_mode == EMode.Normal) // The criteria for what is a media file has changed, rebuild the list
					BuildMediaListAsync(m_settings.MediaPaths, false, false);
				break;
			case "PrimaryDisplay":
				break;
			case "AlwaysOnTop":
				TopMost = m_settings.AlwaysOnTop;
				break;
			case "StartupVersionCheck":
				break;
			case "AllowDuplicates":
				if (m_mode == EMode.Normal) // Rebuild the list with or without duplicates
					BuildMediaListAsync(m_settings.MediaPaths, false, false);
				break;
			case "ResetZoomOnLoad":
				break;
			case "ZoomType":
				ResetZoom();
				if (m_view3d.Visible) m_view3d.Refresh();
				break;
			case "SlideShowRate":
				m_slide_show.Interval = m_settings.SlideShowRate * 1000;
				break;
			case "Volume":
				if (m_mode == EMode.Normal && m_video.Video != null) m_video.Video.Volume = m_settings.Volume;
				break;
			case "SSVolume":
				if (m_mode == EMode.ScreenSaver && m_video.Video != null) m_video.Video.Volume = m_settings.SSVolume;
				break;
			case "FilesOrder":
			case "FolderOrder":
				if (m_mode == EMode.Normal) SortMediaListAsync();
				break;
			case "SSFilesOrder":
			case "SSFolderOrder":
				if (m_mode == EMode.ScreenSaver) SortMediaListAsync();
				break;
			case "ShowFilenames":
				PhotoLabel = PhotoLabel;
				break;
			case "RenderQuality":
				break;
			case "MediaPaths":
				if (m_mode == EMode.Normal)
				{
					if (MessageBox.Show(this,
						"Would you like to rebuild the media list based on these directories?",
						"Rebuild Media List?",
						MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
						BuildMediaListAsync(m_settings.MediaPaths, m_media_list.Current == null, false);
				}
				break;
			case "SSMediaPaths":
				if (m_mode == EMode.ScreenSaver)
					BuildMediaListAsync(m_settings.SSMediaPaths, false, false);
				break;
			}
		}

		/// <summary>Handler that causes the app to exit when the mouse has moved</summary>
		private void MouseMoveCausesExit(object s, MouseEventArgs e)
		{
			if (!MouseMoved) return;
			Util.ShowCursor = true;
			if (!m_media_list_ui.Visible) Close();
		}

		/// <summary>Handle key input</summary>
		private void HandleKeys(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
			default:
				if (m_mode == EMode.ScreenSaver) { Util.ShowCursor = true; Close(); e.Handled = true; }
				if (e.KeyCode == m_menu_media_list.ShortcutKeys && m_media_list_ui.Visible) { m_media_list_ui.Hide(); e.Handled = true; }
				break;
			case Keys.Left:
				Prev();
				LoadCurrent();
				e.Handled = true;
				break;
			case Keys.Right:
				Next();
				LoadCurrent();
				e.Handled = true;
				break;
			case Keys.Escape:
				if      (m_media_list_ui.Visible) { m_media_list_ui.Hide(); e.Handled = true; }
				else if (ViewType == EViewType.FullScreen) { ViewType = EViewType.Normal; e.Handled = true; }
				break;
			case Keys.Return:
				if (m_media_list_ui.Visible) { m_media_list_ui.Hide(); e.Handled = true; }
				break;
			case Keys.Space:
				if (!m_controls.Visible) m_controls.Show(this);
				break;
			}
		}

		/// <summary>Update the status message</summary>
		private string Status
		{
			set { m_msg.Text = value; }
		}

		/// <summary>Set the string for the photo label</summary>
		private string PhotoLabel
		{
			get { return m_lbl_msg.Text; }
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					m_lbl_msg.Text = "";
					m_lbl_msg.Visible = false;
				}
				else
				{
					m_lbl_msg.Text = value;
					m_lbl_msg.Location = new Point((ClientRectangle.Width - m_lbl_msg.Width) / 2, 2+(ViewType == EViewType.Normal ? m_menu.Height : 0));
					m_lbl_msg.Visible   = m_settings.ShowFilenames && m_mode == EMode.Normal;
				}
			}
		}

		/// <summary>Set the image info string</summary>
		private object ImageInfo
		{
			set
			{
				if (value is View3D.Texture)
				{
					View3D.Texture tex = (View3D.Texture)value;
					m_lbl_image_info.Text = tex.m_info.m_width + " x " + tex.m_info.m_height + " pixels";
				}
				else if (value is Video)
				{
					Video vid = (Video)value;
					m_lbl_image_info.Text = vid.NativeSize.Width + " x " + vid.NativeSize.Height + " pixels";
				}
				else
				{
					m_lbl_image_info.Text = "";
				}
			}
		}

		/// <summary>Check for udpates. Returns true if a new version is available</summary>
		private void CheckForUpdates(bool show_dialogs)
		{
			Action<INet.CheckForUpdateResult,Exception> handle_c4u = (res,err) =>
				{
					Version this_version = Assembly.GetExecutingAssembly().GetName().Version;
					if (res == null)
					{
						Status = "Version information not found.";
						if (show_dialogs) MessageBox.Show(this,
							"Failed to retrieve version information from "+m_settings.UpdateURL+".\n\n"+
							"Reason: "+err.Message,
							"Check for Updates",
							MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else if (this_version.CompareTo(res.Version) >  0)
					{
						Status = "Development version running";
						if (show_dialogs) MessageBox.Show(this, "This is newer than the latest version", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else if (this_version.CompareTo(res.Version) == 0)
					{
						Status = "Latest version running";
						if (show_dialogs) MessageBox.Show(this, "This is the latest version", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						Status = "New Version Available!";
						if (show_dialogs)
						{
							if (MessageBox.Show(this,
								"A new version is available!\n"+
								"Current version: "+this_version+"\n"+
								"Latest version: "+res.Version+"\n"+
								"Website: "+(res.DownloadURL??"<Website URL not available>")+"\n"+
								"\n"+
								"Visit website to download the latest version?",
								"New Version Detected",
								MessageBoxButtons.YesNo,
								MessageBoxIcon.Question) == DialogResult.Yes)
							{
								if (res.DownloadURL != null) Process.Start(res.DownloadURL);
								else MessageBox.Show(this, "The download URL is not available. Please update manually", "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
							}
						}
					}
				};

			Status = "Checking for newer version...";
			#if PLATFORM_X64
			INet.BeginCheckForUpdate("imager.x64", m_settings.UpdateURL, ar =>
			#elif PLATFORM_X86
			INet.BeginCheckForUpdate("imager.x86", m_settings.UpdateURL, ar =>
			#endif
				{
					INet.CheckForUpdateResult res = null; Exception error = null;
					try { res = INet.EndCheckForUpdate(ar); }
					catch (OperationCanceledException) {}
					catch (Exception ex) { error = ex; }
					handle_c4u(res, error);
				});
			
		}

		///// <summary>Transition to 'new_image'</summary>
		//private void DoTransition(ViewImageControl view, Image new_image)
		//{
		//    if (!SlideShow) { view.Image = new_image; return; }

		//    // Pick a transition at random from the enabled ones
		//    int bit = m_rng.Next(0, Bit.CountBits(m_settings.TransitionsMask));
		//    Transition trans = m_transitions[Bit.BitIndex(m_settings.TransitionsMask, bit)];

		//    m_slide_show.Interval = (int)(m_settings.SlideShowRate * 1000) + trans.Duration;
		//    trans.Start(view, new_image);
		//}

		///// <summary>Display a dialog allowing the user to set which transitions to use</summary>
		//private void ShowTransitionsDlg()
		//{
		//    uint mask = m_settings.TransitionsMask;

		//    CheckedListBox list = new CheckedListBox{Dock=DockStyle.Fill, IntegralHeight=false, CheckOnClick=true, Tag=true};
		//    for (int i = 0; i != m_transitions.Length; ++i)
		//    {
		//        Transition t = m_transitions[i];
		//        list.Items.Add(t, Bit.AllSet(mask, 1U << i));
		//    }
		//    list.Items.Add("All", Bit.AllSet(mask, Transition.AllMask));
		//    list.ItemCheck += (s,e)=>
		//        {
		//            if ((bool)list.Tag) list.Tag = false; else return;
		//            bool chk = e.NewValue == CheckState.Checked;
		//            if (e.Index == m_transitions.Length) // If it's the last entry, toggle all
		//                for (int i = 0; i != m_transitions.Length; ++i) list.SetItemChecked(i, chk);
		//            else
		//                Bit.SetBits(ref mask, 1U << e.Index, chk);
		//            list.SetItemChecked(m_transitions.Length, Bit.AllSet(mask, Transition.AllMask));
		//            list.Tag = true; // enable reentrancy
		//        };

		//    Form form = new Form{Text="Select Transitions", FormBorderStyle=FormBorderStyle.SizableToolWindow, Size = new Size(300,400), StartPosition=FormStartPosition.CenterParent, ShowInTaskbar=false};
		//    form.Controls.Add(list);
		//    form.ShowDialog(this);
		//    m_settings.TransitionsMask = mask;
		//}

		/// <summary>Update which UI elements are visible and where they are on screen</summary>
		private void UpdateUI()
		{
			Log.Info(this, "UpdateIU - begin\n");
			SuspendLayout();

			// Prevent UI updates while we set the form position
			m_resizing = true;

			// Position the forms
			if (m_view_type == EViewType.Normal)
			{
				Log.Info(this, "UpdateIU - Normal\n");
				FormBorderStyle = FormBorderStyle.Sizable;
				if (m_settings.WindowBounds != Rectangle.Empty) Bounds = m_settings.WindowBounds;
				TopMost = m_settings.AlwaysOnTop;
				foreach (Form form in m_blanks) form.Visible = false;
			}
			else if (m_view_type == EViewType.ChildWindow)
			{
				Log.Info(this, "UpdateIU - Child Window\n");
				Win32.RECT rect; Win32.GetClientRect(m_parent, out rect);
				FormBorderStyle = FormBorderStyle.None;
				Size = rect.ToSize();
				Location = Point.Empty;
				foreach (Form form in m_blanks) form.Visible = false;
			}
			else if (m_view_type == EViewType.FullScreen)
			{
				FormBorderStyle = FormBorderStyle.None;
				int display_count = Screen.AllScreens.Length;
				int primary = Maths.Clamp(m_settings.PrimaryDisplay, 0, display_count);
				if (m_mode == EMode.ScreenSaver)
				{
					Log.Info(this, "UpdateIU - Full Screen - screen saver\n");
					primary = (primary != 0) ? primary - 1 : m_rng.Next(display_count);
					for (int i = 0, j = 0; i != display_count; ++i)
					{
						Rectangle rect = Screen.AllScreens[i].Bounds;
						#if (DEBUG)
						{	
						rect.Width = 200;
						rect.Height = 200;
						}
						#endif
						Form form = i == primary ? this : m_blanks[j++];
						form.Visible   = true;
						form.TopMost   = true;
						form.Location  = rect.Location; // have to do these separately
						form.Size      = rect.Size;     // setting Bounds doesn't work
					}
				}
				else
				{
					Log.Info(this, "UpdateIU - Full Screen - normal mode\n");
					foreach (Form form in m_blanks) form.Visible = false;
					Screen scrn = primary == 0 ? Screen.FromHandle(Handle) : Screen.AllScreens[primary-1];
					WindowState = FormWindowState.Normal;
					Bounds = scrn.Bounds;
					Visible = true;
					TopMost = true;
				}
			}

			// Allow UI updates again
			m_resizing = false;

			// Position the view controls within the forms
			switch (m_view_type)
			{
			case EViewType.Normal:
				m_video.Location   = m_view3d.Location = new Point(0,m_menu.Height);
				m_video.Size       = m_view3d.Size     = new Size(ClientSize.Width, ClientSize.Height - m_menu.Height - m_status.Height);
				m_lbl_msg.Location = new Point((Width - m_lbl_msg.Width) / 2, 2+m_menu.Height);
				break;
			case EViewType.ChildWindow:
			case EViewType.FullScreen:
				m_video.Location    = m_view3d.Location = Point.Empty;
				m_video.Size        = m_view3d.Size     = Size;
				m_lbl_msg.Location  = new Point((Width - m_lbl_msg.Width) / 2, 2);
				break;
			}

			m_menu.Visible   = m_view_type == EViewType.Normal;
			m_status.Visible = m_view_type == EViewType.Normal;
			
			if (m_media_list_ui.Visible) m_media_list_ui.BringToFront();
			if (m_dir_manager.Visible) m_dir_manager.BringToFront();

			Log.Info(this, "UpdateIU - end: ["+Location.X+","+Location.Y+","+Size.Width+","+Size.Height+"]\n");
			ResumeLayout();
		}

		/// <summary>Return the file path of the media list cache file</summary>
		private static string MediaListCacheFilepath
		{
			get { return Path.ChangeExtension(Application.ExecutablePath, ".cache"); }
		}

		/// <summary>Returns true if 'file' is a supported image file format</summary>
		private static bool MatchExtn(string file, string extn)
		{
			string ext = Path.GetExtension(file);
			foreach (string e in extn.Split(';'))
				if (string.Compare(ext, "."+e.Substring(1), StringComparison.OrdinalIgnoreCase) == 0)
					return true;
			return false;
		}
	}
}
