//**************************************
// Triangular Table
//  Copyright (c) Rylogic Ltd 2007
//**************************************
//
// Helper functions for triangular tables
// Inclusive = A table with entries for [0,N) vs. [0,N)
// Exclusive = A table with entries for [0,N) vs. [0,N-1)
//
// Lower triangular exclusive table of size 5 looks like this:
//  ____|_0_|_1_|_2_|_3_|
//  __1_|_X_            |
//  __2_|_X_|_X_        |
//  __3_|_X_|_X_|_X_    |
//  __4_|_X_|_X_|_X_|_X_|
//
// Lower triangular inclusive table:
//  ____|_0_|_1_|_2_|_3_|_4_|
//  __0_|_+_                |
//  __1_|_X_|_+_            |
//  __2_|_X_|_X_|_+_        |
//  __3_|_X_|_X_|_X_|_+_    |
//  __4_|_X_|_X_|_X_|_X_|_+_|
//
// Note:
//  It's not possible to have an upper triangular table 'Index' function without
//  knowing the dimension of the table. This is why only lower triangular is supported.
//  i.e.,
//   Upper triangular exclusive table:
//   ____|_1_|_2_|_3_|_4_|
//   __0_|_X_|_X_|_X_|_X_|
//   __1_|   |_X_|_X_|_X_|
//   __2_|       |_X_|_X_|
//   __3_|           |_X_|
//   
//   Upper triangular inclusive table:
//   ____|_0_|_1_|_2_|_3_|_4_|
//   __0_|_+_|_X_|_X_|_X_|_X_|
//   __1_|   |_+_|_X_|_X_|_X_|
//   __3_|       |_+_|_X_|_X_|
//   __2_|           |_+_|_X_|
//   __4_|               |_+_|
//
// To convert an upper triangular table to a lower triangular table
// you need to transpose the data when creating the table.

#pragma once
#include <cstdint>

namespace pr::tri_table
{
	enum class EType : int { Inclusive = 1, Exclusive = -1 }; 

	// Returns the required array size for a 'num_elements' tri-table
	constexpr size_t Size(EType type, size_t num_elements)
	{
		return num_elements * (num_elements + int(type)) / 2;
	}

	// Returns the square dimension of the tri-table. (i.e. the inverse of the 'Size' function)
	inline int Dimension(EType type, size_t array_size)
	{
		// Size (inclusive) = Si = n(n+1)/2
		//   => n^2 + n - 2Si = 0
		//   => n = (-1 + sqrt(1 + 8Si)) / 2
		// Size (exclusive) = Se = n(n-1)/2
		//   => n^2 - n - 2Se = 0
		//   => n = (+1 + sqrt(1 + 8Se)) / 2
		if (array_size == 0) return 0; // special case for exclusive
		return static_cast<int>(std::floor((-int(type) + std::sqrt(1 + 8 * array_size)) / 2.0));
	}

	// Returns the index into a tri-table array for the element (a,b)|(b,a)
	constexpr int Index(EType type, int indexA, int indexB)
	{
		return (indexA < indexB)
			? indexB * (indexB + int(type)) / 2 + indexA
			: indexA * (indexA + int(type)) / 2 + indexB;
	}

	// Inverse of the 'Index' function. Get the indices A and B for a given table index
	inline std::pair<int, int> FromIndex(EType type, int index)
	{
		int indexL, indexS; // large, small

		// Solve for indexL using the quadratic formula
		indexL = static_cast<int>(std::floor((-int(type) + std::sqrt(1 + 8 * index)) / 2.0));

		// Compute the remainder to find the smaller index
		auto triangularL = (indexL * (indexL + int(type))) / 2;
		indexS = index - triangularL;

		return { indexS, indexL };
	}

	// Type for using statements
	template <EType Type> struct TriTable
	{
		static constexpr size_t Size(int num_elements)
		{
			return tri_table::Size(Type, num_elements);
		}

		static constexpr int Dimension(size_t array_size)
		{
			return tri_table::Dimension(Type, array_size);
		}

		static constexpr int Index(int indexA, int indexB)
		{
			return tri_table::Index(Type, indexA, indexB);
		}

		static constexpr std::pair<int, int> FromIndex(int index)
		{
			return tri_table::FromIndex(Type, index);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(TriangularTableTests)
	{
		using namespace pr::tri_table;

		using IT = TriTable<EType::Inclusive>;
		using ET = TriTable<EType::Exclusive>;

		static_assert(IT::Size(0) == 0);
		static_assert(IT::Size(1) == 1);
		static_assert(IT::Size(2) == 3);
		static_assert(IT::Size(3) == 6);
		static_assert(IT::Size(4) == 10);
		static_assert(IT::Size(5) == 15);

		static_assert(ET::Size(0) == 0);
		static_assert(ET::Size(1) == 0);
		static_assert(ET::Size(2) == 1);
		static_assert(ET::Size(3) == 3);
		static_assert(ET::Size(4) == 6);
		static_assert(ET::Size(5) == 10);

		PR_EXPECT(IT::Dimension(0) == 0);
		PR_EXPECT(IT::Dimension(1) == 1);
		PR_EXPECT(IT::Dimension(3) == 2);
		PR_EXPECT(IT::Dimension(6) == 3);
		PR_EXPECT(IT::Dimension(10) == 4);
		PR_EXPECT(IT::Dimension(15) == 5);

		PR_EXPECT(ET::Dimension(0) == 0);
		PR_EXPECT(ET::Dimension(1) == 2);
		PR_EXPECT(ET::Dimension(3) == 3);
		PR_EXPECT(ET::Dimension(6) == 4);
		PR_EXPECT(ET::Dimension(10) == 5);
		PR_EXPECT(ET::Dimension(15) == 6);

		// Round-trip Size and Dimension
		for (float f = 2.0f; f < 10000.0f; f = 1.2f * f + 0.7f)
		{
			// Start with size 2, because exclusive is ambiguous for size=1
			auto i = static_cast<int>(f);
			PR_EXPECT(IT::Dimension(IT::Size(i)) == i);
			PR_EXPECT(ET::Dimension(ET::Size(i)) == i);
		}
		
		// Round-trip Index and A/B
		for (int a = 0; a != 1000; ++a)
		{
			for (int b = 0; b != 1000; ++b)
			{
				auto index_i = IT::Index(a, b);
				auto index_e = ET::Index(a, b);

				auto [a_i, b_i] = IT::FromIndex(index_i);
				auto [a_e, b_e] = ET::FromIndex(index_e);

				PR_EXPECT(IT::Index(a_i, b_i) == index_i);
				PR_EXPECT(ET::Index(a_e, b_e) == index_e);
			}
		}

		// Last index + 1 == table size
		static_assert(IT::Index(2, 2) + 1 == static_cast<int>(IT::Size(3)));
		static_assert(ET::Index(2, 1) + 1 == static_cast<int>(ET::Size(3)));
		static_assert(IT::Index(4, 4) + 1 == static_cast<int>(IT::Size(5)));
		static_assert(ET::Index(3, 4) + 1 == static_cast<int>(ET::Size(5)));

		// Index order (table size = 3, inclusive)
		static_assert(IT::Index(0, 0) == 0); static_assert(IT::Index(0, 0) == 0);
		static_assert(IT::Index(1, 0) == 1); static_assert(IT::Index(0, 1) == 1);
		static_assert(IT::Index(1, 1) == 2); static_assert(IT::Index(1, 1) == 2);
		static_assert(IT::Index(2, 0) == 3); static_assert(IT::Index(0, 2) == 3);
		static_assert(IT::Index(2, 1) == 4); static_assert(IT::Index(1, 2) == 4);
		static_assert(IT::Index(2, 2) == 5); static_assert(IT::Index(2, 2) == 5);

		// Index order (table size = 4, exclusive)
		static_assert(ET::Index(1, 0) == 0); static_assert(ET::Index(0, 1) == 0);
		static_assert(ET::Index(2, 0) == 1); static_assert(ET::Index(0, 2) == 1);
		static_assert(ET::Index(2, 1) == 2); static_assert(ET::Index(1, 2) == 2);
		static_assert(ET::Index(3, 0) == 3); static_assert(ET::Index(0, 3) == 3);
		static_assert(ET::Index(3, 1) == 4); static_assert(ET::Index(1, 3) == 4);
		static_assert(ET::Index(3, 2) == 5); static_assert(ET::Index(2, 3) == 5);
	}
}
#endif
