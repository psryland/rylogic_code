using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public class Transform :Pattern ,IPattern
	{
		/// <summary>Represents a captured element with a source string</summary>
		public class Capture
		{
			/// <summary>The tag id for the capture</summary>
			public readonly string Id;

			/// <summary>The content of the captured character range from the source string</summary>
			public readonly string Elem;

			/// <summary>The character range for the capture in the source string</summary>
			public readonly Span Span;

			public Capture(string id, string elem)
			{
				Id   = id;
				Elem = elem;
			}
			public Capture(string id, string elem, Span span)
			{
				Id   = id;
				Elem = elem;
				Span = span;
			}
			public override string ToString()
			{
				return string.Format("{0} {1} {2}", Id, Elem, Span);
			}
		}

		/// <summary>Represents a {x} tag in the Match or Replace template strings</summary>
		private class Tag
		{
			/// <summary>The id if the substitution to use on this tag</summary>
			public readonly string Id;

			/// <summary>The range of characters covered by this tag</summary>
			public Span Span;

			public Tag(string id, Span span)
			{
				Id = id;
				Span = span;
			}
			public override string ToString()
			{
				return string.Format("{0} {1}", Id, Span);
			}
		}

		internal static Dictionary<string, ITransformSubstitution> Substitutors
		{
			get
			{
				if (m_substitutors == null)
				{
					m_substitutors = new Dictionary<string, ITransformSubstitution>();

					// Add built in substitutions (before the plugins so that built in subs can be replaced)
					{ var s = new SubNoChange();   m_substitutors.Add(s.Name, s); }
					{ var s = new SubToLower();    m_substitutors.Add(s.Name, s); }
					{ var s = new SubToUpper();    m_substitutors.Add(s.Name, s); }
					{ var s = new SubSwizzle();    m_substitutors.Add(s.Name, s); }
					{ var s = new SubCodeLookup(); m_substitutors.Add(s.Name, s); }

					// Loads dlls from the plugins directory looking for transform substitutions
					var plugins = PluginLoader<TransformSubstitutionAttribute, ITransformSubstitution>.LoadWithUI(null, Misc.ResolveAppPath("plugins"), true);
					foreach (var sub in plugins.Plugins)
						m_substitutors.Add(sub.Name, sub);
				}
				return m_substitutors;
			}
		}
		private static Dictionary<string, ITransformSubstitution> m_substitutors;

		public Transform() :this(EPattern.Substring, string.Empty, string.Empty) {}
		public Transform(EPattern patn_type, string expr, string replace) :base(patn_type, expr)
		{
			Subs = new Dictionary<string, ITransformSubstitution>();
			Replace = replace;
			PatternChanged += HandlePatternChanged;
			UpdateSubs();
		}
		private Transform(Transform rhs) :base(rhs)
		{
			Subs = new Dictionary<string, ITransformSubstitution>(rhs.Subs);
			Replace = rhs.Replace;
			PatternChanged += HandlePatternChanged;
			UpdateSubs();
		}
		public Transform(XElement node) :base(node)
		{
			// ReSharper disable PossibleNullReferenceException
			Replace = node.Element(XmlTag.Replace).Value;

			Subs = new Dictionary<string, ITransformSubstitution>();
			var subs = node.Element(XmlTag.Subs);
			foreach (var s in subs.Elements(XmlTag.Sub))
			{
				var tag  = s.Element(XmlTag.Tag).Value;   // The capture tag that has an associated substitution
				var name = s.Element(XmlTag.Name).Value;  // The name (and unique id) of the substitutor
				try
				{
					// Create a new instance of the appropriate substitutor
					// and populate it with instance specific data
					var sub = Substitutors[name].Clone();
					sub.FromXml(s.Element(XmlTag.SubData));
					Subs.Add(tag, sub);
				}
				catch (Exception ex)
				{
					Log.Warn(this, ex, "Substitutor '{0}' was not found".Fmt(name));
					Subs.Add(tag, new SubNoChange());
				}
			}

			PatternChanged += HandlePatternChanged;
			UpdateSubs();
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>Export this type to an xml node</summary>
		public override XElement ToXml(XElement node)
		{
			// Add the base pattern properties
			base.ToXml(node);
			node.Add(new XElement(XmlTag.Replace ,Replace));

			// Prepare the subs node
			var subs = new XElement(XmlTag.Subs);
			foreach (var s in Subs)
				subs.Add(new XElement(XmlTag.Sub,
					new XElement(XmlTag.Tag  ,s.Key),          // The capture tag that the sub applies to
					new XElement(XmlTag.Name ,s.Value.Name),   // The human readable name of the substitutor
					s.Value.ToXml(new XElement(XmlTag.SubData))// Instance specific data for the substitutor
					));
			node.Add(subs);

			return node;
		}

		/// <summary>A map from capture tag to the substitutions to apply</summary>
		public Dictionary<string, ITransformSubstitution> Subs { get; private set; }

		/// <summary>The template string used to create the transformed row</summary>
		public string Replace { get; set; }

		/// <summary>Handles 'm_match' being changed</summary>
		private void HandlePatternChanged(object sender, EventArgs args)
		{
			UpdateSubs();
		}

		/// <summary>Returns the collection of Tags and their character spans in 'str'</summary>
		private static IEnumerable<Tag> GetTags(string str)
		{
			return Regex.Matches(str, @"\{\w+}").Cast<Match>().Select(
				m => new Tag(m.Value.TrimStart('{').TrimEnd('}'), new Span(m.Index, m.Length)));
		}

		/// <summary>Returns true if the match expression is valid</summary>
		public override bool IsValid
		{
			get { return base.IsValid && ValidateReplace() == null; }
		}

		/// <summary>Returns null if the replace field is valid, otherwise an exception describing what's wrong</summary>
		public Exception ValidateReplace()
		{
			try
			{
				// All tags in 'Replace' must exist in the match expression
				if (!GetTags(Replace).All(t => Subs.ContainsKey(t.Id)))
					return new ArgumentException("The replace pattern contains unknown tags");

				// No prob, bob!
				return null;
			}
			catch (Exception ex) { return ex; }
		}

		/// <summary>Apply the transform to 'text' and return the mapping of capture groups in 'text' and in the returned result</summary>
		public string Txfm(string text, out List<Capture> src_caps, out List<Capture> dst_caps)
		{
			Debug.Assert(IsValid, "Shouldn't be calling this unless the transform is valid");

			src_caps  = new List<Capture>();
			dst_caps  = new List<Capture>();

			// If 'text' doesn't match the 'Match' expression, return an empty map
			Match match = Regex.Match(text);
			if (!match.Success) return text;

			var caps = new Dictionary<string,Capture>();

			// Get the map of capture ids to captured values from 'text'
			var ids  = CaptureGroupNames;  // The collection of tags in the match template
			var grps = match.Groups;       // The captures from 'text' (starting at index 1, elem zero is always the whole match)
			Debug.Assert(ids.Length == grps.Count, "Expected the number of capture ids and number of regex capture groups to be the same");
			for (int i = 0; i != ids.Length; ++i)
			{
				Capture cap = new Capture(ids[i], grps[i].Value, new Span(grps[i].Index, grps[i].Length));
				caps.Add(cap.Id, cap);
				src_caps.Add(cap);
			}

			string result = text;
			result = result.Remove(grps[0].Index, grps[0].Length);
			result = result.Insert(grps[0].Index, Replace);

			// Build a list of the tags to be replaced in the result string
			List<Tag> rtags = GetTags(result).ToList();

			// Perform the substitutions
			int ofs = 0;
			foreach (Tag t in rtags)
			{
				string sub = Subs[t.Id].Result(caps[t.Id].Elem);
				result = result.Remove(t.Span.Index + ofs, t.Span.Count);
				result = result.Insert(t.Span.Index + ofs, sub);
				dst_caps.Add(new Capture(t.Id, sub, new Span(t.Span.Index + ofs, sub.Length)));
				ofs += sub.Length - t.Span.Count;
			}

			// Sort 'dst_caps' so that it's in the same order as 'src_caps'
			for (int i = 0, j = 0; i != src_caps.Count && j != dst_caps.Count; ++i)
			{
				// Find src_caps[i].Id in dst_caps
				string id = src_caps[i].Id;
				int idx = dst_caps.IndexOf(c => c.Id == id);

				// If found move it to position j
				if (idx != -1) dst_caps.Swap(idx, j++);
			}
			return result;
		}

		/// <summary>Apply the transform to 'text' (as fast as possible).</summary>
		public string Txfm(string text)
		{
			// This method is a duplicate of the one above, but is optimised for transforming
			// rows from the log file.

			Debug.Assert(IsValid, "Shouldn't be calling this unless the transform is valid");

			// If 'text' doesn't match the 'Match' expression, return the string unchanged
			Match match = Regex.Match(text);
			if (!match.Success) return text;

			var caps = new Dictionary<string,Capture>();

			// Get the map of capture ids to captured values from 'text'
			var ids  = CaptureGroupNames; // The collection of tags in the match template
			var grps = match.Groups;      // The captures from 'text' (starting at index 1, elem zero is always the whole match)
			Debug.Assert(ids.Length == grps.Count, "Expected the number of capture ids and number of regex capture groups to be the same");
			for (int i = 0; i != ids.Length; ++i)
			{
				Capture cap = new Capture(ids[i], grps[i].Value);
				caps.Add(cap.Id, cap);
			}

			// Transform works by only replacing the portion of 'text' that matches the
			// pattern. If users want to replace the entire line they need to setup the
			// pattern so that it matches the entire line.
			string result = text;
			result = result.Remove(grps[0].Index, grps[0].Length);
			result = result.Insert(grps[0].Index, Replace);

			// Build a list of the tags to be replaced in the result string
			List<Tag> rtags = (from t in GetTags(result) where ids.Contains(t.Id) select t).ToList();

			// Perform the substitutions
			int ofs = 0;
			foreach (Tag t in rtags)
			{
				string sub = Subs[t.Id].Result(caps[t.Id].Elem);
				result = result.Remove(t.Span.Index + ofs, t.Span.Count);
				result = result.Insert(t.Span.Index + ofs, sub);
				ofs += sub.Length - t.Span.Count;
			}
			return result;
		}

		/// <summary>Update the Subs map given the new match string</summary>
		private void UpdateSubs()
		{
			// Preserve the old subs map so we can merge it back in
			var subs = Subs;

			// Build a new map of capture ids to default substitution objects
			Subs = new Dictionary<string, ITransformSubstitution>();
			foreach (var id in CaptureGroupNames)
				Subs.Add(id, new SubNoChange());

			// Merge the old list of subs back into the Subs map
			foreach (var s in subs)
			{
				if (!Subs.ContainsKey(s.Key)) continue;
				Subs[s.Key] = s.Value;
			}
		}

		/// <summary>Reads an xml description of the transforms</summary>
		public static List<Transform> Import(string filters)
		{
			var list = new List<Transform>();

			XDocument doc;
			try { doc = XDocument.Parse(filters); } catch { return list; }
			if (doc.Root == null) return list;
			foreach (XElement n in doc.Root.Elements(XmlTag.Transform))
				try { list.Add(new Transform(n)); } catch {} // Ignore those that fail

			return list;
		}

		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(IEnumerable<Transform> txfms)
		{
			XDocument doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) return "";

			foreach (var tx in txfms)
				doc.Root.Add(tx.ToXml(new XElement(XmlTag.Transform)));

			return doc.ToString(SaveOptions.None);
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		public override object Clone()
		{
			return new Transform(this);
		}

		/// <summary>Value equality test</summary>
		public override bool Equals(object obj)
		{
			var rhs = obj as Transform;
			return rhs != null
				&& base.Equals(obj)
				&& Equals(Replace, rhs.Replace);
		}

		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			// ReSharper disable NonReadonlyFieldInGetHashCode
			return
				base.GetHashCode()^
				Replace.GetHashCode();
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
		internal static class TestTransform
		{
			[TestFixtureSetUp] public static void Setup()
			{
			}
			[TestFixtureTearDown] public static void CleanUp()
			{
			}
			private static void Check(Transform pat, string test, string result, string[] grp_names, string[] captures)
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

				Assert.AreEqual(result, pat.Txfm(test));
			}
			[Test] public static void SubStringMatches0()
			{
				Check(new Transform(EPattern.Substring, "test", string.Empty),
					"A test string",
					"A  string",
					new[]{"0"},
					new[]{"test"});
			}
			[Test] public static void SubStringMatches1()
			{
				Check(new Transform(EPattern.Substring, "test {a}", string.Empty),
					"A test string",
					"A ",
					new[]{"0","a"},
					new[]{"test string", "string"});
			}
			[Test] public static void SubStringMatches2()
			{
				Check(new Transform(EPattern.Substring, "test {a}", "{a} {0}"),
					"A test string",
					"A string test string",
					new[]{"0","a"},
					new[]{"test string", "string"});
			}
			[Test] public static void WildcardMatches()
			{
				Check(new Transform(EPattern.Wildcard, "* {a}ing", "{a} {0}"),
					"A test string",
					"str A test string",
					new[]{"0","a"},
					new[]{"A test string", "str"});
			}
			[Test] public static void RegexMatches0()
			{
				Check(new Transform(EPattern.RegularExpression, "^(.*?) (.*?) (.*?)$", "{2} {1} {3}"),
					"A test string",
					"test A string",
					new[]{"0","1","2","3"},
					new[]{"A test string","A","test","string"});
			}
			[Test] public static void RegexMatches1()
			{
				Check(new Transform(EPattern.RegularExpression, "^(?<a>.*?) (?<b>.*?) (?<c>.*?)$", "{b} {a} {c}"),
					"A test string",
					"test A string",
					new[]{"0","a","b","c"},
					new[]{"A test string","A","test","string"});
			}
		}
	}
}

#endif