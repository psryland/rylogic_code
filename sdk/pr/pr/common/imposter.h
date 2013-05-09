//******************************************
// Imposter
//  Copyright © Rylogic Ltd 2010
//******************************************
// Usage:
//	struct MyType
//	{
//		int m_value;
//		MyType(int value) :m_value(value) {}
//	};
//	typedef pr::Imposter<MyType> MyTypeImpost;
//
//	pr::imposter::construct(impost, 5);
//	pr::imposter::destruct(impost);

#pragma once
#ifndef PR_IMPOSTER_H
#define PR_IMPOSTER_H

#include <new>
#include "pr/meta/alignmentof.h"
#include "pr/meta/alignedstorage.h"
#include "pr/meta/alignedtype.h"
#include "pr/meta/function_arity.h"
#include "pr/meta/enableif.h"
#include "pr/common/assert.h"

namespace pr
{
	namespace imposter
	{
		template <typename ImpostType> void destruct(ImpostType& imp)
		{
			PR_ASSERT(PR_DBG, imp.m_obj, "type not constructed state");
			imp.destruct();
		}

		template <typename ImpostType> void construct(ImpostType& imp)
		{
			PR_ASSERT(PR_DBG, imp.m_obj == 0, "type not in destructed state");
			new (&imp.get()) ImpostType::AliasType;
			imp.m_obj = &imp.get();
		}

		template <typename P0, typename ImpostType> void construct(ImpostType& imp, P0 const& p0)
		{
			PR_ASSERT(PR_DBG, imp.m_obj == 0, "type not in destructed state");
			new (&imp.get()) ImpostType::AliasType(const_cast<P0&>(p0));
			imp.m_obj = &imp.get();
		}

		template <typename P0, typename P1, typename ImpostType> void construct(ImpostType& imp, P0 const& p0, P1 const& p1)
		{
			PR_ASSERT(PR_DBG, imp.m_obj == 0, "type not in destructed state");
			new (&imp.get()) ImpostType::AliasType(const_cast<P0&>(p0), const_cast<P1&>(p1));
			imp.m_obj = &imp.get();
		}

		template <typename P0, typename P1, typename P2, typename ImpostType> void construct(ImpostType& imp, P0 const& p0, P1 const& p1, P2 const& p2)
		{
			PR_ASSERT(PR_DBG, imp.m_obj == 0, "type not in destructed state");
			new (&imp.get()) ImpostType::AliasType(const_cast<P0&>(p0), const_cast<P1&>(p1), const_cast<P2&>(p2));
			imp.m_obj = &imp.get();
		}

		template <typename P0, typename P1, typename P2, typename P3, typename ImpostType> void construct(ImpostType& imp, P0 const& p0, P1 const& p1, P2 const& p2, P3 const& p3)
		{
			PR_ASSERT(PR_DBG, imp.m_obj == 0, "type not in destructed state");
			new (&imp.get()) ImpostType::AliasType(const_cast<P0&>(p0), const_cast<P1&>(p1), const_cast<P2&>(p2), const_cast<P3&>(p3));
			imp.m_obj = &imp.get();
		}
	}

	template <typename Type> struct Imposter
	{
		typedef typename pr::mpl::aligned_storage<sizeof(Type), pr::mpl::alignment_of<Type>::value>::type Buffer;
		typedef typename Type AliasType;

		Buffer m_buf;   // The buffer containing the type
		Type* m_obj;    // A pointer to the type when it is constructed, otherwise 0 (helpful for debugging also)

		Imposter()
		:m_obj(0)
		{}
		Imposter(Imposter const& rhs)
		:m_obj(0)
		{
			if (rhs.m_obj)
				imposter::construct(*this, rhs.get());
		}
		~Imposter()
		{
			if (m_obj)
				destruct();
		}
		Imposter& operator = (Imposter const& rhs)
		{
			if (&rhs == this) return *this;
			PR_ASSERT(PR_DBG, (m_obj != 0) == (rhs.m_obj != 0), "assignment from/to a non-constructed object");
			if (m_obj) get() = rhs.get();
			return *this;
		}

		Type const& get() const         { return *reinterpret_cast<Type const*>(&m_buf); }
		Type&       get()               { return *reinterpret_cast<Type*>(&m_buf); }
		operator Type const& () const   { PR_ASSERT(PR_DBG, m_obj, ""); return get(); }
		operator Type&       ()         { PR_ASSERT(PR_DBG, m_obj, ""); return get(); }
		void destruct()                 { PR_ASSERT(PR_DBG, m_obj, ""); get().~Type(); m_obj = 0; }
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/filesys/filesys.h"
namespace pr
{
	namespace unittests
	{
		namespace imposter
		{
			struct MyType
			{
				int m_value;
				MyType(int value) :m_value(value) {}
			};
			typedef pr::Imposter<MyType> MyTypeImpost;

			int FuncByValue(MyType mt)
			{
				return mt.m_value;
			}
			int FuncByRef(MyType const& mt)
			{
				return mt.m_value;
			}
			int FuncByAddr(MyType const* mt)
			{
				return mt->m_value;
			}
		}

		PRUnitTest(pr_common_imposter)
		{
			using namespace pr::unittests::imposter;
			{//Construction
				MyTypeImpost impost;

				pr::imposter::construct(impost, 5);
				PR_CHECK(impost.get().m_value, 5);

				MyTypeImpost impost2 = impost;
				PR_CHECK(impost2.get().m_value, 5);

				MyTypeImpost impost3;
				//CHECK_ASSERT(impost3 = impost);
		
				pr::imposter::construct(impost3, 2);
				impost3 = impost;
				PR_CHECK(impost3.get().m_value, 5);

				PR_CHECK(FuncByValue(impost), 5);
				PR_CHECK(FuncByRef(impost2), 5);
			}
		}
	}
}
#endif


#endif

