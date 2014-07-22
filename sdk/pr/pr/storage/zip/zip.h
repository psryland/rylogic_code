//******************************************************************
//
//	Compression library based on Gzip/GUnzip
//
//******************************************************************
#pragma once

#include "pr/common/assert.h"
#include "pr/container/byte_data.h"

namespace pr
{
	namespace zip
	{
		enum EResult
		{
			EResult_Success		= 0,
			EResult_SuccessCopy = 1,
			EResult_Failed		= 0x80000000,
		};

		// Compression functions
		enum ELevel
		{
			ELevel_Min	= 0,
			ELevel_0	= ELevel_Min,
			ELevel_1,
			ELevel_2,
			ELevel_3,
			ELevel_4,
			ELevel_5,
			ELevel_6,
			ELevel_7,
			ELevel_8,
			ELevel_9,
			ELevel_10,
			ELevel_11,
			ELevel_Max	= ELevel_11
		};

		// Use this function to get the minimum size of the
		// 'compressed' buffer that should be passed to 'Compress()'
		std::size_t GetCompressionBufferSize(unsigned int data_length);
		
		// Use this function to read the decompressed size of the data
		std::size_t GetDecompressedSize     (const void* compressed_data);
		
		// Use this function to read the actual size of the compressed data
		std::size_t GetCompressedSize       (const void* compressed_data);
		
		// Compress/Decompress data 
		EResult     Decompress              (const void* data, unsigned int data_length, void* decompressed);
		EResult     Compress                (const void* data, unsigned int data_length, void* compressed, unsigned int level);
		inline EResult Compress             (const void* data, unsigned int data_length, void* compressed) { return Compress(data, data_length, compressed, 4); }
		
		// Helper functions for use with binary data
		inline EResult Compress(const void* data, unsigned int data_length, ByteCont& compressed, unsigned int level)
		{
			compressed.resize(GetCompressionBufferSize(data_length));
			EResult result = Compress(data, data_length, &compressed[0], level);
			compressed.resize(GetCompressedSize(&compressed[0]));
			return result;
		}
		inline EResult Compress(const void* data, unsigned int data_length, ByteCont& compressed) { return Compress(data, data_length, compressed, 4); }
		inline EResult Decompress(const void* data, unsigned int data_length, ByteCont& decompressed)
		{
			decompressed.resize(GetDecompressedSize(data));
			return Decompress(data, data_length, &decompressed[0]);
		}

	}

	// Result testing
	inline bool Failed   (zip::EResult result)	{ return result <  0; }
	inline bool Succeeded(zip::EResult result)	{ return result >= 0; }
	inline void Verify   (zip::EResult result)	{ (void)result; PR_ASSERT(PR_DBG, Succeeded(result), "Verify failure"); }
}
