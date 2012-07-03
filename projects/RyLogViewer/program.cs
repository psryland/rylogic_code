using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.util;

namespace RyLogViewer
{
	static class program
	{
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
				HelpUI.ShowResource(null, "RyLogViewer.docs.CommandLineRef.html", "Command Line and Startup Options");
				Environment.ExitCode = 1;
				return;
			}
			
			// Create the main application
			
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
			Environment.ExitCode = 1;
			ClrDump.DefaultDumpHandler(sender, args);
		}
	}
}
