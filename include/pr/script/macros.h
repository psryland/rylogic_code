//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/script_core.h"
#include "pr/script/location.h"
#include "pr/script/fail_policy.h"

namespace pr::script
{
	// A preprocessor macro definition
	struct Macro
	{
		using Params = pr::vector<string_t, 5>;

		string_t m_tag;       // The macro tag
		string_t m_expansion; // The substitution text
		Params   m_params;    // Parameters for the macro, empty() for no parameter list, [0]="" for empty parameter list 'TAG()'
		Loc      m_loc;       // The source location of where the macro was defined

		Macro()
			:m_tag()
			,m_expansion()
			,m_params()
			,m_loc()
		{}

		// Construct a simple #define TWO 2 style macro
		explicit Macro(string_view_t tag, string_view_t expansion, Params const& params = {}, Loc const& loc = Loc())
			:m_tag(tag)
			,m_expansion(expansion)
			,m_params(params)
			,m_loc(loc)
		{}

		// Construct a function style macro of the form: 'TAG(p0,p1,..,pn) expansion...'
		// from a stream of characters. Stops at the first non-escaped new line
		template <typename Iter> explicit Macro(Iter& src, Loc const& loc = Loc())
			:Macro()
		{
			// Extract the tag and find it's hash code
			if (!str::ExtractIdentifier(m_tag, src))
				throw ScriptException(EResult::InvalidIdentifier, loc, "invalid macro name");

			// Extract the optional parameter identifiers
			if (*src == '(')
				ReadParams<true>(src, m_params, loc);

			// Trim whitespace from before the expansion text
			EatLineSpace(src, 0, 0);

			// Extract the expansion and trim all leading and following whitespace
			str::ExtractLine(m_expansion, src, true);
			str::Trim(m_expansion, str::IsWhiteSpace<char_t>, true, true);
		}

		// Extract a comma separated parameter list of the form '(p0,p1,..,pn)'
		// If 'identifiers' is true then the parameters are expected to be identifiers.
		// If not, then anything delimited by commas is accepted. 'identifiers' == true is used when
		// reading the definition of the macro, 'identifiers' == false is used when expanding an instance.
		// If an empty parameter list is given, i.e. "()" then 'params' is returned containing one blank parameter.
		// Returns true if the macro does not take parameters or the correct number
		// of parameters where given, false if the macro takes parameters but none were given
		// Basically, 'false' means, don't treat this macro as matching because no params were given
		// If false is returned the buffer will contain anything read during this method.
		template <bool Identifiers, typename Iter>
		bool ReadParams(Iter& src, Params& params, Loc const& loc) const
		{
			params.resize(0);

			// Buffer up to the first non-whitespace character
			// If no parameters are given, then the macro doesn't match
			if constexpr (!Identifiers)
			{
				// If we're not reading the parameter names for a macro definition
				// and the macro takes no parameters, then ReadParams is a no-op
				if (!m_params.empty())
				{
					int i = 0; for (; str::IsWhiteSpace(src[i]); ++i) {}
					if (src[i] != '(') return false;
					src += i;
				}
				if (m_params.empty())
					return true;
			}

			string_t param;

			// Capture the strings between commas as the parameters
			for (++src; *src != ')'; src += *src != ')')
			{
				// Read parameter names for a macro definition
				if constexpr (Identifiers)
				{
					param.resize(0);
					if (!str::ExtractIdentifier(param, src))
						throw ScriptException(EResult::InvalidIdentifier, loc, "invalid macro identifier");
				}

				// Read parameters being passed to the macro
				else
				{
					param.resize(0);
					for (int nest = 0; nest || (*src != ',' && *src != ')'); ++src)
					{
						if (*src == '\0') throw ScriptException(EResult::UnexpectedEndOfFile, loc, "macro parameter list incomplete");
						param.push_back(*src);
						nest += *src == '(';
						nest -= *src == ')';
					}
				}

				// Save the parameter or parameter name
				params.push_back(param);
			}

			// Skip over the ')'
			assert(*src == ')');
			++src;

			// Add a blank param to distinguish between "TAG()" and "TAG"
			if (params.empty())
				params.push_back(L"");

			// Check enough parameters have been given
			if constexpr (!Identifiers)
			{
				if (m_params.size() != params.size())
					throw ScriptException(EResult::ParameterCountMismatch, loc, "incorrect number of macro parameters");
			}

			return true;
		}

		// Expands the macro into the string 'exp' with the text of this macro including substituted parameter text
		void Expand(string_t& exp, Params const& params, Loc const& loc) const
		{
			if (params.size() != m_params.size())
				throw ScriptException(EResult::ParameterCountMismatch, loc, "macro parameter count mismatch");

			// Set the string to the macro text initially
			exp = m_expansion;

			// Substitute each parameter
			for (size_t i = 0; i != m_params.size(); ++i)
			{
				auto const& what = m_params[i];
				auto len = what.size();
				if (len == 0) continue;

				// Replace the instances of 'what' with 'with'
				string_t with;
				for (size_t j = str::FindIdentifier(exp,what,0); j != exp.size(); j = str::FindIdentifier(exp,what,j+=len))
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
						str::Replace(with, "\"", "\\\"");
						str::Quotes(with, true);
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

		// Operators
		friend bool operator == (Macro const& lhs, Macro const& rhs)
		{
			return
				lhs.m_params.size() == rhs.m_params.size() &&
				lhs.m_expansion == rhs.m_expansion;
		}
		friend bool operator != (Macro const& lhs, Macro const& rhs)
		{
			return !(lhs == rhs);
		}

		// Helper for recursive expansion of macros
		struct Ancestor
		{
			// Notes:
			//  - A macro will not be expanded if the same macro has already been expanded earlier in the recursion
			Macro const*    m_macro;
			Ancestor const* m_parent;

			Ancestor(Macro const* macro, Ancestor const* parent) noexcept
				:m_macro(macro)
				,m_parent(parent)
			{}

			// Returns true if 'm' is an ancestor of itself
			bool IsRecursive(Macro const* macro) const
			{
				auto p = this;
				for (; p && p->m_macro != macro; p = p->m_parent) {}
				return p != nullptr;
			}
		};
	};

	// Interface/Base class for the preprocessor macro handler
	struct IMacroHandler
	{
		virtual ~IMacroHandler() {}

		// Add a macro expansion to the db. Throws EResult::MacroAlreadyDefined if the definition is already defined and different to 'macro'
		virtual void Add(Macro const& macro) = 0;

		// Remove a macro
		virtual void Remove(string_t const& tag) = 0;

		// Find a macro expansion for a given macro tag. Returns nullptr if no macro is found.
		virtual Macro const* Find(string_t const& tag) const = 0;
	};

	// A collection of preprocessor macros
	struct MacroDB :IMacroHandler
	{
		// Notes:
		//  - I was using a hash of the macro tag as the map key but there is no optimal way of handling
		//    key collisions. In the end, unordered_map with a string key is pretty good.
		//  - I've considered making 'DB' a sorted vector, but any memory locality benefits are lost
		//    because of the strings.
		//  - Using a string_view as the map key would work if the string view pointed to the 'm_tag' of
		//    macro instance stored in the map, however there doesn't seem to be a way to do that.
		//  - To programmatically define macros, subclass this type and extend the 'Find' method.

		// The database of macro definitions
		using DB = std::unordered_map<string_t, Macro>;
		DB m_db;

		// Add a macro expansion to the db. Throws EResult::MacroAlreadyDefined if the definition is already defined and different to 'macro'
		void Add(Macro const& macro) override
		{
			auto i = m_db.find(macro.m_tag);
			if (i != std::end(m_db))
			{
				if (i->second == macro) return; // Already defined, but the same definition... allowed
				throw ScriptException(EResult::MacroAlreadyDefined, macro.m_loc, "macro already defined");
			}

			m_db.insert(i, DB::value_type(macro.m_tag, macro));
		}

		// Remove a macro
		void Remove(string_t const& tag) override
		{
			auto i = m_db.find(tag);
			if (i != std::end(m_db))
				m_db.erase(i);
		}

		// Find a macro expansion for a given macro identifier
		// Returns nullptr if no macro is found.
		Macro const* Find(string_t const& tag) const override
		{
			auto i = m_db.find(tag);
			return i != std::end(m_db) ? &i->second : nullptr;
		}
	};
}
