using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Rylogic.Interop.Win32;

namespace Rylogic.Common.Windows
{
	public static class Path_
	{
		/// <summary>Test if two paths actually point to the same file or directory (via symlinks etc)</summary>
		public static bool IsSamePath(string lhs, string rhs)
		{
			if (Common.Path_.Equal(lhs, rhs))
				return true;

			// NOTE: we cannot lift the call to GetFileHandle out of this routine, because we _must_
			// have both file handles open simultaneously in order for the objectFileInfo comparison
			// to be guaranteed as valid.
			using (var handle0 = GetFileHandle(lhs))
			using (var handle1 = GetFileHandle(rhs))
			{
				Win32.BY_HANDLE_FILE_INFORMATION? object_file_info0 = GetFileInfo(handle0);
				Win32.BY_HANDLE_FILE_INFORMATION? object_file_info1 = GetFileInfo(handle1);
				return
					object_file_info0 != null &&
					object_file_info1 != null &&
					(object_file_info0.Value.FileIndexHigh == object_file_info1.Value.FileIndexHigh) &&
					(object_file_info0.Value.FileIndexLow  == object_file_info1.Value.FileIndexLow) &&
					(object_file_info0.Value.VolumeSerialNumber == object_file_info1.Value.VolumeSerialNumber);
			}

			static SafeFileHandle GetFileHandle(string path)
			{
				return Win32.CreateFile(path, Win32.EFileAccess.NEITHER, Win32.EFileShare.READ | Win32.EFileShare.WRITE, IntPtr.Zero, Win32.EFileCreation.OPEN_EXISTING, Win32.EFileFlag.BACKUP_SEMANTICS, IntPtr.Zero);
			}
			static Win32.BY_HANDLE_FILE_INFORMATION? GetFileInfo(SafeFileHandle handle)
			{
				return handle != null && Win32.GetFileInformationByHandle(handle.DangerousGetHandle(), out var object_file_info)
					? (Win32.BY_HANDLE_FILE_INFORMATION?)object_file_info
					: null;
			}
		}

		///<summary>Returns 'full_file_path' relative to 'rel_path'</summary>
		[Obsolete("Use the one in Rylogic.Core but note the parameters are swapped!")] public static string RelativePath(string full_file_path, string rel_path)
		{
			const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
			const int FILE_ATTRIBUTE_NORMAL = 0x80;
			var path_builder = new StringBuilder(260); // MAX_PATH
			PathRelativePathTo(path_builder, rel_path, FILE_ATTRIBUTE_DIRECTORY, full_file_path, FILE_ATTRIBUTE_NORMAL);
			var path = path_builder.ToString();
			return path.Length == 0 ? full_file_path : path;
		}
		[DllImport("shlwapi.dll")] private static extern int PathRelativePathTo(StringBuilder pszPath, string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);
	}
}
