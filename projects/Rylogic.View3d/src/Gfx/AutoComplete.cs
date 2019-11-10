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
			// An auto complete template is a tree structure of tokens.
			// Each token contains an ordered list of child tokens
			// e.g.
			//               *Box
			//           /    |     \
			//     [name]  [colour]   section
			//                      /  |  |  \
			//                   <x> <y> <z>  [*o2w]
			//                                  |
			//                               section
			//                               /     \
			//                            [*pos]   [*euler]
			// 
			// Using auto complete templates:
			//   1) The user selects a template (e.g. via. the auto complete list)
			//   2) Text is added for all the non-optional parts of the template.
			//   3) Given a position within a template, the index of the token can be found.
			//      We can then get list all optional parts up to the next non-optional part.
			//
			// Notes:
			//  - See the ldr_object_templates.cpp for the primary reference.

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
				foreach (var tok in Parse(src))
				{
					if (tok is Template template)
					{
						// Add the template to the lookup map from keyword to template
						lookup.Add(template.Keyword.ToLower(), template);

						// Add 'TemplateMark' templates to the list of root-level templates
						if (!template.Hidden)
							Templates.Add(template);
					}
					else
					{
						throw new ScriptException(Script.EResult.SyntaxError, src.Location, $"Unexpected root level token: {tok.Description}");
					}
				}

				// Replace template references
				foreach (var kv in lookup)
					ReplaceTemplateReferences(kv.Value, lookup);

				// Sort the top level templates to allow binary search
				Templates.Sort();
			}

			/// <summary>A sorted list of templates</summary>
			public List<Template> Templates { get; }

			/// <summary>Match 'partial' to a template</summary>
			public IEnumerable<Template> Lookup(string partial)
			{
				partial = partial.TrimStart('*');
				var idx = partial.Length != 0 ? Templates.BinarySearch(x => string.Compare(x.Keyword, partial, true), find_insert_position: true) : 0;
				for (; idx != Templates.Count; ++idx)
				{
					if (string.Compare(partial, 0, Templates[idx].Keyword, 0, partial.Length, true) != 0) break;
					yield return Templates[idx];
				}
			}

			/// <summary>Parse a template description into tokens</summary>
			private static IEnumerable<Token> Parse(Src src)
			{
				// Recursively build the token tree
				for (; Extract.AdvanceToNonDelim(src, " \t\r\n");)
				{
					switch (src)
					{
					case TemplateMark:
						{
							src.Next(); // Skip the '*'
							var hidden = src == TemplateMark;
							if (hidden) src.Next(); // Skip the '*'

							// Read the template keyword
							var keyword = Extract.Identifier(out var kw, src) ? kw : throw new ScriptException(Script.EResult.InvalidValue, src.Location, $"Invalid template keyword");

							// Buffer the remaining template description
							var len = BufferTemplateDeclaration(src);
							yield return new Template(keyword, hidden, Parse(new WrapSrc(src, len)));
							break;
						}
					case ReferenceMark:
					case ExpandMark:
						{
							// Skip the '@' or '$'
							var expand = src == ExpandMark;
							src.Next();

							// Read the keyword of the referenced template
							var keyword = Extract.Identifier(out var kw, src) ? kw : throw new ScriptException(Script.EResult.InvalidValue, src.Location, $"Invalid template keyword");
							yield return new TemplateRef(keyword, expand);
							break;
						}
					case '{':
					case '}':
						{
							if (src == '{') src.Next(); else throw new ScriptException(Script.EResult.TokenNotFound, src.Location, "Unmatched '}' token");

							// Buffer the content within the section
							int len = 0;
							for (var nest = 1; src[len] != 0 && nest != 0; ++len)
							{
								nest += src[len] == '{' ? 1 : 0;
								nest -= src[len] == '}' ? 1 : 0;
							}
							if (src[len - 1] != '}') throw new ScriptException(Script.EResult.TokenNotFound, src.Location, "Unmatched '{' token");
							yield return new Section(Parse(new WrapSrc(src, len - 1)));
							src.Next();
							break;
						}
					case '[':
					case ']':
						{
							if (src == '[') src.Next(); else throw new ScriptException(Script.EResult.TokenNotFound, src.Location, "Unmatched ']' token");

							// Buffer the content within the optional
							int len = 0;
							for (var nest = 1; src[len] != 0 && nest != 0; ++len)
							{
								nest += src[len] == '[' ? 1 : 0;
								nest -= src[len] == ']' ? 1 : 0;
							}
							if (src[len - 1] != ']') throw new ScriptException(Script.EResult.TokenNotFound, src.Location, "Unmatched '[' token");
							yield return new Optional(Parse(new WrapSrc(src, len - 1)));
							src.Next();
							break;
						}
					case '(':
					case ')':
						{
							if (src == '(') src.Next(); else throw new ScriptException(Script.EResult.TokenNotFound, src.Location, "Unmatched ')' token");

							// Buffer the content within the repeat
							int len = 0;
							for (var nest = 1; src[len] != 0 && nest != 0; ++len)
							{
								nest += src[len] == '(' ? 1 : 0;
								nest -= src[len] == ')' ? 1 : 0;
							}
							if (src[len - 1] != ')') throw new ScriptException(Script.EResult.TokenNotFound, src.Location, $"Unmatched '(' token");
							yield return new Repeat(Parse(new WrapSrc(src, len - 1)));
							src.Next();
							break;
						}
					case '<':
					case '>':
						{
							if (src == '<') src.Next(); else throw new ScriptException(Script.EResult.TokenNotFound, src.Location, $"Unmatched '>' token");
							var name = Extract.Identifier(out var kw, src) ? kw : throw new ScriptException(Script.EResult.InvalidValue, src.Location, $"Invalid field identifier");
							if (src != '>') throw new ScriptException(Script.EResult.TokenNotFound, src.Location, $"Unmatched '<' token");
							yield return new Field(name);
							src.Next();
							break;
						}
					case '|':
						{
							yield return new Select();
							src.Next();
							break;
						}
					case ';':
						{
							yield return new LineBreak();
							src.Next();
							break;
						}
					default:
						{
							// Literal text
							Extract.BufferWhile(src, (s, i) => !" \n*@{}[]()<>|".Contains(s[i]) ? 1 : 0, 0, out var len);
							yield return new Literal(src.Buffer.ToString(0, len));
							src.Next(len);
							break;
						}
					}
				}
			}

			/// <summary>Buffer from the current position to the next keyword</summary>
			private static int BufferTemplateDeclaration(Src src)
			{
				// Stop buffering when the next keyword is found, or, if the
				// template contains a section, when the section end is found.
				var nest = 0;
				var has_section = false;
				var lit = new InLiteral();
				Extract.BufferWhile(src, (s,i) =>
				{
					var ch = s[i];
					if (lit.WithinLiteralString(ch)) return 1;
					nest += "{[(<".Contains(ch) ? 1 : 0;
					nest -= "}])>".Contains(ch) ? 1 : 0;
					has_section |= ch == '{';
					if (nest > 0) return 1;
					if (nest < 0) return 0;
					if (has_section && ch == '}') return 0;
					if (!has_section && (ch == TemplateMark || ch == ReferenceMark || ch == ExpandMark)) return 0;
					return 1;
				}, 1, out var len);
				if (has_section && src[len] == '}') ++len;
				return len;
			}

			/// <summary>Recursive function for replacing template references with the actual templates</summary>
			private static void ReplaceTemplateReferences(Token tok, TemplateLookup lookup)
			{
				for (int i = 0; i != tok.Child.Count; ++i)
				{
					if (tok.Child[i] is TemplateRef tr)
					{
						// Find the referenced template
						if (!lookup.TryGetValue(tr.Keyword.ToLower(), out var templ))
							throw new Exception($"Template reference {tr.Keyword} is undefined in template: {tok.Description}");

						// If the template reference is an 'Expand' link, add the children of 'templ' to the children of 'tok'
						if (tr.Expand)
						{
							if (templ.Child.Count == 0 || !(templ.Child[0] is Section section))
								throw new Exception($"Template reference {tr.Keyword} cannot be expanded. (In template: {tok.Description})");

							tok.Child.RemoveAt(i);
							tok.Child.InsertRange(i, section.Child);
							--i; // Retest 'i'
						}

						// Otherwise, just replace the reference
						else
						{
							// Replace the token with the referenced template
							tok.Child[i] = templ;
						}
					}
					else if (tok.Child[i].Child.Count != 0)
					{
						ReplaceTemplateReferences(tok.Child[i], lookup);
					}
				}
			}

			/// <summary>Base class for tokens</summary>
			[DebuggerDisplay("{Description,nq}")]
			public abstract class Token
			{
				protected Token()
				{
					Child = new List<Token>();
				}
				protected Token(IEnumerable<Token> child_tokens)
					: this()
				{
					Child.AddRange(child_tokens);

					// Fix up 'Select' tokens. Look for sequences of: X|Y|Z and replace them with
					// a select token with the options as child tokens
					for (int i = 0; i != Child.Count; ++i)
					{
						if (!(Child[i] is Select))
							continue;

						// Find the range of tokens that belong to the select group
						var b = Math.Max(i - 1, 0); // inclusive range begin
						for (; i + 2 < Child.Count && Child[i + 2] is Select; i += 2) { }
						var e = Math.Min(i + 2, Child.Count); // exclusive range end

						// Create a new select item with the group as children
						var select = new Select(Child.GetRange(b, e - b).Where(x => !(x is Select)));
						if (select.Child.Count < 2)
							throw new ScriptException(Script.EResult.InvalidValue, new Loc(), "Select operator does not have a left and right option to select from");

						// Replace the range [b,e)
						Child.RemoveRange(b, e - b);
						Child.Insert(b, select);
						i = b;
					}
				}

				/// <summary>Ordered list of child tokens</summary>
				public List<Token> Child { get; }

				/// <summary>Debug view of the token</summary>
				public virtual string Description => string.Join(" ", Child.Select(x => x.Description));
			}
			public class Template :Token, IComparable<Template>
			{
				public Template(string keyword, bool hidden, IEnumerable<Token> child_tokens)
					:base(child_tokens)
				{
					Keyword = keyword;
					Hidden = hidden;
				}

				/// <summary>The keyword part of the template (excludes the '*')</summary>
				public string Keyword { get; }

				/// <summary>False if visible that the current level in the hierarchy</summary>
				public bool Hidden { get; }

				/// <summary></summary>
				public override string Description => $"{(Hidden ? "**" : "*")}{Keyword} {base.Description}";

				/// <summary>Define ordering for templates</summary>
				public int CompareTo(Template rhs)
				{
					return Keyword.CompareTo(rhs.Keyword);
				}
			}
			public class TemplateRef :Token
			{
				public TemplateRef(string keyword, bool expand)
				{
					Keyword = keyword;
					Expand = expand;
				}
				public string Keyword { get; }
				public bool Expand { get; }
				public override string Description => Expand ? $"${Keyword}" : $"@{Keyword}";
			}
			public class Section :Token
			{
				public Section(IEnumerable<Token> child_tokens) : base(child_tokens) {}
				public override string Description => $"{{{base.Description}}}";
			}
			public class Optional :Token
			{
				public Optional(IEnumerable<Token> child_tokens) : base(child_tokens) {}
				public override string Description => $"[{base.Description}]";
			}
			public class Repeat :Token
			{
				public Repeat(IEnumerable<Token> child_tokens) : base(child_tokens) {}
				public override string Description => $"({base.Description})";
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
				public Select(IEnumerable<Token> child_tokens) : base(child_tokens) { }
				public override string Description => Child.Count != 0 ? string.Join("|", Child.Select(x => x.Description)) : "|";
			}
			public class Literal :Token
			{
				public Literal(string text) { Text = text; }
				public string Text { get; }
				public override string Description => Text;
			}
			public class LineBreak :Token
			{
				public override string Description => ";";
			}
		}
	}
}
