//***************************************************
// Xml Helper Functions
//  Copyright (c) Rylogic Ltd 2010
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using pr.extn;
using pr.gui;
using pr.util;
using pr.win32;

namespace pr.common
{
	public static class Path_
	{
		/// <summary>True if 'filepath' is a valid filepath. The file doesn't have to exist</summary>
		public static bool IsValidFilepath(string filepath, bool require_rooted)
		{
			try
			{
				if (!filepath.HasValue()) return false;
				var dir = Path.GetDirectoryName(filepath) ?? string.Empty;
				var fname = Path.GetFileName(filepath) ?? string.Empty;
				var invalid_chars = Path.GetInvalidFileNameChars();
				return
					fname.HasValue() &&                         // no filename
					IsValidDirectory(dir, require_rooted) &&    // directory isn't valid
					fname.IndexOfAny(invalid_chars) == -1 &&    // has invalid chars
					!DirExists(filepath);                       // already exists as a directory
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>True if 'filepath' is a valid filepath. The directory doesn't have to exist</summary>
		public static bool IsValidDirectory(string dir, bool require_rooted)
		{
			try
			{
				var invalid_chars = Path.GetInvalidPathChars().Concat(new[]{'*','?'}).ToArray();
				return
					dir != null &&                                 // null isn't valid
					(!require_rooted || Path.IsPathRooted(dir)) && // requires root
					dir.IndexOfAny(invalid_chars) == -1 &&         // has invalid chars
					!FileExists(dir);                              // already exists as a file
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>A File.Exists() that doesn't throw if invalid path characters are used. Handles 'filepath' being null</summary>
		public static bool FileExists(string filepath)
		{
			// Using 'FileInfo' because it checks security permissions as well
			try { return filepath.HasValue() && new FileInfo(filepath).Exists; }
			catch { return false; }
		}

		/// <summary>A Directory.Exists() that doesn't throw if invalid path characters are used. Handles 'directory' being null</summary>
		public static bool DirExists(string directory)
		{
			// Using 'DirectoryInfo' because it checks security permissions as well
			try { return directory.HasValue() && new DirectoryInfo(directory).Exists; }
			catch { return false; }
		}

		/// <summary>True if 'file_or_directory_path' exists as a file or directory</summary>
		public static bool PathExists(string file_or_directory_path)
		{
			return FileExists(file_or_directory_path) || DirExists(file_or_directory_path);
		}

		/// <summary>True if 'path' is a file</summary>
		public static bool IsFile(string path)
		{
			return FileExists(path) && (File.GetAttributes(path) & FileAttributes.Directory) == 0;
		}

		/// <summary>True if 'path' is a directory</summary>
		public static bool IsDirectory(string path)
		{
			return DirExists(path) && (File.GetAttributes(path) & FileAttributes.Directory) != 0;
		}

		/// <summary>True if 'path' is a directory containing no files or subdirectories</summary>
		public static bool IsEmptyDirectory(string path)
		{
			return IsDirectory(path) && !System.IO.Directory.EnumerateFileSystemEntries(path).Any();
		}

		/// <summary>True if 'path' represents a file or directory equal to or below 'directory'</summary>
		public static bool IsSubPath(string directory, string path)
		{
			var d = Canonicalise(directory, change_case:Case.Lower);
			var p = Canonicalise(path, change_case:Case.Lower);
			return p.StartsWith(d);
		}

		///<summary>Returns 'full_file_path' relative to 'rel_path'</summary>
		public static string RelativePath(string full_file_path, string rel_path)
		{
			const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
			const int FILE_ATTRIBUTE_NORMAL = 0x80;
			var path_builder = new StringBuilder(260); // MAX_PATH
			PathRelativePathTo(path_builder, rel_path, FILE_ATTRIBUTE_DIRECTORY, full_file_path, FILE_ATTRIBUTE_NORMAL);
			var path = path_builder.ToString();
			return path.Length == 0 ? full_file_path : path;
		}
		[DllImport("shlwapi.dll")] private static extern int PathRelativePathTo(StringBuilder pszPath, string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);

		/// <summary>Return the directory part of 'path' (or empty string)</summary>
		public static string Directory(string path)
		{
			try { return Path.GetDirectoryName(path) ?? string.Empty; }
			catch { return string.Empty; }
		}

		/// <summary>Return the filename part of 'path' (or empty string)</summary>
		public static string FileName(string path)
		{
			try { return Path.GetFileName(path) ?? string.Empty; }
			catch { return string.Empty; }
		}

		/// <summary>Return the file title part of 'path' (or empty string)</summary>
		public static string FileTitle(string path)
		{
			try { return Path.GetFileNameWithoutExtension(path) ?? string.Empty; }
			catch { return string.Empty; }
		}

		/// <summary>Return the file extension of 'path' (or empty string). Includes the '.'</summary>
		public static string Extn(string path)
		{
			try { return Path.GetExtension(path) ?? string.Empty; }
			catch { return string.Empty; }
		}

		/// <summary>Returns the drive (i.e. root) of 'path' (or empty string)</summary>
		public static string Drive(string path)
		{
			try { return Path.GetPathRoot(path) ?? string.Empty; }
			catch { return string.Empty; }
		}

		/// <summary>Return a path consisting of the concatenation of path fragments</summary>
		public static string CombinePath(params string[] paths)
		{
			if (paths.Length == 0) return "";

			string path = paths[0];
			for (int i = 1; i != paths.Length; ++i)
			{
				if (string.IsNullOrEmpty(paths[i])) continue;
				path = Path.Combine(path, (paths[i][0] == '/' || paths[i][0] == '\\') ? paths[i].Substring(1) : paths[i]);
			}
			return Canonicalise(path);
		}

		/// <summary>Remove '..' or '.' directories from a path and swap all '/' to '\'</summary>
		public static string Canonicalise(string path, Case change_case = Case.Unchanged)
		{
			if (change_case == Case.Lower) path = path.ToLowerInvariant();
			if (change_case == Case.Upper) path = path.ToUpperInvariant();
			path = path.Replace('/', '\\');
			return Path.GetFullPath(path);
		}
		public enum Case { Unchanged, Lower, Upper };

		/// <summary>Compare two strings as filepaths</summary>
		public static int Compare(string lhs, string rhs)
		{
			if (!lhs.HasValue() && !rhs.HasValue()) return 0; // Both null/empty paths
			if (!lhs.HasValue()) return +1; // Rhs is non-null => greater than lhs
			if (!rhs.HasValue()) return -1; // Lhs is non-null => less than rhs

			return string.Compare(
				Path.GetFullPath(lhs).ToLowerInvariant().TrimEnd(Path.PathSeparator, Path.AltDirectorySeparatorChar),
				Path.GetFullPath(rhs).ToLowerInvariant().TrimEnd(Path.PathSeparator, Path.AltDirectorySeparatorChar),
				StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>Remove the invalid chars from a potential filename</summary>
		public static string SanitiseFileName(string name, string additional_invalid_chars, string replace_with)
		{
			string inv_chars   = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + additional_invalid_chars);
			string inv_reg_str = string.Format(@"[{0}]", inv_chars);
			return Regex.Replace(name, inv_reg_str, replace_with);
		}

		/// <summary>Remove the invalid chars from a potential filename</summary>
		public static string SanitiseFileName(string name)
		{
			return SanitiseFileName(name, "", "_");
		}

		/// <summary>Adds/Removes quotes to/from 'path' if necessary</summary>
		public static string Quote(string path, bool add)
		{
			if (string.IsNullOrEmpty(path)) return path;
			if (!add && (path.Length >= 2 &&  (path[0] == '"' && path[path.Length-1] == '"'))) path = path.Remove(path.Length-1,1).Remove(0,1);
			if ( add && (path.Length <= 1 || !(path[0] == '"' && path[path.Length-1] == '"'))) path = '"' + path + '"';
			return path;
		}

		/// <summary>Heuristic test to see if a file contains text data or binary data</summary>
		public static bool IsProbableTextFile(string path)
		{
			var buf = new byte[1024];
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buf.Length))
			{
				bool is_text = true;
				int n = fs.Read(buf, 0, buf.Length);
				for (int i = 0; i+1 < n && is_text; ++i)
					is_text &= buf[i] != 0 && buf[i+1] != 0;
				
				return is_text;
			}
		}

		/// <summary>Compare the contents of two files, returning true if their contents are different</summary>
		public static bool DiffContent(string lhs, string rhs, Action<string> trace = null)
		{
			// Missing files are considered different
			var sfound = FileExists(lhs);
			var dfound = FileExists(rhs);
			if (!sfound)
			{
				if (trace != null) trace("Content different, '{0}' not found".Fmt(lhs));
				return true;
			}
			if (!dfound)
			{
				if (trace != null) trace("Content different, '{0}' not found".Fmt(rhs));
				return true;
			}

			// Check for differing file lengths
			var infoL = new FileInfo(lhs);
			var infoR = new FileInfo(rhs);
			if (infoL.Length != infoR.Length)
			{
				if (trace != null) trace("Content different, '{0}' and '{1}' have different sizes".Fmt(lhs, rhs));
				return true;
			}

			// Compare contents
			var bufL = new byte[4096];
			var bufR = new byte[4096];
			using (var fileL = new FileStream(lhs, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var fileR = new FileStream(rhs, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				for (;;)
				{
					var readL = fileL.Read(bufL, 0, bufL.Length);
					var readR = fileR.Read(bufR, 0, bufR.Length);
					if (readL == 0 && readR == 0) break;
					if (readL != readR)
					{
						if (trace != null)
						{
							trace("Content different, '{0}' and '{1}' have different content".Fmt(lhs, rhs));
							for (var i = 0; i != Math.Min(readL, readR); ++i)
							{
								if (bufL[i] == bufR[i]) continue;
								trace("diff at byte {0}: {1} != {2}".Fmt(i, bufL[i], bufR[i]));
								break;
							}
						}
						return true;
					}
				}
			}
			if (trace != null) trace("'{0}' and '{1}' are identical".Fmt(lhs,rhs));
			return false;
		}

		/// <summary>
		/// Smart copy from 'src' to 'dst'. Loosely like xcopy.
		/// 'src' can be a single file, a comma separated list of files, or a directory<para/>
		/// 'dst' can be a 
		/// if 'src' is a directory, </summary>
		public static void Copy(string src, string dst, bool overwrite = false, bool only_if_modified = false, bool ignore_non_existing = false, Action<string> feedback = null, bool show_unchanged = false)
		{
			var src_is_dir = IsDirectory(src);
			var dst_is_dir = IsDirectory(dst) || dst.EndsWith("/") || dst.EndsWith("\\") || src_is_dir;

			// Find the names of the source files to copy
			string[] files = null;
			if (src_is_dir)
				files = EnumFileSystem(src, SearchOption.AllDirectories).Select(x => x.FullPath).ToArray();
			else if (FileExists(src))
				files = new [] { src };
			else if (src.Contains('*') || src.Contains('?'))
				files = EnumFileSystem(src, SearchOption.AllDirectories, new Pattern(EPattern.Wildcard, src).RegexString).Select(x => x.FullPath).ToArray();
			else if (!ignore_non_existing)
				throw new FileNotFoundException("ERROR: {0} does not exist".Fmt(src));

			// If the 'src' represents multiple files, 'dst' must be a directory
			if (src_is_dir || files.Length > 1)
			{
				// if 'dst' doesn't exist, assume it's a directory
				if (!DirExists(dst))
					dst_is_dir = true;

				// or if it does exist, check that it is actually a directory
				else if (!dst_is_dir)
					throw new FileNotFoundException("ERROR: {0} is not a valid directory".Fmt(dst));
			}

			// Ensure that 'dstdir' exists. (Canonicalise fixes the case where 'dst' is a drive, e.g. 'C:\')
			var dstdir = Canonicalise((dst_is_dir ? dst : Directory(dst)).TrimEnd('/','\\'));
			if (!DirExists(dstdir))
				System.IO.Directory.CreateDirectory(dstdir);

			// Copy the file(s) to 'dst'
			foreach (var srcfile in files)
			{
				// If 'dst' is a directory, use the same filename from 'srcfile'
				var dstfile = string.Empty;
				if (dst_is_dir)
				{
					var spath = src_is_dir ? RelativePath(srcfile, src) : FileName(srcfile);
					dstfile = CombinePath(dstdir, spath);
				}
				else
				{
					dstfile = dst;
				}

				// If 'srcfile' is a directory, ensure the directory exists at the destination
				if (IsDirectory(srcfile))
				{
					if (!dst_is_dir)
						throw new Exception("ERROR: {0} is not a directory".Fmt(dst));

					// Create the directory at the destination
					if (!DirExists(dstfile)) System.IO.Directory.CreateDirectory(dstfile);
					if (feedback != null) feedback(srcfile + " --> " + dstfile);
				}
				else
				{
					// Copy if modified or always based on the flag
					if (only_if_modified && !DiffContent(srcfile, dstfile))
					{
						if (feedback != null && show_unchanged) feedback(srcfile + " --> unchanged");
						continue;
					}

					// Ensure the directory path exists
					var d = Directory(dstfile);
					var f = FileName(dstfile);
					if (!DirExists(d)) System.IO.Directory.CreateDirectory(d);
					if (feedback != null) feedback(srcfile + " --> " + dstfile);
					File.Copy(srcfile, dstfile, overwrite);
				}
			}
		}

		/// <summary>Contains information about a file</summary>
		[Serializable] public class FileData
		{
			private readonly Win32.WIN32_FIND_DATA m_find_data;

			public FileData(string dir)
			{
				m_find_data = new Win32.WIN32_FIND_DATA();
				var handle = FindFirstFile(dir, ref m_find_data);
				if (handle.IsInvalid) throw new FileNotFoundException("Failed to get WIN32_FIND_Data for {0}".Fmt(dir));
				handle.Close();
				FullPath = dir;
			}
			public FileData(string dir, ref Win32.WIN32_FIND_DATA find_data)
			{
				m_find_data = find_data;
				FullPath = Path.Combine(dir, FileName);
			}
			public FileData(FileData rhs)
			{
				m_find_data = rhs.m_find_data.ShallowCopy();
				FullPath    = rhs.FullPath;
			}

			/// <summary>Full path to the file.</summary>
			public string FullPath { get; private set; }

			/// <summary>The filename.extn of the file</summary>
			public string FileName { get { return m_find_data.FileName; } }

			/// <summary>Attributes of the file.</summary>
			public FileAttributes Attributes { get { return m_find_data.Attributes; } }
			public bool IsDirectory { get { return (Attributes & FileAttributes.Directory) == FileAttributes.Directory; } }

			/// <summary>The file size</summary>
			public long FileSize { get { return m_find_data.FileSize; } }

			/// <summary>File creation time in local time</summary>
			public DateTime CreationTime { get { return m_find_data.CreationTime; } }

			/// <summary>File creation time in UTC</summary>
			public DateTime CreationTimeUtc { get { return m_find_data.CreationTimeUtc; } }

			/// <summary>Gets the last access time in local time.</summary>
			public DateTime LastAccesTime { get { return m_find_data.LastAccessTime; } }

			/// <summary>File last access time in UTC</summary>
			public DateTime LastAccessTimeUtc { get { return m_find_data.LastAccessTimeUtc; } }

			/// <summary>Gets the last access time in local time.</summary>
			public DateTime LastWriteTime { get { return m_find_data.LastWriteTime; } }

			/// <summary>File last write time in UTC</summary>
			public DateTime LastWriteTimeUtc { get { return  m_find_data.LastWriteTimeUtc; } }

			public override string ToString() { return FullPath; }
		}

		/// <remarks>
		/// A fast enumerator of files in a directory.
		/// Use this if you need to get attributes for all files in a directory.
		/// This enumerator is substantially faster than using <see cref="Directory.GetFiles(string)"/>
		/// and then creating a new FileInfo object for each path.  Use this version when you
		/// will need to look at the attributes of each file returned (for example, you need
		/// to check each file in a directory to see if it was modified after a specific date).
		/// </remarks>

		/// <summary>
		/// Gets FileData for all files/directories in a directory that match a specific filter including all sub directories.
		/// 'regex_filter' is a filter on the filename, not the full path</summary>
		[SuppressUnmanagedCodeSecurity]
		public static IEnumerable<FileData> EnumFileSystem(string path, SearchOption search_flags = SearchOption.TopDirectoryOnly, string regex_filter = null, RegexOptions regex_options = RegexOptions.IgnoreCase, FileAttributes exclude = FileAttributes.Hidden, Func<string,bool> progress = null)
		{
			Debug.Assert(DirExists(path), "Attempting to enumerate an invalid directory path");

			// Default progress callback
			if (progress == null)
				progress = s => true;

			// File/Directory name filter
			var filter = regex_filter != null ? new Regex(regex_filter, regex_options) : null;

			// For drive letters, add a \, 'FileIOPermission' needs it
			if (path.EndsWith(":"))
				path += "\\";

			// Local stack for recursion
			var stack = new Stack<string>(20);
			for (stack.Push(path); stack.Count != 0;)
			{
				// Report progress
				var dir = stack.Pop();
				if (!progress(dir))
					break;

				// Skip paths we don't have access to
				try { new FileIOPermission(FileIOPermissionAccess.PathDiscovery, dir).Demand(); }
				catch { continue; }

				// Use the win32 find files
				var pattern = Path.Combine(dir, "*");
				var find_data = new Win32.WIN32_FIND_DATA();
				var handle = FindFirstFile(pattern, ref find_data);
				for (var more = !handle.IsInvalid; more; more = FindNextFile(handle, ref find_data))
				{
					// Exclude files with any of the exclude attributes
					if ((find_data.Attributes & exclude) != 0)
						continue;

					// Filter if provided
					if (find_data.FileName != "." && find_data.FileName != "..")
						if (filter == null || filter.IsMatch(find_data.FileName))
							yield return new FileData(dir, ref find_data);

					// If the found object is a directory, see if we should be recursing
					if (search_flags == SearchOption.AllDirectories && (find_data.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
					{
						if (find_data.FileName == "." || find_data.FileName == "..")
							continue;

						stack.Push(Path.Combine(dir, find_data.FileName));
					}
				}
				handle.Close();
			}
		}

		/// <summary>Wraps a FindFirstFile handle.</summary>
		private sealed class SafeFindHandle :SafeHandleZeroOrMinusOneIsInvalid
		{
			[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
			public SafeFindHandle() :base(true) {}
			protected override bool ReleaseHandle() { return FindClose(handle); }
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern SafeFindHandle FindFirstFile(string fileName, ref Win32.WIN32_FIND_DATA data);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool FindNextFile(SafeFindHandle hndFindFile, ref Win32.WIN32_FIND_DATA lpFindFileData);

		[DllImport("kernel32.dll")][ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private static extern bool FindClose(IntPtr handle);

		/// <summary>Returns the process(es) that have a lock on the specified files.</summary>
		public static IEnumerable<Process> FileLockHolders(string[] filepaths)
		{
			// See also:
			// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
			// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)

			// Begin a session. Only 64 of these can exist at any one time
			uint handle;
			int res = Win32.RmStartSession(out handle, 0, Guid.NewGuid().ToString());
			if (res != 0)
				throw new Exception("Could not begin restart session. Unable to determine file locker.\nError Code {0}".Fmt(res));

			// Create an array to store the retrieved processes in (have an initial guess at the required size)
			var process_info = new Win32.RM_PROCESS_INFO[16];
			var process_count = (uint)process_info.Length;

			// Ensure cleanup of session handle
			using (Scope.Create(null, () => Win32.RmEndSession(handle)))
			{
				// Register the filepaths we're interested in
				res = Win32.RmRegisterResources(handle, (uint)filepaths.Length, filepaths, 0, null, 0, null);
				if (res != 0)
					throw new Exception("Could not register resource.\nError Code {0}".Fmt(res));

				// Get the list of processes/services holding locks on 'filepaths'
				for (;;)
				{
					uint size_needed, reboot_reasons = Win32.RmRebootReasonNone;
					res = Win32.RmGetList(handle, out size_needed, ref process_count, process_info, ref reboot_reasons);
					if (res == 0) break;
					if (res != Win32.ERROR_MORE_DATA) throw new Exception("Failed to retrieve list of processes holding file locks\r\nError Code: {0}".Fmt(Win32.ErrorCodeToString(res)));
					process_info = new Win32.RM_PROCESS_INFO[size_needed];
					process_count = (uint)process_info.Length;
				}
			}

			// Enumerate the results
			for (int i = 0; i != process_count; ++i)
			{
				Process proc = null;
				try { proc = Process.GetProcessById(process_info[i].Process.dwProcessId); }
				catch (ArgumentException) { continue; } // The process might have ended
				yield return proc;
			}
		}

		/// <summary>Delete a directory and all contained files/subdirectories. Returns true if successful</summary>
		public static bool DelTree(string root, EDelTreeOpts opts, Control parent)
		{
			for (;;)
			{
				try
				{
					// Generate a list of all contained files
					var files = System.IO.Directory.GetFiles(root, "*", SearchOption.AllDirectories);
					if (files.Length != 0)
					{
						if (!opts.HasFlag(EDelTreeOpts.FilesOnly) && !opts.HasFlag(EDelTreeOpts.EvenIfNotEmpty))
							throw new IOException("Cannot delete {0}, directory still contains {1} files".Fmt(root, files.Length));

						// Check for processes holding locks on the files
						for (;;)
						{
							// Find the lock holding processes/services
							var lockers = FileLockHolders(files);
							if (!lockers.Any()) break;

							// Prompt the user, Abort, Retry or Ignore
							var msg = "The following processes hold locks on files within {0}:\r\n\t{1}".Fmt(root, string.Join("\r\n\t", lockers.Select(x => x.ProcessName)));
							var r = MsgBox.Show(parent, msg, "Locked Files Detected", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Information);
							if (r == DialogResult.Abort || r == DialogResult.Cancel)
								return false;
							if (r == DialogResult.Ignore)
								break;
						}

						// Delete the contained files
						if (opts.HasFlag(EDelTreeOpts.FilesOnly) || opts.HasFlag(EDelTreeOpts.EvenIfNotEmpty))
						{
							foreach (var file in files)
							{
								if (FileExists(file))
									File.Delete(file);
							}
						}
					}

					// Try to delete the root directory. This can fail because the file system
					// doesn't necessarily update as soon as the files are deleted. Try to delete,
					// if that fails, wait a bit, then try again. If that fails, defer to the user.
					for (int retries = 3; retries-- != 0;)
					{
						try
						{
							if (opts.HasFlag(EDelTreeOpts.DeleteRoot))
							{
								System.IO.Directory.Delete(root, true);
							}
							else
							{
								// Delete the contained directories
								var dirs = System.IO.Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly);
								foreach (var dir in dirs)
								{
									if (DirExists(dir))
										System.IO.Directory.Delete(dir, true);
								}
							}
							return true;
						}
						catch (IOException)
						{
							if (retries == 0) throw;
							Thread.Sleep(500);
						}
					}
				}
				catch (Exception ex)
				{
					var msg = "Failed to delete directory '{0}'\r\n{1}\r\n".Fmt(root, ex.Message);
					var res = MsgBox.Show(parent, msg, "Deleting Directory", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);
					if (res == DialogResult.Abort)
						return false;
					if (res == DialogResult.Ignore)
						return true;
				}
			}
		}
		[Flags] public enum EDelTreeOpts
		{
			None = 0,

			/// <summary>Delete the 'root' folder if set, otherwise just delete the contents of the root folder</summary>
			DeleteRoot = 1 << 0,

			/// <summary>Perform the delete even if there are files/directories in the root folder, otherwise throw if not empty</summary>
			EvenIfNotEmpty = 1 << 1,

			/// <summary>Delete only the files from the directory tree</summary>
			FilesOnly = 1 << 2,
		}

		/// <summary>Flags for shell file operations</summary>
		[Flags] public enum EFileOpFlags
		{
			None = 0,

			/// <summary>Don't display progress UI (confirm prompts may be displayed still)</summary>
			Silent = Win32.FOF_SILENT,

			/// <summary>Automatically rename the source files to avoid the collisions</summary>
			RenameOnCollision = Win32.FOF_RENAMEONCOLLISION,

			/// <summary>Don't display confirmation UI, assume "yes" for cases that can be bypassed, "no" for those that can not</summary>
			NoConfirmation = Win32.FOF_NOCONFIRMATION,

			/// <summary>Enable undo including Recycle behaviour for IFileOperation::Delete()</summary>
			AllowUndo = Win32.FOF_ALLOWUNDO,

			/// <summary>Only operate on the files (non folders), both files and folders are assumed without this</summary>
			FilesOnly = Win32.FOF_FILESONLY,

			/// <summary>Means don't show names of files</summary>
			SimpleProgress = Win32.FOF_SIMPLEPROGRESS,

			/// <summary>Don't display confirmation UI before making any needed directories, assume "Yes" in these cases</summary>
			NoConfirmMakeDir = Win32.FOF_NOCONFIRMMKDIR,

			/// <summary>Don't put up error UI, other UI may be displayed, progress, confirmations</summary>
			NoErrorUI = Win32.FOF_NOERRORUI,

			/// <summary>Don't copy file security attributes (ACLs)</summary>
			NoCopySecurityAttribs = Win32.FOF_NOCOPYSECURITYATTRIBS,

			/// <summary>Don't recurse into directories for operations that would recurse</summary>
			NoRecursion = Win32.FOF_NORECURSION,

			/// <summary>Don't operate on connected elements ("xxx_files" folders that go with .htm files)</summary>
			NoConnectedElements = Win32.FOF_NO_CONNECTED_ELEMENTS,

			/// <summary>During delete operation, warn if object is being permanently destroyed instead of recycling (partially overrides FOF_NOCONFIRMATION)</summary>
			WantNukeWarning = Win32.FOF_WANTNUKEWARNING,

			/// <summary>Don't display any UI at all</summary>
			NoUI = Win32.FOF_NO_UI,
		}

		/// <summary>Copy a file using a Shell file operation</summary>
		public static bool ShellCopy(string src, string dst, EFileOpFlags flags = EFileOpFlags.None, string title = "Copying Files...")
		{
			var shf = new Win32.SHFILEOPSTRUCT(); 
			shf.wFunc = Win32.FO_COPY;
			shf.fFlags = unchecked((short)flags);
			shf.pFrom = src + "\0\0";// ensure double null termination
			shf.pTo = dst + "\0\0";// ensure double null termination
			shf.lpszProgressTitle = title;
			Win32.SHFileOperation(ref shf);
			return !shf.fAnyOperationsAborted;
		}

		/// <summary>Move a file using a Shell file operation</summary>
		public static bool ShellMove(string src, string dst, EFileOpFlags flags = EFileOpFlags.None, string title = "Moving Files...")
		{
			var shf = new Win32.SHFILEOPSTRUCT(); 
			shf.wFunc = Win32.FO_MOVE;
			shf.fFlags = unchecked((short)flags);
			shf.pFrom = src + "\0\0";// ensure double null termination
			shf.pTo = dst + "\0\0";// ensure double null termination
			shf.lpszProgressTitle = title;
			Win32.SHFileOperation(ref shf);
			return !shf.fAnyOperationsAborted;
		}

		/// <summary>Delete a file to the recycle bin. 'flags' should be Win32.FOF_??? flags</summary>
		public static bool ShellDelete(string filepath, EFileOpFlags flags = EFileOpFlags.AllowUndo, string title = "Deleting Files...")
		{
			var shf = new Win32.SHFILEOPSTRUCT(); 
			shf.wFunc = Win32.FO_DELETE;
			shf.fFlags = unchecked((short)flags);
			shf.pFrom = filepath + "\0\0";// ensure double null termination
			shf.lpszProgressTitle = title;
			Win32.SHFileOperation(ref shf);
			return !shf.fAnyOperationsAborted;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Linq;
	using common;

	[TestFixture] public class TestPathEx
	{
		[Test] public void TestPathValidation()
		{
			Assert.True(Path_.IsValidDirectory(@"A:\dir1\..\.\dir2", true));
			Assert.True(Path_.IsValidDirectory(@"A:\dir1\..\.\dir2", false));
			Assert.True(Path_.IsValidDirectory(@".\dir1\..\.\dir2", false));
			Assert.True(Path_.IsValidDirectory(@"A:\dir1\..\.\dir2\", true));
			Assert.True(Path_.IsValidDirectory(@"A:\dir1\..\.\dir2\", false));
			Assert.True(Path_.IsValidDirectory(@".\dir1\..\.\dir2\", false));
			Assert.False(Path_.IsValidDirectory(@".\dir1?\..\.\", false));

			Assert.True(Path_.IsValidFilepath(@"A:\dir1\..\.\dir2\file.txt", true));
			Assert.True(Path_.IsValidFilepath(@"A:\dir1\..\.\dir2\file", false));
			Assert.True(Path_.IsValidFilepath(@".\dir1\..\.\dir2\file", false));
			Assert.False(Path_.IsValidFilepath(@".\dir1\", false));
			Assert.False(Path_.IsValidFilepath(@".\dir1\file*.txt", false));
		}
		[Test] public void TestPathNames()
		{
			string path;

			path = Path_.RelativePath(@"A:\dir\subdir\file.ext", @"A:\dir");
			Assert.AreEqual(@".\subdir\file.ext", path);

			path = Path_.CombinePath(@"A:\", @".\dir\subdir2", @"..\subdir\", "file.ext");
			Assert.AreEqual(@"A:\dir\subdir\file.ext", path);

			path = Path_.SanitiseFileName("1_path@\"[{+\\!@#$%^^&*()\'/?", "@#$%", "A");
			Assert.AreEqual("1_pathAA[{+A!AAAA^^&A()'AA", path);

			const string noquotes   = "C:\\a b\\path.ext";
			const string withquotes = "\"C:\\a b\\path.ext\"";
			Assert.AreEqual(withquotes ,Path_.Quote(noquotes, true));
			Assert.AreEqual(withquotes ,Path_.Quote(withquotes, true));
			Assert.AreEqual(noquotes   ,Path_.Quote(noquotes, false));
			Assert.AreEqual(noquotes   ,Path_.Quote(withquotes, false));
		}
		[Test] public void TestEnumerateFiles()
		{
			var files = Path_.EnumFileSystem(@"C:\Windows\System32", SearchOption.TopDirectoryOnly, @".*\.dll");
			var dlls = files.ToList();
			Assert.True(dlls.Count != 0);
		}
	}
}
#endif
