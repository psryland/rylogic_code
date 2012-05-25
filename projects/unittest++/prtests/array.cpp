//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/assert.h"
#include "pr/common/array.h"
#include "pr/common/refptr.h"
#include "pr/common/refcount.h"
#include "pr/maths/maths.h"

SUITE(Array)
{
	struct Single :pr::RefCount<Single>
	{
		static void RefCountZero(RefCount<Single>*) {}
	} g_single;
	
	int g_start_object_count, g_object_count = 0;;
	inline void ConstrCall()
	{
		++g_object_count;
	}
	inline void DestrCall()
	{
		--g_object_count;
	}

	typedef unsigned int uint;
	struct Type
	{
		uint val;
		pr::RefPtr<Single> ptr;
		operator uint() const                             { return val; }
		
		Type()       :val(0) ,ptr(&g_single)              { ConstrCall(); }
		Type(uint w) :val(w) ,ptr(&g_single)              { ConstrCall(); }
		Type(Type const& rhs) :val(rhs.val) ,ptr(rhs.ptr) { ConstrCall(); }
		~Type()
		{
			DestrCall();
			if (ptr.m_ptr != &g_single)
				throw std::exception("destructing an invalid Type");
			val = 0xcccccccc;
		}
	};
	
	typedef pr::Array<Type, 8, false> Array0;
	typedef pr::Array<Type, 16, true> Array1;
	std::vector<Type> ints;
	
	TEST(Array0)
	{
		ints.resize(16);
		for (uint i = 0; i != 16; ++i) ints[i] = Type(i);
		
		g_start_object_count = g_object_count;
		{
			Array0 arr;
			CHECK(arr.empty());
			CHECK_EQUAL(0U, arr.size());
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array1 arr(15);
			CHECK(!arr.empty());
			CHECK_EQUAL(15U, arr.size());
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array0 arr(5U, 3);
			CHECK_EQUAL(5U, arr.size());
			for (size_t i = 0; i != 5; ++i)
				CHECK_EQUAL(3U, arr[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array0 arr0(5U,3);
			Array1 arr1(arr0);
			CHECK_EQUAL(arr0.size(), arr1.size());
			for (size_t i = 0; i != arr0.size(); ++i)
				CHECK_EQUAL(arr0[i], arr1[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			std::vector<uint> vec0(4U, 6);
			Array0 arr1(vec0);
			CHECK_EQUAL(vec0.size(), arr1.size());
			for (size_t i = 0; i != vec0.size(); ++i)
				CHECK_EQUAL(vec0[i], arr1[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(RefCounting0)
	{
		CHECK_EQUAL(16, g_single.m_ref_count);
	}
	TEST(Assign)
	{
		g_start_object_count = g_object_count;
		{
			Array0 arr0;
			arr0.assign(3U, 5);
			CHECK_EQUAL(3U, arr0.size());
			for (size_t i = 0; i != 3; ++i)
				CHECK_EQUAL(5U, arr0[i]);
		
			Array1 arr1;
			arr1.assign(&ints[0], &ints[8]);
			CHECK_EQUAL(8U, arr1.size());
			for (size_t i = 0; i != 8; ++i) CHECK_EQUAL(ints[i], arr1[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(RefCounting1)
	{
		CHECK_EQUAL(16, g_single.m_ref_count);
	}
	TEST(Clear)
	{
		g_start_object_count = g_object_count;
		{
			Array0 arr0(ints.begin(), ints.end());
			arr0.clear();
			CHECK(arr0.empty());
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(RefCounting2)
	{
		CHECK_EQUAL(16, g_single.m_ref_count);
	}
	TEST(Erase)
	{
		g_start_object_count = g_object_count;
		{
			Array0 arr0(ints.begin(), ints.begin() + 8);
			Array0::const_iterator b = arr0.begin();
			arr0.erase(b + 3, b + 5);
			CHECK_EQUAL(6U, arr0.size());
			for (size_t i = 0; i != 3; ++i) CHECK_EQUAL(ints[i], arr0[i]);
			for (size_t i = 3; i != 6; ++i) CHECK_EQUAL(ints[i+2], arr0[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array1 arr1(ints.begin(), ints.begin() + 4);
			arr1.erase(arr1.begin() + 2);
			CHECK_EQUAL(3U, arr1.size());
			for (size_t i = 0; i != 2; ++i) CHECK_EQUAL(ints[i], arr1[i]);
			for (size_t i = 2; i != 3; ++i) CHECK_EQUAL(ints[i+1], arr1[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array0 arr2(ints.begin(), ints.begin() + 5);
			arr2.erase_fast(arr2.begin() + 2);
			CHECK_EQUAL(4U, arr2.size());
			for (size_t i = 0; i != 2; ++i) CHECK_EQUAL(ints[i], arr2[i]);
			CHECK_EQUAL(ints[4], arr2[2]);
			for (size_t i = 3; i != 4; ++i) CHECK_EQUAL(ints[i], arr2[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(RefCounting3)
	{
		CHECK_EQUAL(16, g_single.m_ref_count);
	}
	TEST(Insert)
	{
		g_start_object_count = g_object_count;
		{
			Array0 arr0;
			arr0.insert(arr0.end(), 4U, 9);
			CHECK_EQUAL(4U, arr0.size());
			for (size_t i = 0; i != 4; ++i) CHECK_EQUAL(9U, arr0[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array1 arr1(4U, 6);
			arr1.insert(arr1.begin() + 2, &ints[2], &ints[7]);
			CHECK_EQUAL(9U, arr1.size());
			for (size_t i = 0; i != 2; ++i) CHECK_EQUAL(6U, arr1[i]);
			for (size_t i = 2; i != 7; ++i) CHECK_EQUAL(ints[i], arr1[i]);
			for (size_t i = 7; i != 9; ++i) CHECK_EQUAL(6U, arr1[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(RefCounting4)
	{
		CHECK_EQUAL(16, g_single.m_ref_count);
	}
	TEST(PushPop)
	{
		g_start_object_count = g_object_count;
		{
			Array0 arr;
			arr.insert(arr.begin(), &ints[0], &ints[4]);
			arr.pop_back();
			CHECK_EQUAL(3U, arr.size());
			for (size_t i = 0; i != 3; ++i)
				CHECK_EQUAL(ints[i], arr[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array1 arr;
			arr.reserve(4);
			for (int i = 0; i != 4; ++i) arr.push_back_fast(i);
			for (int i = 4; i != 9; ++i) arr.push_back(i);
			for (size_t i = 0; i != 9; ++i)
				CHECK_EQUAL(ints[i], arr[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array1 arr;
			arr.insert(arr.begin(), &ints[0], &ints[4]);
			arr.resize(3);
			CHECK_EQUAL(3U, arr.size());
			for (size_t i = 0; i != 3; ++i)
				CHECK_EQUAL(ints[i], arr[i]);
			arr.resize(6);
			CHECK_EQUAL(6U, arr.size());
			for (size_t i = 0; i != 3; ++i)
				CHECK_EQUAL(ints[i], arr[i]);
			for (size_t i = 3; i != 6; ++i)
				CHECK_EQUAL(0U, arr[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(RefCounting5)
	{
		CHECK_EQUAL(16, g_single.m_ref_count);
	}
	TEST(Operators)
	{
		g_start_object_count = g_object_count;
		{
			Array0 arr0(4U, 1);
			Array0 arr1(3U, 2);
			arr1 = arr0;
			CHECK_EQUAL(4U, arr0.size());
			CHECK_EQUAL(4U, arr1.size());
			for (size_t i = 0; i != 4; ++i) CHECK_EQUAL(arr0[i], arr1[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
		g_start_object_count = g_object_count;
		{
			Array0 arr0(4U, 1);
			Array1 arr2;
			arr2 = arr0;
			CHECK_EQUAL(4U, arr0.size());
			CHECK_EQUAL(4U, arr2.size());
			for (size_t i = 0; i != 4; ++i) CHECK_EQUAL(arr0[i], arr2[i]);
		
			struct L
			{
				static std::vector<Type> Conv(std::vector<Type> v) { return v; }
			};
			std::vector<Type> vec0 = L::Conv(arr0);
			CHECK_EQUAL(4U, vec0.size());
			for (size_t i = 0; i != 4; ++i) CHECK_EQUAL(arr0[i], vec0[i]);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(RefCounting6)
	{
		CHECK_EQUAL(16, g_single.m_ref_count);
	}
	TEST(Mem)
	{
		g_start_object_count = g_object_count;
		{
			Array0 arr0;
			arr0.reserve(100);
			for (uint i = 0; i != 50; ++i) arr0.push_back(i);
			CHECK_EQUAL(100U, arr0.capacity());
			arr0.shrink_to_fit();
			CHECK_EQUAL(50U, arr0.capacity());
			arr0.resize(1);
			arr0.shrink_to_fit();
			CHECK_EQUAL((size_t)arr0.LocalLength, arr0.capacity());
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(RefCounting)
	{
		ints.clear();
		CHECK_EQUAL(0, g_single.m_ref_count);
	}
	TEST(AlignedTypes)
	{
		g_start_object_count = g_object_count;
		{
			pr::Spline spline = pr::Spline::make(pr::Random3N(1.0f), pr::Random3N(1.0f), pr::Random3N(1.0f), pr::Random3N(1.0f));
		
			pr::Array<pr::v4> arr0;
			pr::Raster(spline, arr0, 100);
		
			CHECK(arr0.capacity() > arr0.LocalLength);
			arr0.resize(5);
			arr0.shrink_to_fit();
			CHECK_EQUAL(5U, arr0.size());
			CHECK(arr0.capacity() == arr0.LocalLength);
		}
		CHECK_EQUAL(g_start_object_count, g_object_count);
	}
	TEST(GlobalConstrDestrCount)
	{
		CHECK_EQUAL(0, g_object_count);
	}
}
