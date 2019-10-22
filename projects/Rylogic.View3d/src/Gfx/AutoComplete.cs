using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Extn;
using Rylogic.Script;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		public class AutoComplete
		{
			// Notes:
			//  - See the ldr_object.cpp/AutoCompleteTemplates() function for the primary reference.

			private const string Delim = " \t\n";
			private const char TemplateMark = '*';
			private const char ReferenceMark = '@';
			private const char ExpandMark = '$';
			private class TemplateLookup :Dictionary<string, Template> { }

			public AutoComplete(string? templates = null)
			{
				Templates = new List<Template>();
				var lookup = new TemplateLookup();

				// Parse the templates
				templates ??= AutoCompleteTemplates;
				using var src = new StringSrc(templates);
				for (; Extract.AdvanceToNonDelim(src, Delim); )
				{
					// Buffer a single template declaration.
					var len = BufferTemplateDeclaration(src);
					var desc = src.Buffer.ToString(0, len).Trim();
					var template = new Template(desc);

					// Add the template to the lookup map from keyword to template
					lookup.Add(template.Keyword.ToLower(), template);

					// Add 'TemplateMark' templates to the list of root-level templates
					if (desc[0] == TemplateMark)
						Templates.Add(template);

					src.Next(len);
				}

				// Replace placeholder templates
				foreach (var kv in lookup)
					LinkSharedTemplates(kv.Value, lookup);
				foreach (var tm in Templates)
					LinkSharedTemplates(tm, lookup);

				// Sort the templates to allow binary search
				SortTemplates(Templates);
			}

			/// <summary>A sorted list of templates</summary>
			public List<Template> Templates { get; }

			/// <summary>Match 'partial' to a template</summary>
			public IEnumerable<Template> Lookup(string partial)
			{
				partial = partial.TrimStart('*');
				var idx = partial.Length != 0 ? Templates.BinarySearch(x => string.Compare(x.FullText, partial, true), find_insert_position: true) : 0;
				for (; idx != Templates.Count; ++idx)
				{
					if (string.Compare(partial, 0, Templates[idx].Keyword, 0, partial.Length, true) != 0) break;
					yield return Templates[idx];
				}
			}

			/// <summary>Buffer from the current position to the next keyword</summary>
			private static int BufferTemplateDeclaration(Src src)
			{
				if (src != TemplateMark && src != ReferenceMark && src != ExpandMark)
					throw new ScriptException(Script.EResult.KeywordNotFound, src.Location, "Expected a keyword");

				// Stop buffering when the next keyword is found, or, if the
				// template contains a section, when the section end is found.
				var nest = 0;
				var has_section = false;
				var lit = new InLiteralString();
				var len = src.ReadAhead(x =>
				{
					if (lit.WithinLiteralString(x)) return 1;
					nest += "{[(<".Contains(x) ? 1 : 0;
					nest -= "}])>".Contains(x) ? 1 : 0;
					has_section |= x == '{';
					if (nest > 0) return 1;
					if (nest < 0) return 0;
					if (has_section && x == '}') return 0;
					if (!has_section && (x == TemplateMark || x == ReferenceMark || x == ExpandMark)) return 0;
					return 1;
				}, 1);
				if (has_section && src[len] == '}') ++len;
				return len;
			}

			/// <summary>Recursive function for replacing place-holder templates with the shared version</summary>
			private static void LinkSharedTemplates(Template template, TemplateLookup lookup)
			{
				for (int i = 0; i != template.Tokens.Count; ++i)
				{
					if (template.Tokens[i] is Template.TemplateReference ph)
					{
						// Find the referenced template in 'shared'
						if (!lookup.TryGetValue(ph.Keyword.ToLower(), out var templ))
							throw new Exception($"Template reference {ph.Keyword} is undefined in template {template.Keyword}");

						// If the template reference is a link, replace the token with the child template token
						if (!ph.Expand)
						{
							// Add the shared template to the children of 'template' (if not already there)
							if (!template.ChildTemplates.Contains(templ))
								template.ChildTemplates.Add(templ);

							// Replace the reference token with a child template token
							template.Tokens[i] = new Template.ChildTemplate(templ);
						}

						// If the template reference is an 'expand' reference, copy the contents between the first and last Section tokens
						else
						{
							var ibeg = templ.Tokens.IndexOf(x => x is Template.Section sec && sec.Beg);
							var iend = templ.Tokens.LastIndexOf(x => x is Template.Section sec && sec.End);
							if (ibeg == -1 || iend == -1)
								throw new Exception($"Reference template {templ.Keyword} is invalid. Could not find any content");

							// Remove the reference token, and insert the content tokens from 'templ'
							template.Tokens.RemoveAt(i);
							template.Tokens.InsertRange(i, templ.Tokens.GetRange(ibeg + 1, iend - ibeg - 1));
							--i; // Retest 'i'
						}
					}
				}

				// Call recursively
				foreach (var templ in template.ChildTemplates)
					LinkSharedTemplates(templ, lookup);
			}

			// Sort the templates to allow binary search
			private static void SortTemplates(List<Template> templates)
			{
				templates.Sort();
				foreach (var template in templates)
					SortTemplates(template.ChildTemplates);
			}

			/// <summary>A single template</summary>
			[DebuggerDisplay("{Description,nq}")]
			public class Template :IComparable<Template>
			{
				public Template(string template)
				{
					if (template.Length == 0)
						throw new Exception("Empty templates are not allowed");

					var src = new StringSrc(template, 1);

					TypeMark = template[0];
					FullText = template;
					Tokens = new List<Token>();
					ChildTemplates = new List<Template>();
					Keyword = Extract.Identifier(out var kw, src, Delim) ? kw : throw new Exception("Invalid template keyword");

					// Tokenise the template
					int nest0 = 0, nest1 = 0, nest2 = 0;
					for (; Extract.AdvanceToNonDelim(src, Delim);)
					{
						switch (src)
						{
						case TemplateMark:
							{
								// A child template definition
								var len = BufferTemplateDeclaration(src);
								var child_template = src.Buffer.ToString(0, len).Trim();
								var templ = ChildTemplates.Add2(new Template(child_template));
								Tokens.Add(new ChildTemplate(templ));
								src.Next(len);
								break;
							}
						case ReferenceMark:
						case ExpandMark:
							{
								// A placeholder for a referenced to a template
								var expand = src == ExpandMark;
								src.Next(); // Skip the marker character
								var keyword = Extract.Identifier(out kw, src, Delim) ? kw : throw new Exception($"Invalid template reference in template {Keyword}");
								Tokens.Add(new TemplateReference(keyword, expand));
								break;
							}
						case '{':
						case '}':
							{
								// Section begin/end
								Tokens.Add(new Section(src == '{'));
								nest0 += src == '{' ? +1 : -1;
								src.Next();
								break;
							}
						case '[':
						case ']':
							{
								// Optional section
								Tokens.Add(new Optional(src == '['));
								nest1 += src == '[' ? +1 : -1;
								src.Next();
								break;
							}
						case '(':
						case ')':
							{
								// Repeated section
								Tokens.Add(new Repeat(src == '('));
								nest2 += src == '(' ? +1 : -1;
								src.Next();
								break;
							}
						case '<':
						case '>':
							{
								// Field
								if (src == '>') throw new Exception($"Unexpected '>' character found in template: {Keyword}");
								var len = src.ReadAhead(x => x == '>' ? 0 : 1, 1);
								var field = src.Buffer.ToString(1, len - 1);
								Tokens.Add(new Field(field));
								src.Next(len + 1);
								break;
							}
						case '|':
							{
								Tokens.Add(new Select());
								src.Next();
								break;
							}
						default:
							{
								// Literal text
								var len = src.ReadAhead(x => " \n*@{}[]()<>|".Contains(x) ? 0 : 1);
								var lit = src.Buffer.ToString(0, len);
								Tokens.Add(new Literal(lit));
								src.Next(len);
								break;
							}
						}
					}
					if (nest0 != 0)
						throw new Exception($"Unmatched section ('{{' and '}}') for template {Keyword}");
					if (nest1 != 0)
						throw new System.Exception($"Unmatched optional block ('[' and ']') for template {Keyword}");
					if (nest2 != 0)
						throw new System.Exception($"Unmatched repeat block ('(' and ')') for template {Keyword}");
				}

				/// <summary>The template type character</summary>
				public char TypeMark { get; }

				/// <summary>The template raw text</summary>
				public string FullText { get; }

				/// <summary>The keyword part of the template (excludes the '*')</summary>
				public string Keyword { get; }

				/// <summary>Nested templates</summary>
				public List<Template> ChildTemplates { get; }

				/// <summary>The template broken down in to tokens</summary>
				public List<Token> Tokens { get; }

				/// <summary>Define ordering for templates</summary>
				public int CompareTo(Template rhs)
				{
					return FullText.CompareTo(rhs.FullText);
				}

				/// <summary></summary>
				public string Description => $"{TypeMark}{Keyword} {string.Join(" ", Tokens.Select(x => x.Description))}";

				[DebuggerDisplay("{Description,nq}")]
				public abstract class Token
				{
					public abstract string Description { get; }
				}
				public class Literal :Token
				{
					public Literal(string text) { Text = text; }
					public string Text { get; }
					public override string Description => Text;
				}
				public class Section :Token
				{
					public Section(bool start) { Beg = start; }
					public bool Beg { get; }
					public bool End => !Beg;
					public override string Description => Beg ? "{" : "}";
				}
				public class Optional :Token
				{
					public Optional(bool start) { Start = start; }
					public bool Start { get; }
					public bool End => !Start;
					public override string Description => Start ? "[" : "]";
				}
				public class Repeat :Token
				{
					public Repeat(bool start) { Start = start; }
					public bool Start { get; }
					public bool End => !Start;
					public override string Description => Start ? "(" : ")";
				}
				public class Field :Token
				{
					public Field(string name) { Name = name; }
					public string Name { get; }
					public override string Description => $"<{Name}>";
				}
				public class Select :Token
				{
					public Select() { }
					public override string Description => "|";
				}
				public class ChildTemplate :Token
				{
					public ChildTemplate(Template child) { Child = child; }
					public Template Child { get; }
					public override string Description => $"{Child.Keyword}";
				}
				public class TemplateReference :Token
				{
					public TemplateReference(string kw, bool expand) { Keyword = kw; Expand = expand; }
					public string Keyword { get; }
					public bool Expand { get; }
					public override string Description => $"{(Expand ? ExpandMark : ReferenceMark)}{Keyword}";
				}
			}
		}
	}
}
