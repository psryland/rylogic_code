//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/script_core.h"
#include "pr/script/location.h"
#include "pr/script/fail_policy.h"

namespace pr
{
	namespace script
	{
		// A preprocessor macro definition
		struct Macro
		{
			using Params = pr::vector<string, 5>;

			// Helper for recursive expansion of macros
			// A macro will not be expanded if the same macro has already been expanded earlier in the recursion
			struct Ancestor
			{
				Macro const*    m_macro;
				Ancestor const* m_parent;

				Ancestor(Macro const* macro, Ancestor const* parent)
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

			string    m_tag;       // The macro tag
			string    m_expansion; // The substitution text
			Params    m_params;    // Parameters for the macro, empty() for no parameter list, [0]="" for empty parameter list 'TAG()'
			HashValue m_hash;      // The hash of the macro tag
			FileLoc   m_loc;       // The source location of where the macro was defined

			// Return a macro tag from 'src' (or fail)
			template <typename Iter, typename FailPolicy = ThrowOnFailure>
			static string ReadTag(Iter& src, Location const& loc)
			{
				string tag;
				if (!pr::str::ExtractIdentifier(tag, src))
					FailPolicy::Fail(EResult::InvalidIdentifier, loc, "invalid macro name");
				return tag;
			}

			// Construct a simple #define TWO 2 style macro
			Macro(wchar_t const* tag = L"", wchar_t const* expansion = L"", Params const& params = Params(), FileLoc const& loc = FileLoc())
				:m_tag(tag)
				,m_expansion(expansion)
				,m_params(params)
				,m_hash(Hash(m_tag))
				,m_loc(loc)
			{}

			// Construct a preprocessor macro of the form: TAG(p0,p1,..,pn) expansion
			// from a stream of characters. Stops at the first non-escaped new line
			template <typename Iter> explicit Macro(Iter& src, Location const& loc)
				:Macro(L"", L"", Params(), loc)
			{
				// Extract the tag and find it's hash code
				m_tag = ReadTag(src, loc);

				// Hash the tag
				m_hash = Hash(m_tag);

				// Extract the optional parameters
				if (*src == '(')
					ReadParams<true>(src, m_params, loc);

				// Trim whitespace from before the expansion text
				for (; *src &&  pr::str::IsLineSpace(*src); ++src) {}

				// Extract the expansion and trim all leading and following whitespace
				pr::str::ExtractLine(m_expansion, src, true);
				pr::str::Trim(m_expansion, pr::str::IsWhiteSpace<wchar_t>, true, true);
			}

			// Extract a comma separated parameter list of the form "(p0,p1,..,pn)"
			// If 'identifiers' is true then the parameters are expected to be identifiers.
			// If not, then anything delimited by commas is accepted. 'identifiers' == true is used when
			// reading the definition of the macro, 'identifiers' == false is used when expanding an instance.
			// If an empty parameter list is given, i.e. "()" then 'params' is returned containing one blank parameter.
			// Returns true if the macro does not take parameters or the correct number
			// of parameters where given, false if the macro takes parameters but none were given
			// Basically, 'false' means, don't treat this macro as matching because no params were given
			// If false is returned the buffer will contain anything read during this method.
			template <bool Identifiers, typename TBuf, typename FailPolicy = ThrowOnFailure>
			bool ReadParams(TBuf& buf, Params& params, Location const& loc) const
			{
				#pragma warning(push)
				#pragma warning(disable:4127) // constant conditional

				// Buffer up to the first non-whitespace character
				// If no parameters are given, then the macro doesn't match
				if (!Identifiers && !m_params.empty())
				{
					int i = 0; for (; pr::str::IsWhiteSpace(buf[i]); ++i) {}
					if (buf[i] != L'(') return false;
					buf += i;
				}

				// If we're not reading the parameter names for a macro definition
				// and the macro takes no parameters, then ReadParams is a no-op
				if (!Identifiers && m_params.empty())
					return true;

				// Capture the strings between commas as the parameters
				string param;
				params.resize(0);
				for (++buf; *buf != L')'; buf += *buf != L')')
				{
					// Read parameter names for a macro definition
					if (Identifiers)
					{
						param.resize(0);
						if (!pr::str::ExtractIdentifier(param, buf))
							return FailPolicy::Fail(EResult::InvalidIdentifier, loc, "invalid macro identifier"), false;
					}

					// Read parameters being passed to the macro
					else
					{
						for (int nest = 0; (*buf != L',' && *buf != L')') || nest; ++buf)
						{
							if (*buf == 0) return FailPolicy::Fail(EResult::UnexpectedEndOfFile, loc, "macro parameter list incomplete"), false;
							param.push_back(*buf);
							nest += *buf == L'(';
							nest -= *buf == L')';
						}
					}

					// Save the parameter or paramater name
					params.push_back(param);
					param.resize(0);
				}

				// Skip over the ')'
				++buf;

				// Add a blank param to distingush between "TAG()" and "TAG"
				if (params.empty())
					params.push_back(L"");

				// Check enough parameters have been given
				if (!Identifiers && m_params.size() != params.size())
					return FailPolicy::Fail(EResult::ParameterCountMismatch, loc, "incorrect number of macro parameters"), false;

				return true;
				#pragma warning(pop)
			}

			// Expands the macro into the string 'exp' with the text of this macro including substituted parameter text
			void Expand(string& exp, Params const& params) const
			{
				assert(params.size() == m_params.size() && "macro parameter count mismatch");

				// Set the string to the macro text initially
				exp = m_expansion;

				// Substitute each parameter
				for (size_t i = 0; i != m_params.size(); ++i)
				{
					auto const& what = m_params[i];
					if (what.empty()) continue;
					size_t len = what.size();

					// Replace the instances of 'what' with 'with'
					string with;
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
		};
		inline bool operator == (Macro const& lhs, Macro const& rhs)
		{
			return
				lhs.m_hash == rhs.m_hash &&
				lhs.m_params.size() == rhs.m_params.size() &&
				lhs.m_expansion == rhs.m_expansion;
		}
		inline bool operator != (Macro const& lhs, Macro const& rhs)
		{
			return !(lhs == rhs);
		}

		// Interfase/Base class for the preprocessor macro handler
		struct IMacroHandler
		{
			virtual ~IMacroHandler() {}

			// Add a macro expansion to the db.
			// Throw EResult::MacroAlreadyDefined if the definition is already defined and different to 'macro'
			virtual void Add(Macro const& macro) = 0;

			// Remove a macro (by hashed name)
			virtual void Remove(HashValue hash) = 0;

			// Find a macro expansion for a given macro identifier (hashed)
			// Returns nullptr if no macro is found.
			virtual Macro const* Find(HashValue hash) const = 0;
		};

		// A collection of preprocessor macros
		// Note: to programmatically define macros, subclass this type and extend the 'Find' method.
		template <typename FailPolicy = ThrowOnFailure>
		struct MacroDB :IMacroHandler
		{
			// The database of macro definitions
			using DB = std::unordered_map<HashValue, Macro>;
			DB m_db;

			// Add a macro expansion to the db.
			// Throw EResult::MacroAlreadyDefined if the definition is already defined and different to 'macro'
			void Add(Macro const& macro) override
			{
				auto i = m_db.find(macro.m_hash);
				if (i != std::end(m_db))
				{
					if (i->second == macro) return; // Already defined, but the same definition... allowed
					return FailPolicy::Fail(EResult::MacroAlreadyDefined, macro.m_loc, "macro already defined");
				}

				m_db.insert(i, DB::value_type(macro.m_hash, macro));
			}

			// Remove a macro (by hashed name)
			void Remove(HashValue hash) override
			{
				auto i = m_db.find(hash);
				if (i != std::end(m_db))
					m_db.erase(i);
			}

			// Find a macro expansion for a given macro identifier (hashed)
			// Returns nullptr if no macro is found.
			Macro const* Find(HashValue hash) const override
			{
				auto i = m_db.find(hash);
				return i != std::end(m_db) ? &i->second : nullptr;
			}
		};
	}
}
