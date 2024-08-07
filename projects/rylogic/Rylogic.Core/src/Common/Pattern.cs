﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Rylogic.Extn;

namespace Rylogic.Common
{
	/// <summary>Support pattern types</summary>
	public enum EPattern
	{
		Substring,
		Wildcard,
		RegularExpression
	};

	/// <summary>Pattern interface</summary>
	public interface IPattern :ICloneable, INotifyPropertyChanged
	{
		/// <summary>True if the pattern is a regular expression, false if it's just a substring</summary>
		EPattern PatnType { get; set; }

		/// <summary>The pattern to use when matching</summary>
		string Expr { get; set; }

		/// <summary>True if the pattern should ignore case</summary>
		bool IgnoreCase { get; set; }

		/// <summary>True if the match result should be inverted</summary>
		bool Invert { get; set; }

		/// <summary>Only match if the whole line matches</summary>
		bool WholeLine { get; set; }

		/// <summary>True if the dot (.) character matches the new line character</summary>
		bool SingleLine { get; set; }

		/// <summary>Returns the names of the capture groups in this pattern</summary>
		string[] CaptureGroupNames { get; }

		/// <summary>Returns true if the pattern is valid</summary>
		bool IsValid { get; }

		/// <summary>Returns true if the pattern is active</summary>
		bool Active { get; set; }

		/// <summary>Returns the capture groups captured when applying this pattern to 'text'</summary>
		IEnumerable<KeyValuePair<string, string>> CaptureGroups(string text);

		/// <summary>
		/// Return the first range of 'text' that matches this pattern.
		/// Note, the first returned span will be the string that matches the entire pattern.
		/// Any subsequent strings will be the capture groups from within the regex pattern.
		/// i.e.<para/>
		///  if your expression is @"x" you get one group when matched on "xox" equal to [0,1]<para/>
		///  if your expression is @"(x)" you get two groups, [0,1] for the whole expression,
		///  then [0,1] for the sub-expression<para/>
		/// Note, only the first match is returned, [2,1] is not returned by this method.</summary>
		IEnumerable<RangeI> Match(string text, int start = 0, int length = -1);

		/// <summary>
		/// Returns all occurrences of matches within 'text'.
		/// i.e.<para/>
		///   AllMatches(@"(x)", "xoxox") returns [0,1], [2,1], [4,1]<para/>
		/// Note, this method doesn't return capture groups, only whole expression matches.</summary>
		IEnumerable<RangeI> AllMatches(string text);

		/// <summary>Returns true if this pattern matches a substring in 'text'</summary>
		bool IsMatch(string text);
	}

	/// <summary>A handy regex helper that converts wildcard and substring searches into regex's as well</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class Pattern :IPattern
	{
		public Pattern()
			: this(EPattern.Substring, string.Empty)
		{ }
		public Pattern(EPattern patn_type, string expr)
		{
			m_expr = expr;
			PatnType = patn_type;
			IgnoreCase = false;
			Active = true;
			Invert = false;
			WholeLine = false;
			SingleLine = false;
		}
		public Pattern(Pattern rhs)
		{
			m_expr = rhs.Expr;
			Active = rhs.Active;
			PatnType = rhs.PatnType;
			IgnoreCase = rhs.IgnoreCase;
			Invert = rhs.Invert;
			WholeLine = rhs.WholeLine;
			SingleLine = rhs.SingleLine;
		}
		public Pattern(XElement node)
		{
			m_expr     = node.Element(XmlTag.Expr).As<string>(string.Empty);
			PatnType   = node.Element(XmlTag.PatnType).As<EPattern>(EPattern.Substring);
			Active     = node.Element(XmlTag.Active).As<bool>(true);
			IgnoreCase = node.Element(XmlTag.IgnoreCase).As<bool>(false);
			Invert     = node.Element(XmlTag.Invert).As<bool>(false);
			WholeLine  = node.Element(XmlTag.WholeLine).As<bool>(false);
			SingleLine = node.Element(XmlTag.SingleLine).As<bool>(false);
		}

		/// <summary>Export this pattern as XML</summary>
		public virtual XElement ToXml(XElement node)
		{
			node.Add
			(
				Expr      .ToXml(XmlTag.Expr, false),
				Active    .ToXml(XmlTag.Active, false),
				PatnType  .ToXml(XmlTag.PatnType, false),
				IgnoreCase.ToXml(XmlTag.IgnoreCase, false),
				Invert    .ToXml(XmlTag.Invert, false),
				WholeLine .ToXml(XmlTag.WholeLine, false),
				SingleLine.ToXml(XmlTag.SingleLine, false)
			);
			return node;
		}

		/// <summary>True if the pattern is a regular expression, false if it's just a substring</summary>
		public EPattern PatnType
		{
			get => m_patn_type;
			set => SetProp(ref m_patn_type, value, nameof(PatnType), invalidate_patn: true);
		}
		private EPattern m_patn_type;

		/// <summary>The pattern to use when matching</summary>
		public string Expr
		{
			get => m_expr;
			set => SetProp(ref m_expr, value, nameof(Expr), invalidate_patn: true);
		}
		private string m_expr;

		/// <summary>True if the pattern should ignore case</summary>
		public bool IgnoreCase
		{
			get => m_ignore_case;
			set => SetProp(ref m_ignore_case, value, nameof(IgnoreCase), invalidate_patn: true);
		}
		private bool m_ignore_case;

		/// <summary>True if the match result should be inverted</summary>
		public bool Invert
		{
			get => m_invert;
			set => SetProp(ref m_invert, value, nameof(Invert), invalidate_patn: false);
		}
		private bool m_invert;

		/// <summary>Only match if the whole line matches</summary>
		public bool WholeLine
		{
			get => m_whole_line;
			set => SetProp(ref m_whole_line, value, nameof(WholeLine), invalidate_patn: false);
		}
		private bool m_whole_line;

		/// <summary>True when the dot (.) character matches the new line character</summary>
		public bool SingleLine
		{
			get => m_single_line;
			set => SetProp(ref m_single_line, value, nameof(SingleLine), invalidate_patn: true);
		}
		private bool m_single_line;

		/// <summary>True if the pattern is active</summary>
		public bool Active
		{
			get => m_active;
			set => SetProp(ref m_active, value, nameof(Active), invalidate_patn: false);
		}
		private bool m_active;

		/// <summary>Update a property value</summary>
		private void SetProp<T>(ref T prop, T value, string prop_name, bool invalidate_patn)
		{
			if (Equals(prop, value)) return;
			prop = value;
			if (invalidate_patn)
			{
				m_compiled_patn = null;
				m_validation_exception = null;
			}
			NotifyPropertyChanged(prop_name);
			NotifyPropertyChanged(nameof(RegexString));
			NotifyPropertyChanged(nameof(CaptureGroupNames));
			NotifyPropertyChanged(nameof(IsValid));
			NotifyPropertyChanged(nameof(SyntaxErrorDescription));
			NotifyPropertyChanged(nameof(Description));
		}

		/// <summary>Returns the match template as a compiled regular expression</summary>
		public Regex Regex
		{
			get
			{
				if (m_compiled_patn != null)
					return m_compiled_patn;

				// Notes:
				//  If an expression can't be represented in substring, wildcard form, harden up and use a regex

				// Convert the match string into a regular expression string and
				// replace the capture group tags with regex capture groups
				string expr = Expr;

				// If the expression is a regex, expect regex capture group syntax
				// Otherwise, expect capture groups of the form: {tag}
				if (PatnType != EPattern.RegularExpression)
				{
					// Collapse all whitespace to a single space character
					if (!PreserveWhitespace)
						expr = Regex.Replace(expr, @"\s+", " ");

					// Escape the regex special chars
					expr = Regex.Escape(expr);

					// Replace wildcards with Regex equivalents
					if (PatnType == EPattern.Wildcard)
						expr = expr.Replace(@"\*", @"(.*)").Replace(@"\?", @"(.)");

					// Replace the (now escaped) '{tag}' capture
					// groups with named regular expression capture groups
					expr = Regex.Replace(expr, @"\\{(\w+)}", @"(?<$1>.*)");

					// Allow expressions that end with whitespace to also match the eol char
					if (PreserveWhitespace)
					{
						if (expr.EndsWith(@"\ "))
						{
							expr = expr.Remove(expr.Length - 2, 2);
							expr = expr + @"(?:$|\s)";
						}
					}
					else
					{
						// Replace all escaped whitespace with '\s+'
						expr = expr.Replace(@"\ ", @"\s+");

						// Allow expressions that end with whitespace to also match the eol char
						if (expr.EndsWith(@"\s+"))
						{
							expr = expr.Remove(expr.Length - 3, 3);
							expr = expr + @"(?:$|\s)";
						}
					}
				}

				// Compile the expression
				var opts = RegexOptions.Compiled;
				if (IgnoreCase) opts |= RegexOptions.IgnoreCase;
				if (SingleLine) opts |= RegexOptions.Singleline;
				m_compiled_patn = new Regex(expr, opts);
				return m_compiled_patn;
			}
		}
		private Regex? m_compiled_patn;

		/// <summary>Allows derived patterns to optionally keep whitespace in Substring/wildcard patterns</summary>
		protected virtual bool PreserveWhitespace => false;

		/// <summary>Return the compiled regex string for reference</summary>
		public string RegexString => ValidateExpr() is Exception ex ? $"Expression invalid - {ex.Message}" : Regex.ToString();

		/// <summary>Returns null if the match field is valid, otherwise an exception describing what's wrong</summary>
		public Exception? ValidateExpr()
		{
			try
			{
				// Already have a validate exception, return it
				if (m_validation_exception != null)
					return m_validation_exception;

				// Compiling the Regex will throw if there's something wrong with it. It will never be null
				if (Regex == null)
					return m_validation_exception = new ArgumentException("The regular expression is null");

				// No prob, bob!
				return m_validation_exception = null;
			}
			catch (Exception ex)
			{
				return m_validation_exception = ex;
			}
		}
		private Exception? m_validation_exception;

		/// <summary>Returns true if the match expression is valid</summary>
		public virtual bool IsValid => ValidateExpr() == null;

		/// <summary>Returns a string describing what's wrong with the expression</summary>
		public string SyntaxErrorDescription => ValidateExpr() is Exception ex ? ex.Message : string.Empty;

		/// <summary>Returns the names of the capture groups in this pattern</summary>
		public string[] CaptureGroupNames
		{
			get
			{
				try
				{
					var names = Regex.GetGroupNames();
					if (Invert) names = new[] { "0-", "0+" }.Concat(names.Skip(1)).ToArray();
					return names;
				}
				catch
				{
					return Array.Empty<string>();
				}
			}
		}

		/// <summary>Returns the capture groups captured when applying this pattern to 'text'</summary>
		public IEnumerable<KeyValuePair<string, string>> CaptureGroups(string text)
		{
			var grps = Match(text);
			var names = CaptureGroupNames;
			foreach (var (grp, i) in grps.WithIndex())
				yield return new KeyValuePair<string, string>(names[i], text.Substring(grp.Begi, grp.Sizei));
		}

		/// <summary>Returns true if this pattern matches a substring in 'text'</summary>
		public bool IsMatch(string text)
		{
			if (!Active || !IsValid) return false;
			Match match;
			bool is_match = Expr.Length != 0 && (match = Regex.Match(text)).Success && (!WholeLine || match.Value == text);
			return Invert ? !is_match : is_match;
		}

		/// <summary>
		/// Return the first range of 'text' that matches this pattern.
		/// Note, the first returned span will be the string that matches the entire pattern.
		/// Any subsequent strings will be the capture groups from within the regex pattern.
		/// i.e.<para/>
		///  if your expression is @"x" you get one group when matched on "xox" equal to [0,1]<para/>
		///  if your expression is @"(x)" you get two groups, [0,1] for the whole expression,
		///  then [0,1] for the sub-expression<para/>
		/// Note, only the first match is returned, [2,1] is not returned by this method.</summary>
		public IEnumerable<RangeI> Match(string text, int start = 0, int length = -1)
		{
			if (!Active || text == null) yield break;

			var x = new List<int>();
			try
			{
				var match = Regex.Match(text, start, length == -1 ? text.Length - start : length);
				if (match.Success && (!WholeLine || match.Value == text))
				{
					// The GroupCollection object returned by the Match.Groups property always has at least one member.
					// If the regular expression engine cannot find any matches in a particular input string, the
					// Group.Success property of the single Group object in the collection is set to false and the Group
					// object's Value property is set to String.Empty. If the regular expression engine can find a match,
					// the first element of the GroupCollection object returned by the Groups property contains a string
					// that matches the entire regular expression pattern. Each subsequent element represents a captured
					// group, if the regular expression includes capturing groups. For more information, see the
					// "Grouping Constructs and Regular Expression Objects" section of the Grouping Constructs in Regular
					// Expressions article.
					var grps = match.Groups;
					for (int i = 0; i != grps.Count; ++i)
					{
						x.Add(grps[i].Index);
						x.Add(grps[i].Index + grps[i].Length);
					}
				}
			}
			catch (ArgumentException) {}
			if (Invert)
			{
				// An inverted pattern returns matches for text ranges that don't match the pattern.
				// For regular expressions, invert the global match, but keep the capture groups.
				// Ensure there is always two "global" matches for the start and end.
				if (x.Count != 0)
				{
					x.Insert(2, text.Length);
					x.Insert(0, 0);
				}
				else
				{
					x.Add(0);
					x.Add(text.Length);
					x.Add(text.Length);
					x.Add(text.Length);
				}
			}

			// Convert the indices into ranges
			for (int i = 0; i != x.Count; i += 2)
			{
				Debug.Assert(x[i] <= x[i + 1]);
				yield return new RangeI(x[i], x[i + 1]);
			}
		}

		/// <summary>
		/// Returns all occurrences of matches within 'text'.
		/// i.e.<para/>
		///   AllMatches(@"(x)", "xoxox") returns [0,1], [2,1], [4,1]<para/>
		/// Note, this method doesn't return capture groups, only whole expression matches.</summary>
		public IEnumerable<RangeI> AllMatches(string text)
		{
			for (var i = 0;;)
			{
				var span = Match(text,i).FirstOrDefault();
				if (span.Size == 0) yield break;
				yield return span;
				i = (int)span.End;
			}
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public virtual object Clone() => new Pattern(this);

		/// <summary>Expression</summary>
		public string Description => $"{PatnType}: {(Invert?"NOT ":"")}{Expr}";

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string? prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		#region Equals
		public static bool operator == (Pattern? lhs, Pattern? rhs)
		{
			return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
		}
		public static bool operator != (Pattern? lhs, Pattern? rhs)
		{
			return !(lhs == rhs);
		}
		public bool Equals(Pattern? rhs)
		{
			return
				rhs           != null &&
				m_patn_type   == rhs.m_patn_type   && 
				m_expr        == rhs.m_expr        && 
				m_ignore_case == rhs.m_ignore_case && 
				m_active      == rhs.m_active      && 
				m_invert      == rhs.m_invert      && 
				m_whole_line  == rhs.m_whole_line  &&
				m_single_line == rhs.m_single_line; 
		}
		public override bool Equals(object? obj)
		{
			return Equals(obj as Pattern);
		}
		public override int GetHashCode()
		{
			// From MSDN's entry on 'Object.GetHashCode()'
			// In general, for mutable reference types, you should override GetHashCode *only* if:
			//  1) You can compute the hash code from fields that are **not mutable**; or
			//  2) You can ensure that the hash code of a mutable object does not change while
			//     the object is contained in a collection that relies on its hash code.
			return base.GetHashCode();
			//return new { m_patn_type, m_expr, m_ignore_case, m_active, m_invert, m_whole_line, m_single_line }.GetHashCode();
		}
		#endregion

		private static class XmlTag
		{
			public const string Expr = "expr";
			public const string PatnType = "patntype";
			public const string Active = "active";
			public const string IgnoreCase = "ignorecase";
			public const string Invert = "invert";
			public const string WholeLine = "wholeline";
			public const string SingleLine = "singleline";
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Linq;
	using Common;

	[TestFixture] public class TestPattern
	{
		/// <summary>Matches 'pat' to 'test' and checks the results agree with 'grp_names' and 'captures'</summary>
		private static void Check(Pattern pat, string test, string[]? grp_names, string[]? captures)
		{
			if (grp_names == null || captures == null)
			{
				Assert.False(pat.IsMatch(test));
			}
			else
			{
				Assert.True(pat.IsMatch(test));

				var caps = pat.CaptureGroups(test).ToArray();
				Assert.Equal(grp_names.Length, caps.Length);
				Assert.Equal(captures.Length, caps.Length);

				for (int i = 0; i != caps.Length; ++i)
				{
					Assert.Equal(grp_names[i], caps[i].Key);
					Assert.Equal(captures[i], caps[i].Value);
				}
			}
		}
		[Test] public void SubStringMatches0()
		{
			var p = new Pattern(EPattern.Substring, "test");
			Check(p, "A test string",
				new[]{"0"},
				new[]{"test"});
		}
		[Test] public void SubStringMatches1()
		{
			var p = new Pattern(EPattern.Substring, "test"){WholeLine = true};
			Check(p, "A test string",
				null,
				null);
		}
		[Test] public void SubStringMatches2()
		{
			var p = new Pattern(EPattern.Substring, "test ");
			Check(p, "A test string",
				new[]{"0"},
				new[]{"test "});
		}
		[Test] public void SubStringMatches3()
		{
			var p = new Pattern(EPattern.Substring, "string ");
			Check(p, "A test string",
				new[]{"0"},
				new[]{"string"});
		}
		[Test] public void WildcardMatches0()
		{
			var p = new Pattern(EPattern.Wildcard, "test");
			Check(p, "A test string",
				new[]{"0"},
				new[]{"test"});
		}
		[Test] public void WildcardMatches1()
		{
			var p = new Pattern(EPattern.Wildcard, "*test");
			Check(p, "A test string",
				new[]{"0", "1"},
				new[]{"A test", "A "});
		}
		[Test] public void WildcardMatches2()
		{
			var p = new Pattern(EPattern.Wildcard, "test*");
			Check(p, "A test string",
				new[]{"0", "1"},
				new[]{"test string", " string"});
		}
		[Test] public void WildcardMatches3()
		{
			var p = new Pattern(EPattern.Wildcard, "A * string");
			Check(p, "A test string",
				new[]{"0", "1"},
				new[]{"A test string", "test"});
		}
		[Test] public void WildcardMatches4()
		{
			var p = new Pattern(EPattern.Wildcard, "b*e?g");
			Check(p, "abcdefgh",
				new[]{"0", "1", "2"},
				new[]{"bcdefg", "cd", "f"});
		}
		[Test] public void WildcardMatches5()
		{
			var p = new Pattern(EPattern.Wildcard, "b*e?g");
			Check(p, "1b2345e6g7",
				new[]{"0", "1", "2"},
				new[]{"b2345e6g", "2345","6"});
		}
		[Test] public void WildcardMatches6()
		{
			var p = new Pattern(EPattern.Wildcard, "b*e?g"){WholeLine = true};
			Check(p, "1b2345e6g7",
				null,
				null);
			Check(p, "b2345e6g",
				new[]{"0","1","2"},
				new[]{"b2345e6g","2345","6"});
		}
		[Test] public void RegexMatches0()
		{
			var p = new Pattern(EPattern.RegularExpression, "ax*b");
			Check(p, "ab",
				new[]{"0"},
				new[]{"ab"});
		}
		[Test] public void RegexMatches1()
		{
			var p = new Pattern(EPattern.RegularExpression, "ax*b");
			Check(p, "axb",
				new[]{"0"},
				new[]{"axb"});
		}
		[Test] public void RegexMatches2()
		{
			var p = new Pattern(EPattern.RegularExpression, "ax*b");
			Check(p, "axxxxb",
				new[]{"0"},
				new[]{"axxxxb"});
		}
		[Test] public void RegexMatches3()
		{
			var p = new Pattern(EPattern.RegularExpression, "a.b"){WholeLine = true};
			Check(p, "eaxbe",
				null,
				null);
			Check(p, "axb",
				new[]{"0"},
				new[]{"axb"});
		}
		[Test] public void RegexMatches4()
		{
			var p = new Pattern(EPattern.RegularExpression, @"f.ll st.p\.");
			Check(p, "full stop",
				null,
				null);
			Check(p, "full stop.",
				new[]{"0"},
				new[]{"full stop."});
		}
		[Test] public void RegexMatches5()
		{
			var p = new Pattern(EPattern.RegularExpression, @"[aeiousy]");
			Check(p, "purple",
				new[]{"0"},
				new[]{"u"});
			Check(p, "monkey",
				new[]{"0"},
				new[]{"o"});
			Check(p, "dishwasher",
				new[]{"0"},
				new[]{"i"});
		}
		[Test] public void RegexMatches6()
		{
			var p = new Pattern(EPattern.RegularExpression, @"[^abc]");
			Check(p, "boat",
				new[]{"0"},
				new[]{"o"});
			Check(p, "cat",
				new[]{"0"},
				new[]{"t"});
			Check(p, "#123",
				new[]{"0"},
				new[]{"#"});
		}
		[Test] public void RegexMatches7()
		{
			var p = new Pattern(EPattern.RegularExpression, @"[A-C]");
			Check(p, "fat",
				null,
				null);
			Check(p, "cAt",
				new[]{"0"},
				new[]{"A"});
			Check(p, "BAT",
				new[]{"0"},
				new[]{"B"});
		}
		[Test] public void RegexMatches8()
		{
			var p = new Pattern(EPattern.RegularExpression, @"\w");
			Check(p, "@You",
				new[]{"0"},
				new[]{"Y"});
			Check(p, "#123",
				new[]{"0"},
				new[]{"1"});
			Check(p, "_up_",
				new[]{"0"},
				new[]{"_"});
			Check(p, "@#!$%^-",
				null,
				null);
		}
		[Test] public void RegexMatches9()
		{
			var p = new Pattern(EPattern.RegularExpression, @"\W");
			Check(p, "@You",
				new[]{"0"},
				new[]{"@"});
			Check(p, "#123",
				new[]{"0"},
				new[]{"#"});
			Check(p, "_up_",
				null,
				null);
			Check(p, "@#!$%^-",
				new[]{"0"},
				new[]{"@"});
		}
		[Test] public void RegexMatches10()
		{
			const string s = "xoxox";
			var p = new Pattern(EPattern.RegularExpression, @"(x)");

			// Match returns the whole expr match, then the capture group
			var r1 = p.Match(s).ToList();
			Assert.True(r1.SequenceEqual(new[]{ new RangeI(0,1), new RangeI(0,1) }));

			// AllMatches returns only whole expr matches but all occurrences in the string
			var r2 = p.AllMatches(s).ToList();
			Assert.True(r2.SequenceEqual(new[]{ new RangeI(0,1), new RangeI(2,3), new RangeI(4,5) }));
		}
		[Test] public void RegexMatches11()
		{
			const string s = "multi\nline";
			var p = new Pattern(EPattern.RegularExpression, @"(ti.li)") { SingleLine = true };

			// Match returns the whole expr match, then the capture group
			var r1 = p.Match(s).ToList();
			Assert.True(r1.SequenceEqual(new[] { new RangeI(3, 8), new RangeI(3, 8) }));
		}
	}
}
#endif
