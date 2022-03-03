//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/mempool.h"

namespace TestMemPool
{
	using namespace pr;
	
	struct POD
	{
		POD()								{ printf("Default Construct POD: %X\n", m_value); }
		POD(uint value) : m_value(value)	{ printf("Construct POD: %X\n", m_value); }
		~POD()								{ printf("Desstruct POD: %X\n", m_value); }
		uint	m_value;
		POD*	m_next;
	};

	struct NonPOD
	{
		NonPOD()							{ printf("Default Construct NonPOD: %X\n", m_value); }
		NonPOD(uint value) : m_value(value)	{ printf("Construct NonPOD: %X\n", m_value); }
		~NonPOD()							{ printf("Destruct NonPOD: %X\n", m_value); }
		uint	m_value;
		NonPOD*	m_next;
	};

	void Run()
	{
		{
			MemPool<POD> Pool;
			MemPool<POD> PoolCopy;
			PoolCopy = Pool;
			POD* p[10];
			Pool.SetNumberOfObjectsPerBlock(4);
			for( uint i = 0; i < 10; ++i )
				p[i] = Pool.Get();
			Pool.Return(p[0]);
			Pool.GetNumberOfObjectsPerBlock();
			Pool.GetNumberOfFreeObjects();
			Pool.GetNumberOfAllocatedObjects();
			Pool.AllObjectsReturned();
			Pool.ReclaimAll();
			Pool.AllObjectsReturned();
			Pool.ReleaseMemory();
		}
		{
			MemPool<NonPOD> Pool;
			MemPool<NonPOD> PoolCopy(Pool);
			NonPOD* p[10];
			Pool.SetNumberOfObjectsPerBlock(4);
			for( uint i = 0; i < 10; ++i )
				p[i] = Pool.Get();
			Pool.Return(p[0]);
			Pool.GetNumberOfObjectsPerBlock();
			Pool.GetNumberOfFreeObjects();
			Pool.GetNumberOfAllocatedObjects();
			Pool.AllObjectsReturned();
			Pool.ReclaimAll();
			Pool.AllObjectsReturned();
			Pool.ReleaseMemory();
		}
	}
}//namespace MemPool
