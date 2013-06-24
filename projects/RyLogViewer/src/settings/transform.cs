using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using pr.common;
using pr.extn;

namespace RyLogViewer
{
	public class Transform :IPattern
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

		public static TxfmSubLoader SubLoader { get { return m_subs_loader; } }
		private static readonly TxfmSubLoader m_subs_loader = new TxfmSubLoader();
		private readonly Pattern m_match;
		private Regex  m_compiled_patn;
		
		/// <summary>A map of substitutions to apply for each capture tag</summary>
		public Dictionary<string, ITxfmSub> Subs { get; private set; }
		
		/// <summary>The pattern used to match rows that will have the transform applied.</summary>
		public string Expr
		{
			get { return m_match.Expr; }
			set { m_match.Expr = value; }
		}

		/// <summary>The pattern type to interpret 'match' as</summary>
		public EPattern PatnType
		{
			get { return m_match.PatnType; }
			set { m_match.PatnType = value; }
		}

		/// <summary>True if the match pattern should ignore case</summary>
		public bool IgnoreCase
		{
			get { return m_match.IgnoreCase; }
			set { m_match.IgnoreCase = value;  }
		}

		/// <summary>True if this transform should be applied</summary>
		public bool Active
		{
			get { return m_match.Active; }
			set { m_match.Active = value; }
		}

		/// <summary>The template string used to create the transformed row</summary>
		public string Replace { get; set; }

		public Transform()
		{
			m_match      = new Pattern();
			Subs         = new Dictionary<string, ITxfmSub>();
			Expr        = "";
			Replace      = "";
			IgnoreCase   = false;
			Active       = true;
			m_match.PatternChanged += HandlePatternChanged;
			UpdateSubs();
		}
		private Transform(Transform rhs)
		{
			m_match    = new Pattern(rhs.m_match);
			Subs       = new Dictionary<string, ITxfmSub>(rhs.Subs);
			Replace    = rhs.Replace;
			Active     = rhs.Active;
			m_match.PatternChanged += HandlePatternChanged;
			UpdateSubs();
		}
		public Transform(XElement node)
		{
			// ReSharper disable PossibleNullReferenceException
			m_match     = new Pattern(node.Element(XmlTag.Match));
			Replace     = node.Element(XmlTag.Replace).Value;
			
			Subs = new Dictionary<string, ITxfmSub>();
			var subs = node.Element(XmlTag.Subs);
			foreach (var s in subs.Elements(XmlTag.Sub))
			{
				string id   = s.Element(XmlTag.Id  ).Value;
				string name = s.Element(XmlTag.Name).Value;
				ITxfmSub sub;
				try   { sub = SubLoader.Create(id, name); sub.FromXml(s.Element(XmlTag.SubData)); }
				catch { sub = new SubNoChange{Id = id}; }
				Subs.Add(sub.Id, sub);
			}

			m_match.PatternChanged += HandlePatternChanged;
			UpdateSubs();
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>Handles 'm_match' being changed</summary>
		private void HandlePatternChanged(object sender, EventArgs args)
		{
			m_compiled_patn = null;
			UpdateSubs();
		}

		/// <summary>Export this type to an xml node</summary>
		public XElement ToXml(XElement node)
		{
			var subs = new XElement(XmlTag.Subs);
			foreach (var s in Subs)
				subs.Add(new XElement(XmlTag.Sub,
					new XElement(XmlTag.Name ,s.Value.Name),
					new XElement(XmlTag.Id   ,s.Value.Id  ),
					s.Value.ToXml(new XElement(XmlTag.SubData))
					));
			node.Add
			(
				m_match.ToXml(new XElement(XmlTag.Match)),
				new XElement(XmlTag.Replace ,Replace),
				subs
			);
			return node;
		}
		
		/// <summary>Return the compiled regex string for reference</summary>
		public string RegexString
		{
			get
			{
				var ex = ValidateExpr();
				return ex == null
					? Regex.ToString()
					: string.Format("Expression invalid - {0}", ex.Message);
			}
		}

		/// <summary>Returns the match template as a compiled regular expression</summary>
		private Regex Regex
		{
			get
			{
				if (m_compiled_patn != null)
					return m_compiled_patn;
				
				// Notes:
				//  If an expression can't be represented in substr,wildcard form, harden up and use a regex
				
				// Convert the match string into a regular expression string and
				// replace the capture group tags with regex capture groups
				string expr = m_match.Expr;
				
				// If the expression is a regex, expect regex capture group syntax
				// Otherwise, expect capture groups of the form: {tag}
				if (m_match.PatnType != EPattern.RegularExpression)
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

		/// <summary>Returns the names of the capture groups in this pattern</summary>
		public string[] CaptureGroupNames
		{
			get
			{
				try { return Regex.GetGroupNames(); }
				catch { return new string[0]; }
			}
		}

		/// <summary>Returns the collection of Tags and their character spans in 'str'</summary>
		private static IEnumerable<Tag> GetTags(string str)
		{
			return Regex.Matches(str, @"\{\w+}").Cast<Match>().Select(
				m => new Tag(m.Value.TrimStart('{').TrimEnd('}'), new Span(m.Index, m.Length)));
		}

		/// <summary>Returns true if the match expression is valid</summary>
		public bool IsValid
		{
			get { return ValidateExpr() == null && ValidateReplace() == null; }
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

		/// <summary>Return true if 'text' matches the 'Match' pattern</summary>
		public bool IsMatch(string text)
		{
			return Expr.Length != 0 && IsValid && Regex.IsMatch(text);
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
			Subs = new Dictionary<string,ITxfmSub>();
			foreach (var id in CaptureGroupNames)
				Subs.Add(id, new SubNoChange{Id = id});
			
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
		public object Clone()
		{
			return new Transform(this);
		}

		/// <summary>Value equality test</summary>
		public override bool Equals(object obj)
		{
			var rhs = obj as Transform;
			return rhs != null
				&& Equals(m_match, rhs.m_match)
				&& Equals(Replace, rhs.Replace);
		}

		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			return
				m_match.GetHashCode()^
				Replace.GetHashCode();
		}
		
		/// <summary>Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.</summary>
		public override string ToString()
		{
			return Expr;
		}
	}
}

