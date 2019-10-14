using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		public class AutoComplete
		{
			// See the ldr_object.cpp/AutoCompleteTemplates() function
			// for the primary reference.
			//
			// Template syntax:
			//   <~
			//   *Example [<name>] [<colour>]
			//   {
			//      (<x> <y> <z>)
			//      [*SomeFlag One|Two|Three:sf]
			//      [*SomethingElse:se:!sf]
			//      [*o2w {}]
			//   }
			//   ~>
			// Templates are defined between <~ and ~>. White space after <~ and before ~> is ignored.
			// Template definitions can be nested. Nested templates are only valid within the scope of the parent template.
			// Templates should be multi-line, users can replace the \n characters with ' ' if needed.
			// Literal text is text that is required.
			// Text within <> is a field
			// Text within [] is optional. [] can also nest.
			// Text within () is a repeatable section.
			// | means select one of the identifiers on either side of the bar.
			// [] can also describe requirements: [text:label:requirements] where:
			//    text:
			//      the normal optional field text
			//    label:
			//      an identifier to label the optional
			//    requirements:
			//      !a = the optional labelled 'a' must be given for this optional to be available
			//      ^a = the optional labelled 'a' must not be given for this optional to be available
			//      there can be mutliple requirements, e.g. [text:c:!a^b] = must have 'a', must not have 'b'

			public AutoComplete(string? templates = null)
			{
				Templates = new List<Template>();

				templates ??= AutoCompleteTemplates;
				foreach (var template in FindTemplateDeclarations(templates))
					Templates.Add(new Template(template));

				// Sort the templates to allow binary search
				Templates.Sort();
			}

			/// <summary>Split 'templates' into substrings containing template declarations</summary>
			private static IEnumerable<string> FindTemplateDeclarations(string templates)
			{
				// Minimal template is <~~>
				if (templates.Length < 4)
					yield break;

				// Partition the string into template definitions
				int b = 0, e = 0, nest = 0;
				for (int i = 0; i != templates.Length - 1; ++i)
				{
					b = nest == 0 ? i + 2 : b;
					nest += (templates[i + 0] == '<' && templates[i + 1] == '~') ? 1 : 0;
					nest -= (templates[i + 0] == '~' && templates[i + 1] == '>') ? 1 : 0;
					e = nest == 0 ? e : i + 1;

					if (e - b > 0 && nest == 0)
					{
						var template = templates.Substring(b, e - b).Trim(' ', '\n');
						yield return template;
					}
				}
			}

			/// <summary>A sorted list of templates</summary>
			public List<Template> Templates { get; }

			/// <summary>A single template</summary>
			public class Template :IComparable<Template>
			{
				public Template(string template)
				{
					ChildTemplates = new List<Template>();
					Text = template;

					foreach (var child in FindTemplateDeclarations(template))
						ChildTemplates.Add(new Template(child));

					// Sort for binary search
					ChildTemplates.Sort();
				}

				/// <summary>The template raw text</summary>
				public string Text { get; }

				/// <summary>Nested templates</summary>
				public List<Template> ChildTemplates { get; }

				/// <summary>Define ordering for templates</summary>
				public int CompareTo(Template rhs)
				{
					return Text.CompareTo(rhs.Text);
				}
			}
		
			
		}
	}
}