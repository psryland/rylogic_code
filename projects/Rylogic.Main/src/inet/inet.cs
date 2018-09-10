using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Rylogic.Interop.Win32;

namespace Rylogic.Gui.WinForms
{
	public static partial class INet
	{
		[Flags] public enum CREDUI_FLAGS
		{
			INCORRECT_PASSWORD          = 0x1,
			DO_NOT_PERSIST              = 0x2,
			REQUEST_ADMINISTRATOR       = 0x4,
			EXCLUDE_CERTIFICATES        = 0x8,
			REQUIRE_CERTIFICATE         = 0x10,
			SHOW_SAVE_CHECK_BOX         = 0x40,
			ALWAYS_SHOW_UI              = 0x80,
			REQUIRE_SMARTCARD           = 0x100,
			PASSWORD_ONLY_OK            = 0x200,
			VALIDATE_USERNAME           = 0x400,
			COMPLETE_USERNAME           = 0x800,
			PERSIST                     = 0x1000,
			SERVER_CREDENTIAL           = 0x4000,
			EXPECT_CONFIRMATION         = 0x20000,
			GENERIC_CREDENTIALS         = 0x40000,
			USERNAME_TARGET_CREDENTIALS = 0x80000,
			KEEP_USERNAME               = 0x100000,
		}

		public enum CredUIReturnCodes
		{
			NO_ERROR                    = 0,
			ERROR_CANCELLED             = 1223,
			ERROR_NO_SUCH_LOGON_SESSION = 1312,
			ERROR_NOT_FOUND             = 1168,
			ERROR_INVALID_ACCOUNT_NAME  = 1315,
			ERROR_INSUFFICIENT_BUFFER   = 122,
			ERROR_INVALID_PARAMETER     = 87,
			ERROR_INVALID_FLAGS         = 1004,
		}

		[Flags] public enum PromptForWindowsCredentialsFlags
		{
			/// <summary>The caller is requesting that the credential provider return the user name and password in plain text. This value cannot be combined with SECURE_PROMPT.</summary>
			CREDUIWIN_GENERIC = 0x1,

			/// <summary>The Save check box is displayed in the dialog box.</summary>
			CREDUIWIN_CHECKBOX = 0x2,

			/// <summary>Only credential providers that support the authentication package specified by the authPackage parameter should be enumerated. This value cannot be combined with CREDUIWIN_IN_CRED_ONLY. </summary>
			CREDUIWIN_AUTHPACKAGE_ONLY = 0x10,

			/// <summary>Only the credentials specified by the InAuthBuffer parameter for the authentication package specified by the authPackage parameter should be enumerated.
			/// If this flag is set, and the InAuthBuffer parameter is NULL, the function fails.
			/// This value cannot be combined with CREDUIWIN_AUTHPACKAGE_ONLY.</summary>
			CREDUIWIN_IN_CRED_ONLY = 0x20,

			/// <summary>Credential providers should enumerate only administrators. This value is intended for User Account Control (UAC) purposes only. We recommend that external callers not set this flag.</summary>
			CREDUIWIN_ENUMERATE_ADMINS = 0x100,
			
			/// <summary>Only the incoming credentials for the authentication package specified by the authPackage parameter should be enumerated.</summary>
			CREDUIWIN_ENUMERATE_CURRENT_USER = 0x200,

			/// <summary>The credential dialog box should be displayed on the secure desktop. This value cannot be combined with CREDUIWIN_GENERIC.
			/// Windows Vista: This value is not supported until Windows Vista with SP1.</summary>
			CREDUIWIN_SECURE_PROMPT = 0x1000,

			/// <summary>The credential provider should align the credential BLOB pointed to by the refOutAuthBuffer parameter to a 32-bit boundary, even if the provider is running on a 64-bit system.</summary>
			CREDUIWIN_PACK_32_WOW = 0x10000000,
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct CREDUI_INFO
		{
			public int    cbSize;
			public IntPtr hwndParent;
			public string pszMessageText;
			public string pszCaptionText;
			public IntPtr hbmBanner;

			/// <summary>Construct an instance of 'CREDUI_INFO'</summary>
			public static CREDUI_INFO Make(string caption, string message, IntPtr parent_hwnd)
			{
				return new CREDUI_INFO
				{
					cbSize = Marshal.SizeOf(typeof(CREDUI_INFO)),
					pszCaptionText = caption,
					pszMessageText = message,
					hwndParent     = parent_hwnd,
				};
			}
		}

		[DllImport("credui.dll", CharSet = CharSet.Unicode)]
		public static extern uint CredUIPromptForWindowsCredentials(
			ref CREDUI_INFO credui_info,             // Info to display on the dialog
			int last_auth_error,                     // The error code (from WinError.h) that was the reason for the last auth failure
			ref uint auth_package,                   // <shrug>
			IntPtr in_auth_buffer,                   // A pointer to a credential BLOB that is used to populate the credential fields in the dialog box. Set the value of this parameter to NULL to leave the credential fields empty.
			uint in_auth_buffer_size,                // The size, in bytes, of the pvInAuthBuffer buffer.
			out IntPtr out_auth_buffer,              // The address of a pointer that, on output, specifies the credential BLOB. For Kerberos, NTLM, or Negotiate credentials, call the CredUnPackAuthenticationBuffer function to convert this BLOB to string representations of the credentials.
			out uint out_auth_buffer_size,           // The size, in bytes, of the ppvOutAuthBuffer buffer.
			ref bool fSave,                          // A pointer to a Boolean value that, on input, specifies whether the Save check box is selected in the dialog box that this function displays. On output, the value of this parameter specifies whether the Save check box was selected when the user clicks the Submit button in the dialog box. Set this parameter to NULL to ignore the Save check box.
			PromptForWindowsCredentialsFlags flags); // A value that specifies behaviour for this function. This value can be a bitwise-OR combination of one or more of the following values.

		[DllImport("credui.dll", CharSet = CharSet.Auto)]
		private static extern bool CredUnPackAuthenticationBuffer(
			int dwFlags,
			IntPtr pAuthBuffer,
			uint cbAuthBuffer,
			StringBuilder pszUserName,
			ref int pcchMaxUserName,
			StringBuilder pszDomainName,
			ref int pcchMaxDomainame,
			StringBuilder pszPassword,
			ref int pcchMaxPassword);


		/// <summary>Get network credentials for a given location, prompting if necessary</summary>
		public static NetworkCredential GetCredentials(Uri uri)
		{
			// Get the credentials
			uint auth_package = 0; IntPtr out_cred_buffer; uint out_cred_size; bool save = false;
			CREDUI_INFO credui = CREDUI_INFO.Make("Connecting to: " + uri.Host, "Username / Password required", IntPtr.Zero);
			uint result = CredUIPromptForWindowsCredentials(ref credui, 0, ref auth_package, IntPtr.Zero, 0, out out_cred_buffer, out out_cred_size, ref save, PromptForWindowsCredentialsFlags.CREDUIWIN_GENERIC);
			if (result != 0) return null;
			StringBuilder username_buf = new StringBuilder(100); int max_user_name = 100;
			StringBuilder password_buf = new StringBuilder(100); int max_password = 100;
			StringBuilder domain_buf   = new StringBuilder(100); int max_domain = 100;
			if (!CredUnPackAuthenticationBuffer(0, out_cred_buffer, out_cred_size, username_buf, ref max_user_name, domain_buf, ref max_domain, password_buf, ref max_password))
				return null;

			//clear the memory allocated by CredUIPromptForWindowsCredentials 
			//SecureZeroMem(outCredBuffer, outCredSize); // ms documentation says we should call this but i can't get it to work
			Win32.CoTaskMemFree(out_cred_buffer);

			username_buf.Length = max_user_name;
			password_buf.Length = max_password;
			domain_buf  .Length = max_domain;
			return new NetworkCredential{UserName = username_buf.ToString(), Password = password_buf.ToString(), Domain = domain_buf.ToString()};
		}

		public enum ResourceScope
		{
			RESOURCE_CONNECTED = 1,
			RESOURCE_GLOBALNET,
			RESOURCE_REMEMBERED,
			RESOURCE_RECENT,
			RESOURCE_CONTEXT
		}

		public enum ResourceType
		{
			RESOURCETYPE_ANY,
			RESOURCETYPE_DISK,
			RESOURCETYPE_PRINT,
			RESOURCETYPE_RESERVED
		}

		public enum ResourceUsage
		{
			RESOURCEUSAGE_CONNECTABLE = 0x00000001,
			RESOURCEUSAGE_CONTAINER = 0x00000002,
			RESOURCEUSAGE_NOLOCALDEVICE = 0x00000004,
			RESOURCEUSAGE_SIBLING = 0x00000008,
			RESOURCEUSAGE_ATTACHED = 0x00000010,
			RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE | RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED),
		}

		public enum ResourceDisplayType
		{
			RESOURCEDISPLAYTYPE_GENERIC,
			RESOURCEDISPLAYTYPE_DOMAIN,
			RESOURCEDISPLAYTYPE_SERVER,
			RESOURCEDISPLAYTYPE_SHARE,
			RESOURCEDISPLAYTYPE_FILE,
			RESOURCEDISPLAYTYPE_GROUP,
			RESOURCEDISPLAYTYPE_NETWORK,
			RESOURCEDISPLAYTYPE_ROOT,
			RESOURCEDISPLAYTYPE_SHAREADMIN,
			RESOURCEDISPLAYTYPE_DIRECTORY,
			RESOURCEDISPLAYTYPE_TREE,
			RESOURCEDISPLAYTYPE_NDSCONTAINER
		}

		[StructLayout(LayoutKind.Sequential)]
		public class NETRESOURCE
		{
			public ResourceScope dwScope = 0;
			public ResourceType dwType = 0;
			public ResourceDisplayType dwDisplayType = 0;
			public ResourceUsage dwUsage = 0;
			public string lpLocalName;
			public string lpRemoteName;
			public string lpComment;
			public string lpProvider;
		};

		[DllImport("mpr.dll")] public static extern int WNetAddConnection2(NETRESOURCE netResource, string password, string username, int flags);
	}
}
