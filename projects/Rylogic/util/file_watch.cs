//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Timer = System.Windows.Forms.Timer;

namespace pr.util
{
	// Consider using a 'System.IO.FileSystemWatcher' before using this helper
	// Note however that FileSystemWatcher does not notify of file changed immediately.
	// Something has to touch the file before the OS notices it change. For watching a
	// file with high frequency, polling is the best method

	// Note about worker threads:
	// It's tempting to try and make this type a worker thread that notifies the client when
	// a file has changed. However this requires cross-thread marshalling which is only possible
	// if the client has a message queue. There are three possibilities;
	//   1) the client is a window - could use SendMessage() to notify the client (SendMessage
	//      marshals across threads) however it doesn't make sense for the FileWatch type to
	//      require a windows handle
	//   2) use PostThreadMessage - this has synchronisation problems i.e. notifications occur for
	//      all changed files plus the filename cannot be passed to the client without allocation
	//   3) use a custom message queue system - this would require the client to poll their message
	//      queue, in which case they might as well just poll the FileWatch object.

	/// <summary>File watcher</summary>
	public class FileWatch
	{
		private class WatchedFile
		{
			public readonly FileInfo           m_info;           // File info
			public readonly string             m_filepath;       // The filepath of the file to watch
			public readonly FileChangedHandler m_onchange;       // The client to callback when a changed file is found
			public readonly int                m_id;             // A user provided id used to identify groups of watched files
			public readonly object             m_ctx;            // User provided context data
			public long                        m_stamp0;         // The last time the file was modified
			public long                        m_stamp1;         // The new timestamp for the file, copied to stamp0 when the file change is handled
			public long                        m_size0;          // The last recorded size of the file
			public long                        m_size1;          // The new size for the file, copied to size0 when the file change is handled
			public bool                        m_exists0;        // Whether the file existed last time we checked
			public bool                        m_exists1;        // The new existence state of the file
	
			public WatchedFile(string filepath, FileChangedHandler onchange, int id, object ctx)
			{
				m_info     = new FileInfo(filepath);
				m_filepath = filepath;
				m_onchange = onchange;
				m_id       = id;
				m_ctx      = ctx;
				m_stamp0   = m_info.Exists ? m_info.LastWriteTimeUtc.Ticks : 0;
				m_stamp1   = m_stamp0;
				m_size0    = m_info.Exists ? m_info.Length : 0;
				m_size1    = m_size0;
				m_exists0  = m_info.Exists;
				m_exists1  = m_exists0;
			}
		}
		private readonly List<WatchedFile> m_files;
		private readonly List<WatchedFile> m_changed_files; // Recycle the changed files collection
		private readonly Timer m_timer;
		private bool m_in_check_for_changes;
		
		/// <summary>
		/// File changed callback. Return true if the change was handled
		/// Returning false causes the FileChanged notification to happen
		/// again next time CheckForChangedFiles() is called. </summary>
		public delegate bool FileChangedHandler(string filepath, object ctx);

		public FileWatch()
		{
			m_files          = new List<WatchedFile>();
			m_timer          = new Timer{Interval = 1000, Enabled = false};
			m_timer.Tick    += CheckForChangedFiles;
			m_changed_files  = new List<WatchedFile>();
		}
		public FileWatch(FileChangedHandler on_changed, params string[] files)
			:this(on_changed, 0, null, files)
		{}
		public FileWatch(FileChangedHandler on_changed, int id, object ctx, params string[] files)
			:this()
		{
			foreach (string file in files)
				m_files.Add(new WatchedFile(file, on_changed, id, ctx));
		}

		/// <summary>The collection of watched filepaths</summary>
		public IEnumerable<string> Files
		{
			get { return m_files.Select(x => x.m_filepath); }
		}

		/// <summary>Get/Set the auto-polling period. If the period is 0 polling is disabled</summary>
		public int PollPeriod
		{
			get { return m_timer.Enabled ? m_timer.Interval : 0; }
			set { if (value == 0) m_timer.Enabled = false; else { m_timer.Interval = value; m_timer.Enabled = true; } }
		}

		/// <summary>Add a file to be watched.</summary>
		public void Add(string filepath, FileChangedHandler on_changed)
		{
			Remove(filepath);
			m_files.Add(new WatchedFile(filepath, on_changed, 0, null));
		}

		/// <summary>Add a set of files to be watched.</summary>
		public void Add(IEnumerable<string> filepaths, FileChangedHandler on_changed)
		{
			foreach (var f in filepaths)
				Add(f, on_changed);
		}

		/// <summary>Add a file to be watched.
		/// 'id' is a user provided id for identifying file groups
		/// 'ctx' is a user provided context passed back in the 'on_changed' callback</summary>
		public void Add(string filepath, FileChangedHandler on_changed, int id, object ctx)
		{
			Remove(filepath);
			m_files.Add(new WatchedFile(filepath, on_changed, id, ctx));
		}

		/// <summary>Stop watching a set of files</summary>
		public void Remove(IEnumerable<string> filepaths)
		{
			foreach (var f in filepaths)
				Remove(f);
		}

		/// <summary>Stop watching a file</summary>
		public void Remove(string filepath)
		{
			m_files.RemoveAll(f => f.m_filepath == filepath);
		}

		/// <summary>Remove all watches matching 'id'</summary>
		public void RemoveAll(int id)
		{
			m_files.RemoveAll(f => f.m_id == id);
		}

		/// <summary>Remove all watched files</summary>
		public void RemoveAll()
		{
			m_files.Clear();
		}

		/// <summary>Check the collection of filepaths for those that have changed</summary>
		public void CheckForChangedFiles(object sender = null, EventArgs args = null)
		{
			// Prevent reentrancy.
			// This is not done in a worker thread because the main blocking call is
			// 'f.m_info.Refresh()' which blocks all threads so there's no point.
			if (!m_in_check_for_changes) try
			{
				m_in_check_for_changes = true;
				m_changed_files.Clear();
				foreach (var f in m_files)
				{
					f.m_info.Refresh();
					
					bool exists = f.m_info.Exists;
					long size   = exists ? f.m_info.Length : 0;
					long stamp  = exists ? f.m_info.LastWriteTimeUtc.Ticks : 0;
					
					if (f.m_stamp0 != stamp || f.m_size0 != size || f.m_exists0 != f.m_exists1) 
						m_changed_files.Add(f);
				
					f.m_stamp1  = stamp;
					f.m_size1   = size;
					f.m_exists1 = exists;
				}

				// Report each changed file
				foreach (var f in m_changed_files)
				{
					if (f.m_onchange(f.m_filepath, f.m_ctx))
					{
						f.m_stamp0  = f.m_stamp1;
						f.m_size0   = f.m_size1;
						f.m_exists0 = f.m_exists1;
					}
				}
			}
			finally { m_in_check_for_changes = false; }
		}
	}
}
