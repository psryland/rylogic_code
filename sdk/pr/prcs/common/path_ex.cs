//***************************************************
// Xml Helper Functions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;
using pr.extn;
using pr.util;

namespace pr.common
{
	public static class PathEx
	{
		/// <summary>True if 'filepath' is a valid filepath</summary>
		public static bool IsValidFilepath(string filepath, bool require_rooted)
		{
			try
			{
				if (!filepath.HasValue()) return false;
				var dir = Path.GetDirectoryName(filepath) ?? string.Empty;
				var fname = Path.GetFileName(filepath) ?? string.Empty;
				var invalid_chars = Path.GetInvalidFileNameChars();
				return
					fname.HasValue() &&
					IsValidDirectory(dir, require_rooted) &&
					fname.IndexOfAny(invalid_chars) == -1;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>True if 'filepath' is a valid filepath</summary>
		public static bool IsValidDirectory(string dir, bool require_rooted)
		{
			try
			{
				var invalid_chars = Path.GetInvalidPathChars().Concat(new[]{'*','?'}).ToArray();
				return
					dir != null &&
					(!require_rooted || Path.IsPathRooted(dir)) &&
					dir.IndexOfAny(invalid_chars) == -1;
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

		///<summary>Returns 'full_file_path' relative to 'rel_path'</summary>
		public static string MakeRelativePath(string full_file_path, string rel_path)
		{
			const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
			const int FILE_ATTRIBUTE_NORMAL = 0x80;
			var path_builder = new StringBuilder(260); // MAX_PATH
			PathRelativePathTo(path_builder, rel_path, FILE_ATTRIBUTE_DIRECTORY, full_file_path, FILE_ATTRIBUTE_NORMAL);
			var path = path_builder.ToString();
			return path.Length == 0 ? full_file_path : path;
		}
		[DllImport("shlwapi.dll")] private static extern int PathRelativePathTo(StringBuilder pszPath, string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);

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
			return Cannonicalise(path);
		}

		/// <summary>Remove '..' or '.' directories from a path and swap all '/' to '\'</summary>
		public static string Cannonicalise(string path, Case change_case = Case.Unchanged)
		{
			if (change_case == Case.Lower) path = path.ToLowerInvariant();
			if (change_case == Case.Upper) path = path.ToUpperInvariant();
			path = path.Replace('/', '\\');
			return Path.GetFullPath(path);
		}
		public enum Case { Unchanged, Lower, Upper };

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

		/// <summary>Contains information about a file</summary>
		[Serializable] public class FileData
		{
			private readonly Win32.WIN32_FIND_DATA m_find_data;

			/// <summary>Full path to the file.</summary>
			public string FullPath { get; private set; }

			/// <summary>The filename.extn of the file</summary>
			public string FileName { get { return m_find_data.FileName; } }

			/// <summary>Attributes of the file.</summary>
			public FileAttributes Attributes { get { return m_find_data.Attributes; } }

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

			public FileData(string dir, Win32.WIN32_FIND_DATA find_data)
			{
				m_find_data = find_data;
				FullPath = Path.Combine(dir, FileName);
			}
		}

		/// <remarks>
		/// A fast enumerator of files in a directory.
		/// Use this if you need to get attributes for all files in a directory.
		/// This enumerator is substantially faster than using <see cref="Directory.GetFiles(string)"/>
		/// and then creating a new FileInfo object for each path.  Use this version when you
		/// will need to look at the attributes of each file returned (for example, you need
		/// to check each file in a directory to see if it was modified after a specific date).
		/// </remarks>

		/// <summary>Gets FileDatafor all files in a directory that  match a specific filter including all sub directories.</summary>
		[SuppressUnmanagedCodeSecurity]
		public static IEnumerable<FileData> EnumerateFiles(string path, string regex_filter = ".*", SearchOption flags = SearchOption.TopDirectoryOnly, RegexOptions regex_options = RegexOptions.None, Func<string,bool> progress = null)
		{
			var stack = new Stack<string>(20);
			stack.Push(path);

			// Default progress callback
			if (progress == null)
				progress = s => true;

			while (stack.Count != 0)
			{
				var dir = stack.Pop();
				if (!progress(dir))
					break;

				try { new FileIOPermission(FileIOPermissionAccess.PathDiscovery, dir).Demand(); }
				catch { continue; } // skip paths we don't have access to

				var filter = new Regex(regex_filter, regex_options);
				var pattern = Path.Combine(dir, "*");
				var find_data = new Win32.WIN32_FIND_DATA();
				var handle = FindFirstFile(pattern, find_data);
				for (var more = !handle.IsInvalid; more; more = FindNextFile(handle, find_data))
				{
					// If the found object is not a directory, assume it's a file
					if ((find_data.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
					{
						// Yield return files only
						if (filter.IsMatch(find_data.FileName))
							yield return new FileData(dir, find_data);
					}
					// Otherwise, it's a directory, see if we should be recursing
					else if (flags == SearchOption.AllDirectories)
					{
						if (find_data.FileName == "." || find_data.FileName == "..")
							continue;

						stack.Push(Path.Combine(dir, find_data.FileName));
					}
				}
				handle.Close();
			}
		}

		// ReSharper disable ClassNeverInstantiated.Local
		/// <summary>Wraps a FindFirstFile handle.</summary>
		private sealed class SafeFindHandle :SafeHandleZeroOrMinusOneIsInvalid
		{
			[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
			public SafeFindHandle() :base(true) {}
			protected override bool ReleaseHandle() { return FindClose(handle); }
		}
		// ReSharper restore ClassNeverInstantiated.Local

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern SafeFindHandle FindFirstFile(string fileName, [In, Out] Win32.WIN32_FIND_DATA data);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool FindNextFile(SafeFindHandle hndFindFile, [In, Out, MarshalAs(UnmanagedType.LPStruct)] Win32.WIN32_FIND_DATA lpFindFileData);

		[DllImport("kernel32.dll")][ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private static extern bool FindClose(IntPtr handle);
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using System.Linq;
	using common;

	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestPathEx
		{
			[Test] public static void TestPathValidation()
			{
				Assert.IsTrue(PathEx.IsValidDirectory(@"A:\dir1\..\.\dir2", true));
				Assert.IsTrue(PathEx.IsValidDirectory(@"A:\dir1\..\.\dir2", false));
				Assert.IsTrue(PathEx.IsValidDirectory(@".\dir1\..\.\dir2", false));
				Assert.IsTrue(PathEx.IsValidDirectory(@"A:\dir1\..\.\dir2\", true));
				Assert.IsTrue(PathEx.IsValidDirectory(@"A:\dir1\..\.\dir2\", false));
				Assert.IsTrue(PathEx.IsValidDirectory(@".\dir1\..\.\dir2\", false));
				Assert.IsFalse(PathEx.IsValidDirectory(@".\dir1?\..\.\", false));

				Assert.IsTrue(PathEx.IsValidFilepath(@"A:\dir1\..\.\dir2\file.txt", true));
				Assert.IsTrue(PathEx.IsValidFilepath(@"A:\dir1\..\.\dir2\file", false));
				Assert.IsTrue(PathEx.IsValidFilepath(@".\dir1\..\.\dir2\file", false));
				Assert.IsFalse(PathEx.IsValidFilepath(@".\dir1\", false));
				Assert.IsFalse(PathEx.IsValidFilepath(@".\dir1\file*.txt", false));
			}
			[Test] public static void TestPathNames()
			{
				string path;

				path = PathEx.MakeRelativePath(@"A:\dir\subdir\file.ext", @"A:\dir");
				Assert.AreEqual(@".\subdir\file.ext", path);

				path = PathEx.CombinePath(@"A:\", @".\dir\subdir2", @"..\subdir\", "file.ext");
				Assert.AreEqual(@"A:\dir\subdir\file.ext", path);

				path = PathEx.SanitiseFileName("1_path@\"[{+\\!@#$%^^&*()\'/?", "@#$%", "A");
				Assert.AreEqual("1_pathAA[{+A!AAAA^^&A()'AA", path);

				const string noquotes   = "C:\\a b\\path.ext";
				const string withquotes = "\"C:\\a b\\path.ext\"";
				Assert.AreEqual(withquotes ,PathEx.Quote(noquotes, true));
				Assert.AreEqual(withquotes ,PathEx.Quote(withquotes, true));
				Assert.AreEqual(noquotes   ,PathEx.Quote(noquotes, false));
				Assert.AreEqual(noquotes   ,PathEx.Quote(withquotes, false));
			}
			[Test] public static void TestEnumerateFiles()
			{
				var path = Environment.CurrentDirectory;
				var files = PathEx.EnumerateFiles(path, @".*\.dll", SearchOption.AllDirectories);
				var dlls = files.ToList();
				Assert.IsTrue(dlls.Count != 0);
			}
		}
	}
}

#endif
