//**************************************
// Triangular Table
//  Copyright © Rylogic Ltd 2007
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
//	bool flag[TriTable<10>::SizeExc];
//	flag[GetTriTableIndexExc(indexA, indexB)] = true;
// or
//	bool flag[TriTable<10>::SizeInc];
//	flag[TriTableIndexInc<4, 6>::Index] = true;
//
#ifndef PR_TRIANGULAR_TABLE_H
#define PR_TRIANGULAR_TABLE_H

#include "pr/common/assert.h"

namespace pr
{
	// Returns the size required for a triangular table containing num_elements
	template <std::size_t num_elements>
	struct TriTable
	{
		enum
		{
			SizeInc = num_elements * (num_elements + 1) / 2,
			SizeExc = num_elements * (num_elements - 1) / 2
		};
	};

	// Returns a runtime size required for a triangular table containing num_elements
	inline std::size_t GetTriTableSizeInc(std::size_t num_elements)
	{
		return num_elements * (num_elements + 1) / 2;
	}
	inline std::size_t GetTriTableSizeExc(std::size_t num_elements)
	{
		return num_elements * (num_elements - 1) / 2;
	}	

	// Returns a compile time TriTable index 
	template <std::size_t indexA, std::size_t indexB>
	struct TriTableIndexInc
	{
		enum
		{
			Index = (indexA < indexB) ?
					(indexB * (indexB + 1) / 2 + indexA) :
					(indexA * (indexA + 1) / 2 + indexB)
		};
	};

	// Returns a compile time TriTable index 
	template <std::size_t indexA, std::size_t indexB>
	struct TriTableIndexExc
	{
		enum
		{
			Index = (indexA < indexB) ?
					(indexB * (indexB - 1) / 2 + indexA) :
					(indexA * (indexA - 1) / 2 + indexB)
		};
	};

	// Return a runtime TriTable index
	inline std::size_t GetTriTableIndexInc(std::size_t indexA, std::size_t indexB)
	{
		if( indexA < indexB )	return indexB * (indexB + 1) / 2 + indexA;
		else					return indexA * (indexA + 1) / 2 + indexB;
	}

	// Return a runtime TriTable index
	inline std::size_t GetTriTableIndexExc(std::size_t indexA, std::size_t indexB)
	{
		if( indexA < indexB )	return indexB * (indexB - 1) / 2 + indexA;
		else					return indexA * (indexA - 1) / 2 + indexB;
	}

	static_assert((pr::TriTableIndexInc<2,2>::Index + 1 == pr::TriTable<3>::SizeInc), "");
	static_assert((pr::TriTableIndexExc<2,1>::Index + 1 == pr::TriTable<3>::SizeExc), "");
	static_assert((pr::TriTableIndexInc<3,3>::Index + 1 == pr::TriTable<4>::SizeInc), "");
	static_assert((pr::TriTableIndexExc<3,2>::Index + 1 == pr::TriTable<4>::SizeExc), "");

}//namespace pr

#endif//PR_TRIANGULAR_TABLE_H
