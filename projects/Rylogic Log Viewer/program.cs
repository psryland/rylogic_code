using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Rylogic_Log_Viewer
{
	static class program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			//const string line = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
			//Random rnd = new Random(1);
			//var f = new StreamWriter(new FileStream(@"D:\deleteme\huge.txt", FileMode.Create, FileAccess.Write));
			//for (int i = 0; i != 10*1024*1024; ++i)
			//{
			//    int s = rnd.Next(line.Length);
			//    int e = rnd.Next(s, line.Length);
			//    f.WriteLine(line.Substring(s,e-s));
			//}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Main());
		}
	}
}
