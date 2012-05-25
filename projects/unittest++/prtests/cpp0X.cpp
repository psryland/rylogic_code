#include "unittest++/1.3/src/unittest++.h"
#include <cassert>
#include <iostream>
#include <vector>
#include <algorithm>
#include "pr/common/fmt.h"

typedef std::vector<std::string> VecStr;

SUITE(Cpp0X)
{
	TEST(Auto)
	{
		VecStr v;
		v.push_back("Hello");
		v.push_back("World");

		std::stringstream op;
		for (auto i = begin(v), iend = end(v); i != iend; ++i)
			op << *i << " ";

		CHECK(op.str() == "Hello World ");
	}

	template <typename T1, typename T2> auto Add(T1 lhs, T2 rhs) -> decltype(lhs + rhs)
	{
		return lhs + rhs;
	};
	TEST(Decltype)
	{
		CHECK_EQUAL(sizeof(double), sizeof(Add<double,int>(10.0, 2)));
		CHECK_EQUAL(sizeof(int)  , sizeof(Add<char,char>('a','b')));
	}

	TEST(LambdaFunctions)
	{
		VecStr v;
		v.push_back("Paul");
		v.push_back("was");
		v.push_back("here");
		v.push_back("and");
		v.push_back("here");
		
		// [] = capture specification
		// () = parameters
		// {} = body
		auto simple_lambda = [&](){ CHECK(true); };
		simple_lambda();
		auto explicit_return_type = []()-> int { return 1; };
		CHECK(explicit_return_type() == 1);
		
		std::stringstream op;
		std::sort    (begin(v), end(v), [](std::string const& lhs, std::string const& rhs) {return lhs.compare(rhs) < 0;});
		std::for_each(begin(v), end(v), [&](std::string const& s) { op << s << ","; });
		CHECK(op.str() == "Paul,and,here,here,was,");
		
		// []  Capture nothing (or, a scorched earth strategy?)
		// [&] Capture any referenced variable by reference
		// [=] Capture any referenced variable by making a copy
		// [=, &foo] Capture any referenced variable by making a copy, but capture variable foo by reference
		// [bar] Capture bar by making a copy; don't copy anything else
		// [this] Capture the this pointer of the enclosing class
		struct Thing
		{
			int m_member;
			Thing() :m_member(0) {}
			void Func() { [this](){ m_member = 4; }(); }
		};
		Thing thing;
		CHECK_EQUAL(0, thing.m_member); 
		thing.Func();
		CHECK_EQUAL(4, thing.m_member); 
	}

	struct Block
	{
		std::stringstream& m_op;
		int m_id;
		void* m_mem;
		int m_size;
		
		int Id() const { static int id = 0; return id++; }
		
		Block(std::stringstream& op) :m_op(op) ,m_id(Id()) ,m_mem(0) ,m_size(0)
		{
			m_op << "Construct " << m_id << "\n";
		}
		Block(std::stringstream& op, int size) :m_op(op) ,m_id(Id()) ,m_mem(new unsigned char[size]) ,m_size(size)
		{
			m_op << "Construct " << m_id << "\n";
		}
		~Block()
		{
			m_op << "Destruct " << m_id << "\n";
			delete [] m_mem;
		}
		Block(Block const& rhs) :m_op(rhs.m_op) ,m_id(Id()) ,m_mem(new unsigned char[rhs.m_size]) ,m_size(rhs.m_size)
		{
			m_op << "Copy constructor "<<m_id<<"<-"<<rhs.m_id<<"\n";
			memcpy(m_mem, rhs.m_mem, m_size);
		}	
		Block& operator=(Block const& rhs)
		{
			m_op << "Copy assignment "<<m_id<<"<-"<<rhs.m_id<<"\n";
			if (this == &rhs) return *this;
			
			void* mem = new unsigned char[rhs.m_size];
			memcpy(mem, rhs.m_mem, rhs.m_size);
			
			delete [] m_mem;
			m_mem = mem;
			m_size = rhs.m_size;
			return *this;
		}
		Block(Block&& rhs) :m_op(rhs.m_op) ,m_id(Id()) ,m_mem(0) ,m_size(0)
		{
			m_op << "Move constructor "<<m_id<<"<-"<<rhs.m_id<<"\n";
			*this = std::move(rhs);
			//m_mem  = rhs.m_mem;  rhs.m_mem  = 0;
			//m_size = rhs.m_size; rhs.m_size = 0;
		}
		Block& operator=(Block&& rhs)
		{
			m_op << "Move assignment "<<m_id<<"<-"<<rhs.m_id<<"\n";
			if (this == &rhs) return *this;

			delete [] m_mem;
			m_mem  = rhs.m_mem;  rhs.m_mem = 0;
			m_size = rhs.m_size; rhs.m_size = 0;
			return *this;
		}
	};
	TEST(RvalueReference)
	{
		std::stringstream op;
		
		Block b0(op, 10);
		Block b1 = b0;
		Block b2(op);
		b2 = b0;
		b0 = std::move(b2);
		assert(b2.m_size == 0);

		std::vector<Block> blocks;
		blocks.push_back(b0);
		blocks.push_back(b1);
		blocks.push_back(b2);
		blocks.push_back(Block(op, 10));
		blocks.push_back(Block(op, 10));

		CHECK(op.str() == 
			"Construct 0\n"
			"Copy constructor 1<-0\n"
			"Construct 2\n"
			"Copy assignment 2<-0\n"
			"Move assignment 0<-2\n"
			"Copy constructor 3<-0\n"
			"Move constructor 4<-3\n"
			"Move assignment 4<-3\n"
			"Destruct 3\n"
			"Copy constructor 5<-1\n"
			"Move constructor 6<-4\n"
			"Move assignment 6<-4\n"
			"Move constructor 7<-5\n"
			"Move assignment 7<-5\n"
			"Destruct 4\n"
			"Destruct 5\n"
			"Copy constructor 8<-2\n"
			"Construct 9\n"
			"Move constructor 10<-6\n"
			"Move assignment 10<-6\n"
			"Move constructor 11<-7\n"
			"Move assignment 11<-7\n"
			"Move constructor 12<-8\n"
			"Move assignment 12<-8\n"
			"Destruct 6\n"
			"Destruct 7\n"
			"Destruct 8\n"
			"Move constructor 13<-9\n"
			"Move assignment 13<-9\n"
			"Destruct 9\n"
			"Construct 14\n"
			"Move constructor 15<-10\n"
			"Move assignment 15<-10\n"
			"Move constructor 16<-11\n"
			"Move assignment 16<-11\n"
			"Move constructor 17<-12\n"
			"Move assignment 17<-12\n"
			"Move constructor 18<-13\n"
			"Move assignment 18<-13\n"
			"Destruct 10\n"
			"Destruct 11\n"
			"Destruct 12\n"
			"Destruct 13\n"
			"Move constructor 19<-14\n"
			"Move assignment 19<-14\n"
			"Destruct 14\n");
	}

	//TEST(Forwarding)
	//{
	//}
	
	TEST(RangedForLoop)
	{
		VecStr vec;
		vec.push_back("one");
		vec.push_back("two");
		vec.push_back("three");
		vec.push_back("four");
		//for (auto i: v) {} // not supported in vs2010
		CHECK(true);
	}
}