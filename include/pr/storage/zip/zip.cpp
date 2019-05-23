//*****************************************
// Zip Compression
//	Rylogic 2019
//*****************************************
// This code was refactored from http://read.pudn.com/downloads3/5582/LZRW3-A.C__.htm
//*******************************************
// BRIEF DESCRIPTION OF THE LZRW3-A ALGORITHM                                 
// ==========================================                                 
// Note: Before attempting to understand this algorithm, you should first     
// understand the LZRW3 algorithm from which this algorithm is derived.       
//                                                                            
// The LZRW3-A algorithm is identical to the LZRW3 algorithm except that the  
// hash table has been "deepened". The LZRW3 algorithm has a hash table of
// 4096 pointers which point to strings in the buffer. LZRW3-A generalizes    
// this to 4096/(2^n) partitions each of which contains (2^n) pointers.       
// In LZRW3-A, the hash function hashes to a partition number.              
//                                                                            
// During the processing of each phrase, LZRW3 overwrites the pointer in the  
// position selected by the hash function. LZRW3-A overwrites one of the    
// pointers in the partition that was selected by the hash function.        
//                                                                            
// When searching for a match, LZRW3-A matches against all (2^n) strings      
// pointed to by the pointers in the target partition.                        
//                                                                            
// Deep hash tables were used in early versions of LZRW1 in late 1989, but  
// were discarded in an effort to increase speed (which was the primary       
// requirement for LZRW1). They were revived for use in LZRW3-A in order to   
// produce an algorithm with compression performance competitive with Unix    
// compress.                                                                  
//                                                                            
// Until 14-Jul-1991, deep hash tables used in prototype LZRW* algorithms   
// used a queue discipline within each partition. Upon the arrival of a new   
// pointer, the pointers in the partition would be block copied back one      
// position (with the oldest pointer being overwritten) and the new pointer   
// being inserted in the space at the front (the youngest position).          
// This meant that pointers to the (2^n) most recent phrases corresponding to 
// each hash was kept. The only flaw in this system was the time-consuming  
// block copy operation which was cheap for shallow tables but expensive for  
// deep tables.                                                               
//                                                                            
// The traditional solution to ring buffer block copy problems is to maintain 
// a cyclic counter which points to the "head" of the queue. However, this    
// would have required one counter to be stored for each partition and would  
// have been slightly messy. After some thought (on 14-Jul-1991) a better     
// solution was found. Instead of maintaining a counter for each partition,   
// LZRW3-A maintains a single counter for all partitions! This counter is     
// maintained in both the compressor and decompressor and means that the      
// algorithm (effectively) overwrites a RANDOM element of the partition to be 
// updated. The result was to increase the speed of the compressor and        
// decompressor, to make the decompressor's speed independent from whatever   
// depth was selected, and to impair compression by less than 1% absolute.    
//                                                                            
// Setting the depth is a speed/compression tradeoff. The table below gives   
// the tradeoff observed for a typical 50K text file on a Mac-SE.             
// Note: %Rem=Percentage Remaining (after compression).                       
//                                                                            
//      Depth    %Rem    CmpK/s  DecK/s                                       
//          1    45.2    14.77   32.24                                        
//          2    42.6    12.12   31.26                                        
//          4    40.9    10.28   31.91                                        
//          8    40.0     7.81   32.36                                        
//         16    39.5     5.30   32.47                                        
//         32    39.0     3.23   32.59                                        
//                                                                            
// I have chosen a depth of 8 as the "default" depth for LZRW3-A. If you use  
// a depth different to this (e.g. 4), you should use the name LZRW3-A(4) to  
// indicate that a different depth is being used. LZRW3-A(8) is an acceptable 
// longhand for LZRW3-A.                                                      
//                                                                            
// To change the depth, search for "HERE IT IS" in the rest of this file.     
//                                                                            
//                                  +---+                                     
//                                  |___|4095                                 
//                                  |===|                                     
//              +---------------------*_|<---+   /----+---\                   
//              |                   |___|    +---|Hash    |                   
//              |    512 partitions |___|        |Function|                   
//              |    of 8 pointers  |===|        \--------/                   
//              |    each (or any   |___|0            ^                       
//              |    a*b=4096)      +---+             |                       
//              |                   Hash        +-----+                       
//              |                   Table       |                             
//              |                              ---                            
//              v                              ^^^                            
//      +-------------------------------------|----------------+              
//      ||||||||||||||||||||||||||||||||||||||||||||||||||||||||              
//      +-------------------------------------|----------------+              
//      |                                     |1......18|      |              
//      |<------- Lempel=History ------------>|<--Ziv-->|      |              
//      |     (=bytes already processed)      |<-Still to go-->|              
//      |<-------------------- INPUT BLOCK ------------------->|              
//                                                                            
//                                                                            
//***************************************************************************
//                                                                            
//                     DEFINITION OF COMPRESSED FILE FORMAT                   
//                     ====================================                   
//  * A compressed file consists of a COPY FLAG followed by a REMAINDER.      
//  * The copy flag CF uses up four bytes with the first byte being the       
//    least significant.                                                      
//  * If CF=1, then the compressed file represents the remainder of the file  
//    exactly. Otherwise CF=0 and the remainder of the file consists of zero  
//    or more GROUPS, each of which represents one or more bytes.             
//  * Each group consists of two bytes of CONTROL information followed by     
//    sixteen ITEMs except for the last group which can contain from one      
//    to sixteen items.                                                       
//  * An item can be either a LITERAL item or a COPY item.                    
//  * Each item corresponds to a bit in the control bytes.                    
//  * The first control byte corresponds to the first 8 items in the group    
//    with bit 0 corresponding to the first item in the group and bit 7 to    
//    the eighth item in the group.                                           
//  * The second control byte corresponds to the second 8 items in the group  
//    with bit 0 corresponding to the ninth item in the group and bit 7 to    
//    the sixteenth item in the group.                                        
//  * A zero bit in a control word means that the corresponding item is a     
//    literal item. A one bit corresponds to a copy item.                     
//  * A literal item consists of a single byte which represents itself.       
//  * A copy item consists of two bytes that represent from 3 to 18 bytes.    
//  * The first  byte in a copy item will be denoted C1.                      
//  * The second byte in a copy item will be denoted C2.                      
//  * Bits will be selected using square brackets.                            
//    For example: C1[0..3] is the low nibble of the first control byte.      
//    of copy item C1.                                                        
//  * The LENGTH of a copy item is defined to be C1[0..3]+3 which is a number 
//    in the range [3,18].                                                    
//  * The INDEX of a copy item is defined to be C1[4..7]*256+C2[0..8] which   
//    is a number in the range [0,4095].                                      
//  * A copy item represents the sequence of bytes                            
//       text[POS-OFFSET..POS-OFFSET+LENGTH-1] where                          
//          text   is the entire text of the uncompressed string.             
//          POS    is the index in the text of the character following the    
//                   string represented by all the items preceeding the item  
//                   being defined.                                           
//          OFFSET is obtained from INDEX by looking up the hash table.     
//                                                                            
//***************************************************************************

#include <type_traits>
#include <memory.h>
#include "pr/storage/zip/zip.h"

namespace pr::storage::zip
{
	using uint = unsigned int;
	using uint8 = unsigned char;
	using uint32 = unsigned int;

	// This size of the header for the compressed data.
	static int const HeaderBytes = 3 * sizeof(uint32);

	// This constant defines the number of pointers in the hash table. The number
	// of partitions multiplied by the number of pointers in each partition must
	// multiply out to this value of 4096. In LZRW1, LZRW1-A, and LZRW2, this
	// table length value can be changed. However, in LZRW3-A (and LZRW3), the
	// table length cannot be changed because it is connected directly to the
	// coding scheme which is hardwired (the table index of a single pointer is
	// transmitted in the 12-bit index field). So don't change this constant!
	static int const HashTableLength = 4096;

	// The following constant defines the maximum length of an uncompressed item. 
	// This definition must not be changed; its value is hardwired into the code. 
	// The longest number of bytes that can be spanned by a single item is 18     
	// for the longest copy item.                                                 
	static uint const MaxRawItemSize = 18;

	// The following constant defines the maximum length of a compressed group.   
	// This definition must not be changed; its value is hardwired into the code. 
	// A compressed group consists of two control bytes followed by up to 16      
	// compressed items each of which can have a maximum length of two bytes.     
	static uint const MaxCompressedGroupSize = 2 + 16 * 2;

	// The following constant defines the maximum length of an uncompressed group.
	// This definition must not be changed; its value is hardwired into the code. 
	// A group contains at most 16 items which explains this definition.          
	static uint const MaxDecompressedGroupSize = 16 * MaxRawItemSize;

	// A header for the compressed data
	struct CompressedDataHeader
	{
		static const uint CompressedDataIdentifier = ('P' << 8) | ('R' << 16) | ('Z' << 24);
		static const uint CompressedDataIdentifierMask = 0xFFFFFF00;
		static const uint CompressionFlag_Compressed = 0x00000001;
		static const uint CompressionLevelMask = 0x000000F0;
		static const uint CompressionFlag_Copy = 0x00000000;

		uint32 m_compression_flags;
		uint32 m_uncompressed_data_size;
		uint32 m_compressed_data_size;

		bool IsZipData() const
		{
			return (m_compression_flags & CompressedDataIdentifierMask) == CompressedDataIdentifier;
		}
		bool IsCompressed() const
		{
			return (m_compression_flags & CompressionFlag_Compressed) != 0;
		}
		ELevel GetCompressionLevel() const
		{
			return static_cast<ELevel>((m_compression_flags & CompressionLevelMask) >> 4);
		}
	};

	// Initial hash table values. Pointers in the hash table point to these strings initially.
	uint8 const StartString[8][18] =
	{
		{ ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' },
		{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
		{ 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 },
		{ 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0 },
		{ 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
		{ 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0 },
		{ 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
		{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
	};
	uint const NumStartStrings = sizeof(StartString) / sizeof(StartString[0]);
	static_assert(NumStartStrings == 8);

	class Zip
	{
		// The hash table
		union alignas(16) {
		uint8 const* m_hash[HashTableLength];
		};
		//using hash_table_t = std::aligned_storage_t<HashTableLength, 16U>;
		//PR_ALIGN(16, const uint8 * m_hash[HashTableLength];)	// The hash table

		// The following variables represent the literal buffer. m_hash_ptr1 points to
		// the partition (i.e. the zero'th (first) element of the partition)
		// corresponding to the youngest literal. m_hash_ptr2 points to the partition
		// corresponding to the second youngest literal.
		// The value zero denotes an "empty" buffer value with m_hash_ptr1 = 0 => m_hash_ptr2 = 0.
		uint8 const** m_hash_ptr1;
		uint8 const** m_hash_ptr2;
		uint8 const** m_hash_ptr0; // Pointer to current partition.

		// The variables 'm_control_ptr' and 'm_control' are used to buffer control bits.
		// Before each group is processed, the next two bytes of the output block
		// are set aside for the control word for the group about to be processed.
		// 'm_control_ptr' is set to point to the first byte of that word. Meanwhile,
		// 'control' buffers the control bits being generated during the processing
		// of the group. Instead of having a counter to keep track of how many items
		// have been processed (=the number of bits in the control word), at the
		// start of each group, the top word of 'control' is filled with 1 bits.
		// As 'control' is shifted for each item, the 1 bits in the top word are
		// absorbed or destroyed. When they all run out (i.e. when the top word is
		// all zero bits, we know that we are at the end of a group.
		uint8* m_control_ptr;
		uint32 m_control;

		// The following variable holds the current 'cycle' value. This value cycles
		// through the range [0,HashTableDepth-1], being incremented every time
		// the hash table is updated. The value gives the within-partition number of
		// the next pointer to be overwritten. The decompressor maintains a cycle
		// value in synchrony.
		uint m_cycle;

		// HERE IT IS: THE PLACE TO CHANGE THE HASH TABLE DEPTH!
		// The following definition is the log_2 of the depth of the hash table. This
		// constant can be in the range [0,1,2,3,...,12]. Increasing the depth
		// increases compression at the expense of speed. However, you are not likely
		// to see much of a compression improvement (e.g. not more than 0.5%) above a
		// value of 6 and the algorithm will start to get very slow. See the table in
		// the earlier comments block for an idea of the trade-off involved.
		// Note: The parentheses are to avoid macro substitution funnies.
		// Note: The LZRW3-A default is a value of (3).
		// Note: If you end up choosing a value of 0, you should use LZRW3 instead.
		// Note: Changing the value of HASH_TABLE_DEPTH_BITS is the ONLY thing you
		// have to do to change the depth, so go ahead and recompile now!
		// Note: I have tested LZRW3-A for DEPTH_BITS=0,1,2,3,4 and a few other
		// values. However, I have not tested it for 12 as I can't wait that long!
		uint m_compression_level_bits; // Must be in range [0,12].
		uint m_compression_level;
		uint m_hash_mask;
		uint m_depth_mask;

		// In/Out data pointers
		uint8 const* m_src_start;
		uint8 const* m_src_end;
		uint8* m_dst_start;
		uint8* m_dst_end;

	public:

		Zip()
			:m_hash()
			,m_hash_ptr1()
			,m_hash_ptr2()
			,m_hash_ptr0()
			,m_control_ptr()
			,m_control()
			,m_cycle()
			,m_compression_level_bits()
			,m_compression_level()
			,m_hash_mask()
			,m_depth_mask()
			,m_src_start()
			,m_src_end()
			,m_dst_start()
			,m_dst_end()
		{
			// Initialize all elements of the hash table to point to a constant string.
			auto ptr = m_hash;
			for (uint i = 0; i < HashTableLength; ++i)
				*ptr++ = StartString[i % NumStartStrings];
		}

		// Compress data
		void Compress(void const* data, size_t data_length, void* compressed, ELevel level)
		{
			m_src_start = static_cast<uint8 const*>(data);
			m_dst_start = static_cast<uint8*>(compressed);
			m_src_end = m_src_start + data_length;
			m_dst_end = m_dst_start + GetCompressionBufferSize(data_length);
			SetCompressionLevel(level);

			// Leave room for the header
			auto dst = m_dst_start + HeaderBytes;

			if (!BeginGroup(dst))
				return CompressCopy();

			auto loop_count = 0;
			auto src = m_src_start;
			auto end = m_src_end - MaxRawItemSize;
			while (src < end)
			{
				++loop_count;
				auto src_loop_start = src;

				// To process the next phrase, we hash the next three bytes to obtain
				// an index to the zeroth (first) pointer in a target partition. We get the pointer.
				auto index = Hash(src); // Index of current partition.
				m_hash_ptr0 = &m_hash[index];

				// This next part runs through the pointers in the partition matching
				// the bytes they point to in the Lempel with the bytes in the Ziv.
				// The length (bestlen) and within-partition pointer number (bestpos)
				// of the longest match so far is maintained and is the output of this
				// segment of code. The s[bestlen]==... is an optimization only.
				uint bestlen = 0;	// Holds the best length seen so far.
				uint bestpos = 0;	// Holds number of best pointer seen so far.
				for (uint d = 0; d < m_compression_level; ++d) // Depth looping variable.
				{
					auto s = src;
					auto p = m_hash_ptr0[d];
					if (s[bestlen] == p[bestlen])
					{
						#define PS if (*p++ == *s++)
						PS PS PS PS PS PS PS PS PS
						PS PS PS PS PS PS PS PS PS
						s++;
						#undef PS

						auto len = (uint)(s - src - 1);
						if (len > bestlen)
						{
							bestpos = d;
							bestlen = len;
						}
					}
				}

				// The length of the longest match determines
				// whether we code a literal item or a copy item.
				if (bestlen < 3) // Literal.
				{
					// Code the literal byte as itself and a zero control bit.
					*dst++ = *src++;
					m_control &= 0xFFFEFFFF;

					// We have just coded a literal. If we had two pending ones, that
					// makes three and we can update the hash table.
					if (m_hash_ptr2 != 0)
					{
						UpdateHashTable(m_hash_ptr2, src_loop_start - 2);
					}

					// In any case, rotate the hash table pointers for next time.
					m_hash_ptr2 = m_hash_ptr1;
					m_hash_ptr1 = m_hash_ptr0;
				}
				else // Copy
				{
					// To code a copy item, we construct a hash table index of the
					// winning pointer (index += bestpos) and code it and the best length
					// into a 2 byte code word.
					index += bestpos;
					*dst++ = static_cast<uint8>(((index & 0xF00) >> 4) | (bestlen - 3));
					*dst++ = static_cast<uint8>(index & 0xFF);
					src += bestlen;

					// As we have just coded three bytes, we are now in a position to
					// update the hash table with the literal bytes that were pending
					// upon the arrival of extra context bytes.
					if (m_hash_ptr1 != 0)
					{
						if (m_hash_ptr2 != 0)
						{
							UpdateHashTable(m_hash_ptr2, src_loop_start - 2);
							m_hash_ptr2 = 0;
						}
						UpdateHashTable(m_hash_ptr1, src_loop_start - 1);
						m_hash_ptr1 = 0;
					}

					// In any case, we can update the hash table based on the current
					// position as we just coded at least three bytes in a copy items.
					UpdateHashTable(m_hash_ptr0, src_loop_start);
				}
				m_control >>= 1;

				// If this is the end of a group...
				if ((loop_count % 16) == 0)
				{
					loop_count = 0;
					EndGroup();
					if (!BeginGroup(dst))
						return CompressCopy();
				}
			}

			// Copy the remaining data as literal data and update the control word
			while (src < m_src_end)
			{
				// Code the literal byte as itself and a zero control bit.
				*dst++ = *src++;
				m_control &= 0xFFFEFFFF;
				m_control >>= 1;

				++loop_count;
				if ((loop_count % 16) == 0)
				{
					loop_count = 0;
					EndGroup();
					if (!BeginGroup(dst))
						return CompressCopy();
				}
			}

			// At this point all the input bytes have been processed. However, the control
			// word has still to be written to the word reserved for it in the output.
			// Before writing, the control word has to be shifted so that all the bits
			// are in the right place. The "empty" bit positions are filled with 1s
			// which partially fill the top word.
			while (m_control & 0xFFFF0000U) m_control >>= 1;
			EndGroup();

			// If the last group contained no items, delete the control word too.
			if (m_control_ptr == dst)
				dst -= 2;

			// Finally, write the header information
			auto& header = *reinterpret_cast<CompressedDataHeader*>(m_dst_start);
			header.m_compression_flags = CompressedDataHeader::CompressedDataIdentifier | (m_compression_level_bits << 4) | CompressedDataHeader::CompressionFlag_Compressed;
			header.m_uncompressed_data_size = static_cast<uint32>(data_length);
			header.m_compressed_data_size = static_cast<uint32>(dst - m_dst_start);
		}

		// Decompress some data into 'decompressed'. 'decompressed' must point to a buffer at least 'GetDecompressedSize() bytes long
		void Decompress(void const* data, size_t data_length, void* decompressed, size_t decompressed_length)
		{
			throw std::exception("Not working yet");
			auto const& header = *reinterpret_cast<const CompressedDataHeader*>(data);
			if (!header.IsZipData())
				throw std::runtime_error("This is not compressed data");
			if (decompressed_length < GetDecompressedSize(data))
				throw std::runtime_error("Output buffer is too small");

			// Prepare the decompress
			m_src_start = static_cast<const uint8*>(data) + HeaderBytes;
			m_src_end = static_cast<const uint8*>(data) + data_length;
			m_dst_start = static_cast<uint8*>(decompressed);
			m_dst_end = m_dst_start + header.m_uncompressed_data_size;
			SetCompressionLevel(header.GetCompressionLevel());

			// If the "compressed" data is actually just a copy, then copy it to the destination buffer and leave
			if (!header.IsCompressed())
			{
				memcpy(m_dst_start, m_src_start, m_src_end - m_src_start);
				return;
			}

			m_control = 1;
			uint literals = 0;
			auto src = m_src_start;
			auto dst = m_dst_start;
			while (dst != m_dst_end)
			{
				if (src >= m_src_end)
					throw std::runtime_error("Compressed data format is incorrect");

				// When 'm_control' has the value 1, it means that the 16 buffered control
				// bits that were read in at the start of the current group have all been
				// shifted out and that all that is left is the 1 bit that was injected
				// into bit 16 at the start of the current group. When we reach the end
				// of a group, we have to load a new control word and inject a new 1 bit.
				if (m_control == 1)
				{
					m_control = 0x10000 | *src++;
					m_control |= (*src++) << 8;
				}

				// Process a literal or copy item depending on the next control bit.
				if (m_control & 1) // Copy item.
				{
					// Pointer to start of current Ziv.
					auto dst_loop_start = dst;

					// Read and dismantle the copy word. Work out from where to copy.
					auto lenmt = *src++;                         // Length of copy item minus three.
					auto index = ((lenmt & 0xF0) << 4) | *src++; // Index of hash table copy pointer.
					lenmt &= 0xF;

					// Now perform the copy
					memcpy(dst, m_hash[index], lenmt + 3);
					dst += lenmt + 3;

					// Because we have just received 3 or more bytes in a copy item
					// (whose bytes we have just installed in the output), we are now
					// in a position to flush all the pending literal hashings that had
					// been postponed for lack of bytes.
					if (literals > 0)
					{
						auto r = dst_loop_start - literals;
						UpdateHashTable(Hash(r), r);
						if (literals == 2)
						{
							++r;
							UpdateHashTable(Hash(r), r);
						}
						literals = 0;
					}

					// In any case, we can immediately update the hash table with the
					// current position. We don't need to do a Hash(...) to work out
					// where to put the pointer, as the compressor just told us!!!
					UpdateHashTable(index & (~m_depth_mask), dst_loop_start);
				}
				else // Literal item.
				{
					// Copy over the literal byte.
					*dst++ = *src++;

					// If we now have three literals waiting to be hashed into the hash
					// table, we can do one of them now (because there are three).
					if (++literals == 3)
					{
						auto p = dst - 3;
						UpdateHashTable(Hash(p), p);
						literals = 2;
					}
				}

				// Shift the control buffer so the next control bit is in bit 0.
				m_control >>= 1;
			}
		}

	private:

		// Set the level of compression to use
		void SetCompressionLevel(ELevel level)
		{
			if (level > ELevel::Max)
				level = ELevel::Max;

			m_compression_level_bits = static_cast<uint>(level);
			m_compression_level = 1 << m_compression_level_bits;

			// The following definitions are all self-explanatory and follow from the
			// definition of 'm_compression_level_bits' and the hardwired requirement that the
			// hash table contains exactly 4096 pointers.
			auto partition_length_bits = 12 - m_compression_level_bits;
			auto partition_length = 1 << partition_length_bits;
			m_hash_mask = partition_length - 1;
			m_depth_mask = m_compression_level - 1;
		}

		// Hash a pointer into a hash table index
		uint Hash(uint8 const* ptr) const
		{
			return (((40543 * ((*ptr << 8) ^ (*(ptr + 1) << 4) ^ (*(ptr + 2)))) >> 4) & m_hash_mask) << m_compression_level_bits;
		}

		// Another operation that is performed more than once is the updating of the
		// hash table. Here two methods are defined to simplify update operations.
		// Updating consists of identifying and overwriting a pointer in a partition
		// with a newer pointer and then updating the cycle value.
		// These methods accept the new pointer (NEWPTR) and either a pointer to
		// (P_BASE) or the index of (I_BASE) the zeroth (first, or base) pointer in
		// the partition that is to be updated. The macros use the 'cycle' variable
		// to locate and overwrite a pointer and then update the cycle value.
		void UpdateHashTable(uint8 const** base, uint8 const* new_pointer)
		{
			base[m_cycle++] = new_pointer;
			m_cycle &= m_depth_mask;
		}
		void UpdateHashTable(uint32 base, uint8 const* new_pointer)
		{
			m_hash[base + m_cycle++] = new_pointer;
			m_cycle &= m_depth_mask;
		}

		// Reserve the next word in the output for the control word
		bool BeginGroup(uint8*& dst)
		{
			m_control_ptr = dst;
			dst += 2;

			// Reset the control bits buffer.
			m_control = 0xFFFF0000U;

			// Return true if compression is actually smaller
			return dst + MaxCompressedGroupSize - 2 <= m_dst_end;
		}

		// Write the control word into the place saved for it in BeginGroup().
		void EndGroup()
		{
			// Write the control word to the place we saved for it in the output.
			*m_control_ptr++ = static_cast<uint8>((m_control) & 0xFF);
			*m_control_ptr++ = static_cast<uint8>((m_control >> 8) & 0xFF);
		}

		// Copy the data to the destination. Used in the condition that the 
		// compressed data is larger than the uncompressed data
		void CompressCopy()
		{
			// Write the header
			auto& header = *reinterpret_cast<CompressedDataHeader*>(m_dst_start);
			header.m_compression_flags = CompressedDataHeader::CompressedDataIdentifier | CompressedDataHeader::CompressionFlag_Copy;
			header.m_uncompressed_data_size = static_cast<uint32>(m_src_end - m_src_start);
			header.m_compressed_data_size = header.m_uncompressed_data_size + HeaderBytes;

			// Copy the data
			memcpy(m_dst_start + HeaderBytes, m_src_start, header.m_uncompressed_data_size);
		}
	};

	// Return the minimum size of a buffer than can be passed to the Compress function
	size_t GetCompressionBufferSize(size_t data_length)
	{
		return data_length + HeaderBytes;
	}

	// Return the size of the data once it's decompressed
	size_t GetDecompressedSize(void const* compressed_data)
	{
		auto const& header = *reinterpret_cast<CompressedDataHeader const*>(compressed_data);
		return header.m_uncompressed_data_size;
	}

	// Return the actual size of the compressed data including the header
	// This is the number of bytes past 'compressed_data' that actually need saving
	size_t GetCompressedSize(void const* compressed_data)
	{
		auto const& header = *reinterpret_cast<CompressedDataHeader const*>(compressed_data);
		return header.m_compressed_data_size;
	}

	// Compress some data into 'compressed'. 'compressed' must point to at least 'GetCompressionBufferSize()' bytes
	void Compress(void const* data, size_t data_length, void* compressed, ELevel level)
	{
		Zip zipper;
		return zipper.Compress(data, data_length, compressed, level);
	}

	// Decompress some data into 'decompressed'. 'decompressed' must point to a buffer at least 'GetDecompressedSize() bytes long
	void Decompress(void const* data, size_t data_length, void* decompressed, size_t decompressed_length)
	{
		Zip zipper;
		return zipper.Decompress(data, data_length, decompressed, decompressed_length);
	}
}
