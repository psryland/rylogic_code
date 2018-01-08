using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Rylogic.Extn
{
	public static class Path_
	{
		/// <summary>True if 'filepath' is a valid filepath. The file doesn't have to exist</summary>
		public static bool IsValidFilepath(string filepath, bool require_rooted)
		{
			try
			{
				// 'Directory' and 'FileName' don't necessarily get all of the string e.g. "P:\\dump\\file.tx##:t"
				// returns 'P:\\dump' for directory and 't' for filename. Use substring to ensure the entire string
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

		/// <summary>Create any missing directory paths in 'directory'</summary>
		public static void CreateDirs(string path)
		{
			System.IO.Directory.CreateDirectory(path);
		}

		/// <summary>Delete a file</summary>
		public static void DelFile(string path)
		{
			File.Delete(path);
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

		/// <summary>Remove '..' or '.' directories from a path and swap all '/' to '\'. Returns a full filepath</summary>
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
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

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

			Assert.False(Path_.IsValidFilepath(@"P:\dump\file.tx##:t", false));
			Assert.False(Path_.IsValidFilepath(@"P:\dump\fi:.txt", false));
			Assert.False(Path_.IsValidFilepath(@"P:\dump\f*.txt", false));
			Assert.False(Path_.IsValidFilepath(@"P:\dump\f?.txt", false));
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
	}
}
#endif
