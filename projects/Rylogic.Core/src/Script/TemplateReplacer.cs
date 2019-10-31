using System;
using System.IO;
using System.Text.RegularExpressions;
using Rylogic.Extn;

namespace Rylogic.Script
{
	/// <summary>A simple template replacer used for text substitutions</summary>
	public class TemplateReplacer :TextReader
	{
		/// <summary>
		/// A callback function called whenever a template field match is found.
		/// Return the string with which to replace 'field' with. If the substitution
		/// is an include, push a new source on to 'tr' and return string.Empty</summary>
		public delegate string SubstituteFunction(TemplateReplacer tr, Match match);

		public TemplateReplacer(Src src, string pattern, SubstituteFunction subst, int chunk_size = 4096)
		{
			Src = new SrcStack(src);
			Substitute = subst;
			Pattern = new Regex(pattern);
			ChunkSize = chunk_size;
			OutputLocation = new Loc();
			m_match_ofs = 0;
		}
		protected override void Dispose(bool disposing)
		{
			Src.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>The stack of character sources that we're reading from</summary>
		private SrcStack Src { get; }

		/// <summary>The function that provides the text substitution</summary>
		private SubstituteFunction Substitute { get; }

		/// <summary>A regex pattern used to match templates</summary>
		private Regex Pattern { get; }

		/// <summary>The chunk size to buffer the source (must be at least 2x the maximum pattern match length)</summary>
		private int ChunkSize { get; }

		/// <summary>The File/Line/Column of the output file</summary>
		public Loc OutputLocation { get; }

		/// <summary>Push a new stream onto the source stack</summary>
		public void PushSource(Src src)
		{
			Src.Push(src);
		}

		/// <summary>
		/// Returns the next available character without actually reading it from the input stream.
		/// The current position of the TextReader is not changed by this operation.
		/// The returned value is -1 if no further characters are available.</summary>
		public override int Peek()
		{
			// 'm_match_ofs' is the minimum number of characters until the next possible template
			// field match. When == 0 we need to retest the pattern.
			while (!Src.Empty && m_match_ofs == 0)
			{
				var top = Src.Top;

				// Buffer the source in chunks
				top.ReadAhead(ChunkSize);

				// Find the index of the next match
				var m = Pattern.Match(top.Buffer.ToString());
				m_match_ofs = m.Success ? m.Index : top.Buffer.Length != ChunkSize ? top.Buffer.Length : ChunkSize / 2;

				// If there is a match for the next character in the src,
				// call the callback and do the substitution.
				if (m.Success && m_match_ofs == 0)
				{
					top.Buffer.Remove(0, m.Length);
					var replace = Substitute(this, m);
					top.Buffer.Insert(0, replace);
				}
			}
			var ch = Src.Peek;
			return ch != 0 ? ch : -1;
		}

		/// <summary>
		/// Reads the next character from the input stream. The returned value is
		/// -1 if no further characters are available.</summary>
		public override int Read()
		{
			var ch = Peek();
			if (ch == -1)
				return -1;

			Src.Next();
			--m_match_ofs;
			OutputLocation.inc((char)ch);
			return ch;
		}

		/// <summary>How far until another regex pattern match test is needed</summary>
		private int m_match_ofs;

		/// <summary>Process a string template</summary>
		public static string Process(Src template, string pattern, SubstituteFunction subst, int chunk_size = 4096)
		{
			using var tr = new TemplateReplacer(template, pattern, subst, chunk_size);
			return tr.ReadToEnd();
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
