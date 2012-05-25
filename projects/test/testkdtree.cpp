//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/kdtree.h"

namespace TestKDTree
{
	using namespace pr;

	struct Thing
	{
		void Print() const								{ printf("\t%f %f\n", m_value[0], m_value[1]); }
		float& KDTreeValue(unsigned char split_axis)	{ return m_value[split_axis]; }
		float m_value[2];
		int m_num;
	};

	struct AccessorFunctions
	{
		static kdtree::AxisType	GetAxis  (const Thing& elem)						{ return elem.m_num; }
		static void				SetAxis  (      Thing& elem, kdtree::AxisType axis)	{ elem.m_num = axis; }
		static float			GetValue (const Thing& elem, kdtree::AxisType axis)	{ return elem.m_value[axis]; }
		static void				AddResult(const Thing& elem, float)					{ elem.Print(); }
	};

	void Run()
	{
		srand(0);

		int i;
		const int TEST_LENGTH = 1000;

		Thing thing[TEST_LENGTH];
		for( i = 0; i < TEST_LENGTH; ++i )
		{
			thing[i].m_num = i;
			thing[i].m_value[0] = rand() / static_cast<float>(RAND_MAX);
			thing[i].m_value[1] = rand() / static_cast<float>(RAND_MAX);
		}
		
		AccessorFunctions func;
		kdtree::Build<2>(thing, thing + TEST_LENGTH, func);
		
		printf("\n#Tree built *************************************************************\n");
		printf("Point all FFFFFF00\n{\n");
		for( i = 0; i < TEST_LENGTH; ++i )
		{
			thing[i].Print();
		}
		printf("}\n");

		kdtree::Search<2> search;
		search.m_where[0] = 0.5f;
		search.m_where[1] = 0.5f;
		search.m_radius   = 0.05f;
		kdtree::Find<2>(thing, thing + TEST_LENGTH, search, func);
	}
}//namespace KDTree
