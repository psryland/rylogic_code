//**********************************
// Preprocessor macro container
//  Copyright (c) Rylogic Ltd 2011
//**********************************
#ifndef PR_SCRIPT_PP_MACRO_DB_H
#define PR_SCRIPT_PP_MACRO_DB_H
	
#include <map>
#include "pr/script/script_core.h"
#include "pr/script/pp_macro.h"
	
namespace pr
{
	namespace script
	{
		// Interface for a object that stores macro definitions
		struct IPPMacroDB
		{
			virtual ~IPPMacroDB() {}
			
			// Add a macro expansion to the db. Throw
			// EResult::MacroAlreadyDefined if the definition is already defined
			virtual void Add(pr::script::PPMacro const& macro) = 0;
			
			// Remove a macro (by hashed name)
			virtual void Remove(pr::hash::HashValue hash) = 0;
			
			// Find a macro expansion for a given macro identifier (hashed)
			// Returns 0 if no macro is found
			virtual pr::script::PPMacro const* Find(pr::hash::HashValue hash) const = 0;
		};
		
		// A default implementation of a macro database
		// Note: to programmatically define macros, subclass this type and extend the 'Find' method.
		struct PPMacroDB :IPPMacroDB
		{
			typedef std::map<pr::hash::HashValue, PPMacro> DB;
			DB m_db; // The database of macro definitions
			
			PPMacroDB() :m_db() {}
			
			// Add a macro expansion to the db. Throw
			// EResult::MacroAlreadyDefined if the definition is already defined
			void Add(PPMacro const& macro)
			{
				DB::const_iterator i = m_db.find(macro.m_hash);
				if (i != m_db.end()) throw Exception(EResult::MacroAlreadyDefined, macro.m_loc, "macro already defined");
				m_db.insert(i, DB::value_type(macro.m_hash, macro));
			}
			
			// Remove a macro (by hashed name)
			void Remove(pr::hash::HashValue hash)
			{
				DB::const_iterator i = m_db.find(hash);
				if (i != m_db.end()) m_db.erase(i);
			}
			
			// Find a macro expansion for a given macro identifier (hashed)
			// Returns 0 if no macro is found
			PPMacro const* Find(pr::hash::HashValue hash) const
			{
				DB::const_iterator i = m_db.find(hash);
				return (i != m_db.end()) ? &i->second : 0;
			}
			
		private:
			PPMacroDB(PPMacroDB const&);
			PPMacroDB& operator =(PPMacroDB const&);
		};
	}
}

#endif
