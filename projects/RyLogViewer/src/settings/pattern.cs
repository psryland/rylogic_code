using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using pr.common;
using pr.util;

namespace RyLogViewer
{
	public class Pattern :IPattern
	{
		private EPattern m_patn_type;
		private string   m_expr;
		private bool     m_ignore_case;
		private bool     m_active;
		private bool     m_invert;
		private bool     m_binary_match;
		private Regex    m_compiled_patn;

		/// <summary>True if the pattern is a regular expression, false if it's just a substring</summary>
		public EPattern PatnType
		{
			get { return m_patn_type; }
			set { m_patn_type = value; m_compiled_patn = null; RaisePatternChanged(); }
		}

		/// <summary>The pattern to use when matching</summary>
		public string Expr
		{
			get { return m_expr; }
			set { m_expr = value; m_compiled_patn = null; RaisePatternChanged(); }
		}

		/// <summary>True if the pattern should ignore case</summary>
		public bool IgnoreCase
		{
			get { return m_ignore_case; }
			set { m_ignore_case = value; m_compiled_patn = null; RaisePatternChanged(); }
		}

		/// <summary>True if the pattern is active</summary>
		public bool Active
		{
			get { return m_active; }
			set { m_active = value; RaisePatternChanged(); }
		}

		/// <summary>Invert the results of a match</summary>
		public bool Invert
		{
			get { return m_invert; }
			set { m_invert = value; RaisePatternChanged(); }
		}

		/// <summary>True if a match anywhere on the row is considered a match for the full row</summary>
		public bool BinaryMatch
		{
			get { return m_binary_match; }
			set { m_binary_match = value; RaisePatternChanged(); }
		}

		/// <summary>Raised whenever data on this pattern changes</summary>
		public event EventHandler PatternChanged;
		private void RaisePatternChanged()
		{
			if (PatternChanged == null) return;
			PatternChanged(this, EventArgs.Empty);
		}

		public Pattern() :this(EPattern.Substring, "")
		{}
		public Pattern(EPattern patn_type, string expr)
		{
			PatnType    = patn_type;
			Expr        = expr;
			IgnoreCase  = false;
			Active      = true;
			Invert      = false;
			BinaryMatch = true;
		}
		public Pattern(Pattern rhs)
		{
			Expr        = rhs.Expr;
			Active      = rhs.Active;
			PatnType    = rhs.PatnType;
			IgnoreCase  = rhs.IgnoreCase;
			Invert      = rhs.Invert;
			BinaryMatch = rhs.BinaryMatch;
		}
		
		/// <summary>Converts the current search pattern to a string that can be used as a regex</summary>
		public string RegexString
		{
			get
			{
				switch (PatnType)
				{
				default: throw new ArgumentOutOfRangeException();
				case EPattern.Substring:         return Regex.Escape(Expr);
				case EPattern.Wildcard:          return Regex.Escape(Expr).Replace("\\*", "(.*)").Replace("\\?", ".");
				case EPattern.RegularExpression: return Expr;
				}
			}
		}

		/// <summary>Converts this pattern into a compiled regex pattern</summary>
		private Regex Regex
		{
			get
			{
				// Don't make this public because it throws if the regex string isn't valid
				RegexOptions opts = (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled;
				return m_compiled_patn ?? (m_compiled_patn = new Regex(RegexString, opts));
			}
		}

		/// <summary>Return true if the contained expression is valid</summary>
		public bool IsValid
		{
			get
			{
				try
				{
					switch (PatnType)
					{
					default: throw new ArgumentException("Unknown pattern type");
					case EPattern.Substring: return true;
					case EPattern.Wildcard:
					case EPattern.RegularExpression: return Regex != null;
					}
				} catch { return false; }
			}
		}

		/// <summary>Returns true if this pattern matches a substring in 'text'</summary>
		public bool IsMatch(string text)
		{
			if (!Active) return false;
			bool match = false;
			switch (PatnType)
			{
			case EPattern.Substring:
				{
					StringComparison cmp = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
					match = Expr.Length != 0 && text.IndexOf(Expr, 0, cmp) != -1;
					break;
				}
			case EPattern.Wildcard:
			case EPattern.RegularExpression:
				{
					try { match = Expr.Length != 0 && Regex.IsMatch(text); } catch (ArgumentException) {}
					break;
				}
			}
			
			return Invert ? !match : match;
		}
		
		/// <summary>Return the range of 'text' that matches this pattern</summary>
		public IEnumerable<Span> Match(string text)
		{
			if (!Active || text == null) yield break;
			
			var x = new List<int>();
			if (Invert) x.Add(0);
			try
			{
				switch (PatnType)
				{
				case EPattern.Substring:
					{
						StringComparison cmp = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
						if (Expr.Length != 0)
						{
							for (int i = text.IndexOf(Expr, 0, cmp); i != -1; i = text.IndexOf(Expr, i + Expr.Length, cmp))
							{
								x.Add(i);
								x.Add(i + Expr.Length);
							}
						}
						break;
					}
				case EPattern.Wildcard:
				case EPattern.RegularExpression:
					{
						foreach (Match m in Regex.Matches(text))
						{
							if (m.Length == 0) continue;
							x.Add(m.Index);
							x.Add(m.Index + m.Length);
						}
						break;
					}
				}
			} catch (ArgumentException) {}
			if (Invert) x.Add(text.Length);
			for (int i = 0; i != x.Count; i += 2)
				yield return new Span(x[i], x[i+1] - x[i]);
		}

		/// <summary>Returns the names of the capture groups in this pattern</summary>
		public string[] CaptureGroupNames
		{
			get
			{
				if (!IsValid) return new string[0];
				if (PatnType == EPattern.RegularExpression || PatnType == EPattern.Wildcard)
					return Regex.GetGroupNames();
				return new string[0];
			}
		}

		/// <summary>Returns the capture groups captured when applying this pattern to 'text'</summary>
		public IEnumerable<KeyValuePair<string, string>> CaptureGroups(string text)
		{
			if (!IsMatch(text) || !IsValid) yield break;
			if (PatnType != EPattern.RegularExpression)
			{
				int i = 0;
				foreach (var s in Match(text))
					yield return new KeyValuePair<string, string>((++i).ToString(CultureInfo.InvariantCulture), text.Substring(s.Index,s.Count));
			}
			else
			{
				Match match = Regex.Match(text);
				var names = Regex.GetGroupNames();
				var grps  = match.Groups;
				for (int i = 1; i < grps.Count; ++i)
					yield return new KeyValuePair<string, string>(names[i], grps[i].Value);
			}
		}

		/// <summary>Construct from xml description</summary>
		public Pattern(XElement node)
		{
			// ReSharper disable PossibleNullReferenceException
			Expr        = node.Element(XmlTag.Expr      ).Value;
			PatnType    = Enum<EPattern>.Parse(node.Element(XmlTag.PatnType).Value);
			Active      = bool.Parse(node.Element(XmlTag.Active    ).Value);
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
				new XElement(XmlTag.PatnType   ,PatnType   ),
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
			var rhs = obj as Pattern;
			return rhs != null
				&& rhs.m_patn_type     == m_patn_type
				&& rhs.m_expr          == m_expr
				&& rhs.m_ignore_case   == m_ignore_case
				&& rhs.m_active        == m_active
				&& rhs.m_invert        == m_invert
				&& rhs.m_binary_match  == m_binary_match;
		}

		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			return
				Expr       .GetHashCode()^
				Active     .GetHashCode()^
				PatnType   .GetHashCode()^
				IgnoreCase .GetHashCode()^
				Invert     .GetHashCode()^
				BinaryMatch.GetHashCode();
		}

		/// <summary>Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.</summary>
		public override string ToString()
		{
			return Expr;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal static partial class RyLogViewerUnitTests
	{
		internal static class TestPattern
		{
			[TestFixtureSetUp] public static void Setup()
			{
			}
			[TestFixtureTearDown] public static void CleanUp()
			{
			}
			[Test] public static void TestMatches()
			{
			}
		}
	}
}
#endif
