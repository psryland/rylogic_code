using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.container;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public class Transform :IPattern
	{
		private string m_match;
		private Regex  m_compiled_patn;
		
		/// <summary>Represents a captured element with a source string</summary>
		public class Capture
		{
			/// <summary>The tag id for the capture</summary>
			public readonly string Id;

			/// <summary>The character range for the capture in the source string</summary>
			public readonly Span Span;

			/// <summary>The content of the captured character range from the source string</summary>
			public readonly string Elem;

			public Capture(string id, Span span, string elem)
			{
				Id   = id;
				Span = span;
				Elem = elem;
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

		/// <summary>A sorted list of substitutions to apply for each capture tag</summary>
		public ListMap<string, ITxfmSub> Subs { get; private set; }
		
		//assume substring only for now
		/// <summary>The pattern used to match rows that will have the transform applied.</summary>
		public string Match
		{
			get { return m_match; }
			set
			{
				m_match = value;
				m_compiled_patn = null;
				UpdateSubs();
			}
		}

		/// <summary>The template string used to create the transformed row</summary>
		public string Replace { get; set; }

		/// <summary>True if the match pattern should ignore case</summary>
		public bool IgnoreCase { get; set; }

		/// <summary>True if this transform should be applied</summary>
		public bool Active { get; set; }
		
		private static int TxfmCmp(string key, ITxfmSub value) { return -string.CompareOrdinal(key, value.Id); }

		public Transform()
		{
			Subs         = new ListMap<string, ITxfmSub>(TxfmCmp);
			Match        = "";
			Replace      = "";
			IgnoreCase   = false;
			Active       = true;
		}

		private Transform(Transform rhs)
		{
			Subs       = new ListMap<string,ITxfmSub>(rhs.Subs);
			Match      = rhs.Match;
			Replace    = rhs.Replace;
			IgnoreCase = rhs.IgnoreCase;
			Active     = rhs.Active;
		}

		/// <summary>Construct from xml description</summary>
		public Transform(XElement node)
		{
			// ReSharper disable PossibleNullReferenceException
			Match       = node.Element(XmlTag.Match).Value;
			Replace     = node.Element(XmlTag.Replace).Value;
			IgnoreCase  = bool.Parse(node.Element(XmlTag.IgnoreCase).Value);
			Active      = bool.Parse(node.Element(XmlTag.Active).Value);
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>Export this type to an xml node</summary>
		public XElement ToXml(XElement node)
		{
			node.Add
			(
				new XElement(XmlTag.Match      ,Match     ),
				new XElement(XmlTag.Replace    ,Replace   ),
				new XElement(XmlTag.IgnoreCase ,IgnoreCase),
				new XElement(XmlTag.Active     ,Active    )
			);
			return node;
		}
		
		/// <summary>Returns the match template as a compiled regular expression</summary>
		private Regex Regex
		{
			get
			{
				if (m_compiled_patn != null) return m_compiled_patn;
				
				// Convert the match string into a regular expression string and
				// replace the capture group tags with regex capture groups
				string expr = Match;
				expr = Regex.Escape(expr);
				expr = Regex.Replace(expr, @"\\{.*?}", @"(.*?)");
				expr = "^" + expr + "$";
				RegexOptions opts = (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled;
				return m_compiled_patn = new Regex(expr, opts);
			}
		}

		/// <summary>Returns the collection of {tag} strings in 'Match'</summary>
		private MatchCollection GetTags(string str)
		{
			return Regex.Matches(str, @"\{.*?}");
		}

		/// <summary>Returns true if the transform is valid</summary>
		public bool IsValid
		{
			get
			{
				// Check all tags in the match template and the result template exist in 'Subs'
				// (the match template should be true if set_Match is used)
				return
					GetTags(Match  ).Cast<Match>().All(m => Subs.IndexOf(x => x.Id == m.Value) != -1) &&
					GetTags(Replace).Cast<Match>().All(m => Subs.IndexOf(x => x.Id == m.Value) != -1);
			}
		}

		/// <summary>Return true if 'text' matches the 'Match' pattern</summary>
		public bool IsMatch(string text)
		{
			return Match.Length != 0 && Regex.IsMatch(text);
		}

		/// <summary>Returns a map of the capture group tag ids to the string elements read from 'text'</summary>
		public Dictionary<string, Capture> Captures(string text)
		{
			Dictionary<string,Capture> caps = new Dictionary<string,Capture>();
			
			// If 'text' doesn't match the 'Match' expression, return an empty map
			Match match = Regex.Match(text);
			if (!match.Success) return caps;
			
			var tags = GetTags(Match); // The collection of tags in the match template
			var grps = match.Groups;   // The captures from 'text' (starting at index 1, elem zero is always the whole match)
			Debug.Assert(tags.Count == grps.Count - 1, "Expected the number of tag ids and number of regex capture groups to be the same");
			for (int i = 0; i != tags.Count; ++i)
			{
				Capture cap = new Capture(tags[i].Value, new Span(grps[i+1].Index, grps[i+1].Length), grps[i+1].Value);
				caps.Add(cap.Id, cap);
			}
			
			return caps;
		}

		/// <summary>Apply the transform to 'text'.</summary>
		public string Txfm(string text)
		{
			// Get the map of tag ids to captured values from 'text'
			var tags = Captures(text);
			if (tags.Count == 0) return text;
			
			string result = Replace;
			
			// Build a list of the tags to be replaced in the result string
			List<Tag> rtags = (from Match m in GetTags(result) select new Tag(m.Value, new Span(m.Index, m.Length))).ToList();

			// Perform the substitutions on the tags (in reverse order to preserve indices)
			rtags.Reverse();
			foreach (Tag t in rtags)
			{
				string sub = Subs.Find(x=>x.Id==t.Id).Result(tags[t.Id].Elem);
				result = result.Remove(t.Span.Index, t.Span.Count);
				result = result.Insert(t.Span.Index, sub);
			}
			
			return result;
		}
		
		/// <summary>Update the Subs dictionary given the new match string</summary>
		private void UpdateSubs()
		{
			// Preserve the old subs map so we can merge it back in
			var subs = Subs;
			
			// Build a map of tag ids to default substitution objects
			Subs = new ListMap<string,ITxfmSub>(TxfmCmp);
			foreach (Match m in GetTags(Match))
				Subs.Add(m.Value, new SubNoChange{Id = m.Value});
			
			// Merge the old list of subs back into the Subs map
			foreach (var s in subs)
			{
				if (!Subs.ContainsKey(s.Id)) continue;
				Subs[s.Id] = s;
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
			Transform rhs = obj as Transform;
			return rhs != null
				&& Match  .Equals(rhs.Match  )
				&& Replace.Equals(rhs.Replace)
				&& Active .Equals(rhs.Active );
		}
		
		/// <summary>Value hash code</summary>
		public override int GetHashCode()
		{
			return
				Match  .GetHashCode()^
				Replace.GetHashCode()^
				Active .GetHashCode();
		}
		
		/// <summary>Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.</summary>
		public override string ToString()
		{
			return Match;
		}
	}
	
	/// <summary>A helper class for loading transform substitutions</summary>
	internal class TxfmSubLoader
	{
		// ReSharper disable UnassignedField.Global
		[ImportMany(typeof(ITxfmSub))] public List<ITxfmSub> TxfmSubs; // populated by 'ComposeParts'
		// ReSharper restore UnassignedField.Global
		
		/// <summary>Find all type derived from ITxfmSub</summary>
		public TxfmSubLoader()
		{
			TxfmSubs = null;
			var cat       = new AssemblyCatalog(typeof(Transform).Assembly);
			var comp_cont = new CompositionContainer(cat);
			comp_cont.ComposeParts(this);
		}

		/// <summary>Factory method for creating substitution objects by name</summary>
		public ITxfmSub Create(string id, string sub_type)
		{
			foreach (ITxfmSub s in TxfmSubs)
			{
				if (s.Type != sub_type) continue;
				ITxfmSub sub = s.Clone();
				sub.Id = id;
				return sub;
			}
			throw new ArgumentException("Unknown transform substitution type");
		}
	}

	/// <summary>An interface for substitution types used in Transform</summary>
	public interface ITxfmSub
	{
		/// <summary>The tag id for the substitution</summary>
		string Id { get; set; }
		
		/// <summary>The friendly name for the substitution</summary>
		string Type { get; }
		
		/// <summary>A summary of the configuration for this transform substitution</summary>
		string ConfigSummary { get; }
		
		/// <summary>A method to setup the transform substitution's specific data</summary>
		void Config();
		
		/// <summary>Returns 'elem' transformed</summary>
		string Result(string elem);

		/// <summary>Create a copy of this instance</summary>
		ITxfmSub Clone();
	}

	public class TxfmSubBase :ITxfmSub
	{
		/// <summary>The tag id if this substitution. Should be something wrapped in '{','}'. E.g {boobs}</summary>
		public string Id { get; set; }

		/// <summary>A human readable name for the substitution</summary>
		public string Type { get; private set; }

		public TxfmSubBase(string type) { Id = ""; Type = type; }

		/// <summary>A summary of the configuration for this transform substitution</summary>
		public virtual string ConfigSummary { get { return ""; } }

		/// <summary>A method to setup the transform substitution's specific data</summary>
		public virtual void Config() { } // Nothing to configure

		/// <summary>Returns 'elem' transformed</summary>
		public virtual string Result(string elem) { return elem; }

		/// <summary>Create a copy of this instance</summary>
		public ITxfmSub Clone() { return (ITxfmSub)MemberwiseClone(); }

		public override string ToString() { return string.Format("{0} ({1})" ,Id ,Type); }
	}

	/// <summary>A default substitution object which does not transform the input element</summary>
	[Export(typeof(ITxfmSub))]
	public class SubNoChange :TxfmSubBase
	{
		public SubNoChange() :base("No Change") {}
	}

	/// <summary>A substitution type that converts elements to lower case</summary>
	[Export(typeof(ITxfmSub))]
	public class SubToLower :TxfmSubBase
	{
		public SubToLower() :base("To Lower Case") {}
		public override string Result(string elem)
		{
			return elem.ToLower();
		}
	}

	/// <summary>A substitution type that converts elements to lower case</summary>
	[Export(typeof(ITxfmSub))]
	public class SubToUpper :TxfmSubBase
	{
		public SubToUpper() :base("To Upper Case") {}
		public override string Result(string elem)
		{
			return elem.ToUpper();
		}
	}

	/// <summary>A substitution that rearranges text</summary>
	[Export(typeof(ITxfmSub))]
	public class Swizzle :TxfmSubBase
	{
		private int[] m_swap;
		
		/// <summary>A summary of the configuration for this transform substitution</summary>
		public override string ConfigSummary
		{
			get { return "aabb -> abab"; }
		}

		public Swizzle() :base("Swizzle") {}
		public virtual void Config(Form parent)
		{
			var dg = new Form
			{
				FormBorderStyle = FormBorderStyle.FixedSingle,
			};
			var lbl0 = new Label
			{
			};
			var edit0 = new TextBox
			{
			};
			var lbl1 = new Label
			{
			};
			var edit1 = new TextBox
			{
			};
			dg.Controls.Add(lbl0);
			dg.Controls.Add(edit0);
			dg.Controls.Add(lbl1);
			dg.Controls.Add(edit1);
			dg.ShowDialog(parent);
		}
		public override string Result(string elem)
		{
			return elem;
		}
	}
}


		///// <summary>Return the capture groups created by applying 'Pattern' to 'text'</summary>
		//public IEnumerable<string> CaptureGroups(string text)
		//{
		//    if (!Active || Pattern.Expr.Length == 0 || !Pattern.ExprValid) yield break;
		//    Regex re;
		//    try
		//    {
		//        // Convert the expression into a regular expression preserving capture groups
		//        string expr = Pattern.RegexString;
		//        if (Pattern.PatnType == EPattern.Substring || Pattern.PatnType == EPattern.Wildcard)
		//        {
		//            expr = ReplaceIf(expr, "\\(", "(", (s,i) => !(i >= 2 && s[i-2]=='\\' && s[i-1]=='\\'));
		//            expr = ReplaceIf(expr, "\\)", ")", (s,i) => !(i >= 2 && s[i-2]=='\\' && s[i-1]=='\\'));
		//        }
		//        re = new Regex(expr);
		//    }
		//    catch (ArgumentException) { yield break; }

		//    var m = re.Match(text);
		//    foreach (Group g in m.Groups)
		//    {
		//        yield return g.Value;
		//        //foreach (Capture c in g.Captures)
		//        //    c.Value;
		//    }
		//}

		//private static string ReplaceIf(string str, string oldValue, string newValue, Func<string, int, bool> pred)
		//{
		//    for (int i = str.IndexOf(oldValue,0); i != -1; i = str.IndexOf(oldValue, i+1))
		//    {
		//        if (!pred(str,i)) continue;
		//        str = str.Remove(i, oldValue.Length);
		//        str = str.Insert(i, newValue);
		//        i += newValue.Length - 1;
		//    }
		//    return str;
		//}

