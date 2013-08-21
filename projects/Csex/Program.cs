using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using pr.common;
using pr.crypt;
using pr.extn;

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
				"    -gencode   : Generates an activation code\n"+
				"    -signfile  : Sign a file using RSA\n"+
				"    -find_assembly_conflicts : Recursively checks assemblies\n" +
				"      for version conflicts in their dependent assemblies" +
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
			return true;
		}
	}

	#region Cmd base class
	public abstract class Cmd :CmdLine.IReceiver
	{
		/// <summary>Run the command</summary>
		public abstract void Run();

		/// <summary>Display help information in the case of an invalid command line</summary>
		public abstract void ShowHelp();

		/// <summary>Handle a command line option.</summary>
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
		public virtual bool CmdLineData(string[] args, ref int arg) { return true; }

		/// <summary>Return true if all required options have been given</summary>
		public abstract bool OptionsValid();
	}
	#endregion

	#region Generate Activation Code
	public class GenActivationCode :Cmd
	{
		private string m_pk;
		
		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
			Console.Write(
				"Sign a file using RSA\n" +
				" Syntax: Csex -gencode -pk private_key.xml\n" +
				"  -pk : the RSA private key xml file to use\n" +
				"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-gencode": return true;
			case "-pk": m_pk = args[arg++]; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_pk.HasValue();
		}

		/// <summary>Run the command</summary>
		public override void Run()
		{
			var priv = File.ReadAllText(m_pk);
			var code = ActivationCode.Generate(priv);
			Clipboard.SetText(code);
			Console.WriteLine("Code Generated:\n"+code+"\n\nCode has been copied to the clipboard");
		}
	}
	#endregion

	#region SignFile
	public class SignFile :Cmd
	{
		private string m_file;
		private string m_pk;

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
			Console.Write(
				"Sign a file using RSA\n" +
				" Syntax: Csex -signfile -f file_to_sign -pk private_key.xml\n" +
				"  -f : the file to be signed. Signature will be appended to the file if not present\n" +
				"  -pk : the RSA private key xml file to use\n" +
				"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-signfile": return true;
			case "-f": m_file = args[arg++]; return true;
			case "-pk": m_pk = args[arg++]; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return m_file.HasValue() && m_pk.HasValue();
		}

		/// <summary>Run the command</summary>
		public override void Run()
		{
			var priv = File.ReadAllText(m_pk);
			Crypt.SignFile(m_file, priv);
			Console.WriteLine("'"+m_file+"' signed.");
		}
	}
	#endregion

	#region Find Assembly Conflicts
	public class FindAssemblyConflicts :Cmd
	{
		private class Reference
		{
			public AssemblyName Assembly           { get; set; }
			public AssemblyName ReferencedAssembly { get; set; }
		}
		private string m_dir;

		/// <summary>Display help information in the case of an invalid command line</summary>
		public override void ShowHelp()
		{
			Console.Write(
				"This command checks .NET assemblies for mismatches in dependent assembly versions\n" +
				" Syntax: Csex -find_assembly_conflicts [-p directory]\n" +
				"  -p : the directory to check. If not given the current directory is checked\n" +
				"");
		}

		/// <summary>Handle a command line option.</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-find_assembly_conflicts": return true;
			case "-p": m_dir = args[arg++]; return true;
			}
		}

		/// <summary>Return true if all required options have been given</summary>
		public override bool OptionsValid()
		{
			return true;
		}

		/// <summary>Run the command</summary>
		public override void Run()
		{
			if (string.IsNullOrEmpty(m_dir))
				m_dir = Environment.CurrentDirectory;
			
			var assemblies = GetAllAssemblies(m_dir);
			var references = GetReferencesFromAllAssemblies(assemblies);
			var groupsOfConflicts = FindReferencesWithTheSameShortNameButDiffererntFullNames(references);
			foreach (var group in groupsOfConflicts)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("Possible conflicts for {0}:", group.Key);
				foreach (var reference in group)
					Console.Out.WriteLine("{0,-30} references {1}" ,reference.Assembly.Name ,reference.ReferencedAssembly.FullName);
			}
		}
		private IEnumerable<IGrouping<string, Reference>> FindReferencesWithTheSameShortNameButDiffererntFullNames(IEnumerable<Reference> references)
		{
			return
				from reference in references
				group reference by reference.ReferencedAssembly.Name
				into referenceGroup
				where referenceGroup.ToList().Select(reference => reference.ReferencedAssembly.FullName).Distinct().Count() > 1
				select referenceGroup;
		}
		private IEnumerable<Reference> GetReferencesFromAllAssemblies(IEnumerable<Assembly> assemblies)
		{
			var references = new List<Reference>();
			foreach (var assembly in assemblies)
			{
				if (assembly == null) continue;
				foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
					references.Add(new Reference{Assembly = assembly.GetName(),ReferencedAssembly = referencedAssembly});
			}

			return references;
		}
		private IEnumerable<Assembly> GetAllAssemblies(string path)
		{
			var files = new List<FileInfo>();
			var directoryToSearch = new DirectoryInfo(path);
			files.AddRange(directoryToSearch.GetFiles("*.dll", SearchOption.AllDirectories));
			files.AddRange(directoryToSearch.GetFiles("*.exe", SearchOption.AllDirectories));
			return files.ConvertAll(file =>
				{
					try
					{
						return Assembly.LoadFile(file.FullName);
					}
					catch (Exception ex)
					{
						Console.WriteLine("Failed to load assembly: {0}\r\nReason: {1}\r\n{2}", file.FullName, ex.GetType().Name, ex.Message);
						return (Assembly)null;
					}
				});
		}
	}
	#endregion

	#region NEW_COMMAND
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
	#endregion
}
