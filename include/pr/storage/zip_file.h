//*****************************************
// Zip Compression
//	Copyright (c) Rylogic 2019
//*****************************************
// This code is based on the 'miniz' library.
// Refactored to make use of modern C++ language features.
// See the file end for copyright notices.
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
	template <typename TAlloc = std::allocator<void>>
	class ZipArchiveA
	{
		// Notes:
		//  - Zip File Format Reference: https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT
		//  - Deflate algorithm reference: https://www.w3.org/Graphics/PNG/RFC-1951#algorithm
		//  - ZLIB compressed data format spec: https://tools.ietf.org/html/rfc1950

		static_assert(sizeof(uint16_t) == 2);
		static_assert(sizeof(uint32_t) == 4);
		static_assert(sizeof(uint64_t) == 8);

	public:

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

		// Archive flags
		enum class EZipFlags
		{
			None = 0,

			// Used when searching for items by name
			IgnoreCase = 1 << 0,

			// Used when searching for items by name
			IgnorePath = 1 << 1,

			// Used when adding and extracting items. Does not calculate or check CRC's.
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

		// Zip file header flags
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
				,CompressedSize(checked_cast<uint32_t>(compressed_size))
				,UncompressedSize(checked_cast<uint32_t>(uncompressed_size))
				,NameSize(checked_cast<uint16_t>(item_name_size))
				,ExtraSize(checked_cast<uint16_t>(extra_size))
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
				,CompressedSize(checked_cast<uint32_t>(compressed_size))
				,UncompressedSize(checked_cast<uint32_t>(uncompressed_size))
				,NameSize(checked_cast<uint16_t>(name_size))
				,ExtraSize(checked_cast<uint16_t>(extra_size))
				,CommentSize(checked_cast<uint16_t>(comment_size))
				,DiskNumberStart()
				,InternalAttributes(int_attributes)
				,ExternalAttributes(ext_attributes)
				,LocalHeaderOffset(checked_cast<uint32_t>(local_header_ofs))
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

		// Constants
		static size_t const LZDictionarySize = 0x8000;
		static uint32_t const DOSSubDirectoryFlag = 0x10;
		static uint32_t const InitialCrc = 0;

		// Read/Write function pointer types. Functions are expected to read/write 'n' bytes or throw.
		using read_func_t = void(*)(ZipArchiveA const& me, int64_t file_ofs, void* buf, size_t n);
		using write_func_t = void(*)(ZipArchiveA& me, int64_t file_ofs, void const* buf, size_t n);

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
		int IndexOf(std::string_view item_name, std::string_view item_comment = "") const
		{
			return IndexOf(item_name, item_comment, m_flags);
		}
		int IndexOf(std::string_view item_name, std::string_view item_comment, EZipFlags flags) const
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
		void AddAlreadyCompressed(std::string_view item_name, std::span<void const> buf, size_t uncompressed_size, uint32_t uncompressed_crc32, EMethod method = EMethod::Deflate, std::span<uint8_t const> extra = {}, std::string_view item_comment = "")
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
			append(m_central_dir, &cdh, &cdh + 1);
			append(m_central_dir, item_name);
			append(m_central_dir, extra);
			append(m_central_dir, item_comment);
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
		void Add(std::string_view item_name, std::span<uint8_t const> buf, EMethod method = EMethod::Deflate, std::span<uint8_t const> extra = {}, std::string_view item_comment = "", ECompressionLevel level = ECompressionLevel::Default, EZipFlags flags = EZipFlags::None)
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

			// Don't compress if too small
			if (buf.size() <= 3)
				level = ECompressionLevel::None;

			EMethod method = EMethod::None;
			uint16_t bit_flags = 0;
			uint16_t int_attributes = 0;
			uint32_t ext_attributes = 0;
			uint64_t compressed_size = 0;
			uint16_t dos_time = 0;
			uint16_t dos_date = 0;
			uint32_t crc32 = InitialCrc;

			// If the name has a directory divider at the end, set the directory bit
			if (item_name.back() == '/')
			{
				// Set DOS Subdirectory attribute bit.
				ext_attributes |= DOSSubDirectoryFlag;

				// Subdirectories cannot contain data.
				if (!buf.empty())
					throw std::runtime_error("Sub-directories cannot contain data.");
			}

			// Record the current time so the item can be date stamped. Do this before compressing just in case compression takes a while
			TimeToDosTime(time(nullptr), dos_time, dos_date);

			// Reserve space for the entry in the central directory
			m_central_dir.reserve(m_central_dir.size() + sizeof(CDH) + item_name.size() + extra.size() + item_comment.size());
			m_cdir_index.reserve(m_cdir_index.size() + 1);

			// Insert offsets
			auto num_alignment_padding_bytes = CalcAlignmentPadding();
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
				crc32 = Crc(buf.data(), buf.size());

			// Add the data
			auto item_ofs_beg = item_ofs;
			if (level == ECompressionLevel::None)
			{
				// Write the raw data
				m_write(*this, item_ofs, buf.data(), buf.size());
				item_ofs += buf.size();
				
				// Record the stats
				compressed_size = item_ofs - item_ofs_beg;
				method = EMethod::None;
			}
			else
			{
				Deflate algo;

				// Compress into a local buffer and periodically flush to the output
				vector_t<uint8_t> buf(Deflate::MaxBlockSize);
				algo.Compress(buf.data(), buf.size(), buf.data(), [&](auto& out, int)
				{
					m_write(*this, item_ofs, buf.data(), out - buf.data());
					item_ofs += out - buf.data();
					out = buf.data();
				});

				// Record the stats
				compressed_size = item_ofs - item_ofs_beg;
				method = EMethod::Deflate;
			}

			// Write the local directory header now that we have the compressed size
			LDH ldh(item_name.size(), extra.size(), buf.size(), compressed_size, crc32, method, bit_flags, dos_time, dos_date);
			m_write(*this, ldh_ofs, &ldh, sizeof(ldh));

			// Add an entry to the central directory
			CDH cdh(item_name.size(), extra.size(), item_comment.size(), buf.size(), compressed_size, crc32, method, bit_flags, dos_time, dos_date, ldh_ofs, ext_attributes, int_attributes);
			append(m_central_dir, &cdh, &cdh + 1);
			append(m_central_dir, item_name);
			append(m_central_dir, extra);
			append(m_central_dir, item_comment);
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
		void Add(std::string_view item_name, std::filesystem::path const& src_filepath, EMethod method = EMethod::Deflate, std::span<uint8_t const> extra = {}, std::string_view item_comment = "", ECompressionLevel level = ECompressionLevel::Default, EZipFlags flags = EZipFlags::None)
		{
			using namespace std::literals;

			// Sanity checks
			if (m_mode != EMode::Writing)
				throw std::runtime_error("ZipArchive not in writing mode");
			if (!ValidateItemName(item_name))
				throw std::runtime_error("Archive name is invalid or too long");
			if (!ValidateItemComment(item_comment))
				throw std::runtime_error("Item comment is invalid or too long");
			if (!std::filesystem::exists(src_filepath))
				throw std::runtime_error("Path '"s + src_filepath.string() + "' does not exist");
			if (!std::filesystem::is_directory(src_filepath))
				throw std::runtime_error("Path '"s + src_filepath.string() + "' is not a file");
			if (std::filesystem::file_size(src_filepath) > 0xFFFFFFFF)
				throw std::runtime_error("File '"s + src_filepath.string() + "' is too large. Zip64 is not supported");
			if (level < ECompressionLevel::None || level > ECompressionLevel::Uber)
				throw std::runtime_error("Compression level out of range");
			if (has_flag(flags, EZipFlags::CompressedData))
				throw std::runtime_error("Use the 'AddAlreadyCompressed' function to add compressed data.");
			if (m_total_entries >= 0xFFFF)
				throw std::runtime_error("Too many files added.");

			// Open the source file
			auto src_file = std::ifstream(src_filepath, std::ios::binary);
			if (!src_file.good())
				throw std::runtime_error("Failed to open file '"s + src_filepath.string() + "'");

			EBitFlags bit_flags = EBitFlags::None;
			uint16_t int_attributes = 0;
			uint32_t ext_attributes = 0;
			uint64_t compressed_size = 0;
			uint16_t dos_time = 0;
			uint16_t dos_date = 0;
			uint32_t crc32 = InitialCrc;

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
			auto num_alignment_padding_bytes = CalcAlignmentPadding();
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
				crc32 = Crc(src_file);

			// Write the compressed data
			auto item_ofs_beg = item_ofs;
			if (level == ECompressionLevel::None)
			{
				// Read from the file in blocks
				std::array<char, 4096> buf;
				for (; src_file.read(buf.data(), buf.size()).good();)
				{
					m_write(*this, item_ofs, buf.data(), src_file.gcount());
					item_ofs += src_file.gcount();
				}
				
				// Record the stats
				compressed_size = item_ofs - item_ofs_beg;
				method = EMethod::None;
			}
			else
			{
				Deflate algo;

				// Compress into a local buffer and periodically flush to the output
				vector_t<uint8_t> buf(Deflate::MaxBlockSize);
				algo.Compress(std::istream_iterator<uint8_t>(m_ifile), uncompressed_size, buf.data(), [&](auto& out, int)
				{
					auto size = out - buf.data();
					m_write(*this, item_ofs, buf.data(), size);
					item_ofs += size;
					out = buf.data();
				});

				// Record the stats
				compressed_size = item_ofs - item_ofs_beg;
				method = EMethod::Deflate;
			}

			// Write the local directory header now that we have the compressed size
			LDH ldh(item_name.size(), extra.size(), uncompressed_size, compressed_size, crc32, method, bit_flags, dos_time, dos_date);
			m_write(*this, ldh_ofs, &ldh, sizeof(ldh));

			// Add an entry to the central directory
			CDH cdh(item_name.size(), extra.size(), item_comment.size(), uncompressed_size, compressed_size, crc32, method, bit_flags, dos_time, dos_date, ldh_ofs, ext_attributes, int_attributes);
			append(m_central_dir, &cdh, &cdh + 1);
			append(m_central_dir, item_name);
			append(m_central_dir, extra);
			append(m_central_dir, item_comment);
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
		void Extract(std::string_view item_name, std::filesystem::path const& dst_filepath) const
		{
			Extract(item_name, dst_filepath, m_flags);
		}
		void Extract(std::string_view item_name, std::filesystem::path const& dst_filepath, EZipFlags flags) const
		{
			auto index = IndexOf(item_name, "", flags);
			return index >= 0 && index < m_total_entries
				? Extract(index, dst_filepath, flags)
				: throw std::runtime_error("Archive item not found");
		}
		void Extract(int index, std::filesystem::path const& dst_filepath) const
		{
			Extract(index, dst_filepath, m_flags);
		}
		void Extract(int index, std::filesystem::path const& dst_filepath, EZipFlags flags) const
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
		template <typename Elem = uint8_t> void Extract(std::string_view item_name, std::basic_ostream<Elem> & out) const
		{
			Extract(item_name, out, m_flags);
		}
		template <typename Elem = uint8_t> void Extract(std::string_view item_name, std::basic_ostream<Elem>& out, EZipFlags flags) const
		{
			auto index = IndexOf(item_name, "", flags);
			return index >= 0 && index < m_total_entries
				? Extract(index, out, flags)
				: throw std::runtime_error("Archive item not found");
		}
		template <typename Elem = uint8_t> void Extract(int index, std::basic_ostream<Elem> & out) const
		{
			void Extract(index, out, m_flags);
		}
		template <typename Elem = uint8_t> void Extract(int index, std::basic_ostream<Elem>& out, EZipFlags flags) const
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
		template <typename OutputCB> void Extract(std::string_view item_name, OutputCB callback, void* ctx) const
		{
			Extract(item_name, callback, ctx, m_flags);
		}
		template <typename OutputCB> void Extract(std::string_view item_name, OutputCB callback, void* ctx, EZipFlags flags) const
		{
			auto index = IndexOf(item_name, "", flags);
			Extract(index, callback, ctx, flags);
		}
		template <typename OutputCB> void Extract(int index, OutputCB callback, void* ctx) const
		{
			Extract(index, callback, ctx, m_flags);
		}
		template <typename OutputCB> void Extract(int index, OutputCB callback, void* ctx, EZipFlags flags) const
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

			uint64_t ofs = 0;
			uint32_t crc32 = InitialCrc;

			// The item was stored uncompressed or the caller has requested the compressed data.
			if (cdh.Method == EMethod::None || has_flag(flags, EZipFlags::CompressedData))
			{
				// Zip64 check
				if constexpr (sizeof(size_t) == sizeof(uint32_t))
					if (cdh.CompressedSize > 0xFFFFFFFF)
						throw std::runtime_error("Item is too large. Zip64 is not supported");

				// Calculate the crc if the call was not just for the compressed data
				if (!has_flag(flags, EZipFlags::CompressedData) && !has_flag(flags, EZipFlags::IgnoreCrc))
					crc32 = Crc(m_imem.data() + item_ofs, cdh.CompressedSize, crc32);

				// Send the data directly to the callback
				callback(ctx, ofs, m_imem.data() + item_ofs, size_t(cdh.CompressedSize));

				// All data sent
				item_ofs += cdh.CompressedSize;
				ofs += cdh.CompressedSize;

				// CRC check
				if (!has_flag(flags, EZipFlags::CompressedData) && !has_flag(flags, EZipFlags::IgnoreCrc) && cdh.Crc != crc32)
					throw std::runtime_error("CRC check failure");

				return;
			}

			// Data is compressed, inflate before passing to callback
			if (cdh.Method == EMethod::Deflate)
			{
				// Decompress into a temporary buffer. The minimum buffer size must be 'LZDictionarySize'
				// because Deflate uses references to earlier bytes, up to an LZ dictionary size prior.
				Deflate algo;
				vector_t<uint8_t> buf(LZDictionarySize);
				algo.Decompress(m_imem.data(), m_imem.size(), buf.data(), [&](uint8_t*& ptr, int)
				{
					auto count = size_t(ptr - buf.data());
					assert(count <= buf.size());

					// Update the crc
					if (!has_flag(flags, EZipFlags::IgnoreCrc))
						crc32 = Crc(buf.data(), count, crc32);

					// Push the buffered data out to the callback
					callback(ctx, ofs, buf.data(), count);
					ofs += count;

					// Reset to the start of the buffer
					ptr = buf.data();
				});

				// CRC check
				if (!has_flag(flags, EZipFlags::IgnoreCrc) && cdh.Crc != crc32)
					throw std::runtime_error("CRC check failure");

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

			uint64_t ofs = 0;
			uint32_t crc32 = InitialCrc;

			// The item was stored uncompressed or the caller has requested the compressed data.
			if (cdh.Method == EMethod::None || has_flag(flags, EZipFlags::CompressedData))
			{
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

				// Check the CRC
				if (!has_flag(flags, EZipFlags::CompressedData) && !has_flag(flags, EZipFlags::IgnoreCrc) && cdh.Crc != crc32)
					throw std::runtime_error("CRC check failure");

				return;
			}

			// Data is compressed, inflate before passing to callback
			if (cdh.Method == EMethod::Deflate)
			{
				Deflate algo;
		
				m_ifile.seekg(item_ofs);
				auto src = std::istream_iterator<uint8_t>(m_ifile);
				auto len = std::filesystem::file_size(m_filepath);

				// Decompress into a temporary buffer. The minimum buffer size must be 'LZDictionarySize'
				// because Deflate uses references to earlier bytes, upto an LZ dictionary size prior.
				vector_t<uint8_t> buf(LZDictionarySize);
				algo.Decompress(src, len, buf.data(), [&](uint8_t*& ptr, int)
				{
					auto count = size_t(ptr - buf.data());
					assert(count <= buf.size());

					// Update the CRC
					if (!has_flag(flags, EZipFlags::IgnoreCrc))
						crc32 = Crc(buf.data(), count, crc32);

					// Push the buffered data out to the callback
					callback(ctx, ofs, buf.data(), count);
					ofs += count;

					// Reset to the start of the buffer
					ptr = buf.data();
				});

				// Check the CRC
				if (!has_flag(flags, EZipFlags::IgnoreCrc) && cdh.Crc != crc32)
					throw std::runtime_error("CRC check failure");

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

	private:

		struct Deflate
		{
			// Notes:
			//  - Implements the DEFLATE compression algorthim
			//  - Compression format:
			//       https://en.wikipedia.org/wiki/DEFLATE
			//       https://www.w3.org/Graphics/PNG/RFC-1951
			//       https://zlib.net/feldspar.html <- explanation of DEFLATE
			//
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

			// Bit shift register. Could also use uint32_t if on 32-bit
			using bit_buf_t = uint64_t;

			// The maximum size of a block
			static size_t const MaxBlockSize = 64U * 1024U;

			// Huffman table sizes
			static size_t const LitTableSize = 288;
			static size_t const DstTableSize = 32;
			static size_t const DynTableSize = 19;
			static size_t const MaxTableSize = std::max({ LitTableSize, DstTableSize, DynTableSize });

			// The compressor defaults to 128 dictionary probes per dictionary search. 0=Huffman only, 1=Huffman+LZ (fastest/crap compression), 4095=Huffman+LZ (slowest/best compression).
			static uint32_t const DefaultProbes = 0x080;
			static uint32_t const MaxProbesMask = 0xFFF;
			static int const MaxSupportedHuffCodeSize = 32;
			static int const StaticBlockSizeThreshold = 5;
			static int const DynamicBlockSizeThreshold = 48;
			static int const MinMatchLength = 3;
			static int const MaxMatchLength = 258;

			// Flags used in 'Decompress()'
			enum class EDecompressFlags
			{
				None = 0,

				// If set, the input has a valid zlib header and ends with an Adler32 checksum (i.e. a zlib stream). Otherwise, the input is a raw deflate stream.
				ExpectZlibHeader = 1 << 0,
			};

			// Flags used by 'Compress()'
			enum class ECompressFlags
			{
				None = 0,
				
				// If set, the compressor outputs a zlib header before the deflate data, and the Adler-32 of the source data at the end. Otherwise, you'll get raw deflate data.
				WriteZLibHeader = 1 << 0,

				// Set to use faster greedy parsing, instead of more efficient lazy parsing.
				GreedyParsing = 1 << 1,

				// RLE_MATCHES: Only look for RLE matches (matches with a distance of 1)
				RLEMatches = 1 << 2,

				// Disable usage of optimized Huffman tables.
				ForceAllStaticBlocks = 1 << 3,

				// Only use raw (uncompressed) deflate blocks.
				ForceAllRawBlocks = 1 << 4,
			};

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

			// Functor for calculating the Adler32 checksum
			struct AdlerChecksum
			{
				static uint32_t const AdlerMod = 65521;
				uint32_t a, b;

				AdlerChecksum()
					:a(1)
					,b(0)
				{}
				uint32_t checksum() const
				{
					return (b << 16) | a;
				}
				uint8_t operator()(uint8_t byte)
				{
					a = (a + byte) % AdlerMod;
					b = (b + a) % AdlerMod;
					return byte;
				}
			};

			// Shift register used to stream data in/out during Compress()/Decompress()
			bit_buf_t m_bit_buf; // bits in => MSB...LSB => bits out
			int m_num_bits; // The current number of bits in the shift register

			Deflate()
				:m_bit_buf()
				,m_num_bits()
			{}

			// Decompress a stream of bytes from 'stream' and write the decompressed stream to 'output'.
			// 'Src' should have uint8_t pointer-like symmantics.
			// 'Out' should have uint8_t pointer-like symmantics.
			// 'stream' is the input stream of compressed data
			// 'length' is the length of data available via 'stream'
			// 'output' is an output iterator that receives the decompressed data.
			// 'flush' is called after each decompressed block. Signature: void flush(Out& out, int block_number)
			// 'flags' control the decompression behaviour
			// 'adler_checksum' is used to return the checksum value. Only valid for ZLib data.
			//   Callers should use the AdlerChecksum helper with their output iterator to calculate
			//   the checksum then compare it to the 'adler_checksum' value.
			template <typename Src, typename Out, typename FlushCB>
			void Decompress(Src stream, size_t length, Out output, FlushCB flush, EDecompressFlags const flags = EDecompressFlags::None, uint32_t* adler_checksum = nullptr)
			{
				using namespace std::literals;

				m_num_bits = 0;
				m_bit_buf = 0;

				SrcIter<Src> src(stream, length);
				OutIter<Out> out(output);

				// Parse the ZLIB header
				if (has_flag(flags, EDecompressFlags::ExpectZlibHeader))
				{
					auto cmf = *src; ++src; // Compression method an flags
					auto flg = *src; ++src; // More flags
					auto zhdr = ZLibHeader(cmf, flg);
					if (zhdr.Method() != EMethod::Deflate)
						throw std::runtime_error("ZLIB header indicates a compression method other than 'DEFLATE'. Not supported.");
					if (zhdr.PresetDictionary())
						throw std::runtime_error("ZLIB header contains a preset dictionary. Not supported.");
				}

				// A Deflate stream consists of a series of blocks. Each block is preceded by a 3-bit header:
				// First bit: Last-block-in-stream marker:
				//  1: this is the last block in the stream.
				//  0: there are more blocks to process after this one.
				// Second and third bits: Encoding method used for this block type:
				//  00: a stored/raw/literal section, between 0 and 0xFFFF bytes in length.
				//  01: a static Huffman compressed block, using a pre-agreed Huffman tree.
				//  10: a compressed block complete with the Huffman table supplied.
				//  11: reserved, don't use.
				int block_number = 0;
				for (bool more = true; more; ++block_number)
				{
					// Read the block header, and see if this is the last block
					auto hdr = GetBits<uint32_t>(src, 3);
					more = (hdr & 1) == 0;

					// Read the block type and prepare the huff tables based on type
					auto type = static_cast<EBlock>(hdr >> 1);
					switch (type)
					{
					// A stored/raw/literal section, between 0 and 65,535 bytes in length.
					case EBlock::Literal:
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
								*out = GetByte(src);

							break;
						}

					// A static Huffman compressed block, using pre-agreed symbol and distance tables
					case EBlock::Static:
						{
							// Initialise the literal/lengths table
							HuffLookupTable lit_table(LitTableSize);
							memset(&lit_table.m_code_size[  0], 8, 144);
							memset(&lit_table.m_code_size[144], 9, 112);
							memset(&lit_table.m_code_size[256], 7,  24);
							memset(&lit_table.m_code_size[280], 8,   8);
							lit_table.Populate();

							// Initialise the distance table
							HuffLookupTable dst_table(DstTableSize);
							memset(&dst_table.m_code_size[0], 5, dst_table.m_size);
							dst_table.Populate();

							// Decompress the block
							ReadBlock(src, lit_table, dst_table, out);
							break;
						}

					// A compressed block complete with the Huffman table supplied.
					case EBlock::Dynamic:
						{
							HuffLookupTable lit_table(GetBits<uint8_t>(src, 5) + 257); // number of literal/length codes (- 257)
							HuffLookupTable dst_table(GetBits<uint8_t>(src, 5) + 1);   // number of distance codes (- 1)
							HuffLookupTable dyn_table(GetBits<uint8_t>(src, 4) + 4);   // number of code length codes (- 4)

							// Read the 3-bit integer code lengths into 'dyn_table'.
							static std::array<uint8_t, 19> const s_dezigzag = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
							for (int i = 0; i != int(dyn_table.m_size); ++i) dyn_table.m_code_size[s_dezigzag[i]] = GetBits<uint8_t>(src, 3);
							dyn_table.m_size = DynTableSize;
							dyn_table.Populate();

							// Decompress the dynamic code length values
							std::array<uint8_t, LitTableSize + DstTableSize + 137> code_sizes = {};
							for (int i = 0, iend = lit_table.m_size + dst_table.m_size; i != iend;)
							{
								auto sym = HuffDecode(src, dyn_table);
								if (sym < 16)
								{
									// 'Sym' < 16 means it is a literal code size value
									code_sizes[i] = static_cast<uint8_t>(sym);
									++i;
								}
								else
								{
									// The dynamic table of code sizes is run length encoded, so all "distance" values are assumed to be 1.

									// The first symbol cannot be a reference to an earlier location
									if (i == 0 && sym == 16)
										throw std::runtime_error("Dynamic Huffman table is corrupt. Block index "s + std::to_string(block_number));

									// Read the length of the LZ encoded code size
									auto len = "\03\03\013"[sym - 16] + GetBits<uint32_t>(src, "\02\03\07"[sym - 16]); // length + num_extra
									memset(&code_sizes[i], sym == 16 ? code_sizes[i - 1] : 0, len);
									if ((i += len) > iend)
										throw std::runtime_error("Dynamic Huffman table is corrupt. Block index "s + std::to_string(block_number));
								}
							}

							// Copy the code length values to the lit/dst tables
							memcpy(&lit_table.m_code_size[0], &code_sizes[               0], lit_table.m_size);
							memcpy(&dst_table.m_code_size[0], &code_sizes[lit_table.m_size], dst_table.m_size);

							// Populate the Huffman trees and lookup tables
							dst_table.Populate();
							lit_table.Populate();

							// Decompress the block
							ReadBlock(src, lit_table, dst_table, out);
							break;
						}

					// reserved, don't use.
					case EBlock::Reserved:
					default:
						{
							throw std::runtime_error("DEFLATE stream contains an invalid block header");
						}
					}

					// Signal the end of a block
					flush(out.m_ptr, block_number);
				}

				// ZLib streams contain the Alder32 CRC after the data.
				if (has_flag(flags, EDecompressFlags::ExpectZlibHeader))
				{
					// Skip bits up to the next byte boundary
					void(GetBits<uint32_t>(src, m_num_bits & 7));

					// Read the expected Alder32 value
					uint32_t tail_adler32 = 1;
					for (int i = 0; i != 4; ++i)
						tail_adler32 = (tail_adler32 << 8) | GetByte(src);

					// Return the checksum
					if (adler_checksum != nullptr)
						*adler_checksum = tail_adler32;
				}
			}

			// Compress a stream of bytes from 'stream' and write the compressed stream to 'output'.
			// 'Src' should have uint8_t pointer-like symmantics.
			// 'Out' should have uint8_t pointer-like symmantics.
			// 'stream' is the input stream to be compressed.
			// 'length' is the number of bytes available from 'stream'
			// 'output' is the compressed output stream
			// 'flush' is called after each block is written. Signature: void flush(Out& out, int block_number)
			// 'flags' controls the compression output.
			// 'probe_count' controls the level of compression and must be a value in the range [0,4096) where 0=Huffman only, 1=Huffman+LZ (fastest/crap compression), 4095=Huffman+LZ (slowest/best compression).
			template <typename Src, typename Out, typename FlushCB>
			void Compress(Src stream, size_t length, Out output, FlushCB flush, ECompressFlags const flags = ECompressFlags::None, int probe_count = DefaultProbes)
			{
				m_bit_buf = 0;
				m_num_bits = 0;

				LZDictionary dict;
				LZBuffer lz_buffer;
				SymCount lit_counts(LitTableSize);
				SymCount dst_counts(DstTableSize);
				SrcIter<Src> src(stream, length), src_end;
				OutIter<Out> out(output);
				int block_number = 0;
				Range deferred;

				// Write the ZLib header for DEFLATE
				if (has_flag(flags, ECompressFlags::WriteZLibHeader) && length != 0)
				{
					PutByte(out, 0x78);
					PutByte(out, 0x01);
				}

				// Handle raw block output as a special case
				if (has_flag(flags, ECompressFlags::ForceAllRawBlocks))
				{
					for (auto remaining = length; remaining != 0; ++block_number)
					{
						// Header + Data <= MaxBlockSize
						auto const max = MaxBlockSize - 5;
						auto len = static_cast<uint16_t>(std::min(remaining, max));
						remaining -= len;

						// Write block header (1 byte)
						PutBits(out, len != max, 1);           // Write 1 for "last block"
						PutBits(out, int(EBlock::Literal), 2); // Write block type
						PutBits(out, 0, 5);                    // Align to next byte

						// Write length (4 bytes)
						PutBits(out, len, 16);
						PutBits(out, ~len, 16);

						// Write raw data (<= max bytes)
						for (; len-- != 0; ++src)
							PutByte(out, *src);

						flush(out.m_ptr, block_number);
					}
					assert(src == src_end);
				}

				// Add a literal byte to 'lz_buffer' and count frequencies of the byte values
				auto RecordLiteral = [&](uint8_t lit)
				{
					lz_buffer.add(lit);
					lit_counts[lit]++;
				};

				// Add a (length,distance) pair to 'lz_buffer' and count frequencies of the length and distance values
				auto RecordMatch = [&](Range match)
				{
					assert(match.len >= MinMatchLength && match.pos >= 1 && match.pos <= LZDictionarySize && "Match is invalid");
					lz_buffer.add(match);

					// Count frequency of matches of this length
					auto s = s_tdefl_len_sym[match.len - MinMatchLength];
					lit_counts[s]++;

					// Count frequency of matches at this distance
					auto dist = match.pos - 1;
					auto d = dist <= 0x1FF
						? s_tdefl_small_dist_sym[(dist >> 0) & 0x1FF]
						: s_tdefl_large_dist_sym[(dist >> 8) & 0x07F];
					dst_counts[d]++;
				};

				// Consume all bytes from 'stream'
				ptrdiff_t pos = 0;
				for (; src != src_end || pos != dict.m_size; ++block_number)
				{
					// Push up to 'MaxMatchLength' bytes into the dictionary
					for (; src != src_end && dict.m_size - pos < MaxMatchLength; ++src)
						dict.Push(*src);

					// Find the longest match for the current position
					auto match =
						has_flag(flags, ECompressFlags::RLEMatches) ? dict.RLEMatch(pos) :
						dict.Match(pos, probe_count);

					// Encode the source data into 'lz_buffer'
					if (match.len < MinMatchLength) // If there is no suitable match...
					{
						// If there is no deferred match...
						if (deferred.len == 0 || has_flag(flags, ECompressFlags::GreedyParsing))
						{
							// Write a literal byte
							RecordLiteral(dict[pos]);
							++pos;
						}
						else
						{
							// Write the deferred match. It should include the byte at 'pos'
							assert(deferred.begin() < pos && pos <= deferred.end());
							RecordMatch(deferred);
							pos = deferred.end();
							deferred = Range();
						}
					}
					else // A match was found...
					{
						if (has_flag(flags, ECompressFlags::GreedyParsing))
						{
							// Greedy parsing means don't bother with defering matches
							RecordMatch(match);
							pos = match.end();
						}
						else if (deferred.len == 0)
						{
							// Defer recording this match. See "lazy matching" in the comments above
							deferred = match;
							++pos;
						}
						else
						{
							// If 'match' is better than 'deferred'
							if (match.len > deferred.len)
							{
								// Record a literal byte and keep 'match' as the new 'deferred'
								RecordLiteral(dict[deferred.pos]);
								deferred = match;
								++pos;
							}
							else
							{
								// Otherwise, deferred is better than 'match', record 'deferred'. It should include 'match'
								assert(deferred.begin() < match.begin() && match.end() <= deferred.end());
								RecordMatch(deferred);
								pos = deferred.end();
								deferred = Range();
							}
						}
					}

					// Write a block when 'lz_buffer' is full
					if (LZBuffer::Size - lz_buffer.size() < LZBuffer::MinSpaceRequired)
					{
						WriteBlock(out, lz_buffer, dict, pos, lit_counts, dst_counts, flags, false);
						flush(out.m_ptr, block_number);
						
						// Reset the compression bumber and symbol counts
						lz_buffer.reset();
						lit_counts.reset();
						dst_counts.reset();
					}
				}

				// Write any remaining data
				WriteBlock(out, lz_buffer, dict, pos, lit_counts, dst_counts, flags, true);
				flush(out.m_ptr, block_number);

				// Write the ZLib footer
				if (has_flag(flags, ECompressFlags::WriteZLibHeader) && length != 0)
				{
					// Calculate the checksum on the source input
					auto s = stream;
					AdlerChecksum adler;
					for (auto l = length; l-- != 0; ++s)
						adler(*s);

					// Align to the next byte
					if (m_num_bits != 0)
						PutBits(out, 0, 8 - m_num_bits);

					// Write the adler checksum (bit endian)
					auto checksum = adler.checksum();
					PutByte(out, (checksum >> 24) & 0xFF);
					PutByte(out, (checksum >> 16) & 0xFF);
					PutByte(out, (checksum >>  8) & 0xFF);
					PutByte(out, (checksum >>  0) & 0xFF);
				}
			}

		private:

			// Compressed block types
			enum class EBlock
			{
				Literal  = 0,
				Static   = 1,
				Dynamic  = 2,
				Reserved = 3,
			};

			// An iterator wrapper for 'src' pointers
			template <typename Src>
			struct SrcIter
			{
				using value_type = typename std::iterator_traits<Src>::value_type;

				Src m_ptr;       // Iterator to underlying sequence
				ptrdiff_t m_len; // Remaining count

				SrcIter()
					:m_ptr()
					,m_len()
				{}
				SrcIter(Src src, ptrdiff_t len)
					:m_ptr(src)
					,m_len(len)
				{}
				value_type operator *() const
				{
					return m_len != 0 ? *m_ptr : value_type{};
				}
				SrcIter& operator ++()
				{
					if (m_len != 0)
					{
						++m_ptr;
						--m_len;
					}
					return *this;
				}

				friend bool operator == (SrcIter lhs, SrcIter rhs) { return lhs.m_ptr == rhs.m_ptr || (lhs.m_len == 0 && rhs.m_len == 0); }
				friend bool operator != (SrcIter lhs, SrcIter rhs) { return !(lhs == rhs); }
				friend ptrdiff_t operator - (SrcIter lhs, SrcIter rhs)
				{
					// Remember, 'm_len' is length remaining so iterators with 'm_len == 0' are end iterators
					return lhs.m_len - rhs.m_len;
				}
			};

			// An output iterator wrapper
			template <typename Out>
			struct OutIter
			{
				Out m_ptr;       // Underlying output iterator
				ptrdiff_t m_len; // Number of bytes written

				explicit OutIter(Out ptr, ptrdiff_t len = 0)
					:m_ptr(ptr)
					,m_len(len)
				{}
				OutIter& operator*()
				{
					return *this;
				}
				OutIter& operator = (uint8_t b)
				{
					*m_ptr = b;
					return *this;
				}
				OutIter& operator ++()
				{
					++m_ptr;
					++m_len;
					return *this;
				}
				friend OutIter operator - (OutIter lhs, ptrdiff_t ofs)
				{
					if (ofs > lhs.m_len)
						throw std::runtime_error("Corrupt zip. Rereference to an earlier byte sequence that is out of range");

					return OutIter(lhs.m_ptr - ofs, lhs.m_len - ofs);
				}
			};

			// Represents the interval [pos, pos + len)
			struct Range
			{
				ptrdiff_t pos;
				ptrdiff_t len;

				Range()
					:Range(0,0)
				{}
				Range(ptrdiff_t start, ptrdiff_t count)
					:pos(start)
					,len(count)
				{}

				// True if 'x' is within the interval [pos, pos + len)
				bool contains(ptrdiff_t x) const
				{
					return x >= begin() && x < end();
				}
				bool contains(Range x) const
				{
					return x.begin() >= begin() && x.end() <= end();
				}

				// Reduce the range to [pos + count, pos + len - count)
				void move_beg(ptrdiff_t count = 1)
				{
					pos += count;
					len -= count;
					return *this;
				}

				// Extend the range to [pos, pos + len + count)
				void move_end(ptrdiff_t count = 1)
				{
					len += count;
					return *this;
				}

				// The range begin/end
				ptrdiff_t begin() const
				{
					return pos;
				}
				ptrdiff_t end() const
				{
					return pos + len;
				}
			};

			// Fixed buffer for counting symbol frequencies
			struct SymCount
			{
				std::array<uint16_t, MaxTableSize> m_data;
				size_t m_size;

				SymCount()
					:SymCount(0)
				{}
				SymCount(size_t size)
					:m_data()
					,m_size(size)
				{
					assert(size <= MaxTableSize);
				}
				size_t size() const
				{
					return m_size;
				}
				void reset()
				{
					memset(m_data.data(), 0, sizeof(uint16_t) * m_size);
				}
				uint16_t operator[](int idx) const
				{
					assert(idx < m_size);
					return m_data[idx];
				}
				uint16_t& operator[](int idx)
				{
					assert(idx < m_size);
					return m_data[idx];
				}
				operator std::span<uint16_t const>() const
				{
					return std::make_span(m_data.data(), m_size);
				}
			};

			// A ring buffer of 'T' with fixed size 'Size' and optional "tail" of length 'Extend'
			template <typename T, size_t Size, size_t Extend = 0>
			struct RingBuffer
			{
				// This is basically a normal ring buffer of size 'Size'. There is an additional
				// 'Extend' bytes duplicated at the end so that sequences of 'Extend' bytes are contiguous.
				static_assert((Size & (Size - 1)) == 0, "RingBuffer size must be a power of 2");
				static_assert(Extend <= Size, "Size must be large enough to contain 'Extend' bytes");
				static_assert(std::is_pod_v<T>, "Designed for POD types only");
				static int const Mask = Size - 1;

				std::array<T, Size + Extend> m_buf;
				bool m_extend_required; // Dirty flag for when values are modified in the range [0, Extend)

				RingBuffer()
					:m_buf()
					,m_extend_required()
				{}

				// The maximum size of the ring buffer
				constexpr int capacity() const
				{
					return Size;
				}

				// Ring buffer array access
				T operator [](ptrdiff_t idx) const
				{
					return m_buf[idx & Mask];
				}
				T& operator [](ptrdiff_t idx)
				{
					m_extend_required |= idx >= 0 && idx < Extend;
					return m_buf[idx & Mask];
				}

				// Return a pointer into the buffer that is valid for at least 'Extend' values
				T const* ptr(ptrdiff_t ofs) const
				{
					const_cast<RingBuffer*>(this)->extend();
					return &m_buf[ofs & Mask];
				}

			private:

				// Replicate the first 'Extend' value at the end of the buffer
				void extend()
				{
					if (!m_extend_required) return;
					memcpy(&m_buf[Size], &m_buf[0], Extend * sizeof(T));
					m_extend_required = false;
				}
			};

			// Helper for generating Huffman codes up to 'max_code_size' in length
			template <typename T>
			struct HuffCodeGen
			{
				std::array<T, 1 + sizeof(T) * 8> m_next_code;
				int m_max_code_size;

				HuffCodeGen(int max_code_size, std::span<int const> num_sizes)
					:m_next_code({ 0, 0 })
					,m_max_code_size(max_code_size)
				{
					assert(max_code_size <= m_next_code.size());
					assert(num_sizes.size() >= max_code_size);

					uint32_t total = 0;
					for (int i = 1; i != m_max_code_size; ++i)
					{
						total = static_cast<uint32_t>((total + num_sizes[i]) << 1);
						m_next_code[i + 1] = total;
					}
					if (total != 1U << m_max_code_size)
						throw std::runtime_error("'num_sizes' does not span the code space");
				}

				// Return the Huffman code for 'code_size'
				T operator()(int code_size)
				{
					assert(code_size < m_max_code_size);
					return ReverseBits(m_next_code[code_size]++, code_size);
				}
			};

			// Ring buffers used to identify repeating sequences of bytes in the input stream
			struct LZDictionary
			{
				// Hash table constants
				static int const HashTableBits = 15; // Alternative for low memory environments: 12
				static int const LZHashShift = Div3(HashTableBits);
				static size_t const HashTableSize = 1 << HashTableBits;

				// A ring buffer of source bytes
				RingBuffer<uint8_t, LZDictionarySize, MaxMatchLength> m_bytes;

				// Singularly linked lists of locations in 'm_bytes' that have the same hash value
				RingBuffer<uint16_t, LZDictionarySize> m_next;

				// Mapping from the hash of a 3-byte sequence to its starting index position in 'm_bytes'
				RingBuffer<uint16_t, HashTableSize> m_hash;

				// The number of bytes added to the dictionary (not wrapped to LZDictionarySize)
				ptrdiff_t m_size;

				LZDictionary()
					:m_bytes()
					,m_next()
					,m_hash()
					,m_size()
				{}

				// Return the range of bytes currently in the 'm_bytes' ring buffer
				Range Available() const
				{
					return Range(std::max<ptrdiff_t>(0, m_size - LZDictionarySize), std::min<ptrdiff_t>(m_size, LZDictionarySize));
				}

				// Push a source byte into the dictionary
				void Push(uint8_t b)
				{
					assert(m_size < m_bytes.capacity());

					// Add the next byte to the dictionary
					m_bytes[m_size] = b;
					++m_size;

					// Calcalate the hash of the last 3 bytes
					// Wrap around is handled by the ring buffer.
					auto i = m_size - 3;
					auto hash =
						(m_bytes[i + 0] << (LZHashShift * 2)) ^
						(m_bytes[i + 1] << (LZHashShift * 1)) ^
						(m_bytes[i + 2] << (LZHashShift * 0));

					// Insert 'hash' at the head of the singularly linked list of dictionary positions with the same hash value.
					// Note that by inserting at the head, hashes are in order from most recently seen to most distant.
					//  e.g.
					//     hash = HashFn(bytes[i+0], bytes[i+1], bytes[i+2])
					//     m_hash[hash] = i -> the newest index position with hash value 'hash'
					//     m_next[i] -> index of the next most recent position that has the same hash value, say, "j".
					//         m_next[j] -> index of the next most recent position that has the same hash value, say, "k".
					//             m_next[k] -> 0 = end of list.
					//
					// Note that 'm_hash' is only needed for constructing the linked lists of index positions.
					// Matching only requires the 'm_next' buffer and a starting index position.
					m_next[i] = m_hash[hash];
					m_hash[hash] = static_cast<uint16_t>(i);
				}

				// Search the dictionary for another position that matches 'pos' that is longer than 'best_match'
				// 'pos' is the source location in the dictionary to match against.
				// 'probe_count' is the maximum number of searches to perform
				Range Match(ptrdiff_t pos, int probe_count) const
				{
					// Hashes are based on 3-bytes sequences, so at least 3 bytes must have been added before matches can be found.
					assert(pos + 3 <= m_size);

					// The hash value of the 3-byte sequence at 'm_bytes[pos]' is the value in 'm_hash[pos]'.
					// We don't actually need the hash, we just need to search the linked list of index locations whose head is at 'm_next[pos]'.
					// The dictionary only contains a maximum of 'LZDictionarySize' bytes, so if 'i' is further back than this, a match cannot be tested.
					Range best_match;
					for (auto i = m_next[pos]; probe_count-- != 0 && i != 0 && m_size - i >= LZDictionarySize; i = m_next[i])
					{
						auto ref = m_bytes.ptr(pos);
						auto cmp = m_bytes.ptr(i);

						// Find the length of the match
						int len = 0, max = std::min(MaxMatchLength, int(m_size - pos));
						for (; len != max && *cmp == *ref; ++len, ++cmp, ++ref) {}

						// If the match is longer than the current best match record it
						if (len > best_match.len)
						{
							// If a decent match is found, reduce the number of remaining probes to speed up searching
							if (len >= (MaxMatchLength + best_match.len) / 2)
								probe_count >>= 1;

							// Save the best match
							best_match = Range(i, len);

							// Can't do better than this so stop searching
							if (len == MaxMatchLength)
								break;
						}
					}
					return best_match;
				}

				// Look for a range using run length encoding starting at 'pos'
				Range RLEMatch(ptrdiff_t pos) const
				{
					auto ref = m_bytes.ptr(pos);
					auto cmp = m_bytes.ptr(pos);

					int len = 0, max = std::min(MaxMatchLength, int(m_size - pos));
					for (; len != max && *cmp == *ref; ++len, ++cmp) {}

					return Range(pos, len);
				}

				// Data access
				uint8_t operator[](ptrdiff_t idx) const
				{
					return m_bytes[idx];
				}
			};

			// Records literal bytes, or (length,distance) pairs
			struct LZBuffer
			{
				// Notes:
				//  - Constructs an interlaced buffer of flags and literal bytes or (length,distance) pairs.
				//     e.g. [flags, bytes..., flags, bytes..., flags, etc...]
				//  - The LSB of a flags byte is the 'type' of data in the following byte
				//    0 - means a literal byte (length = 1)
				//    1 - means a (length, distance) pair (length = 3 bytes)

				static size_t const Size = 64 * 1024;
				static size_t const MinSpaceRequired = 4;  // for the largest "add" call = 1 byte for flags, 3 bytes for (length,distance)
				static_assert(Size > LZDictionarySize);

				uint8_t m_buf[Size];
				uint8_t* m_flags;   // Flags that record the type of data stored in the following bytes
				uint8_t* m_bytes;   // Where to insert the next literal byte or (length,distance) pair
				int m_num_flags;    // The number of flags used in the current byte pointed to by 'm_flags'
				size_t m_data_size; // The number of source bytes represented

				LZBuffer()
					:m_buf()
					,m_flags(&m_buf[0])
					,m_bytes(&m_buf[1])
					,m_num_flags(0)
					,m_data_size(0)
				{}

				// The number of bytes currently in the buffer;
				size_t size() const
				{
					return (m_num_flags != 0 ? m_bytes : m_flags) - &m_buf[0];
				}

				// Reset the buffer
				void reset()
				{
					m_flags = &m_buf[0];
					m_bytes = &m_buf[1];
					m_num_flags = 0;
					m_data_size = 0;
				}

				// Add a literal byte to the buffer
				void add(uint8_t byte)
				{
					assert(m_bytes - &m_buf[0] + 1 <= Size && "LZBuffer overflow");
					*m_bytes++ = byte;
					m_data_size += 1;
					push_flag<0>();
				}

				// Add a match to the buffer
				void add(Range match)
				{
					static_assert(LZDictionarySize - 1 <= 0xFFFF);
					static_assert(MaxMatchLength - MinMatchLength <= 0xFF);
					assert(match.len >= MinMatchLength && match.len <= MaxMatchLength && "Match length is invalid");
					assert(match.pos >= 1 && match.pos <= LZDictionarySize && "Match distance is invalid");
					assert(m_bytes - &m_buf[0] + 3 <= Size && "LZBuffer overflow");

					m_data_size = static_cast<size_t>(m_data_size + match.len);
					*m_bytes++ = static_cast<uint8_t>(match.len - MinMatchLength);
					*m_bytes++ = static_cast<uint8_t>(((match.pos - 1) >> 0) & 0xFF);
					*m_bytes++ = static_cast<uint8_t>(((match.pos - 1) >> 8) & 0xFF);
					push_flag<1>();
				}

				// The number of input data bytes represented
				size_t data_size() const
				{
					return m_data_size;
				}

				// Begin/End to the range of contained data
				uint8_t const* begin() const
				{
					return &m_buf[0];
				}
				uint8_t const* end() const
				{
					return begin() + size();
				}

			private:

				// Append a flag
				template <int bit> void push_flag()
				{
					static_assert(bit <= 1);

					*m_flags |= static_cast<uint8_t>(bit << m_num_flags);
					if (++m_num_flags == 8)
					{
						// If the flags byte is full, use the next byte in the buffer for flags
						m_num_flags = 0;
						m_flags = m_bytes++;
					}
				}
			};

			// Huffman lookup table
			struct HuffLookupTable
			{
				static int const Bits = 10;
				static size_t const Size = 1 << Bits;
				static size_t const Mask = Size - 1;

				int m_size; // Table size
				std::array<uint8_t, MaxTableSize> m_code_size;
				std::array<int16_t, MaxTableSize * 2> m_tree;
				std::array<int16_t, Size> m_look_up;

				HuffLookupTable() = default;
				HuffLookupTable(int size)
					:m_size(size)
					,m_code_size()
					,m_tree()
					,m_look_up()
				{}

				// Populate the tree and lookup tables after 'm_code_size' has been updated
				void Populate()
				{
					// The alphabet is all byte values in the range [0, m_size)
					// With just the code sizes for each alphabet value, the Huffman tree can be
					// constructed, which implies the Huffman code used for each alphabet value.

					// Find the counts of each code size
					std::array<int, 16> num_sizes = {};
					for (int i = 0; i != m_size; ++i)
						num_sizes[m_code_size[i]]++;

					// Generate the lookup table and tree
					HuffCodeGen<uint16_t> gen(16, num_sizes);
					int16_t tree_cur, tree_next = -1;
					for (int sym_index = 0; sym_index != m_size; ++sym_index)
					{
						// Get the length of the code
						auto code_size = m_code_size[sym_index];
						if (code_size == 0)
							continue;

						// Populate the lookup table with the code size and symbol index bit stuffed into an int16.
						auto rev_code = gen(code_size);
						if (code_size <= HuffLookupTable::Bits)
						{
							auto k = static_cast<int16_t>((code_size << 9) | sym_index);
							for (; rev_code < HuffLookupTable::Size; rev_code += (1 << code_size))
								m_look_up[rev_code] = k;

							continue;
						}

						// Grow the tree
						tree_cur = m_look_up[rev_code & HuffLookupTable::Mask];
						if (tree_cur == 0)
						{
							// Save the index to the next sub-tree
							m_look_up[rev_code & HuffLookupTable::Mask] = tree_next;
							tree_cur = tree_next;
							tree_next -= 2;
						}

						// Navigate the tree to find where to save 'sym_index'
						rev_code >>= HuffLookupTable::Bits - 1;
						for (int i = code_size; i > HuffLookupTable::Bits + 1; --i)
						{
							rev_code >>= 1;
							tree_cur -= rev_code & 1;

							if (!m_tree[~tree_cur])
							{
								m_tree[~tree_cur] = tree_next;
								tree_cur = tree_next;
								tree_next -= 2;
							}
							else
							{
								tree_cur = m_tree[~tree_cur];
							}
						}

						tree_cur -= (rev_code >>= 1) & 1;
						m_tree[~tree_cur] = (int16_t)sym_index;
					}
				}
			};

			// Used to generate Huffman codes
			struct HuffCodeTable
			{
				int m_size;          // Table size
				int m_max_code_size; // The maximum value of any code size
				std::array<uint8_t, MaxTableSize> m_code_size;
				std::array<uint16_t, MaxTableSize> m_code;

				HuffCodeTable() = default;
				HuffCodeTable(int size, int max_code_size)
					:m_size(size)
					,m_max_code_size(max_code_size)
					,m_code_size()
					,m_code()
				{
					assert(max_code_size <= MaxSupportedHuffCodeSize);
				}

				// Populate the 'm_code' table once 'm_code_sizes' have been set
				void Populate(EBlock block_type, std::span<uint16_t const> counts)
				{
					// Count the frequency of each code size
					std::array<int, MaxSupportedHuffCodeSize + 1> num_sizes = {};
					switch (block_type)
					{
					case EBlock::Static:
						{
							// All code sizes have an equal number (i.e. 1)
							for (int i = 0; i != m_size; ++i)
								num_sizes[m_code_size[i]]++;
							break;
						}
					case EBlock::Dynamic:
						{
							// Optimise this table by moving the most common symbols to
							// the start so that common symbols get shorter Huffman codes.

							using SymbolFreq = struct { uint16_t m_count; uint16_t m_index; };
							std::array<SymbolFreq, MaxTableSize> count_to_index;
							int len = 0;

							// Map counts to index position
							for (uint16_t i = 0; i != m_size; ++i)
							{
								if (counts[i] == 0) continue;
								count_to_index[len++] = SymbolFreq{ counts[i], i };
							}

							// Sort the symbols by frequency so that the most common are at the front
							auto syms = std::make_span(count_to_index.data(), len);
							std::sort(begin(syms), end(syms), [](auto& l, auto& r) { return l.m_count > r.m_count; });

							// Calculate Minimum Redunancy
							for(;;)
							{
								// Originally written by: Alistair Moffat, alistair@cs.mu.oz.au, Jyrki Katajainen, jyrki@diku.dk, November 1996.
								if (len == 0)
								{
									break;
								}
								if (len == 1)
								{
									syms[0].m_count = 1;
									break;
								}

								syms[0].m_count += syms[1].m_count;

								int root = 0;
								int next = 1;
								int leaf = 2;
								for (; next < len - 1; ++next)
								{
									if (leaf >= len || syms[root].m_count < syms[leaf].m_count)
									{
										syms[next].m_count = syms[root].m_count;
										syms[root].m_count = static_cast<uint16_t>(next);
										root++;
									}
									else
									{
										syms[next].m_count = syms[leaf].m_count;
										leaf++;
									}

									if (leaf >= len || (root < next && syms[root].m_count < syms[leaf].m_count))
									{
										syms[next].m_count = static_cast<uint16_t>(syms[next].m_count + syms[root].m_count);
										syms[root].m_count = static_cast<uint16_t>(next);
										root++;
									}
									else
									{
										syms[next].m_count = static_cast<uint16_t>(syms[next].m_count + syms[leaf].m_count);
										leaf++;
									}
								}

								syms[len - 2].m_count = 0;
								for (next = len - 3; next >= 0; next--)
									syms[next].m_count = syms[syms[next].m_count].m_count + 1;

								int avbl = 1;
								int used = 0;
								int dpth = 0;
								root = len - 2;
								next = len - 1;
								while (avbl > 0)
								{
									while (root >= 0 && (int)syms[root].m_count == dpth)
									{
										used++;
										root--;
									}
									while (avbl > used)
									{
										syms[next--].m_count = (uint16_t)(dpth);
										avbl--;
									}

									avbl = 2 * used;
									dpth++;
									used = 0;
								}
								break;
							}

							// 
							for (int i = 0; i != len; ++i)
								num_sizes[syms[i].m_count]++;

							// Limits canonical Huffman code table's max code size.
							for (;;)
							{
								if (len <= 1)
									break;

								uint32_t total = 0;
								for (int i = m_max_code_size + 1; i <= MaxSupportedHuffCodeSize; i++)
									num_sizes[m_max_code_size] += num_sizes[i];
								for (int i = m_max_code_size; i > 0; i--)
									total += (((uint32_t)num_sizes[i]) << (m_max_code_size - i));

								for (; total != (1UL << m_max_code_size); total--)
								{
									num_sizes[m_max_code_size]--;
									for (int i = m_max_code_size - 1; i > 0; i--)
									{
										if (num_sizes[i] == 0) continue;
										num_sizes[i]--;
										num_sizes[i + 1] += 2;
										break;
									}
								}

								break;
							}

							// Update the code sizes
							for (int i = 0, j = len; i != m_max_code_size; ++i)
								for (int l = num_sizes[i+1]; l > 0; l--)
									m_code_size[syms[--j].m_index] = checked_cast<uint8_t>(i + 1);

							break;
						}
					case EBlock::Literal:
						{
							throw std::runtime_error("Block type does not have a Huffman table");
						}
					case EBlock::Reserved:
					default:
						{
							throw std::runtime_error("Invalid block type");
						}
					}

					// Generate the huffman codes
					HuffCodeGen<uint16_t> gen(m_max_code_size, num_sizes);
					for (int i = 0; i != m_size; ++i)
					{
						if (m_code_size[i] == 0) continue;
						m_code[i] = gen(m_code_size[i]);
					}
				}
			};

			// Read one byte from 'src'
			template <typename Src>
			uint8_t GetByte(SrcIter<Src>& src)
			{
				if (m_num_bits == 0)
				{
					auto b = *src; ++src;
					return b;
				}

				if (m_num_bits < 8)
				{
					// Append bits on the left
					m_bit_buf |= static_cast<bit_buf_t>(*src) << m_num_bits; ++src;
					m_num_bits += 8;
				}
				auto b = static_cast<uint8_t>(m_bit_buf & 0xFF);
				m_bit_buf >>= 8;
				m_num_bits -= 8;
				return b;
			}

			// Write one byte to 'out'
			template <typename Out>
			void PutByte(OutIter<Out>& out, uint8_t b)
			{
				if (m_num_bits == 0)
				{
					*out = b; ++out;
					return;
				}

				m_bit_buf |= static_cast<bit_buf_t>(b << m_num_bits);
				m_num_bits += 8;

				// Write out a whole byte
				*out = static_cast<uint8_t>(m_bit_buf & 0xFF); ++out;
				m_bit_buf >>= 8;
				m_num_bits -= 8;
			}

			// Read 'n' bits from the source stream into 'm_bit_buf'
			template <typename TInt, typename Src>
			TInt GetBits(SrcIter<Src>& src, int n)
			{
				assert(n <= sizeof(TInt) * 8 && "Return type not large enough for 'n' bits");
				for (; m_num_bits < n;)
				{
					// Append bits on the left
					m_bit_buf |= static_cast<bit_buf_t>(*src) << m_num_bits; ++src;
					m_num_bits += 8;
				}

				// Read and pop the lower 'n' bits
				auto b = static_cast<TInt>(m_bit_buf & ((1 << n) - 1));
				m_bit_buf >>= n;
				m_num_bits -= n;
				return b;
			}

			// Write 'n' bits to the output stream, via 'm_bit_buf'
			template <typename Out>
			void PutBits(OutIter<Out>& out, bit_buf_t bits, int n)
			{
				assert((bits & (~bit_buf_t() << n)) == 0 && "'bits' has more than 'n' bits");
				assert(m_num_bits + n <= sizeof(bit_buf_t) * 8 && "Bit buffer overflow");

				// Add the bits on the left
				m_bit_buf |= bits << m_num_bits;
				m_num_bits += n;

				// Write out whole bytes
				for (; m_num_bits >= 8;)
				{
					*out = static_cast<uint8_t>(m_bit_buf & 0xFF); ++out;
					m_bit_buf >>= 8;
					m_num_bits -= 8;
				}
			}

			// Decodes and returns the next Huffman coded symbol.
			template <typename Src>
			int HuffDecode(SrcIter<Src>& src, HuffLookupTable const& table)
			{
				// Notes:
				//  - This function reads 2 bytes from 'src'.

				// It's more complex than you would initially expect because the zlib API expects the decompressor to never read
				// beyond the final byte of the deflate stream. (In other words, when this macro wants to read another byte from the input, it REALLY needs another byte in order to fully
				// decode the next Huffman code.) Handling this properly is particularly important on raw deflate (non-zlib) streams, which aren't followed by a byte aligned adler-32.
				// The slow path is only executed at the very end of the input buffer.

				// Ensure 'm_bit_buf' contains at least 15 bits
				for (; m_num_bits < 16; m_num_bits += 8)
				{
					m_bit_buf |= static_cast<bit_buf_t>(*src) << m_num_bits;
					++src;
				}

				// if symbol >= 0:
				//   bits [15..9] = code length
				//   bits [8] = word encoded flag
				//   bits [0..7] = literal byte (if bit 8 is 0), length (it bit 8 is 1)
				// else:
				//   Navigate the Huffman tree to locate the symbol

				// Read the Huff symbol
				auto symbol = table.m_look_up[m_bit_buf & HuffLookupTable::Mask];
				auto code_len = symbol >= 0 ? symbol >> 9 : HuffLookupTable::Bits;
				if (symbol >= 0)
					symbol &= 0x1FF;
				else
					for (; symbol < 0;)
						symbol = table.m_tree[~symbol + ((m_bit_buf >> code_len++) & 1)];

				// Consume 'code_len' bits
				m_bit_buf >>= code_len;
				m_num_bits -= code_len;
				return symbol;
			}

			// Uses the given tables to decompress data to the end of the block
			template <typename Src, typename Out>
			void ReadBlock(SrcIter<Src>& src, HuffLookupTable const& lit_table, HuffLookupTable const& dst_table, OutIter<Out>& out)
			{
				for (;;)
				{
					// Read an decode the next symbol
					auto symbol = HuffDecode(src, lit_table);

					// If the symbol is the end-of-block marker, done
					if (symbol == 0x0100)
					{
						break;
					}

					// If the symbol is not a length value, output the literal byte
					else if (!has_flag(symbol, 0x0100))
					{
						*out = static_cast<uint8_t>(symbol & 0xFF);
						++out;
					}

					// Otherwise the symbol is a length value, implying it's followed by a distance value
					else
					{
						// Read the length of the sequence
						auto idx = symbol - 257;
						static std::array<int, 31> const s_length_base = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258, 0, 0 };
						static std::array<int, 31> const s_length_extra = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0, 0, 0 };
						auto count = s_length_base[idx] + GetBits<uint32_t>(src, s_length_extra[idx]);

						// Read the relative offset back to where to read from
						auto ofs = HuffDecode(src, dst_table);
						static std::array<int, 32> const s_dist_base = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577, 0, 0 };
						static std::array<int, 32> const s_dist_extra = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
						auto dist = s_dist_base[ofs] + GetBits<uint32_t>(src, s_dist_extra[ofs]);

						// Repeat an earlier sequence from [prev, prev + count)
						auto prev = out - dist;
						for (; count-- != 0; ++out, ++prev)
							*out = *prev;
					}
				}
			}

			// Write a block to the output
			template <typename Out>
			void WriteBlock(OutIter<Out>& out, LZBuffer const& lz_buffer, LZDictionary const& dict, ptrdiff_t pos, SymCount const& lit_counts, SymCount const& dst_counts, ECompressFlags flags, bool last)
			{
				// Write the "last block" flag
				PutBits(out, last, 1);

				// Decide what block type to output
				auto block_type =
					has_flag(flags, ECompressFlags::ForceAllStaticBlocks) ? EBlock::Static :
					lz_buffer.data_size() < StaticBlockSizeThreshold ? EBlock::Literal :
					lz_buffer.data_size() < DynamicBlockSizeThreshold ? EBlock::Static :
					EBlock::Dynamic;

				// Output a block
				switch (block_type)
				{
				case EBlock::Literal:
					{
						// Output the block header (2 bits)
						PutBits(out, int(EBlock::Literal), 2);

						// Align to the next byte
						if (m_num_bits != 0)
							PutBits(out, 0, 8 - m_num_bits);

						// Output the data length and its 2s complement
						assert(lz_buffer.data_size() <= 0xFFFF);
						auto len = static_cast<uint16_t>(lz_buffer.data_size());
						PutBits(out, len, 16);
						PutBits(out, ~len, 16);

						// Output the literal data
						auto range = Range(pos - len, len);
						assert(dict.Available().contains(range) && "Literal data not in dictionary");
						for (auto i = range.begin(); i != range.end(); ++i)
							PutByte(out, dict[i]);

						break;
					}
				case EBlock::Static:
					{
						// Initialise the literal/lengths table
						HuffCodeTable lit_table(LitTableSize, 15);
						memset(&lit_table.m_code_size[  0], 8, 144);
						memset(&lit_table.m_code_size[144], 9, 112);
						memset(&lit_table.m_code_size[256], 7,  24);
						memset(&lit_table.m_code_size[280], 8,   8);
						lit_table.Populate(EBlock::Static, lit_counts);

						// Initialise the distance table
						HuffCodeTable dst_table(DstTableSize, 15);
						memset(&dst_table.m_code_size[0], 5, 32);
						dst_table.Populate(EBlock::Static, dst_counts);

						// Output the block header (2 bits)
						PutBits(out, int(EBlock::Static), 2);

						// Output the compressed data
						WriteCompressedData(out, lz_buffer, lit_table, dst_table);
						break;
					}
				case EBlock::Dynamic:
					{
						HuffCodeTable lit_table(LitTableSize, 15);
						HuffCodeTable dst_table(DstTableSize, 15);

						//needed? lit_counts[256] = 1;

						lit_table.Populate(EBlock::Dynamic, lit_counts);
						dst_table.Populate(EBlock::Dynamic, dst_counts);

						int num_lit_codes;
						for (num_lit_codes = 286; num_lit_codes > 257; --num_lit_codes)
							if (lit_table.m_code_size[num_lit_codes - 1] != 0)
								break;

						int num_dist_codes;
						for (num_dist_codes = 30; num_dist_codes > 1; num_dist_codes--)
							if (dst_table.m_code_size[num_dist_codes - 1])
								break;

						uint8_t code_sizes_to_pack[LitTableSize + DstTableSize];
						memcpy(&code_sizes_to_pack[0], &lit_table.m_code_size[0], num_lit_codes);
						memcpy(&code_sizes_to_pack[num_lit_codes], &dst_table.m_code_size[0], num_dist_codes);

						int total_code_sizes_to_pack = num_lit_codes + num_dist_codes;
						int rle_z_count = 0;
						int rle_repeat_count = 0;

						// Count the frequencies of the symbols
						SymCount dyn_count;
						uint8_t prev_code_size = 0xFF;
						int num_packed_code_sizes = 0;
						uint8_t packed_code_sizes[LitTableSize + DstTableSize];
						auto TDEFL_RLE_ZERO_CODE_SIZE = [&]
						{
							if (rle_z_count)
							{
								if (rle_z_count < 3)
								{
									dyn_count[0] = (uint16_t)(dyn_count[0] + rle_z_count);
									while (rle_z_count--)
										packed_code_sizes[num_packed_code_sizes++] = 0;
								}
								else if (rle_z_count <= 10)
								{
									dyn_count[17] = (uint16_t)(dyn_count[17] + 1);
									packed_code_sizes[num_packed_code_sizes++] = 17;
									packed_code_sizes[num_packed_code_sizes++] = (uint8_t)(rle_z_count - 3);
								}
								else
								{
									dyn_count[18] = (uint16_t)(dyn_count[18] + 1);
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
									dyn_count[prev_code_size] = (uint16_t)(dyn_count[prev_code_size] + rle_repeat_count);
									while (rle_repeat_count--)
										packed_code_sizes[num_packed_code_sizes++] = prev_code_size;
								}
								else
								{
									dyn_count[16] = (uint16_t)(dyn_count[16] + 1);
									packed_code_sizes[num_packed_code_sizes++] = 16;
									packed_code_sizes[num_packed_code_sizes++] = (uint8_t)(rle_repeat_count - 3);
								}
								rle_repeat_count = 0;
							}
						};
						for (int i = 0; i < total_code_sizes_to_pack; i++)
						{
							uint8_t code_size = code_sizes_to_pack[i];
							if (code_size == 0)
							{
								TDEFL_RLE_PREV_CODE_SIZE();
								if (++rle_z_count == 138)
								{
									TDEFL_RLE_ZERO_CODE_SIZE();
								}
							}
							else
							{
								TDEFL_RLE_ZERO_CODE_SIZE();
								if (code_size != prev_code_size)
								{
									TDEFL_RLE_PREV_CODE_SIZE();
									dyn_count[code_size] = (uint16_t)(dyn_count[code_size] + 1);
									packed_code_sizes[num_packed_code_sizes++] = code_size;
								}
								else if (++rle_repeat_count == 6)
								{
									TDEFL_RLE_PREV_CODE_SIZE();
								}
							}
							prev_code_size = code_size;
						}
						if (rle_repeat_count)
						{
							TDEFL_RLE_PREV_CODE_SIZE();
						}
						else
						{
							TDEFL_RLE_ZERO_CODE_SIZE();
						}

						HuffCodeTable dyn_table(DynTableSize, 7);
						dyn_table.Populate(EBlock::Dynamic, dyn_count);

						// Write a dynamic block header
						PutBits(out, int(EBlock::Dynamic), 2);

						// Write the sizes of the dynamic Huffman tables
						PutBits(out, num_lit_codes - 257, 5);
						PutBits(out, num_dist_codes - 1, 5);

						// Write the Huffman encoded code sizes
						static std::array<uint8_t, 19> const s_tdefl_packed_code_size_syms_swizzle = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

						int num_bit_lengths;
						for (num_bit_lengths = 18; num_bit_lengths >= 0; num_bit_lengths--)
							if (dyn_table.m_code_size[s_tdefl_packed_code_size_syms_swizzle[num_bit_lengths]])
								break;

						num_bit_lengths = std::max(4, (num_bit_lengths + 1));
						PutBits(out, num_bit_lengths - 4, 4);
						for (int i = 0; (int)i < num_bit_lengths; i++)
							PutBits(out, dyn_table.m_code_size[s_tdefl_packed_code_size_syms_swizzle[i]], 3);

						for (int packed_code_sizes_index = 0; packed_code_sizes_index < num_packed_code_sizes; )
						{
							auto code = packed_code_sizes[packed_code_sizes_index++];
							assert(code < DynTableSize);

							PutBits(out, dyn_table.m_code[code], dyn_table.m_code_size[code]);

							if (code >= 16)
								PutBits(out, packed_code_sizes[packed_code_sizes_index++], "\02\03\07"[code - 16]);
						}

						// Output the compressed data
						WriteCompressedData(out, lz_buffer, lit_table, dst_table);
						break;
					}
				case EBlock::Reserved:
				default:
					{
						throw std::runtime_error("Invalid output block type");
					}
				}
			}

			// Write the compressed data in 'lz_buffer' to the output, using the Huffman code tables for symbols and distances
			template <typename Out>
			void WriteCompressedData(OutIter<Out>& out, LZBuffer const& lz_buffer, HuffCodeTable const& lit_table, HuffCodeTable const& dst_table)
			{
				uint8_t flags = 0;
				for (auto ptr = lz_buffer.begin(); ptr != lz_buffer.end(); flags >>= 1)
				{
					// Every 8 loops, the next byte is the 'flags' byte
					if (((ptr - lz_buffer.begin()) & 7) == 0)
						flags = *ptr++;

					// If the LSB is 0, then the next byte is a literal
					if ((flags & 1) == 0)
					{
						// Write out the literal
						auto lit = *ptr++;
						assert(lit_table.m_code_size[lit] != 0 && "No Huffman code assigned to this value");
						PutBits(out, lit_table.m_code[lit], lit_table.m_code_size[lit]);
					}

					// Otherwise, this is a (length,distance) pair
					else
					{
						auto len = ptr[0];
						auto dst = ptr[1] | (ptr[2] << 8);
						ptr += 3;

						static std::array<uint32_t 17> const s_bitmasks = { 0x0000, 0x0001, 0x0003, 0x0007, 0x000F, 0x001F, 0x003F, 0x007F, 0x00FF, 0x01FF, 0x03FF, 0x07FF, 0x0FFF, 0x1FFF, 0x3FFF, 0x7FFF, 0xFFFF };

						// Write out the length value
						assert(lit_table.m_code_size[s_tdefl_len_sym[len]] != 0 && "No Huffman code assigned to this length value");
						PutBits(out, lit_table.m_code[s_tdefl_len_sym[len]], lit_table.m_code_size[s_tdefl_len_sym[len]]);
						PutBits(out, len & s_bitmasks[s_tdefl_len_extra[len]], s_tdefl_len_extra[len]);

						// Write out the distance value
						auto sym = dst <= 0x1FF
							? s_tdefl_small_dist_sym[dst >> 0]
							: s_tdefl_large_dist_sym[dst >> 8];
						auto extra = dst <= 0x1FF
							? s_tdefl_small_dist_extra[dst >> 0]
							: s_tdefl_large_dist_extra[dst >> 8];
						assert(dst_table.m_code_size[sym] != 0 && "No Huffman code assigned to this distance value");
						PutBits(out, dst_table.m_code[sym], dst_table.m_code_size[sym]);
						PutBits(out, dst & s_bitmasks[num_extra_bits], num_extra_bits);
					}
				}

				// Write the unused code
				PutBits(out, lit_table.m_code[256], lit_table.m_code_size[256]);
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

		struct tdefl_compressor
		{
			// Compress data in 'buf'
			tdefl_status tdefl_compress_buffer(void const* buf, size_t buf_size, tdefl_flush flush)
			{
				return tdefl_compress(buf, &buf_size, nullptr, nullptr, flush);
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
		static uint32_t const MaxIOBufferSize = 4096;//64 * 1024;

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
			// Level 6 corresponds to DefaultProbes or MZ_DEFAULT_LEVEL (but we can't depend on MZ_DEFAULT_LEVEL being available in case the zlib API's where #defined out)
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
			tdefl_init(pComp, tdefl_output_buffer_putter, &out_buf, s_tdefl_png_num_probes[std::min<mz_uint>(10, level)] | ECompressFlags::WriteZLibHeader);
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
			return m_prev_return_status;
		}
		uint32_t tdefl_get_adler32(tdefl_compressor* d)
		{
			return m_adler32;
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
				comp_flags &= ~MaxProbesMask;
			else if (strategy == ECompressionStrategy::Fixed)
				comp_flags |= ECompressionFlags::ForceAllStaticBlocks;
			else if (strategy == ECompressionStrategy::RLE)
				comp_flags |= ECompressionFlags::RLEMatches;

			return comp_flags;
		}


		#define TDEFL_READ_UNALIGNED_WORD(p) *(const uint16_t*)(p)

		tdefl_status tdefl_flush_output_buffer(tdefl_compressor* d)
		{
			if (m_pIn_buf_size)
			{
				*m_pIn_buf_size = m_pSrc - (const uint8_t*)m_pIn_buf;
			}

			if (m_pOut_buf_size)
			{
				auto n = std::min<mz_uint>(static_cast<mz_uint>(*m_pOut_buf_size - m_out_buf_ofs), m_output_flush_remaining);
				memcpy((uint8_t*)m_pOut_buf + m_out_buf_ofs, m_output_buf + m_output_flush_ofs, n);
				m_output_flush_ofs += (mz_uint)n;
				m_output_flush_remaining -= (mz_uint)n;
				m_out_buf_ofs += n;

				*m_pOut_buf_size = m_out_buf_ofs;
			}

			return (m_finished && !m_output_flush_remaining) ? TDEFL_STATUS_DONE : TDEFL_STATUS_OKAY;
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

		#endif

	private:

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
		static uint32_t Crc(void const* ptr, size_t buf_len, uint32_t crc = InitialCrc)
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
		static uint32_t Crc(std::ifstream& ifile, uint32_t crc = InitialCrc)
		{
			// Preserve the current file pointer position
			auto fpos = ifile.tellg();
			ifile.seekg(0);

			// Read from the file in blocks
			std::array<char, 4096> buf;
			for (;ifile.read(buf.data(), buf.size()).good();)
				crc = Crc(buf.data(), ifile.gcount(), crc);

			// Restoure the file pointer position
			ifile.seekg(fpos);
			return crc;
		}

		// Return 'value' with 'length' bits reversed
		template <typename TInt>
		static TInt ReverseBits(TInt value, int length)
		{
			assert(length <= sizeof(TInt) * 8);

			TInt reversed = 0;
			for (; length-- != 0;)
			{
				reversed = (reversed << 1) | (value & 1);
				value >>= 1;
			}
			return reversed;
		}

		// CompileTime accumulative hash 
		static uint64_t const FNV_prime64 = 1099511628211ULL;
		static uint64_t const FNV_offset_basis64 = 14695981039346656037ULL;
		static constexpr uint64_t Hash64CT(uint64_t ch, uint64_t h = FNV_offset_basis64)
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

		// Append bytes to a vector of bytes
		static void append(vector_t<uint8_t>& vec, void const* beg, void const* end)
		{
			vec.insert(vec.end(), static_cast<uint8_t const*>(beg), static_cast<uint8_t const*>(end));
		}
		static void append(vector_t<uint8_t>& vec, std::span<uint8_t const> bytes)
		{
			vec.insert(vec.end(), bytes.begin(), bytes.end());
		}
		static void append(vector_t<uint8_t>& vec, std::string_view str)
		{
			vec.insert(vec.end(), str.data(), str.data() + str.size());
		}

		// Helper for detecting data lost when casting
		template <typename T, typename U> static constexpr T checked_cast(U x)
		{
			assert(static_cast<U>(static_cast<T>(x)) == x && "Cast loses data");
			return static_cast<T>(x);
		}

		// True if 'ofs' is an aligned offset in the output stream
		template <typename T> bool is_aligned(T ofs) const
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

		// Integer divide by 3 with round up
		static constexpr int Div3(int x) { return (x + 2) / 3; }

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

	private:

		// Purposely making these tables static for faster init and thread safety.
		inline static std::array<uint16_t, 256> const s_tdefl_len_sym =
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
		inline static std::array<uint8_t, 256> const s_tdefl_len_extra =
		{
			0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
			4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 0,
		};
		inline static std::array<uint8_t, 512> const s_tdefl_small_dist_sym =
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
		inline static std::array<uint8_t, 512> const s_tdefl_small_dist_extra =
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
		inline static std::array<uint8_t, 128> const s_tdefl_large_dist_sym =
		{
			0, 0, 18, 19, 20, 20, 21, 21, 22, 22, 22, 22, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24, 25, 25, 25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
			26, 26, 26, 26, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
			28, 28, 28, 28, 28, 28, 28, 28, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
		};
		inline static std::array<uint8_t, 128> const s_tdefl_large_dist_extra =
		{
			0, 0, 8, 8, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
			12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
			13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
		};
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

		// Write a test zip file
		{
			zip::ZipArchive z;
			z.Add("binary-00-0f.bin", path / "binary-00-0F.bin");

			//std::basic_ifstream<uint8_t> zip(path / "binary-00-0F.zip");
			//std::basic_stringstream<uint8_t> zip_bytes;
			//zip_bytes << zip.rdbuf();
			//PR_CHECK(zip == zip_bytes.str(), true);

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
