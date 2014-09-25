using System;
using pr.common;
using pr.util;

namespace Csex
{
	public class RunUnitTests :Cmd
	{
		/// <summary>The filepath of the assembly to run tests in</summary>
		private string m_ass_filepath;

		/// <summary>Run the command</summary>
		public override int Run()
		{
			return pr.unittest.UnitTest.RunTests(m_ass_filepath) ? 0 : 1;
		}

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
			Console.Write(
				"Execute the unit tests in a .NET assembly\n"+
				" Syntax: Csex -runtests assembly_filepath\n"
				);
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-runtests": return true;
			}
		}

		/// <summary>Handle anything not preceded by '-'. Return true to continue parsing, false to stop</summary>
		public override bool CmdLineData(string data, string[] args, ref int arg)
		{
			m_ass_filepath = data;
			return true;
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return PathEx.FileExists(m_ass_filepath);
		}
	}
}
