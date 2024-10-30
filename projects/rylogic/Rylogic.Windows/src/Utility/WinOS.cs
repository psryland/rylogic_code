using System;
using System.Security.Principal;
using Microsoft.Win32;
using Rylogic.Extn;
using Rylogic.Interop.Win32;

namespace Rylogic.Windows
{
	public static class WinOS
	{
		/// <summary>Prevent windows from going to sleep while the calling thread is running</summary>
		public static void SystemSleep(bool keep_awake)
		{
			var flags = Win32.ExecutionState.EsContinuous;
			if (keep_awake) flags |= Win32.ExecutionState.EsSystemRequired;
			Kernel32.SetThreadExecutionState(flags);
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
				using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true) ?? throw new NullReferenceException("Registry key not found");
				key.SetValue(app_name, executable_path.Quotes(add: true));
			}
			else
			{
				using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true) ?? throw new NullReferenceException("Registry key not found");
				key.DeleteValue(app_name, false);
			}
		}

		/// <summary>Return the windows account SID for a given user name (or null if it doesn't exist)</summary>
		public static string? UserSID(string user_name)
		{
			try
			{
				var account = new NTAccount(user_name);
				var s = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
				return s.ToString();
			}
			catch (IdentityNotMappedException)
			{
				return null;
			}
		}

		/// <summary>Return the User Profile directory of a given user (or null if it doesn't exist)</summary>
		public static string? UserProfilePath(string? user_name = null, string? user_sid = null)
		{
			if (user_sid == null)
			{
				user_name = user_name ?? throw new Exception("Either user_sid or user_name must be given");
				user_sid = UserSID(user_name);
			}
			if (user_sid == null)
			{
				return null;
			}

			// Return the profile path from the registry
			using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{user_sid}") ?? throw new NullReferenceException("Registry key not found");
			return key.GetValue("ProfileImagePath") is string profile_path ? profile_path : null;
		}
	}
}
