using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Csex
{
	public class Call :Cmd
	{
		private string m_ass;
		private string m_typename;
		private string m_method;
		private bool m_ignore_case;

		public Call()
		{
			m_ass = string.Empty;
			m_typename = string.Empty;
			m_method = string.Empty;
			m_ignore_case = false;
		}

		/// <summary>Command specific help</summary>
		public override void ShowHelp(Exception ex)
		{
			if (ex != null) Console.WriteLine("Error parsing command line: {0}", ex.Message);
			Console.Write(
				$"Load a .NET assembly and call a static method\n" +
				$" Syntax: Csex -call <assembly_path> <method_name> -ignore_case\n" +
				$"  <assembly_path> : the filepath of the assembly to load\n" +
				$"  <method_name> : the full name (including namespace) of the type in\n" +
				$"    square brackets followed by the method name\n" +
				$"    e.g.  [Rylogic.Core.Program]::Main()\n" +
				$"  -ignore_case : match method and type names ignoring case\n" +
				$"");
		}

		/// <summary>Command specific options</summary>
		public override bool CmdLineOption(string option, string[] args, ref int arg)
		{
			switch (option)
			{
			default: return base.CmdLineOption(option, args, ref arg);
			case "-call":
				{
					m_ass = arg != args.Length ? args[arg++] : throw new Exception("missing assembly filepath parameter");
					var meth = arg != args.Length ? args[arg++] : throw new Exception("missing method call parameter");
					var parts = meth.SubstringRegex(@"\[(.*?)\]::(.*?)\(\)");
					if (parts.Length != 2) throw new Exception("method call parameter should have the form: [Namespace.Type]::Method()");
					m_typename = parts[0];
					m_method = parts[1];
					return true;
				}
			case "-ignore_case":
				{
					m_ignore_case = true;
					return true;
				}
			}
		}

		/// <summary>Validate the current options</summary>
		public override Exception Validate()
		{
			return
				m_ass.Length == 0 ? new Exception($"Assembly parameter not given") :
				!Path_.FileExists(m_ass) ? new FileNotFoundException($"Assembly file '{m_ass}' not found") :
				null;
		}

		/// <summary>Execute the command</summary>
		public override int Run()
		{
			using (Scope.Create(
				() => AppDomain.CurrentDomain.AssemblyResolve += HandleAssemblyResolve,
				() => AppDomain.CurrentDomain.AssemblyResolve -= HandleAssemblyResolve))

			// Load the assembly
			using (var buf = new MemoryStream())
			{
				// Copy to memory
				//using (var fs = new FileStream(m_ass, FileMode.Open, FileAccess.Read, FileShare.Read))
				//	fs.CopyTo(buf);

				//var ass = Assembly.Load(buf.ToArray());
				var ass = Assembly.LoadFile(m_ass);
				if (ass == null)
					throw new Exception($"Failed to load assembly: {m_ass}");

				// Find the type
				var ty = ass.GetExportedTypes().FirstOrDefault(x => string.Compare(x.FullName, m_typename, m_ignore_case) == 0);
				if (ty == null)
					throw new Exception($"No type called {m_typename} found");

				// Find the static method
				var mi = ty.GetMethod(m_method, BindingFlags.Public | BindingFlags.Static);
				if (mi == null)
					throw new Exception($"No method called {m_method} on {m_typename} found");

				// Call the method
				return mi.Invoke(null, null) is int result ? result : 0;
			}

			// Resolve dependent assemblies
			Assembly HandleAssemblyResolve(object sender, ResolveEventArgs args)
			{
				return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
			}
		}
	}
}
