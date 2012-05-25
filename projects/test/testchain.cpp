//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/PRTypes.h"
#include "pr/common/assert.h"
#include "pr/common/StdVector.h"
#include "pr/common/StdAlgorithm.h"
#include "pr/common/Chain.h"
#include "pr/common/PodChain.h"

namespace TestChain
{
	using namespace pr;
	using namespace pr::chain;

	struct Obj : chain::link<Obj>
	{
		Obj() { static int i = 0; m_i = i++; }
		int m_i;
	};
	inline bool operator < (const Obj& lhs, const Obj& rhs) { return lhs.m_i < rhs.m_i; }

	struct PhysicsObjects;
	struct Colliders;
	struct PhysicsObject : chain::link<PhysicsObject, PhysicsObjects>, chain::link<PhysicsObject, Colliders>
	{
		PhysicsObject() { static int i = 0; m_i = i++; }
		int m_i;
	};

	template <typename ChainHead>
	void Print(const ChainHead& head)
	{
		for( ChainHead::const_iterator i = head.begin(), i_end = head.end(); i != i_end; ++i )
		{
			printf("%d\n", i->m_i);
		}
		printf("\n");
	}

	struct MyStruct
	{
		int i;
		pod_chain::link m_link;
	};

	void Run()
	{
		{
			pod_chain::link chain_of_mystructs;
			chain_of_mystructs.init();

			MyStruct s1,s2,s3;
			s1.m_link.init(s1); s1.i = 1;
			s2.m_link.init(s2); s2.i = 2;
			s3.m_link.init(s3); s3.i = 3;
			pod_chain::insert(chain_of_mystructs, s3.m_link);
			pod_chain::insert(chain_of_mystructs, s2.m_link);
			pod_chain::insert(chain_of_mystructs, s1.m_link);
			for( pod_chain::link *s = chain_of_mystructs.begin(), *s_end = chain_of_mystructs.end(); s != s_end; s = s->m_next )
			{
				printf("pod chain: %d\n", s->owner<MyStruct>().i);
			}
			pod_chain::remove(s2.m_link);
			for( pod_chain::link *s = chain_of_mystructs.begin(), *s_end = chain_of_mystructs.end(); s != s_end; s = s->m_next )
			{
				printf("pod chain: %d\n", s->owner<MyStruct>().i);
			}
			pod_chain::remove(s1.m_link);
			for( pod_chain::link *s = chain_of_mystructs.begin(), *s_end = chain_of_mystructs.end(); s != s_end; s = s->m_next )
			{
				printf("pod chain: %d\n", s->owner<MyStruct>().i);
			}
			pod_chain::remove(s3.m_link);
			for( pod_chain::link *s = chain_of_mystructs.begin(), *s_end = chain_of_mystructs.end(); s != s_end; s = s->m_next )
			{
				printf("pod chain: %d\n", s->owner<MyStruct>().i);
			}
		}
		{
			Obj obj[10];

			typedef chain::head<Obj> TObjGroup;
			TObjGroup head, head2;

			head.push_back(obj[0]);
			head.push_back(obj[1]);
			head.push_back(obj[2]);
			head.push_back(obj[3]);
			head.push_back(obj[4]);
			head.push_back(obj[5]);

			head2.push_back(obj[6]);
			head2.push_back(obj[7]);
			head2.push_back(obj[8]);
			head2.push_back(obj[9]);
			Print(head);

			//Obj obj2[10];
			//for( int i = 9; i >= 0; --i )
			//{
			//	obj2[i] = obj[i];
			//}

			TObjGroup::iterator where = head.begin();
			++where;
			++where;
			head.splice(where, head2);

			Print(head);

			// Test re-entracy
			head.push_front(head.front());
			Print(head);
			head.push_back (head.back());
			Print(head);
			head.push_front(head.back());
			Print(head);
			head.push_back (head.front());
			Print(head);
		}

		{
			PhysicsObject obj[10];

			typedef chain::head<PhysicsObject, PhysicsObjects>	TPhysicsObjectGroup;
			typedef chain::head<PhysicsObject, Colliders>		TColliderObjects;
			typedef chain::head<PhysicsObject, Colliders>		TNonColliderObjects;
			
			TPhysicsObjectGroup	physics_objects;
			TColliderObjects	colliders;
			TNonColliderObjects	non_colliders;
			
			physics_objects.push_back(obj[0]);
			physics_objects.push_back(obj[1]);
			physics_objects.push_back(obj[2]);
			physics_objects.push_back(obj[3]);
			physics_objects.push_back(obj[4]);
			physics_objects.push_back(obj[5]);

			colliders      .push_back(obj[0]);
			colliders      .push_back(obj[1]);
			colliders      .push_back(obj[2]);
			colliders      .push_back(obj[3]);

			non_colliders  .push_back(obj[2]);
			non_colliders  .push_back(obj[3]);
			non_colliders  .push_back(obj[4]);
			non_colliders  .push_back(obj[5]);

			printf("Physics Objects\n");
			for( TPhysicsObjectGroup::const_iterator i = physics_objects.begin(), i_end = physics_objects.end(); i != i_end; ++i )
			{
				printf("%d\n", i->m_i);
			}

			printf("Colliders\n");
			for( TColliderObjects::iterator i = colliders.begin(), i_end = colliders.end(); i != i_end; ++i )
			{
				printf("%d\n", i->m_i);
			}

			printf("Non Colliders\n");
			for( TNonColliderObjects::const_iterator i = non_colliders.begin(), i_end = non_colliders.end(); i != i_end; ++i )
			{
				printf("%d\n", i->m_i);
			}
		}
	}
}//namespace TestChain

