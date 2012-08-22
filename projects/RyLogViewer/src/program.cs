using System;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.inet;
using pr.util;

namespace RyLogViewer
{
	static class program
	{
		private const string CommandLineRef = "RyLogViewer.docs.CommandLineRef.html";
		
		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		[SecurityPermission(SecurityAction.Demand,ControlAppDomain=true)] // for the unhandled exception handler
		static void Main(string[] args)
		{
			// Register ClrDump handler
			try { ClrDump.Init(DumpHandler); } catch { Log.Warn(null, "ClrDump not available"); }
			
			Environment.ExitCode = 0;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			// Start the main app
			StartupOptions su = null; Exception err = null;
			try { su = new StartupOptions(args); } catch (Exception ex) { err = ex; }
			
			// If there was an error, or they just want help displayed...
			if (err != null || su.ShowHelp)
			{
				if (err != null)
				{
					MessageBox.Show(string.Format(
						"There is an error in the startup options provided.\r\n"+
						"Error Details:\r\n{0}"
						,err.Message)
						,"Command Line Error"
						,MessageBoxButtons.OK
						,MessageBoxIcon.Error);
				}
				HelpUI.ShowResource(null, CommandLineRef, Resources.AppTitle);
				Environment.ExitCode = 1;
				return;
			}
			
			// If an export path is given, run as a command line tool doing an export
			if (su.ExportPath != null)
			{
				RyLogViewer.Main.ExportToFile(su);
				return;
			}
			
			// Otherwise show the app
			Application.Run(new Main(su));
		}
		
		/// <summary>Handle dumps</summary>
		private static void DumpHandler(object sender, UnhandledExceptionEventArgs args)
		{
			#if DEBUG
			MessageBox.Show(string.Format(
				"{0} has shutdown with the following error."+
				"\r\nError: {1}"+
				"\r\nVersion: {2}"+
				"\r\n\r\nDeleting the applications settings file {3} (typically found here: 'C:\\Users\\<UserName>\\AppData\\Roaming\\Rylogic Limited\\RyLogViewer\\settings.xml') might prevent this problem."+
				"\r\n\r\nPlease contact {4} with information about this error so that it can be fixed."+
				"\r\n\r\nThanks"
				,Util.GetAssemblyAttribute<AssemblyTitleAttribute>().Title
				,args.ExceptionObject
				,Util.AssemblyVersion()
				,Path.GetFileName(Settings.Default.Filepath)
				,Util.GetAssemblyAttribute<AssemblyCompanyAttribute>().Company)
				,"Unexpected Termination"
				,MessageBoxButtons.OK
				,MessageBoxIcon.Error);
			#endif
			
			var res = MessageBox.Show(string.Format(
				"An unhandled exception has occurred in {0}.\r\n" +
				"\r\n" +
				"Creating a dump file and sending it to {1} will aid in the resolution of this issue.\r\n" +
				"\r\n" +
				"Choose 'Yes' to create a dump file, or 'No' to quit."
				,Application.ProductName
				,Constants.SupportEmail)
				,"Unexpected Termination"
				,MessageBoxButtons.YesNo, MessageBoxIcon.Error);
			if (res == DialogResult.Yes)
			{
				var dg = new SaveFileDialog{Title = "Save dump file", Filter = Resources.DumpFileFilter, DefaultExt = "dmp", CheckPathExists = true};
				if (dg.ShowDialog() == DialogResult.OK)
				{
					ClrDump.Dump(dg.FileName);
					try
					{
						// Try to create an email with the attachment ready to go
						var email = new Email();
						email.AddRecipient(Constants.SupportEmail, Email.MAPIRecipient.To);
						email.Subject = Application.ProductName + " crash report";
						email.Body = string.Format(
							"To {0},\r\n" +
							"\r\n" +
							"Attached is a crash report generated on {1}\r\n" +
							"A brief description of how the application was being used at the time follows:\r\n" +
							"\r\n\r\n\r\n\r\n" +
							"Regards,\r\n" +
							"A Helpful User"
							,Application.CompanyName
							,DateTime.Now);
						email.Attachments.Add(dg.FileName);
						email.Send();
					} catch {}
				}
			}
			Environment.ExitCode = 1;
			Application.Exit();
		}
	}
}
