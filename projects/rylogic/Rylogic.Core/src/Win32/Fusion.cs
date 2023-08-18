using System;
using System.Runtime.InteropServices;

namespace Rylogic.Interop.Win32
{
	public static partial class Win32
	{
		/// <summary>Gets an assembly path from the GAC given a partial name, or null.</summary>
		public static string? GetAssemblyPath(string name)
		{
			var final_name = name;
			var ass_info = new AssemblyInfo
			{
				cchBuf = 1024,
				currentAssemblyPath = new string('\0', 1024),
			};

			var hr = CreateAssemblyCache(out var ac, 0);
			if (hr >= 0)
			{
				hr = ac.QueryAssemblyInfo(0, final_name, ref ass_info);
				if (hr < 0)
					return null;
			}

			return ass_info.currentAssemblyPath;
		}

		[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
		private interface IAssemblyCache
		{
			void Reserved0();

			[PreserveSig]
			int QueryAssemblyInfo(int flags, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, ref AssemblyInfo assemblyInfo);
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct AssemblyInfo
		{
			public int cbAssemblyInfo;
			public int assemblyFlags;
			public long assemblySizeInKB;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string currentAssemblyPath;
			public int cchBuf; // size of path buf.
		}

		[DllImport("fusion.dll")]
		private static extern int CreateAssemblyCache(out IAssemblyCache ppAsmCache, int reserved);
	}
}
