//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.IO;
using System.Linq;
using Rylogic.Extn;

namespace Rylogic.Utility
{
	///// <summary>Utility function container</summary>
	//public static class Util2
	//{
	//	/// <summary>
	//	/// A helper for copying lib files to the current output directory
	//	/// 'src_filepath' is the source lib name with {platform} and {config} optional substitution tags
	//	/// 'dst_filepath' is the output lib name with {platform} and {config} optional substitution tags
	//	/// If 'dst_filepath' already exists then it is overridden if 'overwrite' is true, otherwise 'DestExists' is returned
	//	/// 'src_filepath' can be a relative path, if so, the search order is: local directory, Q:\sdk\pr\lib
	//	/// </summary>
	//	[Obsolete("Use python instead")] public static ELibCopyResult LibCopy(string src_filepath, string dst_filepath, bool overwrite)
	//	{
	//		// Do text substitutions
	//		src_filepath = src_filepath.Replace("{platform}", Environment.Is64BitProcess ? "x64" : "x86");
	//		dst_filepath = dst_filepath.Replace("{platform}", Environment.Is64BitProcess ? "x64" : "x86");
	//
	//		const string config = Util.IsDebug ? "debug" : "release";
	//		src_filepath = src_filepath.Replace("{config}", config);
	//		dst_filepath = dst_filepath.Replace("{config}", config);
	//
	//		// Check if 'dst_filepath' already exists
	//		dst_filepath = Path.GetFullPath(dst_filepath);
	//		if (!overwrite && Path_.FileExists(dst_filepath))
	//		{
	//			Log.Info(null, $"LibCopy: Not copying {src_filepath} as {dst_filepath} already exists");
	//			return ELibCopyResult.DestExists;
	//		}
	//
	//		// Get the full path for 'src_filepath'
	//		for (;!Path.IsPathRooted(src_filepath);)
	//		{
	//			// If 'src_filepath' exists in the local directory
	//			var full = Path.GetFullPath(src_filepath);
	//			if (File.Exists(full))
	//			{
	//				src_filepath = full;
	//				break;
	//			}
	//
	//			// Get the pr libs directory
	//			full = Path.Combine(@"P:\pr\sdk\lib", src_filepath);
	//			if (File.Exists(full))
	//			{
	//				src_filepath = full;
	//				break;
	//			}
	//
	//			// Can't find 'src_filepath'
	//			Log.Info(null, $"LibCopy: Not copying {src_filepath}, file not found");
	//			return ELibCopyResult.SrcNotFound;
	//		}
	//
	//		// Copy the file
	//		Log.Info(null, $"LibCopy: {src_filepath} -> {dst_filepath}");
	//		File.Copy(src_filepath, dst_filepath, true);
	//		return ELibCopyResult.Success;
	//	}
	//	public enum ELibCopyResult { Success, DestExists, SrcNotFound }
	//
	//	/// <summary>
	//	/// Attempts to locate an application installation directory on the local machine.
	//	/// Returns the folder location if found, or null.
	//	/// 'look_in' are optional extra directories to check</summary>
	//	public static string LocateDir(string relative_path, params string[] look_in)
	//	{
	//		if (look_in.Length == 0)
	//			look_in = new []
	//			{
	//				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
	//				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
	//				@"C:\Program Files",
	//				@"C:\Program Files (x86)",
	//				@"D:\Program Files",
	//				@"D:\Program Files (x86)",
	//			};
	//			
	//		return look_in
	//			.Select(x => Path_.CombinePath(x, relative_path))
	//			.FirstOrDefault(Path_.DirExists);
	//	}
	//}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	[TestFixture] public class TestUtils
	{
		[Test] public void ToDo()
		{
		}
	}
}
#endif
