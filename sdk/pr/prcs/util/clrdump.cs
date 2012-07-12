using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace pr.util
{
	/// <summary>Bindings for clrdump. Ensure the clrdump and dbghelp dlls are in a subdirectory called 'lib'</summary>
	public static class ClrDump
	{
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("lib\\clrdump.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		public static extern bool CreateDump(int ProcessId, string FileName, MINIDUMP_TYPE DumpType, int ExcThreadId, IntPtr ExtPtrs);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("lib\\clrdump.dll", CharSet=CharSet.Unicode, SetLastError=true)]
		public static extern bool RegisterFilter(string FileName, MINIDUMP_TYPE DumpType);

		[DllImport("lib\\clrdump.dll")]
		public static extern FILTER_OPTIONS SetFilterOptions(FILTER_OPTIONS Options);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("lib\\clrdump.dll", SetLastError=true)]
		public static extern bool UnregisterFilter();

		[Flags]
		public enum FILTER_OPTIONS
		{
			CLRDMP_OPT_CALLDEFAULTHANDLER = 1
		}

		[Flags]
		public enum MINIDUMP_TYPE
		{
			MiniDumpFilterMemory = 8,
			MiniDumpFilterModulePaths = 0x80,
			MiniDumpNormal = 0,
			MiniDumpScanMemory = 0x10,
			MiniDumpWithCodeSegs = 0x2000,
			MiniDumpWithDataSegs = 1,
			MiniDumpWithFullMemory = 2,
			MiniDumpWithFullMemoryInfo = 0x800,
			MiniDumpWithHandleData = 4,
			MiniDumpWithIndirectlyReferencedMemory = 0x40,
			MiniDumpWithoutManagedState = 0x4000,
			MiniDumpWithoutOptionalData = 0x400,
			MiniDumpWithPrivateReadWriteMemory = 0x200,
			MiniDumpWithProcessThreadData = 0x100,
			MiniDumpWithThreadInfo = 0x1000,
			MiniDumpWithUnloadedModules = 0x20
		}
		
		/// <summary>Create a crash dump</summary>
		public static void Dump(string filepath = @"C:\temp\test.dmp", MINIDUMP_TYPE dump_type = MINIDUMP_TYPE.MiniDumpNormal)
		{
			IntPtr ext_ptrs = Marshal.GetExceptionPointers();
			CreateDump(System.Diagnostics.Process.GetCurrentProcess().Id
				,filepath
				,dump_type
				,Thread.CurrentThread.ManagedThreadId
				,ext_ptrs);
		}
		
		/// <summary>A simple default dump handler. (Serves as an example)</summary>
		public static void DefaultDumpHandler(object sender, UnhandledExceptionEventArgs e)
		{
			string msg = string.Format(
				"An unhandled exception has occurred in {0}\r\n"+
				"\r\n"+
				"Choose 'Yes' to create a dump file, 'No' to quit"
				,Application.ProductName);
			DialogResult res = MessageBox.Show(msg, "Unexpected Termination", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
			if (res == DialogResult.Yes)
			{
				var dg = new SaveFileDialog{Title = "Save dump file", DefaultExt = "dmp", CheckPathExists = true};
				if (dg.ShowDialog() == DialogResult.OK)
				{
					Dump(dg.FileName);
				}
			}
			Application.Exit();
		}

		/// <summary>Hook the unhandled exception handler</summary>
		public static void Init() { Init(DefaultDumpHandler); }
		public static void Init(UnhandledExceptionEventHandler dump_handler)
		{
			// Make GUI thread exceptions work the same as non-gui thread exceptions (i.e. call CurrentDomain.UnhandledException)
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
			AppDomain.CurrentDomain.UnhandledException += dump_handler;
		}
	}
}
