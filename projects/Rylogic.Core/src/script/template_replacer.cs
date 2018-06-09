using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Rylogic.Script
{
	/// <summary>A simple template replacer used for text substitutions</summary>
	public class TemplateReplacer :TextReader
	{
		/// <summary>The stack of character sources that we're reading from</summary>
		private readonly SrcStack m_src;

		/// <summary>The chunk size to buffer the source (must be at least 2x the maximum pattern match length)</summary>
		private readonly int m_chunk_size;

		/// <summary>The function that provides the text substitution</summary>
		private readonly SubstituteFunction m_substitute;

		/// <summary>A regex pattern used to match templates</summary>
		private readonly Regex m_pattern;

		/// <summary>The File/Line/Column of the output file</summary>
		private readonly Loc m_output_loc;

		/// <summary>How far until another regex pattern match test is needed</summary>
		private int m_match_ofs;

		/// <summary>
		/// A callback function called whenever a template field match is found.
		/// Return the string with which to replace 'field' with. If the substitution
		/// is an include, push a new source on to 'tr' and return string.Empty</summary>
		public delegate string SubstituteFunction(TemplateReplacer tr, Match match);

		public TemplateReplacer(Src src, string pattern, SubstituteFunction subst, int chunk_size = 4096)
		{
			m_src = new SrcStack(src);
			m_chunk_size = chunk_size;
			m_substitute = subst;
			m_pattern = new Regex(pattern);
			m_output_loc = new Loc();
			m_match_ofs = 0;
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			m_src.Dispose();
		}

		/// <summary>Push a new stream onto the source stack</summary>
		public void PushSource(Src src)
		{
			m_src.Push(src);
		}

		/// <summary>The File/Line/Column of the output file</summary>
		public Loc OutputLocation
		{
			get { return m_output_loc; }
		}

		/// <summary>
		/// Returns the next available character without actually reading it from
		/// the input stream. The current position of the TextReader is not changed by
		/// this operation. The returned value is -1 if no further characters are
		/// available.</summary>
		public override int Peek()
		{
			if (m_src.Empty) return -1;
			Parse();
			return m_src.Top.Peek;
		}

		/// <summary>
		/// Reads the next character from the input stream. The returned value is
		/// -1 if no further characters are available.</summary>
		public override int Read()
		{
			if (m_src.Empty) return -1;
			try
			{
				return m_output_loc.inc((char)Peek());
			}
			finally
			{
				--m_match_ofs;
				m_src.Next();
			}
		}

		/// <summary>Parses the current source position for template substitutions</summary>
		private void Parse()
		{
			// 'm_match_ofs' is the minimum number of characters until the next possible template
			// field match. When == 0 we need to retest the pattern.
			while (!m_src.Empty && m_match_ofs == 0)
			{
				m_src.Top.Cache(m_chunk_size);
				var m = m_pattern.Match(m_src.Top.TextBuffer.ToString());
				m_match_ofs = m.Success ? m.Index : m_src.Top.Length != m_chunk_size ? m_src.Top.Length : m_chunk_size / 2;

				// If there is a match for the next character in the src,
				// call the callback and do the substitution.
				if (m.Success && m_match_ofs == 0)
				{
					m_src.Top.TextBuffer.Remove(0, m.Length);
					var replace = m_substitute(this, m);
					m_src.Top.TextBuffer.Insert(0, replace);
				}
			}
		}

		/// <summary>Process a string template</summary>
		public static string Process(Src template, string pattern, SubstituteFunction subst, int chunk_size = 4096)
		{
			return new TemplateReplacer(template, pattern, subst, chunk_size).ReadToEnd();
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture] public class TestTemplateReplacer
	{
		[Test] public void SimpleStringSubstitution()
		{
			const string template = "The [speed] [colour] [noun1] [adjective1] over the [adjective2] [noun2]";
			const string substituted = "The quick brown fox jumped over the lazy dog";

			var template_replacer = new TemplateReplacer(new StringSrc(template), @"\[(\w+)\]", (tr,match) =>
				{
					switch (match.Value)
					{
					default: throw new Exception("Unknown field");
					case "[speed]": return "quick";
					case "[colour]": return "brown";
					case "[noun1]": return "fox";
					case "[adjective1]": return "jumped";
					case "[adjective2]": return "lazy";
					case "[noun2]": return "dog";
					}
				});
			using (template_replacer)
			{
				var result = template_replacer.ReadToEnd();
				Assert.Equal(substituted, result);
			}
		}
		[Test] public void SubstitutionWithIncludes()
		{
			const string include = "over the [adjective2]";
			const string template = "The [speed] [colour] [noun1] [adjective1] [include] [noun2]";
			const string substituted = "The quick brown fox jumped over the lazy dog";

			var template_replacer = new TemplateReplacer(new StringSrc(template), @"\[(\w+)\]", (tr,match) =>
				{
					switch (match.Value)
					{
					default: throw new Exception("Unknown field");
					case "[speed]": return "quick";
					case "[colour]": return "brown";
					case "[noun1]": return "fox";
					case "[adjective1]": return "jumped";
					case "[adjective2]": return "lazy";
					case "[noun2]": return "dog";
					case "[include]":  tr.PushSource(new StringSrc(include)); return string.Empty;
					}
				});
			using (template_replacer)
			{
				var result = template_replacer.ReadToEnd();
				Assert.Equal(substituted, result);
			}
		}
	}
}
#endif
