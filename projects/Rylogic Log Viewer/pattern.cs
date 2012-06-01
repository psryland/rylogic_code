using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using pr.common;

namespace Rylogic_Log_Viewer
{
	public class Pattern :ICloneable
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
			bool match = false;
			if (IsRegex)
			{
				RegexOptions opts = IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
				try { match = new Regex(Expr, opts).IsMatch(text); } catch (ArgumentException) {}
			}
			else
			{
				StringComparison cmp = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				match = text.IndexOf(Expr, 0, cmp) != -1;
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
				} catch (ArgumentException) {}
				if (Invert) x.Add(text.Length);
				for (int i = 0; i != x.Count; i += 2)
					yield return new Range(x[i], x[i+1]);
			}
		}
		
		/// <summary>Construct from xml description</summary>
		public Pattern(XElement node)
		{
			try
			{
				// ReSharper disable PossibleNullReferenceException
				Expr       = node.Element("expr").Value;
				Active     = bool.Parse(node.Element("active").Value);
				IsRegex    = bool.Parse(node.Element("isregex").Value);
				IgnoreCase = bool.Parse(node.Element("ignorecase").Value);
				Invert     = bool.Parse(node.Element("invert").Value);
				// ReSharper restore PossibleNullReferenceException
			} catch {} // swallow bad input data
		}
		
		/// <summary>Export this pattern as xml</summary>
		public virtual XElement ToXml(XElement node)
		{
			node.Add
			(
				new XElement("expr"       ,Expr       ),
				new XElement("active"     ,Active     ),
				new XElement("isregex"    ,IsRegex    ),
				new XElement("ignorecase" ,IgnoreCase ),
				new XElement("invert"     ,Invert     )
			);
			return node;
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
