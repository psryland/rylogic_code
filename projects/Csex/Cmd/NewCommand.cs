using System;

namespace Csex
{
	public class NEW_COMMAND :Cmd
	{
		/// <summary>Command specific help</summary>
		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"This is a template command\n" +
				" Syntax: Csex -NEW_COMMAND -o option\n" +
				"  -o : this option is optional\n" +
				"");
		}

		/// <summary>Command specific options</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-NEW_COMMAND": return true;
			}
		}

		/// <summary>Validate the current options</summary>
		public override Exception Validate()
		{
			return null;
		}

		/// <summary>Execute the command</summary>
		public override int Run()
		{
			return 0;
		}
	}
}
