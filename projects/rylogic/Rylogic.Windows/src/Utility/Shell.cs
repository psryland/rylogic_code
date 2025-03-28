﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Rylogic.Common;
using Rylogic.Interop.Win32;
using Rylogic.Utility;

namespace Rylogic.Windows
{
	public static class Shell
	{
		#region Enumerations
		/// <summary></summary>
		public enum EAbortIgnoreRetry
		{
			Abort  = 3, // = DialogResult.Abort
			Retry  = 4, // = DialogResult.Retry
			Ignore = 5, // = DialogResult.Ignore
		}

		/// <summary></summary>
		[Flags]
		public enum EDelTreeOpts
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
		[Flags]
		public enum EFileOpFlags
		{
			None = 0,

			/// <summary>Don't display progress UI (confirm prompts may be displayed still)</summary>
			Silent = Win32.FOF_SILENT,

			/// <summary>Copy/Move multiple files to multiple locations</summary>
			MultipleDstFiles = Win32.FOF_MULTIDESTFILES,

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
		#endregion

		/// <summary>Returns the processes that have a lock on the specified files.</summary>
		public static IEnumerable<Process> FileLockHolders(string[] filepaths)
		{
			// See also:
			// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
			// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)

			// Begin a session. Only 64 of these can exist at any one time
			int res = Win32.RmStartSession(out var handle, 0, Guid.NewGuid().ToString());
			if (res != 0)
				throw new Exception($"Could not begin restart session. Unable to determine file locker.\nError Code {res}");

			// Create an array to store the retrieved processes in (have an initial guess at the required size)
			var process_info = new Win32.RM_PROCESS_INFO[16];
			var process_count = (uint)process_info.Length;

			// Ensure clean up of session handle
			using (Scope.Create(null, () => Win32.RmEndSession(handle)))
			{
				// Register the file paths we're interested in
				res = Win32.RmRegisterResources(handle, (uint)filepaths.Length, filepaths, 0, null, 0, null);
				if (res != 0)
					throw new Exception($"Could not register resource.\nError Code {res}");

				// Get the list of processes/services holding locks on 'filepaths'
				for (;;)
				{
					uint size_needed, reboot_reasons = Win32.RmRebootReasonNone;
					res = Win32.RmGetList(handle, out size_needed, ref process_count, process_info, ref reboot_reasons);
					if (res == 0) break;
					if (res != Win32.ERROR_MORE_DATA) throw new Exception($"Failed to retrieve list of processes holding file locks\r\nError Code: {Win32.ErrorCodeToString(res)}");
					process_info = new Win32.RM_PROCESS_INFO[size_needed];
					process_count = (uint)process_info.Length;
				}
			}

			// Enumerate the results
			for (int i = 0; i != process_count; ++i)
			{
				Process proc;
				try { proc = Process.GetProcessById(process_info[i].Process.dwProcessId); }
				catch (ArgumentException) { continue; } // The process might have ended
				yield return proc;
			}
		}

		/// <summary>
		/// Smart copy from 'src' to 'dst'. Loosely like XCopy.
		/// 'src' can be a single file, a comma separated list of files, or a directory<para/>
		/// 'dst' can be a 
		/// if 'src' is a directory, </summary>
		public static void Copy(string src, string dst, bool overwrite = false, bool only_if_modified = false, bool ignore_non_existing = false, Action<string>? feedback = null, bool show_unchanged = false)
		{
			var src_is_dir = Path_.IsDirectory(src);
			var dst_is_dir = Path_.IsDirectory(dst) || dst.EndsWith("/") || dst.EndsWith("\\") || src_is_dir;

			// Find the names of the source files to copy
			var files = new List<string>();
			if (src_is_dir)
			{
				files = Path_.EnumFileSystem(src, SearchOption.AllDirectories).OfType<FileInfo>().Select(x => x.FullName).ToList();
			}
			else if (Path_.FileExists(src))
			{
				files = new List<string>() { src };
			}
			else if (src.Contains('*') || src.Contains('?'))
			{
				var filter = new Pattern(EPattern.Wildcard, src).Regex;
				files = Path_.EnumFileSystem(src, SearchOption.AllDirectories, file_filter: filter, dir_filter: filter).OfType<FileInfo>().Select(x => x.FullName).ToList();
			}
			else if (!ignore_non_existing)
			{
				throw new FileNotFoundException($"'{src}' does not exist");
			}

			// If the 'src' represents multiple files, 'dst' must be a directory
			if (src_is_dir || files.Count > 1)
			{
				// if 'dst' doesn't exist, assume it's a directory
				if (!Path_.DirExists(dst))
					dst_is_dir = true;

				// or if it does exist, check that it is actually a directory
				else if (!dst_is_dir)
					throw new FileNotFoundException($"'{dst}' is not a valid directory");
			}

			// Ensure that 'dstdir' exists. (Canonicalise fixes the case where 'dst' is a drive, e.g. 'C:\')
			var dstdir = Path_.Canonicalise((dst_is_dir ? dst : Path_.Directory(dst)).TrimEnd('/','\\'));
			if (!Path_.DirExists(dstdir))
				Directory.CreateDirectory(dstdir);

			// Copy the file(s) to 'dst'
			foreach (var srcfile in files)
			{
				// If 'dst' is a directory, use the same filename from 'srcfile'
				var dstfile = string.Empty;
				if (dst_is_dir)
				{
					var spath = src_is_dir ? Path_.RelativePath(src, srcfile) : Path_.FileName(srcfile);
					dstfile = Path_.CombinePath(dstdir, spath);
				}
				else
				{
					dstfile = dst;
				}

				// If 'srcfile' is a directory, ensure the directory exists at the destination
				if (Path_.IsDirectory(srcfile))
				{
					if (!dst_is_dir)
						throw new Exception($"ERROR: {dst} is not a directory");

					// Create the directory at the destination
					if (!Path_.DirExists(dstfile)) System.IO.Directory.CreateDirectory(dstfile);
					if (feedback != null) feedback(srcfile + " --> " + dstfile);
				}
				else
				{
					// Copy if modified or always based on the flag
					if (only_if_modified && !Path_.DiffContent(srcfile, dstfile))
					{
						if (feedback != null && show_unchanged) feedback(srcfile + " --> unchanged");
						continue;
					}

					// Ensure the directory path exists
					var d = Path_.Directory(dstfile);
					var f = Path_.FileName(dstfile);
					if (!Path_.DirExists(d)) System.IO.Directory.CreateDirectory(d);
					if (feedback != null) feedback(srcfile + " --> " + dstfile);
					File.Copy(srcfile, dstfile, overwrite);
				}
			}
		}

		/// <summary>
		/// Delete a directory and all contained files/subdirectories.
		/// Returns true if successful and not interrupted</summary>
		public static bool DelTree(string root, EDelTreeOpts opts, Func<string, EAbortIgnoreRetry> prompt, int timeout_ms = 1000)
		{
			prompt = prompt ?? (_ => EAbortIgnoreRetry.Abort);
			for (;;)
			{
				try
				{
					// Generate a list of all contained files
					var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
					if (files.Length != 0)
					{
						if (!opts.HasFlag(EDelTreeOpts.FilesOnly) && !opts.HasFlag(EDelTreeOpts.EvenIfNotEmpty))
							throw new IOException($"Cannot delete {root}, directory still contains {files.Length} files");

						// Check for processes holding locks on the files
						for (;;)
						{
							// Find the lock holding processes/services
							var lockers = FileLockHolders(files);
							if (!lockers.Any()) break;

							// Prompt the user, Abort, Retry or Ignore
							var r = prompt(
								$"The following processes hold locks on files within {root}:\r\n" +
								$"\t{string.Join("\r\n\t", lockers.Select(x => x.ProcessName))}");
							if (r == EAbortIgnoreRetry.Abort) return false;
							if (r == EAbortIgnoreRetry.Ignore) break;
						}

						// Delete the contained files
						if (opts.HasFlag(EDelTreeOpts.FilesOnly) || opts.HasFlag(EDelTreeOpts.EvenIfNotEmpty))
						{
							foreach (var file in files)
							{
								if (Path_.FileExists(file))
									File.Delete(file);
							}
						}
					}

					// Try to delete the root directory. This can fail because the file system
					// doesn't necessarily update as soon as the files are deleted. Try to delete,
					// if that fails, wait a bit, then try again. If that fails, defer to the user.
					const int Attempts = 3;
					for (int retries = Attempts; retries-- != 0;)
					{
						try
						{
							if (opts.HasFlag(EDelTreeOpts.DeleteRoot))
							{
								Directory.Delete(root, true);
							}
							else
							{
								// Delete the contained directories
								var dirs = System.IO.Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly);
								foreach (var dir in dirs)
								{
									if (Path_.DirExists(dir))
										Directory.Delete(dir, true);
								}
							}
							return true;
						}
						catch (IOException)
						{
							if (retries == 0) throw;
							Thread.Sleep(Math.Max(100, timeout_ms / Attempts));
						}
					}
				}
				catch (Exception ex)
				{
					var r = prompt(
						$"Failed to delete directory '{root}'\r\n" +
						$"{ex.Message}\r\n");
					if (r == EAbortIgnoreRetry.Abort) return false;
					if (r == EAbortIgnoreRetry.Ignore) return true;
				}
			}
		}

		/// <summary>Copy a file using a Shell file operation</summary>
		public static bool ShellCopy(IntPtr hwnd, string src, string dst, EFileOpFlags flags = EFileOpFlags.None, string title = "Copying Files...")
		{
			return ShellCopy(hwnd, new[] { src }, dst, flags, title);
		}
		public static bool ShellCopy(IntPtr hwnd, IEnumerable<string> src, string dst, EFileOpFlags flags = EFileOpFlags.None, string title = "Copying Files...")
		{
			return ShellCopy(hwnd, src, new[] { dst }, flags, title);
		}
		public static bool ShellCopy(IntPtr hwnd, IEnumerable<string> src, IEnumerable<string> dst, EFileOpFlags flags = EFileOpFlags.None, string title = "Copying Files...")
		{
			// Get the list of files as an array
			var src_list = src.ToArray();
			var dst_list = dst.ToArray();

			// Sanity check
			if ((src_list.Length == 0 && dst_list.Length != 0) ||
				(src_list.Length != 0 && dst_list.Length == 0) ||
				(src_list.Length != 0 && dst_list.Length != 1 && dst_list.Length != src_list.Length))
				throw new Exception("Illogical combination of source and destination file paths");

			// Add the multiple files flag
			if (dst_list.Length > 1)
				flags |= EFileOpFlags.MultipleDstFiles;

			// Convert the file paths to a byte buffer so that we can ensure double null termination.
			// Interop as a string causes the double null not to be copied
			var srcs = Encoding.Unicode.GetBytes(string.Join("\0",src_list) + "\0\0");// ensure double null termination
			var dsts = Encoding.Unicode.GetBytes(string.Join("\0",dst_list) + "\0\0");// ensure double null termination
			using (var spin = new PinnedObject<byte[]>(srcs, GCHandleType.Pinned))
			using (var dpin = new PinnedObject<byte[]>(dsts, GCHandleType.Pinned))
			{
				if (Environment.Is64BitProcess)
				{
					var shf = new Win32.SHFILEOPSTRUCTW64
					{
						hwnd = hwnd,
						wFunc = Win32.FO_COPY,
						pFrom = spin.Pointer,
						pTo = dpin.Pointer,
						fFlags = unchecked((ushort)flags),
						lpszProgressTitle = title
					};
					Shell32.SHFileOperationW(ref shf);
					return shf.fAnyOperationsAborted == 0;
				}
				else
				{
					var shf = new Win32.SHFILEOPSTRUCTW32
					{
						hwnd = hwnd,
						wFunc = Win32.FO_COPY,
						pFrom = spin.Pointer,
						pTo = dpin.Pointer,
						fFlags = unchecked((ushort)flags),
						lpszProgressTitle = title
					};
					Shell32.SHFileOperationW(ref shf);
					return shf.fAnyOperationsAborted == 0;
				}
			}
		}

		/// <summary>Move a file using a Shell file operation</summary>
		public static bool ShellMove(IntPtr hwnd, string src, string dst, EFileOpFlags flags = EFileOpFlags.None, string title = "Moving Files...")
		{
			return ShellMove(hwnd, new[] { src }, dst, flags, title);
		}
		public static bool ShellMove(IntPtr hwnd, IEnumerable<string> src, string dst, EFileOpFlags flags = EFileOpFlags.None, string title = "Moving Files...")
		{
			return ShellMove(hwnd, src, new[] { dst }, flags, title);
		}
		public static bool ShellMove(IntPtr hwnd, IEnumerable<string> src, IEnumerable<string> dst, EFileOpFlags flags = EFileOpFlags.None, string title = "Moving Files...")
		{
			// Get the list of files as an array
			var src_list = src.ToArray();
			var dst_list = dst.ToArray();

			// Sanity check
			if ((src_list.Length == 0 && dst_list.Length != 0) ||
				(src_list.Length != 0 && dst_list.Length == 0) ||
				(src_list.Length != 0 && dst_list.Length != 1 && dst_list.Length != src_list.Length))
				throw new Exception("Illogical combination of source and destination file paths");

			// Add the multiple files flag
			if (dst_list.Length > 1)
				flags |= EFileOpFlags.MultipleDstFiles;

			// Convert the file paths to a byte buffer so that we can ensure double null termination.
			// Interop as a string causes the double null not to be copied
			var srcs = Encoding.Unicode.GetBytes(string.Join("\0",src_list) + "\0\0");// ensure double null termination
			var dsts = Encoding.Unicode.GetBytes(string.Join("\0",dst_list) + "\0\0");// ensure double null termination
			using var spin = new PinnedObject<byte[]>(srcs, GCHandleType.Pinned);
			using var dpin = new PinnedObject<byte[]>(dsts, GCHandleType.Pinned);

			if (Environment.Is64BitProcess)
			{
				var shf = new Win32.SHFILEOPSTRUCTW64
				{
					hwnd = hwnd,
					wFunc = Win32.FO_MOVE,
					fFlags = unchecked((ushort)flags),
					pFrom = spin.Pointer,
					pTo = dpin.Pointer,
					lpszProgressTitle = title
				};
				Shell32.SHFileOperationW(ref shf);
				return shf.fAnyOperationsAborted == 0;
			}
			else
			{
				var shf = new Win32.SHFILEOPSTRUCTW32
				{
					hwnd = hwnd,
					wFunc = Win32.FO_MOVE,
					fFlags = unchecked((ushort)flags),
					pFrom = spin.Pointer,
					pTo = dpin.Pointer,
					lpszProgressTitle = title
				};
				Shell32.SHFileOperationW(ref shf);
				return shf.fAnyOperationsAborted == 0;
			}
		}

		/// <summary>Delete a file to the recycle bin. 'flags' should be Win32.FOF_??? flags</summary>
		public static bool ShellDelete(IntPtr hwnd, string filepath, EFileOpFlags flags = EFileOpFlags.AllowUndo, string title = "Deleting Files...")
		{
			// Convert the filepath to a byte buffer so that we can ensure double null termination.
			// Interop as a string causes the double null not to be copied
			var srcs = Encoding.Unicode.GetBytes(filepath + "\0\0");
			using (var spin = new PinnedObject<byte[]>(srcs, GCHandleType.Pinned))
			{
				if (Environment.Is64BitProcess)
				{
					var shf = new Win32.SHFILEOPSTRUCTW64
					{
						hwnd = hwnd,
						wFunc = Win32.FO_DELETE,
						fFlags = unchecked((ushort)flags),
						pFrom = spin.Pointer,
						lpszProgressTitle = title
					};
					Shell32.SHFileOperationW(ref shf);
					return shf.fAnyOperationsAborted == 0;
				}
				else
				{
					var shf = new Win32.SHFILEOPSTRUCTW32
					{
						hwnd = hwnd,
						wFunc = Win32.FO_DELETE,
						fFlags = unchecked((ushort)flags),
						pFrom = spin.Pointer,
						lpszProgressTitle = title
					};
					Shell32.SHFileOperationW(ref shf);
					return shf.fAnyOperationsAborted == 0;
				}
			}
		}
#if false
		/// <summary>
		/// Gets FileData for all files/directories in a directory that match a specific filter including all sub directories.
		/// 'regex_filter' is a filter on the filename, not the full path</summary>
		[SuppressUnmanagedCodeSecurity]
		[Obsolete("Use the Core version")]
		public static IEnumerable<FileData> EnumFileSystem(string path, SearchOption search_flags = SearchOption.TopDirectoryOnly, string? regex_filter = null, RegexOptions regex_options = RegexOptions.IgnoreCase, FileAttributes exclude = FileAttributes.Hidden, Func<string, bool>? progress = null)
		{
			/// <remarks>
			/// A fast enumerator of files in a directory.
			/// Use this if you need to get attributes for all files in a directory.
			/// This enumerator is substantially faster than using <see cref="Directory.GetFiles(string)"/>
			/// and then creating a new FileInfo object for each path.  Use this version when you
			/// will need to look at the attributes of each file returned (for example, you need
			/// to check each file in a directory to see if it was modified after a specific date).
			/// </remarks>

			Debug.Assert(Path_.DirExists(path), "Attempting to enumerate an invalid directory path");

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
				using (var handle = FindFirstFile(pattern, ref find_data))
				{
					for (var more = !handle.IsInvalid; more; more = FindNextFile(handle, ref find_data))
					{
						// Exclude files with any of the exclude attributes
						if ((find_data.Attributes & exclude) != 0)
							continue;

						// Filter if provided
						if (find_data.FileName != "." && find_data.FileName != "..")
						{
							Match m;
							if (filter == null)
								yield return new FileData(dir, ref find_data);
							else if ((m = filter.Match(find_data.FileName)).Success)
								yield return new FileData(dir, ref find_data, m);
						}

						// If the found object is a directory, see if we should be recursing
						if (search_flags == SearchOption.AllDirectories && (find_data.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
						{
							if (find_data.FileName == "." || find_data.FileName == "..")
								continue;

							stack.Push(Path.Combine(dir, find_data.FileName));
						}
					}
				}
			}
		}

		/// <summary>Contains information about a file</summary>
		[Serializable]
		[Obsolete("Use 'FileSystemInfo' instead")]
		public class FileData
		{
			private readonly Win32.WIN32_FIND_DATA m_find_data;

			public FileData(string dir)
			{
				m_find_data = new Win32.WIN32_FIND_DATA();
				using (var handle = FindFirstFile(dir, ref m_find_data))
					if (handle.IsInvalid) throw new FileNotFoundException($"Failed to get WIN32_FIND_Data for {dir}");
				FullPath = dir;
			}
			public FileData(string dir, ref Win32.WIN32_FIND_DATA find_data, Match? regex_match = null)
			{
				m_find_data = find_data;
				FullPath = Path.Combine(dir, FileName);
				RegexMatch = regex_match;
			}
			public FileData(FileData rhs)
			{
				m_find_data = rhs.m_find_data;
				FullPath = rhs.FullPath;
			}

			/// <summary>Full path to the file.</summary>
			public string FullPath { get; private set; }

			/// <summary>The result of the regular expression filter match (if used)</summary>
			public Match? RegexMatch { get; private set; }

			/// <summary>The filename.extn of the file</summary>
			public string FileName { get { return m_find_data.FileName; } }

			/// <summary>Return the file title (without the extension)</summary>
			public string FileTitle { get { return Path_.FileTitle(FullPath); } }

			/// <summary>Attributes of the file.</summary>
			public FileAttributes Attributes { get { return m_find_data.Attributes; } }
			public bool IsDirectory => Attributes.HasFlag(FileAttributes.Directory);

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
			public DateTime LastWriteTimeUtc { get { return m_find_data.LastWriteTimeUtc; } }

			/// <summary></summary>
			public override string ToString() { return FullPath; }
		}
#endif
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	[TestFixture] public class TestShell
	{
		[Test]
		public void Test()
		{ }
	}
}
#endif
