//*************************************************************
// Unit Test for pr macros
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/macros/for_each.h"

SUITE(PRMacro)
{
	TEST(ForEach)
	{
		std::vector<int> ints;
		ints.push_back(0);
		ints.push_back(1);
		ints.push_back(2);
		ints.push_back(3);
		int chk;

		chk = 0;
		FOR_EACH(i, ints)
			CHECK(*i == chk++);

		chk = 0;
		FOR_EACHC(i, ints)
			CHECK(*i == chk++);

		FOR_EACHI(i, chk, ints)
			CHECK(*i == chk);

		FOR_EACHIC(i, chk, ints)
			CHECK(*i == chk);
	}
}


//namespace pr
//{
//	namespace mpl
//	{
//		namespace impl
//		{
//			template <int N> struct size_to_type
//			{
//
//			};
//			
//			template <typename T, int N> struct size_of
//			{
//				typedef T type;
//				char (*size(T&))[N];
//			};
//			
//			template <typename T> size_of<T> type_to_size(T const&, int counter)
//			{
//				return size_of<T>;
//			}
//
//
//
//			template <> struct size_to_type
//			{ typedef typeof<T>::type type; };\
//	char (*typeof_fct(typeof<T>::type&))[N];\
//	}}}
//			};
//
//			template<> struct typeof_n<N>
//			{
//				typedef typeof<T>::type type;
//			};
//
//		}
//	}
//}
//#define typeof_2(x)
//	pr::mpl::impl::size_to_type
//		<
//			sizeof(pr::mpl::impl::type_to_size(x, __COUNTER__).size)
//		>::type

