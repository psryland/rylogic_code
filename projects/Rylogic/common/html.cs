using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using pr.extn;

namespace pr.common
{
	/// <summary>Builder class for making html text</summary>
	public class Html
	{
		#region Elements

		#region Head
		public class Head :Html
		{
			public Head() :base("head")
			{}
		}
		#endregion
		#region Body
		public class Body :Html
		{
			public Body(string class_ = null) :base("body", class_)
			{}
		}
		#endregion
		#region Style
		public class Style :Html
		{
			/// <summary>A Style definition is a map of key value pairs</summary>
			public class Def :Dictionary<string, string>
			{
				public Def(string name) { Name = name; }
				
				/// <summary>The name (or multiple names) for the style definition</summary>
				public string Name { get; set; }

				/// <summary>Add a key value pair to the definition. Returns this object for method chaining</summary>
				public new Def Add(string key, string value)
				{
					base.Add(key, value);
					return this;
				}

				/// <summary>Add the style definition key/value pairs to 'sb'</summary>
				public void GenerateContent(StringBuilder sb, int indent, string newline)
				{
					sb.Append(Name).Append(" {").Append(newline);
					foreach (var x in this)
					{
						if (newline.HasValue()) sb.Append('\t',indent+1); else sb.Append(" ");
						sb.Append(x.Key).Append(": ").Append(x.Value).Append(";").Append(newline);
					}
					sb.Append('\t',indent).Append("}");
				}

				public override string ToString()
				{
					return "{0} Count: {1}".Fmt(Name, Count);
				}
			}

			public Style() :base("style")
			{
				Defs = new List<Def>();
				Attr.Add("type","text/css");
			}
			
			/// <summary>Create a Style instance by parsing html text</summary>
			public Style Parse(string css)
			{
				// Expects [<style>] names { key:value; ... } [</style>]
				foreach (var m in Regex.Matches(css, @"\s*(.*?)\s*{(.*?)}", RegexOptions.Singleline).Cast<Match>())
				{
					var def = new Def(m.Groups[1].Value);
					foreach (var kv in Regex.Matches(m.Groups[2].Value, @"\s*(.+?)\s*:\s*(.*?);").Cast<Match>())
						def.Add(kv.Groups[1].Value, kv.Groups[2].Value);
					Add(def);
				}
				return this;
			}

			/// <summary>The style definitions defined in this style</summary>
			public List<Def> Defs { get; private set; }

			/// <summary>Add an html element to this element. Returns this for method chaining</summary>
			public Style Add(Def def)
			{
				Defs.Add(def);
				return this;
			}

			/// <summary>Get/Set/Add/Update the style definition by name</summary>
			public Def this[string def_name]
			{
				get
				{
					var def = Defs.FirstOrDefault(x => x.Name == def_name);
					if (def == null) def = Defs.Add2(new Def(def_name));
					return def;
				}
				set
				{
					var idx = Defs.IndexOf(x => x.Name == value.Name);
					if (idx >= 0) Defs[idx] = value;
					else Defs.Add(value);
				}
			}

			/// <summary>Check that this element can be added to 'parent'</summary>
			protected override void ValidParent(Html parent)
			{
				if (parent is Head) return;
				throw new Exception("{0} elements can not be parented to {1} elements".Fmt(GetType().Name, parent.GetType().Name));
			}

			/// <summary>True if this element has child content</summary>
			protected override bool HasContent
			{
				get { return base.HasContent || Defs.Count != 0; }
			}

			/// <summary>Add the content of this element to 'sb'</summary>
			protected override void GenerateContent(StringBuilder sb, int indent)
			{
				base.GenerateContent(sb, indent);

				var tabs = NewLine.HasValue() && indent >= 0 ? indent : 0;
				foreach (var x in Defs)
				{
					x.GenerateContent(sb, tabs, NewLine);
					if (x != Defs.Last() && NewLine.HasValue())
						sb.Append(NewLine).Append('\t', indent >= 0 ? indent : 0);
				}
			}
		}
		#endregion
		#region String
		public class Str :Html
		{
			public Str(string content = null, int capacity = 256) :base(string.Empty)
			{
				Content = new StringBuilder(capacity);
				if (content != null) Content.Append(content);
			}

			/// <summary>The string content</summary>
			public StringBuilder Content { get; private set; }

			/// <summary>Generate the html text for this tree of elements in 'sb'. Set 'indent' to -1 for no indenting</summary>
			public override StringBuilder Generate(StringBuilder sb = null, int indent = 0)
			{
				sb = sb ?? new StringBuilder();
				sb.Append(Content.ToString());
				return sb;
			}
		}
		#endregion
		#region Div
		public class Div :Html
		{
			public Div(string class_ = null) :base("div", class_)
			{}
			protected override void ValidParent(Html parent)
			{
				if (parent is Body) return;
				if (parent is Div) return;
				throw new Exception("{0} elements can not be parented to {1} elements".Fmt(GetType().Name, parent.GetType().Name));
			}
		}
		#endregion
		#region Table
		public class Table :Html
		{
			public class Row :Html
			{
				public class Hdr :Html
				{
					public Hdr(string class_ = null) :base("th", class_) { NewLine = string.Empty; }
					protected override void ValidParent(Html parent)
					{
						if (parent is Row) return;
						throw new Exception("{0} elements can not be parented to {1} elements".Fmt(GetType().Name, parent.GetType().Name));
					}
				}
				public class Data :Html
				{
					public Data(string class_ = null) :base("td", class_) { NewLine = string.Empty; }
					protected override void ValidParent(Html parent)
					{
						if (parent is Row) return;
						throw new Exception("{0} elements can not be parented to {1} elements".Fmt(GetType().Name, parent.GetType().Name));
					}
				}

				public Row(string class_ = null) :base("tr", class_) {}
				protected override void ValidParent(Html parent)
				{
					if (parent is Table) return;
					throw new Exception("{0} elements can not be parented to {1} elements".Fmt(GetType().Name, parent.GetType().Name));
				}
			}
			public Table(string class_ = null) :base("table", class_) {}
			protected override void ValidParent(Html parent)
			{
				if (parent is Body) return;
				if (parent is Div) return;
				throw new Exception("{0} elements can not be parented to {1} elements".Fmt(GetType().Name, parent.GetType().Name));
			}
			protected override void GenerateTagOpen(StringBuilder sb, ref int indent)
			{
				base.GenerateTagOpen(sb, ref indent);
				if (NewLine.HasValue()) sb.Append(NewLine).Append('\t', indent >= 0 ? indent : 0);
				sb.Append("<tdata>");
				if (indent >= 0) ++indent;
			}
			protected override void GenerateTagClose(StringBuilder sb, ref int indent)
			{
				if (indent >= 0) --indent;
				sb.Append("</tdata>");
				if (NewLine.HasValue()) sb.Append(NewLine).Append('\t', indent >= 0 ? indent-1 : 0);
				base.GenerateTagClose(sb, ref indent);
			}
		}
		#endregion

		#region Html Element Base

		/// <summary>Element attributes</summary>
		public class Attribs :Dictionary<string,string>
		{
			/// <summary>Add a key value pair to the definition. Returns this object for method chaining</summary>
			public new Attribs Add(string key, string value)
			{
				base.Add(key, value);
				return this;
			}

			/// <summary>Get/Set/Add/Update a key/value pair</summary>
			public new string this[string key]
			{
				get
				{
					string value;
					return TryGetValue(key, out value) ? value : string.Empty;
				}
				set
				{
					base[key] = value;
				}
			}
		}

		/// <summary>The parent element</summary>
		private Html m_parent;

		/// <summary>The child objects of this element</summary>
		private List<Html> m_elems;

		public Html(string tag = "html", string class_ = null, string id = null)
		{
			m_parent   = null;
			m_elems    = new List<Html>();
			Tag        = tag;
			Attr       = new Attribs();
			NewLine    = Environment.NewLine;

			if (class_ != null) Attr.Add("class",class_);
			if (id     != null) Attr.Add("id",id);
		}

		/// <summary>The html tag for this element</summary>
		public string Tag { get; private set; }

		/// <summary>The attributes for this element</summary>
		public Attribs Attr { get; private set; }

		/// <summary>Get/Set the class for this element</summary>
		public string Class
		{
			get { return Attr["class"]; }
			set { Attr["class"] = value; }
		}

		/// <summary>True if the generated html text is all on a single line</summary>
		public string NewLine { get; set; }
		public void NewLineAll(string newline)
		{
			NewLine = newline;
			m_elems.ForEach(x => NewLineAll(newline));
		}

		/// <summary>Return the html text represented by this tree structure of elements</summary>
		public override string ToString()
		{
			return Generate(null, 0).ToString();
		}

		/// <summary>Append string content. Returns this for method chaining</summary>
		public Html Add(string content, bool html_encode)
		{
			if (html_encode)
			{
				content = FromText(content);
				//content = WebUtility.HtmlEncode(content);
			}

			if (m_elems.Count != 0 && m_elems.Last() is Str)
				m_elems.Last().As<Str>().Content.Append(content);
			else
				Add(new Str(content));
			return this;
		}

		/// <summary>Add an html element to this element. Returns this for method chaining</summary>
		public Html Add<T>(T elem) where T:Html
		{
			elem.ValidParent(this);
			elem.m_parent = this;
			m_elems.Add(elem);
			elem.OnAdd();
			return this;
		}

		/// <summary>Add a collection of html elements. Returns this for method chaining</summary>
		public Html Add<T>(IEnumerable<T> elems) where T:Html
		{
			foreach (var e in elems) Add(e);
			return this;
		}

		/// <summary>Remove this element from it's parent</summary>
		public void Remove()
		{
			if (m_parent == null) return;
			m_parent.m_elems.Remove(this);
			m_parent = null;
			OnRemove();
		}

		/// <summary>The number of child elements</summary>
		public int Count
		{
			get { return m_elems.Count; }
		}

		/// <summary>Get/Set elements by index</summary>
		public Html this[int idx]
		{
			get { return m_elems[idx]; }
			set { m_elems[idx] = value; }
		}

		/// <summary>Get an element by tag and index</summary>
		public Html this[string tag, int idx = 0]
		{
			get { return m_elems.Where(x => x.Tag == tag).Skip(idx).FirstOrDefault(); }
		}

		/// <summary>Enumerate the elements</summary>
		public IEnumerable<Html> Elems
		{
			get { return m_elems; }
		}

		/// <summary>Check that this element can be added to 'parent'</summary>
		protected virtual void ValidParent(Html parent)
		{
			// Default to no checking...
			//throw new Exception("{0} elements can not be parented to {1} elements".Fmt(GetType().Name, parent.GetType().Name));
		}

		/// <summary>True if this element has child content</summary>
		protected virtual bool HasContent
		{
			get { return m_elems.Count != 0; }
		}

		/// <summary>Called when this element is added to another Html element</summary>
		protected virtual void OnAdd()
		{}

		/// <summary>Called when this element is removed from another Html element</summary>
		protected virtual void OnRemove()
		{}

		/// <summary>Generate the html text for this tree of elements in 'sb'. Set 'indent' to -1 for no indenting</summary>
		public virtual StringBuilder Generate(StringBuilder sb = null, int indent = 0)
		{
			sb = sb ?? new StringBuilder();

			// Add the tag and attributes
			GenerateTagOpen(sb, ref indent);

			// Add the content. Note, using 'HasContent' because some
			// elements may have content other than stuff in 'm_elems'
			if (HasContent)
			{
				if (NewLine.HasValue())
					sb.Append(NewLine).Append('\t', indent >= 0 ? indent : 0);

				GenerateContent(sb, indent);

				if (NewLine.HasValue())
					sb.Append(NewLine).Append('\t', indent >= 0 ? indent-1 : 0);
			}

			// Close the tag
			GenerateTagClose(sb, ref indent);

			return sb;
		}

		/// <summary>Add the element tag and attributes</summary>
		protected virtual void GenerateTagOpen(StringBuilder sb, ref int indent)
		{
			sb.Append("<").Append(Tag);
			Attr.ForEach(x => sb.Append(" ").Append(x.Key).Append("=\"").Append(x.Value).Append("\""));
			sb.Append(HasContent ? ">" : "/>");
			if (indent >= 0) ++indent;
		}

		/// <summary>Add the element close tag</summary>
		protected virtual void GenerateTagClose(StringBuilder sb, ref int indent)
		{
			if (indent >= 0) --indent;
			if (!HasContent) return;
			sb.Append("</").Append(Tag).Append(">");
		}

		/// <summary>Add the content of this element to 'sb'</summary>
		protected virtual void GenerateContent(StringBuilder sb, int indent)
		{
			foreach (var x in m_elems)
			{
				x.Generate(sb, indent);
				if (x != m_elems.Last() && NewLine.HasValue())
					sb.Append(NewLine).Append('\t', indent >= 0 ? indent : 0);
			}
		}

		#endregion

		#endregion

		#region Text-to-Html Conversion

		/// <summary>Converts HTML to plain text.</summary>
		public static string ToText(string html)
		{
			return HtmlToText.Instance.Convert(html);
		}

		/// <summary>Convert simple plain text to html</summary>
		public static string FromText(string text, bool nofollow = false)
		{
			return TextToHtml.Instance.Convert(text, nofollow);
		}

		#region Conversion Helpers

		/// <summary>Converts HTML to plain text.</summary>
		private class HtmlToText
		{
			// Singleton instance
			public static HtmlToText Instance { get { return m_instance ?? (m_instance = new HtmlToText()); } }
			private static HtmlToText m_instance;
			
			// Static data tables
			private static readonly Dictionary<string, string> Tags;
			private static readonly HashSet<string> IgnoreTags;
			
			// Instance variables
			private TextBuilder m_text;
			private string m_html;
			private int m_pos;
			
			// Static constructor (one time only)
			static HtmlToText()
			{
				Tags = new Dictionary<string, string>();
				Tags.Add("address", "\n");
				Tags.Add("blockquote", "\n");
				Tags.Add("div", "\n");
				Tags.Add("dl", "\n");
				Tags.Add("fieldset", "\n");
				Tags.Add("form", "\n");
				Tags.Add("h1", "\n");
				Tags.Add("/h1", "\n");
				Tags.Add("h2", "\n");
				Tags.Add("/h2", "\n");
				Tags.Add("h3", "\n");
				Tags.Add("/h3", "\n");
				Tags.Add("h4", "\n");
				Tags.Add("/h4", "\n");
				Tags.Add("h5", "\n");
				Tags.Add("/h5", "\n");
				Tags.Add("h6", "\n");
				Tags.Add("/h6", "\n");
				Tags.Add("p", "\n");
				Tags.Add("/p", "\n");
				Tags.Add("table", "\n");
				Tags.Add("/table", "\n");
				Tags.Add("ul", "\n");
				Tags.Add("/ul", "\n");
				Tags.Add("ol", "\n");
				Tags.Add("/ol", "\n");
				Tags.Add("/li", "\n");
				Tags.Add("br", "\n");
				Tags.Add("/td", "\t");
				Tags.Add("/tr", "\n");
				Tags.Add("/pre", "\n");
				
				IgnoreTags = new HashSet<string>();
				IgnoreTags.Add("script");
				IgnoreTags.Add("noscript");
				IgnoreTags.Add("style");
				IgnoreTags.Add("object");
			}
			
			/// <summary>Converts the given HTML to plain text and returns the result.</summary>
			/// <param name="html">HTML to be converted</param>
			/// <returns>Resulting plain text</returns>
			public string Convert(string html)
			{
				// Initialize state variables
				m_text = new TextBuilder();
				m_html = html;
				m_pos = 0;
				
				// Process input
				while (!EndOfText)
				{
					if (Peek() == '<')
					{
						// HTML tag
						bool selfClosing;
						string tag = ParseTag(out selfClosing);
						
						// Handle special tag cases
						if (tag == "body")
						{
							// Discard content before <body>
							m_text.Clear();
						}
						else if (tag == "/body")
						{
							// Discard content after </body>
							m_pos = m_html.Length;
						}
						else if (tag == "pre")
						{
							// Enter preformatted mode
							m_text.Preformatted = true;
							EatWhitespaceToNextLine();
						}
						else if (tag == "/pre")
						{
							// Exit preformatted mode
							m_text.Preformatted = false;
						}
						
						string value;
						if (Tags.TryGetValue(tag, out value))
							m_text.Write(value);
							
						if (IgnoreTags.Contains(tag))
							EatInnerContent(tag);
					}
					else if (Char.IsWhiteSpace(Peek()))
					{
						// Whitespace (treat all as space)
						m_text.Write(m_text.Preformatted ? Peek() : ' ');
						MoveAhead();
					}
					else
					{
						// Other text
						m_text.Write(Peek());
						MoveAhead();
					}
				}
				// Return result
				return WebUtility.HtmlDecode(m_text.ToString());
			}
			
			// Eats all characters that are part of the current tag
			// and returns information about that tag
			private string ParseTag(out bool selfClosing)
			{
				string tag = String.Empty;
				selfClosing = false;
				
				if (Peek() == '<')
				{
					MoveAhead();
					
					// Parse tag name
					EatWhitespace();
					int start = m_pos;
					if (Peek() == '/')
						MoveAhead();
					while (!EndOfText && !Char.IsWhiteSpace(Peek()) &&
							Peek() != '/' && Peek() != '>')
						MoveAhead();
					tag = m_html.Substring(start, m_pos - start).ToLower();
					
					// Parse rest of tag
					while (!EndOfText && Peek() != '>')
					{
						if (Peek() == '"' || Peek() == '\'')
							EatQuotedValue();
						else
						{
							if (Peek() == '/')
								selfClosing = true;
							MoveAhead();
						}
					}
					MoveAhead();
				}
				return tag;
			}
			
			// Consumes inner content from the current tag
			private void EatInnerContent(string tag)
			{
				string endTag = "/" + tag;
				
				while (!EndOfText)
				{
					if (Peek() == '<')
					{
						// Consume a tag
						bool selfClosing;
						if (ParseTag(out selfClosing) == endTag)
							return;
						// Use recursion to consume nested tags
						if (!selfClosing && !tag.StartsWith("/"))
							EatInnerContent(tag);
					}
					else MoveAhead();
				}
			}
			
			// Returns true if the current position is at the end of
			// the string
			private bool EndOfText
			{
				get { return (m_pos >= m_html.Length); }
			}
			
			// Safely returns the character at the current position
			private char Peek()
			{
				return (m_pos < m_html.Length) ? m_html[m_pos] : (char)0;
			}
			
			// Safely advances to current position to the next character
			private void MoveAhead()
			{
				m_pos = Math.Min(m_pos + 1, m_html.Length);
			}
			
			// Moves the current position to the next non-whitespace
			// character.
			private void EatWhitespace()
			{
				while (Char.IsWhiteSpace(Peek()))
					MoveAhead();
			}
			
			// Moves the current position to the next non-whitespace
			// character or the start of the next line, whichever
			// comes first
			private void EatWhitespaceToNextLine()
			{
				while (Char.IsWhiteSpace(Peek()))
				{
					char c = Peek();
					MoveAhead();
					if (c == '\n')
						break;
				}
			}
			
			// Moves the current position past a quoted value
			private void EatQuotedValue()
			{
				char c = Peek();
				if (c == '"' || c == '\'')
				{
					// Opening quote
					MoveAhead();
					// Find end of value
					m_pos = m_html.IndexOfAny(new[] { c, '\r', '\n' }, m_pos);
					if (m_pos < 0)
						m_pos = m_html.Length;
					else
						MoveAhead();    // Closing quote
				}
			}
			
			/// <summary>A StringBuilder class that helps eliminate excess whitespace.</summary>
			private class TextBuilder
			{
				private readonly StringBuilder m_text;
				private readonly StringBuilder m_curr_line;
				private int m_empty_lines;
				private bool m_preformatted;
				
				// Construction
				public TextBuilder()
				{
					m_text = new StringBuilder();
					m_curr_line = new StringBuilder();
					m_empty_lines = 0;
					m_preformatted = false;
				}
				
				/// <summary>
				/// Normally, extra whitespace characters are discarded.
				/// If this property is set to true, they are passed
				/// through unchanged.
				/// </summary>
				public bool Preformatted
				{
					get { return m_preformatted; }
					set
					{
						if (value)
						{
							// Clear line buffer if changing to
							// preformatted mode
							if (m_curr_line.Length > 0)
								FlushCurrLine();
							m_empty_lines = 0;
						}
						m_preformatted = value;
					}
				}
				
				/// <summary>Clears all current text.</summary>
				public void Clear()
				{
					m_text.Length = 0;
					m_curr_line.Length = 0;
					m_empty_lines = 0;
				}
				
				/// <summary>
				/// Writes the given string to the output buffer.
				/// </summary>
				/// <param name="s"></param>
				public void Write(string s)
				{
					foreach (char c in s)
						Write(c);
				}
				
				/// <summary>
				/// Writes the given character to the output buffer.
				/// </summary>
				/// <param name="c">Character to write</param>
				public void Write(char c)
				{
					if (m_preformatted)
					{
						// Write preformatted character
						m_text.Append(c);
					}
					else
					{
						if (c == '\r')
						{
							// Ignore carriage returns. We'll process
							// '\n' if it comes next
						}
						else if (c == '\n')
						{
							// Flush current line
							FlushCurrLine();
						}
						else if (Char.IsWhiteSpace(c))
						{
							// Write single space character
							int len = m_curr_line.Length;
							if (len == 0 || !Char.IsWhiteSpace(m_curr_line[len - 1]))
								m_curr_line.Append(' ');
						}
						else
						{
							// Add character to current line
							m_curr_line.Append(c);
						}
					}
				}
				
				// Appends the current line to output buffer
				private void FlushCurrLine()
				{
					// Get current line
					string line = m_curr_line.ToString().Trim();
					
					// Determine if line contains non-space characters
					string tmp = line.Replace("&nbsp;", String.Empty);
					if (tmp.Length == 0)
					{
						// An empty line
						m_empty_lines++;
						if (m_empty_lines < 2 && m_text.Length > 0)
							m_text.AppendLine(line);
					}
					else
					{
						// A non-empty line
						m_empty_lines = 0;
						m_text.AppendLine(line);
					}
					
					// Reset current line
					m_curr_line.Length = 0;
				}
				
				/// <summary>
				/// Returns the current output as a string.
				/// </summary>
				public override string ToString()
				{
					if (m_curr_line.Length > 0)
						FlushCurrLine();
					return m_text.ToString();
				}
			}
		}

		/// <summary>Converts plain text to HTML</summary>
		private class TextToHtml
		{
			/// <summary>Singleton access</summary>
			private static TextToHtml m_instance;
			public static TextToHtml Instance { get { return m_instance ?? (m_instance = new TextToHtml()); } }
			
			private const string ParaBreak = "\r\n\r\n";
			private const string Link = "<a href=\"{0}\">{1}</a>";
			private const string LinkNoFollow = "<a href=\"{0}\" rel=\"nofollow\">{1}</a>";
			
			/// <summary>Returns a copy of this string converted to HTML markup.</summary>
			/// <param name="s">The plain text string to convert to html</param>
			/// <param name="nofollow">If true, links are given "nofollow" attribute</param>
			public string Convert(string s, bool nofollow)
			{
				StringBuilder sb = new StringBuilder();
				
				int pos = 0;
				while (pos < s.Length)
				{
					// Extract next paragraph
					int start = pos;
					pos = s.IndexOf(ParaBreak, start, StringComparison.Ordinal);
					if (pos < 0) pos = s.Length;
					string para = s.Substring(start, pos - start).Trim();
					
					// Encode non-empty paragraph
					if (para.Length > 0)
						EncodeParagraph(para, sb, nofollow);
						
					// Skip over paragraph break
					pos += ParaBreak.Length;
				}
				// Return result
				return sb.ToString();
			}
			
			/// <summary>Encodes a single paragraph to HTML.</summary>
			/// <param name="s">Text to encode</param>
			/// <param name="sb">StringBuilder to write results</param>
			/// <param name="nofollow">If true, links are given 'nofollow' attribute</param>
			private static void EncodeParagraph(string s, StringBuilder sb, bool nofollow)
			{
				// Start new paragraph
				sb.AppendLine("<p>");
				
				// HTML encode text
				s = WebUtility.HtmlEncode(s);
				
				// Convert single newlines to <br>
				s = s.Replace(Environment.NewLine, "<br />\r\n");
				
				// Encode any hyperlinks
				EncodeLinks(s, sb, nofollow);
				
				// Close paragraph
				sb.AppendLine("\r\n</p>");
			}
			
			/// <summary>Encodes [[URL]] and [[Text][URL]] links to HTML.</summary>
			/// <param name="text">Text to encode</param>
			/// <param name="sb">StringBuilder to write results</param>
			/// <param name="nofollow">If true, links are given "nofollow" attribute</param>
			private static void EncodeLinks(string text, StringBuilder sb, bool nofollow)
			{
				// Parse and encode any hyperlinks
				int pos = 0;
				while (pos < text.Length)
				{
					// Look for next link
					int start = pos;
					pos = text.IndexOf("[[", pos, StringComparison.Ordinal);
					if (pos < 0) pos = text.Length;
					
					// Copy text before link
					sb.Append(text.Substring(start, pos - start));
					if (pos < text.Length)
					{
						string label, link;
						
						start = pos + 2;
						pos = text.IndexOf("]]", start, StringComparison.Ordinal);
						if (pos < 0) pos = text.Length;
						label = text.Substring(start, pos - start);
						int i = label.IndexOf("][", StringComparison.Ordinal);
						if (i >= 0)
						{
							link = label.Substring(i + 2);
							label = label.Substring(0, i);
						}
						else
						{
							link = label;
						}
						
						// Append link
						sb.Append(String.Format(nofollow ? LinkNoFollow : Link, link, label));
						
						// Skip over closing "]]"
						pos += 2;
					}
				}
			}
		}

		#endregion

		#endregion
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using common;

	[TestFixture] public class TestHtmlGen
	{
		[Test] public void HtmlTextGen()
		{
			var html = new Html()
				.Add(new Html.Head()
					.Add(new Html.Style()
						.Add(new Html.Style.Def("table")
							.Add("border","1px solid black")
							.Add("border-collapse","collapse"))))
				.Add(new Html.Body()
					.Add("Paul Was Here", false)
					.Add(new Html.Table()
						.Add(new Html.Table.Row()
							.Add(new Html.Table.Row.Hdr().Add("One", false))
							.Add(new Html.Table.Row.Hdr().Add("Two", false)))
						.Add(new Html.Table.Row()
							.Add(new Html.Table.Row.Data().Add("1", false))
							.Add(new Html.Table.Row.Data().Add("2", false))
							)));

			const string value =
@"<html>
	<head>
		<style type=""text/css"">
			table {
				border: 1px solid black;
				border-collapse: collapse;
			}
		</style>
	</head>
	<body>
		Paul Was Here
		<table>
			<tdata>
				<tr>
					<th>One</th>
					<th>Two</th>
				</tr>
				<tr>
					<td>1</td>
					<td>2</td>
				</tr>
			</tdata>
		</table>
	</body>
</html>";
			var text = html.ToString();
			Assert.True(text == value);
		}
		[Test] public void Convert()
		{
			const string s =
				"This is some plain\r\n" +
				"text containing newlines\r\n" +
				"and these characters: <,>,/,<!--,-->\r\n\r\n";
			
			string html = Html.FromText(s);
			string text = Html.ToText(html);
			Assert.AreEqual(s, text);
		}
	}
}
#endif