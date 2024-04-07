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
#include <string>
#include <vector>
#include <concepts>
#include <span>

namespace pr::suffix_array
{
	// Longest Common Prefix
	struct LCP
	{
		// The length of the match
		size_t length = 0;

		// The index of the match
		size_t sa_index = 0;
	};

	namespace impl
	{
		// Find the start or end of each bucket
		template <std::integral Int>
		void GetBuckets(std::span<Int const> data, std::span<int> bkt, bool end)
		{
			// Compute the size of each bucket
			std::fill(bkt.begin(), bkt.end(), 0);
			for (auto d : data)
				bkt[d]++;

			// Convert the counts into an accumulation
			int sum = 0;
			for (auto& b : bkt)
			{
				sum += b;
				b = end ? sum : sum - b;
			}
		}

		// Compute Suffix Array L
		template <std::integral Int>
		void InduceSuffixArrayL(std::vector<bool> const& s_types, std::span<int> SA, std::span<Int const> data, std::span<int> bkt, bool end)
		{
			// Find the starts of the buckets
			GetBuckets<Int>(data, bkt, end);
			for (auto i = 0; i != std::ssize(data); ++i)
			{
				auto j = SA[i] - 1;
				if (j >= 0 && !s_types[j])
					SA[bkt[data[j]]++] = j;
			}
		}

		// Compute Suffix Array S
		template <std::integral Int>
		void InduceSuffixArrayS(std::vector<bool> const& s_types, std::span<int> SA, std::span<Int const> data, std::span<int> bkt, bool end)
		{
			// Find the ends of the buckets
			GetBuckets<Int>(data, bkt, end);
			for (auto i = std::ssize(data) - 1; i >= 0; --i)
			{
				auto j = SA[i] - 1;
				if (j >= 0 && s_types[j])
					SA[--bkt[data[j]]] = j;
			}
		}
	}

	// Construct the suffix array of 'data' where each element is in the range [0, alphabet_size).
	// Requires a sentinal (0) at data[n-1]
	template <std::integral Int>
	void Build(std::span<Int const> data, std::span<int> SA, int alphabet_size)
	{
		// Uses a working space (excluding 'data' and 'SA') of at most 2.25n+O(1) for a constant alphabet.
		//
		// Basic notation commonly used in the presentations of this algorithm.
		// Let S be a string of n characters stored in an array[0..n − 1], and Σ(S) be the alphabet of S.
		// For a substring, S[i]S[i + 1]...S[j] in S, we denote it as S[i..j].
		// For presentation simplicity, S is supposed to be terminated by a sentinel $, which is the unique
		// lexicographically smallest character in S (using a sentinel is widely adopted in the literature for SACAs [5]).
		// Let suf(S, i) be the sufﬁx in S starting at S[i] and running to the sentinel.
		// A sufﬁx suf (S, i) is said to be S-type or L-type if suf (S, i) < suf(S, i + 1) or suf(S, i) > suf(S, i + 1), respectively.
		// The last sufﬁx suf(S, n − 1) consisting of only the single character $ (the sentinel) is deﬁned as S-type.
		// Correspondingly, we can classify a character S[i] to be S-type or L-type if suf(S, i) is S-type or L-type, respectively.
		// To store the type of every character/sufﬁx, we introduce an n-bit boolean array t, where t[i] records the type of character
		// S[i] as well as sufﬁx suf(S, i): 1 for S-type and 0 for L-type.
		// From the S-type and L-type deﬁnitions, we observe the following properties:
		//   1) S[i] is S-type if (i.1) S[i] < S[i + 1] or (i.2)S[i] = S[i + 1] and suf(S, i + 1) is S-type.
		//   2) S[i] isL-type if (ii.1) S[i] > S[i + 1] or (ii.2) S[i] = S[i + 1] andsuf(S, i + 1) is L-type.
		// These properties suggest that by scanning S once from right to left, we can determine the type of each
		// character/sufﬁx in O(1) time and ﬁll out the type array in O(n) time.
		//
		// So:
		//  S-type means "this suffix is smaller than the next adjacent suffix".
		//  L-type means "this suffix is larger than the next adjacent suffix".

		using namespace impl;

		// Handle degenerate cases
		if (data.empty())
		{
			return;
		}
		if (data.size() > SA.size())
		{
			throw std::runtime_error("The output suffix array size must be >= input data size");
		}
		if (data.back() != 0)
		{
			throw std::runtime_error("There must be a sentinel at the end of 'data'");
		}
		if (data.size() == 1)
		{
			SA[0] = 0;
			return;
		}

		// L/S-type array in bits. Suffixes that aren't S-type are L-type.
		auto s_types = std::vector<bool>(std::size(data));
		static auto IsLeftmostSType = [](int i, std::vector<bool> const& s_types) -> bool
		{
			return i > 0 && s_types[i] && !s_types[i - 1];
		};

		// Classify the type of each character into S or L types.
		// The sentinel must be in 'data', important!!!
		ls_types[std::ssize(data) - 2] = 0; // One before the sentinal
		ls_types[std::ssize(data) - 1] = 1; // The expected sentinal position
		for (auto i = std::ssize(data) - 3; i >= 0; --i)
		{
			s_types[i] =
				(data[i] <  data[i + 1]) ||
				(data[i] == data[i + 1] && s_types[i + 1]);
		}

		// Stage 1: reduce the problem by at least 1/2. Sort all the S-substrings
		{
			auto bkt = std::vector<int>(alphabet_size + 1);

			// Find ends of buckets
			GetBuckets(data, bkt, true);
			std::fill(SA.begin(), SA.end(), -1);
			for (int i = 1; i != std::ssize(data); ++i)
			{
				if (IsLeftmostSType(i, s_types))
					SA[--bkt[data[i]]] = i;
			}
			InduceSuffixArrayL(s_types, SA, data, bkt, false);
			InduceSuffixArrayS(s_types, SA, data, bkt, true);
		}

		// Compact all the sorted substrings into the first 'n1' items of SA.
		// 2*n1 is not larger than 'size' (proveable)
		int n1 = 0;
		for (int i = 0; i != std::ssize(data); ++i)
			if (IsLeftmostSType(SA[i], s_types))
				SA[n1++] = SA[i];

		// Find the lexicographic names of substrings.
		std::fill(SA.begin() + n1, SA.end(), -1);
		int name = 0, prev = -1;
		for (int i = 0; i < n1; ++i)
		{
			int pos = SA[i];
			bool diff = false;
			for (int d = 0; d < std::ssize(data); ++d)
			{
				if (prev == -1 || data[pos + d] != data[prev + d] || s_types[pos + d] != s_types[prev + d])
				{
					diff = true;
					break;
				}
				if (d > 0 && (IsLeftmostSType(pos + d, s_types) || IsLeftmostSType(prev + d, s_types)))
				{
					break;
				}
			}
			if (diff)
			{
				name++;
				prev = pos;
			}
			pos = (pos % 2 == 0) ? pos / 2 : (pos - 1) / 2;
			SA[n1 + pos] = name - 1;
		}

		for (int i = (int)std::ssize(data) - 1, j = (int)std::ssize(data) - 1; i >= n1; --i)
		{
			if (SA[i] >= 0)
				SA[j--] = SA[i];
		}

		// Stage 2: solve the reduced problem.
		// Recurse if names are not yet unique.
		auto SA1 = SA.subspan(0, n1);
		auto s1 = SA.subspan(std::ssize(data) - n1, n1);
		if (name < n1)
		{
			Build<int>(s1, SA1, name - 1);
		}
		else
		{
			// Generate the suffix array of s1 directly
			for (int i = 0; i != n1; ++i)
				SA1[s1[i]] = i;
		}

		// Stage 3: induce the result for the original problem.
		{
			auto bkt = std::vector<int>(alphabet_size + 1);

			// put all the LMS characters into their buckets
			// find ends of buckets
			GetBuckets(data, bkt, true);
			for (int i = 1, j = 0; i < std::ssize(data); ++i)
				if (IsLeftmostSType(i, s_types))
					s1[j++] = i; // get p1

			// Get index in src
			for (int i = 0; i < n1; i++)
				SA1[i] = s1[SA1[i]];

			// Init SA[n1..n-1]
			std::fill(SA.begin() + n1, SA.end(), -1);
			for (int i = n1 - 1; i >= 0; i--)
			{
				auto j = SA[i];
				SA[i] = -1;
				SA[--bkt[data[j]]] = j;
			}

			InduceSuffixArrayL(s_types, SA, data, bkt, false);
			InduceSuffixArrayS(s_types, SA, data, bkt, true);
		}
	}

	// Find the longest common prefix for 'sub' and 'data' (using the suffix array).
	template <std::integral Int>
	LCP LongCommonPrefix(std::span<Int const> sub, std::span<Int const> data, std::span<int const> sa)
	{
		// Binary search for 'sub' in 'data'
		size_t lcp0 = 0, lcp1 = 0; // track the longest common prefix for the lower/upper bounds
		for (size_t low = 0, high = sa.size(); ; )
		{
			auto mid = (low + high) / 2;
			auto sign = 0;

			// Find the longest common prefix at 'mid'. We know the prefix is common up to 'lcp' so far.
			auto prefix = data.subspan(sa[mid]);
			auto match_length = std::min(lcp0, lcp1);
			for (; ; ++match_length)
			{
				if (match_length == sub.size())
					return { match_length, mid };

				sign = match_length < prefix.size()
					? sub[match_length] - prefix[match_length]
					: -1;
				
				if (sign != 0)
					break;
			}

			// Update the search range
			if (sign > 0)
			{
				low = mid + 1;
				lcp0 = match_length;
			}
			else
			{
				high = mid;
				lcp1 = match_length;
			}

			if (low == high)
			{
				return { std::min(lcp0, lcp1), low };
			}
		}
	}

	// See if substring 'sub' occurs in 'data' (using the suffix array).
	template <std::integral Int>
	bool Contains(std::span<Int const> sub, std::span<Int const> data, std::span<int const> sa)
	{
		auto [length,_] = LongCommonPrefix(sub, data, sa);
		return length == sub.size();
	}
	
	// Count the occurrences of a substring in the suffix array.
	template <std::integral Int>
	size_t Count(std::span<Int const> sub, std::span<Int const> data, std::span<int const> sa)
	{
		// Find the node in the SA that matches 'sub'.
		auto [length,_] = LongCommonPrefix(sub, data, sa);
		if (length != sub.size())
			return 0;

		// Count the number of leaves in the sub trees.
		return 0; //todo
	}


	// Return the locations of the occurrances of a substring 
	template <typename T, std::integral Int>
	std::vector<size_t> Find(const T* data, size_t size, Int* sa, size_t sa_size, const T* sub, size_t sub_size)
	{
		return {};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::container
{
	PRUnitTest(SuffixArrayTests)
	{
		{// String data
			//                   0123456789ab
			char const data[] = "abracadabra";

			int sa[sizeof(data)];

			suffix_array::Build<char>(data, sa, 256);

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

			PR_CHECK(suffix_array::Contains<char>({ "aca", 3 }, data, sa), true);
			PR_CHECK(suffix_array::Contains<char>({ "db", 2 }, data, sa), false);
			PR_CHECK(suffix_array::Contains<char>({ "abracadabra", 11 }, data, sa), true);
			PR_CHECK(suffix_array::Contains<char>({ "rab", 3 }, data, sa), false);

			//PR_CHECK(suffix_array::Count<char>({ "ab", 2 }, data, sa) == 2, true);

		}
		//{// Binary data
		//	//                    0    1    2    3    4    5    6    7    8    9    a    b
		//	int const data[] = { 'a', 'b', 'r', 'a', 'c', 'a', 'd', 'a', 'b', 'r', 'a' };
		//
		//	int sa[sizeof(data)];
		//	suffix_array::Build(&data[0], sizeof(data), sa, 256, 1);
		//	PR_CHECK(sa[0], 11);
		//	PR_CHECK(sa[1], 10);
		//	PR_CHECK(sa[2], 7);
		//	PR_CHECK(sa[3], 0);
		//	PR_CHECK(sa[4], 3);
		//	PR_CHECK(sa[5], 5);
		//	PR_CHECK(sa[6], 8);
		//	PR_CHECK(sa[7], 1);
		//	PR_CHECK(sa[8], 4);
		//	PR_CHECK(sa[9], 6);
		//	PR_CHECK(sa[10], 9);
		//	PR_CHECK(sa[11], 2);
		//}
	}
}
#endif
