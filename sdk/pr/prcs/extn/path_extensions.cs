//***************************************************
// Xml Helper Functions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace pr.extn
{
	public static class PathEx
	{
		///<summary>Returns 'full_file_path' relative to 'rel_path'</summary>
		public static string MakeRelativePath(string full_file_path, string rel_path)
		{
			const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
			const int FILE_ATTRIBUTE_NORMAL = 0x80;
			StringBuilder path_builder = new StringBuilder(260); // MAX_PATH
			PathRelativePathTo(path_builder, rel_path, FILE_ATTRIBUTE_DIRECTORY, full_file_path, FILE_ATTRIBUTE_NORMAL);
			string path = path_builder.ToString();
			if (path.Length == 0) return full_file_path;
			return path;
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
			return Path.GetFullPath(path);
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
	}
	
	/// <summary>Unit tests for xml extensions</summary>
	[TestFixture] internal partial class UnitTests
	{
		[Test] public static void TestPathEx()
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
	}
}
