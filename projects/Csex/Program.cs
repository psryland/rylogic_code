using System;
using pr.common;

namespace Csex
{
	public class Program :Cmd
	{
		private const string VersionString = "v1.0";
		private readonly string[] m_args;
		private Cmd m_cmd;

		[STAThread]
		static void Main(string[] args) { new Program(args).Run(); }
		public Program(string[] args)   { m_args = args; }

		/// <summary>Main run</summary>
		public override void Run()
		{
			// Check the name of the exe and do behaviour based on that.
			//Path.GetFileNameWithoutExtension(ExecutablePath);

			try { CmdLine.Parse(this, m_args); }
			catch (Exception ex)
			{
				Console.WriteLine("Error: {0}",ex.Message);
				ShowHelp();
				return;
			}

			if (m_cmd == null) { ShowHelp(); return; }
			try { m_cmd.Run(); }
			catch (Exception ex)
			{
				Console.WriteLine("Error: {0}",ex.Message);
			}
		}

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
			Console.WriteLine(
				"\n"+
				"***********************************************************\n"+
				" --- Commandline Extensions - Copyright © Rylogic 2012 --- \n"+
				"***********************************************************\n"+
				" Version: "+VersionString+"\n"+
				"  Syntax: Csex -command [parameters]\n"+
				"    -gencode : Generates an activation code\n"+
				"    -signfile : Sign a file using RSA\n"+
				"    -find_assembly_conflicts : Recursively checks assemblies\n" +
				"      for version conflicts in their dependent assemblies" +
				"    -expand_template : Expand server side include comments in an html file\n" +
				// NEW_COMMAND - add a help string
				"\n"+
				"  Type Cex -command -help for help on a particular command\n"+
				"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			if (m_cmd == null)
			{
				switch (option)
				{
				case "-gencode":  m_cmd = new GenActivationCode(); break;
				case "-signfile": m_cmd = new SignFile(); break;
				case "-find_assembly_conflicts": m_cmd = new FindAssemblyConflicts(); break;
				case "-expand_template": m_cmd = new MarkupExpand(); break;
					// NEW_COMMAND - handle the command
				}
			}
			return m_cmd == null
				? base.CmdLineOption(option, args, ref arg)
				: m_cmd.CmdLineOption(option, args, ref arg);
		}

		/// <summary>Forward arg to the command</summary>
		public override bool CmdLineData(string[] args, ref int arg)
		{
			return m_cmd == null
				? base.CmdLineData(args, ref arg)
				: m_cmd.CmdLineData(args, ref arg);
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_cmd == null || m_cmd.OptionsValid();
		}
	}
}
