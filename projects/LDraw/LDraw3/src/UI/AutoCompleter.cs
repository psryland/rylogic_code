using System;
using System.Text;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace LDraw.UI
{
	public sealed partial class ScriptUI
	{
		/// <summary></summary>
		private class AutoCompleter
		{
			public AutoCompleter(View3d.AutoComplete.Template template, long start_position, int indent_level, string indent_text)
			{
				Template = template;
				Text = new StringBuilder();
				StartPostion = start_position;
				CurrentPosition = start_position;
				SingleLine = false;

				// Initialise the text with the non-optional parts
				ExpandTemplate(Template, false, indent_level, indent_text);
			}

			/// <summary>The active auto completion template</summary>
			private View3d.AutoComplete.Template Template { get; }

			/// <summary>The text being created from the template</summary>
			public StringBuilder Text { get; set; }

			/// <summary>The position that auto complete starts from</summary>
			public long StartPostion { get; }

			/// <summary>The caret position within 'Text'</summary>
			public long CurrentPosition { get; }

			/// <summary>Expand the template on a single line</summary>
			public bool SingleLine { get; }

			/// <summary>Populate 'Text' with the non-optional parts of 'tok'</summary>
			private void ExpandTemplate(View3d.AutoComplete.Token tok, bool include_optional, int indent_level, string indent_text)
			{
				switch (tok)
				{
				case View3d.AutoComplete.Template template:
					{
						NewLine(indent_level);
						Space();
						Text.Append($"*{template.Keyword}");
						
						foreach (var child in template.Child)
							ExpandTemplate(child, include_optional, indent_level, indent_text);
						break;
					}
				case View3d.AutoComplete.Section section:
					{
						NewLine(indent_level);
						Space();
						Text.Append("{");
						NewLine(indent_level + 1);

						foreach (var child in section.Child)
							ExpandTemplate(child, include_optional, indent_level + 1, indent_text);

						NewLine(indent_level);
						Text.Append("}");
						NewLine(indent_level);
						break;
					}
				case View3d.AutoComplete.Optional optional:
					{
						if (include_optional)
						{
							NewLine(indent_level);
							Space();
							Text.Append("[");

							foreach (var child in optional.Child)
								ExpandTemplate(child, include_optional, indent_level, indent_text);
							
							Text.Append("]");
							NewLine(indent_level);
						}
						break;
					}
				case View3d.AutoComplete.Repeat repeat:
					{
						NewLine(indent_level);
						Space();

						foreach (var child in repeat.Child)
							ExpandTemplate(child, include_optional, indent_level, indent_text);

						NewLine(indent_level);
						break;
					}
				case View3d.AutoComplete.Field field:
					{
						Space();
						Text.Append($"<{field.Name}>");
						break;
					}
				case View3d.AutoComplete.Select select:
					{
						NewLine(indent_level);
						ExpandTemplate(select.Child[0], include_optional, indent_level, indent_text);
						NewLine(indent_level);
						break;
					}
				case View3d.AutoComplete.Literal literal:
					{
						Text.Append(literal.Text);
						break;
					}
				case View3d.AutoComplete.LineBreak _:
					{
						NewLine(indent_level);
						break;
					}
				default:
					{
						throw new Exception("Unknown token type");
					}
				}

				// Add a new line to 'Text'
				void NewLine(int indent)
				{
					if (SingleLine) return;
					if (Text.Length == 0) return;
					Text.TrimEnd(' ', '\t', '\n').Append('\n');
					for (; indent-- != 0;) Text.Append(indent_text);
				}
				void Space()
				{
					if (Text.Length == 0) return;
					if (Text[Text.Length - 1] == '\n' || Text[Text.Length - 1] == '\t' || Text[Text.Length - 1] == ' ') return;
					Text.Append(' ');
				}
			}
		}
	}
}
