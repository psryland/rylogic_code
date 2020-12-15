//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
// Simple tool for building wave banks from 1 or more .WAV files.
// This generates binary wave banks compliant with XACT 3's Wave Bank .XWB format.
// The .WAV files are not format converted or compressed.
//
// For a more full-featured builder, see XACT 3 and the XACTBLD tool in the legacy
// DirectX SDK (June 2010) release.
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// http://go.microsoft.com/fwlink/?LinkId=248929

#include "pr/audio/forward.h"
#include "pr/audio/waves/wave_bank.h"
#include "pr/audio/waves/wave_file.h"

#pragma warning(disable:4201) // nameless structs

namespace pr::audio
{
	static const uint32_t DVD_SECTOR_SIZE = 2048;
	static const uint32_t DVD_BLOCK_SIZE = DVD_SECTOR_SIZE * 16;

	static const uint32_t ALIGNMENT_MIN = 4;
	static const uint32_t ALIGNMENT_DVD = DVD_SECTOR_SIZE;

	static const size_t MAX_DATA_SEGMENT_SIZE = 0xFFFFFFFF;
	static const size_t MAX_COMPACT_DATA_SEGMENT_SIZE = 0x001FFFFF;

	static const int NAME_LENGTH = 64;
	static const DWORD XACT_CONTENT_VERSION = 46; // DirectX SDK (June 2010)

	#pragma pack(push, 1)
	struct REGION
	{
		uint32_t    dwOffset;   // Region offset, in bytes.
		uint32_t    dwLength;   // Region length, in bytes.
	};
	struct SAMPLEREGION
	{
		uint32_t    dwStartSample;  // Start sample for the region.
		uint32_t    dwTotalSamples; // Region length in samples.
	};
	struct HEADER
	{
		static const uint32_t SIGNATURE = 'DNBW';
		static const uint32_t BE_SIGNATURE = 'WBND';
		static const uint32_t VERSION = 44;

		enum SEGIDX
		{
			SEGIDX_BANKDATA = 0,       // Bank data
			SEGIDX_ENTRYMETADATA,      // Entry meta-data
			SEGIDX_SEEKTABLES,         // Storage for seek tables for the encoded waves.
			SEGIDX_ENTRYNAMES,         // Entry friendly names
			SEGIDX_ENTRYWAVEDATA,      // Entry wave data
			SEGIDX_COUNT
		};

		uint32_t    dwSignature;            // File signature
		uint32_t    dwVersion;              // Version of the tool that created the file
		uint32_t    dwHeaderVersion;        // Version of the file format
		REGION      Segments[SEGIDX_COUNT]; // Segment lookup table
	};
	union MINIWAVEFORMAT
	{
		static const uint32_t TAG_PCM   = 0x0;
		static const uint32_t TAG_XMA   = 0x1;
		static const uint32_t TAG_ADPCM = 0x2;
		static const uint32_t TAG_WMA   = 0x3;

		static const uint32_t BITDEPTH_8 = 0x0; // PCM only
		static const uint32_t BITDEPTH_16 = 0x1; // PCM only

		static const size_t ADPCM_BLOCKALIGN_CONVERSION_OFFSET = 22;
    
		struct
		{
			uint32_t wFormatTag      : 2;        // Format tag
			uint32_t nChannels       : 3;        // Channel count (1 - 6)
			uint32_t nSamplesPerSec  : 18;       // Sampling rate
			uint32_t wBlockAlign     : 8;        // Block alignment.  For WMA, lower 6 bits block alignment index, upper 2 bits bytes-per-second index.
			uint32_t wBitsPerSample  : 1;        // Bits per sample (8 vs. 16, PCM only); WMAudio2/WMAudio3 (for WMA)
		};
		uint32_t dwValue;

		WORD BitsPerSample() const
		{
			if (wFormatTag == TAG_XMA)
				return XMA_OUTPUT_SAMPLE_BITS;
			if (wFormatTag == TAG_WMA)
				return 16;
			if (wFormatTag == TAG_ADPCM)
				return MSADPCM_BITS_PER_SAMPLE;

			// wFormatTag must be TAG_PCM (2 bits can only represent 4 different values)
			return (wBitsPerSample == BITDEPTH_16) ? 16 : 8;
		}
		DWORD BlockAlign() const
		{
			switch (wFormatTag)
			{
			case TAG_PCM:
				return wBlockAlign;
            
			case TAG_XMA:
				return (nChannels * XMA_OUTPUT_SAMPLE_BITS / 8);

			case TAG_ADPCM:
				return (wBlockAlign + ADPCM_BLOCKALIGN_CONVERSION_OFFSET) * nChannels;

			case TAG_WMA:
				{
					static const uint32_t aWMABlockAlign[] =
					{
						929,
						1487,
						1280,
						2230,
						8917,
						8192,
						4459,
						5945,
						2304,
						1536,
						1485,
						1008,
						2731,
						4096,
						6827,
						5462,
						1280
					};

					uint32_t dwBlockAlignIndex = wBlockAlign & 0x1F;
					if ( dwBlockAlignIndex < _countof(aWMABlockAlign) )
						return aWMABlockAlign[dwBlockAlignIndex];
				}
				break;
			}

			return 0;
		}
		DWORD AvgBytesPerSec() const
		{
			switch (wFormatTag)
			{
			case TAG_PCM:
				return nSamplesPerSec * wBlockAlign;

			case TAG_XMA:
				return nSamplesPerSec * BlockAlign();

			case TAG_ADPCM:
				{
					uint32_t blockAlign = BlockAlign();
					uint32_t samplesPerAdpcmBlock = AdpcmSamplesPerBlock();
					return blockAlign * nSamplesPerSec / samplesPerAdpcmBlock;
				}
				break;

			case TAG_WMA:
				{
					static const uint32_t aWMAAvgBytesPerSec[] =
					{
						12000,
						24000,
						4000,
						6000,
						8000,
						20000,
						2500
					};
					// bitrate = entry * 8

					uint32_t dwBytesPerSecIndex = wBlockAlign >> 5;
					if ( dwBytesPerSecIndex < _countof(aWMAAvgBytesPerSec) )
						return aWMAAvgBytesPerSec[dwBytesPerSecIndex];
				}
				break;
			}

			return 0;
		}
		DWORD AdpcmSamplesPerBlock() const
		{
			uint32_t nBlockAlign = (wBlockAlign + ADPCM_BLOCKALIGN_CONVERSION_OFFSET) * nChannels;
			return nBlockAlign * 2 / (uint32_t)nChannels - 12;
		}
		void AdpcmFillCoefficientTable(ADPCMWAVEFORMAT *fmt) const
		{
			// These are fixed since we are always using MS ADPCM
			fmt->wNumCoef = MSADPCM_NUM_COEFFICIENTS;

			static ADPCMCOEFSET aCoef[7] = { { 256, 0}, {512, -256}, {0,0}, {192,64}, {240,0}, {460, -208}, {392,-232} };
			memcpy( &fmt->aCoef, aCoef, sizeof(aCoef) );
		}
	};
	struct NAME
	{
		char str[NAME_LENGTH];

		NAME(char const* name = nullptr)
			:str()
		{
			if (name)
				strncpy_s(str, name, _countof(str));
		}
	};
	struct BANKDATA
	{
		static const uint32_t TYPE_BUFFER = 0x00000000;
		static const uint32_t TYPE_STREAMING = 0x00000001;
		static const uint32_t TYPE_MASK = 0x00000001;

		static const uint32_t FLAGS_ENTRYNAMES = 0x00010000;
		static const uint32_t FLAGS_COMPACT = 0x00020000;
		static const uint32_t FLAGS_SYNC_DISABLED = 0x00040000;
		static const uint32_t FLAGS_SEEKTABLES = 0x00080000;
		static const uint32_t FLAGS_MASK = 0x000F0000;

		uint32_t        dwFlags;                    // Bank flags
		uint32_t        dwEntryCount;               // Number of entries in the bank
		NAME            bankName;                   // Bank friendly name
		uint32_t        dwEntryMetaDataElementSize; // Size of each entry meta-data element, in bytes
		uint32_t        dwEntryNameElementSize;     // Size of each entry name element, in bytes
		uint32_t        alignment;                  // Entry alignment, in bytes
		MINIWAVEFORMAT  CompactFormat;              // Format data for compact bank
		FILETIME        BuildTime;                  // Build timestamp
	};
	struct ENTRY
	{
		static const uint32_t FLAGS_READAHEAD = 0x00000001;     // Enable stream read-ahead
		static const uint32_t FLAGS_LOOPCACHE = 0x00000002;     // One or more looping sounds use this wave
		static const uint32_t FLAGS_REMOVELOOPTAIL = 0x00000004;// Remove data after the end of the loop region
		static const uint32_t FLAGS_IGNORELOOP = 0x00000008;    // Used internally when the loop region can't be used
		static const uint32_t FLAGS_MASK = 0x00000008;

		union
		{
			struct
			{
				// Entry flags
				uint32_t                   dwFlags  :  4;

				// Duration of the wave, in units of one sample.
				// For instance, a ten second long wave sampled
				// at 48KHz would have a duration of 480,000.
				// This value is not affected by the number of
				// channels, the number of bits per sample, or the
				// compression format of the wave.
				uint32_t                   Duration : 28;
			};
			uint32_t dwFlagsAndDuration;
		};

		MINIWAVEFORMAT  Format;         // Entry format.
		REGION          PlayRegion;     // Region within the wave data segment that contains this entry.
		SAMPLEREGION    LoopRegion;     // Region within the wave data (in samples) that should loop.
	};
	struct ENTRYCOMPACT
	{
		uint32_t       dwOffset            : 21;       // Data offset, in multiplies of the bank alignment
		uint32_t       dwLengthDeviation   : 11;       // Data length deviation, in bytes

		void ComputeLocations( DWORD& offset, DWORD& length, uint32_t index, const HEADER& header, const BANKDATA& data, const ENTRYCOMPACT* entries ) const
		{
			offset = dwOffset * data.alignment;

			if ( index < ( data.dwEntryCount - 1 ) )
			{
				length = ( entries[index + 1].dwOffset * data.alignment ) - offset - dwLengthDeviation;
			}
			else
			{
				length = header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwLength - offset - dwLengthDeviation;
			}
		}
		static uint32_t GetDuration(DWORD length, const BANKDATA& data, const uint32_t* seekTable)
		{
			switch (data.CompactFormat.wFormatTag)
			{
			case MINIWAVEFORMAT::TAG_ADPCM:
				{
					uint32_t duration = (length / data.CompactFormat.BlockAlign()) * data.CompactFormat.AdpcmSamplesPerBlock();
					uint32_t partial = length % data.CompactFormat.BlockAlign();
					if (partial)
					{
						if (partial >= (7 * data.CompactFormat.nChannels))
							duration += (partial * 2 / data.CompactFormat.nChannels - 12);
					}
					return duration;
				}

			case MINIWAVEFORMAT::TAG_WMA:
				if (seekTable)
				{
					uint32_t seekCount = *seekTable;
					if (seekCount > 0)
					{
						return seekTable[seekCount] / uint32_t(2 * data.CompactFormat.nChannels);
					}
				}
				return 0;

			case MINIWAVEFORMAT::TAG_XMA:
				if (seekTable)
				{
					uint32_t seekCount = *seekTable;
					if (seekCount > 0)
					{
						return seekTable[seekCount];
					}
				}
				return 0;

			default:
				return uint32_t((uint64_t(length) * 8)
					/ uint64_t(data.CompactFormat.BitsPerSample() * data.CompactFormat.nChannels));
			}
		}
	};
	#pragma pack(pop)

	static_assert(sizeof(REGION) == 8, "Mismatch with xact3wb.h");
	static_assert(sizeof(SAMPLEREGION) == 8, "Mismatch with xact3wb.h");
	static_assert(sizeof(HEADER) == 52, "Mismatch with xact3wb.h");
	static_assert(sizeof(ENTRY) == 24, "Mismatch with xact3wb.h");
	static_assert(sizeof(MINIWAVEFORMAT) == 4, "Mismatch with xact3wb.h");
	static_assert(sizeof(ENTRY) == 24, "Mismatch with xact3wb.h");
	static_assert(sizeof(ENTRYCOMPACT) == 4, "Mismatch with xact3wb.h");
	static_assert(sizeof(BANKDATA) == 96, "Mismatch with xact3wb.h");

	struct handle_closer
	{
		void operator()(HANDLE h) { if (h) CloseHandle(h); }
	};
	using ScopedHandle = std::unique_ptr<void, handle_closer>;
	inline HANDLE safe_handle(HANDLE h)
	{
		return (h == INVALID_HANDLE_VALUE) ? 0 : h;
	}

	#pragma region Wave Bank Reader
		
	struct WaveBankReader::Impl
	{
		using NameMap = std::unordered_map<std::string, uint32_t>;

		ScopedHandle m_async;
		ScopedHandle m_event;
		OVERLAPPED   m_request;
		bool         m_prepared;

		HEADER   m_header;
		BANKDATA m_data;
		NameMap  m_names;

		std::unique_ptr<uint8_t[]> m_entries;
		std::unique_ptr<uint8_t[]> m_seek_data;
		std::unique_ptr<uint8_t[]> m_wave_data;

		Impl()
			:m_async(INVALID_HANDLE_VALUE)
			,m_event(INVALID_HANDLE_VALUE)
			,m_prepared()
			,m_header()
			,m_data()
			,m_names()
		{}
		~Impl()
		{
			Close();
		}

		// Reset this object
		void Clear()
		{
			memset(&m_header, 0, sizeof(HEADER));
			memset(&m_data, 0, sizeof(BANKDATA));

			m_names.clear();
			m_entries.reset();
			m_seek_data.reset();
			m_wave_data.reset();

			m_async = nullptr;
			m_event = nullptr;
			m_prepared = false;
		}

		// Open a wave bank file
		void Open(std::filesystem::path const& filepath)
		{
			Close();
			Clear();

			// Create an event used for overlapped file IO
			m_event.reset(safe_handle(CreateEventExW(nullptr, nullptr, CREATE_EVENT_MANUAL_RESET, EVENT_MODIFY_STATE | SYNCHRONIZE)));
			if (!m_event)
				pr::Throw(HRESULT_FROM_WIN32(GetLastError()));

			// Open the wave bank file
			auto params = CREATEFILE2_EXTENDED_PARAMETERS{sizeof(CREATEFILE2_EXTENDED_PARAMETERS), 0};
			params.dwFileAttributes = FILE_ATTRIBUTE_NORMAL;
			params.dwFileFlags = FILE_FLAG_OVERLAPPED | FILE_FLAG_SEQUENTIAL_SCAN;
			ScopedHandle hFile(safe_handle(CreateFile2(filepath.c_str(), GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, &params)));
			if (!hFile)
				pr::Throw(HRESULT_FROM_WIN32(GetLastError()));

			// Read and verify header
			{
				OVERLAPPED request = {0};
				request.hEvent = m_event.get();
				if (!ReadFile(hFile.get(), &m_header, sizeof(m_header), nullptr, &request))
				{
					auto error = GetLastError();
					if (error != ERROR_IO_PENDING)
						pr::Throw(HRESULT_FROM_WIN32(error));
				}

				// Get the results from the file read
				DWORD bytes;
				auto result = GetOverlappedResultEx(hFile.get(), &request, &bytes, INFINITE, FALSE);
				if (!result || (bytes != sizeof(m_header)))
					pr::Throw(HRESULT_FROM_WIN32(GetLastError()));

				// Verify the header
				if (m_header.dwSignature == HEADER::BE_SIGNATURE)
					pr::Throw(HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED), "Big-endian wave banks are not supported");
				if (m_header.dwSignature != HEADER::SIGNATURE)
					pr::Throw(E_FAIL, "Invalid header signature");
				if (m_header.dwHeaderVersion != HEADER::VERSION)
					pr::Throw(E_FAIL, "Unsupported header version");
			}

			// Load bank data
			if (m_header.Segments[HEADER::SEGIDX_BANKDATA].dwLength != 0)
			{
				OVERLAPPED request = {0};
				request.hEvent = m_event.get();
				request.Offset = m_header.Segments[HEADER::SEGIDX_BANKDATA].dwOffset;
				if (!ReadFile(hFile.get(), &m_data, sizeof(m_data), nullptr, &request))
				{
					auto error = GetLastError();
					if (error != ERROR_IO_PENDING)
						pr::Throw(HRESULT_FROM_WIN32(error));
				}

				// Get the results from the file read
				DWORD bytes;
				auto result = GetOverlappedResultEx(hFile.get(), &request, &bytes, INFINITE, FALSE);
				if (!result || (bytes != sizeof(m_data)))
					pr::Throw(HRESULT_FROM_WIN32(GetLastError()));
				if (m_data.dwEntryCount == 0)
					pr::Throw(HRESULT_FROM_WIN32(ERROR_NO_DATA));

				// Verify the data
				if (m_data.dwFlags & BANKDATA::FLAGS_COMPACT)
				{
					if (m_data.dwEntryMetaDataElementSize != sizeof(ENTRYCOMPACT))
						pr::Throw(E_FAIL, "Unsupported entry meta data element size");
					if (m_header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwLength > MAX_COMPACT_DATA_SEGMENT_SIZE)
						pr::Throw(E_FAIL, "Data segment is too large to be valid compact wave bank");
				}
				else
				{
					if (m_data.dwEntryMetaDataElementSize != sizeof(ENTRY))
						pr::Throw(E_FAIL, "Unsupported entry meta data element size");
				}

				auto meta_data_bytes = m_header.Segments[HEADER::SEGIDX_ENTRYMETADATA].dwLength;
				if (meta_data_bytes != (m_data.dwEntryCount * m_data.dwEntryMetaDataElementSize))
					pr::Throw(E_FAIL, "Invalid meta data size");

				if (m_data.dwFlags & BANKDATA::TYPE_STREAMING)
				{
					if (m_data.alignment < ALIGNMENT_DVD)
						pr::Throw(E_FAIL, "Invalid data alignment");
				}
				else if (m_data.alignment < ALIGNMENT_MIN)
				{
					pr::Throw(E_FAIL, "Invalid data alignment");
				}
			}

			// Load names
			if (m_header.Segments[HEADER::SEGIDX_ENTRYNAMES].dwLength != 0)
			{
				auto names_bytes = m_header.Segments[HEADER::SEGIDX_ENTRYNAMES].dwLength;
				if (names_bytes < m_data.dwEntryNameElementSize * m_data.dwEntryCount)
					pr::Throw(E_FAIL, "Invalid names chunk");

				// Allocate a buffer to read the names into
				std::unique_ptr<char[]> temp(new char[names_bytes]);

				// Read the names chunk
				OVERLAPPED request = {0};
				request.hEvent = m_event.get();
				request.Offset = m_header.Segments[HEADER::SEGIDX_ENTRYNAMES].dwOffset;
				if (!ReadFile(hFile.get(), temp.get(), names_bytes, nullptr, &request))
				{
					auto error = GetLastError();
					if (error != ERROR_IO_PENDING)
						pr::Throw(HRESULT_FROM_WIN32(error));
				}

				// Get the read results
				DWORD bytes;
				auto result = GetOverlappedResultEx(hFile.get(), &request, &bytes, INFINITE, FALSE);
				if (!result || (names_bytes != bytes))
					pr::Throw(HRESULT_FROM_WIN32(GetLastError()));

				// Create a map from name to wave index
				for (uint32_t j = 0; j != m_data.dwEntryCount; ++j)
				{
					auto n = m_data.dwEntryNameElementSize * j;

					char name[64] = { 0 };
					strncpy_s(name, &temp.get()[n], 64);
					m_names[name] = j;
				}
			}

			// Load wave entries
			if (m_header.Segments[HEADER::SEGIDX_ENTRYMETADATA].dwLength != 0)
			{
				// Allocate the entry table
				if (m_data.dwFlags & BANKDATA::FLAGS_COMPACT)
					m_entries.reset(reinterpret_cast<uint8_t*>(new ENTRYCOMPACT[m_data.dwEntryCount]));
				else
					m_entries.reset(reinterpret_cast<uint8_t*>(new ENTRY[m_data.dwEntryCount]));

				// Read the entry table
				OVERLAPPED request = {0};
				request.hEvent = m_event.get();
				request.Offset = m_header.Segments[HEADER::SEGIDX_ENTRYMETADATA].dwOffset;
				auto meta_data_bytes = m_header.Segments[HEADER::SEGIDX_ENTRYMETADATA].dwLength;
				if (!ReadFile(hFile.get(), m_entries.get(), meta_data_bytes, nullptr, &request))
				{
					auto error = GetLastError();
					if (error != ERROR_IO_PENDING)
						pr::Throw(HRESULT_FROM_WIN32(error));
				}

				// Get the read results
				DWORD bytes;
				auto result = GetOverlappedResultEx(hFile.get(), &request, &bytes, INFINITE, FALSE);
				if (!result || (meta_data_bytes != bytes))
					pr::Throw(HRESULT_FROM_WIN32(GetLastError()));
			}

			// Load seek tables (XMA2 / xWMA)
			if (m_header.Segments[HEADER::SEGIDX_SEEKTABLES].dwLength != 0)
			{
				// Allocate the seek table
				auto seek_len = m_header.Segments[HEADER::SEGIDX_SEEKTABLES].dwLength;
				m_seek_data.reset(new uint8_t[seek_len]);

				// Read the seek table
				OVERLAPPED request = {0};
				request.hEvent = m_event.get();
				request.Offset = m_header.Segments[HEADER::SEGIDX_SEEKTABLES].dwOffset;
				if (!ReadFile(hFile.get(), m_seek_data.get(), seek_len, nullptr, &request))
				{
					auto error = GetLastError();
					if (error != ERROR_IO_PENDING)
						pr::Throw(HRESULT_FROM_WIN32(error));
				}

				// Get the read results
				DWORD bytes;
				auto result = GetOverlappedResultEx(hFile.get(), &request, &bytes, INFINITE, FALSE);
				if (!result || (seek_len != bytes))
					pr::Throw(HRESULT_FROM_WIN32(GetLastError()));
			}

			// Set up the file handle to read the wave data
			if (m_header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwLength != 0)
			{
				auto wave_len = m_header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwLength;
				if (m_data.dwFlags & BANKDATA::TYPE_STREAMING)
				{
					// If streaming, reopen without buffering
					hFile.reset();

					params = CREATEFILE2_EXTENDED_PARAMETERS{sizeof(CREATEFILE2_EXTENDED_PARAMETERS), 0};
					params.dwFileAttributes = FILE_ATTRIBUTE_NORMAL;
					params.dwFileFlags = FILE_FLAG_OVERLAPPED | FILE_FLAG_NO_BUFFERING;
					params.dwSecurityQosFlags = SECURITY_IMPERSONATION;
					m_async.reset(safe_handle(CreateFile2(filepath.c_str(), GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, &params)));
					if (!m_async)
						pr::Throw(HRESULT_FROM_WIN32(GetLastError()));

					m_prepared = true;
				}
				else
				{
					// If in-memory, kick off read of wave data
					m_wave_data.reset(new uint8_t[wave_len]);

					// Read the wave data into memory
					m_request = OVERLAPPED{};
					m_request.hEvent = m_event.get();
					m_request.Offset = m_header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwOffset;
					if (!ReadFile(hFile.get(), m_wave_data.get(), wave_len, nullptr, &m_request))
					{
						auto error = GetLastError();
						if (error != ERROR_IO_PENDING)
							pr::Throw(HRESULT_FROM_WIN32(error));
					}
					else
					{
						m_prepared = true;
						m_request = OVERLAPPED{};
					}

					m_async.reset(hFile.release());
				}
			}
		}
		void Close()
		{
			if (m_async)
			{
				if (m_request.hEvent != 0)
				{
					DWORD bytes;
					(void)GetOverlappedResultEx(m_async.get(), &m_request, &bytes, INFINITE, FALSE);
				}
				m_async = nullptr;
			}
			if (m_event)
			{
				m_event = nullptr;
			}
		}

		// Get the format of a wave in the bank
		void GetFormat(int index, WaveFormatsU& format) const
		{
			if (index < 0 || index >= int(m_data.dwEntryCount) || !m_entries)
				pr::Throw(E_FAIL);

			memset(&format, 0, sizeof(format));

			auto& miniFmt = (m_data.dwFlags & BANKDATA::FLAGS_COMPACT) ? m_data.CompactFormat : (reinterpret_cast<const ENTRY*>(m_entries.get())[index].Format);
			switch (miniFmt.wFormatTag)
			{
			case MINIWAVEFORMAT::TAG_PCM:
				{
					format.m_wfx.wFormatTag = WORD(EWaveFormat::PCM);
					break;
				}
			case MINIWAVEFORMAT::TAG_ADPCM:
				{
					format.m_wfx.wFormatTag = WORD(EWaveFormat::ADPCM);
					format.m_wfx.cbSize = MSADPCM_FORMAT_EXTRA_BYTES;
					{
						auto adpcmFmt = reinterpret_cast<ADPCMWAVEFORMAT*>(&format);
						adpcmFmt->wSamplesPerBlock = (WORD)miniFmt.AdpcmSamplesPerBlock();
						miniFmt.AdpcmFillCoefficientTable(adpcmFmt);
					}
					break;
				}
			case MINIWAVEFORMAT::TAG_WMA: // xWMA is supported by XAudio 2.7 and by XBox One
				{
					format.m_wfx.wFormatTag = WORD((miniFmt.wBitsPerSample & 0x1) ? EWaveFormat::WMAUDIO3 : EWaveFormat::WMAUDIO2);
					format.m_wfx.cbSize = 0;
					break;
				}
			case MINIWAVEFORMAT::TAG_XMA: // XMA2 is supported by XBox One
				{
					pr::Throw(HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED));
				}
			default:
				{
					pr::Throw(E_FAIL, "Unsupported format");
				}
			}

			format.m_wfx.nChannels       = miniFmt.nChannels;
			format.m_wfx.wBitsPerSample  = miniFmt.BitsPerSample();
			format.m_wfx.nBlockAlign     = (WORD) miniFmt.BlockAlign();
			format.m_wfx.nSamplesPerSec  = miniFmt.nSamplesPerSec;
			format.m_wfx.nAvgBytesPerSec = miniFmt.AvgBytesPerSec();
		}

		// Get a pointer to the wave data for a wave in the bank
		void GetWaveData(int index, uint8_t const*& pData, uint32_t& dataSize) const
		{
			if (index < 0 || index >= int(m_data.dwEntryCount) || !m_entries || !m_wave_data)
				pr::Throw(E_FAIL, "index out of bounds");

			if (m_data.dwFlags & BANKDATA::TYPE_STREAMING)
				pr::Throw(HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED));

			if (!m_prepared)
				pr::Throw(HRESULT_FROM_WIN32(ERROR_IO_INCOMPLETE), "Still loading wave data");

			if (m_data.dwFlags & BANKDATA::FLAGS_COMPACT)
			{
				auto& entry = reinterpret_cast<const ENTRYCOMPACT*>(m_entries.get())[index];

				DWORD dwOffset, dwLength;
				entry.ComputeLocations(dwOffset, dwLength, index, m_header, m_data, reinterpret_cast<const ENTRYCOMPACT*>(m_entries.get()));
				if ((dwOffset + dwLength) > m_header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwLength)
					pr::Throw(HRESULT_FROM_WIN32(ERROR_HANDLE_EOF));

				pData = &m_wave_data.get()[dwOffset];
				dataSize = dwLength;
			}
			else
			{
				auto& entry = reinterpret_cast<const ENTRY*>(m_entries.get())[index];
				if ((entry.PlayRegion.dwOffset + entry.PlayRegion.dwLength) > m_header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwLength)
					pr::Throw(HRESULT_FROM_WIN32(ERROR_HANDLE_EOF));

				pData = &m_wave_data.get()[entry.PlayRegion.dwOffset];
				dataSize = entry.PlayRegion.dwLength;
			}
		}

		// Get a pointer to the seek table in the bank
		void GetSeekTable(int index, uint32_t const*& pData, uint32_t& data_count, uint32_t& tag) const
		{
			pData = nullptr;
			data_count = 0;
			tag = 0;

			if (index < 0 || index >= int(m_data.dwEntryCount) || !m_entries)
				pr::Throw(E_FAIL, "index out of bounds");

			if (!m_seek_data)
				return;

			auto& miniFmt = (m_data.dwFlags & BANKDATA::FLAGS_COMPACT) ? m_data.CompactFormat : (reinterpret_cast<const ENTRY*>(m_entries.get())[index].Format);
			switch (miniFmt.wFormatTag)
			{
			case MINIWAVEFORMAT::TAG_WMA:
				tag = WORD((miniFmt.wBitsPerSample & 0x1) ? EWaveFormat::WMAUDIO3 : EWaveFormat::WMAUDIO2);
				break;

			case MINIWAVEFORMAT::TAG_XMA:
				tag = (uint32_t)EWaveFormat::XMA2;
				break;

			default:
				return;
			}

			auto seekTable = FindSeekTable(index, m_seek_data.get(), m_header, m_data);
			if (!seekTable)
				return;

			data_count = *seekTable;
			pData = seekTable + 1;
		}

		// Get the meta data for a wave in the bank
		void GetMetadata(int index, Metadata& metadata) const
		{
			if (index < 0 || index >= int(m_data.dwEntryCount) || !m_entries)
				pr::Throw(E_FAIL, "index out of bounds");

			if (m_data.dwFlags & BANKDATA::FLAGS_COMPACT)
			{
				auto& entry = reinterpret_cast<const ENTRYCOMPACT*>(m_entries.get())[index];

				DWORD dwOffset, dwLength;
				entry.ComputeLocations(dwOffset, dwLength, index, m_header, m_data, reinterpret_cast<const ENTRYCOMPACT*>(m_entries.get()));

				auto seekTable = FindSeekTable(index, m_seek_data.get(), m_header, m_data);
				metadata.duration = entry.GetDuration(dwLength, m_data, seekTable);
				metadata.loopStart = metadata.loopLength = 0;
				metadata.offsetBytes = dwOffset;
				metadata.lengthBytes = dwLength;
			}
			else
			{
				auto& entry = reinterpret_cast<const ENTRY*>(m_entries.get())[index];

				metadata.duration = entry.Duration;
				metadata.loopStart = entry.LoopRegion.dwStartSample;
				metadata.loopLength = entry.LoopRegion.dwTotalSamples;
				metadata.offsetBytes = entry.PlayRegion.dwOffset;
				metadata.lengthBytes = entry.PlayRegion.dwLength;
			}
		}

		// Return a pointer to the seek table
		uint32_t const* FindSeekTable(int index, uint8_t const* seekTable, HEADER const& header, BANKDATA const& data) const
		{
			if (!seekTable || index >= int(data.dwEntryCount))
				return nullptr;

			uint32_t seekSize = header.Segments[HEADER::SEGIDX_SEEKTABLES].dwLength;
			if ((index * sizeof(uint32_t)) > seekSize)
				return nullptr;

			auto table = reinterpret_cast<const uint32_t*>(seekTable);
			uint32_t offset = table[index];
			if (offset == uint32_t(-1))
				return nullptr;

			offset += sizeof(uint32_t) * data.dwEntryCount;
			if (offset > seekSize)
				return nullptr;

			return reinterpret_cast<const uint32_t*>(seekTable + offset);
		}

		// Update the prepared state flag, returns true when prepared
		bool UpdatePrepared()
		{
			if (m_prepared)
				return true;

			if (!m_async)
				return false;

			if (m_request.hEvent != 0)
			{
				DWORD bytes;
				auto result = GetOverlappedResultEx(m_async.get(), &m_request, &bytes, 0, FALSE);
				if (result)
				{
					m_prepared = true;
					m_request = OVERLAPPED{};
				}
			}
			return m_prepared;
		}
	};

	WaveBankReader::WaveBankReader()
		:pImpl(new Impl)
	{}

	// Open a wave bank
	void WaveBankReader::Open(std::filesystem::path const& filepath)
	{
		return pImpl->Open(filepath);
	}

	// True if the non-streaming wave bank is completed loaded into memory
	bool WaveBankReader::IsPrepared()
	{
		return pImpl->UpdatePrepared();
	}

	// Block until the non streaming bank is fully loaded into memory
	void WaveBankReader::WaitOnPrepare()
	{
		if (pImpl->m_prepared)
			return;

		if (pImpl->m_request.hEvent != 0)
		{
			WaitForSingleObjectEx(pImpl->m_request.hEvent, INFINITE, FALSE);
			pImpl->UpdatePrepared();
		}
	}
		
	// True if the wave bank has names for each wave
	bool WaveBankReader::HasNames() const
	{
		return !pImpl->m_names.empty();
	}

	// Look up the index of a wave with the given name (-1 if not found)
	int WaveBankReader::Find(const char* name) const
	{
		auto it = pImpl->m_names.find(name);
		if (it != std::end(pImpl->m_names))
			return it->second;

		return -1;
	}

	// True if this bank supports streaming
	bool WaveBankReader::IsStreamingBank() const
	{
		return (pImpl->m_data.dwFlags & BANKDATA::TYPE_STREAMING) != 0;
	}

	// The name of this wave bank
	const char* WaveBankReader::BankName() const
	{
		return pImpl->m_data.bankName.str;
	}

	// The number of waves in this wave bank
	int WaveBankReader::Count() const
	{
		return pImpl->m_data.dwEntryCount;
	}

	// The size of the audio data
	uint32_t WaveBankReader::BankAudioSize() const
	{
		return pImpl->m_header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwLength;
	}

	// Get the format of a wave in the bank
	void WaveBankReader::GetFormat(int index, WaveFormatsU& format) const
	{
		return pImpl->GetFormat(index, format);
	}

	// Get a pointer to the wave data for a wave in the bank
	void WaveBankReader::GetWaveData(int index, uint8_t const*& data, uint32_t& dataSize) const
	{
		return pImpl->GetWaveData(index, data, dataSize);
	}

	// Get a pointer to the seek table in the bank
	void WaveBankReader::GetSeekTable(int index, uint32_t const*& data, uint32_t& data_count, uint32_t& tag) const
	{
		return pImpl->GetSeekTable(index, data, data_count, tag);
	}

	// Get the meta data for a wave in the bank
	void WaveBankReader::GetMetadata(int index, Metadata& metadata) const
	{
		return pImpl->GetMetadata(index, metadata);
	}

	// Get the file handle used for overlapped reads (streaming wave bank only)
	HANDLE WaveBankReader::GetAsyncHandle() const
	{
		return (pImpl->m_data.dwFlags & BANKDATA::TYPE_STREAMING) ? pImpl->m_async.get() : INVALID_HANDLE_VALUE;
	}

	#pragma endregion

	#pragma region Wave Bank Builder

	struct WaveBankBuilder::Impl
	{
		struct WaveFile
		{
			std::filesystem::path m_filepath;
			WavData m_hdr;
			std::unique_ptr<uint8_t[]> m_data;
			MINIWAVEFORMAT m_mini_fmt;

			WaveFile()
				:m_filepath()
				,m_hdr()
				,m_data()
				,m_mini_fmt()
			{}
			WaveFile(std::filesystem::path const& filepath)
				:WaveFile()
			{
				m_filepath = filepath;

				// Load the wave file into memory
				LoadWAVAudioFromFile(filepath, m_data, m_hdr);

				// Create the mini format for this wave
				auto& wfx = *m_hdr.wfx;
				if (wfx.nChannels == 0) throw std::runtime_error("ERROR: Wave bank entry must have at least 1 channel");
				if (wfx.nChannels > 7) throw std::runtime_error("ERROR: Wave banks only support up to 7 channels");
				if (wfx.nSamplesPerSec == 0) throw std::runtime_error("ERROR: Wave banks entry sample rate must be non-zero");
				if (wfx.nSamplesPerSec > 262143) throw std::runtime_error("ERROR: Wave banks only support sample rates up to 2^18 (262143)");
						
				m_mini_fmt.dwValue = 0;
				m_mini_fmt.nSamplesPerSec = wfx.nSamplesPerSec;
				m_mini_fmt.nChannels = wfx.nChannels;

				switch ((EWaveFormat)wfx.wFormatTag)
				{
				case EWaveFormat::PCM:
					{
						if ((wfx.wBitsPerSample != 8) && (wfx.wBitsPerSample != 16))
							throw std::runtime_error("ERROR: Wave banks only support 8-bit or 16-bit integer PCM data");
						if (wfx.nBlockAlign >= 256)
							throw std::runtime_error(FmtS("ERROR: Wave banks only support block alignments up to 255 %(%u)", wfx.nBlockAlign));
						if (wfx.nBlockAlign != (wfx.nChannels * wfx.wBitsPerSample / 8))
							throw std::runtime_error(FmtS("ERROR: nBlockAlign (%u) != nChannels (%u) * wBitsPerSample (%u) / 8", wfx.nBlockAlign, wfx.nChannels, wfx.wBitsPerSample));
						if (wfx.nAvgBytesPerSec != (wfx.nSamplesPerSec * wfx.nBlockAlign))
							throw std::runtime_error(FmtS("ERROR: nAvgBytesPerSec (%lu) != nSamplesPerSec (%lu) * nBlockAlign (%u)", wfx.nAvgBytesPerSec, wfx.nSamplesPerSec, wfx.nBlockAlign));

						m_mini_fmt.wFormatTag = MINIWAVEFORMAT::TAG_PCM;
						m_mini_fmt.wBitsPerSample = (wfx.wBitsPerSample == 16) ? MINIWAVEFORMAT::BITDEPTH_16 : MINIWAVEFORMAT::BITDEPTH_8;
						m_mini_fmt.wBlockAlign = wfx.nBlockAlign;
						return;
					}
				case EWaveFormat::ADPCM:
					{
						if ((wfx.nChannels != 1) && (wfx.nChannels != 2))
							throw std::runtime_error(FmtS("ERROR: ADPCM wave format must have 1 or 2 channels (not %u)", wfx.nChannels));
						if (wfx.wBitsPerSample != MSADPCM_BITS_PER_SAMPLE)
							throw std::runtime_error(FmtS("ERROR: ADPCM wave format must have 4 bits per sample (not %u)", wfx.wBitsPerSample));
						if (wfx.cbSize != MSADPCM_FORMAT_EXTRA_BYTES)
							throw std::runtime_error(FmtS("ERROR: ADPCM wave format must have cbSize = 32 (not %u)", wfx.cbSize));

						auto wfadpcm = reinterpret_cast<ADPCMWAVEFORMAT const*>(&wfx);
						if (wfadpcm->wNumCoef != MSADPCM_NUM_COEFFICIENTS)
							throw std::runtime_error(FmtS("ERROR: ADPCM wave format must have 7 coefficients (not %u)", wfadpcm->wNumCoef));

						bool valid = true;
						for (int j = 0; j < MSADPCM_NUM_COEFFICIENTS; ++j)
						{
							// Microsoft ADPCM standard encoding coefficients
							static const short g_pAdpcmCoefficients1[] = { 256,  512, 0, 192, 240,  460,  392 };
							static const short g_pAdpcmCoefficients2[] = { 0, -256, 0,  64,   0, -208, -232 };
							valid &=
								wfadpcm->aCoef[j].iCoef1 == g_pAdpcmCoefficients1[j] &&
								wfadpcm->aCoef[j].iCoef2 == g_pAdpcmCoefficients2[j];
						}
						if (!valid)
							throw std::runtime_error("ERROR: Non-standard coefficients for ADPCM found");

						if ((wfadpcm->wSamplesPerBlock < MSADPCM_MIN_SAMPLES_PER_BLOCK) ||
							(wfadpcm->wSamplesPerBlock > MSADPCM_MAX_SAMPLES_PER_BLOCK))
							throw std::runtime_error("ERROR: ADPCM wave format wSamplesPerBlock must be 4..64000");

						if (wfadpcm->wfx.nChannels == 1 && (wfadpcm->wSamplesPerBlock % 2))
							throw std::runtime_error("ERROR: ADPCM wave format mono files must have even wSamplesPerBlock");

						int nHeaderBytes = MSADPCM_HEADER_LENGTH * wfx.nChannels;
						int nBitsPerFrame = MSADPCM_BITS_PER_SAMPLE * wfx.nChannels;
						int nPcmFramesPerBlock = (wfx.nBlockAlign - nHeaderBytes) * 8 / nBitsPerFrame + 2;
						if (wfadpcm->wSamplesPerBlock != nPcmFramesPerBlock)
							throw std::runtime_error(FmtS("ERROR: ADPCM %u-channel format with nBlockAlign = %u must have wSamplesPerBlock = %u (not %u)",wfx.nChannels, wfx.nBlockAlign, nPcmFramesPerBlock, wfadpcm->wSamplesPerBlock));

						// AdpcmBlockSizeFromPcmFrames
						WORD block_align = 0;
						if (wfadpcm->wSamplesPerBlock != 0)
						{
							// The full calculation is as follows:
							//    UINT uHeaderBytes = MSADPCM_HEADER_LENGTH * nChannels;
							//    UINT uBitsPerSample = MSADPCM_BITS_PER_SAMPLE * nChannels;
							//    UINT uBlockAlign = uHeaderBytes + (nPcmFrames - 2) * uBitsPerSample / 8;
							//    return WORD(uBlockAlign);
							WORD const nChannels = 1;
							assert(nChannels == 1 || nChannels == 2);
							assert((nChannels != 1 || (wfadpcm->wSamplesPerBlock % 2) == 0) && "Mono data needs even nPcmFrames");
							block_align = (nChannels == 1)
								? WORD(wfadpcm->wSamplesPerBlock / 2 + 6)
								: WORD(wfadpcm->wSamplesPerBlock + 12);
						}
						m_mini_fmt.wFormatTag = MINIWAVEFORMAT::TAG_ADPCM;
						m_mini_fmt.wBitsPerSample = 0;
						m_mini_fmt.wBlockAlign = block_align - MINIWAVEFORMAT::ADPCM_BLOCKALIGN_CONVERSION_OFFSET;
						return;
					}
				case EWaveFormat::WMAUDIO2:
				case EWaveFormat::WMAUDIO3:
					{
						if (m_hdr.seek == nullptr)
							throw std::runtime_error("ERROR: xWMA requires seek tables ('dpds' chunk)");
						if (wfx.wBitsPerSample != 16)
							throw std::runtime_error("ERROR: Wave banks only support 16-bit xWMA data");
						if (!wfx.nBlockAlign)
							throw std::runtime_error("ERROR: Wave bank xWMA must have a non-zero nBlockAlign");
						if (!wfx.nAvgBytesPerSec)
							throw std::runtime_error("ERROR: Wave bank xWMA must have a non-zero nAvgBytesPerSec");
						if (wfx.cbSize != 0)
							throw std::runtime_error("ERROR: Unexpected data found in xWMA header");

						m_mini_fmt.wFormatTag = MINIWAVEFORMAT::TAG_WMA;
						m_mini_fmt.wBitsPerSample = (EWaveFormat(wfx.wFormatTag) == EWaveFormat::WMAUDIO3) ? MINIWAVEFORMAT::BITDEPTH_16 : MINIWAVEFORMAT::BITDEPTH_8;
						{
							auto blockAlign = EncodeWMABlockAlign(wfx.nBlockAlign, wfx.nAvgBytesPerSec);
							if (blockAlign == DWORD(-1))
								throw std::runtime_error("ERROR: Failed encoding nBlockAlign and nAvgBytesPerSec for xWMA");

							m_mini_fmt.wBlockAlign = blockAlign;
						}
						return;
					}
				case EWaveFormat::EXTENSIBLE:
					{
						if (wfx.cbSize < (sizeof(WAVEFORMATEXTENSIBLE) - sizeof(WAVEFORMATEX)))
							throw std::runtime_error(FmtS("ERROR: WAVEFORMATEXTENSIBLE cbSize must be at least %Iu (%u)", (sizeof(WAVEFORMATEXTENSIBLE) - sizeof(WAVEFORMATEX)), wfx.cbSize));

						static const GUID s_wfexBase = { 0x00000000, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71 };
						auto wfex = reinterpret_cast<WAVEFORMATEXTENSIBLE const*>(&wfx);

						if (memcmp(
							reinterpret_cast<const BYTE*>(&wfex->SubFormat) + sizeof(DWORD),
							reinterpret_cast<const BYTE*>(&s_wfexBase) + sizeof(DWORD),
							sizeof(GUID) - sizeof(DWORD)) != 0)
						{
							throw std::runtime_error(FmtS("ERROR: WAVEFORMATEXTENSIBLE encountered with unknown GUID ({%8.8lX-%4.4X-%4.4X-%2.2X%2.2X-%2.2X%2.2X%2.2X%2.2X%2.2X%2.2X})",
								wfex->SubFormat.Data1, wfex->SubFormat.Data2, wfex->SubFormat.Data3,
								wfex->SubFormat.Data4[0], wfex->SubFormat.Data4[1], wfex->SubFormat.Data4[2], wfex->SubFormat.Data4[3],
								wfex->SubFormat.Data4[4], wfex->SubFormat.Data4[5], wfex->SubFormat.Data4[6], wfex->SubFormat.Data4[7]));
						}

						switch ((EWaveFormat)wfex->SubFormat.Data1)
						{
						case EWaveFormat::PCM:
							{
								if ((wfx.wBitsPerSample != 8) && (wfx.wBitsPerSample != 16))
									throw std::runtime_error(FmtS("ERROR: Wave banks only support 8-bit or 16-bit integer PCM data (%u)", wfx.wBitsPerSample));
								if ((wfex->Samples.wValidBitsPerSample == 0) ||
									(wfex->Samples.wValidBitsPerSample != 8 && wfex->Samples.wValidBitsPerSample != 16) ||
									(wfex->Samples.wValidBitsPerSample > wfx.wBitsPerSample))
									throw std::runtime_error(FmtS("ERROR: Unexpected wValidBitsPerSample value (%u)", wfex->Samples.wValidBitsPerSample));
								if (wfx.nBlockAlign > 255)
									throw std::runtime_error(FmtS("ERROR: Wave banks only support block alignments up to 255 %(%u)", wfx.nBlockAlign));
								if (wfx.nBlockAlign != (wfx.nChannels * wfx.wBitsPerSample / 8))
									throw std::runtime_error(FmtS("ERROR: nBlockAlign (%u) != nChannels (%u) * wBitsPerSample (%u) / 8\n", wfx.nBlockAlign, wfx.nChannels, wfx.wBitsPerSample));
								if (wfx.nAvgBytesPerSec != (wfx.nSamplesPerSec * wfx.nBlockAlign))
									throw std::runtime_error(FmtS("ERROR: nAvgBytesPerSec (%lu) != nSamplesPerSec (%lu) * nBlockAlign (%u)\n", wfx.nAvgBytesPerSec, wfx.nSamplesPerSec, wfx.nBlockAlign));

								m_mini_fmt.wFormatTag = MINIWAVEFORMAT::TAG_PCM;
								m_mini_fmt.wBitsPerSample = (wfex->Samples.wValidBitsPerSample == 16) ? MINIWAVEFORMAT::BITDEPTH_16 : MINIWAVEFORMAT::BITDEPTH_8;
								m_mini_fmt.wBlockAlign = wfx.nBlockAlign;
								break;
							}
						case EWaveFormat::WMAUDIO2:
						case EWaveFormat::WMAUDIO3:
							{
								if (m_hdr.seek == nullptr)
									throw std::runtime_error("ERROR: xWMA requires seek tables (DPDS chunk)");
								if (wfx.wBitsPerSample != 16)
									throw std::runtime_error("ERROR: Wave banks only support 16-bit xWMA data");
								if (!wfx.nBlockAlign)
									throw std::runtime_error("ERROR: Wave bank xWMA must have a non-zero nBlockAlign");
								if (!wfx.nAvgBytesPerSec)
									throw std::runtime_error("ERROR: Wave bank xWMA must have a non-zero nAvgBytesPerSec");

								auto blockAlign = EncodeWMABlockAlign(wfx.nBlockAlign, wfx.nAvgBytesPerSec);
								if (blockAlign == DWORD(-1))
									throw std::runtime_error("ERROR: Failed encoding nBlockAlign and nAvgBytesPerSec for xWMA");

								m_mini_fmt.wFormatTag = MINIWAVEFORMAT::TAG_WMA;
								m_mini_fmt.wBitsPerSample = (EWaveFormat(wfx.wFormatTag) == EWaveFormat::WMAUDIO3) ? MINIWAVEFORMAT::BITDEPTH_16 : MINIWAVEFORMAT::BITDEPTH_8;
								m_mini_fmt.wBlockAlign = blockAlign;
								break;
							}
						case EWaveFormat::IEEE_FLOAT:
							{
								throw std::runtime_error("ERROR: Wave banks do not support float PCM data");
							}
						case EWaveFormat::ADPCM:
							{
								throw std::runtime_error("ERROR: ADPCM is not supported as a WAVEFORMATEXTENSIBLE");
							}
						case EWaveFormat::XMA2:
							{
								throw std::runtime_error("ERROR: XMA2 is not supported as a WAVEFORMATEXTENSIBLE");
							}
						default:
							{
								throw std::runtime_error("ERROR: Unknown WAVEFORMATEXTENSIBLE format tag");
							}
						}

						if (wfex->dwChannelMask)
						{
							// Note: WAVEFORMATEXTENSIBLE ChannelMask is ignored in wave banks
							WORD channelBits = 0;
							{
								auto x = wfex->dwChannelMask;
								while (x) { ++channelBits; x &= (x - 1); }
							}
							if (channelBits != wfx.nChannels)
								throw std::runtime_error(FmtS("ERROR: WAVEFORMATEXTENSIBLE: nChannels=%u but ChannelMask has %u bits set", wfx.nChannels, channelBits));
						}
						return;
					}
				case EWaveFormat::IEEE_FLOAT:
					{
						throw std::runtime_error("ERROR: Wave banks do not support IEEE float PCM data");
					}
				case EWaveFormat::XMA2:
					{
						throw std::runtime_error("ERROR: XMA2 not supported");
					}
				default:
					{
						throw std::runtime_error(FmtS("Unknown WAVE tag: %d", wfx.wFormatTag));
					}
				}
			}
	
			// Return the friendly name for this wave
			NAME Name() const
			{
				WCHAR fname[_MAX_FNAME];
				_wsplitpath_s(m_filepath.c_str(), nullptr, 0, nullptr, 0, fname, _MAX_FNAME, nullptr, 0);

				NAME name;
				WideCharToMultiByte(CP_ACP, WC_NO_BEST_FIT_CHARS, fname, -1, name.str, _countof(name.str), nullptr, FALSE);

				// Sanitise it
				for (auto& ch : name.str)
					ch = iswdigit(ch) || iswalpha(ch) ? char(towupper(ch)) : '_';

				return name;
			}

			// Return the block alignment, encoded into a DWORD
			DWORD EncodeWMABlockAlign(DWORD dwBlockAlign, DWORD dwAvgBytesPerSec)
			{
				static const uint32_t aWMABlockAlign[] = { 929, 1487, 1280, 2230, 8917, 8192, 4459, 5945, 2304, 1536, 1485, 1008, 2731, 4096, 6827, 5462, 1280 };
				static const uint32_t aWMAAvgBytesPerSec[] = { 12000, 24000, 4000, 6000, 8000, 20000, 2500 };

				auto bit = std::find(std::begin(aWMABlockAlign), std::end(aWMABlockAlign), dwBlockAlign);
				if (bit == std::end(aWMABlockAlign))
					return DWORD(-1);

				DWORD blockAlignIndex = DWORD(bit - std::begin(aWMABlockAlign));

				auto ait = std::find(std::begin(aWMAAvgBytesPerSec), std::end(aWMAAvgBytesPerSec), dwAvgBytesPerSec);
				if (ait == std::end(aWMAAvgBytesPerSec))
					return DWORD(-1);

				DWORD bytesPerSecIndex = DWORD(ait - std::begin(aWMAAvgBytesPerSec));

				return DWORD(blockAlignIndex | (bytesPerSecIndex << 5));
			}

			void PrintInfo()
			{
				wprintf( L" (%S %u channels, %u-bit, %u Hz)", FormatTagName(m_hdr.wfx->wFormatTag), m_hdr.wfx->nChannels, m_hdr.wfx->wBitsPerSample, m_hdr.wfx->nSamplesPerSec);
			}
		};
		std::vector<WaveFile> m_waves;

		// Reset the builder
		void Clear()
		{
			m_waves.resize(0);
		}

		// Add a wave file to the wave bank
		void Add(std::filesystem::path const& filepath)
		{
			m_waves.emplace_back(filepath);
		}

		// The number of waves added so far
		int Count() const
		{
			return int(m_waves.size());
		}

		// Create the wave bank
		void Write(char const* bank_name, std::ostream& xwb, EOptions opts) const
		{
			// No waves, nothing to output
			if (m_waves.empty())
				return;

			// Options
			auto compact = AllSet(opts, EOptions::Compact);
			auto streaming = AllSet(opts, EOptions::Streaming);
			auto friendly_names = AllSet(opts, EOptions::FriendlyNames);
			auto alignment = AllSet(opts, EOptions::Streaming) ? ALIGNMENT_DVD : ALIGNMENT_MIN;
			auto entry_size = compact ? uint32_t(sizeof(ENTRYCOMPACT)) : uint32_t(sizeof(ENTRY));

			// Sanity check the size of the wave bank and collect data to calculate offsets
			size_t seek_entries = 0;
			uint64_t wave_offset = 0;
			auto compact_format = m_waves.front().m_mini_fmt;
			for (auto& wave : m_waves)
			{
				// Sanity check compact
				if (compact && compact_format.dwValue != wave.m_mini_fmt.dwValue)
					throw std::runtime_error("ERROR: Cannot create compact wave bank: Mismatched formats. All formats must be identical for a compact wave bank.");
				if (compact && wave.m_hdr.loop_length > 0)
					throw std::runtime_error("ERROR: Cannot create compact wave bank: Found loop points. Compact wave banks do not support loop points.");

				// Look for seek entries
				if (wave.m_mini_fmt.wFormatTag == MINIWAVEFORMAT::TAG_WMA && wave.m_hdr.seek_count > 0)
					seek_entries += wave.m_hdr.seek_count + 1;

				// Calculate the total wave data size
				wave_offset += PadTo(wave.m_hdr.audio_bytes, alignment);
			}
			if (wave_offset > 0xFFFFFFFF)
				throw std::runtime_error(FmtS("ERROR: Audio wave data is too large to encode into a wave bank (offset %I64u)", wave_offset));
			if (compact && wave_offset > (MAX_COMPACT_DATA_SEGMENT_SIZE * alignment))
				throw std::runtime_error(FmtS("ERROR: Cannot create compact wave bank: Audio wave data is too large to encode in compact wave bank (%I64u > %I64u).", wave_offset, uint64_t(MAX_COMPACT_DATA_SEGMENT_SIZE * alignment)));
			if (seek_entries != 0)
				seek_entries += m_waves.size();

			auto segment_offset = DWORD(0);
			auto segment_size = DWORD(sizeof(HEADER));

			// Write the wave bank header
			HEADER header = {0};
			header.dwSignature = HEADER::SIGNATURE;
			header.dwHeaderVersion = HEADER::VERSION;
			header.dwVersion = XACT_CONTENT_VERSION;
			header.Segments[HEADER::SEGIDX_BANKDATA     ].dwOffset = (segment_offset += segment_size);
			header.Segments[HEADER::SEGIDX_BANKDATA     ].dwLength = (segment_size = sizeof(BANKDATA));
			header.Segments[HEADER::SEGIDX_ENTRYMETADATA].dwOffset = (segment_offset += segment_size);
			header.Segments[HEADER::SEGIDX_ENTRYMETADATA].dwLength = (segment_size = DWORD(m_waves.size() * entry_size));
			header.Segments[HEADER::SEGIDX_SEEKTABLES   ].dwOffset = (segment_offset += segment_size);
			header.Segments[HEADER::SEGIDX_SEEKTABLES   ].dwLength = (segment_size = DWORD(seek_entries * sizeof(uint32_t)));
			header.Segments[HEADER::SEGIDX_ENTRYNAMES   ].dwOffset = (segment_offset += segment_size);
			header.Segments[HEADER::SEGIDX_ENTRYNAMES   ].dwLength = (segment_size = DWORD((friendly_names ? m_waves.size() : 0) * NAME_LENGTH));
			header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwOffset = (segment_offset = PadTo(segment_offset + segment_size, DWORD(alignment)));
			header.Segments[HEADER::SEGIDX_ENTRYWAVEDATA].dwLength = (segment_size = DWORD(wave_offset));
			xwb.write(char_ptr(&header), sizeof(header));

			// Write the bank meta data
			BANKDATA bank_data                   = {0};
			bank_data.bankName                   = bank_name;
			bank_data.dwEntryCount               = uint32_t(m_waves.size());
			bank_data.alignment                  = alignment;
			bank_data.BuildTime                  = []{ FILETIME ft; GetSystemTimeAsFileTime(&ft); return ft; }();
			bank_data.dwEntryNameElementSize     = friendly_names ? NAME_LENGTH : 0;
			bank_data.dwEntryMetaDataElementSize = entry_size;
			bank_data.CompactFormat.dwValue      = compact ? compact_format.dwValue : 0;
			bank_data.dwFlags                    =
				(compact ? BANKDATA::FLAGS_COMPACT : 0) |
				(streaming ? BANKDATA::TYPE_STREAMING : BANKDATA::TYPE_BUFFER) |
				(friendly_names ? BANKDATA::FLAGS_ENTRYNAMES : 0) |
				(seek_entries != 0 ? BANKDATA::FLAGS_SEEKTABLES : 0);
			xwb.write(char_ptr(&bank_data), sizeof(BANKDATA));

			// Write the entry meta data
			wave_offset = 0;
			for (auto& wave : m_waves)
			{
				auto& wfx = wave.m_hdr.wfx;
				auto aligned_size = PadTo(wave.m_hdr.audio_bytes, alignment);

				// Write each entry
				if (compact)
				{
					assert(alignment <= 2048);
					assert(wave_offset <= (MAX_COMPACT_DATA_SEGMENT_SIZE * alignment));

					ENTRYCOMPACT entry = {0};
					entry.dwOffset = uint32_t(wave_offset / alignment);
					entry.dwLengthDeviation = aligned_size - wave.m_hdr.audio_bytes;
					xwb.write(char_ptr(&entry), sizeof(entry));
				}
				else
				{
					// Calculate the duration of the wave
					uint64_t duration = 0;
					switch (wave.m_mini_fmt.wFormatTag)
					{
					case MINIWAVEFORMAT::TAG_ADPCM:
						{
							auto adpcmFmt = reinterpret_cast<const ADPCMEWAVEFORMAT*>(wfx);
							duration = (wave.m_hdr.audio_bytes / wfx->nBlockAlign) * adpcmFmt->wSamplesPerBlock;
							auto partial = wave.m_hdr.audio_bytes % wfx->nBlockAlign;
							if (partial != 0)
							{
								if (partial >= uint32_t(7 * wfx->nChannels))
									duration += (partial * 2 / wfx->nChannels - 12);
							}
							break;
						}
					case MINIWAVEFORMAT::TAG_WMA:
						{
							if (wave.m_hdr.seek_count > 0)
								duration = wave.m_hdr.seek[wave.m_hdr.seek_count - 1] / uint32_t(2 * wfx->nChannels);

							break;
						}
					case MINIWAVEFORMAT::TAG_PCM:
					default:
						{
							duration = (uint64_t(wave.m_hdr.audio_bytes) * 8) / uint64_t(wfx->wBitsPerSample * wfx->nChannels);
							break;
						}
					}
					if (duration > 268435455)
						throw std::runtime_error(FmtS("ERROR: Duration of audio too long to encode into wave bank (%I64u > 2^28))\n", duration));

					ENTRY entry = {0};
					entry.Duration = uint32_t(duration);
					memcpy(&entry.Format, &wave.m_mini_fmt, sizeof(wave.m_mini_fmt));
					entry.PlayRegion.dwOffset = uint32_t(wave_offset);
					entry.PlayRegion.dwLength = wave.m_hdr.audio_bytes;
					if (wave.m_hdr.loop_length > 0)
					{
						entry.LoopRegion.dwStartSample = wave.m_hdr.loop_start;
						entry.LoopRegion.dwTotalSamples = wave.m_hdr.loop_length;
					}
					xwb.write(char_ptr(&entry), sizeof(entry));
				}

				wave_offset += aligned_size;
			}

			// Write the seek tables
			if (seek_entries != 0)
			{
				// Compile the seek table in a buffer
				std::unique_ptr<uint32_t> seek_table_buf(new uint32_t[seek_entries]);
				uint32_t* seek_table = seek_table_buf.get();

				// Create the seek table
				int index = 0;
				uint32_t seekoffset = 0;
				for (auto& wave : m_waves)
				{
					seek_table[index] = uint32_t(-1);
					if (wave.m_mini_fmt.wFormatTag == MINIWAVEFORMAT::TAG_WMA)
					{
						seek_table[index] = seekoffset * sizeof(uint32_t);

						auto baseoffset = uint32_t(m_waves.size() + seekoffset);
						seek_table[baseoffset] = wave.m_hdr.seek_count;

						for (int j = 0; j != int(wave.m_hdr.seek_count); ++j)
							seek_table[baseoffset + j + 1] = wave.m_hdr.seek[j];

						seekoffset += wave.m_hdr.seek_count + 1;
					}
					++index;
				}

				// Write the seek table
				xwb.write(char_ptr(seek_table), seek_entries * sizeof(uint32_t));
			}

			// Write entry names
			if (friendly_names)
			{
				for (auto& wave : m_waves)
				{
					auto name = wave.Name();
					xwb.write(char_ptr(&name), sizeof(name));
				}
			}

			// Write wave data
			for (auto& wave : m_waves)
			{
				// Pad to the alignment boundary
				auto pad = Pad(uint32_t(xwb.tellp()), alignment);
				for (;pad-- != 0;) xwb.write("", 1);

				// Write the wave bytes
				xwb.write(char_ptr(wave.m_hdr.audio_start), wave.m_hdr.audio_bytes);
			}

			// Check we've written the correct number of bytes
			assert(xwb.tellp() == std::streamsize(segment_offset + segment_size));
		}

		// Create a C header file to 'hdr'
		void WriteHeader(char const* bank_name, std::ostream& hdr) const
		{
			hdr << "// This is a generated file\r\n"
				<< "#pragma once\r\n"
				<< "\r\n"
				<< "// XACT Wave Bank Entries\r\n"
				<< "enum class " << bank_name << "\r\n"
				<< "{\r\n";

			int index = 0;
			for (auto& wave : m_waves)
			{
				hdr << "\t" << wave.Name().str << " = " << index << ",\r\n";
				++index;
			}

			hdr << "};\r\n"
				<< "static int const " << bank_name << "Count = " << index << ";\r\n";
		}
	};

	WaveBankBuilder::WaveBankBuilder()
		:pImpl(new Impl)
	{}

	// Reset the builder
	void WaveBankBuilder::Clear()
	{
		pImpl->Clear();
	}

	// Add a wave file to the wave bank
	void WaveBankBuilder::Add(std::filesystem::path const& filepath)
	{
		pImpl->Add(filepath);
	}

	// The number of waves added so far
	int WaveBankBuilder::Count() const
	{
		return pImpl->Count();
	}

	// Create the wave bank
	void WaveBankBuilder::Write(char const* bank_name, std::ostream& xwb, EOptions opts) const
	{
		pImpl->Write(bank_name, xwb, opts);
	}
	void WaveBankBuilder::Write(char const* bank_name, std::filesystem::path const& xwb_filepath, EOptions opts) const
	{
		// Check for file overwriting
		if (!AllSet(opts, EOptions::Overwrite) && exists(xwb_filepath))
			throw std::runtime_error(FmtS("ERROR: Output file '%S' already exists", xwb_filepath.c_str()));

		std::ofstream xwb(xwb_filepath);
		Write(bank_name, xwb, opts);
	}

	// Create a C header file to 'hdr'
	void WaveBankBuilder::WriteHeader(char const* bank_name, std::ostream& hdr) const
	{
		pImpl->WriteHeader(bank_name, hdr);
	}
	void WaveBankBuilder::WriteHeader(char const* bank_name, std::filesystem::path const& header_filepath, EOptions opts) const
	{
		// Check for file overwriting
		if (!AllSet(opts, EOptions::Overwrite) && exists(header_filepath))
			throw std::runtime_error(FmtS("ERROR: Output header file '%S' already exists", header_filepath.c_str()));

		std::ofstream hdr(header_filepath);
		WriteHeader(bank_name, hdr);
	}

	#pragma endregion
}
#pragma warning(default:4201) // nameless structs

