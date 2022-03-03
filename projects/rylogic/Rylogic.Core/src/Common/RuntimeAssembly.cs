#if true
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Common
{
	public sealed class RuntimeAssembly :AssemblyLoadContext, IDisposable
	{
		// Notes:
		//  - System.Runtime.Loader is not supported in .NET Framework (even though it says it's part of .NetStandard2.0).
		//    Everything compiles but when a .NET Framework project attempts to load the DLL you get a FileNotFoundException
		//    for 'System.Runtime.Loader'.
		//  - Until everything is based on .net core, you'll have to use the Rylogic.Core.Windows implementation based on CodeDom
		//  - Careful with loading dynamic assemblies:
		//    https://docs.microsoft.com/en-us/dotnet/framework/deployment/best-practices-for-assembly-loading

		/// <summary>Compile C# source code into an assembly</summary>
		public static RuntimeAssembly Compile(string source_code, IEnumerable<string>? include_paths = null, string? ass_name = null, CSharpCompilationOptions ? compilation_options = null, CSharpParseOptions? parse_options = null)
		{
			parse_options ??= CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
			compilation_options ??= new CSharpCompilationOptions(
				outputKind: OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release,
				assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
				);

			// Create a syntax tree from the source code
			var code_string = SourceText.From(source_code);
			var parsed_syntax_tree = SyntaxFactory.ParseSyntaxTree(code_string, parse_options);

			// Read the reference assemblies from the comments in the source
			var references = References(source_code, include_paths).Select(x => MetadataReference.CreateFromFile(x)).ToArray();

			// Compilation builder instance
			ass_name ??= $"{Guid.NewGuid()}.dll";
			var compilation = CSharpCompilation.Create(ass_name, new[] { parsed_syntax_tree }, references, compilation_options);

			// Compile to a byte buffer
			using var buffer = new MemoryStream();
			var result = compilation.Emit(buffer);

			// If compilation failed, report the errors
			if (!result.Success)
				throw new CompileException(result);

			// Return the compiled result
			return new RuntimeAssembly(buffer.ToArray());

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

		private RuntimeAssembly(byte[] compiled_assembly)
			:base()
		{
			// Assembly.Load will load into the 'Neither' context.
			// Types are considered different to types in the default 'Load' context.
			Assembly = Assembly.Load(compiled_assembly);
		}
		public void Dispose()
		{}

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

		/// <summary></summary>
		protected override Assembly? Load(AssemblyName _) { return null; }

		/// <summary>An instance of a type in a runtime assembly</summary>
		public sealed class Instance :IDisposable
		{
			private readonly object m_inst;
			private readonly Cache<string, MethodInfo> m_methods;
			
			public Instance(Assembly ass, string type_name, object[]? args)
			{
				m_inst = ass.CreateInstance(type_name, false, BindingFlags.Public|BindingFlags.Instance, null, args, null, null) ?? throw new Exception($"No type {type_name} found");
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
		private readonly EmitResult m_result;
		public CompileException(EmitResult result)
		{
			m_result = result;
		}

		/// <summary>The compilation errors</summary>
		public IEnumerable<Diagnostic> Errors => m_result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error);

		/// <summary>The compilation warnings</summary>
		public IEnumerable<Diagnostic> Warnings => m_result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Warning);

		/// <summary>The compilation warnings</summary>
		public IEnumerable<Diagnostic> Info => m_result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Info);

		/// <summary>Returns a string describing all of the errors</summary>
		public string ErrorReport()
		{
			var sb = new StringBuilder();
			foreach (var err in Errors) sb.AppendLine($"{err.Location} - {err.Id}: {err.GetMessage()}");
			return sb.ToString();
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	//Disabled because pwsh can't find Microsoft.CodeAnalysis.CSharp
	//[TestFixture]
	public class TestCompile
	{
		[Test]
		public void Test0()
		{
			//var rylogic_dll = Path_.FileName(Assembly.GetExecutingAssembly().Location);

			var source = @"
			//Assembly: System.dll
			//Assembly: System.Runtime.dll
			//Assembly: System.Drawing.Primitives.dll

			using System;
			using System.Drawing;

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
			var r = inst.Invoke<System.Drawing.Point>("SayHello");
			Assert.Equal(r, new System.Drawing.Point(1, 2));
		}
	}
}
#endif
#endif