using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Rylogic.Gui.WinForms
{
	/// <summary>A control that makes a web browser behaviour as much like a text (or RTB) as possible</summary>
	public class HtmlTextBox : System.Windows.Forms.WebBrowser
	{
		#region IE Version
		
		public enum EBrowserEmulationVersion
		{
			Default            = 0,
			Version7           = 7000,
			Version8           = 8000,
			Version8Standards  = 8888,
			Version9           = 9000,
			Version9Standards  = 9999,
			Version10          = 10000,
			Version10Standards = 10001,
			Version11          = 11000,
			Version11Edge      = 11001
		}

		private const string InternetExplorerRootKey = @"Software\Microsoft\Internet Explorer";
		private const string BrowserEmulationKey     = InternetExplorerRootKey + @"\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

		/// <summary>
		/// Get the WebBrowser emulation version for the given application name.<para/>
		/// Throws: <para/>
		///  SecurityException: The user does not have the permissions required to read from the registry key.<para/>
		///  UnauthorizedAccessException: The user does not have the necessary registry rights.<para/></summary>
		public static EBrowserEmulationVersion BrowserEmulationVersion(string app_name = null)
		{
			using (var key = Registry.CurrentUser.OpenSubKey(BrowserEmulationKey, true))
			{
				if (key == null)
				{
					Debug.WriteLine($"Registry key '{BrowserEmulationKey}' not found");
					return EBrowserEmulationVersion.Default;
				}

				// Auto detect the app name
				if (app_name == null)
					app_name = Process.GetCurrentProcess().ProcessName  + ".exe";

				var value = key.GetValue(app_name, null);
				if (value == null)
				{
					Debug.WriteLine($"No registry value for BrowserEmulationVersion for {app_name}");
					return EBrowserEmulationVersion.Default;
				}

				return (EBrowserEmulationVersion)Convert.ToInt32(value);
			}
		}

		/// <summary>
		/// Get the WebBrowser emulation version for the given application name.<para/>
		/// Throws: <para/>
		///  SecurityException: The user does not have the permissions required to read from the registry key.<para/>
		///  UnauthorizedAccessException: The user does not have the necessary registry rights.<para/></summary>
		public static bool BrowserEmulationVersion(EBrowserEmulationVersion version, string app_name = null)
		{
			using (var Regkey = Registry.LocalMachine.OpenSubKey(BrowserEmulationKey, true))
			{
				// If the path is not correct or if user's have privileges to access registry 
				if (Regkey == null)
				{
					Debug.WriteLine($"Registry key '{BrowserEmulationKey}' not found");
					return false;
				}

				// Auto detect the app name
				if (app_name == null)
					app_name = Process.GetCurrentProcess().ProcessName  + ".exe";

				// Check if key is already present 
				var FindAppkey = Convert.ToString(Regkey.GetValue(app_name));
				if (FindAppkey == version.ToString())
					return true;

				// If key is not present or different from desired, add/modify the key value 
				Regkey.SetValue(app_name, unchecked((int)version), RegistryValueKind.DWord);

				// Check for the key after adding
				FindAppkey = Convert.ToString(Regkey.GetValue(app_name));
				return FindAppkey == version.ToString();
			}
		}

		#endregion

		public HtmlTextBox()
		{
			ResetView();
			AllowNavigation = true;
			AllowWebBrowserDrop = false;
			ScriptErrorsSuppressed = true;
		}
		protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			
			if (e.KeyCode == Keys.F5)
				e.IsInputKey = true; // Blocks Refresh which causes rendered html to vanish
		}

		/// <summary>Get/Set the text to display in the control</summary>
		public string Html
		{
			get { return m_impl_html; }
			set
			{
				m_impl_html = value;
				ResetView();
			}
		}
		private string m_impl_html;

		/// <summary>Clear the view</summary>
		public void ResetView()
		{
			Url = new Uri("about:blank", UriKind.Absolute);
			DocumentStream = new MemoryStream(Encoding.UTF8.GetBytes(Html ?? string.Empty));
		}
	}
}
