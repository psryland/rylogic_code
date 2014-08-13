using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public class Transform :Pattern
	{
		/// <summary>Represents a captured element with a source string</summary>
		public class Capture
		{
			/// <summary>The tag id for the capture</summary>
			public readonly string Id;

			/// <summary>The content of the captured character range from the source string</summary>
			public readonly string Elem;

			/// <summary>The character range for the capture in the source string</summary>
			public readonly Range Span;

			public Capture(string id, string elem)
			{
				Id   = id;
				Elem = elem;
			}
			public Capture(string id, string elem, Range span)
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
			public Range Span;

			public Tag(string id, Range span)
			{
				Id = id;
				Span = span;
			}
			public override string ToString()
			{
				return string.Format("{0} {1}", Id, Span);
			}
		}

		/// <summary>The collection of available text transformation implementations</summary>
		internal static List<ITransformSubstitution> Substitutors
		{
			get
			{
				if (m_substitutors == null)
				{
					m_substitutors = new List<ITransformSubstitution>();

					// Add built in substitutions (before the plugins so that built in subs can be replaced)
					{ var s = new SubNoChange();   m_substitutors.Add(s); }
					{ var s = new SubToLower();    m_substitutors.Add(s); }
					{ var s = new SubToUpper();    m_substitutors.Add(s); }
					{ var s = new SubSwizzle();    m_substitutors.Add(s); }
					{ var s = new SubCodeLookup(); m_substitutors.Add(s); }

					// Loads dlls from the plugins directory looking for transform substitutions
					var plugins = PluginLoader<ITransformSubstitution>.LoadWithUI(null, Misc.ResolveAppPath("plugins"), null, true);
					foreach (var sub in plugins.Plugins)
						m_substitutors.Add(sub);
				}
				return m_substitutors;
			}
		}
		private static List<ITransformSubstitution> m_substitutors;

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
				var tag  = s.Element(XmlTag.Tag ).As<string>();   // The capture tag that has an associated substitution
				var guid = s.Element(XmlTag.Id  ).As<Guid>();    // The unique id of the text transform
				var name = s.Element(XmlTag.Name).As<string>();  // The friendly name of the transform
				try
				{
					// Create a new instance of the appropriate transform and populate it with instance specific data
					var sub = Substitutors.FirstOrDefault(x => string.Compare(x.Guid.ToString(), guid.ToString(), StringComparison.OrdinalIgnoreCase) == 0);
					if (sub == null) throw new Exception("Text transform '{0}' (unique id: {1}) was not found\r\nThis text transform will not behaviour correctly".Fmt(name, guid));
					sub = (ITransformSubstitution)Activator.CreateInstance(sub.GetType());
					sub.FromXml(s.Element(XmlTag.SubData));
					Subs.Add(tag, sub);
				}
				catch (Exception ex)
				{
					Log.Warn(this, ex, "Text transform '{0}' ({1}) failed to load".Fmt(name, guid));
					Misc.ShowMessage(null,
						"Text transform '{0}' ({1}) failed to load\r\n".Fmt(name, guid) +
						"Reason:\r\n" + ex.Message,
						"Text Transform Load Failure", MessageBoxIcon.Information);
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
			// Add the base pattern properties and the replace template string
			base.ToXml(node);
			node.Add(Replace.ToXml(XmlTag.Replace ,false));

			// Add sub nodes for each transform instance
			var subs = new XElement(XmlTag.Subs);
			foreach (var s in Subs)
			{
				subs.Add(new XElement(XmlTag.Sub,
					s.Key               .ToXml(XmlTag.Tag     , false), // The capture tag that the sub applies to
					s.Value.Guid        .ToXml(XmlTag.Id      , false), // The unique id of the text transform type
					s.Value.DropDownName.ToXml(XmlTag.Name    , false), // The human readable name of the transform
					s.Value             .ToXml(XmlTag.SubData , false)  // Instance specific data for the transform
					));
			}
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
				m => new Tag(m.Value.TrimStart('{').TrimEnd('}'), new Range(m.Index, m.Index + m.Length)));
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
			var match = Regex.Match(text);
			if (!match.Success) return text;

			var caps = new Dictionary<string,Capture>();

			// Get the map of capture ids to captured values from 'text'
			var ids  = CaptureGroupNames;  // The collection of tags in the match template
			var grps = match.Groups;       // The captures from 'text' (starting at index 1, elem zero is always the whole match)
			Debug.Assert(ids.Length == grps.Count, "Expected the number of capture ids and number of regex capture groups to be the same");
			for (var i = 0; i != ids.Length; ++i)
			{
				var cap = new Capture(ids[i], grps[i].Value, new Range(grps[i].Index, grps[i].Index + grps[i].Length));
				caps.Add(cap.Id, cap);
				src_caps.Add(cap);
			}

			var result = text;
			result = result.Remove(grps[0].Index, grps[0].Length);
			result = result.Insert(grps[0].Index, Replace);

			// Build a list of the tags to be replaced in the result string
			var rtags = GetTags(result).ToList();

			// Perform the substitutions
			var ofs = 0;
			foreach (var t in rtags)
			{
				var sub = Subs[t.Id].Result(caps[t.Id].Elem);
				result = result.Remove(t.Span.Begini + ofs, t.Span.Sizei);
				result = result.Insert(t.Span.Begini + ofs, sub);
				dst_caps.Add(new Capture(t.Id, sub, new Range(t.Span.Begin + ofs, t.Span.Begin + ofs + sub.Length)));
				ofs += sub.Length - t.Span.Sizei;
			}

			// Sort 'dst_caps' so that it's in the same order as 'src_caps'
			for (int i = 0, j = 0; i != src_caps.Count && j != dst_caps.Count; ++i)
			{
				// Find src_caps[i].Id in dst_caps
				var id = src_caps[i].Id;
				var idx = dst_caps.IndexOf(c => c.Id == id);

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
			var match = Regex.Match(text);
			if (!match.Success) return text;

			var caps = new Dictionary<string,Capture>();

			// Get the map of capture ids to captured values from 'text'
			var ids  = CaptureGroupNames; // The collection of tags in the match template
			var grps = match.Groups;      // The captures from 'text' (starting at index 1, elem zero is always the whole match)
			Debug.Assert(ids.Length == grps.Count, "Expected the number of capture ids and number of regex capture groups to be the same");
			for (var i = 0; i != ids.Length; ++i)
			{
				var cap = new Capture(ids[i], grps[i].Value);
				caps.Add(cap.Id, cap);
			}

			// Transform works by only replacing the portion of 'text' that matches the
			// pattern. If users want to replace the entire line they need to setup the
			// pattern so that it matches the entire line.
			var result = text;
			result = result.Remove(grps[0].Index, grps[0].Length);
			result = result.Insert(grps[0].Index, Replace);

			// Build a list of the tags to be replaced in the result string
			List<Tag> rtags = (from t in GetTags(result) where ids.Contains(t.Id) select t).ToList();

			// Perform the substitutions
			int ofs = 0;
			foreach (Tag t in rtags)
			{
				string sub = Subs[t.Id].Result(caps[t.Id].Elem);
				result = result.Remove(t.Span.Begini + ofs, t.Span.Sizei);
				result = result.Insert(t.Span.Begini + ofs, sub);
				ofs += sub.Length - t.Span.Sizei;
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
			foreach (var n in doc.Root.Elements(XmlTag.Transform))
				try { list.Add(new Transform(n)); } catch {} // Ignore those that fail

			return list;
		}

		/// <summary>Serialise the highlight patterns to xml</summary>
		public static string Export(IEnumerable<Transform> txfms)
		{
			var doc = new XDocument(new XElement(XmlTag.Root));
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

	[TestFixture] internal static class RyLogViewerUnitTests
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