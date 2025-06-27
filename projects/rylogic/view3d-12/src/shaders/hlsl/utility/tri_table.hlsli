//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_TRI_TABLE_HLSLI
#define PR_RDR_TRI_TABLE_HLSLI

// Careful, HLSL < SM6.6 doesn't support int64 or double

// Returns the required array size for a 'num_elements' tri-table
int64_t TriTable_Size(int type, int64_t num_elements)
{
	return num_elements * (num_elements + type) / 2;
}

// Returns the square dimension of the tri-table. (i.e. the inverse of the 'Size' function)
int64_t Dimension(int type, int64_t array_size)
{
	// Size = S = n(n+t)/2, t = +1 (incl), -1 (excl)
	//'   => n² + n.t - 2S = 0
	//'   => n = (-t +/- sqrt(t² + 8S)) / 2  (quadratic formula)
	// However:
	//  sqrt isn't constexpr
	//  sqrt(1 + 8S) overflows for large S (and doesn't work on GPU with only sqrt(float)
	// But:
	//'   sqrt(1 + 8S) = sqrt(A²*(1 + 8S)/A²) = A*sqrt((1 + 8S)/A²)
	//' let A = 2*sqrt(S) then
	//'   sqrt(1 + 8S) = 2*sqrt(S)*sqrt((1 + 8S)/4S) = 2*sqrt(S)*sqrt(1/(4*S) + 2))
	//'   ~= 2*sqrt(2)*sqrt(S) if S >> 1.
	//'   => n = sqrt(2)*sqrt(S) - t/2. Since n must be positive

	#if 0
	// Remember this is the number of full rows that fit into 'array_size'
	static constexpr int64_t small_sizes[] = {
		0,
		1, 1,
		2, 2, 2,
		3, 3, 3, 3,
		4, 4, 4, 4, 4,
	};
	if (array_size < _countof(small_sizes) - 1)
		return small_sizes[array_size] + int(type == EType::Exclusive);

	auto sqrt_array_size = impl::ISqrt(array_size);
	auto num_elements = static_cast<int64_t>(std::numbers::sqrt2 * sqrt_array_size - int(type) / 2.0);

	// Ensure 'num_elements' is the maximum possible value.
	for (; Size(type, num_elements + 0) > array_size; --num_elements) {}
	for (; Size(type, num_elements + 1) <= array_size; ++num_elements) {}
	return num_elements;
	#endif
	return 0;
}

// Returns the index into a tri-table array for the element (a,b)|(b,a)
int64_t Index(int type, int indexA, int indexB)
{
	//assert(indexA >= 0 && indexB >= 0 && indexA < MaxIndex && indexB < MaxIndex);
	//assert((type != EType::Exclusive || indexA != indexB) && "indexA == indexB is invalid for an exclusive table");
	return (indexA < indexB)
		? indexB * (indexB + int(type)) / 2 + indexA
		: indexA * (indexA + int(type)) / 2 + indexB;
}

// Inverse of the 'Index' function. Get the indices A and B for a given table index
int2 FromIndex(int type, int64_t tri_index)
{
	#if 0
	// If 'tri_index' is valid, then the array must have at least 'tri_index+1' elements.
	// The dimension of a table with 'tri_index+1' elements is the length of each side of the table, N.
	// Since 'indexL' is the first item in the last row, it's index value is N - 1.
	auto indexL = Dimension(EType::Inclusive, tri_index); // large

	// The array size of a table with 'indexL' values is the sum of all rows < 'indexL'.
	// Since 'tri_index' equals the required array size - 1, subtract gives the position
	// along the row.
	auto indexS = tri_index - Size(EType::Inclusive, indexL); // small

	// Exclusive tables are the same shape as inclusive tables, except the row index is +1
	return { static_cast<int>(indexS), static_cast<int>(indexL) + int(type == EType::Exclusive)};
	#endif
	return int2(0,0);
}
#endif
