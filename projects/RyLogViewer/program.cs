using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	static class program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		[SecurityPermission(SecurityAction.Demand,ControlAppDomain=true)] // for the unhandled exception handler
		static void Main(string[] args)
		{
			try { ClrDump.Init(DumpHandler); } catch { Log.Warn(null, "ClrDump not available"); }
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Main(args));
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
			ClrDump.DefaultDumpHandler(sender, args);
		}
	}
}
