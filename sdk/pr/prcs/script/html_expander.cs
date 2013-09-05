using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using pr.common;
using pr.extn;

namespace pr.script
{
	/// <summary>Methods for expanding templates</summary>
	public static class Expand
	{
		/// <summary>Expands template xml files</summary>
		public static string Xml(Src src, string current_dir) { return Markup(src,current_dir); }

		/// <summary>Expands template html files</summary>
		public static string Html(Src src, string current_dir) { return Markup(src,current_dir); }

		/// <summary>Expands template mark-up files</summary>
		public static string Markup(Src src, string current_dir)
		{
			// Supported substitutions:
			// <!--#include file="filepath"-->
			//   Substitutes the content of the file 'filepath' at the declared location.
			//   'filepath' can be relative to directory of the template file, or a fullpath
			// <!--#var name="count" file="filepath" value="const int Count = (?<value>\d+);"-->
			//   Declares a variable for later use. Applies a regex match to the contents of 'filepath' to get a definition for 'value'
			//   'filepath' can be relative to directory of the template file, or a fullpath
			// <!--#value name="count"-->
			//   Substitute a previously defined variable called 'count'

			var variables = new Dictionary<string,string>();

			// General form: [optional leading whitespace]<!--#command key="value" key="value"... -->
			// The pattern matches leading white space which is then inserted before every line in the substituted result.
			// This means includes on lines of the own are correctly tabbed, and <!--inline--> substitutions are also correct
			var result = TemplateReplacer.Process(src, @"(?<indent>[ \t]*)<!--#(?<cmd>\w+)\s+(?<kv>.*?)-->", (tr, match) =>
				{
					var indent = match.Result("${indent}");
					var cmd    = match.Result("${cmd}");
					var kv     = match.Result("${kv}");  // list of key="value" pairs
					Match m;
					switch (cmd)
					{
					default:
						{
							throw new Exception("Unknown template command: '{0}' ({1})".Fmt(cmd, match.ToString()));
						}
					case "include":
						#region include
						{
							const string expected_form = "<!--#include file=\"filepath\"-->";

							// Read the include filepath and check it exists
							m = Regex.Match(kv, @".*file=""(?<file>.*?)"".*");
							if (!m.Success) throw new Exception("Invalid include: '{0}'.\nCould not match 'file' field.\nExpected form: {1}".Fmt(match.ToString(), expected_form));
							var filepath = m.Result("${file}");
							filepath = Path.IsPathRooted(filepath) ? filepath : Path.Combine(current_dir, filepath);
							if (!PathEx.FileExists(filepath)) throw new FileNotFoundException("File reference not found: {0}".Fmt(filepath), filepath);

							tr.PushSource(new IndentSrc(new FileSrc(filepath), indent, true));
							return string.Empty;
						}
						#endregion
					case "var":
						#region var
						{
							const string expected_form = "<!--#var name=\"variable_name\" file=\"filepath\" value=\"regex_pattern_defining_value\"-->";

							// Read the name of the variable
							m = Regex.Match(kv, @".*name=""(?<name>\w+)"".*");
							if (!m.Success) throw new Exception("Invalid variable declaration: '{0}'.\nCould not match 'name' field.\nExpected form: {1}".Fmt(match.ToString(), expected_form));
							var name = m.Result("${name}");

							// Read the source filepath
							m = Regex.Match(kv, @".*file=""(?<file>.*?)"".*");
							if (!m.Success) throw new Exception("Invalid variable declaration: '{0}'.\nCould not match 'file' field.\nExpected form: {1}".Fmt(match.ToString(), expected_form));
							var filepath = m.Result("${file}");
							filepath = Path.IsPathRooted(filepath) ? filepath : Path.Combine(current_dir, filepath);
							if (!PathEx.FileExists(filepath)) throw new FileNotFoundException("File reference not found: {0}".Fmt(filepath), filepath);

							// Read the regex pattern that defines 'value'
							m = Regex.Match(kv, @".*value=""(?<pattern>.*?)"".*");
							if (!m.Success) throw new Exception("Invalid variable declaration: '{0}'.\nCould not match 'value' field.\nExpected form: {1}".Fmt(match.ToString(), expected_form));
							var pat = m.Result("${pattern}");
							Regex pattern;
							try { pattern = new Regex(pat, RegexOptions.Compiled); }
							catch (Exception ex) { throw new Exception("Invalid variable declaration: '{0}'.\n'value' field is not a valid Regex expression.\nExpected form: {1}".Fmt(match.ToString(), expected_form), ex); }

							// Use the pattern to get the value for the variable
							using (var sr = new StreamReader(filepath, Encoding.UTF8, true, 65536))
							for (string line; (line = sr.ReadLine()) != null && !(m = pattern.Match(line)).Success;) {}
							if (!m.Success) throw new Exception("Invalid variable declaration: '{0}'.\n'value' regex expression did not find a match in {2}.\nExpected form: {1}".Fmt(match.ToString(), expected_form, filepath));
							var value = m.Result("${value}");

							// Save the name/value pair
							variables[name] = value;
							return string.Empty;
						}
						#endregion
					case "value":
						#region value
						{
							const string expected_form = "<!--#value name=\"variable_name\"-->";

							// Read the name of the variable
							m = Regex.Match(kv, @".*name=""(?<name>\w+)"".*");
							if (!m.Success) throw new Exception("Invalid value declaration: '{0}'.\nCould not match 'name' field.\nExpected form: {1}".Fmt(match.ToString(), expected_form));
							var name = m.Result("${name}");

							// Lookup the value for the variable
							string value;
							if (!variables.TryGetValue(name, out value)) throw new Exception("Invalid value declaration: '{0}'.\nVariable with 'name' {2} is not defined.\nExpected form: {1}".Fmt(match.ToString(), expected_form, name));
							return indent + value;
						}
						#endregion
					}
				});
			return result;
		}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using script;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestExpand
		{
			private const string IncludeFile = "TestExpandHtml_include.txt";
			[SetUp] public static void Setup()
			{
				File.WriteAllText(IncludeFile, "include file\r\ntext data");
			}
			[TearDown] public static void TearDown()
			{
				File.Delete(IncludeFile);
			}
			[Test] public static void TestExpandHtml()
			{
				var template =
					"<root>\r\n" +
					"\t\t <!--#include file=\"{0}\"-->\r\n".Fmt(IncludeFile) +
					"<!--#var name=\"MyVar\" file=\"{0}\" value=\"text\\s+(?<value>\\w+)\"-->\r\n".Fmt(IncludeFile) +
					"  <!--#value name=\"MyVar\"-->\r\n"+
					"    <!--#value name=\"MyVar\"-->\r\n"+
					"</root>\r\n";
				const string result =
					"<root>\r\n" +
					"\t\t include file\r\n" +
					"\t\t text data\r\n" +
					"\r\n" +
					"  data\r\n" +
					"    data\r\n" +
					"</root>\r\n";
				var r = Expand.Html(new StringSrc(template), Environment.CurrentDirectory);
				Assert.AreEqual(result, r);
			}
		}
	}
}

#endif
