//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/prlistinanarray.h"

namespace TestListInAnArray
{
	using namespace pr;

	struct Thing
	{
		Thing() {}
		Thing*	m_next;
	};

	void Run()
	{
		ListInAnArray<Thing> list(10);
	}
}//namespace TestListInAnArray
