using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Rylogic.Script
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
			//   'filepath' can be relative to directory of the template file, or a full path
			// <!--#var name="count" file="filepath" value="const int Count = (?<value>\d+);"-->
			//   Declares a variable for later use. Applies a regex match to the contents of 'filepath' to get a definition for 'value'
			//   'filepath' can be relative to directory of the template file, or a full path
			// <!--#value name="count"-->
			//   Substitute a previously defined variable called 'count'
			// <!--#image file="filepath"-->
			//   Substitute a base64 image file 'filepath'

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
							throw new Exception(string.Format("Unknown template command: '{0}' ({1})", cmd, match.ToString()));
						}
					case "include":
						#region include
						{
							const string expected_form = "<!--#include file=\"filepath\"-->";

							// Read the include filepath and check it exists
							m = Regex.Match(kv, @".*file=""(?<file>.*?)"".*");
							if (!m.Success) throw new Exception(string.Format("Invalid include: '{0}'.\nCould not match 'file' field.\nExpected form: {1}",match.ToString(), expected_form));
							var fpath = m.Result("${file}");

							fpath = Path.IsPathRooted(fpath) ? fpath : Path.Combine(current_dir, fpath);
							if (!File.Exists(fpath)) throw new FileNotFoundException(string.Format("File reference not found: {0}", fpath), fpath);

							tr.PushSource(new IndentSrc(new FileSrc(fpath), indent, true));
							return string.Empty;
						}
						#endregion
					case "var":
						#region var
						{
							const string expected_form = "<!--#var name=\"variable_name\" file=\"filepath\" value=\"regex_pattern_defining_value\"-->";

							// Read the name of the variable
							m = Regex.Match(kv, @".*name=""(?<name>\w+)"".*");
							if (!m.Success) throw new Exception(string.Format("Invalid variable declaration: '{0}'.\nCould not match 'name' field.\nExpected form: {1}", match.ToString(), expected_form));
							var name = m.Result("${name}");

							// Read the source filepath
							m = Regex.Match(kv, @".*file=""(?<file>.*?)"".*");
							if (!m.Success) throw new Exception(string.Format("Invalid variable declaration: '{0}'.\nCould not match 'file' field.\nExpected form: {1}", match.ToString(), expected_form));
							var filepath = m.Result("${file}");
							filepath = Path.IsPathRooted(filepath) ? filepath : Path.Combine(current_dir, filepath);
							if (!File.Exists(filepath)) throw new FileNotFoundException(string.Format("File reference not found: {0}", filepath), filepath);

							// Read the regex pattern that defines 'value'
							m = Regex.Match(kv, @".*value=""(?<pattern>.*?)"".*");
							if (!m.Success) throw new Exception(string.Format("Invalid variable declaration: '{0}'.\nCould not match 'value' field.\nExpected form: {1}", match.ToString(), expected_form));
							var pat = m.Result("${pattern}");
							Regex pattern;
							try { pattern = new Regex(pat, RegexOptions.Compiled); }
							catch (Exception ex) { throw new Exception(string.Format("Invalid variable declaration: '{0}'.\n'value' field is not a valid Regex expression.\nExpected form: {1}", match.ToString(), expected_form), ex); }

							// Use the pattern to get the value for the variable
							using (var sr = new StreamReader(filepath, Encoding.UTF8, true, 65536))
							for (string line; (line = sr.ReadLine()) != null && !(m = pattern.Match(line)).Success;) {}
							if (!m.Success) throw new Exception(string.Format("Invalid variable declaration: '{0}'.\n'value' regex expression did not find a match in {2}.\nExpected form: {1}", match.ToString(), expected_form, filepath));
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
							if (!m.Success) throw new Exception(string.Format("Invalid value declaration: '{0}'.\nCould not match 'name' field.\nExpected form: {1}", match.ToString(), expected_form));
							var name = m.Result("${name}");

							// Lookup the value for the variable
							string value;
							if (!variables.TryGetValue(name, out value)) throw new Exception(string.Format("Invalid value declaration: '{0}'.\nVariable with 'name' {2} is not defined.\nExpected form: {1}", match.ToString(), expected_form, name));
							return indent + value;
						}
						#endregion
					case "image":
						#region image
						{
							const string expected_form = "<!--#image file=\"filepath\"-->";

							// Read the filename
							m = Regex.Match(kv, @".*file=""(?<file>.*?)"".*");
							if (!m.Success) throw new Exception(string.Format("Invalid image declaration: '{0}'.\nCould not match 'file' field.\nExpected form: {1}", match.ToString(), expected_form));
							var fpath = m.Result("${file}");

							fpath = Path.IsPathRooted(fpath) ? fpath : Path.Combine(current_dir, fpath);
							if (!File.Exists(fpath)) throw new FileNotFoundException(string.Format("File reference not found: {0}", fpath), fpath);

							// Determine the image type
							var extn = (Path.GetExtension(fpath) ?? string.Empty).ToLowerInvariant();
							if (string.IsNullOrEmpty(extn)) throw new Exception(string.Format("Could not determine image file format from path '{0}'", fpath));
							
							// Read the image file
							var img = File.ReadAllBytes(fpath);
							switch (extn)
							{
							default: throw new Exception(string.Format("Unsupported image format: {0}", extn));
							case ".png": return string.Format("data:image/png;base64,{0}", Convert.ToBase64String(img));
							case ".jpg": return string.Format("data:image/jpg;base64,{0}", Convert.ToBase64String(img));
							}
							
						}
						#endregion
					}
				});
			return result;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;
	using Script;

	[TestFixture] public class TestHtmlExpander
	{
		private const string IncludeFile = "TestExpandHtml_include.txt";
		[SetUp] public void Setup()
		{
			File.WriteAllText(IncludeFile, "include file\r\ntext data");
		}
		[TearDown] public void TearDown()
		{
			File.Delete(IncludeFile);
		}
		[Test] public void TestExpandHtml()
		{
			var template =
				$"<root>\r\n" +
				$"\t\t <!--#include file=\"{IncludeFile}\"-->\r\n" +
				$"<!--#var name=\"MyVar\" file=\"{IncludeFile}\" value=\"text\\s+(?<value>\\w+)\"-->\r\n" +
				$"  <!--#value name=\"MyVar\"-->\r\n"+
				$"    <!--#value name=\"MyVar\"-->\r\n"+
				$"</root>\r\n";
			const string result =
				"<root>\r\n" +
				"\t\t include file\r\n" +
				"\t\t text data\r\n" +
				"\r\n" +
				"  data\r\n" +
				"    data\r\n" +
				"</root>\r\n";
			var r = Expand.Html(new StringSrc(template), Environment.CurrentDirectory);
			Assert.Equal(result, r);
		}
	}
}
#endif
