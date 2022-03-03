using System;

namespace Csex
{
	public class NEW_COMMAND :Cmd
	{
		/// <inheritdoc/>
		public override void ShowHelp(Exception? ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"This is a template command\n" +
				" Syntax: Csex -NEW_COMMAND -o option\n" +
				"  -o : this option is optional\n" +
				"");
		}

		/// <inheritdoc/>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
				case "-NEW_COMMAND": return true;
				default: return base.CmdLineOption(option, args, ref arg);
			}
		}

		/// <inheritdoc/>
		public override Exception? Validate()
		{
			return null;
		}

		/// <inheritdoc/>
		public override int Run()
		{
			return 0;
		}
	}
}
