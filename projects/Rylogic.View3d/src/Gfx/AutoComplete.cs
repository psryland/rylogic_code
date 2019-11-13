using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rylogic.Extn;
using Rylogic.Script;
using Rylogic.Str;

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
			private const char HiddenMark = '*';
			private const char RootLevelOnlyMark = '!';
			private class TemplateLookup :Dictionary<string, Template> { }

			public AutoComplete(string? templates = null)
			{
				Templates = new List<Template>();
				var lookup = new TemplateLookup();

				// Parse the templates
				templates ??= AutoCompleteTemplates;
				using var src = new StringSrc(templates);
				foreach (var tok in Parse(src, true))
				{
					if (tok is Template template)
					{
						// Add 'TemplateMark' templates to the list of root-level templates
						if (!template.Flags.HasFlag(ETemplateFlags.Hidden))
							Templates.Add(template);

						// Add the template to the lookup map from keyword to template.
						// All root level templates must be unique.
						if (!template.Flags.HasFlag(ETemplateFlags.RootLevelOnly))
							lookup.Add(template.Keyword.ToLower(), template);
					}
					else
					{
						throw new ScriptException(Script.EResult.SyntaxError, src.Location, $"Unexpected root level token: {tok.Description}");
					}
				}

				// Replace template references. '*!' templates can existing in 'Templates' and not in 'lookup'
				foreach (var kv in lookup)
					ReplaceTemplateReferences(kv.Value, lookup);
				foreach (var tp in Templates)
					ReplaceTemplateReferences(tp, lookup);

				// Sort the top level templates to allow binary search
				Templates.Sort();
			}

			/// <summary>A sorted list of templates</summary>
			public List<Template> Templates { get; }

			/// <summary>Return templates that match 'keyword'</summary>
			public IEnumerable<Template> Lookup(string keyword, bool partial, Template? parent = null)
			{
				keyword = keyword.TrimStart('*');
				if (keyword.Length == 0 && !partial)
					yield break;

				// If there is a parent template, return the child templates
				if (parent != null)
				{
					// Search the child templates of 'parent'
					foreach (var child in parent.ChildTemplates)
					{
						if (IsMatch(keyword, partial, child))
							yield return child;
					}
				}

				// If 'parent' is null or a recursive template, check the root level templates
				if (parent == null || parent.Flags.HasFlag(ETemplateFlags.Recursive))
				{
					// Find index of the first match
					var idx = keyword.Length != 0 ? Templates.BinarySearch(x => string.Compare(x.Keyword, keyword, true), find_insert_position: true) : 0;
					for (; idx != Templates.Count; ++idx)
					{
						// Return each matching template
						var template = Templates[idx];
						if (template.Flags.HasFlag(ETemplateFlags.RootLevelOnly) && parent != null) continue;
						if (!IsMatch(keyword, partial, template)) break;
						yield return template;
					}
				}

				// Compare string to template
				static bool IsMatch(string keyword, bool partial, Template template)
				{
					var lhs = keyword.ToLower();
					var rhs = template.Keyword.ToLower();
					return partial ? string.Compare(lhs, 0, rhs, 0, lhs.Length) == 0 : lhs == rhs;
				}
			}

			/// <summary>Return the template associated with the given address</summary>
			public Template? this[string address]
			{
				get
				{
					var parts = address.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

					var template = (Template?)null;
					foreach (var part in parts)
					{
						var next = Lookup(part, false, template).SingleOrDefault();
						if (next == null)
							return null;

						template = next;
					}

					return template;
				}
			}

			/// <summary>Parse a template description into tokens</summary>
			private static IEnumerable<Token> Parse(Src src, bool root = false)
			{
				// Recursively build the token tree
				for (; Extract.AdvanceToNonDelim(src, " \t\r\n");)
				{
					switch (src)
					{
					case TemplateMark:
						{
							src.Next(); // Skip the '*'
							var flags = ETemplateFlags.Recursive;
							if (!root)
							{
								flags |= ETemplateFlags.Hidden;
								flags &= ~ETemplateFlags.Recursive;
							}
							if (src == HiddenMark)
							{
								src.Next();
								flags |= ETemplateFlags.Hidden;
								flags &= ~ETemplateFlags.Recursive;
							}
							if (src == RootLevelOnlyMark)
							{
								src.Next();
								flags |= ETemplateFlags.RootLevelOnly;
								flags &= ~ETemplateFlags.Recursive;
							}

							// Read the template keyword
							var keyword = Extract.Identifier(out var kw, src) ? kw : throw new ScriptException(Script.EResult.InvalidValue, src.Location, $"Invalid template keyword");

							// Buffer the remaining template description
							var len = BufferTemplateDeclaration(src);

							// Create the template instance
							yield return new Template(keyword, flags, Parse(new WrapSrc(src, len)));
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

			/// <summary>Return a string representation of a recursively expanded template</summary>
			public static string ExpandTemplate(Token tok, EExpandFlags flags, int indent_level = 0, string indent_text = "\t")
			{
				// Recursive expansion function
				StringBuilder DoExpand(StringBuilder sb, Token token, int indent, bool root = false)
				{
					switch (token)
					{
					case Template template:
						{
							NewLine(sb);
							Append(sb, $"*{template.Keyword}", indent);
							if (root || flags.HasFlag(EExpandFlags.ExpandChildTemplates))
							{
								foreach (var child in template.Child)
									DoExpand(sb, child, indent);
							}
							NewLine(sb);
							break;
						}
					case Section section:
						{
							NewLine(sb);
							Append(sb, "{", indent);
							NewLine(sb);

							foreach (var child in section.Child)
								DoExpand(sb, child, indent + 1);

							NewLine(sb);
							Append(sb, "}", indent);
							NewLine(sb);
							break;
						}
					case Optional optional:
						{
							if (optional.Child.FirstOrDefault() is Template)
								NewLine(sb);
							else
								Space(sb);

							Append(sb, "[", indent);
							foreach (var child in optional.Child)
							{
								if (flags.HasFlag(EExpandFlags.OptionalChildTemplates) && child is Template)
									DoExpand(sb, child, indent);
								if (flags.HasFlag(EExpandFlags.Optionals) && !(child is Template))
									DoExpand(sb, child, indent);
							}
							Append(sb, "]", indent);
							sb.TrimEnd("[]").TrimEnd('\n', '\t', ' ');
							break;
						}
					case Repeat repeat:
						{
							foreach (var child in repeat.Child)
								DoExpand(sb, child, indent);
							Append(sb, "...", indent);
							break;
						}
					case Field field:
						{
							Space(sb);
							Append(sb, $"<{field.Name}>", indent);
							break;
						}
					case Select select:
						{
							var sep = "";
							NewLine(sb);
							foreach (var child in select.Child)
							{
								Append(sb, sep, indent);
								if (!(child is Literal)) NewLine(sb);
								DoExpand(sb, child, indent);
								sep = " | ";
							}
							break;
						}
					case Literal literal:
						{
							Space(sb);
							Append(sb, literal.Text, indent);
							break;
						}
					case LineBreak _:
						{
							NewLine(sb);
							break;
						}
					default:
						{
							throw new Exception("Unknown token type");
						}
					}
					return sb;
				}
				void Append(StringBuilder sb, string text, int indent)
				{
					if (sb.Length != 0 && sb.Back() == '\n' && !flags.HasFlag(EExpandFlags.SingleLine))
						sb.Append(indent_text.Repeat(indent));

					sb.Append(text);
				}
				void NewLine(StringBuilder sb)
				{
					// Trim whitespace from the end of a line
					sb.TrimEnd(' ', '\t');
					var last = sb.Length != 0 ? sb.Back() : '\0';
					if (!flags.HasFlag(EExpandFlags.SingleLine) && last != '\0' && last != '\n' && last != '[' && last != '<')
						sb.Append('\n');
					if (flags.HasFlag(EExpandFlags.SingleLine) && last != '\0' && last != ' ' && last != '[' && last != '<')
						sb.Append(' ');
				}
				void Space(StringBuilder sb)
				{
					var last = sb.Length != 0 ? sb.Back() : '\0';
					if (last != '\0' && last != '\n' && last != '\t' && last != ' ' && last != '[' && last != '<')
						sb.Append(' ');
				}

				return DoExpand(new StringBuilder(), tok, indent_level, true).ToString();
			}

			/// <summary>Buffer from the current position to the next keyword</summary>
			private static int BufferTemplateDeclaration(Src src)
			{
				// Stop buffering when the next keyword is found, or, if the
				// template contains a section, when the section end is found.
				var nest = 0;
				var has_section = false;
				var lit = new InLiteral();
				Extract.BufferWhile(src, (s, i) =>
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

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is Token tok && 
						Equals(tok.Child, Child);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class Template :Token, IComparable<Template>
			{
				public Template(string keyword, ETemplateFlags flags, IEnumerable<Token> child_tokens)
					: base(child_tokens)
				{
					Keyword = keyword;
					Flags = flags;
				}

				/// <summary>The keyword part of the template (excludes the '*')</summary>
				public string Keyword { get; }

				/// <summary>Properties of this template</summary>
				public ETemplateFlags Flags { get; }

				/// <summary>Enumerate the child templates (recursive)</summary>
				public IEnumerable<Template> ChildTemplates
				{
					get
					{
						IEnumerable<Template> EnumChildren(Token tok)
						{
							foreach (var child in tok.Child)
							{
								if (child is Template templ)
									yield return templ;
								else if (child.Child.Count != 0)
									foreach (var c in EnumChildren(child))
										yield return c;
							}
						}
						return EnumChildren(this);
					}
				}

				/// <summary></summary>
				public override string Description => $"*{Keyword} {base.Description}";

				/// <summary>Define ordering for templates</summary>
				public int CompareTo(Template rhs)
				{
					return Keyword.CompareTo(rhs.Keyword);
				}

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is Template rhs &&
						rhs.Keyword == Keyword &&
						rhs.Flags == Flags &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class TemplateRef :Token
			{
				public TemplateRef(string keyword, bool expand)
				{
					Keyword = keyword;
					Expand = expand;
				}

				/// <summary>The name of the referenced template</summary>
				public string Keyword { get; }

				/// <summary>Copy the children of the referenced template</summary>
				public bool Expand { get; }

				/// <summary></summary>
				public override string Description => Expand ? $"${Keyword}" : $"@{Keyword}";

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is TemplateRef rhs &&
						rhs.Keyword == Keyword &&
						rhs.Expand == Expand &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class Section :Token
			{
				public Section(IEnumerable<Token> child_tokens)
					:base(child_tokens)
				{}

				/// <summary></summary>
				public override string Description => $"{{{base.Description}}}";

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is Section rhs &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class Optional :Token
			{
				public Optional(IEnumerable<Token> child_tokens)
					: base(child_tokens)
				{}

				/// <summary></summary>
				public override string Description => $"[{base.Description}]";

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is Optional rhs &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class Repeat :Token
			{
				public Repeat(IEnumerable<Token> child_tokens)
					:base(child_tokens)
				{}

				/// <summary></summary>
				public override string Description => $"({base.Description})";

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is Repeat rhs &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class Select :Token
			{
				public Select()
				{}
				public Select(IEnumerable<Token> child_tokens)
					:base(child_tokens)
				{}

				/// <summary></summary>
				public override string Description => Child.Count != 0 ? string.Join("|", Child.Select(x => x.Description)) : "|";

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is Select rhs &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class Field :Token
			{
				public Field(string name)
				{
					Name = name;
				}

				/// <summary>The field name</summary>
				public string Name { get; }

				/// <summary></summary>
				public override string Description => $"<{Name}>";

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is Field rhs &&
						rhs.Name == Name &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class Literal :Token
			{
				public Literal(string text) 
				{
					Text = text;
				}

				/// <summary>The literal text</summary>
				public string Text { get; }

				/// <summary></summary>
				public override string Description => Text;

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is Literal rhs &&
						rhs.Text == Text &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}
			public class LineBreak :Token
			{
				/// <summary></summary>
				public override string Description => ";";

				#region Equals
				public override bool Equals(object obj)
				{
					return obj is LineBreak rhs &&
						base.Equals(obj);
				}
				public override int GetHashCode()
				{
					return base.GetHashCode();
				}
				#endregion
			}

			[Flags]
			public enum ETemplateFlags
			{
				None = 0,

				/// <summary>Set if the template is only used via 'TemplateRef'</summary>
				Hidden = 1 << 0,

				/// <summary>Set if this template can only be used at the root level</summary>
				RootLevelOnly = 1 << 1,

				/// <summary>Set if this template can recursively include other root level templates (except for RootLevelOnly ones)</summary>
				Recursive = 1 << 2,
			}
			[Flags]
			public enum EExpandFlags
			{
				None = 0,

				/// <summary>Expand the template onto a single line</summary>
				SingleLine = 1 << 0,

				/// <summary>Include 'non-child-template' optionals</summary>
				Optionals = 1 << 1,

				/// <summary>Include 'child-template' optionals</summary>
				OptionalChildTemplates = 1 << 2,

				/// <summary>Recursively expand child templates</summary>
				ExpandChildTemplates = 1 << 3,
			}
		}
	}
}
