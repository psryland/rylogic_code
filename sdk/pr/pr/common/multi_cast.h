//******************************************
// Multicast
//  Copyright © Oct 2011 Rylogic Ltd
//******************************************
	
#ifndef PR_MULTI_CAST_H
#define PR_MULTI_CAST_H
#pragma once
	
#include <vector>
#include "pr/threads/critical_section.h"
	
namespace pr
{
	// Another variation on a multicast delegate
	// Use:
	//  Note: doesn't have to be an interface necessarily
	//  struct IThing { virtual void Func(int params) = 0; };
	//  pr::MultiCast<IThing*> OnThingHappens; // clients can +=, -= onto this
	// To invoke the delegate:
	//  pr::MultiCast<IEvent*>::Lock lock(OnThingHappens);
	//  for (pr::MultiCast<IThing*>::iter i = lock.begin(), iend = lock.end(); i != iend; ++i)
	//     (*i)->Func(params);
	template <typename Type> class MultiCast
	{
		typedef std::vector<Type> TypeCont; TypeCont m_cont;
		pr::threads::CritSection m_cs;
		
	public:
		typedef typename TypeCont::const_iterator citer;
		typedef typename TypeCont::iterator       iter;
		
		// A lock context for accessing the clients
		class Lock
		{
			MultiCast<Type>&    m_mc;
			pr::threads::CSLock m_lock;
			Lock(Lock const&);
			Lock& operator =(Lock const&);
		public:
			Lock(MultiCast& mc) :m_mc(mc) ,m_lock(mc.m_cs) {}
			citer begin() const { return m_mc.m_cont.begin(); }
			iter  begin()       { return m_mc.m_cont.begin(); }
			citer end() const   { return m_mc.m_cont.end(); }
			iter  end()         { return m_mc.m_cont.end(); }
		};
		
		void operator =  (Type client)
		{
			pr::threads::CSLock lock(m_cs);
			m_cont.resize(0);
			m_cont.push_back(client);
		}
		void operator += (Type client)
		{
			pr::threads::CSLock lock(m_cs);
			m_cont.push_back(client);
		}
		void operator -= (Type client)
		{
			pr::threads::CSLock lock(m_cs);
			for (TypeCont::const_iterator i = m_cont.begin(), iend = m_cont.end(); i != iend; ++i)
				if (*i == client) { m_cont.erase(i); break; }
		}
	};
}

#endif

