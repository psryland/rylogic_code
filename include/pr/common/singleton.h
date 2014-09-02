//***********************************************************************
// Singleton
//  Copyright (c) Rylogic Ltd 2003
//***********************************************************************
// Usage:
//	class Thing1 : public Singleton<Thing1>
//	{
//	public:
//		void DoStuff()	{ printf("Doin stuff\n"); }
//	};
//
//	class Thing2
//	{
//	public:
//		void DoStuff()	{ printf("Doin stuff\n"); }
//	};
//
//	void main(void)
//	{
//		Thing1::Get().DoStuff();
//		Singleton<Thing2>::DoStuff();
//
//		// Optional. Once delete has been called the singleton may not be created again
//		Thing1::Delete();
//		Singleton<Thing2>::Delete();
//	}
//
#ifndef PR_SINGLETON_H
#define PR_SINGLETON_H

#include "pr/common/assert.h"

namespace pr
{
	// Singleton
	template <typename T>
	class Singleton
	{
	public:
		static T& Get();
		static void Delete();

	protected:
		Singleton() {}
		Singleton(const Singleton&);
		
	private:
		static T*   m_singleton;
		static bool m_has_been_deleted;
	};

	//******
	// Instanciate static members
	template <typename T> T*   Singleton<T>::m_singleton = 0;
	template <typename T> bool Singleton<T>::m_has_been_deleted = false;

	//*****
	// Return a reference to the singleton object
	template <typename T>
	inline T& Singleton<T>::Get()
	{
		if( m_singleton == 0 )
		{
			// If this asset fires then you've tried to use this
			// singleton after it has been deleted.
			PR_ASSERT(PR_DBG, !m_has_been_deleted, "");

			m_singleton = new T();
			atexit(&Delete);
		}
		return *m_singleton;
	}

	//*****
	// Delete the singleton object
	template <typename T>
	inline void Singleton<T>::Delete()
	{
		delete m_singleton;
		m_singleton = 0;
		m_has_been_deleted = true;	// Ensure deleted once only
	}
}//namespace pr

#endif//PR_SINGLETON_H