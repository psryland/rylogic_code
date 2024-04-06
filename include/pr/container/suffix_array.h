//******************************************
// Suffix Array
//  Copyright (c) Rylogic Ltd 2024
//******************************************
// Explanation:
//   https://www.youtube.com/watch?v=Oj1wKc3CRL0&list=PL2mpR0RYFQsDFNyRsTNcWkFTHTkxWREeb
// Based on:
//   Nong G, Zhang S, Chan WH "Two efficient algorithms for linear time suffix array construction"
//   https://www.researchgate.net/publication/224176324_Two_Efficient_Algorithms_for_Linear_Time_Suffix_Array_Construction
#pragma once
#include <vector>
#include <concepts>

namespace pr::suffix_array
{
	namespace impl
	{
		inline int chr(int i, int cs, unsigned char const* s)
		{
			return cs == sizeof(int) ? ((int const*)s)[i] : ((unsigned char const*)s)[i];
		}
		inline int isLMS(int i, std::vector<bool> const& ls_types)
		{
			return i > 0 && ls_types[i] && !ls_types[i - 1];
		}

		// Find the start or end of each bucket
		void getBuckets(unsigned char const* s, int* bkt, int n, int K, int cs, bool end)
		{
			int i, sum = 0;

			// clear all buckets
			for (i = 0; i <= K; i++)
				bkt[i] = 0;

			// compute the size of each bucket
			for (i = 0; i < n; i++)
				bkt[chr(i, cs, s)]++;

			for (i = 0; i <= K; i++)
			{
				sum += bkt[i];
				bkt[i] = end ? sum : sum - bkt[i];
			}
		}

		// compute SAl
		void induceSAl(std::vector<bool> const& ls_types, int* SA, unsigned char const* s, int* bkt, int n, int K, int cs, bool end)
		{
			// find starts of buckets
			getBuckets(s, bkt, n, K, cs, end);
			for (int i = 0; i < n; i++)
			{
				auto j = SA[i] - 1;
				if (j >= 0 && !ls_types[j])
					SA[bkt[chr(j, cs, s)]++] = j;
			}
		}

		// compute SAs
		void induceSAs(std::vector<bool> const& ls_types, int* SA, unsigned char const* s, int* bkt, int n, int K, int cs, bool end)
		{
			// find ends of buckets
			getBuckets(s, bkt, n, K, cs, end);
			for (int i = n - 1; i >= 0; i--)
			{
				auto j = SA[i] - 1;
				if (j >= 0 && ls_types[j])
					SA[--bkt[chr(j, cs, s)]] = j;
			}
		}
	}

	// Construct the suffix array of data[0..n-1] in {1..K}ˆn
	// require s[n-1]=0 (the sentinel!), n>=2
	// use a working space (excluding s and SA) of
	// at most 2.25n+O(1) for a constant alphabet
	void Build(void const* data, int size, int* SA, int K, int cs)
	{
		using namespace impl;

		unsigned char const* s = (unsigned char*)data;

		// A character s[i] is said to be L-type and S-type if the sufﬁx s[i..n − 1] is L-type and S-type, respectively.
		// Based on the classiﬁed sufﬁxes of L-type and S-type, an S-substring is deﬁned as any substring s[i..j], j > i,
		// satisfying that s[i] and s[j] are the only two S-type characters in s[i..j]. Similarly, an L-substring s[i..j], j > i,
		// satisﬁes that s[i] and s[j] are the only two L-type characters in S[i..j].

		// L/S-type array in bits
		auto ls_types = std::vector<bool>(size);

		// Classify the type of each character
		// The sentinel must be in s1, important!!!
		ls_types[size - 2] = 0;
		ls_types[size - 1] = 1;
		for (int i = size - 3; i >= 0; i--)
		{
			auto val = chr(i, cs, s) < chr(i + 1, cs, s) || (chr(i, cs, s) == chr(i + 1, cs, s) && ls_types[i + 1]) ? true : false;
			ls_types[i] = val;
		}

		// stage 1: reduce the problem by at least 1/2
		// sort all the S-substrings
		{
			// bucket array
			auto bkt = std::vector<int>(K + 1);

			// find ends of buckets
			getBuckets(s, bkt.data(), size, K, cs, true);
			for (int i = 0; i < size; i++)
				SA[i] = -1;
			for (int i = 1; i < size; i++)
				if (isLMS(i, ls_types))
					SA[--bkt[chr(i, cs, s)]] = i;

			induceSAl(ls_types, SA, s, bkt.data(), size, K, cs, false);
			induceSAs(ls_types, SA, s, bkt.data(), size, K, cs, true);
		}

		// compact all the sorted substrings into
		// the first n1 items of SA
		// 2*n1 must be not larger than 'size' (proveable)
		int n1 = 0;
		for (int i = 0; i < size; i++)
			if (isLMS(SA[i], ls_types))
				SA[n1++] = SA[i];

		// find the lexicographic names of substrings
		// init the name array buffer
		for (int i = n1; i < size; i++)
			SA[i] = -1;

		int name = 0, prev = -1;
		for (int i = 0; i < n1; i++)
		{
			int pos = SA[i];
			bool diff = false;
			for (int d = 0; d < size; d++)
				if (prev == -1 || chr(pos + d, cs, s) != chr(prev + d, cs, s) || ls_types[pos + d] != ls_types[prev + d])
				{
					diff = true;
					break;
				}
				else if (d > 0 && (isLMS(pos + d, ls_types) || isLMS(prev + d, ls_types)))
					break;

			if (diff)
			{
				name++;
				prev = pos;
			}
			pos = (pos % 2 == 0) ? pos / 2 : (pos - 1) / 2;
			SA[n1 + pos] = name - 1;
		}
		for (int i = size - 1, j = size - 1; i >= n1; i--)
			if (SA[i] >= 0)
				SA[j--] = SA[i];

		// stage 2: solve the reduced problem
		// recurse if names are not yet unique
		int* SA1 = SA, * s1 = SA + size - n1;
		if (name < n1)
		{
			Build((unsigned char*)s1, n1, SA1, name - 1, sizeof(int));
		}
		else
		{
			// Generate the suffix array of s1 directly
			for (int i = 0; i < n1; i++)
				SA1[s1[i]] = i;
		}

		// stage 3: induce the result for
		// the original problem

		{// bucket array
			auto bkt = std::vector<int>(K + 1);

			// put all the LMS characters into their buckets
			// find ends of buckets
			getBuckets(s, bkt.data(), size, K, cs, true);
			for (int i = 1, j = 0; i < size; i++)
				if (isLMS(i, ls_types))
					s1[j++] = i; // get p1

			// get index in s
			for (int i = 0; i < n1; i++)
				SA1[i] = s1[SA1[i]];

			// init SA[n1..n-1]
			for (int i = n1; i < size; i++)
				SA[i] = -1;

			for (int i = n1 - 1; i >= 0; i--)
			{
				auto j = SA[i];
				SA[i] = -1;
				SA[--bkt[chr(j, cs, s)]] = j;
			}

			induceSAl(ls_types, SA, s, bkt.data(), size, K, cs, false);
			induceSAs(ls_types, SA, s, bkt.data(), size, K, cs, true);
		}
	}


	// Find the longest common prefix between two suffixes.
	template <typename T, std::integral Int>
	size_t LongestCommonPrefix(const T* data, size_t size, Int* sa, size_t sa_size, size_t a, size_t b)
	{
		size_t lcp = 0;
		while (a + lcp < size && b + lcp < size && data[a + lcp] == data[b + lcp])
			lcp++;
		return lcp;
	}

	// See if a substring is in the suffix array.
	template <typename T, std::integral Int>
	bool Contains(const T* data, size_t size, Int* sa, size_t sa_size, const T* sub, size_t sub_size)
	{
		// Binary search for the substring
		size_t low = 0;
		size_t high = size;
		while (low < high)
		{
			size_t mid = (low + high) / 2;
			size_t lcp = LongestCommonPrefix(data, size, sa, sa_size, mid, 0);
			if (lcp == sub_size)
				return true;
			if (lcp < sub_size)
			{
				if (data[sa[mid] + lcp] < sub[lcp])
					low = mid + 1;
				else
					high = mid;
			}
			else
			{
				if (data[sa[mid] + lcp] < sub[lcp])
					low = mid + 1;
				else
					high = mid;
			}
		}
		return false;
	}

	// Count the occurrances of a substring in the suffix array.
	template <typename T, std::integral Int>
	size_t Count(const T* data, size_t size, Int* sa, size_t sa_size, const T* sub, size_t sub_size)
	{
		// Binary search for the substring
		size_t low = 0;
		size_t high = size;
		while (low < high)
		{
			size_t mid = (low + high) / 2;
			size_t lcp = LongestCommonPrefix(data, size, sa, sa_size, mid, 0);
			if (lcp == sub_size)
			{
				size_t count = 1;
				size_t left = mid;
				size_t right = mid;
				while (left > 0)
				{
					size_t lcp = LongestCommonPrefix(data, size, sa, sa_size, left - 1, 0);
					if (lcp < sub_size)
						break;
					count++;
					left--;
				}
				while (right < size)
				{
					size_t lcp = LongestCommonPrefix(data, size, sa, sa_size, right + 1, 0);
					if (lcp < sub_size)
						break;
					count++;
					right++;
				}
				return count;
			}
			if (lcp < sub_size)
			{
				if (data[sa[mid] + lcp] < sub[lcp])
					low = mid + 1;
				else
					high = mid;
			}
			else
			{
				if (data[sa[mid] + lcp] < sub[lcp])
					low = mid + 1;
				else
					high = mid;
			}
		}
		return 0;
	}

	// Return the locations of the occurrances of a substring 
	template <typename T, std::integral Int>
	std::vector<size_t> Find(const T* data, size_t size, Int* sa, size_t sa_size, const T* sub, size_t sub_size)
	{
		std::vector<size_t> locations;
		// Binary search for the substring
		size_t low = 0;
		size_t high = size;
		while (low < high)
		{
			size_t mid = (low + high) / 2;
			size_t lcp = LongestCommonPrefix(data, size, sa, sa_size, mid, 0);
			if (lcp == sub_size)
			{
				size_t left = mid;
				size_t right = mid;
				while (left > 0)
				{
					size_t lcp = LongestCommonPrefix(data, size, sa, sa_size, left - 1, 0);
					if (lcp < sub_size)
						break;
					locations.push_back(left - 1);
					left--;
				}
				while (right < size)
				{
					size_t lcp = LongestCommonPrefix(data, size, sa, sa_size, right + 1, 0);
					if (lcp < sub_size)
						break;
					locations.push_back(right + 1);
					right++;
				}
				return locations;
			}
			if (lcp < sub_size)
			{
				if (data[sa[mid] + lcp] < sub[lcp])
					low = mid + 1;
				else
					high = mid;
			}
			else
			{
				if (data[sa[mid] + lcp] < sub[lcp])
					low = mid + 1;
				else
					high = mid;
			}
		}
		return locations;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::container
{
	PRUnitTest(SuffixArrayTests)
	{
		//                   0123456789ab
		char const data[] = "abracadabra";
		int sa[12];
		suffix_array::Build(&data[0], sizeof(data), sa, 256, 1);
		PR_CHECK(sa[0], 11);
		PR_CHECK(sa[1], 10);
		PR_CHECK(sa[2], 7);
		PR_CHECK(sa[3], 0);
		PR_CHECK(sa[4], 3);
		PR_CHECK(sa[5], 5);
		PR_CHECK(sa[6], 8);
		PR_CHECK(sa[7], 1);
		PR_CHECK(sa[8], 4);
		PR_CHECK(sa[9], 6);
		PR_CHECK(sa[10], 9);
		PR_CHECK(sa[11], 2);


		//PR_CHECK(suffix_array::Contains(data, 12, sa, 12, "aca", 3), true);
		//PR_CHECK(suffix_array::Contains(data, 12, sa, 12, "cad", 3), true);
		//PR_CHECK(suffix_array::Contains(data, 12, sa, 12, "dabra", 5), true);

	}
}
#endif
