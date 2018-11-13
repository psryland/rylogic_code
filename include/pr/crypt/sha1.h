//*************************************************************************************************
// SHA1
//*************************************************************************************************
//  100% free public domain implementation of the SHA-1 algorithm
//  by Dominik Reichl <dominik.reichl@t-online.de>
//  Web: http://www.dominik-reichl.de/
//
// Reduced to a single header by me.
#pragma once

#include <array>
#include <fstream>
#include <intrin.h>

namespace pr
{
	namespace hash
	{
		// Class for computing the SHA-1 hash of blocks of data
		class SHA1
		{
			// Define variable types
			using byte = unsigned char;
			using u32  = unsigned int;
			union Block
			{
				byte c[64];
				u32 l[16];
			};

			// Member variables
			u32  m_state[5];
			u32  m_count[2];
			u32  m_reserved0[1]; // Memory alignment padding
			byte m_buffer[64];
			byte m_digest[20];
			u32  m_reserved1[3]; // Memory alignment padding

			byte m_workspace[64];
			Block* m_block; // SHA1 pointer to the byte array above

		public:

			SHA1()
			{
				m_block = (Block*)m_workspace;

				// SHA1 initialization constants
				m_state[0] = 0x67452301;
				m_state[1] = 0xEFCDAB89;
				m_state[2] = 0x98BADCFE;
				m_state[3] = 0x10325476;
				m_state[4] = 0xC3D2E1F0;

				m_count[0] = 0;
				m_count[1] = 0;
			}
			~SHA1()
			{
				// Reset memory for security reasons
				memset(&m_state[0], 0, sizeof(m_state));
			}

			// Add data to the hash
			void Add(void const* data, int len)
			{
				auto bytes = static_cast<byte const*>(data);
				auto length = static_cast<u32>(len);

				auto j = (m_count[0] >> 3) & 0x3F;
				if ((m_count[0] += (length << 3)) < (length << 3))
					++m_count[1]; // Overflow

				m_count[1] += (length >> 29);

				u32 i = 0;
				if ((j + length) > 63)
				{
					i = 64 - j;
					memcpy(&m_buffer[j], bytes, i);
					Transform(m_state, m_buffer, m_block);

					for (; (i + 63) < length; i += 64)
						Transform(m_state, &bytes[i], m_block);

					j = 0;
				}

				if ((length - i) != 0)
					memcpy(&m_buffer[j], &bytes[i], length - i);
			}

			// Finalize hash; call this after all data is added
			byte const (&Final())[20]
			{
				byte final_count[8];
				for (u32 i = 0; i < 8; ++i)
					final_count[i] = static_cast<byte>((m_count[((i >= 4) ? 0 : 1)] >> ((3 - (i & 3)) * 8) ) & 0xFF); // Endian independent

				Add((byte*)"\200", 1);

				while((m_count[0] & 504) != 448)
					Add((byte*)"\0", 1);

				Add(final_count, 8); // Cause a Transform()

				for (u32 i = 0; i < 20; ++i)
					m_digest[i] = static_cast<byte>((m_state[i >> 2] >> ((3 - (i & 3)) * 8)) & 0xFF);

				// Wipe variables for security reasons
				memset(m_buffer, 0, 64);
				memset(m_state, 0, 20);
				memset(m_count, 0, 8);
				memset(final_count, 0, 8);
				Transform(m_state, m_buffer, m_block);
				
				return Hash();
			}

			// Get the hash value
			byte const (&Hash() const)[20]
			{
				return m_digest;
			}

		private:

			// Private SHA-1 transformation
			static void Transform(u32* pState, byte const* pBuffer, Block* block)
			{
				#pragma region Helper functions

				// Rotate 'p_val32' by 'n' bits to the left
				auto RotateLeft = [](u32 x, u32 n)
				{
					#ifdef _MSC_VER
					return _rotl(x, n);
					#else
					return (x << n) | (x >> (32 - n));
					#endif
				};
				auto SHABLK0 = [=](u32 i)
				{
					return block->l[i] = (RotateLeft(block->l[i], 24) & 0xFF00FF00) | (RotateLeft(block->l[i], 8) & 0x00FF00FF); // LittleEndian
					//return m_block->l[i]; // BigEndian
				};
				auto SHABLK = [=](u32 i)
				{
					return block->l[i & 15] = RotateLeft(block->l[(i + 13) & 15] ^ block->l[(i + 8) & 15] ^ block->l[(i + 2) & 15] ^ block->l[i & 15], 1);
				};

				// SHA-1 rounds
				auto R0 = [=](u32 v, u32 w, u32 x, u32 y, u32& z, u32 i)
				{
					z += ((w&(x^y)) ^ y) + SHABLK0(i) + 0x5A827999 + RotateLeft(v, 5);
					w = RotateLeft(w, 30);
				};
				auto R1 = [=](u32 v, u32 w, u32 x, u32 y, u32& z, u32 i)
				{
					z += ((w&(x^y)) ^ y) + SHABLK(i) + 0x5A827999 + RotateLeft(v, 5);
					w = RotateLeft(w, 30);
				};
				auto R2 = [=](u32 v, u32 w, u32 x, u32 y, u32& z, u32 i)
				{
					z += (w^x^y) + SHABLK(i) + 0x6ED9EBA1 + RotateLeft(v, 5);
					w = RotateLeft(w, 30);
				};
				auto R3 = [=](u32 v, u32 w, u32 x, u32 y, u32& z, u32 i)
				{
					z += (((w | x)&y) | (w&x)) + SHABLK(i) + 0x8F1BBCDC + RotateLeft(v, 5);
					w = RotateLeft(w, 30);
				};
				auto R4 = [=](u32 v, u32 w, u32 x, u32 y, u32& z, u32 i)
				{
					z += (w^x^y) + SHABLK(i) + 0xCA62C1D6 + RotateLeft(v, 5);
					w = RotateLeft(w, 30);
				};
				#pragma endregion

				u32 a = pState[0], b = pState[1], c = pState[2], d = pState[3], e = pState[4];
				memcpy(block, pBuffer, 64);

				// 4 rounds of 20 operations each, loop unrolled
				R0(a,b,c,d,e, 0); R0(e,a,b,c,d, 1); R0(d,e,a,b,c, 2); R0(c,d,e,a,b, 3);
				R0(b,c,d,e,a, 4); R0(a,b,c,d,e, 5); R0(e,a,b,c,d, 6); R0(d,e,a,b,c, 7);
				R0(c,d,e,a,b, 8); R0(b,c,d,e,a, 9); R0(a,b,c,d,e,10); R0(e,a,b,c,d,11);
				R0(d,e,a,b,c,12); R0(c,d,e,a,b,13); R0(b,c,d,e,a,14); R0(a,b,c,d,e,15);
				R1(e,a,b,c,d,16); R1(d,e,a,b,c,17); R1(c,d,e,a,b,18); R1(b,c,d,e,a,19);
				R2(a,b,c,d,e,20); R2(e,a,b,c,d,21); R2(d,e,a,b,c,22); R2(c,d,e,a,b,23);
				R2(b,c,d,e,a,24); R2(a,b,c,d,e,25); R2(e,a,b,c,d,26); R2(d,e,a,b,c,27);
				R2(c,d,e,a,b,28); R2(b,c,d,e,a,29); R2(a,b,c,d,e,30); R2(e,a,b,c,d,31);
				R2(d,e,a,b,c,32); R2(c,d,e,a,b,33); R2(b,c,d,e,a,34); R2(a,b,c,d,e,35);
				R2(e,a,b,c,d,36); R2(d,e,a,b,c,37); R2(c,d,e,a,b,38); R2(b,c,d,e,a,39);
				R3(a,b,c,d,e,40); R3(e,a,b,c,d,41); R3(d,e,a,b,c,42); R3(c,d,e,a,b,43);
				R3(b,c,d,e,a,44); R3(a,b,c,d,e,45); R3(e,a,b,c,d,46); R3(d,e,a,b,c,47);
				R3(c,d,e,a,b,48); R3(b,c,d,e,a,49); R3(a,b,c,d,e,50); R3(e,a,b,c,d,51);
				R3(d,e,a,b,c,52); R3(c,d,e,a,b,53); R3(b,c,d,e,a,54); R3(a,b,c,d,e,55);
				R3(e,a,b,c,d,56); R3(d,e,a,b,c,57); R3(c,d,e,a,b,58); R3(b,c,d,e,a,59);
				R4(a,b,c,d,e,60); R4(e,a,b,c,d,61); R4(d,e,a,b,c,62); R4(c,d,e,a,b,63);
				R4(b,c,d,e,a,64); R4(a,b,c,d,e,65); R4(e,a,b,c,d,66); R4(d,e,a,b,c,67);
				R4(c,d,e,a,b,68); R4(b,c,d,e,a,69); R4(a,b,c,d,e,70); R4(e,a,b,c,d,71);
				R4(d,e,a,b,c,72); R4(c,d,e,a,b,73); R4(b,c,d,e,a,74); R4(a,b,c,d,e,75);
				R4(e,a,b,c,d,76); R4(d,e,a,b,c,77); R4(c,d,e,a,b,78); R4(b,c,d,e,a,79);

				// Add the working variables back into state
				pState[0] += a;
				pState[1] += b;
				pState[2] += c;
				pState[3] += d;
				pState[4] += e;

				// Wipe variables
				a = b = c = d = e = 0;
			}
		};

		// Return the SHA1 hash of the given data
		inline std::array<unsigned char, 20> Sha1Hash(void const* data, int length)
		{
			SHA1 sha1;
			sha1.Add(data, length);
			auto hash = sha1.Final();

			std::array<unsigned char, 20> ret;
			memcpy(ret.data(), &hash[0], ret.size());
			return ret;
		}

		// Hash file contents
		inline std::array<unsigned char, 20> Sha1HashFile(wchar_t const* filepath)
		{
			if (filepath == nullptr)
				throw std::exception("filepath is null");

			SHA1 sha1;
			std::array<char, 4096> buf;
			std::ifstream file(filepath, std::ios_base::binary);
			for (int read = 1; read != 0;)
			{
				read = file.good() ? int(file.read(&buf[0], buf.size()).gcount()) : 0;
				sha1.Add(buf.data(), int(read));
			}
			auto hash = sha1.Final();

			std::array<unsigned char, 20> ret;
			memcpy(ret.data(), &hash[0], ret.size());
			return ret;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::hash
{
	PRUnitTest(Sha1Tests)
	{
		using namespace pr::hash;

		char str0[] = "01234567890";
		char str1[] = "0123456789a";
		auto hash1 = Sha1Hash(str0, sizeof(str0));
		auto hash2 = Sha1Hash(str1, sizeof(str1));
		PR_CHECK(hash1 != hash2, true);
	}
}
#endif


/*
  Paul's changes
  - Made variable wiping not optional
  - Enabled utility functions, the link can remove them if they're not used
  - Enabled stl functions, the link can remove them if they're not used
  
  Version 2.1 - 2012-06-19
  - Destructor (resetting internal variables) is now only
    implemented if SHA1_WIPE_VARIABLES is defined (which is the
    default).
  - Renamed inclusion guard to contain a GUID.
  - Demo application is now using C++/STL objects and functions.
  - Unicode build of the demo application now outputs the hashes of both
    the ANSI and Unicode representations of strings.
  - Various other demo application improvements.

  Version 2.0 - 2012-06-14
  - Added 'limits.h' include.
  - Renamed inclusion guard and macros for compliancy (names beginning
    with an underscore are reserved).

  Version 1.9 - 2011-11-10
  - Added Unicode test vectors.
  - Improved support for hashing files using the HashFile method that
    are larger than 4 GB.
  - Improved file hashing performance (by using a larger buffer).
  - Disabled unnecessary compiler warnings.
  - Internal variables are now private.

  Version 1.8 - 2009-03-16
  - Converted project files to Visual Studio 2008 format.
  - Added Unicode support for HashFile utility method.
  - Added support for hashing files using the HashFile method that are
    larger than 2 GB.
  - HashFile now returns an error code instead of copying an error
    message into the output buffer.
  - GetHash now returns an error code and validates the input parameter.
  - Added ReportHashStl STL utility method.
  - Added REPORT_HEX_SHORT reporting mode.
  - Improved Linux compatibility of test program.

  Version 1.7 - 2006-12-21
  - Fixed buffer underrun warning that appeared when compiling with
    Borland C Builder (thanks to Rex Bloom and Tim Gallagher for the
    patch).
  - Breaking change: ReportHash writes the final hash to the start
    of the buffer, i.e. it's not appending it to the string anymore.
  - Made some function parameters const.
  - Added Visual Studio 2005 project files to demo project.

  Version 1.6 - 2005-02-07 (thanks to Howard Kapustein for patches)
  - You can set the endianness in your files, no need to modify the
    header file of the CSHA1 class anymore.
  - Aligned data support.
  - Made support/compilation of the utility functions (ReportHash and
    HashFile) optional (useful when bytes count, for example in embedded
    environments).

  Version 1.5 - 2005-01-01
  - 64-bit compiler compatibility added.
  - Made variable wiping optional (define SHA1_WIPE_VARIABLES).
  - Removed unnecessary variable initializations.
  - ROL32 improvement for the Microsoft compiler (using _rotl).

  Version 1.4 - 2004-07-22
  - CSHA1 now compiles fine with GCC 3.3 under Mac OS X (thanks to Larry
    Hastings).

  Version 1.3 - 2003-08-17
  - Fixed a small memory bug and made a buffer array a class member to
    ensure correct working when using multiple CSHA1 class instances at
    one time.

  Version 1.2 - 2002-11-16
  - Borlands C++ compiler seems to have problems with string addition
    using sprintf. Fixed the bug which caused the digest report function
    not to work properly. CSHA1 is now Borland compatible.

  Version 1.1 - 2002-10-11
  - Removed two unnecessary header file includes and changed BOOL to
    bool. Fixed some minor bugs in the web page contents.

  Version 1.0 - 2002-06-20
  - First official release.

  ================ Test Vectors ================

  SHA1("abc" in ANSI) =
    A9993E36 4706816A BA3E2571 7850C26C 9CD0D89D
  SHA1("abc" in Unicode LE) =
    9F04F41A 84851416 2050E3D6 8C1A7ABB 441DC2B5

  SHA1("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq"
    in ANSI) =
    84983E44 1C3BD26E BAAE4AA1 F95129E5 E54670F1
  SHA1("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq"
    in Unicode LE) =
    51D7D876 9AC72C40 9C5B0E3F 69C60ADC 9A039014

  SHA1(A million repetitions of "a" in ANSI) =
    34AA973C D4C4DAA4 F61EEB2B DBAD2731 6534016F
  SHA1(A million repetitions of "a" in Unicode LE) =
    C4609560 A108A0C6 26AA7F2B 38A65566 739353C5
*/
