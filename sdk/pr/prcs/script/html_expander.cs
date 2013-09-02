using System;
using System.IO;
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
			// General form: <!--#command key="value" key="value"... -->
			// <!--#include file=".\help_index_panel.html"-->
			// <!--#value file="..\src\misc.cs" field="const int MaxHighlights\s+=\s+(?<field>\d+);"-->
			var result = TemplateReplacer.Process(src, @"<!--#(?<cmd>\w+)\s+(?<kv>.*?)-->", (tr, match) =>
				{
					var cmd = match.Result("${cmd}");
					var kv  = match.Result("${kv}");  // list of key="value" pairs
					switch (cmd)
					{
					default:
						{
							throw new Exception("Unknown template command: '{0}' ({1})".Fmt(cmd, match.ToString()));
						}
					case "include":
						{
							const string expected_form = "<!--#include file=\"filepath\" indent=\"1\"-->";

							// Read the include filepath and check it exists
							var m = Regex.Match(kv, @".*file=""(?<file>.*?)"".*");
							if (!m.Success) throw new Exception("Invalid include: '{0}'. Expected form: {1}".Fmt(match.ToString(), expected_form));
							var file = m.Result("${file}");
							var filepath = Path.Combine(current_dir, file);
							if (!PathEx.FileExists(filepath)) throw new FileNotFoundException("Failed to include file", filepath);

							// Look for an optional 'indent="N"' kv pair
							m = Regex.Match(kv, @".*indent=""(?<indent>\d+?)"".*");
							if (m.Success)
							{
								var indent = int.Parse(m.Result("${indent}"));
								tr.PushSource(new IndentSrc(new FileSrc(filepath), "\t".Repeat(indent), false));
								return string.Empty;
							}

							// Otherwise, just include the file directly
							tr.PushSource(new FileSrc(filepath));
							return string.Empty;
						}
					case "value":
						{
							const string expected_form = "<!--#value file=\"filepath\" field=\"regex_pattern\"-->";

							// Read the include filepath and check it exists
							var m = Regex.Match(kv, @".*file=""(?<file>.*?)"".*");
							if (!m.Success) throw new Exception("Invalid value: '{0}'. Expected form: {1}".Fmt(match.ToString(), expected_form));
							var file = m.Result("${file}");
							var filepath = Path.Combine(current_dir, file);
							if (!PathEx.FileExists(filepath)) throw new FileNotFoundException("Failed to open file", filepath);

							// Read the pattern to apply to extract 'field'
							m = Regex.Match(kv, @".*field=""(?<pattern>.*?)"".*");
							if (!m.Success) throw new Exception("Invalid value: '{0}'. Expected form: {1}".Fmt(match.ToString(), expected_form));
							var pat = m.Result("${pattern}");

							// Read the file into memory, and search it using the given pattern
							m = Regex.Match(File.ReadAllText(filepath), pat);
							if (!m.Success) throw new Exception("Pattern {0} did not match anything content within file {1}".Fmt(pat, filepath));
							var field = m.Result("${field}");
							return field;
						}
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
				File.WriteAllText(IncludeFile, "include file\r\n text data");
			}
			[TearDown] public static void TearDown()
			{
				File.Delete(IncludeFile);
			}
			[Test] public static void TestExpandHtml()
			{
				var template =
					"<root>" +
					"<!--#include file=\"{0}\" indent=\"2\"-->".Fmt(IncludeFile) +
					"<!--#value file=\"{0}\" field=\"text\\s+(?<field>\\w+)\"-->".Fmt(IncludeFile) +
					"</root>";
				const string result =
					"<root>" +
					"include file\r\n\t\t text data" +
					"data" +
					"</root>";
				var r = Expand.Html(new StringSrc(template), Environment.CurrentDirectory);
				Assert.AreEqual(result, r);
			}
		}
	}
}

#endif
