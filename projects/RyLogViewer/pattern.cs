using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using pr.common;

namespace RyLogViewer
{
	public class Pattern :ICloneable
	{
		private string m_expr;
		private bool m_active;
		private bool m_is_regex;
		private bool m_ignore_case;
		private bool m_invert;
		private bool m_binary_match;
		private Regex m_compiled_regex;
		
		/// <summary>The pattern to use when matching</summary>
		public string Expr
		{
			get { return m_expr; }
			set { m_expr = value; m_compiled_regex = null; }
		}

		/// <summary>True if the highlight pattern is active</summary>
		public bool Active
		{
			get { return m_active; }
			set { m_active = value; }
		}

		/// <summary>True if the pattern is a regular expression, false if it's just a substring</summary>
		public bool IsRegex
		{
			get { return m_is_regex; }
			set { m_is_regex = value; m_compiled_regex = null; }
		}

		
		/// <summary>True if the pattern should ignore case</summary>
		public bool IgnoreCase
		{
			get { return m_ignore_case; }
			set { m_ignore_case = value; m_compiled_regex = null; }
		}

		/// <summary>Invert the results of a match</summary>
		public bool Invert
		{
			get { return m_invert; }
			set { m_invert = value; }
		}

		/// <summary>True if a match anywhere on the row is considered a match for the full row</summary>
		public bool BinaryMatch
		{
			get { return m_binary_match; }
			set { m_binary_match = value; }
		}

		public Pattern()
		{
			Expr = "";
			Active = true;
			IsRegex = false;
			IgnoreCase = false;
			Invert = false;
			BinaryMatch = true;
		}
		public Pattern(Pattern rhs)
		{
			Expr        = rhs.Expr;
			Active      = rhs.Active;
			IsRegex     = rhs.IsRegex;
			IgnoreCase  = rhs.IgnoreCase;
			Invert      = rhs.Invert;
			BinaryMatch = rhs.BinaryMatch;
		}
		
		private Regex Regex
		{
			get
			{
				RegexOptions opts = (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled;
				return m_compiled_regex ?? (m_compiled_regex = new Regex(Expr, opts));
			}
		}
		
		/// <summary>Return true if the contained expression is valid</summary>
		public bool ExprValid
		{
			// ReSharper disable ConditionIsAlwaysTrueOrFalse
			get { try { return !IsRegex || new Regex(Expr) != null; } catch { return false; } }
			// ReSharper restore ConditionIsAlwaysTrueOrFalse
		}

		/// <summary>Returns true if this pattern matches a substring in 'text'</summary>
		public bool IsMatch(string text)
		{
			if (!Active) return false;
			bool match = false;
			if (IsRegex)
			{
				try { match = Regex.IsMatch(text); } catch (ArgumentException) {}
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
						foreach (Match m in Regex.Matches(text))
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
			// ReSharper disable PossibleNullReferenceException
			Expr        =            node.Element(XmlTag.Expr      ).Value;
			Active      = bool.Parse(node.Element(XmlTag.Active    ).Value);
			IsRegex     = bool.Parse(node.Element(XmlTag.IsRegex   ).Value);
			IgnoreCase  = bool.Parse(node.Element(XmlTag.IgnoreCase).Value);
			Invert      = bool.Parse(node.Element(XmlTag.Invert    ).Value);
			BinaryMatch = bool.Parse(node.Element(XmlTag.Binary    ).Value);
			// ReSharper restore PossibleNullReferenceException
		}
		
		/// <summary>Export this pattern as xml</summary>
		public virtual XElement ToXml(XElement node)
		{
			node.Add
			(
				new XElement(XmlTag.Expr       ,Expr       ),
				new XElement(XmlTag.Active     ,Active     ),
				new XElement(XmlTag.IsRegex    ,IsRegex    ),
				new XElement(XmlTag.IgnoreCase ,IgnoreCase ),
				new XElement(XmlTag.Invert     ,Invert     ),
				new XElement(XmlTag.Binary     ,BinaryMatch)
			);
			return node;
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public virtual object Clone()
		{
			return new Pattern(this);
		}

		/// <summary>Value equality test</summary>
		public override bool Equals(object obj)
		{
			Pattern rhs = obj as Pattern;
			return
				rhs != null &&
				Expr       .Equals(rhs.Expr       ) &&
				Active     .Equals(rhs.Active     ) &&
				IsRegex    .Equals(rhs.IsRegex    ) &&
				IgnoreCase .Equals(rhs.IgnoreCase ) &&
				Invert     .Equals(rhs.Invert     ) &&
				BinaryMatch.Equals(rhs.BinaryMatch);
		}

		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			return
				Expr       .GetHashCode()^
				Active     .GetHashCode()^
				IsRegex    .GetHashCode()^
				IgnoreCase .GetHashCode()^
				Invert     .GetHashCode()^
				BinaryMatch.GetHashCode();
		}
	}
}
