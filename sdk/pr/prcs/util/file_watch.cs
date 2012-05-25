//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace pr.util
{
	// Consider using a 'System.IO.FileSystemWatcher' before using this helper

	// Note about worker threads:
	// It's tempting to try and make this type a worker thread that notifies the client when
	// a file has changed. However this requires cross-thread marshelling which is only possible
	// if the client has a message queue. There are three possibilities;
	//   1) the client is a window - could use SendMessage() to notify the client (SendMessage
	//      marshalls across threads) however it doesn't make sense for the FileWatch type to
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
			public readonly string             m_filepath;       // The filepath of the file to watch
			public readonly FileChangedHandler m_onchange;       // The client to callback when a changed file is found
			public readonly int                m_id;             // A user provided id used to identify groups of watched files
			public readonly object             m_ctx;            // User provided context data
			public long                        m_stamp0;         // The last time the file was modified
			public long                        m_stamp1;         // The new timestamp for the file, copied to stamp0 when the file change is handled
	
			public WatchedFile(string filepath, FileChangedHandler onchange, int id, object ctx)
			{
				m_filepath = filepath;
				m_onchange = onchange;
				m_id       = id;
				m_ctx      = ctx;
				m_stamp0   = File.GetLastWriteTimeUtc(m_filepath).Ticks;
				m_stamp1   = m_stamp0;
			}
		}
		private readonly List<WatchedFile> m_files = new List<WatchedFile>();
		private readonly Timer             m_timer = new Timer{Interval = 1000, Enabled = false};

		// File changed callback. Return true if the change was handled
		// Returning false causes the FileChanged notification to happen
		// again next time CheckForChangedFiles() is called.
		public delegate bool FileChangedHandler(string filepath, object ctx);

		public FileWatch() {}
		public FileWatch(FileChangedHandler on_changed, params string[] files) :this(on_changed, 0, null, files) {}
		public FileWatch(FileChangedHandler on_changed, int id, object ctx, params string[] files)
		{
			foreach (string file in files)
				m_files.Add(new WatchedFile(file, on_changed, id, ctx));
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

		/// <summary>Add a file to be watched.
		/// 'id' is a user provided id for identifying file groups
		/// 'ctx' is a user provided context passed back in the 'on_changed' callback</summary>
		public void Add(string filepath, FileChangedHandler on_changed, int id, object ctx)
		{
			Remove(filepath);
			m_files.Add(new WatchedFile(filepath, on_changed, id, ctx));
		}

		/// <summary>Stop watching a file</summary>
		public void Remove(string filepath)
		{
			m_files.RemoveAll(delegate (WatchedFile f) {return f.m_filepath == filepath;});
		}

		/// <summary>Remove all watches matching 'id'</summary>
		public void RemoveAll(int id)
		{
			m_files.RemoveAll(delegate (WatchedFile f) {return f.m_id == id;});
		}

		/// <summary>Remove all watched files</summary>
		public void RemoveAll()
		{
			m_files.Clear();
		}

		/// <summary>Check the collection of filepaths for those that have changed</summary>
		public void CheckForChangedFiles()
		{
			// Build a collection of the changed files to prevent re-entrancy problems with the callbacks
			List<WatchedFile> changed_files = new List<WatchedFile>();
			foreach (WatchedFile f in m_files)
			{
				long stamp = File.GetLastWriteTimeUtc(f.m_filepath).Ticks;
				if (f.m_stamp0 != stamp) { changed_files.Add(f); }
				f.m_stamp1 = stamp;
			}

			// Report each changed file
			foreach (WatchedFile f in changed_files)
			{
				if (f.m_onchange(f.m_filepath, f.m_ctx))
					f.m_stamp0 = f.m_stamp1;
			}
		}
	}
}
