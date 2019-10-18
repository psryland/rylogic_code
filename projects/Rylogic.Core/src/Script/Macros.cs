using System.Collections.Generic;
using System.Text;
using Rylogic.Extn;

namespace Rylogic.Script
{
	/// <summary>Interface for the preprocessor macro handler</summary>
	public interface IMacroHandler
	{
		/// <summary>
		/// Add a macro expansion to the db.
		/// Throw EResult::MacroAlreadyDefined if the definition is already defined and different to 'macro'</summary>
		void Add(Macro macro);

		/// <summary>Remove a macro</summary>
		void Remove(string tag);

		/// <summary>Find a macro expansion for a given macro identifier. Returns null if no macro is found.</summary>
		Macro? Find(string tag);
	}

	/// <summary>A preprocessor macro definition</summary>
	public class Macro
	{
		/// <summary>Construct a simple '#define TWO 2' style macro</summary>
		public Macro(string? tag = null, string? expansion = null, List<string>? args = null, Loc? loc = null)
		{
			m_tag = tag ?? string.Empty;
			m_expansion = expansion ?? string.Empty;
			m_params = args ?? new List<string>();
			m_loc = loc ?? new Loc();
		}

		/// <summary>
		/// Construct a preprocessor macro of the form: 'TAG(p0,p1,..,pn)' expansion
		/// from a stream of characters. Stops at the first non-escaped new line</summary>
		public Macro(Src src, Loc loc)
			: this(null, null, null, loc)
		{
			// Extract the tag and find it's hash code
			m_tag = ReadTag(src, loc);

			// Hash the tag
			//	m_hash = Hash(m_tag);

			// Extract the optional parameters
			if (src == '(')
				ReadParams(src, m_params, loc, true);

			// Trim whitespace from before the expansion text
			for (; src != 0 && Str_.IsLineSpace(src); ++src) { }

			// Extract the expansion and trim all leading and following whitespace
			if (!Extract.Line(out var expansion, src, true))
				throw new ScriptException(EResult.InvalidMacroDefinition, loc, "invalid macro expansion");

			m_expansion = expansion.Trim();
		}

		/// <summary>The macro tag</summary>
		public string m_tag { get; }

		/// <summary>The substitution text</summary>
		public string m_expansion { get; }

		/// <summary>Parameters for the macro, Length==0 for no parameter list, [0]="" for empty parameter list 'TAG()'</summary>
		public List<string> m_params { get; }

		/// <summary>The source location of where the macro was defined</summary>
		public Loc m_loc { get; }

		/// <summary>Return a macro tag from 'src' (or fail)</summary>
		private string ReadTag(Src src, Loc loc)
		{
			if (Extract.Identifier(out var tag, src)) return tag;
			throw new ScriptException(EResult.InvalidIdentifier, loc, "invalid macro name");
		}

		/// <summary>
		/// Extract a comma separated parameter list of the form '(p0,p1,..,pn)'
		/// If 'identifiers' is true then the parameters are expected to be identifiers.
		/// If not, then anything delimited by commas is accepted. 'identifiers' == true is used when
		/// reading the definition of the macro, 'identifiers' == false is used when expanding an instance.
		/// If an empty parameter list is given, i.e. "()" then 'args' is returned containing one blank parameter.
		/// Returns true if the macro does not take parameters or the correct number of parameters where given,
		/// false if the macro takes parameters but none were given. Basically, 'false' means, don't treat
		/// this macro as matching because no params were given. If false is returned the buffer will
		/// contain anything read during this method.</summary>
		private bool ReadParams(Src buf, List<string> args, Loc location, bool identifiers)
		{
			// Buffer up to the first non-whitespace character
			// If no parameters are given, then the macro doesn't match
			if (!identifiers && args.Count != 0)
			{
				int i = 0; for (; Str_.IsWhiteSpace(buf[i]); ++i) { }
				if (buf[i] != '(') return false;
				buf += i;
			}

			// If we're not reading the parameter names for a macro definition
			// and the macro takes no parameters, then ReadParams is a no-op
			if (!identifiers && m_params.Count == 0)
				return true;

			// Capture the strings between commas as the parameters
			for (++buf; buf != ')'; buf += buf != ')' ? 1 : 0)
			{
				// Read parameter names for a macro definition
				if (identifiers)
				{
					if (!Extract.Identifier(out var arg, buf))
						throw new ScriptException(EResult.InvalidIdentifier, location, "invalid macro identifier");
					args.Add(arg);
				}

				// Read parameters being passed to the macro
				else
				{
					var arg = new StringBuilder();
					for (int nest = 0; (buf != ',' && buf != ')') || nest != 0; ++buf)
					{
						if (buf == 0) throw new ScriptException(EResult.UnexpectedEndOfFile, location, "macro parameter list incomplete");
						arg.Append(buf);
						nest += buf == '(' ? 1 : 0;
						nest -= buf == ')' ? 1 : 0;
					}
					args.Add(arg.ToString());
				}
			}
			++buf; // Skip over the ')'

			// Add a blank argument to distinguish between "TAG()" and "TAG"
			if (args.Count == 0)
				args.Add(string.Empty);

			// Check enough parameters have been given
			if (!identifiers && m_params.Count != args.Count)
				throw new ScriptException(EResult.ParameterCountMismatch, location, "incorrect number of macro parameters");

			return true;
		}

		/// <summary>
		/// Expands the macro into the string 'exp' with the text of this macro including substituted parameter text.</summary>
		private string Expand(List<string> args, Loc location)
		{
			if (args.Count != m_params.Count)
				throw new ScriptException(EResult.ParameterCountMismatch, location, "macro parameter count mismatch");

			// Set the string to the macro text initially
			var exp = new StringBuilder(m_expansion);

			// Substitute each parameter
			for (int i = 0; i != m_params.Count; ++i)
			{
				var what = m_params[i];
				if (what.Length == 0) continue;
				var len = what.Length;

				// Replace the instances of 'what' with 'with'
				string with;
				for (int j = Str_.IndexOfIdentifier(exp.ToString(), what, 0); j != exp.Length; j = Str_.IndexOfIdentifier(exp.ToString(), what, j += len))
				{
					// If the identifier is prefixed with '##' then just remove the '##'
					// this will have the effect of concatenating the substituted strings.
					if (j >= 2 && exp[j - 1] == '#' && exp[j - 2] == '#')
					{
						j -= 2;
						len += 2;
						with = args[i];
					}

					// If the identifier is prefixed with '#' then replace 'what' with 'with' as a literal string
					else if (j >= 1 && exp[j - 1] == '#')
					{
						j -= 1; len += 1;
						with = args[i];
						with.Replace("\"", "\\\"");
						with = with.Quotes(add: true);
					}

					// Otherwise, normal substitution
					else
					{
						with = args[i];
					}

					// Do the substitution
					exp.Remove(j, len);
					exp.Insert(j, with);
					j += with.Length - len;
				}
			}

			return exp.ToString();
		}

		#region Equals
		public override bool Equals(object? obj)
		{
			return
				obj is Macro rhs &&
				m_tag == rhs.m_tag &&
				m_expansion == rhs.m_expansion &&
				m_params.Count == rhs.m_params.Count;
		}
		public override int GetHashCode()
		{
			return new { m_tag, m_expansion }.GetHashCode();
		}
		public static bool operator ==(Macro lhs, Macro rhs)
		{
			return Equals(lhs, rhs);
		}
		public static bool operator !=(Macro lhs, Macro rhs)
		{
			return !(lhs == rhs);
		}
		#endregion

		/// <summary>Helper for recursive expansion of macros. A macro will not be expanded if the same macro has already been expanded earlier in the recursion</summary>
		private class Ancestor
		{
			public Ancestor(Macro macro, Ancestor parent)
			{
				m_macro = macro;
				m_parent = parent;
			}

			/// <summary></summary>
			public Macro m_macro;

			/// <summary></summary>
			public Ancestor? m_parent;

			// Returns true if 'm' is an ancestor of itself
			public bool IsRecursive(Macro macro)
			{
				var p = (Ancestor?)this;
				for (; p != null && p.m_macro != macro; p = p.m_parent) { }
				return p != null;
			}
		}
	}

	/// <summary>A collection of preprocessor macros</summary>
	public class MacroDB :IMacroHandler
	{
		// Notes:
		//  - To programmatically define macros, subclass this type and extend the 'Find' method.
		// The database of macro definitions

		private class DB :Dictionary<string, Macro> { };
		private readonly DB m_db;

		public MacroDB()
		{
			m_db = new DB();
		}

		/// <summary>Add a macro expansion to the db. Throws EResult.MacroAlreadyDefined if the definition is already defined and different to 'macro'</summary>
		public void Add(Macro macro)
		{
			if (m_db.TryGetValue(macro.m_tag, out var existing))
			{
				if (existing == macro) return; // Already defined, but the same definition... allowed
				throw new ScriptException(EResult.MacroAlreadyDefined, macro.m_loc, "macro already defined");
			}
			m_db.Add(macro.m_tag, macro);
		}

		/// <summary>Remove a macro </summary>
		public void Remove(string tag)
		{
			m_db.Remove(tag);
		}

		/// <summary>Find a macro expansion for a given macro identifier. Returns null if no macro is found.</summary>
		public Macro? Find(string tag)
		{
			return m_db.TryGetValue(tag, out var m) ? m : null;
		}
	}

	/// <summary>Macro handler that doesn't handle macros</summary>
	public class NoMacros :IMacroHandler
	{
		public NoMacros()
		{ }

		/// <summary>
		/// Add a macro expansion to the db.
		/// Throw EResult::MacroAlreadyDefined if the definition is already defined and different to 'macro'</summary>
		public void Add(Macro macro)
		{
			throw new ScriptException(EResult.MacrosNotSupported, macro.m_loc, "macros are not supported");
		}

		/// <summary>Remove a macro</summary>
		public void Remove(string tag)
		{
			throw new ScriptException(EResult.MacrosNotSupported, new Loc(), "macros are not supported");
		}

		/// <summary>Find a macro expansion for a given macro identifier. Returns null if no macro is found.</summary>
		public Macro? Find(string tag)
		{
			throw new ScriptException(EResult.MacrosNotSupported, new Loc(), "macros are not supported");
		}
	}
}
