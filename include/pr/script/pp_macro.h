//**********************************
// Preprocessor macro container
//  Copyright (c) Rylogic Ltd 2011
//**********************************
#pragma once
#ifndef PR_SCRIPT_PP_MACRO_H
#define PR_SCRIPT_PP_MACRO_H

#include "pr/script/script_core.h"
#include "pr/script/char_stream.h"

#pragma warning(push)
#pragma warning(disable:4127) // constant conditional

namespace pr
{
	namespace script
	{
		// A preprocessor macro definition
		struct PPMacro
		{
			typedef pr::vector<string,5> Params;

			pr::hash::HashValue m_hash;      // The hash of the macro tag
			string              m_tag;       // The macro tag
			Params              m_params;    // Parameters for the macro, empty() for no parameter list, [0]="" for empty parameter list 'TAG()'
			string              m_expansion; // The substitution text
			Loc                 m_loc;       // The source location of where the macro was defined

			PPMacro()
				:m_hash()
				,m_tag()
				,m_params()
				,m_expansion()
				,m_loc()
			{}

			// Construct a simple macro expansion
			explicit PPMacro(pr::hash::HashValue hash, char const* expansion = "", Params const& params = Params(), Loc const& loc = Loc())
				:m_hash(hash)
				,m_tag()
				,m_params(params)
				,m_expansion(expansion)
				,m_loc(loc)
			{}
			explicit PPMacro(char const* tag, char const* expansion = "", Params const& params = Params(), Loc const& loc = Loc())
				:m_hash(pr::hash::HashC(tag))
				,m_tag(tag)
				,m_params(params)
				,m_expansion(expansion)
				,m_loc(loc)
			{}

			// Construct the preprocessor macro of the form: TAG(p0,p1,..,pn) expansion
			// from a stream of characters. Stops at the first non-escaped new line
			template <typename Iter> explicit PPMacro(Iter& src, Loc const& loc)
				:m_hash()
				,m_tag()
				,m_params()
				,m_expansion()
				,m_loc(loc)
			{
				// Extract the tag and find it's hash code
				if (!pr::str::ExtractIdentifier(m_tag, src))
					throw Exception(EResult::InvalidIdentifier, loc, "invalid macro name");

				m_hash = Hash::String(m_tag);

				// Extract the optional parameters
				if (*src == '(')
					ReadParams<true>(src, m_params, loc);

				Eat::LineSpace(src);
				
				// Extract the expansion and trim all leading and following whitespace
				CommentStrip cs(src);
				pr::str::ExtractLine(m_expansion, cs, true);
				pr::str::Trim(m_expansion, pr::str::IsWhiteSpace<char>, true, true);
			}

			// Populates the string 'exp' with the text of this macro including substituted parameter text
			void GetSubstString(string& exp, Params const& params) const
			{
				PR_ASSERT(PR_DBG, params.size() == m_params.size(), "macro parameter count mismatch");

				// Set the string to the macro text initially
				exp = m_expansion;

				// Substitute each parameter
				for (size_t i = 0; i != m_params.size(); ++i)
				{
					string const& what = m_params[i];
					if (what.empty()) continue;

					string with;
					size_t len = what.size();
					for (size_t j = pr::str::FindIdentifier(exp,what,0); j != exp.size(); j = pr::str::FindIdentifier(exp,what,j+=len))
					{
						// If the identifier is prefixed with '##' then just remove the '##'
						// this will have the effect of concatenating the substituted strings.
						if (j >= 2 && exp[j-1] == '#' && exp[j-2] == '#')
						{
							j -= 2;
							len += 2;
							with = params[i];
						}

						// If the identifier is prefixed with '#' then replace 'what' with 'with' as a literal string
						else if (j >= 1 && exp[j-1] == '#')
						{
							j -= 1; len += 1;
							with = params[i];
							pr::str::Replace(with, "\"", "\\\"");
							pr::str::Quotes(with, true);
						}

						// Otherwise, normal substitution
						else
						{
							with = params[i];
						}

						// Do the substitution
						exp.erase (j, len);
						exp.insert(j, with);
						j += with.size() - len;
					}
				}
			}

			// Extract a comma separated parameter list of the form "(p0,p1,..,pn)"
			// If 'identifiers' is true then the parameters are expected to be identifiers.
			// If not, then anything delimited by commas is accepted.
			// If an empty parameter list is given, i.e. "()" then 'params' is returned
			// containing one blank parameter.
			// Returns true if the macro does not take parameters or the current number
			// of parameters where given, false if the macro takes parameters but none were given
			// Basically, 'false' means, don't treat this macro as matching because no params were given
			// If false is returned the buffer will contain anything read during this method.
			template <bool Identifiers, typename TBuf> bool ReadParams(TBuf& buf, Params& params, Loc const& loc) const
			{
				// Buffer up to the first non-whitespace character
				// If no parameters are given, then the macro doesn't
				// match leave the contents of 'buf' buffered.
				if (!Identifiers && !m_params.empty())
				{
					for (; pr::str::IsWhiteSpace(*buf.m_src); buf.buffer()) {}
					if (*buf.m_src != '(') return false;
				}

				buf.clear();

				// If we're not reading the identifiers for a macro and the macro takes no parameters, then all good.
				if (!Identifiers && m_params.empty())
					return true;

				string param;
				params.resize(0);
				for (++buf; *buf != ')'; buf += *buf != ')')
				{
					if (Identifiers)
					{
						if (!pr::str::ExtractIdentifier(param, buf))
							throw Exception(EResult::InvalidIdentifier, loc, "invalid macro identifier");
					}
					else
					{
						for (int nest = 0; (*buf != ',' && *buf != ')') || nest; ++buf)
						{
							if (*buf == 0) throw Exception(EResult::UnexpectedEndOfFile, loc, "macro parameter list incomplete");
							param.push_back(*buf);
							nest += *buf == '(';
							nest -= *buf == ')';
						}
					}
					params.push_back(param);
					param.resize(0);
				}
				++buf;
				if (params.empty()) params.push_back(""); // add a blank param to distingush between "TAG()" and "TAG"
				if (!Identifiers && m_params.size() != params.size())
					throw Exception(EResult::ParameterCountMismatch, loc, "incorrect number of macro parameters");
				return true;
			}
		};

		inline bool operator == (PPMacro const& lhs, PPMacro const& rhs) { return lhs.m_hash == rhs.m_hash && lhs.m_params.size() == rhs.m_params.size() && lhs.m_expansion == rhs.m_expansion; }
		inline bool operator != (PPMacro const& lhs, PPMacro const& rhs) { return !(lhs == rhs); }

		// Helper for recursive expansion of macros
		struct PPMacroAncestor
		{
			PPMacro const*         m_macro;
			PPMacroAncestor const* m_parent;
			PPMacroAncestor() :m_macro(0) ,m_parent(0) {}
			PPMacroAncestor(PPMacro const* macro, PPMacroAncestor const* parent) :m_macro(macro) ,m_parent(parent) {}
		};
	}
}
#pragma warning(pop)
#endif
