using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rylogic.Gfx
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
		//    requirements:
		//      !a = the optional labelled 'a' must be given for this optional to be available
		//      ^a = the optional labelled 'a' must not be given for this optional to be available
		//      there can be mutliple requirements, e.g. [text:c:!a^b] = must have 'a', must not have 'b'
		//    text:
		//      the normal optional field text
		//    label:
		//      an identifier to label the optional

		public AutoComplete(string templates)
		{
			Templates = new List<Template>();

			// Minimal template is <~~>
			if (templates.Length < 4)
				return;

			// Partition the string into template definitions
			int b = 0, e = 0, nest = 0;
			for (int i = 0; i != templates.Length - 1; ++i)
			{
				b = nest == 0 ? i : b;
				nest += (templates[i + 0] == '<' && templates[i + 1] == '~') ? 1 : 0;
				nest -= (templates[i + 0] == '~' && templates[i + 1] == '>') ? 1 : 0;
				e = nest > 0 ? i : Math.Max(b,e);

				if (e - b > 4 && nest == 0)
				{
					var template = templates.Substring(b, e - b).Trim(' ', '\n');
					Templates.Add(new Template(template));
				}
			}

			// Sort the templates to allow binary search
			Templates.Sort();
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
