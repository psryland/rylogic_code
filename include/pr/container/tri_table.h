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
		enum EType { Inclusive = 1, Exclusive = -1 };

		// Runtime inclusive/exclusive tri-table count
		template <EType inc> inline size_t Size(size_t num_elements)
		{
			return num_elements * (num_elements + inc) / 2;
		}
		
		// Runtime inclusive/exclusive tri-table index
		template <EType inc> inline size_t Index(size_t indexA, size_t indexB)
		{
			return (indexA < indexB)
				? indexB * (indexB + inc) / 2 + indexA
				: indexA * (indexA + inc) / 2 + indexB;
		}
	}

	// Compile time inclusive/exclusive table count
	template <size_t num_elements, tri_table::EType inc> struct TriTable
	{
		enum { size = num_elements * (int(num_elements) + inc) / 2 };
	};

	// Compile time inclusive/exclusive tri table index
	template <size_t indexA, size_t indexB, tri_table::EType inc> struct TriIndex
	{
		enum
		{
			index = (indexA < indexB)
			? (indexB * (int(indexB) + inc) / 2 + indexA)
			: (indexA * (int(indexA) + inc) / 2 + indexB)
		};
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(TriangularTableTests)
	{
		using namespace pr::tri_table;

		// Compile time checks
		static_assert(pr::TriIndex<2,2,Inclusive>::index + 1 == pr::TriTable<3,Inclusive>::size, "");
		static_assert(pr::TriIndex<2,1,Exclusive>::index + 1 == pr::TriTable<3,Exclusive>::size, "");
		static_assert(pr::TriIndex<3,3,Inclusive>::index + 1 == pr::TriTable<4,Inclusive>::size, "");
		static_assert(pr::TriIndex<3,2,Exclusive>::index + 1 == pr::TriTable<4,Exclusive>::size, "");

		// Runtime checks
		PR_CHECK(pr::tri_table::Index<Inclusive>(2,2) + 1, pr::tri_table::Size<Inclusive>(3));
		PR_CHECK(pr::tri_table::Index<Exclusive>(2,1) + 1, pr::tri_table::Size<Exclusive>(3));
		PR_CHECK(pr::tri_table::Index<Inclusive>(3,3) + 1, pr::tri_table::Size<Inclusive>(4));
		PR_CHECK(pr::tri_table::Index<Exclusive>(3,2) + 1, pr::tri_table::Size<Exclusive>(4));
	}
}
#endif
