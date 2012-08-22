using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using pr.common;

namespace pr.inet
{
	/// <summary>Collects info for an email message</summary>
	public class Email
	{
		private readonly List<MAPI32.RecipDesc> m_recipients = new List<MAPI32.RecipDesc>();
		
		public enum MAPIRecipient
		{
			Orig=0,
			To,
			Cc,
			Bcc
		};

		public enum MAPISendMethod
		{
			Popup = MAPI32.LogonUI | MAPI32.Dialog,
			Direct = MAPI32.LogonUI
		}

		/// <summary>The email subject</summary>
		public string Subject { get; set; }

		/// <summary>The email body</summary>
		public string Body { get; set; }

		/// <summary>Attachments associated with the email</summary>
		public readonly List<string> Attachments = new List<string>();

		/// <summary>Add email recipients</summary>
		public void AddRecipient(string email, MAPIRecipient mapi_recipient)
		{
			var recipient = new MAPI32.RecipDesc();
			recipient.recipClass = (int)mapi_recipient;
			recipient.name = email;
			m_recipients.Add(recipient);
		}

		/// <summary>Calls the Mail api to send this email</summary>
		public void Send(MAPISendMethod how = MAPISendMethod.Popup)
		{
			using (var recip_buf = HGlobalBuffer.New<MAPI32.RecipDesc>(m_recipients.Count))
			using (var attach_buf = HGlobalBuffer.New<MAPI32.FileDesc>(Attachments.Count))
			{
				// Fill the recipient buffer
				if (m_recipients.Count != 0)
				{
					var ptr = recip_buf.Ptr;
					var size = Marshal.SizeOf(typeof(MAPI32.RecipDesc));
					foreach (var r in m_recipients)
					{
						Marshal.StructureToPtr(r, ptr, false);
						ptr += size;
					}
				}
					
				// Fill the attachments buffer
				if (Attachments.Count != 0)
				{
					var ptr = attach_buf.Ptr;
					var size = Marshal.SizeOf(typeof(MAPI32.FileDesc));
					foreach (var a in Attachments)
					{
						var file_desc = new MAPI32.FileDesc
						{
							position = -1,
							name = Path.GetFileName(a),
							path = a
						};
						Marshal.StructureToPtr(file_desc, ptr, false);
						ptr += size;
					}
				}
					
				var msg = new MAPI32.Message();
				msg.subject    = Subject ?? "";
				msg.noteText   = Body ?? "";
				msg.recipCount = m_recipients.Count;
				msg.recips     = recip_buf.Ptr;
				msg.fileCount  = Attachments.Count;
				msg.files      = attach_buf.Ptr;
				var res = MAPI32.SendMail(IntPtr.Zero, IntPtr.Zero, msg, (int)how, 0);
				if (res > 1) throw new Exception("MAPI32.SendMail failed. Result: " + MAPI32.ErrorString(res));
			}
		}
	}

	/// <summary>Wraps the MAPI32.dll for loading the default email client and composing an email</summary>
	public static class MAPI32
	{
		public const int LogonUI = 0x00000001;
		public const int Dialog  = 0x00000008;

		// ReSharper disable NotAccessedField.Global
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public class Message
		{
			public int reserved;
			public string subject;
			public string noteText;
			public string messageType;
			public string dateReceived;
			public string conversationID;
			public int flags;
			public IntPtr originator;
			public int recipCount;
			public IntPtr recips;
			public int fileCount;
			public IntPtr files;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public class FileDesc
		{
			public int reserved;
			public int flags;
			public int position;
			public string path;
			public string name;
			public IntPtr type;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
		public class RecipDesc
		{
			public int reserved;
			public int recipClass;
			public string name;
			public string address;
			public int eIDSize;
			public IntPtr entryID;
		}
		// ReSharper restore NotAccessedField.Global

		[DllImport("MAPI32.DLL", EntryPoint = "MAPISendMail", CallingConvention = CallingConvention.Winapi)]
		public static extern int SendMail(IntPtr sess, IntPtr hwnd, Message message, int flg, int rsv);

		/// <summary>Convert an error code into an error string</summary>
		public static string ErrorString(int error_code)
		{
			switch (error_code)
			{
			default: return "MAPI32 error ["+error_code+"]";
			case  0: return "OK [0]";
			case  1: return "User abort [1]";
			case  2: return "General MAPI failure [2]";
			case  3: return "MAPI login failure [3]";
			case  4: return "Disk full [4]";
			case  5: return "Insufficient memory [5]";
			case  6: return "Access denied [6]";
			case  7: return "-unknown- [7]";
			case  8: return "Too many sessions [8]";
			case  9: return "Too many files were specified [9]";
			case 10: return "Too many recipients were specified [10]";
			case 11: return "A specified attachment was not found [11]";
			case 12: return "Attachment open failure [12]";
			case 13: return "Attachment write failure [13]";
			case 14: return "Unknown recipient [14]";
			case 15: return "Bad recipient type [15]";
			case 16: return "No messages [16]";
			case 17: return "Invalid message [17]";
			case 18: return "Text too large [18]";
			case 19: return "Invalid session [19]";
			case 20: return "Type not supported [20]";
			case 21: return "A recipient was specified ambiguously [21]";
			case 22: return "Message in use [22]";
			case 23: return "Network failure [23]";
			case 24: return "Invalid edit fields [24]";
			case 25: return "Invalid recipients [25]";
			case 26: return "Not supported [26]";
			}
		}
	}
}


#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using inet;
	
	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestEmail()
		{
			var email = new Email();
			email.AddRecipient("paul@rylogic.co.nz", Email.MAPIRecipient.To);
			email.Subject = "NUnit test of Email";
			email.Body = "It Workz!";
			email.Send();
		}
	}
}
#endif
