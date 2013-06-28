//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/assert.h"
#include "pr/meta/or.h"
#include "pr/meta/and.h"
#include "pr/meta/isclass.h"
#include "pr/meta/ispod.h"

namespace TestMeta
{
	using namespace pr::meta;

	// or_
	PR_STATIC_ASSERT(  (or_<true_ , true_ >::value) );
	PR_STATIC_ASSERT(  (or_<false_, true_ >::value) );
	PR_STATIC_ASSERT(  (or_<true_ , false_>::value) );
	PR_STATIC_ASSERT( !(or_<false_, false_>::value) );

	// and_
	PR_STATIC_ASSERT(  (and_<true_ , true_ >::value) );
	PR_STATIC_ASSERT( !(and_<false_, true_ >::value) );
	PR_STATIC_ASSERT( !(and_<true_ , false_>::value) );
	PR_STATIC_ASSERT( !(and_<false_, false_>::value) );

	// is_class
	struct Class {};
	PR_STATIC_ASSERT(  is_class<Class>::value );
	PR_STATIC_ASSERT( !is_class<int>::value   );

	// is_pod
	struct POD			{ struct fuz_is_pod { enum { value = true  }; }; struct pr_is_pod { enum { value = true  }; }; };
	struct NotPOD		{ struct fuz_is_pod { enum { value = false }; }; struct pr_is_pod { enum { value = false }; }; };
	int    PODnotag;
	struct NotPODnotag	{ };
	PR_STATIC_ASSERT(  is_pod<POD>::value         );
	PR_STATIC_ASSERT( !is_pod<NotPOD>::value      );
	PR_STATIC_ASSERT(  is_pod<int>::value         );
	PR_STATIC_ASSERT( !is_pod<NotPODnotag>::value );

	void Run()
	{}
}//namespace TestMeta
