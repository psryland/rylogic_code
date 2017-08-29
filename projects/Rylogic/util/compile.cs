using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using pr.common;
using pr.extn;

namespace pr.util
{
	public class RuntimeAssembly
	{
		/// <summary>
		/// Create a runtime assembly from a source file.
		/// Use comments such as:<para/>
		///  //Assembly: System.dll<para/>
		/// to declare required assembly references </summary>
		public static RuntimeAssembly FromFile(string inst_name, string file)
		{
			if (!Path_.FileExists(file))
				throw new FileNotFoundException("File {0} not found".Fmt(file));

			return FromString(inst_name, File.ReadAllText(file));
		}

		/// <summary>
		/// Create a runtime assembly from a source string
		/// Use comments such as:<para/>
		///  //Assembly: System.dll<para/>
		/// to declare required assembly references.
		/// 'inst_name' is the full name (namespace.classname) of the class in the code to instantiate</summary>
		public static RuntimeAssembly FromString(string inst_name, string source)
		{
			// Scan the file for Reference Assembles
			var assemblies = new List<string>();
			foreach (var line in source.Lines().Select(x => (string)x.Trim(' ','\t','\r','\n')))
			{
				if (!line.HasValue()) continue;
				if (!line.StartsWith("//")) break;
				if (!line.StartsWith("//Assembly:")) continue;
				var ass = line.Substring(11).Trim(' ','\t','\r','\n');
				if (ass.HasValue()) assemblies.Add(ass);
			}

			return new RuntimeAssembly(inst_name, source, assemblies.ToArray());
		}

		/// <summary>A cache of method info objects from 'Instance'</summary>
		private Cache<string, MethodInfo> m_mi_cache;

		/// <summary>Compile source into an in-memory assembly</summary>
		private RuntimeAssembly(string inst_name, string source, string[] assemblies)
		{
			m_mi_cache = new Cache<string,MethodInfo>{ ThreadSafe = true };

			var provider = new CSharpCodeProvider(new Dictionary<string, string>{{"CompilerVersion", "v4.0"}});
			var compilerParams = new CompilerParameters(assemblies)
			{
				GenerateInMemory   = true,
				GenerateExecutable = false,
			};

			Results = provider.CompileAssemblyFromSource(compilerParams, source);
			if (Results.Errors.Count != 0)
				throw new CompileException(Results.Errors);

			if (inst_name.HasValue())
			{
				if (!Results.CompiledAssembly.ExportedTypes.Any(x => x.FullName == inst_name))
					throw new Exception($"Runtime assembly does not export a type called ${inst_name}");

				Instance = Results.CompiledAssembly.CreateInstance(inst_name);
			}
		}

		/// <summary>The main instance</summary>
		private object Instance { get; set; }

		/// <summary>Compilation results</summary>
		public CompilerResults Results { get; private set; }

		/// <summary>Execute a method on 'Instance'</summary>
		public TResult Invoke<TResult>(string function, params object[] args)
		{
			if (Instance == null) throw new Exception($"Invoke requires an instance");
			var mi = m_mi_cache.Get(function, k => Instance.GetType().GetMethod(k));
			return (TResult)mi.Invoke(Instance, args);
		}
		public void Invoke(string function, params object[] args)
		{
			if (Instance == null) throw new Exception($"Invoke requires an instance");
			var mi = m_mi_cache.Get(function, k => Instance.GetType().GetMethod(k));
			mi.Invoke(Instance, args);
		}
	}

	/// <summary>Exception type for errors in runtime assembly code</summary>
	public class CompileException :Exception
	{
		public CompileException(CompilerErrorCollection errors)
		{
			Errors = errors;
		}

		/// <summary>The compilation errors</summary>
		public CompilerErrorCollection Errors { get; set; }

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
namespace pr.unittests
{
	using util;

	[TestFixture] public class TestCompile
	{
		[Test] public void Test0()
		{
			var rylogic_dll = Path_.FileName(Assembly.GetExecutingAssembly().Location);

			var source = @"
			//Assembly: System.dll
			//Assembly: System.Drawing.dll
			//Assembly: System.Windows.Forms.dll

			using System;
			using System.Drawing;
			using System.Windows.Forms;

			namespace Foo
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

			var ass = RuntimeAssembly.FromString("Foo.Main", source);
			var r = ass.Invoke<System.Drawing.Point>("SayHello");
			Assert.AreEqual(r, new System.Drawing.Point(1,2));
		}
	}
}
#endif
