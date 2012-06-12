using System;
using System.IO;
using System.Reflection;
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
		static void Main(string[] args)
		{
			Exception err;
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new Main(args));
				return;
			}
			catch (Exception ex) { err = ex; }
			MessageBox.Show(string.Format(
				@"{0} has crashed with the following error.\r\n"+
				@"Error: {1}\r\n"+
				@"\r\n"+
				@"Version: {2}\r\n"+
				@"\r\n"+
				@"Deleting the applications settings file {3} (typically found here: 'C:\Users\<UserName>\AppData\Roaming\Rylogic Limited\RyLogViewer\settings.xml') might prevent this problem.\r\n"+
				@"Please contact {4} with information about this error so that it can be fixed.\r\n"+
				@"\r\n"+
				@"Thanks"
				,Util.GetAssemblyAttribute<AssemblyTitleAttribute>().Title
				,err
				,Util.AssemblyVersion()
				,Path.GetFileName(Settings.Default.Filepath)
				,Util.GetAssemblyAttribute<AssemblyCompanyAttribute>().Company)
				,"Unexpected Termination"
				,MessageBoxButtons.OK
				,MessageBoxIcon.Error);
		}
	}
}
