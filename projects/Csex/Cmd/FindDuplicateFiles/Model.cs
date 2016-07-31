using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using pr.common;
using pr.container;
using pr.extn;
using pr.gfx;
using pr.gui;

namespace Csex
{
	public class Model
	{
		private readonly Control m_parent;
		public Model(Control parent)
		{
			m_parent    = parent;
			Settings    = new Settings(Settings.DefaultLocalFilepath){AutoSaveOnChanges = true};
			FInfoMap    = new FileInfoMap();
			Duplicates  = new BindingListEx<FileInfo>();
			Errors      = new List<string>();
		}

		/// <summary>Application settings</summary>
		public Settings Settings { get; private set; }

		/// <summary>Data for a file</summary>
		public class FileInfo :Path_.FileData
		{
			public FileInfo(Path_.FileData fi) :base(fi)
			{
				Key = MakeKey(this);
				Duplicates = new BindingListEx<FileInfo>();
			}

			/// <summary>A unique identifier for this file</summary>
			public string Key { get; private set; }

			/// <summary>The number of copies of this file</summary>
			public int CopyCount { get { return Duplicates.Count; } }

			/// <summary>Other files that are duplicates of this one</summary>
			public BindingListEx<FileInfo> Duplicates { get; private set; }

			/// <summary>Create a key for 'fi'</summary>
			public static string MakeKey(FileInfo fi)
			{
				// Generate a key for the file
				var fname = fi.FileName.ToLowerInvariant();

				// Special case jpgs
				if (Exif.IsJpgFile(fi.FullPath))
				{
					using (var fs = new FileStream(fi.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						var exif = Exif.Read(fs, false);
						if (exif != null && exif.HasTag(Exif.Tag.DateTimeOriginal))
						{
							var dat = exif[Exif.Tag.DateTimeOriginal];
							var ts = dat.AsString;
							return ts + "-" + fname;
						}
					}
				}

				// Include the file size in the key
				return fi.FileSize +  "-" + fname;
			}

			/// <summary>FileInfo comparer</summary>
			public static Cmp<FileInfo> Compare = Cmp<FileInfo>.From((l,r) => string.CompareOrdinal(l.Key,r.Key));
		}
		public class FileInfoMap :Dictionary<string,FileInfo> {}

		/// <summary>A map of file info for the found files</summary>
		public FileInfoMap FInfoMap { get; private set; }

		/// <summary>The found duplicates</summary>
		public BindingListEx<FileInfo> Duplicates { get; private set; }

		/// <summary>Errors encountered when finding duplicates</summary>
		public List<string> Errors { get; private set; }

		/// <summary>Find the duplicates</summary>
		public void FindDuplicates()
		{
			using (var dlg = new ProgressForm("Finding Duplicates...", string.Empty, SystemIcons.Information, ProgressBarStyle.Marquee, FindDuplicates))
				dlg.ShowDialog(m_parent);
		}

		/// <summary>Does the work of finding and identifying duplicates</summary>
		private void FindDuplicates(ProgressForm dlg, object ctx, ProgressForm.Progress progress)
		{
			// Build a map of file data
			var dir = string.Empty;
			foreach (var path in Settings.SearchPaths)
			{
				if (dlg.CancelPending) break;
				foreach (var fi in Path_.EnumFileSystem(path, search_flags:SearchOption.AllDirectories).Where(x => !x.IsDirectory))
				{
					if (dlg.CancelPending) break;

					// Report progress whenever the directory changes
					var d = Path.GetDirectoryName(fi.FullPath) ?? string.Empty;
					if (d != dir)
					{
						dir = d;
						progress(new ProgressForm.UserState{Description = "Scanning files...\r\n{0}".Fmt(dir)});
					}

					try
					{
						// Create file info for the file and look for a duplicate
						var finfo = new FileInfo(fi);
						FileInfo existing = FInfoMap.TryGetValue(finfo.Key, out existing) ? existing : null;
						if (existing != null)
						{
							m_parent.Invoke(() =>
								{
									existing.Duplicates.Add(finfo);
									var idx = Duplicates.BinarySearch(existing, FileInfo.Compare);
									if (idx < 0) Duplicates.Insert(~idx, existing);
								});
						}
						else
						{
							FInfoMap.Add(finfo.Key, finfo);
						}
					}
					catch (Exception ex)
					{
						Errors.Add("Failed to add {0} to the map. {1}".Fmt(fi.FullPath, ex.Message));
					}
				}
			}
		}
	}
}

