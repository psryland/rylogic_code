#if NETFRAMEWORK
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Rylogic.Extn;
using Rylogic.Interop.Win32;
using Rylogic.Utility;

namespace Rylogic.Common.Windows
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
		public static RuntimeAssembly Compile(string source_code, IEnumerable<string>? include_paths = null, IEnumerable<string>? defines = null)
		{
			var provider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
			var search_paths = new List<string>(include_paths ?? Array.Empty<string>())
			{
				Util.ResolveAppPath(),
			};
			var compiler_params = new CompilerParameters()
			{
				GenerateInMemory = true,
				GenerateExecutable = false,
				CompilerOptions = 
					$"/lib:\"{Util.ResolveAppPath()}\" " +
					(defines != null ? $"/define:{string.Join(";", defines)}" : ""),
			};

			// Add assembly references
			foreach (var ass in AssemblyPaths(source_code, search_paths))
				compiler_params.ReferencedAssemblies.Add(ass);

			// Compile the script into a dynamic assembly
			var results = provider.CompileAssemblyFromSource(compiler_params, source_code);
			if (results.Errors.Count != 0)
				throw new CompileException(results);

			// Return the compiled result
			return new RuntimeAssembly(results.CompiledAssembly);

			// Return the assembly references
			static IEnumerable<string> AssemblyPaths(string source_code, IEnumerable<string> search_paths)
			{
				// Get the currently loaded assemblies. We'll use these paths in preference to anything else
				var loaded_assemblies = AppDomain.CurrentDomain.GetAssemblies()
					.Where(x => !x.IsDynamic)
					.ToDictionary(x => x.GetName().Name, x => x.Location);

				// Add standard assemblies
				yield return typeof(object).Assembly.Location;

				// Scan the file for Reference Assembles
				foreach (var ass in AssemblyReferences(source_code))
				{
					var (ass_filetitle, ass_filename) =
						ass.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
						? (Common.Path_.FileTitle(ass), Common.Path_.FileName(ass))
						: (Common.Path_.FileName(ass), $"{Common.Path_.FileName(ass)}.dll");

					// Full paths do not need resolving
					if (Path.IsPathRooted(ass) && Common.Path_.FileExists(ass))
					{
						yield return ass;
						continue;
					}

					// See if the assembly is already loaded
					if (loaded_assemblies.TryGetValue(ass_filetitle, out string ass_fullpath))
					{
						yield return ass_fullpath;
						continue;
					}

					// See if .net can resolve it
					if (Win32.GetAssemblyPath(ass_filetitle) is string ass_filepath)
					{
						yield return ass_filepath;
						continue;
					}

					// Search the include paths
					if (search_paths.Select(x => Path.Combine(x, ass_filename)).FirstOrDefault(Common.Path_.FileExists) is string ass_filepath2)
					{
						yield return ass_filepath2;
						continue;
					}

					throw new FileNotFoundException($"Failed to resolve referenced assembly: {ass}\nSearch paths:\n{string.Join("\n", search_paths)}");
				}
			}
			static IEnumerable<string> AssemblyReferences(string source_code)
			{
				foreach (var line in source_code.Lines().Select(x => (string)x.Trim(' ', ';', '\t', '\r', '\n')))
				{
					if (!line.HasValue()) continue;
					if (!line.StartsWith("//")) break;
					if (!line.StartsWith("//Assembly:")) continue;

					// Read the assembly name
					var ass = line.Substring("//Assembly:".Length).Trim(' ', '\t');
					if (!ass.HasValue())
						continue;

					yield return ass;
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
				m_methods = new Cache<string, MethodInfo>(100) { ThreadSafe = true };
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