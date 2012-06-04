using System;
using System.Windows.Forms;

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
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Main(args));
		}
	}
}
