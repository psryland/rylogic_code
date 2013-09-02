using System;

namespace Csex
{
	// NEW_COMMAND - implement
	public class NEW_COMMAND :Cmd
	{
		public override void ShowHelp()
		{
			Console.Write(
				"This is a template command\n" +
				" Syntax: Csex -NEW_COMMAND -o option\n" +
				"  -o : this option is optional\n" +
				"");
		}

		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-NEW_COMMAND": return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			throw new NotImplementedException();
		}

		public override void Run()
		{
			throw new NotImplementedException();
		}
	}
}
