﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Common
{
	public static class Path_
	{
		/// <summary>True if 'filepath' is a valid filepath. The file doesn't have to exist</summary>
		public static bool IsValidFilepath(string filepath, bool require_rooted)
		{
			try
			{
				// 'Directory' and 'FileName' don't necessarily get all of the string e.g. "A:\\dump\\file.tx##:t"
				// returns 'A:\\dump' for directory and 't' for filename. Use substring to ensure the entire string
				// is tested

				if (!filepath.HasValue())
					return false;

				// Get the directory and filename
				var dir = Directory(filepath) ?? string.Empty;
				var fname = filepath.Substring(dir.Length, filepath.Length - dir.Length);
				if (fname.Length > 0 && (fname[0] == Path.DirectorySeparatorChar || fname[0] == Path.AltDirectorySeparatorChar))
					fname = fname.Substring(1, fname.Length - 1);

				var invalid_chars = Path.GetInvalidFileNameChars();
				return
					fname.Length > 0 &&                         // Cannot be an empty filename
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
		public static bool FileExists(string? filepath)
		{
			// Using 'FileInfo' because it checks security permissions as well
			try { return filepath is not null && filepath.Length != 0 && new FileInfo(filepath).Exists; }
			catch (UnauthorizedAccessException) { return true; }
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

		/// <summary>The length of the file at 'path'</summary>
		public static long FileLength(string path)
		{
			return new FileInfo(path).Length;
		}

		/// <summary>Create any missing directory paths in 'directory'</summary>
		public static string CreateDirs(string path)
		{
			return System.IO.Directory.CreateDirectory(path).FullName;
		}

		/// <summary>Delete a file</summary>
		public static void DelFile(string path, bool fail_if_missing = true)
		{
			if (!fail_if_missing && !PathExists(path)) return;
			File.Delete(path);
		}

		/// <summary>True if 'path' is a file</summary>
		public static bool IsFile(string path)
		{
			return FileExists(path) && (File.GetAttributes(path) & FileAttributes.Directory) == 0;
		}
		public static bool IsFile(this FileSystemInfo fsi)
		{
			return fsi is FileInfo || fsi.Attributes.HasFlag(FileAttributes.Directory) == false;
		}

		/// <summary>True if 'path' is a directory</summary>
		public static bool IsDirectory(string path)
		{
			return DirExists(path) && File.GetAttributes(path).HasFlag(FileAttributes.Directory);
		}
		public static bool IsDirectory(this FileSystemInfo fsi)
		{
			return fsi is DirectoryInfo || fsi.Attributes.HasFlag(FileAttributes.Directory) == true;
		}

		/// <summary>True if 'path' is a directory containing no files or subdirectories</summary>
		public static bool IsEmptyDirectory(string path)
		{
			return IsDirectory(path) && !System.IO.Directory.EnumerateFileSystemEntries(path).Any();
		}

		/// <summary>True if 'path' represents a file or directory equal to or below 'directory'</summary>
		public static bool IsSubPath(string directory, string path)
		{
			if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException(nameof(directory));
			if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
			var d = Canonicalise(directory, change_case:Case.Lower);
			var p = Canonicalise(path, change_case:Case.Lower);
			return p.StartsWith(d);
		}

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
			return CombinePath((IEnumerable<string>)paths);
		}
		public static string CombinePath(IEnumerable<string> paths)
		{
			if (!paths.Any())
				return string.Empty;

			// Path combine now supports arrays...
			// Don't trim '\\' from the start of paths because this breaks UNC paths
			var path = Path.Combine(paths.ToArray());
			return Canonicalise(path);
		}

		///<summary>Returns 'full_file_path' relative to 'rel_path'</summary>
		public static string RelativePath(string rel_path, string full_file_path)
		{
			#if NETSTANDARD2_0 || true

			// Inspired by https://source.dot.net/ System.IO.GetRelativePath
			rel_path       = Canonicalise(rel_path, Case.Unchanged);
			full_file_path = Canonicalise(full_file_path, Case.Unchanged);

			// Normalise the paths - this ensures neither is effectively an empty path
			var rel = Canonicalise(rel_path, Case.Lower).TrimEnd(Path.DirectorySeparatorChar);
			var ful = Canonicalise(full_file_path, Case.Lower).TrimEnd(Path.DirectorySeparatorChar);

			// Don't have a common root?
			if (Path.GetPathRoot(ful) != Path.GetPathRoot(rel))
				return full_file_path;

			// Same paths, return '.'
			if (rel == ful)
				return ".";

			// Find the length of the common directory segments
			var len = 0;
			for (int i = 0, iend = Math.Min(rel.Length, ful.Length); i != iend && rel[i] == ful[i]; ++i)
				++len;

			// Remove partial directory matches. Both should match up to a separator.
			// 'len' will be the matching length excluding any last separator.
			if (len == 0) { }
			else if (len == rel.Length && (len == ful.Length || ful[len] == Path.DirectorySeparatorChar)) { }
			else if (len == ful.Length && (len == rel.Length || rel[len] == Path.DirectorySeparatorChar)) { }
			else for (; --len != 0 && rel[len] != Path.DirectorySeparatorChar;) { }

			var sb = new StringBuilder();

			// Add parent segments for segments past the common on the "from" path
			if (len < rel.Length)
			{
				sb.Append("..");
				for (int i = len + 1; i != rel.Length; ++i)
				{
					if (rel[i] != Path.DirectorySeparatorChar) continue;
					sb.Append(Path.DirectorySeparatorChar);
					sb.Append("..");
				}
			}
			else if (ful[len] == Path.DirectorySeparatorChar)
			{
				// No parent segments and we need to eat the initial separator
				//  (C:\Foo C:\Foo\Bar case)
				len++;
			}

			// Now add the rest of the "to" path, adding back the trailing separator
			if (len != ful.Length)
				sb.Append(full_file_path.Substring(len));

			return sb.ToString();

			#else
			return Path.GetRelativePath(rel_path, full_file_path);
			#endif
		}

		/// <summary>Remove '..' or '.' directories from a path and swap all '/' to '\'. Returns a full filepath</summary>
		public static string Canonicalise(string path, Case change_case = Case.Unchanged)
		{
			if (change_case == Case.Lower) path = path.ToLowerInvariant();
			if (change_case == Case.Upper) path = path.ToUpperInvariant();
			path = path.Replace('/', Path.DirectorySeparatorChar);
			path = path.Replace('\\', Path.DirectorySeparatorChar);
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

		/// <summary>Test two paths for equality</summary>
		public static bool Equal(string lhs, string rhs)
		{
			return Compare(lhs, rhs) == 0;
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

		/// <summary>Searches up the directory tree from 'initial_dir' looking for a directory matching 'dir_name'. Returns the first found</summary>
		public static string? FindAncestorDirectory(string dir_name, string initial_dir)
		{
			for (;;)
			{
				// Check 'dir_name' doesn't exist in the current directory
				var dir = Canonicalise(CombinePath(initial_dir, dir_name));
				if (DirExists(dir))
					return dir;

				// Prune off a sub directory. Exit if we reach the root
				var parent = Canonicalise(CombinePath(initial_dir, ".."));
				if (parent == initial_dir)
					break;

				initial_dir = parent;
			}
			return null;
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
		public static bool DiffContent(string lhs, string rhs, Action<string>? trace = null)
		{
			// Missing files are considered different
			var sfound = FileExists(lhs);
			var dfound = FileExists(rhs);
			if (!sfound)
			{
				if (trace != null) trace($"Content different, '{lhs}' not found");
				return true;
			}
			if (!dfound)
			{
				if (trace != null) trace($"Content different, '{rhs}' not found");
				return true;
			}

			// Check for differing file lengths
			var infoL = new FileInfo(lhs);
			var infoR = new FileInfo(rhs);
			if (infoL.Length != infoR.Length)
			{
				if (trace != null) trace($"Content different, '{lhs}' and '{rhs}' have different sizes");
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
							trace($"Content different, '{lhs}' and '{rhs}' have different content");
							for (var i = 0; i != Math.Min(readL, readR); ++i)
							{
								if (bufL[i] == bufR[i]) continue;
								trace($"diff at byte {i}: {bufL[i]} != {bufR[i]}");
								break;
							}
						}
						return true;
					}
				}
			}
			if (trace != null) trace($"'{lhs}' and '{rhs}' are identical");
			return false;
		}

		/// <summary>Attempt to resolve a partial filepath given a list of directories to search</summary>
		public static string ResolvePath(string partial_path, IList<string>? search_paths = null, string? current_dir = null, bool check_working_dir = true)
		{
			var searched_paths = new List<string>();
			return ResolvePath(partial_path, search_paths, current_dir, check_working_dir, ref searched_paths);
		}
		public static string ResolvePath(string partial_path, IList<string>? search_paths, string? current_dir, bool check_working_dir, ref List<string> searched_paths)
		{
			// If the partial path is actually a full path
			if (Path.IsPathRooted(partial_path))
			{
				if (FileExists(partial_path))
					return partial_path;
			}

			// If a current directory is provided, search there first
			if (current_dir != null)
			{
				var path = CombinePath(current_dir, partial_path);
				if (FileExists(path))
					return path;

				searched_paths.Add(Directory(path));
			}

			// Check the working directory
			if (check_working_dir)
			{
				var path = Path.GetFullPath(partial_path);
				if (FileExists(path))
					return path;

				searched_paths.Add(Directory(path));
			}

			// Search the search paths
			if (search_paths != null)
			{
				foreach (var dir in search_paths)
				{
					var path = CombinePath(dir, partial_path);
					if (FileExists(path))
						return path;

					// If the search paths contain partial paths, resolve recursively
					if (!Path.IsPathRooted(path))
					{
						var paths = search_paths;
						paths.RemoveIf(x => x == dir);
						path = ResolvePath(path, paths, current_dir, check_working_dir, ref searched_paths);
						if (FileExists(path))
							return path;
					}

					searched_paths.Add(Directory(path));
				}
			}

			// Return an empty string for unresolved
			return string.Empty;
		}

		/// <summary>
		/// Gets file system info for all files/directories in a directory that match a specific filter including all sub directories.
		/// 'file_filter/dir_filter' are filters on the filename only, not the full path.
		/// Returns 'FileInfo' or 'DirectoryInfo'. 'FileSystemInfo' is just the common base class.</summary>
		public static IEnumerable<FileSystemInfo> EnumFileSystem(string path, SearchOption search_flags = SearchOption.TopDirectoryOnly, Regex? file_filter = null, Regex? dir_filter = null, FileAttributes exclude = FileAttributes.Hidden, int max_depth = int.MaxValue, Func<string, bool>? progress = null, Func<string, Exception, bool>? error = null)
		{
			// Notes:
			//  - This function needs to be recursive in order to trap UnauthorizedAccessExceptions.
			//    Otherwise, it will throw without reporting all files/directories. Using 'queue' for recursion.

			// Default callbacks
			progress = progress ?? (s => true);
			error = error ?? ((s, e) => true);

			// For drive letters, add a \, 'FileIOPermission' needs it
			if (path.EndsWith(":"))
				path += "\\";

			// Sanity check
			if (!DirExists(path))
				throw new Exception($"Attempting to enumerate an invalid directory path: {path}");

			// Iterative recursion into directories
			var queue = new[] { path }.ToQueue(x => new { Info = new DirectoryInfo(x), Depth = 0 });
			for (; queue.Count != 0;)
			{
				var dir = queue.Dequeue();

				// If excluding directories, use the enumerate files function
				IEnumerable<string> entries;
				try
				{
					entries = exclude.HasFlag(FileAttributes.Directory)
						? System.IO.Directory.EnumerateFiles(dir.Info.FullName, "*", SearchOption.TopDirectoryOnly)
						: System.IO.Directory.EnumerateFileSystemEntries(dir.Info.FullName, "*", SearchOption.TopDirectoryOnly);
				}
				catch (UnauthorizedAccessException)
				{
					entries = Enumerable.Empty<string>();
				}
				catch (IOException)
				{
					entries = Enumerable.Empty<string>();
				}

				// Yield the files/directories
				foreach (var entry in entries)
				{
					var attr = FileAttributes.Normal;
					var exception = (Exception?)null;
					var entry_is_dir = false;
					try
					{
						attr = File.GetAttributes(entry);
						entry_is_dir = attr.HasFlag(FileAttributes.Directory);

						// Report progress on directories
						if (entry_is_dir && !progress(entry))
							break;

						// Exclude files/folders by attribute
						if ((attr & exclude) != 0)
							continue;

						// Filter if provided. Filter on the file/directory name only
						if (entry_is_dir && dir_filter?.Match(FileName(entry)) is Match dm && !dm.Success)
							continue;
						if (!entry_is_dir && file_filter?.Match(FileName(entry)) is Match fm && !fm.Success)
							continue;
					}
					catch (Exception ex) { exception = ex; }

					// Report errors, then skip
					if (exception != null)
					{
						if (!error(entry, exception)) break;
						continue;
					}

					// Yield the file/directory path
					if (entry_is_dir)
					{
						var di = new DirectoryInfo(entry);
						yield return di;

						// Recurse directories
						if (search_flags.HasFlag(SearchOption.AllDirectories) && dir.Depth < max_depth)
							queue.Enqueue(new { Info = di, Depth = dir.Depth + 1 });
					}
					else
					{
						yield return new FileInfo(entry);
					}
				}
			}
		}

		/// <summary>
		/// Scope object that creates a file called 'filepath.locked'.
		/// Blocks until 'filepath.locked' is created or 'max_block_time_ms' it reached.
		/// Throws if the lock cannot be created within the timeout.
		/// Used as a file system mutex-file.
		/// Note: Requires other processes to use a similar locking method</summary>
		public static Scope LockFile(string filepath, int max_attempts = 3, int max_block_time_ms = 1000)
		{
			// Arithmetic series: Sn = 1+2+3+..+n = n(1 + n)/2. 
			// For n attempts, the i'th attempt sleep time is: max_block_time_ms * i / Sn
			// because the sum of sleep times need to add up to max_block_time_ms.
			//  sleep_time(a) = a * back_off = a * max_block_time_ms / Sn
			//  back_off = max_block_time_ms / Sn = 2*max_block_time_ms / n(1+n)
			var back_off = 2.0 * max_block_time_ms / (max_attempts * (1 + max_attempts));

			var fpath = filepath + ".locked";
			for (var a = 0; a != max_attempts; ++a)
			{
				try
				{
					var fs = new FileStream(fpath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 8, FileOptions.DeleteOnClose);
					File.SetAttributes(fpath, FileAttributes.Hidden | FileAttributes.Temporary);
					return Scope.Create(null, () => fs.Dispose());
				}
				catch (IOException)
				{
					Thread.Sleep((int)(a * back_off)); // Back off delay
				}
			}
			throw new Exception($"Failed to lock file: '{filepath}'");
		}

#if false
		/// <summary>Test a directory for create file access permissions</summary>
		public static bool HasDirectoryPermission(string dir, FileSystemRights rights)
		{
			if (string.IsNullOrEmpty(dir))
				return false;

			try
			{
				var rules = Directory.GetAccessControl(dir).GetAccessRules(true, true, typeof(SecurityIdentifier));
				var identity = WindowsIdentity.GetCurrent();
				foreach (FileSystemAccessRule rule in rules)
				{
					if (!identity.Groups.Contains(rule.IdentityReference))
						continue;

					if ((rights & rule.FileSystemRights) != rights)
						continue;

					if (rule.AccessControlType != AccessControlType.Allow)
						continue;

					return true;
				}
			}
			catch { }
			return false;
		}
#endif
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture]
	public class TestPathEx
	{
		[Test]
		public void TestPathValidation()
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

			Assert.False(Path_.IsValidFilepath(@"A:\dump\file.tx##:t", false));
			Assert.False(Path_.IsValidFilepath(@"A:\dump\fi:.txt", false));
			Assert.False(Path_.IsValidFilepath(@"A:\dump\f*.txt", false));
			Assert.False(Path_.IsValidFilepath(@"A:\dump\f?.txt", false));
		}
		[Test]
		public void TestPathNames()
		{
			string path;

			//path = Path_.RelativePath(@"A:\dir\subdir\file.ext", @"A:\dir");
			//Assert.Equal(@".\subdir\file.ext", path);

			path = Path_.CombinePath(@"A:\", @".\dir\subdir2", @"..\subdir\", "file.ext");
			Assert.Equal(@"A:\dir\subdir\file.ext", path);

			path = Path_.SanitiseFileName("1_path@\"[{+\\!@#$%^^&*()\'/?", "@#$%", "A");
			Assert.Equal("1_pathAA[{+A!AAAA^^&A()'AA", path);

			const string noquotes   = "C:\\a b\\path.ext";
			const string withquotes = "\"C:\\a b\\path.ext\"";
			Assert.Equal(withquotes ,Path_.Quote(noquotes, true));
			Assert.Equal(withquotes ,Path_.Quote(withquotes, true));
			Assert.Equal(noquotes   ,Path_.Quote(noquotes, false));
			Assert.Equal(noquotes   ,Path_.Quote(withquotes, false));
		}
		[Test]
		public void RelativePaths()
		{
			// Note: since this test runs for netcoreapp3.1 and netstandard2.0 it effectively
			// ensures consistent behaviour between my implementation and the the .net core one.
			string rel;

			rel = Path_.RelativePath(@"A:\aaa\eee\fff\", @"A:\aaa\BBB\ccc\ddd");
			Assert.Equal(rel, @"..\..\BBB\ccc\ddd");

			rel = Path_.RelativePath(@"A:\aaa\eee\fff\", @"A:\aaa\bbb\..\..\ddd");
			Assert.Equal(rel, @"..\..\..\ddd");

			rel = Path_.RelativePath(@"A:\aaa\eee\fff\", @"B:\AAA\BBB\CCC\DDD");
			Assert.Equal(rel, @"B:\AAA\BBB\CCC\DDD");

			rel = Path_.RelativePath(@"A:\aaa\eee\fff", @"A:\aaa\");
			Assert.Equal(rel, @"..\..");

			rel = Path_.RelativePath(@"A:\aAa\eee\fff", @"A:\AaA\BbB\");
			Assert.Equal(rel, @"..\..\BbB\");

			rel = Path_.RelativePath(@"A:\aAa", @"A:\AaA\BbB\CcC");
			Assert.Equal(rel, @"BbB\CcC");

			rel = Path_.RelativePath(@"A:\aAa\Part", @"A:\AaA\Partial\CcC");
			Assert.Equal(rel, @"..\Partial\CcC");
		}
	}
}
#endif
