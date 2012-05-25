//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
//#include "pr/common/event.h"
#include "pr/common/events.h"

SUITE(PREvents)
{
	struct Evt
	{
		mutable int m_order;
		Evt() :m_order(0) {}
	};
	struct Thing0 :pr::events::IRecv<Evt>
	{
		int m_recv;
		Thing0() :pr::events::IRecv<Evt>(0) ,m_recv() {}
		void OnEvent(Evt const& e) { m_recv = ++e.m_order; }
	};
	struct Thing1 :pr::events::IRecv<Evt>
	{
		int m_recv;
		Thing1() :pr::events::IRecv<Evt>(1) ,m_recv() {}
		void OnEvent(Evt const& e) { m_recv = ++e.m_order; }
	};
	TEST(IRecvEvents)
	{
		{
			Thing0 thing0;
			Thing1 thing1;
			pr::events::Send(Evt());
			CHECK_EQUAL(2, thing0.m_recv);
			CHECK_EQUAL(1, thing1.m_recv);
		}
		{
			Thing1 thing1;
			Thing0 thing0;
			pr::events::Send(Evt());
			CHECK_EQUAL(2, thing0.m_recv);
			CHECK_EQUAL(1, thing1.m_recv);
		}
		{
			Thing0 thing0;
			Thing1 thing1;
			pr::events::Send(Evt(), false);
			CHECK_EQUAL(1, thing0.m_recv);
			CHECK_EQUAL(2, thing1.m_recv);
		}
	}
	/*	struct Type0
	{
		pr::event<void(void)>		VoidVoid;
		pr::event<bool(void)>		BoolVoid;
		pr::event<void(int)>		VoidInt;
		pr::event<bool(int)>		BoolInt;
		pr::event<bool(int, float)>	BoolIntFloat;
	};
	struct Type1
	{
		bool called[5];

		Type1() :called()			{}
		void Member0()				{ called[0] = true; }
		bool Member1()				{ called[1] = true; return true; }
		void Member2(int)			{ called[2] = true; }
		bool Member3(int)			{ called[3] = true; return true; }
		bool Member4(int, float)	{ called[4] = true; return true; }
	}

	bool g_called[5] = {};

	void Func0()				{ g_called[0] = true; }
	bool Func1()				{ g_called[1] = true; return true; }
	void Func2(int)				{ g_called[2] = true; }
	bool Func3(int)				{ g_called[3] = true; return true; }
	bool Func4(int, float)		{ g_called[4] = true; return true; }

	TEST(Event)
	{
		Type0 type0;
		Type1 type1;

		type0.VoidVoid += &type1.Member0;
		type0.VoidVoid += &Func0;
		type0.VoidVoid();
		CHECK(type1.called[0]);
		CHECK(g_called[0]);

		type0.BoolVoid += &type1.Member1;
		type0.BoolVoid += &Func1;
		type0.BoolVoid();
		CHECK(type1.called[1]);
		CHECK(g_called[1]);

		type0.VoidInt += &type1.Member2;
		type0.VoidInt += &Func2;
		type0.VoidInt(1);
		CHECK(type1.called[2]);
		CHECK(g_called[2]);

		type0.BoolInt += &type1.Member3;
		type0.BoolInt += &Func3;
		type0.BoolInt(1);
		CHECK(type1.called[3]);
		CHECK(g_called[3]);
		
		type0.BoolIntFloat += &type1.Member4;
		type0.BoolIntFloat += &Func4;
		type0.BoolIntFloat(1,2.0f);
		CHECK(type1.called[4]);
		CHECK(g_called[4]);

		memset(type1.called, 0, sizeof(type1.called));
		memset(g_called, 0, sizeof(g_called));

		type0.VoidVoid  = &type1.Member0;
		type0.VoidVoid();
		CHECK(type1.called[0]);
		CHECK(!g_called[0]);

		type0.BoolVoid -= &type1.Member1;
		type0.BoolVoid();
		CHECK(!type1.called[1]);
		CHECK(g_called[1]);

		type0.VoidInt -= &type1.Member2;
		type0.VoidInt -= &Func2;
		type0.VoidInt();
		CHECK(!type1.called[2]);
		CHECK(!g_called[2]);

		type0.BoolInt -= &Func3;
		type0.BoolInt();
		CHECK(type1.called[3]);
		CHECK(!g_called[3]);
		
		type0.BoolIntFloat = &Func4;
		type0.BoolIntFloat();
		CHECK(!type1.called[4]);
		CHECK(g_called[4]);
	}
*/
}
