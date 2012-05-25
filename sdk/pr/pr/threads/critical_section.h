//**********************************
// Critical Section
//  Copyright © Rylogic Ltd 2007
//**********************************

#ifndef PR_THREADS_CRITICAL_SECTION_H
#define PR_THREADS_CRITICAL_SECTION_H

#include <windows.h>

//#define PR_CRIT_SECTION_CALL_STACK
#ifdef PR_CRIT_SECTION_CALL_STACK
#   include "pr/common/fmt.h"
#   include "pr/common/stackdump.h"
#   define PR_CRIT_SECT_CS(exp) exp
#else
#   define PR_CRIT_SECT_CS(exp)
#endif

namespace pr
{
	namespace threads
	{
		struct CritSection :CRITICAL_SECTION
		{
			CritSection()  { InitializeCriticalSection(this); }
			~CritSection() { DeleteCriticalSection(this); }
			void Enter()
			{
				::EnterCriticalSection(this);
				PR_CRIT_SECT_CS(void (__thiscall pr::CritSection::* f)(void)  = &CritSection::Enter; f;)
				PR_CRIT_SECT_CS(m_call_stack = ""; StackDump(*this));
			}
			void Leave()
			{
				::LeaveCriticalSection(this);
			}
			
			PR_CRIT_SECT_CS(void operator ()(const CallAddress& addr))
			PR_CRIT_SECT_CS({ CallSource src = GetCallSource(addr); m_call_stack += Fmt("%s(%d) ", src.m_filename.c_str(), src.m_line); })
			PR_CRIT_SECT_CS(std::string m_call_stack);
			
		private:
			CritSection(CritSection const&);
			void operator =(CritSection const&);
		};
		
		// RAII lock for a CS
		struct CSLock
		{
			CRITICAL_SECTION* m_cs;
			explicit CSLock(CritSection& cs) :m_cs(&cs)     { static_cast<CritSection*>(m_cs)->Enter(); }
			explicit CSLock(CRITICAL_SECTION* cs) :m_cs(cs) { static_cast<CritSection*>(m_cs)->Enter(); }
			~CSLock()                                       { static_cast<CritSection*>(m_cs)->Leave(); }
		private:
			CSLock(CSLock const&);
			CSLock& operator=(CSLock const&);
		};
	}
}
	
#endif
