using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using pr.extn;

namespace RyLogViewer
{
	public static class SpecialTags
	{
		public const string FilePath  = "FilePath";
		public const string FileName  = "FileName";
		public const string FileTitle = "FileTitle";
		public const string FileDir   = "FileDir";
		public const string FileExtn  = "FileExtn";
		public const string FileRoot  = "FileRoot";
	}

	/// <summary>An action that occurs in response to a click on a row that matches a pattern</summary>
	public class ClkAction :Pattern
	{
		/// <summary>The program to launch when activated</summary>
		public string Executable { get; set; }

		/// <summary>Arguments passed to the program to launch</summary>
		public string Arguments { get; set; }

		/// <summary>The working directory of the launched program</summary>
		public string WorkingDirectory { get; set; }

		/// <summary>Return a string description of the action</summary>
		public string ActionString { get { return Executable + " " + Arguments; } }

		public ClkAction()
		{
			Executable       = "";
			Arguments        = "";
			WorkingDirectory = "";
		}
		public ClkAction(ClkAction rhs) :base(rhs)
		{
			Executable       = rhs.Executable;
			Arguments        = rhs.Arguments;
			WorkingDirectory = rhs.WorkingDirectory;
		}
		public ClkAction(XElement node) :base(node)
		{
			Executable       = node.Element(XmlTag.Executable).As<string>();
			Arguments        = node.Element(XmlTag.Arguments ).As<string>();
			WorkingDirectory = node.Element(XmlTag.WorkingDir).As<string>();
		}

		/// <summary>Export this ClkAction as xml</summary>
		public override XElement ToXml(XElement node)
		{
			base.ToXml(node);
			node.Add
			(
				Executable      .ToXml(XmlTag.Executable, false),
				Arguments       .ToXml(XmlTag.Arguments , false),
				WorkingDirectory.ToXml(XmlTag.WorkingDir, false)
			);
			return node;
		}

		/// <summary>The expanded command line</summary>
		public string CommandLine(string text, string filepath)
		{
			return Expand(Executable, text, filepath) + " " + Expand(Arguments, text, filepath);
		}

		/// <summary>Performs the click action using 'matched_text'. 'filepath' should be the full filepath containing the line 'matched_text'</summary>
		public void Execute(string text, string filepath)
		{
			// Create the process
			var info = new ProcessStartInfo
			{
				UseShellExecute        = false,
				FileName               = Expand(Executable, text, filepath),
				Arguments              = Expand(Arguments, text, filepath),
				WorkingDirectory       = Expand(WorkingDirectory, text, filepath),
			};
			Process.Start(info);
		}

		/// <summary>Returns the arguments expanded with the substitutions</summary>
		private string Expand(string str, string matched_text, string filepath)
		{
			// Environment variable substitutions
			var substs = new Dictionary<string, string>();
			foreach (var v in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
				substs.Add(v.Key.ToString(), v.Value.ToString());

			// Special tag substitutions
			substs.Add(SpecialTags.FilePath  , filepath ?? string.Empty);
			substs.Add(SpecialTags.FileName  , Path.GetFileName(filepath) ?? string.Empty);
			substs.Add(SpecialTags.FileTitle , Path.GetFileNameWithoutExtension(filepath) ?? string.Empty);
			substs.Add(SpecialTags.FileDir   , Path.GetDirectoryName(filepath) ?? string.Empty);
			substs.Add(SpecialTags.FileExtn  , Path.GetExtension(filepath) ?? string.Empty);
			substs.Add(SpecialTags.FileRoot  , Path.GetPathRoot(filepath) ?? string.Empty);

			// Capture groups substitutions
			var grps = CaptureGroups(matched_text);
			foreach (var c in grps)
				substs.Add(c.Key, c.Value.Trim());

			// Perform substitutions
			var sb = new StringBuilder(str.Length);
			for (int i = 0, iend = str.Length; i != iend;)
			{
				if (str[i] == '{')
				{
					if (++i == iend)    { sb.Append('{'); break; }
					if (str[i] == '{') { sb.Append('{'); ++i; continue; }

					int j;
					for (j = i; j != iend && str[j] != '}'; ++j) {}
					if (j == iend) { sb.Append(str.Substring(i)); break; }

					var key = str.Substring(i, j - i);
					string value;
					sb.Append(substs.TryGetValue(key, out value) ? value : key);
					i = j + 1;
					continue;
				}
				if (str[i] == '}')
				{
					if (++i == iend)    { sb.Append('}'); break; }
					if (str[i] == '}') { sb.Append('}'); ++i; continue; }
					continue;
				}
				sb.Append(str[i]);
				++i;
			}

			return sb.ToString();
		}

		/// <summary>Reads an xml description of the ClkAction expressions</summary>
		public static List<ClkAction> Import(string actions)
		{
			var list = new List<ClkAction>();

			XDocument doc;
			try { doc = XDocument.Parse(actions); } catch { return list; }
			if (doc.Root == null) return list;
			foreach (XElement n in doc.Root.Elements(XmlTag.ClkAction))
				try { list.Add(new ClkAction(n)); } catch {} // Ignore those that fail

			return list;
		}

		/// <summary>Serialise the ClkAction patterns to xml</summary>
		public static string Export(IEnumerable<ClkAction> ClkActions)
		{
			var doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) return "";

			foreach (var ac in ClkActions)
				doc.Root.Add(ac.ToXml(new XElement(XmlTag.ClkAction)));

			return doc.ToString(SaveOptions.None);
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public override object Clone()
		{
			return MemberwiseClone();
		}

		/// <summary>Value equality test</summary>
		public override bool Equals(object obj)
		{
			var rhs = obj as ClkAction;
			return rhs != null
				&& base.Equals(obj)
				&& Equals(Executable      , rhs.Executable      )
				&& Equals(Arguments       , rhs.Arguments       )
				&& Equals(WorkingDirectory, rhs.WorkingDirectory);
		}

		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			return
				base.GetHashCode()^
				Executable      .GetHashCode()^
				Arguments       .GetHashCode()^
				WorkingDirectory.GetHashCode();
		}
	}
}
