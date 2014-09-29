//////////////////////////////////////////////////////////////////////////////
// This source code and all associated files and resources are copyrighted by
// the author(s). This source code and all associated files and resources may
// be used as long as they are used according to the terms and conditions set
// forth in The Code Project Open License (CPOL), which may be viewed at
// http://www.blackbeltcoder.com/Legal/Licenses/CPOL.
//
// Copyright (c) 2011 Jonathan Wood
//

using System.Collections.Generic;
using System.Net;
using System.Text;
using System;

namespace pr.inet
{
	public static class Html
	{
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
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using inet;
	
	[TestFixture] public class TestHtml
	{
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
