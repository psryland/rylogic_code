//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Common
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
	public class FileWatch :IDisposable
	{
		private readonly List<IWatcher> m_watched;
		private readonly List<IWatcher> m_changed; // Recycle the changed files collection
		private readonly CancellationToken m_shutdown;
		private bool m_disposed;

		public FileWatch(CancellationToken? shutdown = null)
		{
			m_watched = new List<IWatcher>();
			m_changed = new List<IWatcher>();
			m_shutdown = shutdown ?? CancellationToken.None;
			m_cancel = null;
		}
		public FileWatch(CancellationToken? shutdown, ChangedHandler on_changed, params string[] files)
			:this(shutdown, on_changed, 0, null, files)
		{}
		public FileWatch(CancellationToken? shutdown, ChangedHandler on_changed, int id, object? ctx, params string[] files)
			:this(shutdown)
		{
			foreach (string file in files)
				m_watched.Add(new WatchedFile(file, on_changed, id, ctx));
		}
		public virtual void Dispose()
		{
			PollPeriod = TimeSpan.Zero;
			m_disposed = true;
		}

		/// <summary>Get/Set the auto-polling period. If the period is 0 polling is disabled</summary>
		public TimeSpan PollPeriod
		{
			get => m_poll_period;
			set
			{
				if (m_disposed) throw new ObjectDisposedException("FileWatch disposed. Do not set PollPeriod");
				if (m_poll_period == value) return;
				if (m_poll_period != TimeSpan.Zero)
				{
					m_cancel?.Cancel();
					m_cancel = null;
				}
				m_poll_period = value;
				if (m_poll_period != TimeSpan.Zero)
				{
					m_cancel = CancellationTokenSource.CreateLinkedTokenSource(m_shutdown);
					DoWatch(m_cancel.Token);
				}

				// Async loop to watch for changed files
				async void DoWatch(CancellationToken cancel)
				{
					try
					{
						for (; !cancel.IsCancellationRequested;)
						{
							CheckForChangedFiles();
							await Task.Delay(m_poll_period, cancel);
						}
					}
					catch (OperationCanceledException) { }
				}
			}
		}
		private CancellationTokenSource? m_cancel;
		private TimeSpan m_poll_period;

		/// <summary>The collection of watched paths</summary>
		public IEnumerable<string> Paths => m_watched.Select(x => x.Path);

		/// <summary>
		/// Add a file or directory to be watched.
		/// 'id' is a user provided id for identifying file groups.
		/// 'ctx' is a user provided context passed back in the 'on_changed' callback</summary>
		public void Add(string path, ChangedHandler on_changed)
		{
			Add(path, on_changed, 0, null);
		}
		public void Add(string path, ChangedHandler on_changed, int id, object? ctx)
		{
			Remove(path);
			if (Path_.IsDirectory(path))
				m_watched.Add(new WatchedDir(path, on_changed, id, ctx));
			else
				m_watched.Add(new WatchedFile(path, on_changed, id, ctx));
		}
		public void Add(IEnumerable<string> paths, ChangedHandler on_changed, int id = 0, object? ctx = null)
		{
			foreach (var f in paths)
				Add(f, on_changed, id, ctx);
		}

		/// <summary>Stop watching a file or directory</summary>
		public void Remove(string path)
		{
			Remove(new[] { path });
		}

		/// <summary>Stop watching a set of files</summary>
		public void Remove(IEnumerable<string> filepaths)
		{
			var set = filepaths.ToHashSet(x => x.ToLowerInvariant());
			m_watched.RemoveAll(x => set.Contains(x.Path.ToLowerInvariant()));
		}

		/// <summary>Remove all watches matching 'id'</summary>
		public void RemoveAll(int id)
		{
			m_watched.RemoveAll(f => f.Id == id);
		}

		/// <summary>Remove all watched files</summary>
		public void RemoveAll()
		{
			m_watched.Clear();
		}

		/// <summary>Check the collection of filepaths for those that have changed</summary>
		public void CheckForChangedFiles(object? sender = null, EventArgs? args = null)
		{
			// Prevent reentrancy.
			// This is not done in a worker thread because the main blocking call is
			// 'f.m_info.Refresh()' which blocks all threads so there's no point.
			if (m_in_check_for_changes != 0) return;
			using (Scope.Create(() => ++m_in_check_for_changes, () => --m_in_check_for_changes))
			{
				// Build a collection of items that have changed
				m_changed.Assign(m_watched.Where(x => x.HasChanged));

				// Report each changed item
				foreach (var item in m_changed)
					item.NotifyChanged();
			}
		}
		private int m_in_check_for_changes;

		/// <summary>
		/// Item changed callback. Return true if the change was handled.
		/// Returning false causes the FileChanged notification to happen
		/// again next time CheckForChangedFiles() is called. </summary>
		public delegate bool ChangedHandler(string filepath, object? ctx);

		/// <summary>Interface for a watch on the file system</summary>
		private interface IWatcher
		{
			/// <summary>A user provided ID for the watcher</summary>
			int Id { get; }

			/// <summary>The file or directory path being watched</summary>
			string Path { get; }

			/// <summary>True if the watched item has changed</summary>
			bool HasChanged { get; }

			/// <summary>Notify observers that this item has changed. Returns true if handled</summary>
			bool NotifyChanged();
		}

		/// <summary></summary>
		private class WatchedFile : IWatcher
		{
			public readonly FileInfo m_info;           // File info
			public readonly ChangedHandler m_onchange; // The client to callback when a changed file is found
			public readonly object? m_ctx;             // User provided context data
			public bool m_exists0, m_exists1;          // Whether the file existed last time we checked
			public long m_stamp0, m_stamp1;            // The last time the file was modified
			public long m_size0, m_size1;              // The last recorded size of the file

			public WatchedFile(string filepath, ChangedHandler onchange, int id, object? ctx)
			{
				Path = filepath;
				m_onchange = onchange;
				m_ctx = ctx;
				Id = id;
				m_info = new FileInfo(filepath);
				m_exists0 = m_info.Exists;
				m_stamp0 = m_exists0 ? m_info.LastWriteTimeUtc.Ticks : 0;
				m_size0 = m_exists0 ? m_info.Length : 0;
				m_exists1 = m_exists0;
				m_stamp1 = m_stamp0;
				m_size1 = m_size0;
			}

			/// <summary>A user provided ID for the watcher</summary>
			public int Id { get; }

			/// <summary>The file path being watched</summary>
			public string Path { get; }

			/// <summary>True if the watched item has changed</summary>
			public bool HasChanged
			{
				get
				{
					m_info.Refresh();

					var exists = m_info.Exists;
					var size = exists ? m_info.Length : 0;
					var stamp = exists ? m_info.LastWriteTimeUtc.Ticks : 0;
					var changed = m_exists0 != exists || m_stamp0 != stamp || m_size0 != size;

					m_exists1 = exists;
					m_stamp1 = stamp;
					m_size1 = size;

					return changed;
				}
			}

			/// <summary>Notify observers that this item has changed</summary>
			public bool NotifyChanged()
			{
				if (!m_onchange(Path, m_ctx))
					return false;
				
				// Only update the reference values when the change is acknowledged
				m_stamp0 = m_stamp1;
				m_size0 = m_size1;
				m_exists0 = m_exists1;
				return true;
			}

			/// <summary></summary>
			public override string ToString() => $"Watching: {Path}";
		}

		/// <summary></summary>
		private class WatchedDir : IWatcher
		{
			private readonly Dictionary<string, WatchedFile> m_files; // The files in 'Path'
			private readonly ChangedHandler m_onchange; // The client to callback when a file is changed within the directory
			private readonly object? m_ctx;

			public WatchedDir(string dirpath, ChangedHandler onchange, int id, object? ctx)
			{
				m_files = new Dictionary<string, WatchedFile>();
				Path = dirpath;
				m_onchange = onchange;
				m_ctx = ctx;
				Id = id;

				// Get the files in the directory and create a watcher for each
				var files = Path_.EnumFileSystem(Path, SearchOption.TopDirectoryOnly, exclude: FileAttributes.Hidden | FileAttributes.Directory).ToList();
				m_files = files.ToDictionary(x => x.FullName.ToLowerInvariant(), x => new WatchedFile(x.FullName, onchange, id, ctx));
			}

			/// <summary>User provided ID</summary>
			public int Id { get; private set; }

			/// <summary>The directory path being watched</summary>
			public string Path { get; private set; }

			/// <summary>True if the watched item has changed</summary>
			public bool HasChanged
			{
				get
				{
					// Get the new files in the directory
					var new_files = Path_.EnumFileSystem(Path, SearchOption.TopDirectoryOnly, exclude: FileAttributes.Hidden | FileAttributes.Directory).Any(x => !m_files.ContainsKey(x.FullName.ToLowerInvariant()));
					return new_files || m_files.Values.Any(x => x.HasChanged);
				}
			}

			/// <summary>Notify observers about the changed items</summary>
			public bool NotifyChanged()
			{
				// Scan the directory and notify about created, deleted, or changed files
				var current_files = Path_.EnumFileSystem(Path, SearchOption.TopDirectoryOnly, exclude: FileAttributes.Hidden | FileAttributes.Directory)
					.Select(x => x.FullName).ToList();
				var existing = current_files
					.ToHashSet(x => x.ToLowerInvariant());

				foreach (var path in current_files)
				{
					// If there is an existing watcher for the file, simply check for changed
					if (m_files.TryGetValue(path.ToLowerInvariant(), out var file))
					{
						// File unchanged
						if (!file.HasChanged)
							continue;

						// File changed, but change not handled
						if (!file.NotifyChanged())
							continue;

						// File no longer exists, remove from 'm_files'
						if (!Path_.FileExists(file.Path))
							m_files.Remove(file.Path.ToLowerInvariant());

						continue;
					}

					// If this is a new file, notify then add
					file = new WatchedFile(path, m_onchange, Id, m_ctx);
					if (file.NotifyChanged())
						m_files[path.ToLowerInvariant()] = file;
				}

				return true;
			}

			/// <summary></summary>
			public override string ToString() => $"Watching: {Path}";
		}
	}
}
