using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rylogic.Extn;

namespace Csex
{
	public class FindAssemblyConflicts :Cmd
	{
		private class Reference
		{
			public AssemblyName? Assembly ;
			public AssemblyName? ReferencedAssembly;
		}
		private string m_dir = string.Empty;

		/// <inheritdoc/>
		public override void ShowHelp(Exception? ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				"This command checks .NET assemblies for mismatches in dependent assembly versions\n" +
				" Syntax: Csex -find_assembly_conflicts [-p directory]\n" +
				"  -p : the directory to check. If not given the current directory is checked\n" +
				"");
		}

		/// <inheritdoc/>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
				case "-find_assembly_conflicts": return true;
				case "-p": m_dir = args[arg++]; return true;
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
			if (string.IsNullOrEmpty(m_dir))
				m_dir = Environment.CurrentDirectory;

			var assemblies = GetAllAssemblies(m_dir);
			var references = GetReferencesFromAllAssemblies(assemblies);
			var conflicts = FindReferencesWithTheSameShortNameButDiffererntFullNames(references);
			foreach (var group in conflicts)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("Possible conflicts for {0}:", group.Key);
				foreach (var reference in group.NotNull())
				{
					if (reference.Assembly is not AssemblyName a) continue;
					if (reference.ReferencedAssembly is not AssemblyName ra) continue;
					Console.Out.WriteLine($"{a.Name,-30} references {ra.FullName}");
				}
			}
			return 0;

			// Helpers
			IEnumerable<Assembly> GetAllAssemblies(string path)
			{
				var files = new List<FileInfo>();
				var directoryToSearch = new DirectoryInfo(path);
				files.AddRange(directoryToSearch.GetFiles("*.dll", SearchOption.AllDirectories));
				files.AddRange(directoryToSearch.GetFiles("*.exe", SearchOption.AllDirectories));
				return files
					.Select(file =>
					{
						try { return Assembly.LoadFile(file.FullName); }
						catch (Exception ex)
						{
							Console.WriteLine("Failed to load assembly: {0}\r\nReason: {1}\r\n{2}", file.FullName, ex.GetType().Name, ex.Message);
							return (Assembly?)null;
						}
					})
					.NotNull();
			}
			IEnumerable<Reference> GetReferencesFromAllAssemblies(IEnumerable<Assembly> assemblies)
			{
				var references = new List<Reference>();
				foreach (var assembly in assemblies)
				{
					if (assembly == null) continue;
					foreach (var ass in assembly.GetReferencedAssemblies())
						references.Add(new Reference { Assembly = assembly.GetName(), ReferencedAssembly = ass });
				}
				return references;
			}
			IEnumerable<IGrouping<string, Reference>> FindReferencesWithTheSameShortNameButDiffererntFullNames(IEnumerable<Reference> references)
			{
				return
					from reference in references
					group reference by reference.ReferencedAssembly?.Name
					into referenceGroup
					where referenceGroup.ToList().Select(reference => reference.ReferencedAssembly?.FullName).Distinct().Count() > 1
					select referenceGroup;
			}
		}
	}
}
