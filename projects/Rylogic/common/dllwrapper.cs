//***********************************************
// Dll Wrapper
//	(c)opyright Paul Ryland 2008
//***********************************************

// C++ Dll cpp file: 
//	int __declspec(dllexport) Func(int i) { return i; }
//	Makes "my_dll.dll"
//
// To static link to a dll use:
//	[DllImport("my_dll.dll")]
//	private static extern int Func(int b);
//	static void Main(string[] args)
//	{
//		Console.WriteLine(Func(3));
//	}

// Example use of the dynamic linking below:
//	class Program
//	{
//		delegate int NativeMessageBox(int hwnd, string text, string caption, uint type);
//		static void Main(string[] args)
//		{
//			using (DllWrapper user32dll = new DllWrapper("user32.dll"))
//			{
//				NativeMessageBox msg_box = (NativeMessageBox)user32dll.GetDelegate("MessageBoxA", typeof(NativeMessageBox));
//				msg_box(0, "Hello World", "Test Native DLL", 0);
//			}
//		}
//	}


using System;
using System.Runtime.InteropServices;

namespace PR
{
	/// <summary>
	/// A wrapper for a native dll
	/// </summary>
	public class DllWrapper : IDisposable
	{
		[DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
		static extern int LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

		[DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
		static extern IntPtr GetProcAddress(int hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		[DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
		static extern bool FreeLibrary(int hModule);

		private int m_module = 0;
		private bool m_disposed = false; // to detect redundant calls

		/// <param name="dll_name">The name of the dll to dynamically load</param>
		public DllWrapper(string dll_name)
		{
			m_module = LoadLibrary(dll_name);
			if (m_module == 0) throw new Exception("Failed to load dll: " + dll_name);
		}
		~DllWrapper()
		{
			Dispose(false);
		}

		/// <summary>
		/// Release the dll
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!m_disposed)
			{
				// Dispose managed resources
				if (disposing) { }

				// Release unmanaged resources
				if (m_module != 0)
				{
					FreeLibrary(m_module);
					m_module = 0;
				}

				// Flag as disposed... My god this is shit. RAII anyone?!?! FFS
				m_disposed = true;
			}
		}
		/// <summary>
		/// Convert the name of a function in the native dll to a delegate
		/// Use: DelegateType my_del = (DelegateType)dll_wrapper.GetDelegate("FunctionName", typeof(DelegateType));
		/// </summary>
		/// <param name="function_name">The name of the function to find in the dll</param>
		/// <returns>Returns a delegate for the exported function in the wrapped dll</returns>
		public Delegate GetDelegate(string function_name, Type delegate_type)
		{
			if( m_disposed ) throw new System.ObjectDisposedException("");

			IntPtr address = GetProcAddress(m_module, function_name);
			return Marshal.GetDelegateForFunctionPointer(address, delegate_type);
		}
	}	
}
