//***************************************************
// Win32 wrapper
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Rylogic.Common;
using Rylogic.Utility;
using HWND = System.IntPtr;

namespace Rylogic.Interop.Win32
{
	public static partial class Win32
	{
		/// <summary>Helper method for loading a dll from a platform specific path. 'dllname' should include the extn</summary>
		public static IntPtr LoadDll(string dllname, out Exception? load_error, string dir = @".\lib\$(platform)\$(config)", bool throw_if_missing = true, ELoadLibraryFlags flags = ELoadLibraryFlags.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR|ELoadLibraryFlags.LOAD_LIBRARY_SEARCH_USER_DIRS|ELoadLibraryFlags.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS)
		{
			var searched = new List<string>();
			var ass = Assembly.GetEntryAssembly();

			// Substitute the platform and config
			dir = dir.Replace("$(platform)", Environment.Is64BitProcess ? "x64" : "x86");
			dir = dir.Replace("$(config)", Util.IsDebug ? "debug" : "release");
			
			// Try the working directory
			if (dir.Length != 0)
			{
				// 'dir' should be '.' to get the current working directory
				var working_dir = Path.GetFullPath(dir);
				var dll_path = Path_.CombinePath(working_dir, dllname);
				if (!searched.Contains(dll_path))
				{
					searched.Add(dll_path);
					if (Path_.FileExists(dll_path))
					{
						// Add all directories in 'dir' to the search path
						using var users_dirs = PushSearchPaths(dll_path, dir);
						return TryLoad(dll_path, out load_error, flags);
					}
				}
			}

			// Try the EXE directory
			if (ass != null)
			{
				var exe_path = ass.Location;
				var exe_dir = Path_.Directory(exe_path);
				var dll_path = Path_.CombinePath(exe_dir, dir, dllname);
				if (!searched.Contains(dll_path))
				{
					searched.Add(dll_path);
					if (Path_.FileExists(dll_path))
					{
						// Add all directories in 'dir' to the search path
						using var users_dirs = PushSearchPaths(dll_path, dir);
						return TryLoad(dll_path, out load_error, flags);
					}
				}
			}

			// Try the local directory
			if (ass != null)
			{
				var exe_path = ass.Location;
				var exe_dir = Path_.Directory(exe_path);
				var dll_path = Path_.CombinePath(exe_dir, dllname);
				if (!searched.Contains(dll_path))
				{
					searched.Add(dll_path);
					if (Path_.FileExists(dll_path))
					{
						// Add all directories in 'dir' to the search path
						using var users_dirs = PushSearchPaths(dll_path, dir);
						return TryLoad(dll_path, out load_error, flags);
					}
				}
			}

			// Allow LoadDll to be called multiple times if needed
			load_error = new DllNotFoundException($"Could not find dependent library '{dllname}'\r\nLocations searched:\r\n{string.Join("\r\n", [.. searched])}");
			return !throw_if_missing ? IntPtr.Zero : throw load_error;

			// The path is found, attempt to load the dll
			static HWND TryLoad(string path, out Exception? load_error, ELoadLibraryFlags flags)
			{
				load_error = null;

				Debug.WriteLine($"Loading native dll '{path}'...");
				var module = Kernel32.LoadLibraryEx(path, IntPtr.Zero, flags);
				if (module != IntPtr.Zero)
					return module;

				var msg = GetLastErrorString();
				load_error = new Exception(
					$"Found dependent library '{path}' but it failed to load.\r\n" +
					$"This is likely to be because a library that '{path}' is dependent on failed to load.\r\n" +
					$"Last Error: {msg}");
				throw load_error;
			}

			// Push more search directories into the dll search path
			static IDisposable PushSearchPaths(string dll_path, string dir)
			{
				// If the parent directories are: debug/release, x64/x86, or lib, add them to the search paths
				return Scope.Create(() =>
				{
					var cookies = new List<IntPtr>();
					var d = Path.GetDirectoryName(dll_path);
					HashSet<string> parents = ["debug", "release", "x64", "x86", "lib"];
					for (; d != null && parents.Contains(Path.GetFileName(d).ToLower()); d = Path.GetDirectoryName(d))
					{
						var cookie = Kernel32.AddDllDirectory(d);
						if (cookie != IntPtr.Zero)
							cookies.Add(cookie);
					}

					return cookies;
				},
				(cookies) =>
				{
					foreach (var cookie in cookies)
						Kernel32.RemoveDllDirectory(cookie);
				});
			}
		}

	}
}
