using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using pr.common;
using pr.extn;

namespace Rylogic_Log_Viewer
{
	public class Pattern
	{
		/// <summary>The pattern to use when matching</summary>
		public string Expr { get; set; }
		
		/// <summary>True if the highlight pattern is active</summary>
		public bool Active { get; set; }
		
		/// <summary>True if the pattern is a regular expression, false if it's just a substring</summary>
		public bool IsRegex { get; set; }
		
		/// <summary>True if the pattern should ignore case</summary>
		public bool IgnoreCase { get; set; }
		
		/// <summary>Invert the results of a match</summary>
		public bool Invert { get; set; }
		
		public Pattern()
		{
			Expr = "";
			Active = true;
			IsRegex = false;
			IgnoreCase = false;
			Invert = false;
		}
		
		/// <summary>Returns true if this pattern matches a substring in 'text'</summary>
		public bool IsMatch(string text)
		{
			if (!Active) return false;
			bool match;
			if (IsRegex)
			{
				RegexOptions opts = RegexOptions.None;
				if (IgnoreCase) opts |= RegexOptions.IgnoreCase;
				match = new Regex(Expr, opts).IsMatch(text);
			}
			else
			{
				match = IgnoreCase
					? text.ToLower().Contains(Expr.ToLower())
					: text.Contains(Expr);
			}
			return Invert ? !match : match;
		}
		
		/// <summary>Return the range of 'text' that matches this pattern</summary>
		public IEnumerable<Range> Match(string text)
		{
			if (Active && text != null)
			{
				List<long> x = new List<long>();
				if (Invert) x.Add(0);
				if (Expr.Length != 0) try
				{
					if (IsRegex)
					{
						RegexOptions opts = IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
						foreach (Match m in new Regex(Expr, opts).Matches(text))
						{
							x.Add(m.Index);
							x.Add(m.Index + m.Length);
						}
					}
					else
					{
						StringComparison cmp = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
						for (int i = text.IndexOf(Expr, 0, cmp); i != -1; i = text.IndexOf(Expr, i + Expr.Length, cmp))
						{
							x.Add(i);
							x.Add(i + Expr.Length);
						}
					}
				} catch {}
				if (Invert) x.Add(text.Length);
				for (int i = 0; i != x.Count; i += 2)
					yield return new Range(x[i], x[i+1]);
			}
		}
		
		/// <summary>Reads an xml description of the highlight expressions</summary>
		public static List<Pattern> Import(string highlights)
		{
			var list = new List<Pattern>();
			XDocument doc; try { doc = XDocument.Parse(highlights); } catch { return list; }
			foreach (XElement n in doc.Root.Elements("highlight"))
			{
				try
				{
					list.Add(new Pattern
					{
						// ReSharper disable PossibleNullReferenceException
						Expr       = n.Element("expr").Value,
						Active     = bool.Parse(n.Element("active").Value),
						IsRegex    = bool.Parse(n.Element("isregex").Value),
						IgnoreCase = bool.Parse(n.Element("ignorecase").Value),
						Invert     = bool.Parse(n.Element("invert").Value),
						// ReSharper restore PossibleNullReferenceException
					});
				}
				catch {} // swallow bad input data
			}
			return list;
		}
		
		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(IList<Pattern> highlights)
		{
			XDocument doc = new XDocument();
			var root = doc.Add2(new XElement("root"));
			foreach (var hl in highlights)
			{
				var pattern = root.Add2(new XElement("pattern"));
				pattern.Add2("expr"       ,hl.Expr       );
				pattern.Add2("active"     ,hl.Active     );
				pattern.Add2("isregex"    ,hl.IsRegex    );
				pattern.Add2("ignorecase" ,hl.IgnoreCase );
				pattern.Add2("invert"     ,hl.Invert     );
			}
			return doc.ToString(SaveOptions.None);
		}
	}
}
