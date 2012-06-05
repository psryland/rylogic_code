using System;
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
				"{0} has crashed with the following error.\r\n"+
				"Error: {1}\r\n{2}\r\n"+
				"\r\n"+
				"Version: {3}\r\n"+
				"\r\n"+
				"Please contact {4} with information about this error so that it can be fixed.\r\n"+
				"\r\n"+
				"Thanks"
				,Util.GetAssemblyAttribute<AssemblyTitleAttribute>().Title
				,err.Message
				,err.InnerException != null ? err.InnerException.Message : ""
				,Util.AssemblyVersion()
				,Util.GetAssemblyAttribute<AssemblyCompanyAttribute>().Company)
				,"Unexpected Termination"
				,MessageBoxButtons.OK
				,MessageBoxIcon.Error);
		}
	}
}
