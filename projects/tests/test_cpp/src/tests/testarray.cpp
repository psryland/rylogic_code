//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/PRArray.h"

namespace TestArray
{
	using namespace pr;

	struct POD
	{
		POD()											{ printf("Default Construct POD: %X\n", m_value); }
		POD(int value) : m_value(value)					{ printf("Construct POD: %X\n", m_value); }
		POD(const POD& copy) : m_value(copy.m_value)	{ printf("Copy Construct POD: %X\n", m_value); }
		~POD()											{ printf("Destruct POD: %X\n", m_value); }
		int m_value;
	};
	struct NonPOD
	{
		NonPOD()										{ printf("Default Construct NonPOD: %X\n", m_value); }
		NonPOD(int value) : m_value(value)				{ printf("Construct NonPOD: %X\n", m_value); }
		NonPOD(const POD& copy) : m_value(copy.m_value)	{ printf("Copy Construct NonPOD: %X\n", m_value); }
		~NonPOD()										{ printf("Destruct NonPOD: %X\n", m_value); }
		int m_value;
	};

	void Run()
	{
		{
			// Test POD types
			Array<POD, 5> PODarrayNone;
			Array<POD, 5> PODarray(10);
			Array<POD, 5> PODarrayCopy(PODarray);
			PR_ASSERT(1, !PODarray.empty());
			PODarray.reserve(15);
			PODarray.push_back(POD(0));
			PODarray.push_back_fast(POD(1));
			PODarray.insert(PODarray.begin(), POD(2));
			PODarray.resize(8);
			for( int i = 0; i != 8; ++i ) { PODarray[i].m_value = i; }
			PODarray.erase(4);
			PODarray.erase_fast(4);
			PODarray[5];
			PODarray.front();
			PODarray.back();
			PR_ASSERT(1, !PODarray.empty());
			PODarray.size();
			PODarray.clear();
		}

		{
			// Test NonPOD types
			Array<NonPOD, 5> NonPODarray(10, NonPOD(1));
			Array<NonPOD, 5> NonPODarrayCopy;
			NonPODarrayCopy = NonPODarray;
			NonPODarray.push_back(NonPOD(2));
			NonPODarray.insert(NonPODarray.end(), NonPOD(3));
			NonPODarray.resize(10);
			NonPODarray.erase(1);
			NonPODarray.pop_back();
			NonPODarray[0];
			NonPODarray.clear();
		}

		{
			Array<void*> PointerArray;
			PointerArray.push_back(0);
			PointerArray.push_back(0);
			PointerArray.push_back(0);
			PointerArray.push_back(0);
			PointerArray.push_back(0);
			PointerArray.pop_back();
			PointerArray.empty();
			PointerArray.clear();
		}
	}
}//namespace TestArray
