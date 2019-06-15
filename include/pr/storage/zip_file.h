//*****************************************
// Zip Compression
//	Copyright (c) Rylogic 2019
//*****************************************
// This code is a refactored copy of: https://github.com/tfussell/miniz-cpp/blob/master/zip_file.hpp
// See file end for copyright notices
#pragma once

#include <memory>
#include <string>
#include <vector>
#include <unordered_map>
#include <limits>
#include <iostream>
#include <sstream>
#include <fstream>
#include <algorithm>
#include <filesystem>
#include <cstdint>
#include <cctype>
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <assert.h>
#include <time.h>
#include <sys/stat.h>
#if defined(_MSC_VER) || defined(__MINGW64__)
#include <sys/utime.h>
#elif defined(__MINGW32__)
#include <sys/utime.h>
#elif defined(__TINYC__)
#include <sys/utime.h>
#elif defined(__GNUC__) && _LARGEFILE64_SOURCE
#include <utime.h>
#else
#include <utime.h>
#endif

namespace pr::storage::zip
{
	template <typename TAlloc = std::allocator<void>, bool LittleEndian = true, bool UnalignedLoadStore = true>
	class ZipArchiveA
	{
	public:
		// Notes:
		//  - Format Reference: https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT
		//  - For more compatibility with zlib, miniz uses unsigned long for some parameters/struct members.
		//    Beware: mz_ulong can be either 32 or 64-bits!

		using mz_ulong = unsigned long;
		using mz_uint = unsigned int;
		using mz_bool = int;
		static_assert(sizeof(uint16_t) == 2);
		static_assert(sizeof(uint32_t) == 4);
		static_assert(sizeof(uint64_t) == 8);

		// Per-entry compression methods
		enum class EMethod :uint16_t
		{
			None = 0,
			Shrunk = 1,
			Reduce1 = 2,
			Reduce2 = 3,
			Reduce3 = 4,
			Reduce4 = 5,
			Implode = 6,
			Reserved_for_Tokenizing_Compression_Algorithm = 7,
			Deflate = 8,
			Deflate64 = 9,
			PKWARE_Data_Compression_Library_Imploding = 10,
			Reserved_by_PKWARE_1 = 11,
			BZIP2 = 12,
			Reserved_by_PKWARE_2 = 13,
			LZMA = 14,
			Reserved_by_PKWARE_3 = 15,
			IBM_CMPSC = 16,
			Reserved_by_PKWARE_4 = 17,
			IBM_TERSE = 18,
			IBM_LZ77 = 19, // z Architecture (PFS)
			JPEG_Variant = 96,
			WavPack = 97,
			PPMd = 98, // version I, Rev 1
			AE_x = 99, // encryption marker (see APPENDIX E)
		};

		// Flags
		enum class EZipFlags
		{
			None = 0,

			// Used when searching for items by name
			IgnoreCase = 1 << 0,

			// Used when searching for items by name
			IgnorePath = 1 << 1,

			// Used when extracting items. Does not calculate Crc's.
			IgnoreCrc = 1 << 2,
			
			// Used when opening an archive. Generates a hash table of zip entry names to
			// offsets allowing for faster access to contained files. Combine with 'IgnoreCase' and 'IgnorePath'
			FastNameLookup = 1 << 3,

			// Used in 'Extract' to copy data without decompressing it
			CompressedData = 1 << 4,
		};

		// Compression levels: 0-9 are the standard zlib-style levels, 10 is best possible compression (not zlib compatible, and may be very slow), MZ_DEFAULT_COMPRESSION=MZ_DEFAULT_LEVEL.
		enum class ECompressionLevel
		{
			None = 0,
			Fastest = 1,
			Default = 6,
			Best = 9,
			Uber = 10,
		};

		// Compression strategies.
		enum class ECompressionStrategy
		{
			DefaultStrategy = 0,
			Filtered = 1,
			HuffmanOnly = 2,
			RLE = 3,
			Fixed = 4,
		};

		// Flags that control compression
		enum class ECompressionFlags
		{
			// The low 12 bits are reserved to control the max # of hash probes per dictionary lookup (see MAX_PROBES_MASK).
			None = 0,

			// WRITE_ZLIB_HEADER: If set, the compressor outputs a zlib header before the deflate data, and the Adler-32 of the source data at the end. Otherwise, you'll get raw deflate data.
			WriteZLibHeader = 0x01000,

			// COMPUTE_ADLER32: Always compute the adler-32 of the input data (even when not writing zlib headers).
			ComputeAdler32 = 0x02000,

			// GREEDY_PARSING_FLAG: Set to use faster greedy parsing, instead of more efficient lazy parsing.
			GreedyParsing = 0x04000,

			// NONDETERMINISTIC_PARSING_FLAG: Enable to decrease the compressor's initialization time to the minimum, but the output may vary from run to run given the same input (depending on the contents of memory).
			NonDeterministicParsing = 0x08000,

			// RLE_MATCHES: Only look for RLE matches (matches with a distance of 1)
			RLEMatches = 0x10000,

			// FILTER_MATCHES: Discards matches <= 5 chars if enabled.
			FilterMatches = 0x20000,

			// FORCE_ALL_STATIC_BLOCKS: Disable usage of optimized Huffman tables.
			ForceAllStaticBlocks = 0x40000,

			// FORCE_ALL_RAW_BLOCKS: Only use raw (uncompressed) deflate blocks.
			ForceAllRawBlocks = 0x80000,
		};

		// Bit flags
		enum class EBitFlags :uint16_t
		{
			None = 0,
			Encrypted             = 1 << 0,
			CompressionFlagBit1   = 1 << 1, 
			CompressionFlagBit2   = 1 << 2,
			DescriptorUsedMask    = 1 << 3,
			Reserved1             = 1 << 4,
			PatchFile             = 1 << 5,
			StrongEncrypted       = 1 << 6,
			CurrentlyUnused1      = 1 << 7,
			CurrentlyUnused2      = 1 << 8,
			CurrentlyUnused3      = 1 << 9,
			CurrentlyUnused4      = 1 << 10,
			Utf8                  = 1 << 11, // filename and comment encoded using UTF-8
			ReservedPKWARE1       = 1 << 12,
			CDEncrypted           = 1 << 13, // Used when encrypting the Central Directory to indicate selected data values in the Local Header are masked to hide their actual values.
			ReservedPKWARE2       = 1 << 14,
			ReservedPKWARE3       = 1 << 15,
		};

		// The mode this archive is in
		enum class EMode
		{
			Invalid = 0,
			Reading = 1,
			Writing = 2,
		};

		#pragma pack(push, 1)

		// Local directory header
		struct LDH
		{
			static uint32_t const Signature = 0x04034b50; // PK34

			uint32_t Sig;
			uint16_t Version;
			EBitFlags BitFlags;
			EMethod  Method;
			uint16_t FileTime;
			uint16_t FileDate;
			uint32_t Crc;
			uint32_t CompressedSize;
			uint32_t UncompressedSize;
			uint16_t NameSize;
			uint16_t ExtraSize;

			// Following this header:
			// char    Name[NameSize];
			// uint8_t Extra[ExtraSize];
			// uint8_t Data[CompressedSize];

			LDH() = default;
			LDH(size_t item_name_size, size_t extra_size, size_t uncompressed_size, size_t compressed_size, uint32_t uncompressed_crc32, EMethod method, EBitFlags bit_flags, uint16_t dos_time, uint16_t dos_date)
				:Sig(Signature)
				,Version()
				,BitFlags(bit_flags)
				,Method(method)
				,FileTime(dos_time)
				,FileDate(dos_date)
				,Crc(uncompressed_crc32)
				,CompressedSize(compressed_size)
				,UncompressedSize(uncompressed_size)
				,NameSize(item_name_size)
				,ExtraSize(extra_size)
			{}
			std::string_view ItemName() const
			{
				return std::string_view(reinterpret_cast<char const*>(this + 1), NameSize);
			}
			std::span<void> Extra() const
			{
				return std::span<void>(reinterpret_cast<uint8_t const*>(this + 1) + NameSize, ExtraSize);
			}
			std::span<void> Data() const
			{
				return std::span<void>(reinterpret_cast<uint8_t const*>(this + 1) + NameSize + ExtraSize, CompressedSize);
			}
		};
		static_assert(sizeof(LDH) == 30);
		static_assert(std::is_pod_v<LDH>);

		// Central directory header
		struct CDH
		{
			static uint32_t const Signature = 0x02014b50;// PK12

			uint32_t Sig;
			uint16_t VersionMadeBy;
			uint16_t VersionNeeded;
			EBitFlags BitFlags;
			EMethod  Method;
			uint16_t FileTime;
			uint16_t FileDate;
			uint32_t Crc;
			uint32_t CompressedSize;
			uint32_t UncompressedSize;
			uint16_t NameSize;
			uint16_t ExtraSize;
			uint16_t CommentSize;
			uint16_t DiskNumberStart;
			uint16_t InternalAttributes;
			uint32_t ExternalAttributes;
			uint32_t LocalHeaderOffset;

			// Following this header:
			// char    ItemName[NameSize];
			// uint8_t Extra[ExtraSize];
			// uint8_t ItemComment[ItemCommentSize];

			CDH() = default;
			CDH(size_t name_size, size_t extra_size, size_t comment_size, size_t uncompressed_size, size_t compressed_size, uint32_t uncompressed_crc32, EMethod method, EBitFlags bit_flags, uint16_t dos_time, uint16_t dos_date, size_t local_header_ofs, uint32_t ext_attributes, uint16_t int_attributes)
				:Sig(Signature)
				,VersionMadeBy()
				,VersionNeeded(method == EMethod::Deflate ? 20 : 0)
				,BitFlags(bit_flags)
				,Method(method)
				,FileTime(dos_time)
				,FileDate(dos_date)
				,Crc(uncompressed_crc32)
				,CompressedSize(compressed_size)
				,UncompressedSize(uncompressed_size)
				,NameSize(name_size)
				,ExtraSize(extra_size)
				,CommentSize(comment_size)
				,DiskNumberStart()
				,InternalAttributes(int_attributes)
				,ExternalAttributes(ext_attributes)
				,LocalHeaderOffset(local_header_ofs)
			{}
			std::string_view ItemName() const
			{
				return std::string_view(reinterpret_cast<char const*>(this + 1), NameSize);
			}
			std::span<void> Extra() const
			{
				return std::span<void>(reinterpret_cast<uint8_t const*>(this + 1) + NameSize, ExtraSize);
			}
			std::string_view Comment() const
			{
				return std::string_view(reinterpret_cast<char const*>(this + 1) + NameSize + ExtraSize, CommentSize);
			}
			bool IsDirectory() const
			{
				return
					NameSize != 0 && *(reinterpret_cast<char const*>(this + 1) + NameSize - 1) == '/' ||
					// Bugfix: This code was also checking if the internal attribute was non-zero, which wasn't correct.
					// Most/all zip writers (hopefully) set DOS file/directory attributes in the low 16-bits, so check for the DOS directory flag and ignore the source OS ID in the created by field.
					// FIXME: Remove this check? Is it necessary - we already check the filename.
					has_flag(ExternalAttributes, DOSSubDirectoryFlag);
			}
			time_t Time() const
			{
				return DosTimeToTime(FileTime, FileDate);
			}
		};
		static_assert(sizeof(CDH) == 46);
		static_assert(std::is_pod_v<CDH>);

		// End of central directory
		struct ECDH
		{
			static uint32_t const Signature = 0x06054b50; // PK56

			uint32_t Sig;                 // Magic number signature
			uint16_t DiskNumber;          // The number of this disk in a multi-disk archive
			uint16_t CDirDiskNumber;      // The disk containing the start of the central directory
			uint16_t NumEntriesOnDisk;    // The number of entries on this disk
			uint16_t TotalEntries;        // The number of entries on the central directory
			uint32_t CDirSize;            // The central directory size
			uint32_t CDirOffset;          // Offset to the start of central directory, relative to the 'CDirDiskNumber' disk
			uint16_t CommentSize;         // ZIP comment

			// Following this header:
			//   char Comment[CommentLength];

			ECDH() = default;
			ECDH(uint16_t disk_number, uint16_t cdir_disk_number, uint16_t num_entries_on_disk, uint16_t total_entries, uint32_t cdir_size, uint32_t cdir_offset, uint16_t comment_size)
				:Sig(Signature)
				,DiskNumber(disk_number)
				,CDirDiskNumber(cdir_disk_number)
				,NumEntriesOnDisk(num_entries_on_disk)
				,TotalEntries(total_entries)
				,CDirSize(cdir_size)
				,CDirOffset(cdir_offset)
				,CommentSize(comment_size)
			{}
		};
		static_assert(sizeof(ECDH) == 22);
		static_assert(std::is_pod_v<ECDH>);

		#pragma pack(pop)

	private:

		using allocator_t = TAlloc;
		using alloc_traits_t = typename std::allocator_traits<allocator_t>;
		template <typename U> using allocator_u = typename alloc_traits_t::template rebind_alloc<U>;
		template <typename T> using vector_t = std::vector<T, allocator_u<T>>;
		using name_hash_index_pair_t = struct { uint64_t name_hash; int index; };
		using string_t = std::basic_string<char, allocator_u<char>>;
		using iarray_t = std::basic_string_view<uint8_t>;
		using oarray_t = vector_t<uint8_t>;

		static uint32_t const DOSSubDirectoryFlag = 0x10;
		static uint32_t const MaxIOBufferSize = 4096;//64 * 1024;
		static uint32_t const LZDictionarySize = 32768;

		// Read/Write function pointer types. Functions are expected to read/write 'n' bytes or throw.
		using read_func_t = void(*)(ZipArchiveA const& me, int64_t file_ofs, void* buf, size_t n);
		using write_func_t = void(*)(ZipArchiveA& me, int64_t file_ofs, void const* buf, size_t n);

		// Output stream interface. The compressor uses this interface to write compressed data. It'll typically be called TDEFL_OUT_BUF_SIZE at a time.
		using tinfl_put_buf_func_ptr = int (*)(void const* pBuf, int len, void *pUser);
		using tdefl_put_buf_func_ptr = mz_bool(*)(const void* pBuf, int len, void *pUser);

	private:

		// std compliant allocator 
		TAlloc m_alloc;

		// The mode this archive was opened as
		EMode const m_mode;

		// In reading mode, this is the size of the entire zip data including the central directory header.
		// In writing mode, this is the size of the data written to the output stream so far.
		size_t m_archive_size;

		// The number of entries in the archive
		int m_total_entries;

		// The byte alignment of entries in the archive
		int m_entry_alignment;

		// Construction flags
		EZipFlags m_flags;

		// In-memory copy of the central directory
		vector_t<uint8_t> m_central_dir;

		// Byte offsets to the start of the header for each entry 
		vector_t<uint32_t> m_cdir_index;

		// A lookup table from entry name hash to central directory index
		vector_t<name_hash_index_pair_t> m_central_dir_lookup;

		// Zip file
		std::filesystem::path m_filepath;
		mutable std::ifstream m_ifile;
		std::ofstream m_ofile;

		// Zip in-memory
		mutable iarray_t m_imem;
		oarray_t m_omem;

		// Read/Write functions that change depending on whether the archive is in memory or a file on disk.
		read_func_t m_read;
		write_func_t m_write;

		// Construct an empty archive
		ZipArchiveA(EZipFlags flags, int entry_alignment, EMode mode)
			:m_alloc()
			,m_mode(mode)
			,m_archive_size()
			,m_total_entries()
			,m_entry_alignment(entry_alignment)
			,m_flags(flags)
			,m_central_dir()
			,m_cdir_index()
			,m_central_dir_lookup()
			,m_filepath()
			,m_ifile()
			,m_ofile()
			,m_imem()
			,m_omem()
			,m_read(nullptr)
			,m_write(nullptr)
		{
			// Ensure user specified entry alignment is a power of 2.
			if (m_entry_alignment != 0 && (m_entry_alignment & (m_entry_alignment - 1)) != 0)
				throw std::runtime_error("Zip archive entry alignment must be a power of 2");
		}

	public:

		// Construct an empty archive ready for adding entries to
		ZipArchiveA(size_t reserve = 0, EZipFlags flags = EZipFlags::None, int entry_alignment = 0)
			:ZipArchiveA(flags, entry_alignment, EMode::Writing)
		{
			m_omem.reserve(reserve);
			m_write = InMemoryWriteFunc;
		}

		// Construct from an in-memory zip
		ZipArchiveA(void const* mem, size_t size, EZipFlags flags = EZipFlags::None, int entry_alignment = 0)
			:ZipArchiveA(flags, entry_alignment, EMode::Reading)
		{
			m_archive_size = size;
			m_imem = iarray_t(static_cast<uint8_t const*>(mem), size);
			m_read = InMemoryReadFunc;
			ReadCentralDirectory();
		}

		// Construct from an existing archive file
		ZipArchiveA(std::filesystem::path const& filepath, EZipFlags flags = EZipFlags::None, int entry_alignment = 0)
			:ZipArchiveA(flags, entry_alignment, EMode::Reading)
		{
			m_filepath = filepath;
			m_ifile = std::ifstream(filepath, std::ios::binary);
			m_archive_size = std::filesystem::file_size(filepath);
			m_read = FileReadFunc;
			ReadCentralDirectory();
		}
		
		// The number of items in the archive
		size_t Count() const
		{
			return m_total_entries;
		}

		// Return the central directory header entry for 'index'
		CDH const& ItemStat(int index) const
		{
			using namespace std::literals;

			if (index >= m_total_entries)
				throw std::runtime_error("Entry index ("s + std::to_string(index) + ") out of range ("s + std::to_string(m_total_entries) + ")"s);

			return *reinterpret_cast<CDH const*>(m_central_dir.data() + m_cdir_index[index]);
		}

		// Retrieves the name of an archive entry.
		std::string_view Name(int index) const
		{
			return ItemStat(index).ItemName();
		}

		// Retrieves the extra data associated with an archive entry
		std::span<void> Extra(int index) const
		{
			return ItemStat(index).Extra();
		}

		// Retrieves the comment associated with an archive entry
		std::string_view Comment(int index) const
		{
			return ItemStat(index).Comment();
		}

		// Determines if an archive file entry is a directory entry.
		bool ItemIsDirectory(int index) const
		{
			return ItemStat(index).IsDirectory();
		}

		// Searches the archive's central directory for an entry matching 'item_name' and 'item_comment' (if not null).
		// Valid flags: EZipFlags::IgnoreCase | EZipFlags::IgnorePath. Returns -1 if the file cannot be found.
		int IndexOf(std::string_view item_name, std::string_view item_comment = "", EZipFlags flags = EZipFlags::None) const
		{
			if (!ValidateItemName(item_name))
				throw std::runtime_error("Item name is invalid or too long");
			if (!ValidateItemComment(item_comment))
				throw std::runtime_error("Archive comment is invalid or too long");

			// See if the lookup hash table is available.
			// Check the flags used to create the cache are the same as the flags provided here.
			if (!m_central_dir_lookup.empty() && m_flags == flags)
			{
				// Get the range of items that match 'name'
				auto hash = Hash(item_name.data(), flags);
				auto matches = std::equal_range(begin(m_central_dir_lookup), end(m_central_dir_lookup), hash);
				for (auto p = matches.first; p != matches.second; ++p)
				{
					// Find a matching item name
					auto name = Name(p->index);
					if (Compare(item_name, name, flags) != 0)
						continue;

					// Check matching comment
					if (!item_comment.empty())
					{
						auto comment = Comment(p->index);
						if (Compare(item_comment, comment) != 0)
							continue;
					}

					// Found it
					return p->index;
				}
			}

			// Otherwise, fall back to a linear search
			else
			{
				for (int i = 0; i != m_total_entries; ++i)
				{
					// Find a matching item name
					auto name = Name(i);
					if (Compare(item_name, name, flags) != 0)
						continue;

					// Check matching comment
					if (!item_comment.empty())
					{
						auto comment = Comment(i);
						if (Compare(item_comment, comment) != 0)
							continue;
					}

					// Found it.
					return i;
				}
			}
			
			// Not found
			return -1;
		}

		// Add already compressed data.
		// 'item_name' is the entry name for the data to be added.
		// 'buf' is the already compressed data.
		// 'method' is the method that was used to compressed the data.
		// 'uncompressed_size' is the original size of the data.
		// 'uncompressed_crc' is the crc of the uncompressed data.
		void AddAlreadyCompressed(std::string_view item_name, std::span<void const> buf, size_t uncompressed_size, uint32_t uncompressed_crc32, EMethod method = EMethod::Deflate, std::span<void const> extra = {}, std::string_view item_comment = "")
		{
			// Sanity checks
			if (m_mode != EMode::Writing)
				throw std::runtime_error("ZipArchive not in writing mode");
			if (!ValidateItemName(item_name))
				throw std::runtime_error("Archive name is invalid or too long");
			if (!ValidateItemComment(item_comment))
				throw std::runtime_error("Item comment is invalid or too long");
			if (buf.size() > 0xFFFFFFFF || uncompressed_size > 0xFFFFFFFF)
				throw std::runtime_error("Data too large. Zip64 is not supported");
			if (level < ECompressionLevel::None || level > ECompressionLevel::Uber)
				throw std::runtime_error("Compression level out of range");
			if (m_total_entries >= 0xFFFF)
				throw std::runtime_error("Too many files added.");
			if (uncompressed_size == 0)
				throw std::runtime_error("Uncompressed data size must be provided when adding already compressed data.");

			// Overflow check
			auto num_alignment_padding_bytes = CalcAlignmentPadding();
			if (m_archive_size + m_central_dir.size() + num_alignment_padding_bytes + sizeof(CDH) + sizeof(LDH) + item_name.size() + extra.size() + item_comment.size() + buf.size() > 0xFFFFFFFF)
				throw std::runtime_error("Zip too large. Zip64 is not supported");

			EBitFlags bit_flags = 0;
			uint16_t int_attribytes = 0;
			uint32_t ext_attributes = 0;
			uint16_t dos_time = 0;
			uint16_t dos_date = 0;

			// Record the current time so the item can be date stamped.
			// Do this before compressing just in case compression takes a while
			TimeToDosTime(time(nullptr), dos_time, dos_date);

			// Reserve space for the entry in the central directory
			m_central_dir.reserve(m_central_dir.size() + sizeof(CDH) + item_name.size() + extra.size() + item_comment.size());
			m_cdir_index.reserve(m_cdir_index.size() + 1);

			// Insert offsets
			auto ldh_ofs = m_archive_size + num_alignment_padding_bytes;
			auto item_ofs = m_archive_size + num_alignment_padding_bytes + sizeof(LDH);
			assert(is_aligned(ldh_ofs) && "header offset should be aligned");

			// Write zeros for padding
			WriteZeros(m_archive_size, num_alignment_padding_bytes);

			// Write the local directory header
			LDH ldh(item_name.size(), extra.size(), uncompressed_size, buf.size(), uncompressed_crc32, method, bit_flags, dos_time, dos_date);
			m_write(*this, ldh_ofs, &ldh, sizeof(ldh));

			// Write the item name
			m_write(*this, item_ofs, item_name.data(), item_name.size());
			item_ofs += item_name.size();

			// Write the extra data
			m_write(*this, item_ofs, extra.data(), extra.size());
			item_ofs += extra.size();

			// Write the item data
			m_write(*this, item_ofs, buf.data(), buf.size());
			item_ofs += buf.size();

			// Add an entry to the central directory
			CDH cdh(item_name.size(), extra.size(), item_comment.size(), uncompressed_size, buf.size(), uncompressed_crc32, method, bit_flags, dos_time, dos_date, ldh_ofs, ext_attributes, int_attributes);
			m_central_dir.append(&cdh, &cdh + 1);
			m_central_dir.append(item_name.begin(), item_name.end());
			m_central_dir.append(extra.begin(), extra.end());
			m_central_dir.append(item_comment.begin(), item_comment.end());
			m_cdir_index.push_back(checked_cast<uint32_t>(m_archive_size));

			// Update stats
			m_archive_size = item_ofs;
			m_total_entries++;
		}

		// Compresses and adds the contents of a memory buffer to the archive.
		// To add a directory entry, call this method with an archive name ending in a forward slash and an empty buffer.
		// 'item_name' is the entry name for the data to be added.
		// 'buf' is the uncompressed data to be compressed and added.
		// 'method' is the method to use to compressed the data.
		// 'uncompressed_size' is the original size of the data.
		// 'uncompressed_crc' is the crc of the uncompressed data.
		void Add(std::string_view item_name, std::span<void const> buf, EMethod method = EMethod::Deflate, std::span<void const> extra = {}, std::string_view item_comment = "", ECompressionLevel level = ECompressionLevel::Default, ECompressionFlags comp_flags = ECompressionFlags::None, EZipFlags flags = EZipFlags::None)
		{
			// Sanity checks
			if (m_mode != EMode::Writing)
				throw std::runtime_error("ZipArchive not in writing mode");
			if (!ValidateItemName(item_name))
				throw std::runtime_error("Archive name is invalid or too long");
			if (!ValidateItemComment(item_comment))
				throw std::runtime_error("Item comment is invalid or too long");
			if (buf.size() > 0xFFFFFFFF)
				throw std::runtime_error("Data too large. Zip64 is not supported");
			if (level < ECompressionLevel::None || level > ECompressionLevel::Uber)
				throw std::runtime_error("Compression level out of range");
			if (m_total_entries >= 0xFFFF)
				throw std::runtime_error("Too many files added.");
			if (has_flag(flags, EZipFlags::CompressedData))
				throw std::runtime_error("Use the 'AddAlreadyCompressed' function to add compressed data.");

			// Overflow check
			auto num_alignment_padding_bytes = CalcAlignmentPadding();
			if (m_archive_size + m_central_dir.size() + num_alignment_padding_bytes + sizeof(CDH) + sizeof(LDH) + item_name.size() + extra.size() + item_comment.size() + buf.size() > 0xFFFFFFFF)
				throw std::runtime_error("Zip too large. Zip64 is not supported");

			// Don't compress if too small
			if (buf.size() <= 3)
				level = ECompressionLevel::None;

			EMethod method = EMethod::None;
			uint16_t bit_flags = 0;
			uint16_t int_attributes = 0;
			uint32_t ext_attributes = 0;
			uint32_t buf_crc32 = 0;
			uint64_t compressed_size = 0;
			uint16_t dos_time = 0;
			uint16_t dos_date = 0;

			// If the name has a directory divider at the end, set the directory bit
			if (item_name.back() == '/')
			{
				// Set DOS Subdirectory attribute bit.
				ext_attributes |= DOSSubDirectoryFlag;

				// Subdirectories cannot contain data.
				if (!buf.empty())
					throw std::runtime_error("Sub-directories cannot contain data.");
			}

			// Record the current time so the item can be date stamped.
			// Do this before compressing just in case compression takes a while
			TimeToDosTime(time(nullptr), dos_time, dos_date);

			// Reserve space for the entry in the central directory
			m_central_dir.reserve(m_central_dir.size() + sizeof(CDH) + item_name.size() + extra.size() + item_comment.size());
			m_cdir_index.reserve(m_cdir_index.size() + 1);

			// Insert offsets
			auto ldh_ofs = m_archive_size + num_alignment_padding_bytes;
			auto item_ofs = m_archive_size + num_alignment_padding_bytes + sizeof(LDH);
			assert(is_aligned(ldh_ofs) && "header offset should be aligned");

			// Write zeros for padding
			WriteZeros(m_archive_size, num_alignment_padding_bytes);

			// Write a dummy local directory header. This will be overwritten once the data has been compressed
			WriteZeros(ldh_ofs, sizeof(LDH));

			// Write the item name
			m_write(*this, item_ofs, item_name.data(), item_name.size());
			item_ofs += item_name.size();

			// Write the extra data
			m_write(*this, item_ofs, extra.data(), extra.size());
			item_ofs += extra.size();

			// Calculate the uncompressed crc
			if (!has_flag(flags, EZipFlags::IgnoreCrc))
				buf_crc32 = Crc(buf.data(), buf.size());

			// Write the compressed data
			if (level == ECompressionLevel::None)
			{
				m_write(*this, item_ofs, buf.data(), buf.size());
				item_ofs += buf.size();
				method = EMethod::None;
				compressed_size = buf.size();
			}
			else
			{
				mz_zip_writer_add_state state(item_ofs, 0);

				// Create a compressor
				auto mem = alloc_traits_t::allocate(m_alloc, sizeof(tdefl_compressor));
				std::unique_ptr<tdefl_compressor> compressor.reset(new (mem) tdefl_compressor(ZipWriterFunc, &state, CompressionFlagsFrom(level, -15, ECompressionStrategy::DefaultStrategy)));

				// Compress the data, writing it to the zip
				compresser->compress(buf, buf_size, TDEFL_FINISH);
				item_ofs = state.m_cur_archive_file_ofs;

				// Record the stats
				method = EMethod::Deflate;
				compressed_size = state.m_comp_size;
			}

			// Write the local directory header now that we have the compressed size
			LDH ldh(item_name.size(), extra.size(), buf.size(), compressed_size, buf_crc32, method, bit_flags, dos_time, dos_date);
			m_write(*this, ldh_ofs, &ldh, sizeof(ldh));

			// Add an entry to the central directory
			CDH cdh(item_name.size(), extra.size(), item_comment.size(), buf.size(), compressed_size, buf_crc32, method, bit_flags, dos_time, dos_date, ldh_ofs, ext_attributes, int_attributes);
			m_central_dir.append(&cdh, &cdh + 1);
			m_central_dir.append(item_name.begin(), item_name.end());
			m_central_dir.append(extra.begin(), extra.end());
			m_central_dir.append(item_comment.begin(), item_comment.end());
			m_cdir_index.push_back(checked_cast<uint32_t>(m_archive_size));

			// Update stats
			m_total_entries++;
			m_archive_size = item_ofs;
		}

		// Compresses and adds the contents of a disk file to an archive.
		// To add a directory entry, call this method with an archive name ending in a forward slash and an empty buffer.
		// 'item_name' is the entry name for the data to be added.
		// 'buf' is the uncompressed data to be compressed and added.
		// 'method' is the method to use to compressed the data.
		// 'uncompressed_size' is the original size of the data.
		// 'uncompressed_crc' is the crc of the uncompressed data.
		void Add(std::string_view item_name, std::filesystem::path const& src_filepath, EMethod method = EMethod::Deflate, std::span<void const> extra = {}, std::string_view item_comment = "", ECompressionLevel level = ECompressionLevel::Default, EZipFlags flags = EZipFlags::None)
		{
			using namespace std::literals;

			// Sanity checks
			if (m_zip_mode != EMode::Writing)
				throw std::runtime_error("ZipArchive not in writing mode");
			if (!ValidateItemName(item_name))
				throw std::runtime_error("Archive name is invalid or too long");
			if (!ValidateItemComment(item_comment))
				throw std::runtime_error("Item comment is invalid or too long");
			if (!std::filesystem::exists(src_filepath))
				throw std::runtime_error("Path '"s + src_filepath + "' does not exist"s);
			if (!std::filesystem::is_directory(src_filepath))
				throw std::runtime_error("Path '"s + src_filepath + "' is not a file"s);
			if (std::filesystem::file_size(src_filepath) > 0xFFFFFFFF)
				throw std::runtime_error("File '"s + src_filepath + "' is too large. Zip64 is not supported");
			if (level < ECompressionLevel::None || level > ECompressionLevel::Uber)
				throw std::runtime_error("Compression level out of range");
			if (has_flag(flags, EZipFlags::CompressedData))
				throw std::runtime_error("Use the 'AddAlreadyCompressed' function to add compressed data.");
			if (m_total_entries >= 0xFFFF)
				throw std::runtime_error("Too many files added.");

			// Overflow check
			auto num_alignment_padding_bytes = CalcAlignmentPadding();
			if (m_archive_size + m_central_dir.size() + num_alignment_padding_bytes + sizeof(CDH) + sizeof(LDH) + item_name.size() + extra.size() + item_comment.size() + buf.size() > 0xFFFFFFFF)
				throw std::runtime_error("Zip too large. Zip64 is not supported");

			// Open the source file
			auto src_file = std::ifstream(src_filepath, std::ios::binary);
			if (!src_file.good())
				throw std::runtime_error("Failed to open file '"s + src_filepath + "'");

			EMethod method = EMethod::None;
			uint16_t bit_flags = 0;
			uint16_t int_attributes = 0;
			uint32_t ext_attributes = 0;
			uint32_t file_crc32 = 0;
			uint64_t compressed_size = 0;
			uint16_t dos_time = 0;
			uint16_t dos_date = 0;

			// Don't compress if too small
			auto uncompressed_size = std::filesystem::file_size(src_filepath);
			if (uncompressed_size <= 3)
				level = ECompressionLevel::None;

			// Record the current time so the item can be date stamped.
			// Do this before compressing just in case compression takes a while
			TimeToDosTime(time(nullptr), dos_time, dos_date);

			// Reserve space for the entry in the central directory
			m_central_dir.reserve(m_central_dir.size() + sizeof(CDH) + item_name.size() + extra.size() + item_comment.size());
			m_cdir_index.reserve(m_cdir_index.size() + 1);

			// Insert offsets
			auto lhd_ofs = m_archive_size + num_alignment_padding_bytes;
			auto item_ofs = m_archive_size + num_alignment_padding_bytes + sizeof(LDH);
			assert(is_aligned(lhd_ofs) && "header offset should be aligned");

			// Write zeros for padding
			WriteZeros(m_archive_size, num_alignment_padding_bytes);

			// Write a dummy local directory header. This will be overwritten once the data has been compressed
			WriteZeros(ldh_ofs, sizeof(LDH));

			// Write the item name
			m_write(*this, item_ofs, item_name.data(), item_name.size());
			item_ofs += item_name.size();

			// Write the extra data
			m_write(*this, item_ofs, extra.data(), extra.size());
			item_ofs += extra.size();

			// Write the compressed data
			if (level == ECompressionLevel::None)
			{
				// Read from the file in blocks
				std::array<char, 4096> buf;
				for (auto remaining = uncompressed_size; remaining != 0; )
				{
					auto n = std::min<size_t>(buf.size(), remaining);
					if (!src_file.read(buf.data(), n).good())
						throw std::runtime_error("File read error when reading '"s + src_filepath + "'");
					
					// Calculate the CRC as we go
					file_crc32 = Crc(buf.data(), n, file_crc32);

					// Write the data into the archive
					m_write(*this, item_ofs, buf.data(), n);
					item_ofs += n;
					remaining -= n;
				}
				compressed_size = uncompressed_size;
				method = EMethod::None;
			}
			else
			{
				#if 0//todo
				mz_zip_writer_add_state state(item_ofs, 0);

				// Create a compressor
				auto mem = alloc_traits_t::allocate(m_alloc, sizeof(tdefl_compressor));
				std::unique_ptr<tdefl_compressor> compressor.reset(new (mem) tdefl_compressor(ZipWriterFunc, &state, CompressionFlagsFrom(level, -15, ECompressionStrategy::DefaultStrategy)));

				// Compress the data, writing to the zip
				std::array<char, 4096> buf;
				for (auto remaining = uncompressed_size; remaining != 0; )
				{
					auto n = std::min<size_t>(buf.size(), remaining);
					if (!src_file.read(buf.data(), n).good())
						throw std::runtime_error("File read error when reading '"s + src_filepath + "'");

					uncompressed_crc32 = Crc(buf.data(), n, uncompressed_crc32);
					compressor->tdefl_compress_buffer(buf.data(), n, remaining == n ? TDEFL_NO_FLUSH : TDEFL_FINISH);
					remaining -= n;
				}

				compressed_size = state.m_comp_size;
				item_ofs = state.m_cur_archive_file_ofs;
				#endif
				method = EMethod::Deflate;
			}

			// Write the local directory header now that we have the compressed size
			LDH ldh(item_name.size(), extra.size(), uncompressed_size, compressed_size, file_crc32, method, bit_flags, dos_time, dos_date);
			m_write(*this, ldh_ofs, &ldh, sizeof(ldh));

			// Add an entry to the central directory
			CDH cdh(item_name.size(), extra.size(), item_comment.size(), uncompressed_size, compressed_size, file_crc32, method, bit_flags, dos_time, dos_date, ldh_ofs, ext_attributes, int_attributes);
			m_central_dir.append(&cdh, &cdh + 1);
			m_central_dir.append(item_name.begin(), item_name.end());
			m_central_dir.append(extra.begin(), extra.end());
			m_central_dir.append(item_comment.begin(), item_comment.end());
			m_cdir_index.push_back(checked_cast<uint32_t>(m_archive_size));

			// Update stats
			m_total_entries++;
			m_archive_size = item_ofs;
		}

		// Extracts an archive entry to disk and restores its last accessed and modified times.
		// 'item_name' is the entry name for the item to extract.
		// 'dst_filepath' is where to write the extracted file.
		// 'index' is the item's index within the archive.
		// This function only extracts files, not archive directory records.
		void Extract(std::string_view item_name, std::filesystem::path const& dst_filepath, EZipFlags flags = EZipFlags::None) const
		{
			auto index = IndexOf(item_name, "", flags);
			return index >= 0 && index < m_total_entries
				? Extract(index, dst_filepath, flags)
				: throw std::runtime_error("Archive item not found");
		}
		void Extract(int index, std::filesystem::path const& dst_filepath, EZipFlags flags = EZipFlags::None) const
		{
			// Create the destination file
			auto outfile = std::ostream(dst_filepath, std::ios::binary);
			Extract(index, outfile, flags);
			outfile.close();

			// Set the file time on the extracted file to match the times recorded in the archive
			utimbuf time = {};
			auto stat = ItemStat(index);
			time.actime = stat.m_time;
			time.modtime = stat.m_time;
			if (utime(dst_filepath.c_str(), &time) != 0)
				throw std::runtime_error(strerror("Failed to update modified time."));
		}

		// Extracts an archive entry to a stream.
		// 'item_name' is the entry name for the item to extract.
		// 'out' is where to write the extracted file.
		// 'index' is the item's index within the archive.
		// This function only extracts files, not archive directory records.
		template <typename Elem = uint8_t>
		void Extract(std::string_view item_name, std::basic_ostream<Elem>& out, EZipFlags flags = EZipFlags::None) const
		{
			auto index = IndexOf(item_name, "", flags);
			return index >= 0 && index < m_total_entries
				? Extract(index, out, flags)
				: throw std::runtime_error("Archive item not found");
		}
		template <typename Elem = uint8_t>
		void Extract(int index, std::basic_ostream<Elem>& out, EZipFlags flags = EZipFlags::None) const
		{
			Extract(index, [&out](void*, uint64_t ofs, void const* buf, size_t n)
			{
				out.seekp(ofs);
				out.write(static_cast<Elem const*>(buf), n / sizeof(Elem));
			}, nullptr, flags);
		}

		// Extracts a archive entry using a callback function to output the uncompressed data.
		// OutputCB = void(void* ctx, uint64_t output_buffer_ofs, void const* buf, size_t len)
		// OutputCB is expected to copy 'buf[0:len) to '&somewhere[output_buffer_ofs]'.
		// OutputCB should throw if not all bytes can be copied.
		// 'output_buffer_ofs' is a convenience for output streams that do not have an internal 'file' pointer.
		template <typename OutputCB>
		void Extract(std::string_view item_name, OutputCB callback, void* ctx, EZipFlags flags = EZipFlags::None) const
		{
			auto index = IndexOf(item_name, "", flags);
			Extract(index, callback, ctx, flags);
		}
		template <typename OutputCB>
		void Extract(int index, OutputCB callback, void* ctx, EZipFlags flags = EZipFlags::None) const
		{
			using namespace std::literals;

			// Empty file, or a directory (but not always a directory - I've seen odd zips with directories that have compressed data which inflates to 0 bytes)
			auto& cdh = ItemStat(index);
			if (cdh.CompressedSize == 0)
				return;

			// Entry is a subdirectory (I've seen old zips with dir entries which have compressed deflate data which inflates to 0 bytes,
			// but these entries claim to uncompress to 512 bytes in the headers). I'm torn how to handle this case - should it fail instead?
			if (cdh.IsDirectory())
				throw std::runtime_error("Item is a directory entry. Only file items can be extracted");

			// Encryption and patch files are not supported.
			if (has_flag(cdh.BitFlags, EBitFlags::Encrypted) || has_flag(cdh.BitFlags, EBitFlags::PatchFile))
				throw std::runtime_error("Encryption and patch files are not supported");

			// This function only supports stored and deflate.
			if (cdh.Method != EMethod::Deflate && cdh.Method != EMethod::None && !has_flag(flags, EZipFlags::CompressedData))
				throw std::runtime_error("Unsupported compression method type: "s + std::to_string(int(cdh.Method)));

			// Read and parse the local directory entry.
			LDH ldh; m_read(*this, cdh.LocalHeaderOffset, &ldh, sizeof(ldh));
			if (ldh.Sig != LDH::Signature)
				throw std::runtime_error("Item header structure is invalid. Signature mismatch");

			// Get the byte offset to the start of the compressed data
			auto item_ofs = cdh.LocalHeaderOffset + sizeof(LDH) + ldh.NameSize + ldh.ExtraSize;
			if (item_ofs + cdh.CompressedSize > m_archive_size)
				throw std::runtime_error("Archive corrupt. Indicated item size exceeds the available data");

			// From input memory stream
			if (!m_imem.empty())
				ExtractFromMemory(callback, ctx, cdh, item_ofs, flags);
			else if (m_ifile.good())
				ExtractFromFile(callback, ctx, cdh, item_ofs, flags);
			else
				throw std::runtime_error("Input data stream not available");
		}

	private:

		// Read the top level directory structure contained in the zip and populate our state variables
		void ReadCentralDirectory()
		{
			// Basic sanity checks - reject files that are too small, and check the
			// first 4 bytes of the file to make sure a local header is there.
			if (m_archive_size < sizeof(ECDH))
				throw std::runtime_error("Archive is invalid. Smaller than header structure size");

			// The current position in the data
			auto ofs = m_archive_size;
			std::array<uint8_t, 4096> buf;

			// Find the end of central directory record by scanning the file from end to start.
			for (;;)
			{
				// Read a chunk
				auto n = std::min<int64_t>(buf.size(), ofs);
				m_read(*this, ofs - n, buf.data(), n);
				ofs -= n;

				// Search (backwards) for the CDH end marker
				for (uint32_t sig = 0; n-- != 0;)
				{
					sig = (sig << 8) | buf[n];
					if (sig == ECDH::Signature) break;
				}
				if (ofs == 0 && n == -1)
					throw std::runtime_error("Invalid zip. Central directory header not found");
				if (n == -1)
					continue;

				// Found the CDH end marker at '@buf[n]', move 'ofs' to the start of the ECDH.
				ofs += n;
				break;
			}

			// Read and verify the end of central directory record.
			ECDH ecdh; m_read(*this, ofs, &ecdh, sizeof(ecdh));
			if (ecdh.Sig != ECDH::Signature)
				throw std::runtime_error("Invalid zip. Central directory end marker not found");
			if (ecdh.TotalEntries != ecdh.NumEntriesOnDisk || ecdh.DiskNumber > 1)
				throw std::runtime_error("Invalid zip. Archives that span multiple disks are not supported");
			if (ecdh.CDirSize < ecdh.TotalEntries * sizeof(CDH))
				throw std::runtime_error("Invalid zip. Central directory size is invalid");
			if (ecdh.CDirOffset + ecdh.CDirSize > m_archive_size)
				throw std::runtime_error("Invalid zip. Central directory size exceeds archive size");

			// Read the central directory into memory.
			m_total_entries = ecdh.TotalEntries;
			m_central_dir.resize(ecdh.CDirSize);
			m_cdir_index.resize(m_total_entries);
			m_read(*this, ecdh.CDirOffset, m_central_dir.data(), m_central_dir.size());

			// Populate the index of offsets into the central directory
			auto p = m_central_dir.data();
			for (size_t n = ecdh.CDirSize, i = 0; i != m_total_entries; ++i)
			{
				auto& cdh = *reinterpret_cast<CDH const*>(p);

				// Sanity checks
				if (n < sizeof(CDH) || cdh.Sig != CDH::Signature)
					throw std::runtime_error("Invalid zip. Central directory header corrupt");
				if ((cdh.UncompressedSize != 0 && cdh.CompressedSize == 0) || cdh.UncompressedSize == 0xFFFFFFFF || cdh.CompressedSize == 0xFFFFFFFF)
					throw std::runtime_error("Invalid zip. Compressed and Decompressed sizes are invalid");
				if (cdh.Method == EMethod::None && cdh.UncompressedSize != cdh.CompressedSize)
					throw std::runtime_error("Invalid zip. Header indicates no compression, but compressed and decompressed sizes differ");
				if (cdh.DiskNumberStart != ecdh.DiskNumber && cdh.DiskNumberStart != 1)
					throw std::runtime_error("Unsupported zip. Archive spans multiple disks");
				if (cdh.LocalHeaderOffset + sizeof(LDH) + cdh.CompressedSize > m_archive_size)
					throw std::runtime_error("Invalid zip. Item size value exceeds actual data size");
				auto total_header_size = sizeof(CDH) + cdh.NameSize + cdh.ExtraSize + cdh.CommentSize;
				if (total_header_size > n)
					throw std::runtime_error("Invalid zip. Computed header size does not agree header end signature location");

				m_cdir_index[i] = checked_cast<uint32_t>(p - m_central_dir.data());
				n -= total_header_size;
				p += total_header_size;
			}

			// Generate a lookup table from name (hashed) to index
			if (has_flag(m_flags, EZipFlags::FastNameLookup))
			{
				m_central_dir_lookup.reserve(m_total_entries);
				for (int i = 0, iend = int(m_cdir_index.size()); i != iend; ++i)
				{
					auto name = Name(i);
					auto hash = Hash(name, m_flags);
					m_central_dir_lookup.push_back(name_hash_index_pair_t{ hash, i });
				}
				std::sort(begin(m_central_dir_lookup), end(m_central_dir_lookup));
			}
		}

		// Return the required padding needed to align an item in the archive
		int CalcAlignmentPadding() const
		{
			if (m_entry_alignment == 0) return 0;
			auto n = static_cast<int>(m_archive_size & (m_entry_alignment - 1));
			return (m_entry_alignment - n) & (m_entry_alignment - 1);
		}

		// Write zeros into the output
		void WriteZeros(int64_t ofs, size_t count)
		{
			static char const zeros[1024] = {};
			while (count)
			{
				auto sz = std::min<size_t>(sizeof(zeros), count);
				m_write(*this, ofs, zeros, sz);
				ofs += sz;
				count -= sz;
			}
		}

		// Extract from a zip archive in memory
		template <typename OutputCB>
		void ExtractFromMemory(OutputCB callback, void* ctx, CDH const& cdh, int64_t item_ofs, EZipFlags flags) const
		{
			if (m_imem.empty())
				throw std::runtime_error("There is no in-memory archive");

			// The item was stored uncompressed or the caller has requested the compressed data.
			if (cdh.Method == EMethod::None || has_flag(flags, EZipFlags::CompressedData))
			{
				// Zip64 check
				if constexpr (sizeof(size_t) == sizeof(uint32_t))
					if (cdh.CompressedSize > 0xFFFFFFFF)
						throw std::runtime_error("Item is too large. Zip64 is not supported");

				uint64_t ofs = 0;
				uint32_t crc32 = 0;

				// Calculate the crc if the call was not just for the compressed data
				if (!has_flag(flags, EZipFlags::CompressedData) && !has_flag(flags, EZipFlags::IgnoreCrc))
					crc32 = Crc(m_imem.data() + item_ofs, cdh.CompressedSize, crc32);

				// Send the data directly to the callback
				callback(ctx, ofs, m_imem.data() + item_ofs, size_t(cdh.CompressedSize));

				// All data sent
				item_ofs += cdh.CompressedSize;
				ofs += cdh.CompressedSize;
				return;
			}

			// Data is compressed, inflate before passing to callback
			if (cdh.Method == EMethod::Deflate)
			{
				// Decompress into a temporary buffer. The minimum buffer size must be 'LZDictionarySize'
				// because Deflate uses references to earlier bytes, upto an LZ dictionary size prior.
				Deflate algo;
				uint64_t ofs = 0;
				uint32_t crc32 = 0;
				vector_t<uint8_t> buf(LZDictionarySize);
				algo.Decompress(m_imem.data(), buf.data(), [&](uint8_t*& ptr)
				{
					auto count = size_t(ptr - buf.data());
					assert(count <= buf.size());

					// Update the crc
					crc32 = Crc(buf.data(), count, crc32);

					// Push the buffered data out to the callback
					callback(ctx, ofs, buf.data(), count);
					ofs += count;

					// Reset to the start of the buffer
					ptr = buf.data();
				}, Deflate::EFlags::ExpectZlibHeader);
				return;
			}
			
			using namespace std::literals;
			std::runtime_error("Unsupported compression method:"s + std::to_string(int(cdh.Method)));
		}

		// Extract from a zip archive file
		template <typename OutputCB>
		void ExtractFromFile(OutputCB callback, void* ctx, CDH const& cdh, int64_t item_ofs, EZipFlags flags) const
		{
			if (!m_ifile.good())
				throw std::runtime_error("There is no archive file");

			// The item was stored uncompressed or the caller has requested the compressed data.
			if (cdh.Method == EMethod::None || has_flag(flags, EZipFlags::CompressedData))
			{
				uint64_t ofs = 0;
				uint32_t crc32 = 0;

				// Zip is a file. Read chunks into a temporary buffer
				std::array<uint8_t, 4096> buf;
				for (size_t remaining = cdh.CompressedSize; remaining != 0;)
				{
					// Read chunk
					auto n = std::min<size_t>(buf.size(), remaining);
					m_read(*this, item_ofs, buf.data(), n);

					// Calculate the crc if the call was not just for the compressed data
					if (!has_flag(flags, EZipFlags::CompressedData) && !has_flag(flags, EZipFlags::IgnoreCrc))
						crc32 = Crc(buf.data(), n, crc32);

					// Send the data directly to the callback
					callback(ctx, ofs, buf.data(), n);

					// Accumulate
					remaining -= n;
					item_ofs += n;
					ofs += n;
				}
				return;
			}

			// Data is compressed, inflate before passing to callback
			if (cdh.Method == EMethod::Deflate)
			{
				Deflate algo;
				uint64_t ofs = 0;
				uint32_t crc32 = 0;

				m_ifile.seekg(item_ofs);
				auto src = std::istream_iterator<uint8_t>(m_ifile);

				// Decompress into a temporary buffer. The minimum buffer size must be 'LZDictionarySize'
				// because Deflate uses references to earlier bytes, upto an LZ dictionary size prior.
				vector_t<uint8_t> buf(LZDictionarySize);
				algo.Decompress(src, buf.data(), [&](uint8_t*& ptr)
				{
					auto count = size_t(ptr - buf.data());
					assert(count <= buf.size());

					// Update the crc
					crc32 = Crc(buf.data(), count, crc32);

					// Push the buffered data out to the callback
					callback(ctx, ofs, buf.data(), count);
					ofs += count;

					// Reset to the start of the buffer
					ptr = buf.data();
				});
				return;
			}

			using namespace std::literals;
			std::runtime_error("Unsupported compression method:"s + std::to_string(int(cdh.Method)));
		}

		// Lexicographically compare strings
		template <bool IgnorePath = false, bool IgnoreCase = false> static int Compare(std::string_view lhs, std::string_view rhs)
		{
			// One range empty => the empty range is less. Both ranges empty => equal
			if (lhs.empty() || rhs.empty())
				return int(rhs.empty()) - int(lhs.empty());

			// The ranges to compare
			auto lhs_beg = begin(lhs);
			auto rhs_beg = begin(rhs);
			auto lhs_end = end(lhs);
			auto rhs_end = end(rhs);

			// Exclude everything prior to the last '/', '\\', ':' character
			if constexpr (IgnorePath)
			{
				auto PathDivider = [](char c) { return c == '/' || c == '\\' || c == ':'; };

				auto p = lhs_end;
				for (p = lhs_end; p-- != lhs_beg && !PathDivider(*p);) {}
				lhs_beg = p + int(p != lhs_beg);
				for (p = rhs_end; p-- != rhs_beg && !PathDivider(*p);) {}
				rhs_beg = p + int(p != rhs_beg);
			}

			// Compare ordinal
			for (;lhs_beg != lhs_end && rhs_beg != rhs_end;)
			{
				int c = IgnoreCase
					? std::tolower(*lhs_beg++) - std::tolower(*rhs_beg++)
					: *lhs_beg++ - *rhs_beg++;

				if (c != 0)
					return c;
			}

			return int(rhs_beg == rhs_end) - int(lhs_beg == lhs_end);
		}
		static int Compare(std::string_view lhs, std::string_view rhs, EZipFlags flags = EZipFlags::None)
		{
			auto ignore_path = has_flag(flags, EZipFlags::IgnorePath);
			auto ignore_case = has_flag(flags, EZipFlags::IgnoreCase);
			return
				!ignore_path && !ignore_case ? Compare<false, false>(lhs, rhs) :
				!ignore_path &&  ignore_case ? Compare<false, true>(lhs, rhs) :
				 ignore_path && !ignore_case ? Compare<true, false>(lhs, rhs) :
				 ignore_path &&  ignore_case ? Compare<true, true>(lhs, rhs) :
				throw std::runtime_error("");
		}
		static bool Equals(std::string_view lhs, std::string_view rhs, EZipFlags flags = EZipFlags::None)
		{
			return Compare(lhs, rhs, flags) == 0;
		}

		// Generate a hash of 'name' based on 'flags'
		static uint64_t Hash(std::string_view name, EZipFlags flags)
		{
			if (name.empty())
				return 0;

			// Hash from end to start so that 'IgnorePath' quick outs at the first path divider
			uint64_t hash = 0;
			auto end = rend(name);
			auto p = rbegin(name);
			for (p += *p == '/'; p != end; ++p) // Skip the last '/' for sub-directories
			{
				hash = Hash64CT(has_flag(flags, EZipFlags::IgnoreCase) ? std::tolower(*p) : *p, hash);
				if (has_flag(flags, EZipFlags::IgnorePath) && (*p == '/' || *p == '\\' || *p == ':'))
					break;
			}
			return hash;
		}

		// Callback functions for reading/writing data in memory
		static void InMemoryReadFunc(ZipArchiveA const& me, int64_t ofs, void* buf, size_t n)
		{
			using namespace std::literals;
			if (ofs + n > me.m_archive_size)
				throw std::runtime_error("Out of bounds read (@ "s + std::to_string(ofs) + ") from zip memory buffer");

			memcpy(buf, me.m_imem.data() + ofs, n);
		}
		static void InMemoryWriteFunc(ZipArchiveA& me, int64_t ofs, void const* buf, size_t n)
		{
			me.m_omem.resize(std::max<size_t>(size_t(ofs + n), me.m_omem.size()));
			memcpy(me.m_omem.data() + ofs, buf, n);
		}

		// Callback function for reading/writing data from/to file
		static void FileReadFunc(ZipArchiveA const& me, int64_t ofs, void* buf, size_t n)
		{
			using namespace std::literals;
			if (!me.m_ifile.seekg(ofs, std::ios::beg).good())
				throw std::runtime_error("File seek read position to "s + std::to_string(ofs) + " failed");

			me.m_ifile.read(static_cast<char*>(buf), n);
		}
		static void FileWriteFunc(ZipArchiveA& me, int64_t ofs, void const* buf, size_t n)
		{
			using namespace std::literals;
			if (!me.m_ofile.seekp(ofs, std::ios::beg).good())
				throw std::runtime_error("File seek write position to "s + std::to_string(ofs) + " failed");

			me.m_ofile.write(static_cast<char const*>(buf), n);
		}

		// Callback for writing compressed data to the zip
		static void ZipWriterFunc(void const* buf, int len, void* ctx)
		{
			#if 0 // Finding compiler bug
			auto& pState = static_cast<mz_zip_writer_add_state*>(ctx);
			if ((int)pState->m_pZip->m_pWrite(pState->m_pZip->m_pIO_opaque, pState->m_cur_archive_file_ofs, pBuf, len) != len)
				return MZ_FALSE;
			pState->m_cur_archive_file_ofs += len;
			pState->m_comp_size += len;
			return MZ_TRUE;
			#endif
		}

	private:

		struct Deflate
		{
			// Notes:
			//  - Implements the DEFLATE compression algorthim
			//  - Compression format:
			//       https://en.wikipedia.org/wiki/DEFLATE
			//       https://www.w3.org/Graphics/PNG/RFC-1951
			// Algorithm:
			//  The compressor terminates a block when it determines that starting a new block with fresh trees
			//  would be useful, or when the block size fills up the compressor's block buffer.
			//
			//  The compressor uses a chained hash table to find duplicated strings, using a hash function that
			//  operates on 3-byte sequences. At any given point during compression, let XYZ be the next 3 input
			//  bytes to be examined (not necessarily all different, of course). First, the compressor examines
			//  the hash chain for XYZ. If the chain is empty, the compressor simply writes out X as a literal
			//  byte and advances one byte in the input. If the hash chain is not empty, indicating that the
			//  sequence XYZ (or, if we are unlucky, some other 3 bytes with the same hash function value) has
			//  occurred recently, the compressor compares all strings on the XYZ hash chain with the actual
			//  input data sequence starting at the current point, and selects the longest match.
			//
			//  The compressor searches the hash chains starting with the most recent strings, to favor small
			//  distances and thus take advantage of the Huffman encoding. The hash chains are singly linked.
			//  There are no deletions from the hash chains; the algorithm simply discards matches that are too
			//  old. To avoid a worst-case situation, very long hash chains are arbitrarily truncated at a certain
			//  length, determined by a run-time parameter.
			//
			//  To improve overall compression, the compressor optionally defers the selection of matches ("lazy matching"):
			//    after a match of length N has been found, the compressor searches for a longer match starting at
			//    the next input byte. If it finds a longer match, it truncates the previous match to a length of one
			//    (thus producing a single literal byte) and then emits the longer match. Otherwise, it emits the
			//    original match, and, as described above, advances N bytes before continuing.
			//
			//  Run-time parameters also control this "lazy match" procedure. If compression ratio is most important,
			//  the compressor attempts a complete second search regardless of the length of the first match. In the
			//  normal case, if the current match is "long enough", the compressor reduces the search for a longer
			//  match, thus speeding up the process. If speed is most important, the compressor inserts new strings
			//  in the hash table only when no match was found, or when the match is not "too long". This degrades
			//  the compression ratio but saves time since there are both fewer insertions and fewer searches.

			// Flags
			enum class EFlags
			{
				None = 0,

				// Used by Decompress(). If set, the input has a valid zlib header and ends with an
				// Adler32 checksum (i.e. a zlib stream). Otherwise, the input is a raw deflate stream.
				ExpectZlibHeader = 1 << 0,
			};

			// Bit shift register. Could also use uint32_t if on 32-bit
			using bit_buf_t = uint64_t;

			// A decoded zlib header
			struct ZLibHeader
			{
				// The ZLib header is two bytes, interpretted as:
				//  CMF
				//   bits [0..3] = CM - Compression method
				//   bits [4..7] = CINFO - Compression info
				//  FLG
				//   bits [0..4] = FCHECK - Check bits for CMF and FLG
				//   bits [5..5] = FDICT - Preset dictionary
				//   bits [6..7] = FLEVEL - Compression level
				// FCHECK is used to ensure ((CMF<<8)|FLG) is a multiple of 31

				// See: https://tools.ietf.org/html/rfc1950
				uint8_t CMF;
				uint8_t FLG;

				ZLibHeader(uint8_t cmf, uint8_t flg)
					:CMF(cmf)
					,FLG(flg)
				{
					// Header checksum
					auto fcheck = CMF * 256 + FLG;
					if ((fcheck % 31) != 0)
						throw std::runtime_error("ZLIB header invalid. FCHECK failed.");
				}

				// Compression method
				EMethod Method() const
				{
					return EMethod(CMF & 0xF);
				}

				// Deflate compression window size
				uint32_t DeflateWindowSize() const
				{
					if (Method != EMethod::Deflate)
						throw std::runtime_error("ZLIB header LZ77 Window size is only valid when the compression method is DEFLATE");
					
					auto log_sz = ((CMF >> 4) & 0xF);
					if (log_sz > 7)
						throw std::runtime_error("ZLIB header invalid. ZLIB header CINFO field is greater than 7.");

					return 1 << (log_sz + 8);
				}

				// True if a preset dictionary immediately follows the ZLIB header
				bool PresetDictionary() const
				{
					// If set, a DICT dictionary identifier is present immediately after the FLG byte.
					// The dictionary is a sequence of bytes which are initially fed to the compressor
					// without producing any compressed output. DICT is the Adler-32 checksum of this
					// sequence of bytes (see the definition of ADLER32 below). The decompressor can
					// use this identifier to determine which dictionary has been used by the compressor.
					return (FLG & (1 << 5)) != 0;
				}

				// The compression level
				uint32_t CompressionLevel() const
				{
					// 0 = Compressor used fastest algorithm 
					// 1 = Compressor used fast algorithm 
					// 2 = Compressor used default algorithm 
					// 2 = Compressor used maximum/slowest algorithm
					return (FLG >> 6) & 0x3;
				}
			};

			// Huffman table
			struct HuffTable
			{
				enum
				{
					HuffSymbols0 = 288,
					HuffSymbols1 = 32,
					Huffsymbols2 = 19,
					LookupTableBits = 10,
					LookupTableSize = 1 << LookupTableBits,
					LookupTableMask = LookupTableSize - 1,
				};

				uint32_t m_size; // Table size
				int16_t m_look_up[LookupTableSize];
				int16_t m_tree[HuffSymbols0 * 2];
				uint8_t m_code_size[HuffSymbols0];
			};

			bit_buf_t m_bit_buf; // MSB -> LSB shift register
			uint32_t m_num_bits; // The current number of bits in the shift register

			Deflate()
				:m_bit_buf()
				,m_num_bits()
			{}

			// Decompress a stream of bytes from 'src' and write the decompressed stream to 'out'.
			// 'Src' should have uint8_t pointer-like symmantics.
			// 'Out' should have uint8_t pointer-like symmantics and be copyable.
			// 'flush' is called after each decompressed block. Signature: void flush(Out& out)
			template <typename Src, typename Out, typename FlushCB, bool CalcChecksum = true>
			void Decompress(Src src, Out out, FlushCB flush, EFlags const decomp_flags = EFlags::ExpectZlibHeader)
			{
				// Notes:
				//  - Reads beyond the end of 'src' should return 0

				HuffTable tables[2] = {};
				auto out_beg = out;
				m_num_bits = 0;
				m_bit_buf = 0;

				// Parse the ZLIB header
				if (has_flag(decomp_flags, EFlags::ExpectZlibHeader))
				{
					auto cmf = *src++; // Compression method an flags
					auto flg = *src++; // More flags
					auto zhdr = ZLibHeader(cmf, flg);
					if (zhdr.Method() != EMethod::Deflate)
						throw std::runtime_error("ZLIB header indicates a compression method other than 'DEFLATE'. Not supported.");
					if (zhdr.PresetDictionary())
						throw std::runtime_error("ZLIB header contains a preset dictionary. Not supported.");
				}

				// Checksum accumulator
				AlderChecksum<CalcChecksum> adler;

				// A Deflate stream consists of a series of blocks. Each block is preceded by a 3-bit header:
				// First bit: Last-block-in-stream marker:
				//  1: this is the last block in the stream.
				//  0: there are more blocks to process after this one.
				// Second and third bits: Encoding method used for this block type:
				//  00: a stored/raw/literal section, between 0 and 65,535 bytes in length.
				//  01: a static Huffman compressed block, using a pre-agreed Huffman tree.
				//  10: a compressed block complete with the Huffman table supplied.
				//  11: reserved, don't use.
				for (auto hdr = GetBits<uint32_t>(src, 3); (hdr & 1) == 0; hdr = GetBits<uint32_t>(src, 3))
				{
					// Read the block type and prepare the huff tables based on type
					auto type = hdr >> 1;
					switch (type)
					{
					// A stored/raw/literal section, between 0 and 65,535 bytes in length.
					case 0:
						{
							// Skip bits up to the next byte boundary
							void(GetBits<uint32_t>(src, m_num_bits & 7));

							// The length and two's complement of length of uncompressed data follows.
							auto a0 = static_cast<uint16_t>(GetByte(src));
							auto a1 = static_cast<uint16_t>(GetByte(src));
							auto len = a0 | (a1 << 8); 

							auto b0 = static_cast<uint16_t>(GetByte(src));
							auto b1 = static_cast<uint16_t>(GetByte(src));
							auto nlen = b0 | (b1 << 8);

							if (len != ~nlen)
								throw std::runtime_error("DEFLATE uncompressed block has an invalid length");

							// Copy bytes directly to the output stream
							for (; len-- != 0; ++out)
								*out = adler(GetByte(src));

							continue;
						}
					// A static Huffman compressed block, using a pre-agreed Huffman tree.
					case 1:
						{
							tables[0].m_size = HuffTable::HuffSymbols0;
							tables[1].m_size = HuffTable::HuffSymbols1;

							// Initialise the literal code sizes
							int i = 0;
							auto p = &tables[0].m_code_size[0];
							for (; i <= 143; ++i) *p++ = 8;
							for (; i <= 255; ++i) *p++ = 9;
							for (; i <= 279; ++i) *p++ = 7;
							for (; i <= 287; ++i) *p++ = 8;

							// Initialise the distance code sizes
							memset(&tables[1].m_code_size[0], 5, tables[1].m_size);
							break;
						}
					// A compressed block complete with the Huffman table supplied.
					case 2:
						{
							HuffTable dyn_codes;
							tables[0].m_size = GetBits<uint8_t>(src, 5) + 257; // number of literal codes (- 256)
							tables[1].m_size = GetBits<uint8_t>(src, 5) + 1;   // number of distance codes (- 1)
							dyn_codes.m_size = GetBits<uint8_t>(src, 4) + 4;   // number of bit length codes (- 3)

							// Copy the compressed Huffman codes into table[2]
							memset(&dyn_codes.m_code_size[0], 0, sizeof(dyn_codes.m_code_size));
							static uint8_t const s_length_dezigzag[19] = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
							for (int i = 0; i != int(dyn_codes.m_size); ++i) dyn_codes.m_code_size[s_length_dezigzag[i]] = GetBits<uint8_t>(src, 3);
							dyn_codes.m_size = HuffTable::Huffsymbols2;

							// Decompress the Huffman codes
							PopulateHuffmanTree(dyn_codes);
							uint8_t code_sizes[HuffTable::HuffSymbols0 + HuffTable::HuffSymbols1 + 137] = {};
							for (int i = 0, iend = tables[0].m_size + tables[1].m_size; i != iend;)
							{
								auto dist = HuffDecode(src, dyn_codes);
								if (dist < 16)
								{
									code_sizes[i++] = (uint8_t)dist;
									continue;
								}

								//
								if (dist == 16 && i == 0)
								{
									throw std::runtime_error("");
								}

								//
								auto s = GetBits<uint32_t>(src, "\02\03\07"[dist - 16]) + "\03\03\013"[dist - 16];
								memset(code_sizes + i, dist == 16 ? code_sizes[i - 1] : 0, s);

								//
								if ((i += s) > iend)
									throw std::runtime_error("Corrupt Huffman table");
							}

							// Append the dynamic Huffman tables to ends of the static tables
							memcpy(&tables[0].m_code_size[0], &code_sizes[0], tables[0].m_size);
							memcpy(&tables[1].m_code_size[0], &code_sizes[0] + tables[0].m_size, tables[1].m_size);
							break;
						}
					// reserved, don't use.
					case 3:
					default:
						{
							throw std::runtime_error("DEFLATE stream contains an invalid block header");
						}
					}

					// Populate the Huffman tree in each table so that they can be used for decompression
					PopulateHuffmanTree(tables[1]);
					PopulateHuffmanTree(tables[0]);

					// Decompress the block
					for (;;)
					{
						int16_t sym;
						for (;;)
						{
							// Read and decode a symbol from the source stream
							sym = ReadSym(src, tables[0]);
							if (sym & 0x0100) break;
							*out = adler(static_cast<uint8_t>(sym));
							++out;
						}

						// Is this symbol the end-of-block marker?
						sym &= 0x1FF;
						if (sym == 0x0100)
							break;

						static int const s_length_base[31] = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258, 0, 0 };
						static int const s_length_extra[31] = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0, 0, 0 };
						auto count = s_length_base[sym - 257] + GetBits<uint32_t>(src, s_length_extra[sym - 257]);

						// Read the relative offset back to where to read from
						auto ofs = HuffDecode(src, tables[1]);
						static int const s_dist_base[32] = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577, 0, 0 };
						static int const s_dist_extra[32] = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
						auto dist = s_dist_base[ofs] + GetBits<uint32_t>(src, s_dist_extra[ofs]);

						// The number of bytes output so far
						if (dist > out - out_beg)
							throw std::runtime_error("Corrupt zip. Rereference to an earlier byte sequence that is out of range");

						// Repeat an earlier sequence from [existing, existing + count)
						auto existing = out - dist;
						for (; count-- != 0; ++out, ++existing)
							*out = adler(*existing);
					}

					// Signal the end of a block
					flush(out);
				}

				// ZLib streams contain the Alder32 CRC after the data.
				if (has_flag(decomp_flags, EFlags::ExpectZlibHeader))
				{
					// Skip bits up to the next byte boundary
					void(GetBits<uint32_t>(src, m_num_bits & 7));

					// Read the expected Alder32 value
					uint32_t tail_adler32 = 1;
					for (int i = 0; i != 4; ++i)
						tail_adler32 = (tail_adler32 << 8) | GetByte(src);

					// Check the CRC of the output data
					if constexpr (CalcChecksum)
						if (adler.checksum() != tail_adler32)
							throw std::runtime_error("CRC check failure");
				}
			}

		private:

			// Wrapper to help calculate the Adler32 checksum
			template <bool Enabled = true> struct AlderChecksum
			{
				static uint32_t const AdlerMod = 65521;
				uint32_t a, b;

				AlderChecksum()
					:a(1)
					,b(0)
				{}
				uint32_t checksum() const
				{
					return (b << 16) | a;
				}
				uint8_t operator()(uint8_t byte)
				{
					if constexpr (Enabled)
					{
						a = (a + byte) % AdlerMod;
						b = (b + a) % AdlerMod;
					}
					return byte;
				}
			};

			//// Read 4 bytes from 'src'
			//template <typename Src>
			//static uint32_t ReadLE32(Src src)
			//{
			//	if constexpr (std::is_pointer_v<Src> && UnalignedLoadStore && LittleEndian)
			//	{
			//		return *reinterpret_cast<uint32_t const*>(src);
			//	}
			//	else
			//	{
			//		auto b0 = *src++;
			//		auto b1 = *src++;
			//		auto b2 = *src++;
			//		auto b3 = *src++;
			//		return
			//			(static_cast<uint32_t>(b0) <<  0U) |
			//			(static_cast<uint32_t>(b1) <<  8U) |
			//			(static_cast<uint32_t>(b2) << 16U) |
			//			(static_cast<uint32_t>(b3) << 24U);
			//	}
			//}

			//// Read 2 bytes from 'src'
			//template <typename Src>
			//static uint16_t ReadLE16(Src src)
			//{
			//	if constexpr (std::is_pointer_v<Src> && UnalignedLoadStore && LittleEndian)
			//	{
			//		return *reinterpret_cast<uint16_t const*>(src);
			//	}
			//	else
			//	{
			//		auto b0 = *src++;
			//		auto b1 = *src++;
			//		return
			//			(static_cast<uint16_t>(b0) <<  0U) |
			//			(static_cast<uint16_t>(b1) <<  8U);
			//	}
			//}

			// Return 'value' with 'length' bits reversed
			uint32_t ReverseBits(uint32_t value, uint32_t length)
			{
				uint32_t reversed = 0;
				for (; length-- != 0;)
				{
					reversed = (reversed << 1) | (value & 1);
					value >>= 1;
				}
				return reversed;
			}

			// Read one byte from 'src'
			template <typename Src>
			uint8_t GetByte(Src& src)
			{
				if (m_num_bits == 0)
					return *src++;

				if (m_num_bits < 8)
				{
					// Append bits on the left
					m_bit_buf |= static_cast<bit_buf_t>(*src++) << m_num_bits;
					m_num_bits += 8;
				}
				auto b = static_cast<uint8_t>(m_bit_buf & 0xFF);
				m_bit_buf >>= 8;
				m_num_bits -= 8;
				return b;
			}

			// Read 'n' bits from the source stream into 'm_bit_buf'
			template <typename TInt, typename Src>
			TInt GetBits(Src& src, uint32_t n)
			{
				assert(n <= sizeof(TInt) * 8);
				for (; m_num_bits < n;)
				{
					// Append bits on the left
					m_bit_buf |= static_cast<bit_buf_t>(*src++) << m_num_bits;
					m_num_bits += 8;
				}

				// Read and pop the lower 'n' bits
				auto b = static_cast<TInt>(m_bit_buf & ((1 << n) - 1));
				m_bit_buf >>= n;
				m_num_bits -= n;
				return b;
			}

			// Interpret the next bits as a symbol and pop the bits
			template <typename Src>
			int16_t ReadSym(Src& src, HuffTable const& table)
			{
				// Ensure 'm_bit_buf' contains at least 15 bits
				if (m_num_bits < 8)
				{
					m_bit_buf |= static_cast<bit_buf_t>(*src++) << m_num_bits;
					m_num_bits += 8;
				}
				if (m_num_bits < 16)
				{
					m_bit_buf |= static_cast<bit_buf_t>(*src++) << m_num_bits;
					m_num_bits += 8;
				}

				// Read the symbol
				auto sym = table.m_look_up[m_bit_buf & HuffTable::LookupTableMask];

				int code_len;
				if (sym >= 0)
				{
					code_len = sym >> 9;
				}
				else
				{
					code_len = HuffTable::LookupTableBits;
					for (; sym < 0;)
					{
						sym = table.m_tree[~sym + ((m_bit_buf >> code_len++) & 1)];
					}
				}
				m_bit_buf >>= code_len;
				m_num_bits -= code_len;
				return sym;
			}

			// Decodes and returns the next Huffman coded symbol.
			template <typename Src>
			int HuffDecode(Src& src, HuffTable const& table)
			{
				// Notes:
				//  - This function reads 2 bytes from 'src'.
				// It's more complex than you would initially expect because the zlib API expects the decompressor to never read
				// beyond the final byte of the deflate stream. (In other words, when this macro wants to read another byte from the input, it REALLY needs another byte in order to fully
				// decode the next Huffman code.) Handling this properly is particularly important on raw deflate (non-zlib) streams, which aren't followed by a byte aligned adler-32.
				// The slow path is only executed at the very end of the input buffer.
				if (m_num_bits < 15)
				{
					m_bit_buf |= static_cast<bit_buf_t>(*src++) << (m_num_bits + 0);
					m_bit_buf |= static_cast<bit_buf_t>(*src++) << (m_num_bits + 8);
					m_num_bits += 16;
				}

				// Read the Huff symbol
				int code_len = 0;
				int sym = table.m_look_up[m_bit_buf & HuffTable::LookupTableMask];
				if (sym >= 0)
				{
					code_len = sym >> 9;
					sym &= 511;
				}
				else
				{
					code_len = HuffTable::LookupTableBits;
					for (;sym < 0;)
					{
						sym = table.m_tree[~sym + ((m_bit_buf >> code_len++) & 1)]; 
					}
				}

				m_bit_buf >>= code_len;
				m_num_bits -= code_len;
				return sym;
			}

			// Populate the tree and lookup tables in 'table'
			void PopulateHuffmanTree(HuffTable& table)
			{
				// Reset the tree and lookup arrays
				memset(&table.m_look_up[0], 0, sizeof(table.m_look_up));
				memset(&table.m_tree[0], 0, sizeof(table.m_tree));

				// Find the counts of each code size
				uint32_t total_syms[16] = {};
				for (int i = 0; i != int(table.m_size); ++i)
					total_syms[table.m_code_size[i]]++;

				// Fill the 'next_code' buffer
				uint32_t next_code[17] = { 0, 0 }, total = 0, used_syms = 0;
				for (int i = 2; i != 17; ++i)
				{
					total = (total + total_syms[i - 1]) << 1;
					used_syms += total_syms[i - 1];
					next_code[i] = total;
				}
				if (total != 65536 && used_syms > 1)
					throw std::runtime_error(""); // ??? If this is a partial block (i.e. not 65536 in size) then only one symbol should be used? a literal?

				// Generate the lookup table
				int16_t tree_cur, tree_next = -1;
				for (int sym_index = 0; sym_index != int(table.m_size); ++sym_index)
				{
					// Get the length of the code
					auto code_size = table.m_code_size[sym_index];
					if (code_size == 0)
						continue;

					// Get the code (bit reversed)
					auto rev_code = ReverseBits(next_code[code_size]++, code_size);

					// 
					if (code_size <= HuffTable::LookupTableBits)
					{
						auto k = static_cast<int16_t>((code_size << 9) | sym_index);
						while (rev_code < HuffTable::LookupTableSize)
						{
							table.m_look_up[rev_code] = k;
							rev_code += (1 << code_size);
						}
						continue;
					}

					// 
					tree_cur = table.m_look_up[rev_code & HuffTable::LookupTableMask];
					if (tree_cur == 0)
					{
						// Save the index to the next sub-tree
						table.m_look_up[rev_code & HuffTable::LookupTableMask] = tree_next;
						tree_cur = tree_next;
						tree_next -= 2;
					}

					//
					rev_code >>= HuffTable::LookupTableBits - 1;
					for (int i = code_size; i > HuffTable::LookupTableBits + 1; --i)
					{
						rev_code >>= 1;
						tree_cur -= rev_code & 1;

						if (!table.m_tree[~tree_cur])
						{
							table.m_tree[~tree_cur] = tree_next;
							tree_cur = tree_next;
							tree_next -= 2;
						}
						else
						{
							tree_cur = table.m_tree[~tree_cur];
						}
					}

					tree_cur -= (rev_code >>= 1) & 1;
					table.m_tree[~tree_cur] = (int16_t)sym_index;
				}
			}
		};

		#if 0 // Finding compiler bug
		// Change the archive to the given mode
		void ChangeMode(EMode mode)
		{
			if (m_zip_mode == mode)
				return;

			// Switching from reading to writing
			if (m_zip_mode == EMode::Reading && mode == EMode::Writing)
			{
				if (!m_imem.empty())
				{
					// Leave the central directory data intact.

					// If the input stream is a view of memory other than 'm_omem' then copy the data to 'm_omem'.
					if (m_omem.empty() || m_imem.data() != m_omem.data())
						m_omem.assign(begin(m_imem), end(m_imem));

					// Close the input stream
					m_imem = iarray_t();
					m_write = InMemoryWriteFunc;
					m_read = nullptr;
				}
				else
				{
					m_ifile.close();
					m_ofile = std::ofstream(m_filepath, std::ios::binary);
					m_write = FileWriteFunc;
					m_read = nullptr;
				}
				m_zip_mode = EMode::Writing;
				break;

			}

			// Switching from writing to reading
			if (m_zip_mode == EMode::Writing && mode == EMode::Reading)
			{
			}



			// Finalise and close
			m_central_dir.clear();
			m_cdir_index.clear();
			m_central_dir_lookup.clear();

			// Close input stream
			m_ifile.close();
			m_imem = iarray_t();
			m_write = nullptr;
			m_read = nullptr;





			// Clean up from the old mode
			switch (m_zip_mode)
			{
			default:
				{
					throw std::runtime_error("Unknown zip archive mode");
				}
			case EMode::Invalid:
				{
					break;
				}
			case EMode::Reading:
				{
					break;
				}
			case EMode::Writing:
				{
					// Finalizes the archive by writing the central directory records followed by the end of central directory record.

					// No zip64 support
					if (m_total_entries > 0xFFFF || m_archive_size + m_central_dir.size() + sizeof(ECDH) > 0xFFFFFFFF)
						throw std::runtime_error("Zip too large. Zip64 is not supported");

					int64_t central_dir_ofs = 0;
					size_t central_dir_size = 0;
					if (m_total_entries != 0)
					{
						// Write central directory
						central_dir_ofs = m_archive_size;
						central_dir_size = m_central_dir.size();
						m_write(*this, central_dir_ofs, m_central_dir.data(), central_dir_size);
						m_archive_size += central_dir_size;
					}

					// Write end of central directory record
					std::array<uint8_t, sizeof(ECDH)> hdr = {};
					WriteLE32(&hdr[0] + ECDH::SignatureOffset, ECDH::Signature);
					WriteLE16(&hdr[0] + ECDH::CDirNumEntriesOnDiskOffset, m_total_entries);
					WriteLE16(&hdr[0] + ECDH::CDirTotalEntriesOffset, m_total_entries);
					WriteLE32(&hdr[0] + ECDH::CDirSize, central_dir_size);
					WriteLE32(&hdr[0] + ECDH::CDirOffset, central_dir_ofs);
					m_write(*this, m_archive_size, hdr.data(), hdr.size());

					// Release buffers
					m_central_dir.clear();
					m_cdir_index.clear();
					m_central_dir_lookup.clear();
					m_archive_size = 0;

					// Close output stream
					m_ofile.close();
					m_omem.clear();
					m_read = nullptr;
					m_write = nullptr;
					break;
				}
			}

			// Activate the new mode
			switch (mode)
			{
			default:
				{
					throw std::runtime_error("Unknown zip archive mode");
				}
			case EMode::Invalid:
				{
					// If not switching to the reading state, reset the buffers
					break;
				}
			case EMode::Reading:
				{
					// Switch to reading from the in-memory zip or file
					if (!m_omem.empty())
					{
						m_imem = iarray_t(m_omem.data(), m_omem.size());
						m_read = InMemoryReadFunc;
						m_write = nullptr;
					}
					else
					{
						m_ofile.close();
						m_read = FileReadFunc;
						m_write = nullptr;
					}
					m_zip_mode = EMode::Reading;
					ReadCentralDirectory();
					break;
				}
			case EMode::Writing:
				{
					if (!m_imem.empty())
					{
						m_omem.assign(begin(m_imem), end(m_imem));
						m_imem = iarray_t();
						m_write = InMemoryWriteFunc;
						m_read = nullptr;
					}
					else
					{
						m_ifile.close();
						m_ofile = std::ofstream(m_filepath, std::ios::binary);
						m_write = FileWriteFunc;
						m_read = nullptr;
					}
					m_zip_mode = EMode::Writing;
					break;
				}
			}

			m_zip_mode = mode;
		}
		#endif

	private: // ------------------- Low-level Decompression (completely independent from all compression API's)

		#if 0 // yes to be refactored...
		// tinfl_decompress_mem_to_callback() decompresses a block in memory to an internal 32KB buffer, and a user
		// provided callback function will be called to flush the buffer. Returns 1 on success or 0 on failure.
		int tinfl_decompress_mem_to_callback(const void* pIn_buf, size_t* pIn_buf_size, tinfl_put_buf_func_ptr pPut_buf_func, void* pPut_buf_user, int flags)
		{
			int result = 0;
			Deflate decomp;
			uint8_t* pDict = (uint8_t*)mz_malloc(LZDictionarySize);
			size_t in_buf_ofs = 0;
			size_t dict_ofs = 0;
			if (!pDict)
				return TINFL_STATUS_FAILED;

			decomp.m_state = 0;
			for(;;)
			{
				size_t in_buf_size = *pIn_buf_size - in_buf_ofs;
				size_t dst_buf_size = LZDictionarySize - dict_ofs;
				tinfl_status status = Decompress(&decomp,
					(const uint8_t*)pIn_buf + in_buf_ofs, &in_buf_size,
					pDict, pDict + dict_ofs, &dst_buf_size,
					(flags & ~(TINFL_FLAG_HAS_MORE_INPUT | TINFL_FLAG_USING_NON_WRAPPING_OUTPUT_BUF)));
				
				in_buf_ofs += in_buf_size;
				if ((dst_buf_size) && (!(*pPut_buf_func)(pDict + dict_ofs, (int)dst_buf_size, pPut_buf_user)))
					break;

				if (status != TINFL_STATUS_HAS_MORE_OUTPUT)
				{
					result = (status == TINFL_STATUS_DONE);
					break;
				}

				dict_ofs = (dict_ofs + dst_buf_size) & (LZDictionarySize - 1);
			}

			mz_free(pDict);
			*pIn_buf_size = in_buf_ofs;
			return result;
		}

	private:

		struct tdefl_compressor
		{
			enum
			{
				TDEFL_MAX_HUFF_TABLES = 3,
				TDEFL_MAX_HUFF_SYMBOLS_0 = 288,
				TDEFL_MAX_HUFF_SYMBOLS_1 = 32,
				TDEFL_MAX_HUFF_SYMBOLS_2 = 19,
				TDEFL_LZ_DICT_SIZE = 32768,
				TDEFL_LZ_DICT_SIZE_MASK = TDEFL_LZ_DICT_SIZE - 1,
				TDEFL_MIN_MATCH_LEN = 3,
				TDEFL_MAX_MATCH_LEN = 258,
				TDEFL_MAX_SUPPORTED_HUFF_CODESIZE = 32,
			};
			enum
			{
				// TDEFL_OUT_BUF_SIZE MUST be large enough to hold a single entire compressed output block (using static/fixed Huffman codes).
				TDEFL_LZ_CODE_BUF_SIZE = 64 * 1024,
				TDEFL_OUT_BUF_SIZE = (TDEFL_LZ_CODE_BUF_SIZE * 13) / 10,
				TDEFL_MAX_HUFF_SYMBOLS = 288,
				TDEFL_LZ_HASH_BITS = 15,
				TDEFL_LEVEL1_HASH_SIZE_MASK = 4095,
				TDEFL_LZ_HASH_SHIFT = (TDEFL_LZ_HASH_BITS + 2) / 3,
				TDEFL_LZ_HASH_SIZE = 1 << TDEFL_LZ_HASH_BITS,

				// Alternative for low memory environments:
				// TDEFL_LZ_CODE_BUF_SIZE = 24 * 1024, 
				// TDEFL_OUT_BUF_SIZE = (TDEFL_LZ_CODE_BUF_SIZE * 13) / 10, 
				// TDEFL_MAX_HUFF_SYMBOLS = 288, 
				// TDEFL_LZ_HASH_BITS = 12, 
				// TDEFL_LEVEL1_HASH_SIZE_MASK = 4095, 
				// TDEFL_LZ_HASH_SHIFT = (TDEFL_LZ_HASH_BITS + 2) / 3, 
				// TDEFL_LZ_HASH_SIZE = 1 << TDEFL_LZ_HASH_BITS,
			};

			// Must map to MZ_NO_FLUSH, MZ_SYNC_FLUSH, etc. enums
			enum class tdefl_flush
			{
				NO_FLUSH = 0,
				SYNC_FLUSH = 2,
				FULL_FLUSH = 3,
				FINISH = 4
			};
			enum tdefl_status
			{
				TDEFL_STATUS_BAD_PARAM = -2,
				TDEFL_STATUS_PUT_BUF_FAILED = -1,
				TDEFL_STATUS_OKAY = 0,
				TDEFL_STATUS_DONE = 1,
			};

			// tdefl's compression state structure.
			uint8_t m_dict[TDEFL_LZ_DICT_SIZE + TDEFL_MAX_MATCH_LEN - 1];
			uint16_t m_huff_count[TDEFL_MAX_HUFF_TABLES][TDEFL_MAX_HUFF_SYMBOLS];
			uint16_t m_huff_codes[TDEFL_MAX_HUFF_TABLES][TDEFL_MAX_HUFF_SYMBOLS];
			uint8_t m_huff_code_sizes[TDEFL_MAX_HUFF_TABLES][TDEFL_MAX_HUFF_SYMBOLS];
			uint8_t m_lz_code_buf[TDEFL_LZ_CODE_BUF_SIZE];
			uint16_t m_next[TDEFL_LZ_DICT_SIZE];
			uint16_t m_hash[TDEFL_LZ_HASH_SIZE];
			uint8_t m_output_buf[TDEFL_OUT_BUF_SIZE];
			tdefl_put_buf_func_ptr m_pPut_buf_func;
			void *m_pPut_buf_user;
			ECompressionFlags m_flags;
			mz_uint m_max_probes[2];
			bool m_greedy_parsing;
			mz_uint m_adler32;
			mz_uint m_lookahead_pos;
			mz_uint m_lookahead_size;
			mz_uint m_dict_size;
			uint8_t* m_pLZ_code_buf;
			uint8_t* m_pLZ_flags;
			uint8_t* m_pOutput_buf;
			uint8_t* m_pOutput_buf_end;
			mz_uint m_num_flags_left;
			mz_uint m_total_lz_bytes;
			mz_uint m_lz_code_buf_dict_pos;
			mz_uint m_bits_in;
			mz_uint m_bit_buffer;
			mz_uint m_saved_match_dist;
			mz_uint m_saved_match_len;
			mz_uint m_saved_lit;
			mz_uint m_output_flush_ofs;
			mz_uint m_output_flush_remaining;
			mz_uint m_finished;
			mz_uint m_block_index;
			mz_uint m_wants_to_finish;
			tdefl_status m_prev_return_status;
			void const* m_pIn_buf;
			void* m_pOut_buf;
			size_t* m_pIn_buf_size;
			size_t* m_pOut_buf_size;
			tdefl_flush m_flush;
			uint8_t const* m_pSrc;
			size_t m_src_buf_left;
			size_t m_out_buf_ofs;

			tdefl_compressor(tdefl_put_buf_func_ptr pPut_buf_func, void* pPut_buf_user, ECompressionFlags flags)
				:m_dict()
				,m_huff_count()
				,m_huff_codes()
				,m_huff_code_sizes()
				,m_lz_code_buf()
				,m_next()
				,m_hash()
				,m_output_buf()
				,m_pPut_buf_func(pPut_buf_func)
				,m_pPut_buf_user(pPut_buf_user)
				,m_flags(flags)
				,m_max_probes()
				,m_greedy_parsing((flags & ECompressionFlags::GreedyParsing) != 0)
				,m_adler32(1)
				,m_lookahead_pos(0)
				,m_lookahead_size(0)
				,m_dict_size(0)
				,m_pLZ_code_buf(&m_lz_code_buf[1])
				,m_pLZ_flags(&m_lz_code_buf[0])
				,m_pOutput_buf(&m_output_buf[0])
				,m_pOutput_buf_end(&m_output_buf[0])
				,m_num_flags_left(8)
				,m_total_lz_bytes(0)
				,m_lz_code_buf_dict_pos(0)
				,m_bits_in(0)
				,m_bit_buffer(0)
				,m_saved_match_dist(0)
				,m_saved_match_len(0)
				,m_saved_lit(0)
				,m_output_flush_ofs(0)
				,m_output_flush_remaining(0)
				,m_finished(0)
				,m_block_index(0)
				,m_wants_to_finish(0)
				,m_prev_return_status(TDEFL_STATUS_OKAY)
				,m_pIn_buf(nullptr)
				,m_pOut_buf(nullptr)
				,m_pIn_buf_size(nullptr)
				,m_pOut_buf_size(nullptr)
				,m_flush(TDEFL_NO_FLUSH)
				,m_pSrc(nullptr)
				,m_src_buf_left(0)
				,m_out_buf_ofs(0)
			{
				if (m_pPut_buf_func == nullptr)
					throw std::runtime_error("The writer callback function must be provided");
				
				// Initializes the compressor.
				// pBut_buf_func: If nullptr, output data will be supplied to the specified callback.
				// In this case, the user should call the tdefl_compress_buffer() API for compression.
				// If pBut_buf_func is nullptr the user should always call the tdefl_compress() API.
				// flags: See the above enums (TDEFL_HUFFMAN_ONLY, TDEFL_WRITE_ZLIB_HEADER, etc.)
				m_max_probes[0] = 1 + ((flags & TDEFL_MAX_PROBES_MASK) + 2) / 3;
				m_max_probes[1] = 1 + (((flags & TDEFL_MAX_PROBES_MASK) >> 2) + 2) / 3;

				//if ((flags & ECompressionFlags::NonDeterministicParsing) == 0)
				//	memset(m_hash); // wtf
			}

			// Compress data in 'buf'
			tdefl_status tdefl_compress_buffer(void const* buf, size_t buf_size, tdefl_flush flush)
			{
				return tdefl_compress(buf, &buf_size, nullptr, nullptr, flush);
			}

		private:

			// Compresses a block of data, consuming as much of the specified input buffer as possible, and writing as much compressed data to the specified output buffer as possible.
			tdefl_status tdefl_compress(void const* buf, size_t* pIn_buf_size, void* pOut_buf, size_t* pOut_buf_size, tdefl_flush flush)
			{
				m_pIn_buf = buf;
				m_pIn_buf_size = buf_size;
				m_pOut_buf = pOut_buf;
				m_pOut_buf_size = pOut_buf_size;
				m_pSrc = (const uint8_t*)(buf);
				m_src_buf_left = buf_size ? *buf_size : 0;
				m_out_buf_ofs = 0;
				m_flush = flush;

				if (((m_pPut_buf_func != nullptr) == ((pOut_buf != nullptr) || (pOut_buf_size != nullptr))) || (m_prev_return_status != TDEFL_STATUS_OKAY) ||
					(m_wants_to_finish && (flush != TDEFL_FINISH)) || (buf_size && *buf_size && !buf) || (pOut_buf_size && *pOut_buf_size && !pOut_buf))
				{
					if (buf_size)* buf_size = 0;
					if (pOut_buf_size)* pOut_buf_size = 0;
					return (m_prev_return_status = TDEFL_STATUS_BAD_PARAM);
				}
				m_wants_to_finish |= (flush == TDEFL_FINISH);

				if ((m_output_flush_remaining) || (m_finished))
					return (m_prev_return_status = tdefl_flush_output_buffer(d));

				if constexpr (UnalignedLoadStore && LittleEndian)
				{
					if (((m_flags & TDEFL_MAX_PROBES_MASK) == 1) &&
						((m_flags & TDEFL_GREEDY_PARSING_FLAG) != 0) &&
						((m_flags & (TDEFL_FILTER_MATCHES | TDEFL_FORCE_ALL_RAW_BLOCKS | TDEFL_RLE_MATCHES)) == 0))
					{
						if (!tdefl_compress_fast(d))
							return m_prev_return_status;
					}
				}
				else
				{
					if (!tdefl_compress_normal(d))
						return m_prev_return_status;
				}

				if ((m_flags & (TDEFL_WRITE_ZLIB_HEADER | TDEFL_COMPUTE_ADLER32)) && (buf))
					m_adler32 = (uint32_t)Adler32(m_adler32, (const uint8_t*)buf, m_pSrc - (const uint8_t*)buf);

				if ((flush) && (!m_lookahead_size) && (!m_src_buf_left) && (!m_output_flush_remaining))
				{
					if (tdefl_flush_block(d, flush) < 0)
						return m_prev_return_status;
					m_finished = (flush == TDEFL_FINISH);
					if (flush == TDEFL_FULL_FLUSH) { memset(m_hash); memset(m_next); m_dict_size = 0; }
				}

				return (m_prev_return_status = tdefl_flush_output_buffer(d));
			}

		};
		struct mz_zip_writer_add_state
		{
			uint64_t m_cur_archive_file_ofs;
			uint64_t m_comp_size;

			mz_zip_writer_add_state(uint64_t cur_archive_file_ofs, uint64_t comp_size)
				:m_cur_archive_file_ofs(cur_archive_file_ofs)
				,m_comp_size(comp_size)
			{}
		};


		// Inits a ZIP archive writer.


		mz_bool mz_zip_writer_init_file(mz_zip_archive* pZip, const char* pFilename, uint64_t size_to_reserve_at_beginning)
		{
			mz_file_t* pFile;
			pZip->m_pWrite = FileWriteFunc;
			pZip->m_pIO_opaque = pZip;
			if (!mz_zip_writer_init(pZip, size_to_reserve_at_beginning))
				return MZ_FALSE;

			if (nullptr == (pFile = mz_fopen(pFilename, "wb")))
			{
				mz_zip_writer_end(pZip);
				return MZ_FALSE;
			}

			pZip->m_pState->m_pFile = pFile;
			if (size_to_reserve_at_beginning)
			{
				char buf[4096] = {};
				uint64_t cur_ofs = 0;
				do
				{
					size_t n = (size_t)std::min(sizeof(buf), size_to_reserve_at_beginning);
					if (pZip->m_pWrite(pZip->m_pIO_opaque, cur_ofs, buf, n) != n)
					{
						mz_zip_writer_end(pZip);
						return MZ_FALSE;
					}
					cur_ofs += n; size_to_reserve_at_beginning -= n;
				}
				while (size_to_reserve_at_beginning);
			}
			return MZ_TRUE;
		}

		// Converts a ZIP archive reader object into a writer object, to allow efficient in-place file appends to occur on an existing archive.
		// For archives opened using mz_zip_reader_init_file, pFilename must be the archive's filename so it can be reopened for writing. If the file can't be reopened, mz_zip_reader_end() will be called.
		// For archives opened using mz_zip_reader_init_mem, the memory block must be growable using the realloc callback (which defaults to realloc unless you've overridden it).
		// Finally, for archives opened using mz_zip_reader_init, the mz_zip_archive's user provided m_pWrite function cannot be nullptr.
		// Note: In-place archive modification is not recommended unless you know what you're doing, because if execution stops or something goes wrong before
		// the archive is finalized the file's central directory will be hosed.
		mz_bool mz_zip_writer_init_from_reader(mz_zip_archive* pZip, const char* pFilename)
		{
			if ((!pZip) || (!pZip->m_pState) || (pZip->m_zip_mode != MZ_ZIP_MODE_READING))
				return MZ_FALSE;

			// No sense in trying to write to an archive that's already at the support max size
			if ((pZip->m_total_entries == 0xFFFF) || ((pZip->m_archive_size + sizeof(CDH) + sizeof(LDH)) > 0xFFFFFFFF))
				return MZ_FALSE;

			auto pState = pZip->m_pState;
			if (pState->m_pFile)
			{
				// Archive is being read from stdio - try to reopen as writable.
				if (pZip->m_pIO_opaque != pZip)
					return MZ_FALSE;
				if (!pFilename)
					return MZ_FALSE;

				pZip->m_pWrite = FileWriteFunc;
				pState->m_pFile = mz_freopen(pFilename, "r+b", pState->m_pFile);
				if (pState->m_pFile == nullptr)
				{
					// The mz_zip_archive is now in a bogus state because pState->m_pFile is nullptr, so just close it.
					mz_zip_reader_end(pZip);
					return MZ_FALSE;
				}
			}
			else if (pState->m_pMem)
			{
				// Archive lives in a memory block. Assume it's from the heap that we can resize using the realloc callback.
				if (pZip->m_pIO_opaque != pZip)
					return MZ_FALSE;
				pState->m_mem_capacity = pState->m_mem_size;
				pZip->m_pWrite = InMemoryWriteFunc;
			}
			// Archive is being read via a user provided read function - make sure the user has specified a write function too.
			else if (!pZip->m_pWrite)
			{
				return MZ_FALSE;
			}

			// Start writing new files at the archive's current central directory location.
			pZip->m_archive_size = pZip->m_central_directory_ofs;
			pZip->m_zip_mode = MZ_ZIP_MODE_WRITING;
			pZip->m_central_directory_ofs = 0;
			return MZ_TRUE;
		}

		// --------------------------- ZIP archive reading

		mz_bool mz_zip_reader_is_file_encrypted(mz_zip_archive* pZip, mz_uint file_index)
		{
			mz_uint m_bit_flag;
			const uint8_t* p = ItemStat(pZip, file_index);
			if (!p)
				return MZ_FALSE;
			m_bit_flag = ReadLE16(p + CDH::BitFlagsOffset);
			return (m_bit_flag & 1);
		}


		// Extracts a archive file to a memory buffer using no memory allocation.
		mz_bool mz_zip_reader_extract_to_mem_no_alloc(mz_zip_archive* pZip, mz_uint file_index, void* pBuf, size_t buf_size, mz_uint flags, void* pUser_read_buf, size_t user_read_buf_size)
		{
			int status = TINFL_STATUS_DONE;
			uint64_t needed_size, cur_file_ofs, comp_remaining, out_buf_ofs = 0, read_buf_size, read_buf_ofs = 0, read_buf_avail;
			ZipItemStat stat;
			void* pRead_buf;
			uint32_t local_header_u32[(sizeof(LDH) + sizeof(uint32_t) - 1) / sizeof(uint32_t)]; uint8_t * pLocal_header = (uint8_t*)local_header_u32;
			Deflate aglo;

			if ((buf_size) && (!pBuf))
				return MZ_FALSE;

			if (!ItemStat(pZip, file_index, &stat))
				return MZ_FALSE;

			// Empty file, or a directory (but not always a directory - I've seen odd zips with directories that have compressed data which inflates to 0 bytes)
			if (!stat.m_comp_size)
				return MZ_TRUE;

			// Entry is a subdirectory (I've seen old zips with dir entries which have compressed deflate data which inflates to 0 bytes, but these entries claim to uncompress to 512 bytes in the headers).
			// I'm torn how to handle this case - should it fail instead?
			if (ItemIsDirectory(pZip, file_index))
				return MZ_TRUE;

			// Encryption and patch files are not supported.
			if (stat.m_bit_flag & (1 | 32))
				return MZ_FALSE;

			// This function only supports stored and deflate.
			if ((!(flags & MZ_ZIP_FLAG_COMPRESSED_DATA)) && (stat.m_method != 0) && (stat.m_method != MZ_DEFLATED))
				return MZ_FALSE;

			// Ensure supplied output buffer is large enough.
			needed_size = (flags & MZ_ZIP_FLAG_COMPRESSED_DATA) ? stat.m_comp_size : stat.m_uncomp_size;
			if (buf_size < needed_size)
				return MZ_FALSE;

			// Read and parse the local directory entry.
			cur_file_ofs = stat.m_local_header_ofs;
			if (pZip->m_pRead(pZip->m_pIO_opaque, cur_file_ofs, pLocal_header, sizeof(LDH)) != sizeof(LDH))
				return MZ_FALSE;
			if (ReadLE32(pLocal_header) != LocalDirHeaderSignature)
				return MZ_FALSE;

			cur_file_ofs += sizeof(LDH) + ReadLE16(pLocal_header + LDH::FilenameLengthOffset) + ReadLE16(pLocal_header + LDH::ExtraLengthOffset);
			if ((cur_file_ofs + stat.m_comp_size) > pZip->m_archive_size)
				return MZ_FALSE;

			if ((flags & MZ_ZIP_FLAG_COMPRESSED_DATA) || (!stat.m_method))
			{
				// The file is stored or the caller has requested the compressed data.
				if (pZip->m_pRead(pZip->m_pIO_opaque, cur_file_ofs, pBuf, (size_t)needed_size) != needed_size)
					return MZ_FALSE;
				return ((flags & MZ_ZIP_FLAG_COMPRESSED_DATA) != 0) || (mz_crc32(MZ_CRC32_INIT, (const uint8_t*)pBuf, (size_t)stat.m_uncomp_size) == stat.m_crc32);
			}

			// Decompress the file either directly from memory or from a file input buffer.
			aglo.m_state = 0;

			if (pZip->m_pState->m_pMem)
			{
				// Read directly from the archive in memory.
				pRead_buf = (uint8_t*)pZip->m_pState->m_pMem + cur_file_ofs;
				read_buf_size = read_buf_avail = stat.m_comp_size;
				comp_remaining = 0;
			}
			else if (pUser_read_buf)
			{
				// Use a user provided read buffer.
				if (!user_read_buf_size)
					return MZ_FALSE;
				pRead_buf = (uint8_t*)pUser_read_buf;
				read_buf_size = user_read_buf_size;
				read_buf_avail = 0;
				comp_remaining = stat.m_comp_size;
			}
			else
			{
				// Temporarily allocate a read buffer.
				read_buf_size = std::min<uint64_t>(stat.m_comp_size, MaxIOBufferSize);
				if (sizeof(size_t) == sizeof(uint32_t) && read_buf_size > 0x7FFFFFFF)
					return MZ_FALSE;
				if (nullptr == (pRead_buf = pZip->m_pAlloc(pZip->m_pAlloc_opaque, 1, (size_t)read_buf_size)))
					return MZ_FALSE;
				read_buf_avail = 0;
				comp_remaining = stat.m_comp_size;
			}

			do
			{
				size_t in_buf_size, out_buf_size = (size_t)(stat.m_uncomp_size - out_buf_ofs);
				if ((!read_buf_avail) && (!pZip->m_pState->m_pMem))
				{
					read_buf_avail = std::min(read_buf_size, comp_remaining);
					if (pZip->m_pRead(pZip->m_pIO_opaque, cur_file_ofs, pRead_buf, (size_t)read_buf_avail) != read_buf_avail)
					{
						status = TINFL_STATUS_FAILED;
						break;
					}
					cur_file_ofs += read_buf_avail;
					comp_remaining -= read_buf_avail;
					read_buf_ofs = 0;
				}
				in_buf_size = (size_t)read_buf_avail;
				status = aglo.Decompress(&inflator, (uint8_t*)pRead_buf + read_buf_ofs, &in_buf_size, (uint8_t*)pBuf, (uint8_t*)pBuf + out_buf_ofs, &out_buf_size, TINFL_FLAG_USING_NON_WRAPPING_OUTPUT_BUF | (comp_remaining ? TINFL_FLAG_HAS_MORE_INPUT : 0));
				read_buf_avail -= in_buf_size;
				read_buf_ofs += in_buf_size;
				out_buf_ofs += out_buf_size;
			}
			while (status == TINFL_STATUS_NEEDS_MORE_INPUT);

			if (status == TINFL_STATUS_DONE)
			{
				// Make sure the entire file was decompressed, and check its CRC.
				if ((out_buf_ofs != stat.m_uncomp_size) || (mz_crc32(MZ_CRC32_INIT, (const uint8_t*)pBuf, (size_t)stat.m_uncomp_size) != stat.m_crc32))
					status = TINFL_STATUS_FAILED;
			}

			if ((!pZip->m_pState->m_pMem) && (!pUser_read_buf))
				pZip->m_pFree(pZip->m_pAlloc_opaque, pRead_buf);

			return status == TINFL_STATUS_DONE;
		}
		mz_bool mz_zip_reader_extract_file_to_mem_no_alloc(mz_zip_archive* pZip, const char* pFilename, void* pBuf, size_t buf_size, mz_uint flags, void* pUser_read_buf, size_t user_read_buf_size)
		{
			auto file_index = IndexOf(pZip, pFilename, nullptr, flags);
			if (file_index < 0)
				return MZ_FALSE;
			
			return mz_zip_reader_extract_to_mem_no_alloc(pZip, file_index, pBuf, buf_size, flags, pUser_read_buf, user_read_buf_size);
		}

		// Extracts a archive file to a memory buffer.
		mz_bool mz_zip_reader_extract_to_mem(mz_zip_archive* pZip, mz_uint file_index, void* pBuf, size_t buf_size, mz_uint flags)
		{
			return mz_zip_reader_extract_to_mem_no_alloc(pZip, file_index, pBuf, buf_size, flags, nullptr, 0);
		}
		mz_bool mz_zip_reader_extract_file_to_mem(mz_zip_archive* pZip, const char* pFilename, void* pBuf, size_t buf_size, mz_uint flags)
		{
			return mz_zip_reader_extract_file_to_mem_no_alloc(pZip, pFilename, pBuf, buf_size, flags, nullptr, 0);
		}

		// Extracts a archive file to a dynamically allocated heap buffer.
		void* mz_zip_reader_extract_to_heap(mz_zip_archive* pZip, mz_uint file_index, size_t* pSize, mz_uint flags)
		{
			if (pSize)
				*pSize = 0;
			
			auto p = ItemStat(pZip, file_index);
			if (!p)
				return nullptr;

			uint64_t comp_size = ReadLE32(p + CDH::CompressedSizeOffset);
			uint64_t uncomp_size = ReadLE32(p + CDH::UncompressedSizeOffset);
			uint64_t alloc_size = (flags & MZ_ZIP_FLAG_COMPRESSED_DATA) ? comp_size : uncomp_size;
			if (sizeof(size_t) == sizeof(uint32_t) && alloc_size > 0x7FFFFFFF)
				return nullptr;
			
			auto pBuf = pZip->m_pAlloc(pZip->m_pAlloc_opaque, 1, (size_t)alloc_size);
			if (pBuf == nullptr)
				return nullptr;

			if (!mz_zip_reader_extract_to_mem(pZip, file_index, pBuf, (size_t)alloc_size, flags))
			{
				pZip->m_pFree(pZip->m_pAlloc_opaque, pBuf);
				return nullptr;
			}

			if (pSize) *pSize = (size_t)alloc_size;
			return pBuf;
		}
		void* mz_zip_reader_extract_file_to_heap(mz_zip_archive* pZip, const char* pFilename, size_t* pSize, mz_uint flags)
		{
			auto file_index = IndexOf(pZip, pFilename, nullptr, flags);
			if (file_index < 0)
			{
				if (pSize) *pSize = 0;
				return nullptr;
			}
			return mz_zip_reader_extract_to_heap(pZip, file_index, pSize, flags);
		}




		// ------------------------------ ZIP archive writing



		// Adds a file to an archive by fully cloning the data from another archive.
		// This function fully clones the source file's compressed data (no recompression), along with its full filename, extra data, and comment fields.
		mz_bool mz_zip_writer_add_from_zip_reader(mz_zip_archive* pZip, mz_zip_archive* pSource_zip, mz_uint file_index)
		{
			uint32_t local_header_u32[(sizeof(LDH) + sizeof(uint32_t) - 1) / sizeof(uint32_t)];
			uint8_t* pLocal_header = (uint8_t*)&local_header_u32[0];
			uint8_t central_header[sizeof(CDH)];

			if ((!pZip) || (!pZip->m_pState) || (pZip->m_zip_mode != MZ_ZIP_MODE_WRITING))
				return MZ_FALSE;

			auto pSrc_central_header = ItemStat(pSource_zip, file_index);
			if (pSrc_central_header == nullptr)
				return MZ_FALSE;

			auto pState = pZip->m_pState;
			auto num_alignment_padding_bytes = CalcAlignmentPadding(pZip);

			// no zip64 support yet
			if ((pZip->m_total_entries == 0xFFFF) || ((pZip->m_archive_size + num_alignment_padding_bytes + sizeof(LDH) + sizeof(CDH)) > 0xFFFFFFFF))
				return MZ_FALSE;

			auto cur_src_file_ofs = static_cast<uint64_t>(ReadLE32(pSrc_central_header + CDH::LocalHeaderOffset));
			auto cur_dst_file_ofs = pZip->m_archive_size;

			if (pSource_zip->m_pRead(pSource_zip->m_pIO_opaque, cur_src_file_ofs, pLocal_header, sizeof(LDH)) != sizeof(LDH))
				return MZ_FALSE;
			if (ReadLE32(pLocal_header) != LocalDirHeaderSignature)
				return MZ_FALSE;
			cur_src_file_ofs += sizeof(LDH);

			if (!WriteZeros(pZip, cur_dst_file_ofs, num_alignment_padding_bytes))
				return MZ_FALSE;
			cur_dst_file_ofs += num_alignment_padding_bytes;
			auto local_dir_header_ofs = cur_dst_file_ofs;
			if (pZip->m_entry_alignment) { mz_assert((local_dir_header_ofs & (pZip->m_entry_alignment - 1)) == 0); }

			if (pZip->m_pWrite(pZip->m_pIO_opaque, cur_dst_file_ofs, pLocal_header, sizeof(LDH)) != sizeof(LDH))
				return MZ_FALSE;
			cur_dst_file_ofs += sizeof(LDH);

			auto n = static_cast<uint64_t>(ReadLE16(pLocal_header + LDH::FilenameLengthOffset) + ReadLE16(pLocal_header + LDH::ExtraLengthOffset));
			auto comp_bytes_remaining = n + ReadLE32(pSrc_central_header + CDH::CompressedSizeOffset);

			auto pBuf = pZip->m_pAlloc(pZip->m_pAlloc_opaque, 1, std::max<size_t>(sizeof(uint32_t) * 4, std::min<size_t>(MaxIOBufferSize, comp_bytes_remaining)));
			if (pBuf == nullptr)
				return MZ_FALSE;

			while (comp_bytes_remaining)
			{
				n = std::min<uint64_t>(MaxIOBufferSize, comp_bytes_remaining);
				if (pSource_zip->m_pRead(pSource_zip->m_pIO_opaque, cur_src_file_ofs, pBuf, n) != n)
				{
					pZip->m_pFree(pZip->m_pAlloc_opaque, pBuf);
					return MZ_FALSE;
				}
				cur_src_file_ofs += n;

				if (pZip->m_pWrite(pZip->m_pIO_opaque, cur_dst_file_ofs, pBuf, n) != n)
				{
					pZip->m_pFree(pZip->m_pAlloc_opaque, pBuf);
					return MZ_FALSE;
				}
				cur_dst_file_ofs += n;

				comp_bytes_remaining -= n;
			}

			mz_uint bit_flags = ReadLE16(pLocal_header + LDH::BitFlagsOffset);
			if (bit_flags & 8)
			{
				// Copy data descriptor
				if (pSource_zip->m_pRead(pSource_zip->m_pIO_opaque, cur_src_file_ofs, pBuf, sizeof(uint32_t) * 4) != sizeof(uint32_t) * 4)
				{
					pZip->m_pFree(pZip->m_pAlloc_opaque, pBuf);
					return MZ_FALSE;
				}

				n = sizeof(uint32_t) * ((ReadLE32(pBuf) == 0x08074b50) ? 4 : 3);
				if (pZip->m_pWrite(pZip->m_pIO_opaque, cur_dst_file_ofs, pBuf, n) != n)
				{
					pZip->m_pFree(pZip->m_pAlloc_opaque, pBuf);
					return MZ_FALSE;
				}

				cur_src_file_ofs += n;
				cur_dst_file_ofs += n;
			}
			pZip->m_pFree(pZip->m_pAlloc_opaque, pBuf);

			// no zip64 support yet
			if (cur_dst_file_ofs > 0xFFFFFFFF)
				return MZ_FALSE;

			auto orig_central_dir_size = pState->m_central_dir.m_size;

			memcpy(central_header, pSrc_central_header, sizeof(CDH));
			WriteLE32(&central_header[0] + CDH::LocalHeaderOffset, local_dir_header_ofs);
			if (!mz_zip_array_push_back(pZip, &pState->m_central_dir, central_header, sizeof(CDH)))
				return MZ_FALSE;

			n = ReadLE16(pSrc_central_header + CDH::FilenameLengthOffset) + ReadLE16(pSrc_central_header + CDH::ExtraLengthOffset) + ReadLE16(pSrc_central_header + CDH::CommentLengthOffset);
			if (!mz_zip_array_push_back(pZip, &pState->m_central_dir, pSrc_central_header + sizeof(CDH), n))
			{
				mz_zip_array_resize(pZip, &pState->m_central_dir, orig_central_dir_size, MZ_FALSE);
				return MZ_FALSE;
			}

			if (pState->m_central_dir.m_size > 0xFFFFFFFF)
				return MZ_FALSE;

			n = (uint32_t)orig_central_dir_size;
			if (!mz_zip_array_push_back(pZip, &pState->m_cdir_index, &n, 1))
			{
				mz_zip_array_resize(pZip, &pState->m_central_dir, orig_central_dir_size, MZ_FALSE);
				return MZ_FALSE;
			}

			pZip->m_total_entries++;
			pZip->m_archive_size = cur_dst_file_ofs;

			return MZ_TRUE;
		}


		mz_bool mz_zip_writer_finalize_heap_archive(mz_zip_archive* pZip, void** pBuf, size_t* pSize)
		{
			if ((!pZip) || (!pZip->m_pState) || (!pBuf) || (!pSize))
				return MZ_FALSE;

			if (pZip->m_pWrite != InMemoryWriteFunc)
				return MZ_FALSE;

			if (!mz_zip_writer_finalize_archive(pZip))
				return MZ_FALSE;

			*pBuf = pZip->m_pState->m_pMem;
			*pSize = pZip->m_pState->m_mem_size;
			pZip->m_pState->m_pMem = nullptr;
			pZip->m_pState->m_mem_size = pZip->m_pState->m_mem_capacity = 0;
			return MZ_TRUE;
		}



		// ------------------- Miscellaneous high-level helper functions:
		void mz_zip_array_clear(mz_zip_archive* pZip, mz_zip_array* pArray)
		{
			pZip->m_pFree(pZip->m_pAlloc_opaque, pArray->m_p);
			memset(pArray, 0, sizeof(mz_zip_array));
		}
		mz_bool mz_zip_array_ensure_capacity(mz_zip_archive* pZip, mz_zip_array* pArray, size_t min_new_capacity, mz_uint growing)
		{
			void* pNew_p;
			size_t new_capacity = min_new_capacity;
			mz_assert(pArray->m_element_size);
			if (pArray->m_capacity >= min_new_capacity)
				return MZ_TRUE;
			if (growing)
			{
				new_capacity = std::max<size_t>(1, pArray->m_capacity);
				while (new_capacity < min_new_capacity) new_capacity *= 2;
			}
			if (nullptr == (pNew_p = pZip->m_pRealloc(pZip->m_pAlloc_opaque, pArray->m_p, pArray->m_element_size, new_capacity)))
				return MZ_FALSE;

			pArray->m_p = pNew_p; pArray->m_capacity = new_capacity;
			return MZ_TRUE;
		}
		mz_bool mz_zip_array_reserve(mz_zip_archive * pZip, mz_zip_array * pArray, size_t new_capacity, mz_uint growing)
		{
			if (new_capacity > pArray->m_capacity) { if (!mz_zip_array_ensure_capacity(pZip, pArray, new_capacity, growing)) return MZ_FALSE; }
			return MZ_TRUE;
		}
		mz_bool mz_zip_array_resize(mz_zip_archive * pZip, mz_zip_array * pArray, size_t new_size, mz_uint growing)
		{
			if (new_size > pArray->m_capacity) { if (!mz_zip_array_ensure_capacity(pZip, pArray, new_size, growing)) return MZ_FALSE; }
			pArray->m_size = new_size;
			return MZ_TRUE;
		}
		mz_bool mz_zip_array_ensure_room(mz_zip_archive * pZip, mz_zip_array * pArray, size_t n)
		{
			return mz_zip_array_reserve(pZip, pArray, pArray->m_size + n, MZ_TRUE);
		}
		mz_bool mz_zip_array_push_back(mz_zip_archive * pZip, mz_zip_array * pArray, const void* pElements, size_t n)
		{
			size_t orig_size = pArray->m_size; if (!mz_zip_array_resize(pZip, pArray, orig_size + n, MZ_TRUE)) return MZ_FALSE;
			memcpy((uint8_t*)pArray->m_p + orig_size * pArray->m_element_size, pElements, n * pArray->m_element_size);
			return MZ_TRUE;
		}

		// mz_zip_add_mem_to_archive_file_in_place() efficiently (but not atomically) appends a memory blob to a ZIP archive.
		// level_and_flags - compression level (0-10, see MZ_BEST_SPEED, MZ_BEST_COMPRESSION, etc.) logically OR'd with zero or more EZipFlags, or just set to MZ_DEFAULT_COMPRESSION.
		mz_bool mz_zip_add_mem_to_archive_file_in_place(const char* pZip_filename, const char* pArchive_name, const void* pBuf, size_t buf_size, const void* pComment, uint16_t comment_size, mz_uint level_and_flags)
		{
			mz_bool status, created_new_archive = MZ_FALSE;
			mz_zip_archive zip_archive;
			mz_file_stat_t stat;
			memset(zip_archive);
			if ((int)level_and_flags < 0)
				level_and_flags = MZ_DEFAULT_LEVEL;
			if ((!pZip_filename) || (!pArchive_name) || ((buf_size) && (!pBuf)) || ((comment_size) && (!pComment)) || ((level_and_flags & 0xF) > MZ_UBER_COMPRESSION))
				return MZ_FALSE;

			if (!ValidateItemName(pArchive_name))
				return MZ_FALSE;

			if (mz_file_stat(pZip_filename, &stat) != 0)
			{
				// Create a new archive.
				if (!mz_zip_writer_init_file(&zip_archive, pZip_filename, 0))
					return MZ_FALSE;
				created_new_archive = MZ_TRUE;
			}
			else
			{
				// Append to an existing archive.
				if (!mz_zip_reader_init_file(&zip_archive, pZip_filename, level_and_flags | MZ_ZIP_FLAG_DO_NOT_SORT_CENTRAL_DIRECTORY))
					return MZ_FALSE;
				if (!mz_zip_writer_init_from_reader(&zip_archive, pZip_filename))
				{
					mz_zip_reader_end(&zip_archive);
					return MZ_FALSE;
				}
			}
			status = mz_zip_writer_add_mem_ex(&zip_archive, pArchive_name, pBuf, buf_size, pComment, comment_size, level_and_flags, 0, 0);

			// Always finalize, even if adding failed for some reason, so we have a valid central directory. (This may not always succeed, but we can try.)
			if (!mz_zip_writer_finalize_archive(&zip_archive))
				status = MZ_FALSE;
			if (!mz_zip_writer_end(&zip_archive))
				status = MZ_FALSE;
			if ((!status) && (created_new_archive))
			{
				// It's a new archive and something went wrong, so just delete it.
				int ignoredStatus = mz_delete_file(pZip_filename);
				(void)ignoredStatus;
			}
			return status;
		}

		// Reads a single file from an archive into a heap block.
		// Returns nullptr on failure.
		void* mz_zip_extract_archive_file_to_heap(const char* pZip_filename, const char* pArchive_name, size_t* pSize, mz_uint flags)
		{
			int file_index;
			mz_zip_archive zip_archive;
			void* p = nullptr;

			if (pSize)
				* pSize = 0;

			if ((!pZip_filename) || (!pArchive_name))
				return nullptr;

			memset(zip_archive);
			if (!mz_zip_reader_init_file(&zip_archive, pZip_filename, flags | MZ_ZIP_FLAG_DO_NOT_SORT_CENTRAL_DIRECTORY))
				return nullptr;

			if ((file_index = IndexOf(&zip_archive, pArchive_name, nullptr, flags)) >= 0)
				p = mz_zip_reader_extract_to_heap(&zip_archive, file_index, pSize, flags);

			mz_zip_reader_end(&zip_archive);
			return p;
		}


	private: // ------------------- Low-level Compression (independent from all decompression API's)

		 // tdefl_init() compression flags logically OR'd together (low 12 bits contain the max. number of probes per dictionary search):
		enum
		{
			TDEFL_HUFFMAN_ONLY = 0,
			// TDEFL_DEFAULT_MAX_PROBES: The compressor defaults to 128 dictionary probes per dictionary search.
			// 0=Huffman only, 1=Huffman+LZ (fastest/crap compression), 4095=Huffman+LZ (slowest/best compression).
			TDEFL_DEFAULT_MAX_PROBES = 128,
			TDEFL_MAX_PROBES_MASK = 0xFFF,
		};



		struct tdefl_sym_freq
		{
			uint16_t m_key;
			uint16_t m_sym_index;
		};
		struct tdefl_output_buffer
		{
			size_t m_size;
			size_t m_capacity;
			uint8_t* m_pBuf;
			mz_bool m_expandable;
		};

		// tdefl_compress_mem_to_heap() compresses a block in memory to a heap block allocated via malloc().
		// On entry:
		//  pSrc_buf, src_buf_len: Pointer and size of source block to compress.
		//  flags: The max match finder probes (default is 128) logically OR'd against the above flags. Higher probes are slower but improve compression.
		// On return:
		//  Function returns a pointer to the compressed data, or nullptr on failure.
		//  *pOut_len will be set to the compressed data's size, which could be larger than src_buf_len on uncompressible data.
		//  The caller must free() the returned block when it's no longer needed.
		void* tdefl_compress_mem_to_heap(const void* pSrc_buf, size_t src_buf_len, size_t* pOut_len, int flags)
		{
			if (!pOut_len)
				return nullptr;

			*pOut_len = 0;

			tdefl_output_buffer out_buf = {};
			out_buf.m_expandable = MZ_TRUE;
			if (!tdefl_compress_mem_to_output(pSrc_buf, src_buf_len, tdefl_output_buffer_putter, &out_buf, flags))
				return nullptr;

			*pOut_len = out_buf.m_size;
			return out_buf.m_pBuf;
		}

		// tdefl_compress_mem_to_mem() compresses a block in memory to another block in memory. Returns 0 on failure.
		size_t tdefl_compress_mem_to_mem(void* pOut_buf, size_t out_buf_len, const void* pSrc_buf, size_t src_buf_len, int flags)
		{
			if (!pOut_buf)
				return 0;

			tdefl_output_buffer out_buf = {};
			out_buf.m_pBuf = (uint8_t*)pOut_buf; out_buf.m_capacity = out_buf_len;
			if (!tdefl_compress_mem_to_output(pSrc_buf, src_buf_len, tdefl_output_buffer_putter, &out_buf, flags)) return 0;
			return out_buf.m_size;
		}

		// Compresses an image to a compressed PNG file in memory.
		// On entry:
		//  pImage, w, h, and num_chans describe the image to compress. num_chans may be 1, 2, 3, or 4. 
		//  The image pitch in bytes per scanline will be w*num_chans. The leftmost pixel on the top scanline is stored first in memory.
		//  level may range from [0,10], use MZ_NO_COMPRESSION, MZ_BEST_SPEED, MZ_BEST_COMPRESSION, etc. or a decent default is MZ_DEFAULT_LEVEL
		//  If flip is true, the image will be flipped on the Y axis (useful for OpenGL apps).
		// On return:
		//  Function returns a pointer to the compressed data, or nullptr on failure.
		//  *pLen_out will be set to the size of the PNG image file.
		//  The caller must mz_free() the returned heap block (which will typically be larger than *pLen_out) when it's no longer needed.
		void* tdefl_write_image_to_png_file_in_memory(const void* pImage, int w, int h, int num_chans, size_t* pLen_out)
		{
			// Level 6 corresponds to TDEFL_DEFAULT_MAX_PROBES or MZ_DEFAULT_LEVEL (but we can't depend on MZ_DEFAULT_LEVEL being available in case the zlib API's where #defined out)
			return tdefl_write_image_to_png_file_in_memory_ex(pImage, w, h, num_chans, pLen_out, 6, MZ_FALSE);
		}
		void* tdefl_write_image_to_png_file_in_memory_ex(const void* pImage, int w, int h, int num_chans, size_t* pLen_out, mz_uint level, mz_bool flip)
		{
			// Simple PNG writer function by Alex Evans, 2011. Released into the public domain: https://gist.github.com/908299, more context at
			// http://altdevblogaday.org/2011/04/06/a-smaller-jpg-encoder/.
			// This is actually a modification of Alex's original code so PNG files generated by this function pass pngcheck.

			// Using a local copy of this array here in case MINIZ_NO_ZLIB_APIS was defined.
			static const mz_uint s_tdefl_png_num_probes[11] = { 0, 1, 6, 32, 16, 32, 128, 256, 512, 768, 1500 };
			tdefl_compressor* pComp = (tdefl_compressor*)mz_malloc(sizeof(tdefl_compressor)); tdefl_output_buffer out_buf; int i, bpl = w * num_chans, y, z; uint32_t c; *pLen_out = 0;
			if (!pComp) return nullptr;
			memset(out_buf); out_buf.m_expandable = MZ_TRUE; out_buf.m_capacity = 57 + std::max(64, (1 + bpl) * h); if (nullptr == (out_buf.m_pBuf = (uint8_t*)mz_malloc(out_buf.m_capacity))) { mz_free(pComp); return nullptr; }
			// write dummy header
			for (z = 41; z; --z) tdefl_output_buffer_putter(&z, 1, &out_buf);
			// compress image data
			tdefl_init(pComp, tdefl_output_buffer_putter, &out_buf, s_tdefl_png_num_probes[std::min<mz_uint>(10, level)] | TDEFL_WRITE_ZLIB_HEADER);
			for (y = 0; y < h; ++y) { tdefl_compress_buffer(pComp, &z, 1, TDEFL_NO_FLUSH); tdefl_compress_buffer(pComp, (uint8_t*)pImage + (flip ? (h - 1 - y) : y) * bpl, bpl, TDEFL_NO_FLUSH); }
			if (tdefl_compress_buffer(pComp, nullptr, 0, TDEFL_FINISH) != TDEFL_STATUS_DONE) { mz_free(pComp); mz_free(out_buf.m_pBuf); return nullptr; }
			// write real header
			*pLen_out = out_buf.m_size - 41;
			{
				static const uint8_t chans[] = { 0x00, 0x00, 0x04, 0x02, 0x06 };
				uint8_t pnghdr[41] = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
					0, 0, (uint8_t)(w >> 8), (uint8_t)w, 0, 0, (uint8_t)(h >> 8), (uint8_t)h, 8, chans[num_chans], 0, 0, 0, 0, 0, 0, 0,
					(uint8_t)(*pLen_out >> 24), (uint8_t)(*pLen_out >> 16), (uint8_t)(*pLen_out >> 8), (uint8_t)* pLen_out, 0x49, 0x44, 0x41, 0x54 };
				c = (uint32_t)mz_crc32(MZ_CRC32_INIT, pnghdr + 12, 17); for (i = 0; i < 4; ++i, c <<= 8) ((uint8_t*)(pnghdr + 29))[i] = (uint8_t)(c >> 24);
				memcpy(out_buf.m_pBuf, pnghdr, 41);
			}
			// write footer (IDAT CRC-32, followed by IEND chunk)
			if (!tdefl_output_buffer_putter("\0\0\0\0\0\0\0\0\x49\x45\x4e\x44\xae\x42\x60\x82", 16, &out_buf)) { *pLen_out = 0; mz_free(pComp); mz_free(out_buf.m_pBuf); return nullptr; }
			c = (uint32_t)mz_crc32(MZ_CRC32_INIT, out_buf.m_pBuf + 41 - 4, *pLen_out + 4); for (i = 0; i < 4; ++i, c <<= 8) (out_buf.m_pBuf + out_buf.m_size - 16)[i] = (uint8_t)(c >> 24);
			// compute final size of file, grab compressed data buffer and return
			*pLen_out += 57; mz_free(pComp); return out_buf.m_pBuf;
		}

		// tdefl_compress_mem_to_output() compresses a block to an output stream. The above helpers use this function internally.
		mz_bool tdefl_compress_mem_to_output(const void* pBuf, size_t buf_len, tdefl_put_buf_func_ptr pPut_buf_func, void* pPut_buf_user, int flags)
		{
			tdefl_compressor* pComp;
			mz_bool succeeded;
			if (((buf_len) && (!pBuf)) || (!pPut_buf_func))
				return MZ_FALSE;

			pComp = (tdefl_compressor*)mz_malloc(sizeof(tdefl_compressor)); if (!pComp) return MZ_FALSE;
			succeeded = (tdefl_init(pComp, pPut_buf_func, pPut_buf_user, flags) == TDEFL_STATUS_OKAY);
			succeeded = succeeded && (tdefl_compress_buffer(pComp, pBuf, buf_len, TDEFL_FINISH) == TDEFL_STATUS_DONE);
			mz_free(pComp);
			return succeeded;
		}



		tdefl_status tdefl_get_prev_return_status(tdefl_compressor* d)
		{
			return d->m_prev_return_status;
		}
		uint32_t tdefl_get_adler32(tdefl_compressor* d)
		{
			return d->m_adler32;
		}

		// Create tdefl_compress() flags given zlib-style compression parameters.
		// level may range from [0,10] (where 10 is absolute max compression, but may be much slower on some files)
		// window_bits may be -15 (raw deflate) or 15 (zlib)
		// strategy may be either MZ_DEFAULT_STRATEGY, MZ_FILTERED, MZ_HUFFMAN_ONLY, MZ_RLE, or MZ_FIXED
		static ECompressionFlags CompressionFlagsFrom(ECompressionLevel level, int window_bits, ECompressionStrategy strategy)
		{
			assert(level >= ECompressionLevel::None && level <= ECompressionLevel::Uber);
			static mz_uint const s_tdefl_num_probes[11] = { 0, 1, 6, 32, 16, 32, 128, 256, 512, 768, 1500 };

			// level may actually range from [0,10] (10 is a "hidden" max level, where we want a bit more compression and it's fine if throughput to fall off a cliff on some files).
			auto comp_flags = static_cast<ECompressionFlags>(s_tdefl_num_probes[level]) | (level <= 3 ? ECompressionFlags::GreedyParsing : ECompressionFlags::None);

			if (window_bits > 0)
				comp_flags |= ECompressionFlags::WriteZLibHeader;

			if (level != ECompressionLevel::None)
				comp_flags |= ECompressionFlags::ForceAllRawBlocks;
			else if (strategy == ECompressionStrategy::Filtered)
				comp_flags |= ECompressionFlags::FilterMatches;
			else if (strategy == ECompressionStrategy::HuffmanOnly)
				comp_flags &= ~TDEFL_MAX_PROBES_MASK;
			else if (strategy == ECompressionStrategy::Fixed)
				comp_flags |= ECompressionFlags::ForceAllStaticBlocks;
			else if (strategy == ECompressionStrategy::RLE)
				comp_flags |= ECompressionFlags::RLEMatches;

			return comp_flags;
		}

		// Radix sorts tdefl_sym_freq[] array by 16-bit key m_key. Returns ptr to sorted values.
		tdefl_sym_freq* tdefl_radix_sort_syms(mz_uint num_syms, tdefl_sym_freq * pSyms0, tdefl_sym_freq * pSyms1)
		{
			uint32_t total_passes = 2, pass_shift, pass, i, hist[256 * 2] = {};
			tdefl_sym_freq* pCur_syms = pSyms0, *pNew_syms = pSyms1;

			for (i = 0; i < num_syms; i++) { mz_uint freq = pSyms0[i].m_key; hist[freq & 0xFF]++; hist[256 + ((freq >> 8) & 0xFF)]++; }
			while ((total_passes > 1) && (num_syms == hist[(total_passes - 1) * 256])) total_passes--;
			for (pass_shift = 0, pass = 0; pass < total_passes; pass++, pass_shift += 8)
			{
				const uint32_t* pHist = &hist[pass << 8];
				mz_uint offsets[256], cur_ofs = 0;
				for (i = 0; i < 256; i++) { offsets[i] = cur_ofs; cur_ofs += pHist[i]; }
				for (i = 0; i < num_syms; i++) pNew_syms[offsets[(pCur_syms[i].m_key >> pass_shift) & 0xFF]++] = pCur_syms[i];
				{ tdefl_sym_freq* t = pCur_syms; pCur_syms = pNew_syms; pNew_syms = t; }
			}
			return pCur_syms;
		}

		// tdefl_calculate_minimum_redundancy() originally written by: Alistair Moffat, alistair@cs.mu.oz.au, Jyrki Katajainen, jyrki@diku.dk, November 1996.
		void tdefl_calculate_minimum_redundancy(tdefl_sym_freq* A, int n)
		{
			int root, leaf, next, avbl, used, dpth;
			if (n == 0) return;
			else if (n == 1) { A[0].m_key = 1; return; }
			A[0].m_key += A[1].m_key; root = 0; leaf = 2;
			for (next = 1; next < n - 1; next++)
			{
				if (leaf >= n || A[root].m_key < A[leaf].m_key) { A[next].m_key = A[root].m_key; A[root++].m_key = (uint16_t)next; }
				else A[next].m_key = A[leaf++].m_key;
				if (leaf >= n || (root < next && A[root].m_key < A[leaf].m_key)) { A[next].m_key = (uint16_t)(A[next].m_key + A[root].m_key); A[root++].m_key = (uint16_t)next; }
				else A[next].m_key = (uint16_t)(A[next].m_key + A[leaf++].m_key);
			}
			A[n - 2].m_key = 0; for (next = n - 3; next >= 0; next--) A[next].m_key = A[A[next].m_key].m_key + 1;
			avbl = 1; used = dpth = 0; root = n - 2; next = n - 1;
			while (avbl > 0)
			{
				while (root >= 0 && (int)A[root].m_key == dpth) { used++; root--; }
				while (avbl > used) { A[next--].m_key = (uint16_t)(dpth); avbl--; }
				avbl = 2 * used; dpth++; used = 0;
			}
		}

		// Limits canonical Huffman code table's max code size.
		void tdefl_huffman_enforce_max_code_size(int* pNum_codes, int code_list_len, int max_code_size)
		{
			int i; uint32_t total = 0; if (code_list_len <= 1) return;
			for (i = max_code_size + 1; i <= TDEFL_MAX_SUPPORTED_HUFF_CODESIZE; i++) pNum_codes[max_code_size] += pNum_codes[i];
			for (i = max_code_size; i > 0; i--) total += (((uint32_t)pNum_codes[i]) << (max_code_size - i));
			while (total != (1UL << max_code_size))
			{
				pNum_codes[max_code_size]--;
				for (i = max_code_size - 1; i > 0; i--) if (pNum_codes[i]) { pNum_codes[i]--; pNum_codes[i + 1] += 2; break; }
				total--;
			}
		}

		#define TDEFL_READ_UNALIGNED_WORD(p) *(const uint16_t*)(p)

		void tdefl_optimize_huffman_table(tdefl_compressor* d, int table_num, int table_len, int code_size_limit, int static_table)
		{
			int i, j, l, num_codes[1 + TDEFL_MAX_SUPPORTED_HUFF_CODESIZE];
			mz_uint next_code[TDEFL_MAX_SUPPORTED_HUFF_CODESIZE + 1] = {};
			if (static_table)
			{
				for (i = 0; i < table_len; i++) num_codes[d->m_huff_code_sizes[table_num][i]]++;
			}
			else
			{
				tdefl_sym_freq syms0[TDEFL_MAX_HUFF_SYMBOLS], syms1[TDEFL_MAX_HUFF_SYMBOLS], * pSyms;
				int num_used_syms = 0;
				const uint16_t* pSym_count = &d->m_huff_count[table_num][0];
				for (i = 0; i < table_len; i++) if (pSym_count[i]) { syms0[num_used_syms].m_key = (uint16_t)pSym_count[i]; syms0[num_used_syms++].m_sym_index = (uint16_t)i; }

				pSyms = tdefl_radix_sort_syms(num_used_syms, syms0, syms1);
				tdefl_calculate_minimum_redundancy(pSyms, num_used_syms);

				for (i = 0; i < num_used_syms; i++) num_codes[pSyms[i].m_key]++;

				tdefl_huffman_enforce_max_code_size(num_codes, num_used_syms, code_size_limit);

				memset(d->m_huff_code_sizes[table_num]);
				memset(d->m_huff_codes[table_num]);
				for (i = 1, j = num_used_syms; i <= code_size_limit; i++)
					for (l = num_codes[i]; l > 0; l--) d->m_huff_code_sizes[table_num][pSyms[--j].m_sym_index] = (uint8_t)(i);
			}

			next_code[1] = 0; for (j = 0, i = 2; i <= code_size_limit; i++) next_code[i] = j = ((j + num_codes[i - 1]) << 1);

			for (i = 0; i < table_len; i++)
			{
				mz_uint rev_code = 0, code, code_size; if ((code_size = d->m_huff_code_sizes[table_num][i]) == 0) continue;
				code = next_code[code_size]++; for (l = code_size; l > 0; l--, code >>= 1) rev_code = (rev_code << 1) | (code & 1);
				d->m_huff_codes[table_num][i] = (uint16_t)rev_code;
			}
		}
		void tdefl_start_dynamic_block(tdefl_compressor* d)
		{
			static uint8_t s_tdefl_packed_code_size_syms_swizzle[] = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

			int num_lit_codes, num_dist_codes, num_bit_lengths;
			mz_uint i, total_code_sizes_to_pack, num_packed_code_sizes, rle_z_count, rle_repeat_count, packed_code_sizes_index;
			uint8_t code_sizes_to_pack[TDEFL_MAX_HUFF_SYMBOLS_0 + TDEFL_MAX_HUFF_SYMBOLS_1], packed_code_sizes[TDEFL_MAX_HUFF_SYMBOLS_0 + TDEFL_MAX_HUFF_SYMBOLS_1], prev_code_size = 0xFF;

			d->m_huff_count[0][256] = 1;

			tdefl_optimize_huffman_table(d, 0, TDEFL_MAX_HUFF_SYMBOLS_0, 15, MZ_FALSE);
			tdefl_optimize_huffman_table(d, 1, TDEFL_MAX_HUFF_SYMBOLS_1, 15, MZ_FALSE);

			for (num_lit_codes = 286; num_lit_codes > 257; num_lit_codes--) if (d->m_huff_code_sizes[0][num_lit_codes - 1]) break;
			for (num_dist_codes = 30; num_dist_codes > 1; num_dist_codes--) if (d->m_huff_code_sizes[1][num_dist_codes - 1]) break;

			memcpy(code_sizes_to_pack, &d->m_huff_code_sizes[0][0], num_lit_codes);
			memcpy(code_sizes_to_pack + num_lit_codes, &d->m_huff_code_sizes[1][0], num_dist_codes);
			total_code_sizes_to_pack = num_lit_codes + num_dist_codes;
			num_packed_code_sizes = 0;
			rle_z_count = 0;
			rle_repeat_count = 0;

			auto TDEFL_RLE_ZERO_CODE_SIZE = [&]
			{
				if (rle_z_count)
				{
					if (rle_z_count < 3)
					{
						d->m_huff_count[2][0] = (uint16_t)(d->m_huff_count[2][0] + rle_z_count);
						while (rle_z_count--)
							packed_code_sizes[num_packed_code_sizes++] = 0;
					}
					else if (rle_z_count <= 10)
					{
						d->m_huff_count[2][17] = (uint16_t)(d->m_huff_count[2][17] + 1);
						packed_code_sizes[num_packed_code_sizes++] = 17;
						packed_code_sizes[num_packed_code_sizes++] = (uint8_t)(rle_z_count - 3);
					}
					else
					{
						d->m_huff_count[2][18] = (uint16_t)(d->m_huff_count[2][18] + 1);
						packed_code_sizes[num_packed_code_sizes++] = 18;
						packed_code_sizes[num_packed_code_sizes++] = (uint8_t)(rle_z_count - 11);
					}
					rle_z_count = 0;
				}
			};
			auto TDEFL_RLE_PREV_CODE_SIZE = [&]
			{
				if (rle_repeat_count)
				{
					if (rle_repeat_count < 3)
					{
						d->m_huff_count[2][prev_code_size] = (uint16_t)(d->m_huff_count[2][prev_code_size] + rle_repeat_count);
						while (rle_repeat_count--)
							packed_code_sizes[num_packed_code_sizes++] = prev_code_size;
					}
					else
					{
						d->m_huff_count[2][16] = (uint16_t)(d->m_huff_count[2][16] + 1);
						packed_code_sizes[num_packed_code_sizes++] = 16;
						packed_code_sizes[num_packed_code_sizes++] = (uint8_t)(rle_repeat_count - 3);
					}
					rle_repeat_count = 0;
				}
			};

			memset(&d->m_huff_count[2][0], 0, sizeof(d->m_huff_count[2][0]) * TDEFL_MAX_HUFF_SYMBOLS_2);
			for (i = 0; i < total_code_sizes_to_pack; i++)
			{
				uint8_t code_size = code_sizes_to_pack[i];
				if (!code_size)
				{
					TDEFL_RLE_PREV_CODE_SIZE();
					if (++rle_z_count == 138) { TDEFL_RLE_ZERO_CODE_SIZE(); }
				}
				else
				{
					TDEFL_RLE_ZERO_CODE_SIZE();
					if (code_size != prev_code_size)
					{
						TDEFL_RLE_PREV_CODE_SIZE();
						d->m_huff_count[2][code_size] = (uint16_t)(d->m_huff_count[2][code_size] + 1); packed_code_sizes[num_packed_code_sizes++] = code_size;
					}
					else if (++rle_repeat_count == 6)
					{
						TDEFL_RLE_PREV_CODE_SIZE();
					}
				}
				prev_code_size = code_size;
			}
			if (rle_repeat_count) { TDEFL_RLE_PREV_CODE_SIZE(); }
			else { TDEFL_RLE_ZERO_CODE_SIZE(); }

			tdefl_optimize_huffman_table(d, 2, TDEFL_MAX_HUFF_SYMBOLS_2, 7, MZ_FALSE);

			tdefl_put_bits(d, 2, 2);

			tdefl_put_bits(d, num_lit_codes - 257, 5);
			tdefl_put_bits(d, num_dist_codes - 1, 5);

			for (num_bit_lengths = 18; num_bit_lengths >= 0; num_bit_lengths--) if (d->m_huff_code_sizes[2][s_tdefl_packed_code_size_syms_swizzle[num_bit_lengths]]) break;
			num_bit_lengths = std::max(4, (num_bit_lengths + 1));
			tdefl_put_bits(d, num_bit_lengths - 4, 4);
			for (i = 0; (int)i < num_bit_lengths; i++)
				tdefl_put_bits(d, d->m_huff_code_sizes[2][s_tdefl_packed_code_size_syms_swizzle[i]], 3);

			for (packed_code_sizes_index = 0; packed_code_sizes_index < num_packed_code_sizes; )
			{
				mz_uint code = packed_code_sizes[packed_code_sizes_index++];
				mz_assert(code < TDEFL_MAX_HUFF_SYMBOLS_2);
				tdefl_put_bits(d, d->m_huff_codes[2][code], d->m_huff_code_sizes[2][code]);
				if (code >= 16) tdefl_put_bits(d, packed_code_sizes[packed_code_sizes_index++], "\02\03\07"[code - 16]);
			}
		}
		void tdefl_start_static_block(tdefl_compressor* d)
		{
			uint8_t* p = &d->m_huff_code_sizes[0][0];

			mz_uint i = 0;
			for (; i <= 143; ++i) *p++ = 8;
			for (; i <= 255; ++i) *p++ = 9;
			for (; i <= 279; ++i) *p++ = 7;
			for (; i <= 287; ++i) *p++ = 8;

			memset(d->m_huff_code_sizes[1], 5, 32);

			tdefl_optimize_huffman_table(d, 0, 288, 15, MZ_TRUE);
			tdefl_optimize_huffman_table(d, 1, 32, 15, MZ_TRUE);

			tdefl_put_bits(d, 1, 2);
		}
		void tdefl_put_bits(tdefl_compressor * d, mz_uint bits, mz_uint len)
		{
			mz_assert(bits <= ((1U << len) - 1U));

			d->m_bit_buffer |= (bits << d->m_bits_in); d->m_bits_in += len;
			while (d->m_bits_in >= 8)
			{
				if (d->m_pOutput_buf < d->m_pOutput_buf_end)
					*d->m_pOutput_buf++ = (uint8_t)(d->m_bit_buffer);

				d->m_bit_buffer >>= 8;
				d->m_bits_in -= 8;
			}
		}
		mz_bool tdefl_compress_lz_codes(tdefl_compressor* d)
		{
			if constexpr (UnalignedLoadStore && LittleEndian && sizeof(size_t) == 8)
			{
				mz_uint flags;
				uint8_t* pLZ_codes;
				uint8_t* pOutput_buf = d->m_pOutput_buf;
				uint8_t* pLZ_code_buf_end = d->m_pLZ_code_buf;
				uint64_t bit_buffer = d->m_bit_buffer;
				mz_uint bits_in = d->m_bits_in;

				#define TDEFL_PUT_BITS_FAST(b, l) { bit_buffer |= (((uint64_t)(b)) << bits_in); bits_in += (l); }

				flags = 1;
				for (pLZ_codes = d->m_lz_code_buf; pLZ_codes < pLZ_code_buf_end; flags >>= 1)
				{
					if (flags == 1)
						flags = *pLZ_codes++ | 0x100;

					if (flags & 1)
					{
						mz_uint s0, s1, n0, n1, sym, num_extra_bits;
						mz_uint match_len = pLZ_codes[0], match_dist = *(const uint16_t*)(pLZ_codes + 1); pLZ_codes += 3;

						mz_assert(d->m_huff_code_sizes[0][s_tdefl_len_sym[match_len]]);
						TDEFL_PUT_BITS_FAST(d->m_huff_codes[0][s_tdefl_len_sym[match_len]], d->m_huff_code_sizes[0][s_tdefl_len_sym[match_len]]);
						TDEFL_PUT_BITS_FAST(match_len & mz_bitmasks[s_tdefl_len_extra[match_len]], s_tdefl_len_extra[match_len]);

						// This sequence coaxes MSVC into using cmov's vs. jmp's.
						s0 = s_tdefl_small_dist_sym[match_dist & 511];
						n0 = s_tdefl_small_dist_extra[match_dist & 511];
						s1 = s_tdefl_large_dist_sym[match_dist >> 8];
						n1 = s_tdefl_large_dist_extra[match_dist >> 8];
						sym = (match_dist < 512) ? s0 : s1;
						num_extra_bits = (match_dist < 512) ? n0 : n1;

						mz_assert(d->m_huff_code_sizes[1][sym]);
						TDEFL_PUT_BITS_FAST(d->m_huff_codes[1][sym], d->m_huff_code_sizes[1][sym]);
						TDEFL_PUT_BITS_FAST(match_dist & mz_bitmasks[num_extra_bits], num_extra_bits);
					}
					else
					{
						mz_uint lit = *pLZ_codes++;
						mz_assert(d->m_huff_code_sizes[0][lit]);
						TDEFL_PUT_BITS_FAST(d->m_huff_codes[0][lit], d->m_huff_code_sizes[0][lit]);

						if (((flags & 2) == 0) && (pLZ_codes < pLZ_code_buf_end))
						{
							flags >>= 1;
							lit = *pLZ_codes++;
							mz_assert(d->m_huff_code_sizes[0][lit]);
							TDEFL_PUT_BITS_FAST(d->m_huff_codes[0][lit], d->m_huff_code_sizes[0][lit]);

							if (((flags & 2) == 0) && (pLZ_codes < pLZ_code_buf_end))
							{
								flags >>= 1;
								lit = *pLZ_codes++;
								mz_assert(d->m_huff_code_sizes[0][lit]);
								TDEFL_PUT_BITS_FAST(d->m_huff_codes[0][lit], d->m_huff_code_sizes[0][lit]);
							}
						}
					}

					if (pOutput_buf >= d->m_pOutput_buf_end)
						return MZ_FALSE;

					*(uint64_t*)pOutput_buf = bit_buffer;
					pOutput_buf += (bits_in >> 3);
					bit_buffer >>= (bits_in & ~7);
					bits_in &= 7;
				}

				#undef TDEFL_PUT_BITS_FAST

				d->m_pOutput_buf = pOutput_buf;
				d->m_bits_in = 0;
				d->m_bit_buffer = 0;

				while (bits_in)
				{
					auto n = std::min<mz_uint>(bits_in, 16);
					tdefl_put_bits(d, (mz_uint)bit_buffer & mz_bitmasks[n], n);
					bit_buffer >>= n;
					bits_in -= n;
				}

				tdefl_put_bits(d, d->m_huff_codes[0][256], d->m_huff_code_sizes[0][256]);

				return (d->m_pOutput_buf < d->m_pOutput_buf_end);
			}
			else
			{
				mz_uint flags;
				uint8_t* pLZ_codes;

				flags = 1;
				for (pLZ_codes = d->m_lz_code_buf; pLZ_codes < d->m_pLZ_code_buf; flags >>= 1)
				{
					if (flags == 1)
						flags = *pLZ_codes++ | 0x100;
					if (flags & 1)
					{
						mz_uint sym, num_extra_bits;
						mz_uint match_len = pLZ_codes[0], match_dist = (pLZ_codes[1] | (pLZ_codes[2] << 8)); pLZ_codes += 3;

						mz_assert(d->m_huff_code_sizes[0][s_tdefl_len_sym[match_len]]);
						tdefl_put_bits(d, d->m_huff_codes[0][s_tdefl_len_sym[match_len]], d->m_huff_code_sizes[0][s_tdefl_len_sym[match_len]]);
						tdefl_put_bits(d, match_len & mz_bitmasks[s_tdefl_len_extra[match_len]], s_tdefl_len_extra[match_len]);

						if (match_dist < 512)
						{
							sym = s_tdefl_small_dist_sym[match_dist]; num_extra_bits = s_tdefl_small_dist_extra[match_dist];
						}
						else
						{
							sym = s_tdefl_large_dist_sym[match_dist >> 8]; num_extra_bits = s_tdefl_large_dist_extra[match_dist >> 8];
						}
						mz_assert(d->m_huff_code_sizes[1][sym]);
						tdefl_put_bits(d, d->m_huff_codes[1][sym], d->m_huff_code_sizes[1][sym]);
						tdefl_put_bits(d, match_dist & mz_bitmasks[num_extra_bits], num_extra_bits);
					}
					else
					{
						mz_uint lit = *pLZ_codes++;
						mz_assert(d->m_huff_code_sizes[0][lit]);
						tdefl_put_bits(d, d->m_huff_codes[0][lit], d->m_huff_code_sizes[0][lit]);
					}
				}

				tdefl_put_bits(d, d->m_huff_codes[0][256], d->m_huff_code_sizes[0][256]);

				return (d->m_pOutput_buf < d->m_pOutput_buf_end);
			}
		}
		mz_bool tdefl_compress_block(tdefl_compressor * d, mz_bool static_block)
		{
			if (static_block)
				tdefl_start_static_block(d);
			else
				tdefl_start_dynamic_block(d);

			return tdefl_compress_lz_codes(d);
		}
		mz_bool tdefl_compress_fast(tdefl_compressor* d)
		{
			if constexpr (UnalignedLoadStore && LittleEndian)
			{
				// Faster, minimally featured LZRW1-style match+parse loop with better register utilization. Intended for applications where raw throughput is valued more highly than ratio.
				mz_uint lookahead_pos = d->m_lookahead_pos, lookahead_size = d->m_lookahead_size, dict_size = d->m_dict_size, total_lz_bytes = d->m_total_lz_bytes, num_flags_left = d->m_num_flags_left;
				uint8_t* pLZ_code_buf = d->m_pLZ_code_buf, * pLZ_flags = d->m_pLZ_flags;
				mz_uint cur_pos = lookahead_pos & TDEFL_LZ_DICT_SIZE_MASK;

				while ((d->m_src_buf_left) || ((d->m_flush) && (lookahead_size)))
				{
					const mz_uint TDEFL_COMP_FAST_LOOKAHEAD_SIZE = 4096;
					mz_uint dst_pos = (lookahead_pos + lookahead_size) & TDEFL_LZ_DICT_SIZE_MASK;
					mz_uint num_bytes_to_process = std::min<mz_uint>(static_cast<mz_uint>(d->m_src_buf_left), static_cast<mz_uint>(TDEFL_COMP_FAST_LOOKAHEAD_SIZE - lookahead_size));
					d->m_src_buf_left -= num_bytes_to_process;
					lookahead_size += num_bytes_to_process;

					while (num_bytes_to_process)
					{
						uint32_t n = std::min(TDEFL_LZ_DICT_SIZE - dst_pos, num_bytes_to_process);
						memcpy(d->m_dict + dst_pos, d->m_pSrc, n);
						if (dst_pos < (TDEFL_MAX_MATCH_LEN - 1))
							memcpy(d->m_dict + TDEFL_LZ_DICT_SIZE + dst_pos, d->m_pSrc, std::min(n, (TDEFL_MAX_MATCH_LEN - 1) - dst_pos));
						d->m_pSrc += n;
						dst_pos = (dst_pos + n) & TDEFL_LZ_DICT_SIZE_MASK;
						num_bytes_to_process -= n;
					}

					dict_size = std::min(TDEFL_LZ_DICT_SIZE - lookahead_size, dict_size);
					if ((!d->m_flush) && (lookahead_size < TDEFL_COMP_FAST_LOOKAHEAD_SIZE)) break;

					while (lookahead_size >= 4)
					{
						mz_uint cur_match_dist, cur_match_len = 1;
						uint8_t* pCur_dict = d->m_dict + cur_pos;
						mz_uint first_trigram = (*(const uint32_t*)pCur_dict) & 0xFFFFFF;
						mz_uint hash = (first_trigram ^ (first_trigram >> (24 - (TDEFL_LZ_HASH_BITS - 8)))) & TDEFL_LEVEL1_HASH_SIZE_MASK;
						mz_uint probe_pos = d->m_hash[hash];
						d->m_hash[hash] = (uint16_t)lookahead_pos;

						if (((cur_match_dist = (uint16_t)(lookahead_pos - probe_pos)) <= dict_size) && ((*(const uint32_t*)(d->m_dict + (probe_pos &= TDEFL_LZ_DICT_SIZE_MASK)) & 0xFFFFFF) == first_trigram))
						{
							const uint16_t* p = (const uint16_t*)pCur_dict;
							const uint16_t* q = (const uint16_t*)(d->m_dict + probe_pos);
							uint32_t probe_len = 32;
							do {}
							while ((TDEFL_READ_UNALIGNED_WORD(++p) == TDEFL_READ_UNALIGNED_WORD(++q)) && (TDEFL_READ_UNALIGNED_WORD(++p) == TDEFL_READ_UNALIGNED_WORD(++q)) &&
								(TDEFL_READ_UNALIGNED_WORD(++p) == TDEFL_READ_UNALIGNED_WORD(++q)) && (TDEFL_READ_UNALIGNED_WORD(++p) == TDEFL_READ_UNALIGNED_WORD(++q)) && (--probe_len > 0));
							cur_match_len = ((mz_uint)(p - (const uint16_t*)pCur_dict) * 2) + (mz_uint)(*(const uint8_t*)p == *(const uint8_t*)q);
							if (!probe_len)
								cur_match_len = cur_match_dist ? TDEFL_MAX_MATCH_LEN : 0;

							if ((cur_match_len < TDEFL_MIN_MATCH_LEN) || ((cur_match_len == TDEFL_MIN_MATCH_LEN) && (cur_match_dist >= 8U * 1024U)))
							{
								cur_match_len = 1;
								*pLZ_code_buf++ = (uint8_t)first_trigram;
								*pLZ_flags = (uint8_t)(*pLZ_flags >> 1);
								d->m_huff_count[0][(uint8_t)first_trigram]++;
							}
							else
							{
								uint32_t s0, s1;
								cur_match_len = std::min(cur_match_len, lookahead_size);

								mz_assert((cur_match_len >= TDEFL_MIN_MATCH_LEN) && (cur_match_dist >= 1) && (cur_match_dist <= TDEFL_LZ_DICT_SIZE));

								cur_match_dist--;

								pLZ_code_buf[0] = (uint8_t)(cur_match_len - TDEFL_MIN_MATCH_LEN);
								*(uint16_t*)(&pLZ_code_buf[1]) = (uint16_t)cur_match_dist;
								pLZ_code_buf += 3;
								*pLZ_flags = (uint8_t)((*pLZ_flags >> 1) | 0x80);

								s0 = s_tdefl_small_dist_sym[cur_match_dist & 511];
								s1 = s_tdefl_large_dist_sym[cur_match_dist >> 8];
								d->m_huff_count[1][(cur_match_dist < 512) ? s0 : s1]++;

								d->m_huff_count[0][s_tdefl_len_sym[cur_match_len - TDEFL_MIN_MATCH_LEN]]++;
							}
						}
						else
						{
							*pLZ_code_buf++ = (uint8_t)first_trigram;
							*pLZ_flags = (uint8_t)(*pLZ_flags >> 1);
							d->m_huff_count[0][(uint8_t)first_trigram]++;
						}

						if (--num_flags_left == 0) { num_flags_left = 8; pLZ_flags = pLZ_code_buf++; }

						total_lz_bytes += cur_match_len;
						lookahead_pos += cur_match_len;
						dict_size = std::min<mz_uint>(dict_size + cur_match_len, TDEFL_LZ_DICT_SIZE);
						cur_pos = (cur_pos + cur_match_len) & TDEFL_LZ_DICT_SIZE_MASK;
						mz_assert(lookahead_size >= cur_match_len);
						lookahead_size -= cur_match_len;

						if (pLZ_code_buf > & d->m_lz_code_buf[TDEFL_LZ_CODE_BUF_SIZE - 8])
						{
							int n;
							d->m_lookahead_pos = lookahead_pos; d->m_lookahead_size = lookahead_size; d->m_dict_size = dict_size;
							d->m_total_lz_bytes = total_lz_bytes; d->m_pLZ_code_buf = pLZ_code_buf; d->m_pLZ_flags = pLZ_flags; d->m_num_flags_left = num_flags_left;
							if ((n = tdefl_flush_block(d, 0)) != 0)
								return (n < 0) ? MZ_FALSE : MZ_TRUE;
							total_lz_bytes = d->m_total_lz_bytes; pLZ_code_buf = d->m_pLZ_code_buf; pLZ_flags = d->m_pLZ_flags; num_flags_left = d->m_num_flags_left;
						}
					}

					while (lookahead_size)
					{
						uint8_t lit = d->m_dict[cur_pos];

						total_lz_bytes++;
						*pLZ_code_buf++ = lit;
						*pLZ_flags = (uint8_t)(*pLZ_flags >> 1);
						if (--num_flags_left == 0) { num_flags_left = 8; pLZ_flags = pLZ_code_buf++; }

						d->m_huff_count[0][lit]++;

						lookahead_pos++;
						dict_size = std::min<mz_uint>(dict_size + 1, TDEFL_LZ_DICT_SIZE);
						cur_pos = (cur_pos + 1) & TDEFL_LZ_DICT_SIZE_MASK;
						lookahead_size--;

						if (pLZ_code_buf > & d->m_lz_code_buf[TDEFL_LZ_CODE_BUF_SIZE - 8])
						{
							int n;
							d->m_lookahead_pos = lookahead_pos; d->m_lookahead_size = lookahead_size; d->m_dict_size = dict_size;
							d->m_total_lz_bytes = total_lz_bytes; d->m_pLZ_code_buf = pLZ_code_buf; d->m_pLZ_flags = pLZ_flags; d->m_num_flags_left = num_flags_left;
							if ((n = tdefl_flush_block(d, 0)) != 0)
								return (n < 0) ? MZ_FALSE : MZ_TRUE;
							total_lz_bytes = d->m_total_lz_bytes; pLZ_code_buf = d->m_pLZ_code_buf; pLZ_flags = d->m_pLZ_flags; num_flags_left = d->m_num_flags_left;
						}
					}
				}

				d->m_lookahead_pos = lookahead_pos; d->m_lookahead_size = lookahead_size; d->m_dict_size = dict_size;
				d->m_total_lz_bytes = total_lz_bytes; d->m_pLZ_code_buf = pLZ_code_buf; d->m_pLZ_flags = pLZ_flags; d->m_num_flags_left = num_flags_left;
				return MZ_TRUE;
			}
			else
			{
				return MZ_FALSE;
			}
		}
		mz_bool tdefl_compress_normal(tdefl_compressor* d)
		{
			const uint8_t* pSrc = d->m_pSrc; size_t src_buf_left = d->m_src_buf_left;
			tdefl_flush flush = d->m_flush;

			while ((src_buf_left) || ((flush) && (d->m_lookahead_size)))
			{
				mz_uint len_to_move, cur_match_dist, cur_match_len, cur_pos;
				// Update dictionary and hash chains. Keeps the lookahead size equal to TDEFL_MAX_MATCH_LEN.
				if ((d->m_lookahead_size + d->m_dict_size) >= (TDEFL_MIN_MATCH_LEN - 1))
				{
					mz_uint dst_pos = (d->m_lookahead_pos + d->m_lookahead_size) & TDEFL_LZ_DICT_SIZE_MASK, ins_pos = d->m_lookahead_pos + d->m_lookahead_size - 2;
					mz_uint hash = (d->m_dict[ins_pos & TDEFL_LZ_DICT_SIZE_MASK] << TDEFL_LZ_HASH_SHIFT) ^ d->m_dict[(ins_pos + 1) & TDEFL_LZ_DICT_SIZE_MASK];
					mz_uint num_bytes_to_process = std::min<mz_uint>(static_cast<mz_uint>(src_buf_left), static_cast<mz_uint>(TDEFL_MAX_MATCH_LEN - d->m_lookahead_size));
					const uint8_t * pSrc_end = pSrc + num_bytes_to_process;
					src_buf_left -= num_bytes_to_process;
					d->m_lookahead_size += num_bytes_to_process;
					while (pSrc != pSrc_end)
					{
						uint8_t c = *pSrc++; d->m_dict[dst_pos] = c; if (dst_pos < (TDEFL_MAX_MATCH_LEN - 1)) d->m_dict[TDEFL_LZ_DICT_SIZE + dst_pos] = c;
						hash = ((hash << TDEFL_LZ_HASH_SHIFT) ^ c) & (TDEFL_LZ_HASH_SIZE - 1);
						d->m_next[ins_pos & TDEFL_LZ_DICT_SIZE_MASK] = d->m_hash[hash]; d->m_hash[hash] = (uint16_t)(ins_pos);
						dst_pos = (dst_pos + 1) & TDEFL_LZ_DICT_SIZE_MASK; ins_pos++;
					}
				}
				else
				{
					while ((src_buf_left) && (d->m_lookahead_size < TDEFL_MAX_MATCH_LEN))
					{
						uint8_t c = *pSrc++;
						mz_uint dst_pos = (d->m_lookahead_pos + d->m_lookahead_size) & TDEFL_LZ_DICT_SIZE_MASK;
						src_buf_left--;
						d->m_dict[dst_pos] = c;
						if (dst_pos < (TDEFL_MAX_MATCH_LEN - 1))
							d->m_dict[TDEFL_LZ_DICT_SIZE + dst_pos] = c;
						if ((++d->m_lookahead_size + d->m_dict_size) >= TDEFL_MIN_MATCH_LEN)
						{
							mz_uint ins_pos = d->m_lookahead_pos + (d->m_lookahead_size - 1) - 2;
							mz_uint hash = ((d->m_dict[ins_pos & TDEFL_LZ_DICT_SIZE_MASK] << (TDEFL_LZ_HASH_SHIFT * 2)) ^ (d->m_dict[(ins_pos + 1) & TDEFL_LZ_DICT_SIZE_MASK] << TDEFL_LZ_HASH_SHIFT) ^ c) & (TDEFL_LZ_HASH_SIZE - 1);
							d->m_next[ins_pos & TDEFL_LZ_DICT_SIZE_MASK] = d->m_hash[hash]; d->m_hash[hash] = (uint16_t)(ins_pos);
						}
					}
				}
				d->m_dict_size = std::min(TDEFL_LZ_DICT_SIZE - d->m_lookahead_size, d->m_dict_size);
				if ((!flush) && (d->m_lookahead_size < TDEFL_MAX_MATCH_LEN))
					break;

				// Simple lazy/greedy parsing state machine.
				len_to_move = 1; cur_match_dist = 0; cur_match_len = d->m_saved_match_len ? d->m_saved_match_len : (TDEFL_MIN_MATCH_LEN - 1); cur_pos = d->m_lookahead_pos & TDEFL_LZ_DICT_SIZE_MASK;
				if (d->m_flags & (TDEFL_RLE_MATCHES | TDEFL_FORCE_ALL_RAW_BLOCKS))
				{
					if ((d->m_dict_size) && (!(d->m_flags & TDEFL_FORCE_ALL_RAW_BLOCKS)))
					{
						uint8_t c = d->m_dict[(cur_pos - 1) & TDEFL_LZ_DICT_SIZE_MASK];
						cur_match_len = 0; while (cur_match_len < d->m_lookahead_size) { if (d->m_dict[cur_pos + cur_match_len] != c) break; cur_match_len++; }
						if (cur_match_len < TDEFL_MIN_MATCH_LEN) cur_match_len = 0; else cur_match_dist = 1;
					}
				}
				else
				{
					tdefl_find_match(d, d->m_lookahead_pos, d->m_dict_size, d->m_lookahead_size, &cur_match_dist, &cur_match_len);
				}
				if (((cur_match_len == TDEFL_MIN_MATCH_LEN) && (cur_match_dist >= 8U * 1024U)) || (cur_pos == cur_match_dist) || ((d->m_flags & TDEFL_FILTER_MATCHES) && (cur_match_len <= 5)))
				{
					cur_match_dist = cur_match_len = 0;
				}
				if (d->m_saved_match_len)
				{
					if (cur_match_len > d->m_saved_match_len)
					{
						tdefl_record_literal(d, (uint8_t)d->m_saved_lit);
						if (cur_match_len >= 128)
						{
							tdefl_record_match(d, cur_match_len, cur_match_dist);
							d->m_saved_match_len = 0; len_to_move = cur_match_len;
						}
						else
						{
							d->m_saved_lit = d->m_dict[cur_pos]; d->m_saved_match_dist = cur_match_dist; d->m_saved_match_len = cur_match_len;
						}
					}
					else
					{
						tdefl_record_match(d, d->m_saved_match_len, d->m_saved_match_dist);
						len_to_move = d->m_saved_match_len - 1; d->m_saved_match_len = 0;
					}
				}
				else if (!cur_match_dist)
					tdefl_record_literal(d, d->m_dict[std::min<mz_uint>(cur_pos, sizeof(d->m_dict) - 1)]);
				else if ((d->m_greedy_parsing) || (d->m_flags & TDEFL_RLE_MATCHES) || (cur_match_len >= 128))
				{
					tdefl_record_match(d, cur_match_len, cur_match_dist);
					len_to_move = cur_match_len;
				}
				else
				{
					d->m_saved_lit = d->m_dict[std::min<mz_uint>(cur_pos, sizeof(d->m_dict) - 1)]; d->m_saved_match_dist = cur_match_dist; d->m_saved_match_len = cur_match_len;
				}
				// Move the lookahead forward by len_to_move bytes.
				d->m_lookahead_pos += len_to_move;
				mz_assert(d->m_lookahead_size >= len_to_move);
				d->m_lookahead_size -= len_to_move;
				d->m_dict_size = std::min<mz_uint>(d->m_dict_size + len_to_move, TDEFL_LZ_DICT_SIZE);
				// Check if it's time to flush the current LZ codes to the internal output buffer.
				if ((d->m_pLZ_code_buf > & d->m_lz_code_buf[TDEFL_LZ_CODE_BUF_SIZE - 8]) ||
					((d->m_total_lz_bytes > 31 * 1024) && (((((mz_uint)(d->m_pLZ_code_buf - d->m_lz_code_buf) * 115) >> 7) >= d->m_total_lz_bytes) || (d->m_flags & TDEFL_FORCE_ALL_RAW_BLOCKS))))
				{
					int n;
					d->m_pSrc = pSrc; d->m_src_buf_left = src_buf_left;
					if ((n = tdefl_flush_block(d, 0)) != 0)
						return (n < 0) ? MZ_FALSE : MZ_TRUE;
				}
			}

			d->m_pSrc = pSrc; d->m_src_buf_left = src_buf_left;
			return MZ_TRUE;
		}
		int tdefl_flush_block(tdefl_compressor* d, int flush)
		{
			mz_uint saved_bit_buf, saved_bits_in;
			uint8_t* pSaved_output_buf;
			mz_bool comp_block_succeeded = MZ_FALSE;
			int n, use_raw_block = ((d->m_flags & TDEFL_FORCE_ALL_RAW_BLOCKS) != 0) && (d->m_lookahead_pos - d->m_lz_code_buf_dict_pos) <= d->m_dict_size;
			uint8_t * pOutput_buf_start = ((d->m_pPut_buf_func == nullptr) && ((*d->m_pOut_buf_size - d->m_out_buf_ofs) >= TDEFL_OUT_BUF_SIZE)) ? ((uint8_t*)d->m_pOut_buf + d->m_out_buf_ofs) : d->m_output_buf;

			d->m_pOutput_buf = pOutput_buf_start;
			d->m_pOutput_buf_end = d->m_pOutput_buf + TDEFL_OUT_BUF_SIZE - 16;

			mz_assert(!d->m_output_flush_remaining);
			d->m_output_flush_ofs = 0;
			d->m_output_flush_remaining = 0;

			*d->m_pLZ_flags = (uint8_t)(*d->m_pLZ_flags >> d->m_num_flags_left);
			d->m_pLZ_code_buf -= (d->m_num_flags_left == 8);

			if ((d->m_flags & TDEFL_WRITE_ZLIB_HEADER) && (!d->m_block_index))
			{
				tdefl_put_bits(d, 0x78, 8);
				tdefl_put_bits(d, 0x01, 8);
			}

			tdefl_put_bits(d, flush == TDEFL_FINISH, 1);

			pSaved_output_buf = d->m_pOutput_buf; saved_bit_buf = d->m_bit_buffer; saved_bits_in = d->m_bits_in;

			if (!use_raw_block)
				comp_block_succeeded = tdefl_compress_block(d, (d->m_flags & TDEFL_FORCE_ALL_STATIC_BLOCKS) || (d->m_total_lz_bytes < 48));

			// If the block gets expanded, forget the current contents of the output buffer and send a raw block instead.
			if (((use_raw_block) || ((d->m_total_lz_bytes) && ((d->m_pOutput_buf - pSaved_output_buf + 1U) >= d->m_total_lz_bytes))) &&
				((d->m_lookahead_pos - d->m_lz_code_buf_dict_pos) <= d->m_dict_size))
			{
				mz_uint i; d->m_pOutput_buf = pSaved_output_buf; d->m_bit_buffer = saved_bit_buf, d->m_bits_in = saved_bits_in;
				tdefl_put_bits(d, 0, 2);
				if (d->m_bits_in) { tdefl_put_bits(d, 0, 8 - d->m_bits_in); }
				for (i = 2; i; --i, d->m_total_lz_bytes ^= 0xFFFF)
				{
					tdefl_put_bits(d, d->m_total_lz_bytes & 0xFFFF, 16);
				}
				for (i = 0; i < d->m_total_lz_bytes; ++i)
				{
					tdefl_put_bits(d, d->m_dict[(d->m_lz_code_buf_dict_pos + i) & TDEFL_LZ_DICT_SIZE_MASK], 8);
				}
			}
			// Check for the extremely unlikely (if not impossible) case of the compressed block not fitting into the output buffer when using dynamic codes.
			else if (!comp_block_succeeded)
			{
				d->m_pOutput_buf = pSaved_output_buf; d->m_bit_buffer = saved_bit_buf, d->m_bits_in = saved_bits_in;
				tdefl_compress_block(d, MZ_TRUE);
			}

			if (flush)
			{
				if (flush == TDEFL_FINISH)
				{
					if (d->m_bits_in) { tdefl_put_bits(d, 0, 8 - d->m_bits_in); }
					if (d->m_flags & TDEFL_WRITE_ZLIB_HEADER) { mz_uint i, a = d->m_adler32; for (i = 0; i < 4; i++) { tdefl_put_bits(d, (a >> 24) & 0xFF, 8); a <<= 8; } }
				}
				else
				{
					mz_uint i, z = 0; tdefl_put_bits(d, 0, 3); if (d->m_bits_in) { tdefl_put_bits(d, 0, 8 - d->m_bits_in); } for (i = 2; i; --i, z ^= 0xFFFF) { tdefl_put_bits(d, z & 0xFFFF, 16); }
				}
			}

			mz_assert(d->m_pOutput_buf < d->m_pOutput_buf_end);

			memset(&d->m_huff_count[0][0], 0, sizeof(d->m_huff_count[0][0]) * TDEFL_MAX_HUFF_SYMBOLS_0);
			memset(&d->m_huff_count[1][0], 0, sizeof(d->m_huff_count[1][0]) * TDEFL_MAX_HUFF_SYMBOLS_1);

			d->m_pLZ_code_buf = d->m_lz_code_buf + 1; d->m_pLZ_flags = d->m_lz_code_buf; d->m_num_flags_left = 8; d->m_lz_code_buf_dict_pos += d->m_total_lz_bytes; d->m_total_lz_bytes = 0; d->m_block_index++;

			if ((n = (int)(d->m_pOutput_buf - pOutput_buf_start)) != 0)
			{
				if (d->m_pPut_buf_func)
				{
					*d->m_pIn_buf_size = d->m_pSrc - (const uint8_t*)d->m_pIn_buf;
					if (!(*d->m_pPut_buf_func)(d->m_output_buf, n, d->m_pPut_buf_user))
						return (d->m_prev_return_status = TDEFL_STATUS_PUT_BUF_FAILED);
				}
				else if (pOutput_buf_start == d->m_output_buf)
				{
					int bytes_to_copy = (int)std::min((size_t)n, (size_t)(*d->m_pOut_buf_size - d->m_out_buf_ofs));
					memcpy((uint8_t*)d->m_pOut_buf + d->m_out_buf_ofs, d->m_output_buf, bytes_to_copy);
					d->m_out_buf_ofs += bytes_to_copy;
					if ((n -= bytes_to_copy) != 0)
					{
						d->m_output_flush_ofs = bytes_to_copy;
						d->m_output_flush_remaining = n;
					}
				}
				else
				{
					d->m_out_buf_ofs += n;
				}
			}

			return d->m_output_flush_remaining;
		}
		void tdefl_find_match(tdefl_compressor* d, mz_uint lookahead_pos, mz_uint max_dist, mz_uint max_match_len, mz_uint* pMatch_dist, mz_uint* pMatch_len)
		{

			if constexpr (UnalignedLoadStore)
			{
				mz_uint dist, pos = lookahead_pos & TDEFL_LZ_DICT_SIZE_MASK, match_len = *pMatch_len, probe_pos = pos, next_probe_pos, probe_len;
				mz_uint num_probes_left = d->m_max_probes[match_len >= 32];
				const uint16_t* s = (const uint16_t*)(d->m_dict + pos), *p, *q;
				uint16_t c01 = TDEFL_READ_UNALIGNED_WORD(&d->m_dict[pos + match_len - 1]), s01 = TDEFL_READ_UNALIGNED_WORD(s);
				mz_assert(max_match_len <= TDEFL_MAX_MATCH_LEN); if (max_match_len <= match_len) return;
				for(;;)
				{
					for(;;)
					{
						if (--num_probes_left == 0) return;
						#define TDEFL_PROBE \
							next_probe_pos = d->m_next[probe_pos]; \
							if ((!next_probe_pos) || ((dist = (uint16_t)(lookahead_pos - next_probe_pos)) > max_dist)) return; \
							probe_pos = next_probe_pos & TDEFL_LZ_DICT_SIZE_MASK; \
							if (TDEFL_READ_UNALIGNED_WORD(&d->m_dict[probe_pos + match_len - 1]) == c01) break;
						TDEFL_PROBE; TDEFL_PROBE; TDEFL_PROBE;
					}
					if (!dist) break; q = (const uint16_t*)(d->m_dict + probe_pos); if (TDEFL_READ_UNALIGNED_WORD(q) != s01) continue; p = s; probe_len = 32;
					do {}
					while ((TDEFL_READ_UNALIGNED_WORD(++p) == TDEFL_READ_UNALIGNED_WORD(++q)) && (TDEFL_READ_UNALIGNED_WORD(++p) == TDEFL_READ_UNALIGNED_WORD(++q)) &&
						(TDEFL_READ_UNALIGNED_WORD(++p) == TDEFL_READ_UNALIGNED_WORD(++q)) && (TDEFL_READ_UNALIGNED_WORD(++p) == TDEFL_READ_UNALIGNED_WORD(++q)) && (--probe_len > 0));
					if (!probe_len)
					{
						*pMatch_dist = dist;
						*pMatch_len = std::min<mz_uint>(max_match_len, TDEFL_MAX_MATCH_LEN);
						break;
					}
					else if ((probe_len = ((mz_uint)(p - s) * 2) + (mz_uint)(*(const uint8_t*)p == *(const uint8_t*)q)) > match_len)
					{
						*pMatch_dist = dist; if ((*pMatch_len = match_len = std::min(max_match_len, probe_len)) == max_match_len) break;
						c01 = TDEFL_READ_UNALIGNED_WORD(&d->m_dict[pos + match_len - 1]);
					}
				}
			}
			else
			{
				mz_uint dist, pos = lookahead_pos & TDEFL_LZ_DICT_SIZE_MASK, match_len = *pMatch_len, probe_pos = pos, next_probe_pos, probe_len;
				mz_uint num_probes_left = d->m_max_probes[match_len >= 32];
				const uint8_t* s = d->m_dict + pos, * p, * q;
				uint8_t c0 = d->m_dict[pos + match_len], c1 = d->m_dict[pos + match_len - 1];
				mz_assert(max_match_len <= TDEFL_MAX_MATCH_LEN); if (max_match_len <= match_len) return;
				for(;;)
				{
					for(;;)
					{
						if (--num_probes_left == 0) return;
						#define TDEFL_PROBE \
			next_probe_pos = d->m_next[probe_pos]; \
			if ((!next_probe_pos) || ((dist = (uint16_t)(lookahead_pos - next_probe_pos)) > max_dist)) return; \
			probe_pos = next_probe_pos & TDEFL_LZ_DICT_SIZE_MASK; \
			if ((d->m_dict[probe_pos + match_len] == c0) && (d->m_dict[probe_pos + match_len - 1] == c1)) break;
						TDEFL_PROBE; TDEFL_PROBE; TDEFL_PROBE;
					}
					if (!dist) break; p = s; q = d->m_dict + probe_pos; for (probe_len = 0; probe_len < max_match_len; probe_len++) if (*p++ != *q++) break;
					if (probe_len > match_len)
					{
						*pMatch_dist = dist; if ((*pMatch_len = match_len = probe_len) == max_match_len) return;
						c0 = d->m_dict[pos + match_len]; c1 = d->m_dict[pos + match_len - 1];
					}
				}
			}
		}
		void tdefl_record_literal(tdefl_compressor* d, uint8_t lit)
		{
			d->m_total_lz_bytes++;
			*d->m_pLZ_code_buf++ = lit;
			*d->m_pLZ_flags = (uint8_t)(*d->m_pLZ_flags >> 1); if (--d->m_num_flags_left == 0) { d->m_num_flags_left = 8; d->m_pLZ_flags = d->m_pLZ_code_buf++; }
			d->m_huff_count[0][lit]++;
		}
		void tdefl_record_match(tdefl_compressor* d, mz_uint match_len, mz_uint match_dist)
		{
			uint32_t s0, s1;

			mz_assert((match_len >= TDEFL_MIN_MATCH_LEN) && (match_dist >= 1) && (match_dist <= TDEFL_LZ_DICT_SIZE));

			d->m_total_lz_bytes += match_len;

			d->m_pLZ_code_buf[0] = (uint8_t)(match_len - TDEFL_MIN_MATCH_LEN);

			match_dist -= 1;
			d->m_pLZ_code_buf[1] = (uint8_t)(match_dist & 0xFF);
			d->m_pLZ_code_buf[2] = (uint8_t)(match_dist >> 8); d->m_pLZ_code_buf += 3;

			*d->m_pLZ_flags = (uint8_t)((*d->m_pLZ_flags >> 1) | 0x80); if (--d->m_num_flags_left == 0) { d->m_num_flags_left = 8; d->m_pLZ_flags = d->m_pLZ_code_buf++; }

			s0 = s_tdefl_small_dist_sym[match_dist & 511]; s1 = s_tdefl_large_dist_sym[(match_dist >> 8) & 127];
			d->m_huff_count[1][(match_dist < 512) ? s0 : s1]++;

			if (match_len >= TDEFL_MIN_MATCH_LEN) d->m_huff_count[0][s_tdefl_len_sym[match_len - TDEFL_MIN_MATCH_LEN]]++;
		}
		tdefl_status tdefl_flush_output_buffer(tdefl_compressor* d)
		{
			if (d->m_pIn_buf_size)
			{
				*d->m_pIn_buf_size = d->m_pSrc - (const uint8_t*)d->m_pIn_buf;
			}

			if (d->m_pOut_buf_size)
			{
				auto n = std::min<mz_uint>(static_cast<mz_uint>(*d->m_pOut_buf_size - d->m_out_buf_ofs), d->m_output_flush_remaining);
				memcpy((uint8_t*)d->m_pOut_buf + d->m_out_buf_ofs, d->m_output_buf + d->m_output_flush_ofs, n);
				d->m_output_flush_ofs += (mz_uint)n;
				d->m_output_flush_remaining -= (mz_uint)n;
				d->m_out_buf_ofs += n;

				*d->m_pOut_buf_size = d->m_out_buf_ofs;
			}

			return (d->m_finished && !d->m_output_flush_remaining) ? TDEFL_STATUS_DONE : TDEFL_STATUS_OKAY;
		}
		static mz_bool tdefl_output_buffer_putter(const void* pBuf, int len, void* pUser)
		{
			tdefl_output_buffer* p = (tdefl_output_buffer*)pUser;
			size_t new_size = p->m_size + len;
			if (new_size > p->m_capacity)
			{
				size_t new_capacity = p->m_capacity; uint8_t* pNew_buf; if (!p->m_expandable) return MZ_FALSE;
				do { new_capacity = std::max<size_t>(128U, new_capacity << 1U); }
				while (new_size > new_capacity);
				pNew_buf = (uint8_t*)mz_realloc(p->m_pBuf, new_capacity); if (!pNew_buf) return MZ_FALSE;
				p->m_pBuf = pNew_buf; p->m_capacity = new_capacity;
			}
			memcpy((uint8_t*)p->m_pBuf + p->m_size, pBuf, len); p->m_size = new_size;
			return MZ_TRUE;
		}

		#undef TDEFL_READ_UNALIGNED_WORD

	private:
		// Adler32() returns the initial adler-32 value to use when called with ptr==nullptr.
		mz_ulong Adler32(mz_ulong adler, const unsigned char *ptr, size_t buf_len)
		{
			mz_ulong const MZ_ADLER32_INIT = 1;
			if (!ptr)
				return MZ_ADLER32_INIT;

			auto s1 = static_cast<uint32_t>((adler      ) & 0xffff);
			auto s2 = static_cast<uint32_t>((adler >> 16) & 0xffff);
			auto block_len = static_cast<size_t>(buf_len % 5552);
			while (buf_len)
			{
				uint32_t i;
				for (i = 0; i + 7 < block_len; i += 8, ptr += 8)
				{
					s1 += ptr[0], s2 += s1;
					s1 += ptr[1], s2 += s1;
					s1 += ptr[2], s2 += s1;
					s1 += ptr[3], s2 += s1;
					s1 += ptr[4], s2 += s1;
					s1 += ptr[5], s2 += s1;
					s1 += ptr[6], s2 += s1;
					s1 += ptr[7], s2 += s1;
				}
				for (; i < block_len; ++i)
				{
					s1 += *ptr++, s2 += s1;
				}
				s1 %= 65521U, s2 %= 65521U;
				buf_len -= block_len;
				block_len = 5552;
			}
			return (s2 << 16) + s1;
		}



		// Purposely making these tables static for faster init and thread safety.
		inline static const uint16_t s_tdefl_len_sym[256] =
		{
			257, 258, 259, 260, 261, 262, 263, 264, 265, 265, 266, 266, 267, 267, 268, 268, 269, 269, 269, 269, 270, 270, 270, 270, 271, 271, 271, 271, 272, 272, 272, 272,
			273, 273, 273, 273, 273, 273, 273, 273, 274, 274, 274, 274, 274, 274, 274, 274, 275, 275, 275, 275, 275, 275, 275, 275, 276, 276, 276, 276, 276, 276, 276, 276,
			277, 277, 277, 277, 277, 277, 277, 277, 277, 277, 277, 277, 277, 277, 277, 277, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278, 278,
			279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 279, 280, 280, 280, 280, 280, 280, 280, 280, 280, 280, 280, 280, 280, 280, 280, 280,
			281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281, 281,
			282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282, 282,
			283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283, 283,
			284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 284, 285,
		};
		inline static const uint8_t s_tdefl_len_extra[256] =
		{
			0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
			4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 0,
		};
		inline static const uint8_t s_tdefl_small_dist_sym[512] =
		{
			0, 1, 2, 3, 4, 4, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11,
			11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13,
			13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
			14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
			14, 14, 14, 14, 14, 14, 14, 14, 14, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
			15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17,
			17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17,
			17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17,
			17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17, 17,
		};
		inline static const uint8_t s_tdefl_small_dist_extra[512] =
		{
			0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5,
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
			6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
			6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
			7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
			7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
			7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
			7, 7, 7, 7, 7, 7, 7, 7,
		};
		inline static const uint8_t s_tdefl_large_dist_sym[128] =
		{
			0, 0, 18, 19, 20, 20, 21, 21, 22, 22, 22, 22, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24, 25, 25, 25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
			26, 26, 26, 26, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
			28, 28, 28, 28, 28, 28, 28, 28, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
		};
		inline static const uint8_t s_tdefl_large_dist_extra[128] =
		{
			0, 0, 8, 8, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
			12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
			13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
		};
		inline static const mz_uint mz_bitmasks[17] =
		{
			0x0000, 0x0001, 0x0003, 0x0007, 0x000F, 0x001F, 0x003F, 0x007F, 0x00FF, 0x01FF, 0x03FF, 0x07FF, 0x0FFF, 0x1FFF, 0x3FFF, 0x7FFF, 0xFFFF
		};
		#endif

	private:

		// Validate an archive item name
		static bool ValidateItemName(std::string_view item_name)
		{
			// Valid names cannot start with a forward slash, cannot contain a drive letter, and cannot use DOS-style backward slashes.
			if (item_name.empty())
				return false;
			if (item_name.size() > 0xFFFF)
				return false;
			if (item_name[0] == '/')
				return false;
			for (auto c : item_name)
				if (c == '\\' || c == ':')
					return false;
			return true;
		}

		// Validate an archive item comment
		static bool ValidateItemComment(std::string_view item_comment)
		{
			if (item_comment.size() > 0xFFFF)
				return false;
			return true;
		}

		// Convert a time to a local time
		static bool LocalTime(time_t time, tm& tm)
		{
			#ifdef _MSC_VER
			return localtime_s(&tm, &time) == 0;
			#else
			tm = *localtime(&time);
			return true;
			#endif
		}
		static time_t DosTimeToTime(int dos_time, int dos_date)
		{
			struct tm tm = {};
			tm.tm_isdst = -1;
			tm.tm_year = ((dos_date >> 9) & 127) + 1980 - 1900;
			tm.tm_mon = ((dos_date >> 5) & 15) - 1;
			tm.tm_mday = dos_date & 31;
			tm.tm_hour = (dos_time >> 11) & 31;
			tm.tm_min = (dos_time >> 5) & 63;
			tm.tm_sec = (dos_time << 1) & 62;
			return mktime(&tm);
		}
		static void TimeToDosTime(time_t time, uint16_t& dos_time, uint16_t& dos_date)
		{
			struct tm tm;
			if (LocalTime(time, tm))
			{
				dos_time = static_cast<uint16_t>((tm.tm_hour << 11) + (tm.tm_min << 5) + (tm.tm_sec >> 1));
				dos_date = static_cast<uint16_t>(((tm.tm_year + 1900 - 1980) << 9) + ((tm.tm_mon + 1) << 5) + tm.tm_mday);
			}
			else
			{
				dos_date = 0;
				dos_time = 0;
			}
		}
		static void FileTimeToDosTime(std::filesystem::path filepath, uint16_t& dos_time, uint16_t& dos_date)
		{
			auto ftime = std::filesystem::last_write_time(filepath);
			auto time = decltype(ftime)::clock::to_time_t(ftime);
			TimeToDosTime(time, dos_time, dos_date);
		}

		// Accumulate the crc of given data.
		static uint32_t Crc(void const* ptr, size_t buf_len, uint32_t crc = 0)
		{
			// Karl Malbrain's compact CRC-32. See "A compact CCITT crc16 and crc32 C implementation that balances
			// processor cache usage against speed": http://www.geocities.com/malbrain/
			static const uint32_t s_crc32[16] =
			{
				0x00000000, 0x1db71064, 0x3b6e20c8, 0x26d930ac, 0x76dc4190, 0x6b6b51f4, 0x4db26158, 0x5005713c,
				0xedb88320, 0xf00f9344, 0xd6d6a3e8, 0xcb61b38c, 0x9b64c2b0, 0x86d3d2d4, 0xa00ae278, 0xbdbdf21c,
			};

			if (!ptr) return crc;
			auto p = static_cast<uint8_t const*>(ptr);

			crc = ~crc;
			while (buf_len--)
			{
				auto b = *p++;
				crc = (crc >> 4) ^ s_crc32[(crc & 0xF) ^ (b & 0xF)];
				crc = (crc >> 4) ^ s_crc32[(crc & 0xF) ^ (b >> 4)];
			}
			return ~crc;
		}

		// CompileTime accumulative hash 
		static uint64_t const FNV_offset_basis64 = 14695981039346656037ULL;
		static uint64_t const FNV_prime64        = 1099511628211ULL;
		static constexpr uint64_t Hash64CT(uint64_t ch, uint64_t h)
		{
			return Mul64(h ^ ch, FNV_prime64);
		}
		static constexpr uint64_t Mul64(uint64_t a, uint64_t b)
		{
			// 64 bit multiply without a warning...
			auto ffffffff = uint32_t(-1);
			auto ab = Lo32(a) * Lo32(b);
			auto aB = Lo32(a) * Hi32(b);
			auto Ab = Hi32(a) * Lo32(b);
			auto hi = ((((Hi32(ab) + aB) & ffffffff) + Ab) & ffffffff) << 32;
			auto lo = ab & ffffffff;
			return hi + lo;
		}
		static constexpr uint64_t Hi32(uint64_t x) { return (x >> 32) & uint32_t(-1); }
		static constexpr uint64_t Lo32(uint64_t x) { return (x      ) & uint32_t(-1); }
		static_assert(Mul64(0x1234567887654321, 0x1234567887654321) == 0x290D0FCAD7A44A41, "Compile time multiply failed");

		// Helper for detecting data lost when casting
		template <typename T, typename U> T checked_cast(U x)
		{
			assert("Cast loses data" && static_cast<U>(static_cast<T>(x)) == x);
			return static_cast<T>(x);
		}

		// True if 'ofs' is an aligned offset in the output stream
		template <typename T> bool is_aligned(int64_t ofs) const
		{
			if (m_entry_alignment == 0) return true;
			return (ofs & (m_entry_alignment - 1)) == 0;
		}

		// True if (value & flags) == flags;
		template <typename T> static constexpr bool has_flag(T value, T flags)
		{
			if constexpr (std::is_enum_v<T>)
			{
				using ut = typename std::underlying_type<T>::type;
				return (ut(value) & ut(flags)) == ut(flags);
			}
			else
				return (value & flags) == flags;
		}

		// Less operator for name_hash_index_pair_t (used when searching 'm_central_dir_lookup')
		friend constexpr bool operator < (name_hash_index_pair_t const& lhs, name_hash_index_pair_t const& rhs)
		{
			return lhs.name_hash < rhs.name_hash;
		}
		friend constexpr bool operator < (name_hash_index_pair_t const& lhs, uint64_t rhs)
		{
			return lhs.name_hash < rhs;
		}
		friend constexpr bool operator < (uint64_t lhs, name_hash_index_pair_t const& rhs)
		{
			return lhs < rhs.name_hash;
		}

		// Casting helper
		union variant_cptr_t
		{
			void const* vp;
			uint32_t const* u32;
			uint16_t const* u16;
			uint8_t const* u8;
			variant_cptr_t(void const* x) :vp(x) {}
		};
		union variant_mptr_t
		{
			void* vp;
			uint32_t* u32;
			uint16_t* u16;
			uint8_t* u8;
			variant_mptr_t(void* x) :vp(x) {}
		};

		//// Read / Write helper functions
		//static uint16_t ReadLE16(variant_cptr_t ptr)
		//{
		//	if constexpr (LittleEndian && UnalignedLoadStore)
		//		return *ptr.u16;
		//	else
		//		return static_cast<uint16_t>((ptr.u8[1] << 8U) | (ptr.u8[0]));
		//}
		//static uint32_t ReadLE32(variant_cptr_t ptr)
		//{
		//	if constexpr (LittleEndian && UnalignedLoadStore)
		//		return *ptr.u32;
		//	else
		//		return static_cast<uint32_t>((ptr.u8[3] << 24U) | (ptr.u8[2] << 16U) | (ptr.u8[1] << 8U) | (ptr.u8[0]));
		//}
		//static void WriteLE16(variant_mptr_t ptr, uint64_t v)
		//{
		//	assert(v <= 0xFFFF);
		//	if constexpr (LittleEndian && UnalignedLoadStore)
		//	{
		//		ptr.u16[0] = static_cast<uint16_t>(v);
		//	}
		//	else
		//	{
		//		ptr.u8[0] = static_cast<uint8_t>((v >> 0) & 0xFF);
		//		ptr.u8[1] = static_cast<uint8_t>((v >> 8) & 0xFF);
		//	}
		//}
		//static void WriteLE32(variant_mptr_t ptr, uint64_t v)
		//{
		//	assert(v <= 0xFFFFFFFF);
		//	if constexpr (LittleEndian && UnalignedLoadStore)
		//	{
		//		ptr.u32[0] = static_cast<uint32_t>(v);
		//	}
		//	else
		//	{
		//		ptr.u8[0] = static_cast<uint8_t>((v >>  0) & 0xFF);
		//		ptr.u8[1] = static_cast<uint8_t>((v >>  8) & 0xFF);
		//		ptr.u8[2] = static_cast<uint8_t>((v >> 16) & 0xFF);
		//		ptr.u8[3] = static_cast<uint8_t>((v >> 24) & 0xFF);
		//	}
		//}
	};
	using ZipArchive = ZipArchiveA<std::allocator<void>>;
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/memstream.h"

namespace pr::storage
{
	PRUnitTest(ZipArchiveTests)
	{
		auto path = (std::filesystem::path(__FILE__).parent_path() / ".." / ".." / ".." / "projects" / "unittest_resources").lexically_normal();

		// Read a test zip file
		{
			zip::ZipArchive z(path / "binary-00-0F.zip", zip::ZipArchive::EZipFlags::FastNameLookup);
			PR_CHECK(z.Count(), 1);
			PR_CHECK(z.Name(0), "binary-00-0F.bin");
			PR_CHECK(z.IndexOf("binary-00-0F.bin"), 0);
			
			std::basic_string<uint8_t> bytes;
			pr::mem_ostream out(bytes);
			z.Extract("binary-00-0F.bin", out);

			std::basic_ifstream<uint8_t> ifile(path / "binary-00-0F.bin");
			std::basic_stringstream<uint8_t> file_bytes;
			file_bytes << ifile.rdbuf();

			PR_CHECK(bytes == file_bytes.str(), true);
		}

		//{// Write a zip file
		//	zip::ZipArchive z;
		//	z.Write(path / "file1.txt");
		//	z.Write(path / "file2.txt");
		//	z.Save(path / "zip_out.zip");
		//}

		//{// Read the zip file
		//	zip::ZipArchive z(path / "zip_out.zip");
		//	auto names = z.NameList();
		//	PR_CHECK(names[0], "file1.txt");
		//	PR_CHECK(names[1], "file2.txt");
		//	auto file1 = z.Read("file1.txt");
		//	auto file2 = z.Read("file2.txt");
		//	PR_CHECK(file1, "This is file1");
		//	PR_CHECK(file2, "This is file2");
		//	std::filesystem::remove(z.Filepath());
		//}
		
		//{// Read a zip file created using a different tool
		//	zip::ZipArchive z(path / "zip_file.zip");
		//	auto names = z.NameList();
		//	PR_CHECK(names[0], "file1.txt");
		//	PR_CHECK(names[1], "file2.txt");
		//	auto file1 = z.Read("file1.txt");
		//	auto file2 = z.Read("file2.txt");
		//	PR_CHECK(file1, "This is file1");
		//	PR_CHECK(file2, "This is file2");
		//}
	}
}
#endif




#if 0
struct ZipInfo
{
	std::string filename;

	struct
	{
		int year = 1980;
		int month = 0;
		int day = 0;
		int hours = 0;
		int minutes = 0;
		int seconds = 0;
	} date_time;

	std::string comment;
	std::string extra;
	uint16_t create_system = 0;
	uint16_t create_version = 0;
	uint16_t extract_version = 0;
	uint16_t flag_bits = 0;
	std::size_t volume = 0;
	uint32_t internal_attr = 0;
	uint32_t external_attr = 0;
	std::size_t header_offset = 0;
	uint32_t crc = 0;
	std::size_t compress_size = 0;
	std::size_t file_size = 0;
};

class ZipFile
{
	using miniz = ZipArchive<>;
	std::unique_ptr<miniz::mz_zip_archive> mutable m_archive;
	std::filesystem::path m_filepath;
	std::stringstream m_open_stream;
	std::vector<char> m_buffer;
	std::string m_comment;
	miniz m;

public:
	enum class ECompression
	{
		None = miniz::MZ_NO_COMPRESSION,
		Fast = miniz::MZ_BEST_SPEED,
		Default = miniz::MZ_DEFAULT_LEVEL,
		Best = miniz::MZ_BEST_COMPRESSION,
		Uber = miniz::MZ_UBER_COMPRESSION, // Not zlib compatible
	};

	ZipFile()
		:m_archive(new miniz::mz_zip_archive())
		,m_filepath()
		,m_open_stream()
		,m_buffer()
		,m_comment()
		,m()
	{
		Reset();
	}
	ZipFile(std::filesystem::path const& filename)
		:ZipFile()
	{
		Load(filename);
	}
	ZipFile(std::istream& stream)
		:ZipFile()
	{
		Load(stream);
	}
	ZipFile(std::vector<unsigned char> const& bytes)
		:ZipFile()
	{
		Load(bytes);
	}
	~ZipFile()
	{
		Reset();
	}

	// The archive filepath
	std::filesystem::path Filepath() const
	{
		return m_filepath;
	}

	// Comment associated with the archive
	std::string Comment() const
	{
		return m_comment;
	}

	// Load a zip file into memory
	void Load(std::istream& stream)
	{
		Reset();
		m_buffer.assign(std::istreambuf_iterator<char>(stream), std::istreambuf_iterator<char>());
		RemoveComment();
		StartRead();
	}
	void Load(std::filesystem::path const& filepath)
	{
		if (!std::filesystem::exists(filepath))
			throw std::runtime_error(std::string().append("file '").append(filepath.string()).append("' not found"));

		m_filepath = filepath;

		std::ifstream stream(filepath, std::ios::binary);
		Load(stream);
	}
	void Load(std::vector<unsigned char> const& bytes)
	{
		Reset();
		m_buffer.assign(bytes.begin(), bytes.end());
		RemoveComment();
		StartRead();
	}

	// Save a zip file
	void Save(std::filesystem::path const& filepath)
	{
		m_filepath = filepath;

		std::ofstream stream(filepath, std::ios::binary);
		Save(stream);
	}
	void Save(std::ostream &stream)
	{
		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_WRITING)
			m.mz_zip_writer_finalize_archive(m_archive.get());

		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_WRITING_HAS_BEEN_FINALIZED)
			m.mz_zip_writer_end(m_archive.get());

		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_INVALID)
			StartRead();

		AppendComment();
		stream.write(m_buffer.data(), static_cast<long>(m_buffer.size()));
	}
	void Save(std::vector<unsigned char> &bytes)
	{
		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_WRITING)
			m.mz_zip_writer_finalize_archive(m_archive.get());

		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_WRITING_HAS_BEEN_FINALIZED)
			m.mz_zip_writer_end(m_archive.get());

		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_INVALID)
			StartRead();

		AppendComment();
		bytes.assign(m_buffer.begin(), m_buffer.end());
	}

	// True if the archive contains 'name'
	bool HasFile(std::string const& name)
	{
		if (m_archive->m_zip_mode != miniz::MZ_ZIP_MODE_READING)
			StartRead();

		auto index = m.IndexOf(m_archive.get(), name.c_str(), nullptr, 0);
		return index != -1;
	}
	bool HasFile(ZipInfo const& name)
	{
		return HasFile(name.filename);
	}

	// Read info about an item in the archive
	ZipInfo Info(std::string const& name)
	{
		if (m_archive->m_zip_mode != miniz::MZ_ZIP_MODE_READING)
			StartRead();

		int index = m.IndexOf(m_archive.get(), name.c_str(), nullptr, 0);
		if (index == -1) throw std::runtime_error("not found");
		return Info(index);
	}

	// Read info for each item in the archive
	std::vector<ZipInfo> InfoList()
	{
		if (m_archive->m_zip_mode != miniz::MZ_ZIP_MODE_READING)
			StartRead();

		std::vector<ZipInfo> info;
		for (std::size_t i = 0; i < m.mz_zip_reader_get_num_files(m_archive.get()); i++)
			info.push_back(Info(static_cast<int>(i)));

		return info;
	}

	// Return the names of the items in the archive
	std::vector<std::string> NameList()
	{
		std::vector<std::string> names;
		for (auto& info : InfoList())
			names.push_back(info.filename);

		return names;
	}

	// Open an item in the archive into the internal stream
	std::ostream& Open(std::string const& name)
	{
		return Open(Info(name));
	}
	std::ostream& Open(ZipInfo const& name)
	{
		auto data = Read(name);

		std::string data_string(data.begin(), data.end());
		m_open_stream << data_string;
		return m_open_stream;
	}

	// Extract an item to a file on disk
	void Extract(std::string const& member, std::filesystem::path path)
	{
		auto outpath = path.append(member);
		std::fstream stream(outpath, std::ios::binary | std::ios::out);
		stream << Open(member).rdbuf();
	}
	void Extract(ZipInfo const& member, std::filesystem::path path)
	{
		auto outpath = path.append(member.filename);
		std::fstream stream(outpath, std::ios::binary | std::ios::out);
		stream << Open(member).rdbuf();
	}
	void ExtractAll(std::filesystem::path const& path)
	{
		ExtractAll(path, InfoList());
	}
	void ExtractAll(std::filesystem::path const& path, std::vector<std::string> const& members)
	{
		for (auto& member : members)
			Extract(member, path);
	}
	void ExtractAll(std::filesystem::path const& path, std::vector<ZipInfo> const& members)
	{
		for (auto &member : members)
			Extract(member, path);
	}

	// Extract an item to memory
	std::string Read(std::string const& name)
	{
		return Read(Info(name));
	}
	std::string Read(ZipInfo const& info)
	{
		std::size_t size;
		auto data = static_cast<char*>(m.mz_zip_reader_extract_file_to_heap(m_archive.get(), info.filename.c_str(), &size, 0));
		if (data == nullptr)
			throw std::runtime_error("file couldn't be read");

		std::string extracted(data, data + size);
		m.mz_free(data);
		return extracted;
	}

	// Add an item to the archive
	void Write(std::filesystem::path const& filepath)
	{
		Write(filepath, filepath.filename().string());
	}
	void Write(std::filesystem::path const& filepath, std::string const& arcname)
	{
		if (!std::filesystem::exists(filepath))
			throw std::runtime_error(filepath.string() + " - file not found");

		std::fstream file(filepath, std::ios::binary | std::ios::in);
		std::stringstream ss; ss << file.rdbuf();
		std::string bytes = ss.str();
		WriteStr(arcname, bytes);
	}

	// Add an item from memory to the archive
	void WriteStr(std::string const& arcname, std::string const& bytes, ECompression compression = ECompression::Default)
	{
		if (m_archive->m_zip_mode != miniz::MZ_ZIP_MODE_WRITING)
			StartWrite();

		if (!m.mz_zip_writer_add_mem(m_archive.get(), arcname.c_str(), bytes.data(), bytes.size(), static_cast<unsigned int>(compression)))
			throw std::runtime_error("write error");
	}
	void WriteStr(ZipInfo const& info, std::string const& bytes, ECompression compression = ECompression::Default)
	{
		if (info.filename.empty() || info.date_time.year < 1980)
			throw std::runtime_error("must specify a filename and valid date (year >= 1980");

		if (m_archive->m_zip_mode != miniz::MZ_ZIP_MODE_WRITING)
			StartWrite();

		auto crc = crc32buf(bytes.c_str(), bytes.size());
		if (!m.mz_zip_writer_add_mem_ex(m_archive.get(), info.filename.c_str(), bytes.data(), bytes.size(), info.comment.c_str(), static_cast<unsigned short>(info.comment.size()), static_cast<unsigned int>(compression), 0, crc))
			throw std::runtime_error("write error");
	}

	// Print the directory structure in the archive
	void PrintDir()
	{
		PrintDir(std::cout);
	}
	void PrintDir(std::ostream& stream)
	{
		stream << "  Length " << "  " << "   " << "Date" << "   " << " " << "Time " << "   " << "Name" << std::endl;
		stream << "---------  ---------- -----   ----" << std::endl;

		std::size_t sum_length = 0;
		std::size_t file_count = 0;
		for (auto& member : InfoList())
		{
			sum_length += member.file_size;
			file_count++;

			auto length_string = std::to_string(member.file_size);
			while (length_string.length() < 9)
			{
				length_string = " " + length_string;
			}
			stream << length_string;

			stream << "  ";
			stream << (member.date_time.month < 10 ? "0" : "") << member.date_time.month;
			stream << "/";
			stream << (member.date_time.day < 10 ? "0" : "") << member.date_time.day;
			stream << "/";
			stream << member.date_time.year;
			stream << " ";
			stream << (member.date_time.hours < 10 ? "0" : "") << member.date_time.hours;
			stream << ":";
			stream << (member.date_time.minutes < 10 ? "0" : "") << member.date_time.minutes;
			stream << "   ";
			stream << member.filename;
			stream << std::endl;
		}

		stream << "---------                     -------" << std::endl;

		auto length_string = std::to_string(sum_length);
		while (length_string.length() < 9)
		{
			length_string = " " + length_string;
		}
		stream << length_string << "                     " << file_count << " " << (file_count == 1 ? "file" : "files");
		stream << std::endl;
	}

	std::pair<bool, std::string> TestZip()
	{
		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_INVALID)
			throw std::runtime_error("not open");

		for (auto &file : InfoList())
		{
			auto content = Read(file);
			auto crc = crc32buf(content.c_str(), content.size());

			if (crc != file.crc)
			{
				return { false, file.filename };
			}
		}

		return { true, "" };
	}

private:

	// Reset the miniz archive structure
	void Reset()
	{
		switch (m_archive->m_zip_mode)
		{
		case miniz::MZ_ZIP_MODE_READING:
			m.mz_zip_reader_end(m_archive.get());
			break;
		case miniz::MZ_ZIP_MODE_WRITING:
			m.mz_zip_writer_finalize_archive(m_archive.get());
			m.mz_zip_writer_end(m_archive.get());
			break;
		case miniz::MZ_ZIP_MODE_WRITING_HAS_BEEN_FINALIZED:
			m.mz_zip_writer_end(m_archive.get());
			break;
		case miniz::MZ_ZIP_MODE_INVALID:
			break;
		}
		if (m_archive->m_zip_mode != miniz::MZ_ZIP_MODE_INVALID)
			throw std::runtime_error("Failed to reset the archive");

		m_buffer.clear();
		m_comment.clear();

		StartWrite();
		m.mz_zip_writer_finalize_archive(m_archive.get());
		m.mz_zip_writer_end(m_archive.get());
	}

	// Prepare the archive for reading
	void StartRead()
	{
		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_READING)
			return;

		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_WRITING)
			m.mz_zip_writer_finalize_archive(m_archive.get());

		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_WRITING_HAS_BEEN_FINALIZED)
			m.mz_zip_writer_end(m_archive.get());

		if (!m.mz_zip_reader_init_mem(m_archive.get(), m_buffer.data(), m_buffer.size(), 0))
			throw std::runtime_error("bad zip");
	}

	// Prepare the archive for reading
	void StartWrite()
	{
		if (m_archive->m_zip_mode == miniz::MZ_ZIP_MODE_WRITING)
			return;

		auto write_callback = [](void* opaque, std::uint64_t file_ofs, void const* pBuf, std::size_t n)
		{
			auto buffer = static_cast<std::vector<char>*>(opaque);
			if (file_ofs + n > buffer->size())
			{
				auto new_size = static_cast<std::vector<char>::size_type>(file_ofs + n);
				buffer->resize(new_size);
			}

			for (std::size_t i = 0; i < n; i++)
			{
				(*buffer)[static_cast<std::size_t>(file_ofs + i)] = (static_cast<const char *>(pBuf))[i];
			}

			return n;
		};

		switch (m_archive->m_zip_mode)
		{
		case miniz::MZ_ZIP_MODE_READING:
			{
				miniz::mz_zip_archive archive_copy;
				std::memset(&archive_copy, 0, sizeof(miniz::mz_zip_archive));
				std::vector<char> buffer_copy(m_buffer.begin(), m_buffer.end());

				if (!m.mz_zip_reader_init_mem(&archive_copy, buffer_copy.data(), buffer_copy.size(), 0))
					throw std::runtime_error("bad zip");

				m.mz_zip_reader_end(m_archive.get());

				m_archive->m_pWrite = write_callback;
				m_archive->m_pIO_opaque = &m_buffer;
				m_buffer = std::vector<char>();

				if (!m.mz_zip_writer_init(m_archive.get(), 0))
				{
					throw std::runtime_error("bad zip");
				}

				for (unsigned int i = 0; i < static_cast<unsigned int>(archive_copy.m_total_entries); i++)
				{
					if (!m.mz_zip_writer_add_from_zip_reader(m_archive.get(), &archive_copy, i))
					{
						throw std::runtime_error("fail");
					}
				}

				m.mz_zip_reader_end(&archive_copy);
				return;
			}
		case miniz::MZ_ZIP_MODE_WRITING_HAS_BEEN_FINALIZED:
			m.mz_zip_writer_end(m_archive.get());
			break;
		case miniz::MZ_ZIP_MODE_INVALID:
		case miniz::MZ_ZIP_MODE_WRITING:
			break;
		}


		m_archive->m_pWrite = write_callback;
		m_archive->m_pIO_opaque = &m_buffer;

		if (!m.mz_zip_writer_init(m_archive.get(), 0))
			throw std::runtime_error("bad zip");
	}

	//
	void AppendComment()
	{
		if (!m_comment.empty())
		{
			auto comment_length = std::min(static_cast<uint16_t>(m_comment.length()), std::numeric_limits<uint16_t>::max());
			m_buffer[m_buffer.size() - 2] = static_cast<char>(comment_length);
			m_buffer[m_buffer.size() - 1] = static_cast<char>(comment_length >> 8);
			std::copy(m_comment.begin(), m_comment.end(), std::back_inserter(m_buffer));
		}
	}

	//
	void RemoveComment()
	{
		if (m_buffer.empty())
			return;

		auto position = m_buffer.size() - 1;
		for (; position >= 3; position--)
		{
			if (m_buffer[position - 3] == 'P' &&
				m_buffer[position - 2] == 'K' &&
				m_buffer[position - 1] == '\x05' &&
				m_buffer[position - 0] == '\x06')
			{
				position = position + 17;
				break;
			}
		}
		if (position == 3)
			throw std::runtime_error("didn't find end of central directory signature");

		auto length = static_cast<uint16_t>(m_buffer[position + 1]);
		length = static_cast<uint16_t>(length << 8) + static_cast<uint16_t>(m_buffer[position]);
		position += 2;

		if (length != 0)
		{
			m_comment = std::string(m_buffer.data() + position, m_buffer.data() + position + length);
			m_buffer.resize(m_buffer.size() - length);
			m_buffer[m_buffer.size() - 1] = 0;
			m_buffer[m_buffer.size() - 2] = 0;
		}
	}

	// Read file info for the file at position 'index' in the archive
	ZipInfo Info(int index)
	{
		if (m_archive->m_zip_mode != miniz::MZ_ZIP_MODE_READING)
			StartRead();

		miniz::ZipItemStat stat;
		m.ItemStat(m_archive.get(), static_cast<unsigned int>(index), &stat);

		tm time;
		if (!m.LocalTime(stat.m_time, time))
			throw std::runtime_error("");

		ZipInfo result = {};
		result.filename = std::string(stat.m_filename, stat.m_filename + std::strlen(stat.m_filename));
		result.comment = std::string(stat.m_comment, stat.m_comment + stat.m_comment_size);
		result.compress_size = static_cast<std::size_t>(stat.m_comp_size);
		result.file_size = static_cast<std::size_t>(stat.m_uncomp_size);
		result.header_offset = static_cast<std::size_t>(stat.m_local_header_ofs);
		result.crc = stat.m_crc32;
		result.date_time.year = 1900 + time.tm_year;
		result.date_time.month = 1 + time.tm_mon;
		result.date_time.day = time.tm_mday;
		result.date_time.hours = time.tm_hour;
		result.date_time.minutes = time.tm_min;
		result.date_time.seconds = time.tm_sec;
		result.flag_bits = stat.m_bit_flag;
		result.internal_attr = stat.m_internal_attr;
		result.external_attr = stat.m_external_attr;
		result.extract_version = stat.m_version_needed;
		result.create_version = stat.m_version_made_by;
		result.volume = stat.m_file_index;
		result.create_system = stat.m_method;
		return result;
	}

	//
	static uint32_t crc32buf(const char *buf, std::size_t len, uint32_t crc = 0xFFFFFFFF)
	{
		/* CRC polynomial 0xedb88320 */
		uint32_t crc_32_tab[] =
		{

			0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419, 0x706af48f,
			0xe963a535, 0x9e6495a3, 0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
			0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91, 0x1db71064, 0x6ab020f2,
			0xf3b97148, 0x84be41de, 0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
			0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9,
			0xfa0f3d63, 0x8d080df5, 0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
			0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b, 0x35b5a8fa, 0x42b2986c,
			0xdbbbc9d6, 0xacbcf940, 0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
			0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423,
			0xcfba9599, 0xb8bda50f, 0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
			0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d, 0x76dc4190, 0x01db7106,
			0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
			0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818, 0x7f6a0dbb, 0x086d3d2d,
			0x91646c97, 0xe6635c01, 0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
			0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457, 0x65b0d9c6, 0x12b7e950,
			0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
			0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2, 0x4adfa541, 0x3dd895d7,
			0xa4d1c46d, 0xd3d6f4fb, 0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
			0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9, 0x5005713c, 0x270241aa,
			0xbe0b1010, 0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
			0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17, 0x2eb40d81,
			0xb7bd5c3b, 0xc0ba6cad, 0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
			0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683, 0xe3630b12, 0x94643b84,
			0x0d6d6a3e, 0x7a6a5aa8, 0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
			0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb,
			0x196c3671, 0x6e6b06e7, 0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
			0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5, 0xd6d6a3e8, 0xa1d1937e,
			0x38d8c2c4, 0x4fdff252, 0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
			0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55,
			0x316e8eef, 0x4669be79, 0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
			0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f, 0xc5ba3bbe, 0xb2bd0b28,
			0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
			0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a, 0x9c0906a9, 0xeb0e363f,
			0x72076785, 0x05005713, 0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
			0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21, 0x86d3d2d4, 0xf1d4e242,
			0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
			0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c, 0x8f659eff, 0xf862ae69,
			0x616bffd3, 0x166ccf45, 0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
			0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db, 0xaed16a4a, 0xd9d65adc,
			0x40df0b66, 0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
			0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605, 0xcdd70693,
			0x54de5729, 0x23d967bf, 0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
			0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
		};

		for (; len; --len, ++buf)
			crc = crc_32_tab[(crc ^ static_cast<uint8_t>(*buf)) & 0xff] ^ (crc >> 8);

		return ~crc;
	}
};
#endif

// Copyright (c) 2014-2017 Thomas Fussell
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, WRISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE

// @license: http://www.opensource.org/licenses/mit-license.php
/*
   miniz.c v1.15 - public domain deflate/inflate, zlib-subset, ZIP reading/writing/appending, PNG writing
   See "unlicense" statement at the end of this file.
   Rich Geldreich <richgel99@gmail.com>, last updated Oct. 13, 2013
   Implements RFC 1950: http://www.ietf.org/rfc/rfc1950.txt and RFC 1951: http://www.ietf.org/rfc/rfc1951.txt

   Most API's defined in miniz.c are optional. For example, to disable the archive related functions just define
   MINIZ_NO_ARCHIVE_APIS, or to get rid of all stdio usage define MINIZ_NO_STDIO (see the list below for more macros).

   * Change History
	 10/13/13 v1.15 r4 - Interim bugfix release while I work on the next major release with Zip64 support (almost there!):
	   - Critical fix for the MZ_ZIP_FLAG_DO_NOT_SORT_CENTRAL_DIRECTORY bug (thanks kahmyong.moon@hp.com) which could cause locate files to not find files. This bug
		would only have occured in earlier versions if you explicitly used this flag, OR if you used mz_zip_extract_archive_file_to_heap() or mz_zip_add_mem_to_archive_file_in_place()
		(which used this flag). If you can't switch to v1.15 but want to fix this bug, just remove the uses of this flag from both helper funcs (and of course don't use the flag).
	   - Bugfix in mz_zip_reader_extract_to_mem_no_alloc() from kymoon when pUser_read_buf is not nullptr and compressed size is > uncompressed size
	   - Fixing mz_zip_reader_extract_*() funcs so they don't try to extract compressed data from directory entries, to account for weird zipfiles which contain zero-size compressed data on dir entries.
		 Hopefully this fix won't cause any issues on weird zip archives, because it assumes the low 16-bits of zip external attributes are DOS attributes (which I believe they always are in practice).
	   - Fixing ItemIsDirectory() so it doesn't check the internal attributes, just the filename and external attributes
	   - mz_zip_reader_init_file() - missing MZ_FCLOSE() call if the seek failed
	   - Added cmake support for Linux builds which builds all the examples, tested with clang v3.3 and gcc v4.6.
	   - Clang fix for tdefl_write_image_to_png_file_in_memory() from toffaletti
	   - Merged MZ_FORCEINLINE fix from hdeanclark
	   - Fix <time.h> include before config #ifdef, thanks emil.brink
	   - Added tdefl_write_image_to_png_file_in_memory_ex(): supports Y flipping (super useful for OpenGL apps), and explicit control over the compression level (so you can
		set it to 1 for real-time compression).
	   - Merged in some compiler fixes from paulharris's github repro.
	   - Retested this build under Windows (VS 2010, including static analysis), tcc  0.9.26, gcc v4.6 and clang v3.3.
	   - Added example6.c, which dumps an image of the mandelbrot set to a PNG file.
	   - Modified example2 to help test the MZ_ZIP_FLAG_DO_NOT_SORT_CENTRAL_DIRECTORY flag more.
	   - In r3: Bugfix to mz_zip_writer_add_file() found during merge: Fix possible src file fclose() leak if alignment bytes+local header file write faiiled
	   - In r4: Minor bugfix to mz_zip_writer_add_from_zip_reader(): Was pushing the wrong central dir header offset, appears harmless in this release, but it became a problem in the zip64 branch
	 5/20/12 v1.14 - MinGW32/64 GCC 4.6.1 compiler fixes: added MZ_FORCEINLINE, #include <time.h> (thanks fermtect).
	 5/19/12 v1.13 - From jason@cornsyrup.org and kelwert@mtu.edu - Fix mz_crc32() so it doesn't compute the wrong CRC-32's when mz_ulong is 64-bit.
	   - Temporarily/locally slammed in "typedef unsigned long mz_ulong" and re-ran a randomized regression test on ~500k files.
	   - Eliminated a bunch of warnings when compiling with GCC 32-bit/64.
	   - Ran all examples, miniz.c, and tinfl.c through MSVC 2008's /analyze (static analysis) option and fixed all warnings (except for the silly
		"Use of the comma-operator in a tested expression.." analysis warning, which I purposely use to work around a MSVC compiler warning).
	   - Created 32-bit and 64-bit Codeblocks projects/workspace. Built and tested Linux executables. The codeblocks workspace is compatible with Linux+Win32/x64.
	   - Added miniz_tester solution/project, which is a useful little app derived from LZHAM's tester app that I use as part of the regression test.
	   - Ran miniz.c and tinfl.c through another series of regression testing on ~500,000 files and archives.
	   - Modified example5.c so it purposely disables a bunch of high-level functionality (MINIZ_NO_STDIO, etc.). (Thanks to corysama for the MINIZ_NO_STDIO bug report.)
	   - Fix ftell() usage in examples so they exit with an error on files which are too large (a limitation of the examples, not miniz itself).
	 4/12/12 v1.12 - More comments, added low-level example5.c, fixed a couple minor level_and_flags issues in the archive API's.
	  level_and_flags can now be set to MZ_DEFAULT_COMPRESSION. Thanks to Bruce Dawson <bruced@valvesoftware.com> for the feedback/bug report.
	 5/28/11 v1.11 - Added statement from unlicense.org
	 5/27/11 v1.10 - Substantial compressor optimizations:
	  - Level 1 is now ~4x faster than before. The L1 compressor's throughput now varies between 70-110MB/sec. on a
	  - Core i7 (actual throughput varies depending on the type of data, and x64 vs. x86).
	  - Improved baseline L2-L9 compression perf. Also, greatly improved compression perf. issues on some file types.
	  - Refactored the compression code for better readability and maintainability.
	  - Added level 10 compression level (L10 has slightly better ratio than level 9, but could have a potentially large
	   drop in throughput on some files).
	 5/15/11 v1.09 - Initial stable release.

   * Low-level Deflate/Inflate implementation notes:

	 Compression: Use the "tdefl" API's. The compressor supports raw, static, and dynamic blocks, lazy or
	 greedy parsing, match length filtering, RLE-only, and Huffman-only streams. It performs and compresses
	 approximately as well as zlib.

	 Decompression: Use the "tinfl" API's. The entire decompressor is implemented as a single function
	 coroutine: see Decompress(). It supports decompression into a 32KB (or larger power of 2) wrapping buffer, or into a memory
	 block large enough to hold the entire file.

	 The low-level tdefl/tinfl API's do not make any use of dynamic memory allocation.

   * zlib-style API notes:

	 miniz.c implements a fairly large subset of zlib. There's enough functionality present for it to be a drop-in
	 zlib replacement in many apps:
		The z_stream struct, optional memory allocation callbacks
		deflateInit/deflateInit2/deflate/deflateReset/deflateEnd/deflateBound
		inflateInit/inflateInit2/inflate/inflateEnd
		compress, compress2, compressBound, uncompress
		CRC-32, Adler-32 - Using modern, minimal code size, CPU cache friendly routines.
		Supports raw deflate streams or standard zlib streams with adler-32 checking.

	 Limitations:
	  The callback API's are not implemented yet. No support for gzip headers or zlib static dictionaries.
	  I've tried to closely emulate zlib's various flavors of stream flushing and return status codes, but
	  there are no guarantees that miniz.c pulls this off perfectly.

   * PNG writing: See the tdefl_write_image_to_png_file_in_memory() function, originally written by
	 Alex Evans. Supports 1-4 bytes/pixel images.

   * ZIP archive API notes:

	 The ZIP archive API's where designed with simplicity and efficiency in mind, with just enough abstraction to
	 get the job done with minimal fuss. There are simple API's to retrieve file information, read files from
	 existing archives, create new archives, append new files to existing archives, or clone archive data from
	 one archive to another. It supports archives located in memory or the heap, on disk (using stdio.h),
	 or you can specify custom file read/write callbacks.

	 - Archive reading: Just call this function to read a single file from a disk archive:

	  void *mz_zip_extract_archive_file_to_heap(const char *pZip_filename, const char *pArchive_name,
		size_t *pSize, mz_uint zip_flags);

	 For more complex cases, use the "mz_zip_reader" functions. Upon opening an archive, the entire central
	 directory is located and read as-is into memory, and subsequent file access only occurs when reading individual files.

	 - Archives file scanning: The simple way is to use this function to scan a loaded archive for a specific file:

	 int IndexOf(mz_zip_archive *pZip, const char *pName, const char *pComment, mz_uint flags);

	 The locate operation can optionally check file comments too, which (as one example) can be used to identify
	 multiple versions of the same file in an archive. This function uses a simple linear search through the central
	 directory, so it's not very fast.

	 Alternately, you can iterate through all the files in an archive (using mz_zip_reader_get_num_files()) and
	 retrieve detailed info on each file by calling ItemStat().

	 - Archive creation: Use the "mz_zip_writer" functions. The ZIP writer immediately writes compressed file data
	 to disk and builds an exact image of the central directory in memory. The central directory image is written
	 all at once at the end of the archive file when the archive is finalized.

	 The archive writer can optionally align each file's local header and file data to any power of 2 alignment,
	 which can be useful when the archive will be read from optical media. Also, the writer supports placing
	 arbitrary data blobs at the very beginning of ZIP archives. Archives written using either feature are still
	 readable by any ZIP tool.

	 - Archive appending: The simple way to add a single file to an archive is to call this function:

	  mz_bool mz_zip_add_mem_to_archive_file_in_place(const char *pZip_filename, const char *pArchive_name,
		const void *pBuf, size_t buf_size, const void *pComment, uint16_t comment_size, mz_uint level_and_flags);

	 The archive will be created if it doesn't already exist, otherwise it'll be appended to.
	 Note the appending is done in-place and is not an atomic operation, so if something goes wrong
	 during the operation it's possible the archive could be left without a central directory (although the local
	 file headers and file data will be fine, so the archive will be recoverable).

	 For more complex archive modification scenarios:
	 1. The safest way is to use a mz_zip_reader to read the existing archive, cloning only those bits you want to
	 preserve into a new archive using using the mz_zip_writer_add_from_zip_reader() function (which compiles the
	 compressed file data as-is). When you're done, delete the old archive and rename the newly written archive, and
	 you're done. This is safe but requires a bunch of temporary disk space or heap memory.

	 2. Or, you can convert an mz_zip_reader in-place to an mz_zip_writer using mz_zip_writer_init_from_reader(),
	 append new files as needed, then finalize the archive which will write an updated central directory to the
	 original archive. (This is basically what mz_zip_add_mem_to_archive_file_in_place() does.) There's a
	 possibility that the archive's central directory could be lost with this method if anything goes wrong, though.

	 - ZIP archive support limitations:
	 No zip64 or spanning support. Extraction functions can only handle unencrypted, stored or deflated files.
	 Requires streams capable of seeking.

   * This is a header file library, like stb_image.c. To get only a header file, either cut and paste the
	 below header, or create miniz.h, #define MINIZ_HEADER_FILE_ONLY, and then include miniz.c from it.

   * Important: For best perf. be sure to customize the below macros for your target platform:
	 #define MINIZ_USE_UNALIGNED_LOADS_AND_STORES 1
	 #define MINIZ_LITTLE_ENDIAN 1
	 #define MINIZ_HAS_64BIT_REGISTERS 1

   * On platforms using glibc, Be sure to "#define _LARGEFILE64_SOURCE 1" before including miniz.c to ensure miniz
	 uses the 64-bit variants: fopen64(), stat64(), etc. Otherwise you won't be able to process large files
	 (i.e. 32-bit stat() fails for me on files > 0x7FFFFFFF bytes).

 This is free and unencumbered software released into the public domain.

  Anyone is free to copy, modify, publish, use, compile, sell, or
  distribute this software, either in source code form or as a compiled
  binary, for any purpose, commercial or non-commercial, and by any
  means.

  In jurisdictions that recognize copyright laws, the author or authors
  of this software dedicate any and all copyright interest in the
  software to the public domain. We make this dedication for the benefit
  of the public at large and to the detriment of our heirs and
  successors. We intend this dedication to be an overt act of
  relinquishment in perpetuity of all present and future rights to this
  software under copyright law.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
  IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
  OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
  ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
  OTHER DEALINGS IN THE SOFTWARE.

  For more information, please refer to <http://unlicense.org/>
*/
