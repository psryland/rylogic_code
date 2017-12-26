using System;
using Rylogic.Extn;

namespace Csex
{
	public class XmlEdit :Cmd
	{
		private string m_filepath;

		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Edit an XML file\n" +
				" Syntax: Csex -xmledit -f filepath\n"
				);		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-xmledit": return true;
			case "-f": m_filepath = args[arg++]; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return true;
		}

		/// <summary>Run the command</summary>
		public override int Run()
		{
			Rylogic.Windows32.Win32.FreeConsole();
			using (var dlg = new XmlEditUI())
			{
				if (m_filepath.HasValue()) dlg.OpenFile(m_filepath);
				dlg.ShowDialog();
			}
			return 0;
		}
	}
}
