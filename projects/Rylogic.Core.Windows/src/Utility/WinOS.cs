using System;
using Microsoft.Win32;
using Rylogic.Extn;
using Rylogic.Interop.Win32;

namespace Rylogic.Core.Windows
{
	public static class WinOS
	{
		/// <summary>Prevent windows from going to sleep while the calling thread is running</summary>
		public static void SystemSleep(bool keep_awake)
		{
			var flags = Win32.ExecutionState.EsContinuous;
			if (keep_awake) flags |= Win32.ExecutionState.EsSystemRequired;
			Win32.SetThreadExecutionState(flags);
		}

		/// <summary>Register an application to start when windows starts</summary>
		public static void StartWithWindows(bool enable, string app_name, string? executable_path)
		{
			if (string.IsNullOrEmpty(app_name))
				throw new Exception($"A valid application name is required");
			if (enable && string.IsNullOrEmpty(executable_path))
				throw new Exception($"A valid application executable path is required");

			if (enable && executable_path != null)
			{
				using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
				key.SetValue(app_name, executable_path.Quotes(add: true));
			}
			else
			{
				using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
				key.DeleteValue(app_name, false);
			}
		}
	}
}
