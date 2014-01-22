using System;
using System.Windows.Forms;
using pr.common;

namespace Csex
{
	public class Program :Cmd
	{
		private const string VersionString = "v1.0";
		private readonly string[] m_args;
		private Cmd m_cmd;

		[STAThread]
		static int Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			return new Program(args).Run();
		}

		public Program(string[] args)
		{
			m_args = args;
		}

		/// <summary>Main run</summary>
		public override int Run()
		{
			// Check the name of the exe and do behaviour based on that.
			//var name = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
			//switch (name.ToLowerInvariant())
			//{
			//default: break;
			//}

			// Invalid arguments, error exit
			if (CmdLine.Parse(this, m_args) != CmdLine.Result.Success)
				return 1;

			try
			{
				// Execute the command
				if (m_cmd != null)
					return m_cmd.Run();

				// No command, show help and clean exit
				ShowHelp();
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: {0}",ex.Message);
				return 1;
			}
		}

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
			if (m_cmd != null)
				m_cmd.ShowHelp();
			else
				Console.WriteLine(
				"\n"+
				"***********************************************************\n"+
				" --- Commandline Extensions - Copyright © Rylogic 2012 --- \n"+
				"***********************************************************\n"+
				"                                         Version: "+VersionString+"\n"+
				"  Syntax: Csex -command [parameters]\n"+
				"\n"+
				"  Commands:\n"+
				"    -gencode\n" +
				"        Generates an activation code\n" +
				"\n"+
				"    -signfile\n" +
				"        Sign a file using RSA\n" +
				"\n"+
				"    -find_assembly_conflicts\n" +
				"        Recursively checks assemblies for version conflicts in their dependent assemblies\n" +
				"\n" +
				"    -expand_template\n" +
				"       Expand specific comments in a markup language (xml,html) file\n" +
				"\n" +
				"    -PatternUI\n" +
				"       Show the Regex pattern testing ui\n" +
				"\n" +
				"    -find_duplicate_files\n" +
				"       Find duplicate files within a directory tree\n" +
				"\n" +
				"    -showexif\n" +
				"       Display Exif info for a jpg file\n" +
				"\n" +
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
				switch (option.ToLowerInvariant())
				{
				case "-gencode":  m_cmd = new GenActivationCode(); break;
				case "-signfile": m_cmd = new SignFile(); break;
				case "-find_assembly_conflicts": m_cmd = new FindAssemblyConflicts(); break;
				case "-expand_template": m_cmd = new MarkupExpand(); break;
				case "-patternui": m_cmd = new PatternUI(); break;
				case "-find_duplicate_files": m_cmd = new FindDuplicateFiles(); break;
				case "-showexif": m_cmd = new ShowExif(); break;
					// NEW_COMMAND - handle the command
				}
			}
			return m_cmd == null
				? base.CmdLineOption(option, args, ref arg)
				: m_cmd.CmdLineOption(option, args, ref arg);
		}

		/// <summary>Forward arg to the command</summary>
		public override bool CmdLineData(string data, string[] args, ref int arg)
		{
			return m_cmd == null
				? base.CmdLineData(data, args, ref arg)
				: m_cmd.CmdLineData(data, args, ref arg);
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_cmd == null || m_cmd.OptionsValid();
		}
	}
}
