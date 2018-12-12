//**************************************
// Triangular Table
//  Copyright (c) Rylogic Ltd 2007
//**************************************
//
// Helper functions for triangular tables
// Inclusive = A table with entries for N vs. 0 -> N
// Exclusive = A table with entries for N vs. 0 -> N - 1
//
// An Exclusive Triangular table looks like this:
//	Exc_|_0_|_1_|_2_|_..._|_N_|
//	__1_|_X_|___  
//	__2_|_X_|_X_|___
//	__3_|_X_|_X_|_X_|__
//	_.._|_X_|_X_|_X_|_..._
//	_N-1|_X_|_X_|_X_|_..._|
//
//	(Inclusive has an extra row at the bottom and a diagonal

// Usage:
//	bool flag[pr::TriTable<10>::SizeExc];
//	flag[tri_table::IndexExc(indexA, indexB)] = true;
// or
//	bool flag[TriTable<10>::SizeInc];
//	flag[TriTableIndexInc<4, 6>::Index] = true;
//
#pragma once
#include "pr/common/assert.h"

namespace pr
{
	namespace tri_table
	{
		enum class EType : int { Inclusive = 1, Exclusive = -1 };

		// Runtime inclusive/exclusive tri-table count
		constexpr size_t Size(EType inc, int num_elements)
		{
			return num_elements * (num_elements + int(inc)) / 2;
		}
		
		// Runtime inclusive/exclusive tri-table index
		constexpr size_t Index(EType inc, int indexA, int indexB)
		{
			return (indexA < indexB)
				? indexB * (indexB + int(inc)) / 2 + indexA
				: indexA * (indexA + int(inc)) / 2 + indexB;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(TriangularTableTests)
	{
		using namespace pr::tri_table;

		// Compile time checks
		static_assert(Index(EType::Inclusive, 2, 2) + 1 == Size(EType::Inclusive, 3), "");
		static_assert(Index(EType::Exclusive, 2, 1) + 1 == Size(EType::Exclusive, 3), "");
		static_assert(Index(EType::Inclusive, 3, 3) + 1 == Size(EType::Inclusive, 4), "");
		static_assert(Index(EType::Exclusive, 3, 2) + 1 == Size(EType::Exclusive, 4), "");
	}
}
#endif
