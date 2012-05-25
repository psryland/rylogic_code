//*********************************************
// Bit Array
//  Copyright © Rylogic Ltd 2007
//*********************************************
#ifndef PR_BIT_ARRAY_H
#define PR_BIT_ARRAY_H

#include <bitset>
#include <vector>

namespace pr
{
	// Use std::bitset for compile time sets
	template <std::size_t N>
	class bitset : std::bitset<N> {};

	// Dynamic bitset
	// 'n' is always a bit index
	template <typename WordType = unsigned int>
	class bitsetRT
	{
		enum { BitsPerWord = sizeof(WordType) * 8, Mask = ~(BitsPerWord - 1) };
		typedef std::vector<WordType> TBits;
		TBits m_bits;
		
		WordType			word(std::size_t n) const			{ return m_bits[n / BitsPerWord]; }
		WordType&			word(std::size_t n)					{ return m_bits[n / BitsPerWord]; }

	public:
		// Proxy for a single bit
		class reference
		{	
			friend class bitsetRT;
			bitsetRT*	m_bs;
			std::size_t m_n;
			reference(bitsetRT& bs, std::size_t n) :m_bs(&bs) ,m_n(n) {}
		public:
			reference& operator = (bool val)					{ m_bs->set(m_n, val); return *this; }
			reference& operator = (const reference& ref)		{ m_bs->set(m_n, bool(ref)); return *this; }
			reference& flip()									{ m_bs->flip(m_n); return *this; }
			bool operator~() const								{ return !m_bs->test(m_n); }
			operator bool() const								{ return m_bs->test(m_n); }
		};

		bool				empty() const						{ return m_bits.empty(); }
		std::size_t			size() const						{ return m_bits.size() * BitsPerWord; }
		void				resize(std::size_t count)			{ m_bits.resize((count + BitsPerWord - 1) / BitsPerWord); }
		void				reset()								{ if( !empty() ) { memset(&m_bits[0], 0x00, m_bits.size() * sizeof(WordType))); } }
		void				reset(std::size_t n)				{ word(n) &= ~(1 << (n & Mask)); }
		void				set()								{ if( !empty() ) { memset(&m_bits[0], 0xFF, m_bits.size() * sizeof(WordType))); } }
		void				set(std::size_t n)					{ word(n) |=  (1 << (n & Mask)); }
		void				set(std::size_t n, bool val)		{ return val ? set(n) : reset(n); }

//		bool				any() const							{ PR_STUB_FUNC(); return false; }
//		std::size_t			count() const						{ PR_STUB_FUNC(); return 0; }
//		BitSet<NumBits>&	flip()								{ PR_STUB_FUNC(); return *this; }
//		BitSet<NumBits>&	flip(std::size_t n)					{ PR_STUB_FUNC(); return *this; }
//		bool				none() const						{ PR_STUB_FUNC(); return false; }
		bool				testW(std::size_t n) const			{ return word(n) != 0; }
		bool				test(std::size_t n) const			{ return ((word(n) >> (n & Mask)) & 1) != 0; }
//		// to_string - Converts a bitset object to a string representation.
//		// to_ulong -  Converts a bitset object to the integer that would generate the sequence of bits contained if used to initialize the bitset.

		// Operators 
		bool				operator[](std::size_t n) const		{ return test(n); }
		reference			operator[](std::size_t n)			{ return reference(*this, n); }
	};
//
//operator!=
// Tests a target bitset for inequality with a specified bitset.
// 
//operator&=
// Performs a bitwise combination of bitsets with the logical AND operation.
// 
//operator<<
// Shifts the bits in a bitset to the left a specified number of positions and returns the result to a new bitset.
// 
//operator<<=
// Shifts the bits in a bitset to the left a specified number of positions and returns the result to the targeted bitset.
// 
//operator==
// Tests a target bitset for equality with a specified bitset.
// 
//operator>>
// Shifts the bits in a bitset to the right a specified number of positions and returns the result to a new bitset.
// 
//operator>>=
// Shifts the bits in a bitset to the right a specified number of positions and returns the result to the targeted bitset.
// 
//operator[]
// Returns a reference to a bit at a specified position in a bitset if the bitset is modifiable; otherwise, it returns the value of the bit at that position.
// 
//operator^=
// Performs a bitwise combination of bitsets with the exclusive OR operation.
// 
//operator|=
// Performs a bitwise combination of bitsets with the inclusive OR operation.
// 
//operator~
// Toggles all the bits in a target bitset and returns the result.
//
//	//std::bitset

}//namespace pr

#endif//PR_BIT_ARRAY_H
