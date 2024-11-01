using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Rylogic.Interop.Win32;

namespace Rylogic.Windows.Extn
{
	public static class Registry_
	{
		// Note:
		//  Microsoft.Win32 has a 'Registry' class which should probably be preferred to this.

		/// <summary>Return the data for a key along with its type</summary>
		public static (uint type, byte[] data) QueryValue(this RegistryKey key, string name)
		{
			var required_size = 0;
			var result = RegQueryValueEx_(key.Handle, name, IntPtr.Zero, out var type, IntPtr.Zero, ref required_size);
			if (result == Win32.ERROR_SUCCESS)
				return (type, Array.Empty<byte>());
			if (Marshal.GetLastWin32Error() != Win32.ERROR_INSUFFICIENT_BUFFER)
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"Registry.QueryValue failed for field: {name}");

			var buf = new byte[required_size];
			result = RegQueryValueEx_(key.Handle, name, IntPtr.Zero, out type, buf, ref required_size);
			if (result != Win32.ERROR_SUCCESS)
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"Registry.QueryValue failed for field: {name}");

			return (type, buf);
		}

		/// <summary>Open a registry key</summary>
		public static SafeRegistryHandle OpenKey(SafeRegistryHandle parent_key, string sub_key, uint ulOptions, uint samDesired)
		{
			if (RegOpenKeyEx_(parent_key, sub_key, ulOptions, samDesired, out var key) != Win32.ERROR_SUCCESS)
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open registry key: {sub_key}");
			
			return key;
		}
		[DllImport("advapi32.dll", EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint RegOpenKeyEx_(SafeRegistryHandle hKey, [MarshalAs(UnmanagedType.LPWStr)] string lpSubKey, uint ulOptions, uint samDesired, out SafeRegistryHandle result);

		/// <summary></summary>
		public static (uint type, byte[] data) QueryValue(SafeRegistryHandle key, string name)
		{
			var required_size = 0;
			var result = RegQueryValueEx_(key, name, IntPtr.Zero, out var type, IntPtr.Zero, ref required_size);
			if (result == Win32.ERROR_SUCCESS)
				return (type, Array.Empty<byte>());
			if (Marshal.GetLastWin32Error() != Win32.ERROR_INSUFFICIENT_BUFFER)
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"Registry.QueryValue failed for field: {name}");

			var buf = new byte[required_size];
			result = RegQueryValueEx_(key, name, IntPtr.Zero, out type, buf, ref required_size);
			if (result != Win32.ERROR_SUCCESS)
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"Registry.QueryValue failed for field: {name}");

			return (type, buf);
		}
		[DllImport("advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int RegQueryValueEx_(SafeRegistryHandle hKey, [MarshalAs(UnmanagedType.LPWStr)] string lpValueName, IntPtr lpReserved, out uint lpType, byte[] lpData, ref int lpcbData);
		[DllImport("advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int RegQueryValueEx_(SafeRegistryHandle hKey, [MarshalAs(UnmanagedType.LPWStr)] string lpValueName, IntPtr lpReserved, out uint lpType, IntPtr lpData, ref int lpcbData);

		/// <summary></summary>
		[DllImport("advapi32.dll", EntryPoint = "RegCloseKey", CharSet = CharSet.Ansi, SetLastError = true)]
		private static extern int RegCloseKey_(IntPtr hKey);
	}
}
