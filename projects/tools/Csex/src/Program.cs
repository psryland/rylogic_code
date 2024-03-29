﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rylogic.Common;
using Rylogic.Extn;

namespace Csex
{
	public class Program :Cmd
	{
		private const string VersionString = "v2.0.0";

		[STAThread]
		static int Main(string[] args)
		{
			return new Program(args).Run();
		}
		public Program(string[] args)
		{
			m_args = args.ToList();
			//Rylogic.Interop.Win32.Win32.AllocConsole();
			AvailableCmds = typeof(Program).Assembly.GetExportedTypes()
				.Where(x => typeof(Cmd).IsAssignableFrom(x))
				.Except(typeof(Cmd))
				.Except(typeof(Program))
				.Except(typeof(NEW_COMMAND))
				.ToList();
		}

		private readonly List<string> m_args;
		private Cmd m_cmd = null!;

		/// <summary>Reflected commands</summary>
		public IList<Type> AvailableCmds { get; }

		/// <summary>Main run</summary>
		public override int Run()
		{
			// Check the name of the exe and do behaviour based on that.
			var name = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
			switch (name.ToLowerInvariant())
			{
				case "RegexTest":
				{
					m_args.Insert(0, "-regex_test");
					break;
				}
			}

			// Invalid arguments, error exit
			if (CmdLine.Parse(this, m_args.ToArray()) != CmdLine.Result.Success)
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
		public override void ShowHelp(Exception? ex = null)
		{
			// Show command specific help
			if (m_cmd != null)
			{
				m_cmd.ShowHelp(ex);
				return;
			}

			// Show the command line error message
			if (ex != null)
				Console.WriteLine("Error parsing command line: {0}", ex.Message);

			// Show the available commands
			Console.Write(
			$"*************************************************************\n" +
			$" --- Commandline Extensions - Copyright (c) Rylogic 2012 --- \n" +
			$"*************************************************************\n" +
			$"                                         Version: {VersionString}\n" +
			$"  Syntax: Csex -command [parameters]\n" +
			$"\n" +
			$"  Commands:\n" +
			$"    -call\n" +
			$"        Call a static method in a .NET assembly\n" +
			$"\n" +
			$"    -gencode\n" +
			$"        Generates an activation code\n" +
			$"\n" +
			$"    -signfile\n" +
			$"        Sign a file using RSA\n" +
			$"\n" +
			$"    -find_assembly_conflicts\n" +
			$"        Recursively checks assemblies for version conflicts in their dependent assemblies\n" +
			$"\n" +
			$"    -expand_template\n" +
			$"       Expand specific comments in a markup language (xml,html) file\n" +
			$"\n" +
			$"    -regex_test\n" +
			$"       Show the Regex pattern testing ui\n" +
			$"\n" +
			$"    -find_duplicate_files\n" +
			$"       Find duplicate files within a directory tree\n" +
			$"\n" +
			$"    -showexif\n" +
			$"       Display Exif info for a jpg file\n" +
			$"\n" +
			$"    -showtree\n" +
			$"       Display a tree grid view of a text file containing whitespace indenting\n" +
			$"\n" +
			$"    -showbase64\n" +
			$"       Display a tool for encoding/decoding base64 text\n" +
			$"\n" +
			$"    -xmledit\n" +
			$"       Display a tool for editing XML files\n" +
			$"\n" +
			// NEW_COMMAND
			$"  Type Cex -command -help for help on a particular command\n" +
			$"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			if (m_cmd == null)
			{
				switch (option.ToLowerInvariant())
				{
					case "-call": m_cmd = new Call(); break;
					case "-gencode": m_cmd = new GenActivationCode(); break;
					case "-signfile": m_cmd = new SignFile(); break;
					case "-find_assembly_conflicts": m_cmd = new FindAssemblyConflicts(); break;
					case "-expand_template": m_cmd = new MarkupExpand(); break;
					case "-regex_test": m_cmd = new RegexTest(); break;
					case "-showexif": m_cmd = new ShowExif(); break;
					case "-showtree": m_cmd = new ShowTree(); break;
					case "-showbase64": m_cmd = new ShowBase64(); break;
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
		public override Exception? Validate() => m_cmd?.Validate();
	}
}
