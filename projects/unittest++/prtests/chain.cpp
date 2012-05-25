//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/chain.h"

SUITE(PRChain)
{
	struct Member
	{
		int m_i;
		Member *m_next, *m_prev;
		Member(int i) :m_i(i) { pr::chain::Init(*this); }
	};
	TEST(MemberChains)
	{
		//Member const* i;
		Member m0(0), m1(1), m2(2);
		pr::chain::Insert(m2, m1);
		pr::chain::Insert(m1, m0);
		CHECK_EQUAL(3U, pr::chain::Size(m0));
		CHECK_EQUAL(3U, pr::chain::Size(m1));
		CHECK_EQUAL(3U, pr::chain::Size(m2));
		{
			pr::chain::Iter<Member> iter(m0);
			CHECK_EQUAL(0, iter->m_i); ++iter;
			CHECK_EQUAL(1, iter->m_i); ++iter;
			CHECK_EQUAL(2, iter->m_i); ++iter;
			CHECK(iter == 0);
		}
		
		Member m3(3), m4(4), m5(5);
		pr::chain::Insert(m5, m4);
		pr::chain::Insert(m4, m3);
		CHECK_EQUAL(3U, pr::chain::Size(m4));
		{
			pr::chain::Iter<Member> iter(m4);
			CHECK_EQUAL(4, iter->m_i); --iter;
			CHECK_EQUAL(3, iter->m_i); --iter;
			CHECK_EQUAL(5, iter->m_i); --iter;
			CHECK(iter == 0);
		}
		
		pr::chain::Remove(m5);
		CHECK_EQUAL(2U, pr::chain::Size(m3));
		CHECK_EQUAL(2U, pr::chain::Size(m4));
		
		pr::chain::Join(m0, m3);
		{
			pr::chain::Iter<Member> iter(m0);
			CHECK_EQUAL(0, iter->m_i); ++iter;
			CHECK_EQUAL(1, iter->m_i); ++iter;
			CHECK_EQUAL(2, iter->m_i); ++iter;
			CHECK_EQUAL(3, iter->m_i); ++iter;
			CHECK_EQUAL(4, iter->m_i); ++iter;
			CHECK(iter == 0);
		}
	}
	
	struct Field
	{
		int m_i;
		pr::chain::Link<Field> m_link;
		Field(int i) :m_i(i) { m_link.init(this); }
	};
	TEST(FieldChains)
	{
		pr::chain::Link<Field> head;
		Field f0(0), f1(1), f2(2);
		pr::chain::Insert(head, f0.m_link);
		pr::chain::Insert(head, f1.m_link);
		pr::chain::Insert(head, f2.m_link);
		{
			pr::chain::Link<Field>* i = head.begin();
			CHECK_EQUAL(0, i->m_owner->m_i); i = i->m_next;
			CHECK_EQUAL(1, i->m_owner->m_i); i = i->m_next;
			CHECK_EQUAL(2, i->m_owner->m_i); i = i->m_next;
			CHECK(i == &head);
		}
		
		Field f3(f2), f4(4); f4 = f3; // copy construct/assignment
		{
			pr::chain::Link<Field>* i = head.begin();
			CHECK_EQUAL(0, i->m_owner->m_i); i = i->m_next;
			CHECK_EQUAL(1, i->m_owner->m_i); i = i->m_next;
			CHECK_EQUAL(2, i->m_owner->m_i); i = i->m_next;
			CHECK_EQUAL(2, i->m_owner->m_i); i = i->m_next;
			CHECK_EQUAL(2, i->m_owner->m_i); i = i->m_next;
			CHECK(i == &head);
		}

	}
	//TEST(MixinChains)
	//{
	//	
	//}
}
