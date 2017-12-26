using System;
using Rylogic.Common;

namespace Csex
{
	public abstract class Cmd :CmdLine.IReceiver
	{
		/// <summary>Run the command</summary>
		public abstract int Run();

		/// <summary>Display help information in the case of an invalid command line</summary>
		public abstract void ShowHelp(Exception ex = null);

		/// <summary>
		/// Handle a command line option.
		/// Handles the /?, -h, and -help commands</summary>
		public virtual bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			case "/?":
			case "-h":
			case "-help":
				ShowHelp();
				arg = args.Length;
				return true;
			}
			Console.WriteLine("Error: Unknown option '"+option+"'\n");
			return false;
		}

		/// <summary>Handle anything not preceded by '-'. Return true to continue parsing, false to stop</summary>
		public virtual bool CmdLineData(string data, string[] args, ref int arg)
		{
			return true;
		}

		/// <summary>Return true if all required options have been given</summary>
		public abstract bool OptionsValid();
	}
}
