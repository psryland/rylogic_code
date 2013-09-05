using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Csex
{
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
		public override int Run()
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
			return 0;
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
}
