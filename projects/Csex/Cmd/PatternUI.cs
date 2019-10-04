using System;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;

namespace Csex
{
	public class PatternUI :Cmd
	{
		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"Shows the regex pattern UI\n" +
				" Syntax: Csex -PatternUI\n" +
				"");
		}

		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option.ToLowerInvariant())
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-patternui": return true;
			}
		}

		/// <summary>Validate the current options</summary>
		public override Exception Validate()
		{
			return null;
		}

		public override int Run()
		{
			new Rylogic.Gui.WinForms.PatternUI().FormWrap(title:"Edit Pattern", sz:new Size(1024,768), start_pos:FormStartPosition.CenterScreen).ShowDialog();
			return 0;
		}
	}
}
