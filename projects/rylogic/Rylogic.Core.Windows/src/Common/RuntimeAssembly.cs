#if false
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Common
{
	public sealed class RuntimeAssembly :IDisposable
	{
		// Notes:
		//  - .NET core apps can use the implementation in Rylogic.Core that is based on Roslyn.
		//  - This is based on CodeDom. Roslyn is the future so, use the Rylogic.Core version if
		//    possible (i.e. in .net core, not .net framework).
		//    see: https://stackoverflow.com/questions/7852926/microsoft-roslyn-vs-codedom
		//  - This class mirrors the Rylogic.Core interface as much as possible

		/// <summary>Compile C# source code into an assembly</summary>
		public static RuntimeAssembly Compile(string source_code, IEnumerable<string>? include_paths = null, string? ass_name = null)
		{
			var provider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
			var compiler_params = new CompilerParameters()
			{
				GenerateInMemory = true,
				GenerateExecutable = false,
				CompilerOptions = $@"/lib:""{Util.ResolveAppPath()}""",
			};
			foreach (var ass in References(source_code, include_paths))
			{
				compiler_params.ReferencedAssemblies.Add(ass);
			}

			var results = provider.CompileAssemblyFromSource(compiler_params, source_code);
			if (results.Errors.Count != 0)
				throw new CompileException(results);

			// Return the compiled result
			return new RuntimeAssembly(results.CompiledAssembly);

			/// <summary>Return the reference assemblies from the source code</summary>
			static IEnumerable<string> References(string source_code, IEnumerable<string>? include_paths = null)
			{
				// Add standard assemblies
				var mscore_dll = typeof(object).Assembly.Location;
				yield return mscore_dll;

				// Get the directory for the .net assemblies
				var libdir = Path.GetDirectoryName(mscore_dll) ?? string.Empty;

				// Scan the file for Reference Assembles
				foreach (var line in source_code.Lines().Select(x => (string)x.Trim(' ', '\t', '\r', '\n')))
				{
					if (!line.HasValue()) continue;
					if (!line.StartsWith("//")) break;
					if (!line.StartsWith("//Assembly:")) continue;

					// Read the assembly name
					var ass = line.Substring(11).Trim(' ', '\t', '\r', '\n');
					if (!ass.HasValue())
						continue;

					// Full paths do not need resolving
					if (Path.IsPathRooted(ass) && Path_.FileExists(ass))
					{
						yield return ass;
						continue;
					}

					var search_paths = (include_paths ?? Array.Empty<string>()).Append(Util.ResolveAppPath());

					// Relative paths should be relative to the executable
					if (ass.StartsWith("."))
					{
						// Look in likely paths
						var ass_filepath = search_paths.Select(x => Path.Combine(x, ass)).FirstOrDefault(x => Path_.FileExists(x));
						if (ass_filepath != null)
						{
							yield return ass_filepath;
							continue;
						}
					}
					else
					{
						// Look in 'libdir' for a system assembly
						var ass_filepath = Path.Combine(libdir, ass);
						if (Path_.FileExists(ass_filepath))
						{
							yield return ass_filepath;
							continue;
						}
					}

					throw new FileNotFoundException($"Failed to resolve referenced assembly: {ass}\nSearch paths: {string.Join("\n", search_paths)}");
				}
			}
		}

		/// <summary>Compile source into an in-memory assembly</summary>
		private RuntimeAssembly(Assembly assembly)
		{
			Assembly = assembly;
		}
		public void Dispose()
		{
		}

		/// <summary>The loaded runtime assembly</summary>
		private Assembly Assembly { get; }

		/// <summary>Call the entry point function</summary>
		public int Main(string[] args)
		{
			var entry = Assembly.EntryPoint;
			var ret =
				entry == null ? throw new Exception($"This runtime assembly does not have an entry point") :
				entry.GetParameters().Length > 0 ? entry.Invoke(null, new object?[] { args }) :
				entry.Invoke(null, null);

			return (int?)ret ?? 0;
		}

		/// <summary>Create an instance of a type from this assembly</summary>
		public Instance New(string type_name, object[]? args = null)
		{
			return new Instance(Assembly, type_name, args);
		}

		/// <summary>An instance of a type in a runtime assembly</summary>
		public sealed class Instance :IDisposable
		{
			private readonly object m_inst;
			private readonly Cache<string, MethodInfo> m_methods;

			public Instance(Assembly ass, string type_name, object[]? args)
			{
				m_inst = ass.CreateInstance(type_name, false, BindingFlags.Public | BindingFlags.Instance, null, args, null, null) ?? throw new Exception($"No type {type_name} found");
				m_methods = new Cache<string, MethodInfo>(100);
			}
			public void Dispose()
			{
				if (m_inst is IDisposable dis)
					dis.Dispose();
			}

			/// <summary>Call a method with a return value</summary>
			public TResult Invoke<TResult>(string function, params object?[] args)
			{
				var mi = m_methods.Get(function, f => m_inst.GetType().GetMethod(f) ?? throw new Exception($"No method called {function} found"));
				return (TResult)mi.Invoke(m_inst, args)!;
			}

			/// <summary>Call a method with a return value</summary>
			public void Invoke(string function, params object?[] args)
			{
				var mi = m_methods.Get(function, f => m_inst.GetType().GetMethod(f) ?? throw new Exception($"No method called {function} found"));
				_ = mi.Invoke(m_inst, args)!;
			}
		}
	}

	/// <summary>Exception type for errors in runtime assembly code</summary>
	public class CompileException :Exception
	{
		private readonly CompilerResults m_results;
		public CompileException(CompilerResults results)
		{
			m_results = results;
		}

		/// <summary>The compilation errors</summary>
		public CompilerErrorCollection Errors => m_results.Errors;

		/// <summary>Returns a string describing all of the errors</summary>
		public string ErrorReport()
		{
			var sb = new StringBuilder();
			foreach (var err in Errors) sb.AppendLine(err.ToString());
			return sb.ToString();
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture] public class TestCompile
	{
		//HACK disable because pwsh can't run it [Test]
		public void Test0()
		{
			//var rylogic_dll = Path_.FileName(Assembly.GetExecutingAssembly().Location);

			var source = @"
			//Assembly: System.dll
			//Assembly: System.Drawing.dll
			//Assembly: System.Windows.Forms.dll

			using System;
			using System.Drawing;
			using System.Windows.Forms;

			namespace Food
			{
				public class Main
				{
					public Point SayHello()
					{
						//MessageBox.Show(""Boo!"");
						return new Point(1,2);
					}
				}
			}";

			using var ass = RuntimeAssembly.Compile(source);
			using var inst = ass.New("Food.Main");
			var result = inst.Invoke<System.Drawing.Point>("SayHello");
			Assert.Equal(result, new System.Drawing.Point(1,2));
		}
	}
}
#endif
#endif