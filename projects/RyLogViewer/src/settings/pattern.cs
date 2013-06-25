using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using pr.common;
using pr.extn;
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

		public Pattern() :this(EPattern.Substring, string.Empty) {}
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

		/// <summary>Returns the match template as a compiled regular expression</summary>
		protected Regex Regex
		{
			get
			{
				if (m_compiled_patn != null)
					return m_compiled_patn;

				// Notes:
				//  If an expression can't be represented in substr,wildcard form, harden up and use a regex

				// Convert the match string into a regular expression string and
				// replace the capture group tags with regex capture groups
				string expr = Expr;

				// If the expression is a regex, expect regex capture group syntax
				// Otherwise, expect capture groups of the form: {tag}
				if (PatnType != EPattern.RegularExpression)
				{
					// Collapse all whitespace to a single space character
					expr = Regex.Replace(expr, @"\s+", " ");

					// Escape the regex special chars
					expr = Regex.Escape(expr);

					// Replace wildcards with Regex equivalents
					if (PatnType == EPattern.Wildcard)
						expr = expr.Replace(@"\*", @".*").Replace(@"\?", @".");

					// Replace the (now escaped) '{tag}' capture
					// groups with named regular expression capture groups
					expr = Regex.Replace(expr, @"\\{(\w+)}", @"(?<$1>.*)");

					// Replace all escaped whitespace with '\s+'
					expr = expr.Replace(@"\ ", @"\s+");

					// Allow expressions the end with whitespace to also match the eol char
					if (expr.EndsWith(@"\s+"))
					{
						expr = expr.Remove(expr.Length - 3, 3);
						expr = expr + @"(?:$|\s)";
					}
				}

				// Compile the expression
				RegexOptions opts = (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled;
				return m_compiled_patn = new Regex(expr, opts);
			}
		}

		/// <summary>Return the compiled regex string for reference</summary>
		public string RegexString
		{
			get
			{
				var ex = ValidateExpr();
				return ex == null ? Regex.ToString() : "Expression invalid - {0}".Fmt(ex.Message);
			}
		}

		/// <summary>Returns null if the match field is valid, otherwise an exception describing what's wrong</summary>
		public Exception ValidateExpr()
		{
			try
			{
				// Compiling the Regex will throw if there's something wrong with it. It will never be null
				if (Regex == null)
					return new ArgumentException("The regular expression is null");
				
				// No prob, bob!
				return null;
			}
			catch (Exception ex) { return ex; }
		}

		/// <summary>Returns true if the match expression is valid</summary>
		public virtual bool IsValid
		{
			get { return ValidateExpr() == null; }
		}

		/// <summary>Returns the names of the capture groups in this pattern</summary>
		public string[] CaptureGroupNames
		{
			get
			{
				try { return Regex.GetGroupNames(); }
				catch { return new string[0]; }
			}
		}

		/// <summary>Returns the capture groups captured when applying this pattern to 'text'</summary>
		public IEnumerable<KeyValuePair<string, string>> CaptureGroups(string text)
		{
			if (!IsMatch(text) || !IsValid) yield break;
			Match match = Regex.Match(text);
			var names = Regex.GetGroupNames();
			var grps = match.Groups;
			for (int i = 0; i != grps.Count; ++i)
				yield return new KeyValuePair<string, string>(names[i], grps[i].Value);
		}

		/// <summary>Returns true if this pattern matches a substring in 'text'</summary>
		public bool IsMatch(string text)
		{
			if (!Active || !IsValid) return false;
			bool match = Expr.Length != 0 && Regex.IsMatch(text);
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
				foreach (Match m in Regex.Matches(text))
				{
					if (m.Length == 0) continue;
					x.Add(m.Index);
					x.Add(m.Index + m.Length);
				}
			}
			catch (ArgumentException) {}
			if (Invert) x.Add(text.Length);

			for (int i = 0; i != x.Count; i += 2)
				yield return new Span(x[i], x[i+1] - x[i]);
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
			// ReSharper disable NonReadonlyFieldInGetHashCode
			return
				m_patn_type   .GetHashCode()^
				m_expr        .GetHashCode()^
				m_ignore_case .GetHashCode()^
				m_active      .GetHashCode()^
				m_invert      .GetHashCode()^
				m_binary_match.GetHashCode();
			// ReSharper restore NonReadonlyFieldInGetHashCode
		}

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
	using RyLogViewer;

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
			private static void Check(Pattern pat, string test, string[] grp_names, string[] captures)
			{
				Assert.IsTrue(pat.IsMatch(test));
				var caps = pat.CaptureGroups(test).ToArray();

				Assert.AreEqual(grp_names.Length, caps.Length);
				Assert.AreEqual(captures.Length, caps.Length);

				for (int i = 0; i != caps.Length; ++i)
				{
					Assert.AreEqual(grp_names[i], caps[i].Key);
					Assert.AreEqual(captures[i], caps[i].Value);
				}
			}
			[Test] public static void SubStringMatches()
			{
				Check(new Pattern(EPattern.Substring, "test"),
					"A test string",
					new[]{"1"},
					new[]{"test"});
			}
			[Test] public static void WildcardMatches()
			{
				Check(new Pattern(EPattern.Wildcard, "test"),
					"A test string",
					new[]{"0"},
					new[]{"test"});
				Check(new Pattern(EPattern.Wildcard, "*test"),
					"A test string",
					new[]{"0","1"},
					new[]{"A test", "A "});
				Check(new Pattern(EPattern.Wildcard, "test*"),
					"A test string",
					new[]{"0","1"},
					new[]{"test string"," string"});
				Check(new Pattern(EPattern.Wildcard, "A * string"),
					"A test string",
					new[]{"0","1"},
					new[]{"A test string","test"});
			}
			[Test] public static void RegexMatches()
			{
				//Check(new Pattern(EPattern.RegularExpression, "A * string"),
				//	"A test string",
				//	new[]{"0","1"},
				//	new[]{"A test string","test"});
			}
		}
	}
}
#endif
